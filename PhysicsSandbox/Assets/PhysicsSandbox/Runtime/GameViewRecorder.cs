#if UNITY_EDITOR || UNITY_STANDALONE
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Records the Game view to an H.264 file by piping raw frames to ffmpeg. Game time is
    /// locked to the capture frame rate, so the video is smooth and its timestamps equal
    /// game time however long each frame takes to encode.
    /// </summary>
    public class GameViewRecorder : MonoBehaviour
    {
        string m_FfmpegPath;
        string m_OutputPath;
        int m_Fps;
        int m_Width;
        int m_Height;
        byte[] m_Buffer;
        Process m_Ffmpeg;
        Stream m_Pipe;

        public bool IsRecording { get; private set; }
        public int FrameCount { get; private set; }
        public string Error { get; private set; }
        public string Resolution => $"{m_Width}x{m_Height}";

        public void Begin(string ffmpegPath, string outputPath, int fps)
        {
            m_FfmpegPath = ffmpegPath;
            m_OutputPath = outputPath;
            m_Fps = fps;
            FrameCount = 0;
            Error = null;
            Time.captureFramerate = fps;
            IsRecording = true;
            StartCoroutine(Capture());
        }

        public void End()
        {
            IsRecording = false;
            Time.captureFramerate = 0;

            try
            {
                if (m_Pipe != null)
                {
                    m_Pipe.Flush();
                    m_Pipe.Close();
                }

                if (m_Ffmpeg != null)
                {
                    m_Ffmpeg.WaitForExit(60000);
                    m_Ffmpeg.Dispose();
                }
            }
            catch (Exception e)
            {
                Error = e.Message;
            }

            m_Pipe = null;
            m_Ffmpeg = null;
        }

        void OnDestroy()
        {
            if (IsRecording)
                End();
        }

        IEnumerator Capture()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (IsRecording)
            {
                yield return endOfFrame;
                if (!IsRecording)
                    break;

                var frame = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    if (m_Ffmpeg == null)
                        StartFfmpeg(frame);

                    // A resized Game view would corrupt the stream, so such frames are dropped.
                    if (frame.width == m_Width && frame.height == m_Height)
                    {
                        frame.GetRawTextureData<byte>().CopyTo(m_Buffer);
                        m_Pipe.Write(m_Buffer, 0, m_Buffer.Length);
                        FrameCount++;
                    }
                }
                catch (Exception e)
                {
                    Error = e.Message;
                    IsRecording = false;
                }
                finally
                {
                    Destroy(frame);
                }
            }
        }

        void StartFfmpeg(Texture2D firstFrame)
        {
            m_Width = firstFrame.width;
            m_Height = firstFrame.height;
            m_Buffer = new byte[firstFrame.GetRawTextureData<byte>().Length];

            string pixelFormat;
            switch (firstFrame.format)
            {
                case TextureFormat.RGB24: pixelFormat = "rgb24"; break;
                case TextureFormat.RGBA32: pixelFormat = "rgba"; break;
                case TextureFormat.ARGB32: pixelFormat = "argb"; break;
                case TextureFormat.BGRA32: pixelFormat = "bgra"; break;
                default: throw new NotSupportedException("Unsupported capture format " + firstFrame.format);
            }

            // Unity's rows run bottom-up, hence vflip. Output is letterboxed to 1080p.
            const string filter = "vflip,scale=1920:1080:force_original_aspect_ratio=decrease:flags=lanczos,pad=1920:1080:(ow-iw)/2:(oh-ih)/2";
            var arguments =
                $"-y -loglevel error -f rawvideo -pix_fmt {pixelFormat} -s {m_Width}x{m_Height} -r {m_Fps} -i - " +
                $"-an -vf \"{filter}\" -c:v libx264 -preset veryfast -crf 17 -pix_fmt yuv420p -movflags +faststart \"{m_OutputPath}\"";

            m_Ffmpeg = Process.Start(new ProcessStartInfo(m_FfmpegPath, arguments)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            });
            m_Pipe = m_Ffmpeg.StandardInput.BaseStream;
        }
    }
}
#endif
