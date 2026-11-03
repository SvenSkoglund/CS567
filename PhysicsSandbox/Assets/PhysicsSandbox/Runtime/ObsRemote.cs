#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Starts and stops OBS recording over its local WebSocket, so the demo videos can be
    /// controlled from inside the headset. The address and password come from OBS's own config
    /// file, so nothing secret is kept in the project. Reconnects if OBS is restarted.
    /// </summary>
    public sealed class ObsRemote : IDisposable
    {
        const int k_OutputEvents = 1 << 6;
        const int k_ReconnectMilliseconds = 3000;

        [Serializable]
        class Config
        {
            public int server_port;
            public string server_password;
            public bool auth_required;
        }

        [Serializable]
        class Authentication
        {
            public string challenge;
            public string salt;
        }

        [Serializable]
        class OutputState
        {
            public bool outputActive;
        }

        [Serializable]
        class Payload
        {
            public string eventType;
            public string requestType;
            public OutputState eventData;
            public OutputState responseData;
            public Authentication authentication;
        }

        [Serializable]
        class Message
        {
            public int op;
            public Payload d;
        }

        readonly CancellationTokenSource m_Cancel = new();
        readonly SemaphoreSlim m_SendLock = new(1, 1);
        readonly Stopwatch m_RecordingTime = new();
        ClientWebSocket m_Socket;
        volatile bool m_Connected;
        volatile bool m_Recording;

        public bool Connected => m_Connected;
        public bool Recording => m_Recording;
        public int RecordingSeconds => (int)m_RecordingTime.Elapsed.TotalSeconds;

        public ObsRemote()
        {
            Task.Run(Run);
        }

        public void ToggleRecording()
        {
            if (m_Connected)
                Task.Run(() => SendRequest("ToggleRecord"));
        }

        async Task Run()
        {
            while (!m_Cancel.IsCancellationRequested)
            {
                try
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "obs-studio/plugin_config/obs-websocket/config.json");
                    var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));

                    using var socket = new ClientWebSocket();
                    await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{config.server_port}"), m_Cancel.Token);
                    m_Socket = socket;

                    var hello = JsonUtility.FromJson<Message>(await Receive(socket));
                    await Send(Identify(config, hello.d.authentication));
                    await Receive(socket);
                    await SendRequest("GetRecordStatus");
                    m_Connected = true;

                    while (socket.State == WebSocketState.Open)
                        Handle(JsonUtility.FromJson<Message>(await Receive(socket)));
                }
                catch (Exception) when (!m_Cancel.IsCancellationRequested)
                {
                    // OBS is closed or restarting; try again shortly.
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                m_Connected = false;
                SetRecording(false);
                try
                {
                    await Task.Delay(k_ReconnectMilliseconds, m_Cancel.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        void Handle(Message message)
        {
            if (message.op == 5 && message.d.eventType == "RecordStateChanged")
                SetRecording(message.d.eventData.outputActive);
            else if (message.op == 7 && message.d.requestType == "GetRecordStatus")
                SetRecording(message.d.responseData.outputActive);
        }

        void SetRecording(bool recording)
        {
            if (recording && !m_Recording)
                m_RecordingTime.Restart();
            else if (!recording)
                m_RecordingTime.Reset();
            m_Recording = recording;
        }

        static string Identify(Config config, Authentication authentication)
        {
            var proof = "";
            if (!string.IsNullOrEmpty(authentication.challenge))
            {
                var secret = Hash(config.server_password + authentication.salt);
                proof = $"\"authentication\":\"{Hash(secret + authentication.challenge)}\",";
            }

            return $"{{\"op\":1,\"d\":{{\"rpcVersion\":1,{proof}\"eventSubscriptions\":{k_OutputEvents}}}}}";
        }

        static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        Task SendRequest(string requestType)
        {
            return Send($"{{\"op\":6,\"d\":{{\"requestType\":\"{requestType}\",\"requestId\":\"{Guid.NewGuid()}\",\"requestData\":{{}}}}}}");
        }

        async Task Send(string json)
        {
            await m_SendLock.WaitAsync(m_Cancel.Token);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await m_Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, m_Cancel.Token);
            }
            finally
            {
                m_SendLock.Release();
            }
        }

        async Task<string> Receive(ClientWebSocket socket)
        {
            var buffer = new byte[8192];
            using var text = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), m_Cancel.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    throw new WebSocketException("OBS closed the connection");
                text.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            return Encoding.UTF8.GetString(text.ToArray());
        }

        public void Dispose()
        {
            m_Cancel.Cancel();
            m_Connected = false;
        }
    }
}
#endif
