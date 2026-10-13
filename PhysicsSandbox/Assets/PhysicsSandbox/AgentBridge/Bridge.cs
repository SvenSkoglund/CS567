using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AgentBridge
{
    /// <summary>
    /// File-based command channel that lets the AI coding agent drive an already-open editor:
    /// refresh/compile, call static methods, enter/exit Play mode, and render screenshots.
    /// Lives in its own assembly so it keeps working when the sandbox code fails to compile.
    ///
    /// Protocol (all files in Library/AgentBridge):
    ///   cmd.txt       written by the agent: "&lt;id&gt; &lt;verb&gt; [args]"
    ///   result.txt    written by the bridge: "&lt;id&gt;\n&lt;result&gt;"
    ///   heartbeat.txt rewritten twice a second with editor state
    /// </summary>
    [InitializeOnLoad]
    static class Bridge
    {
        const string k_PendingId = "AgentBridge.PendingRefreshId";
        const string k_PendingTime = "AgentBridge.PendingRefreshTime";

        static readonly string s_Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/AgentBridge"));
        static double s_NextBeat;

        static string CmdPath => Path.Combine(s_Dir, "cmd.txt");
        static string ResultPath => Path.Combine(s_Dir, "result.txt");
        static string BeatPath => Path.Combine(s_Dir, "heartbeat.txt");

        static Bridge()
        {
            Directory.CreateDirectory(s_Dir);
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now >= s_NextBeat)
            {
                s_NextBeat = now + 0.5;
                TryWrite(BeatPath,
                    $"{DateTime.Now:HH:mm:ss} compiling={EditorApplication.isCompiling} updating={EditorApplication.isUpdating} " +
                    $"playing={EditorApplication.isPlaying} compileFailed={EditorUtility.scriptCompilationFailed}");
            }

            // A refresh may trigger a domain reload, so its completion is tracked in SessionState.
            var pendingId = SessionState.GetString(k_PendingId, "");
            if (pendingId.Length > 0)
            {
                var started = SessionState.GetFloat(k_PendingTime, 0f);
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || now < started + 1.5)
                    return;

                SessionState.EraseString(k_PendingId);
                WriteResult(pendingId, $"refresh done compileFailed={EditorUtility.scriptCompilationFailed}");
                return;
            }

            if (!File.Exists(CmdPath))
                return;

            string text;
            try
            {
                text = File.ReadAllText(CmdPath).Trim();
                File.Delete(CmdPath);
            }
            catch (IOException)
            {
                return;
            }

            var parts = text.Split(new[] { ' ' }, 3);
            if (parts.Length < 2)
                return;

            var id = parts[0];
            var verb = parts[1];
            var args = parts.Length > 2 ? parts[2] : "";

            try
            {
                switch (verb)
                {
                    case "refresh":
                        SessionState.SetString(k_PendingId, id);
                        SessionState.SetFloat(k_PendingTime, (float)now);
                        AssetDatabase.Refresh();
                        break;
                    case "exec":
                        WriteResult(id, Exec(args));
                        break;
                    case "play":
                        EditorApplication.isPlaying = true;
                        WriteResult(id, "entering play mode");
                        break;
                    case "stop":
                        EditorApplication.isPlaying = false;
                        WriteResult(id, "exiting play mode");
                        break;
                    case "shot":
                        WriteResult(id, Shot(args));
                        break;
                    case "gameshot":
                        ScreenCapture.CaptureScreenshot(args);
                        WriteResult(id, "requested " + args);
                        break;
                    default:
                        WriteResult(id, "unknown verb " + verb);
                        break;
                }
            }
            catch (Exception e)
            {
                SessionState.EraseString(k_PendingId);
                WriteResult(id, "EXCEPTION " + (e is TargetInvocationException && e.InnerException != null ? e.InnerException : e));
            }
        }

        // "Namespace.Type.Method" or "Namespace.Type.Method some string argument"
        static string Exec(string args)
        {
            var split = args.Split(new[] { ' ' }, 2);
            var path = split[0];
            var dot = path.LastIndexOf('.');
            var typeName = path.Substring(0, dot);
            var methodName = path.Substring(dot + 1);

            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    break;
            }

            if (type == null)
                return "type not found: " + typeName;

            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                return "method not found: " + methodName;

            var parameters = method.GetParameters().Length == 0 ? null : new object[] { split.Length > 1 ? split[1] : "" };
            var result = method.Invoke(null, parameters);
            return result != null ? result.ToString() : "ok";
        }

        // "<path> px py pz rx ry rz [fov]" renders the open scene from a temporary camera.
        static string Shot(string args)
        {
            var a = args.Split(' ');
            float F(int i) => float.Parse(a[i], CultureInfo.InvariantCulture);

            var go = new GameObject("~AgentShotCamera") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(new Vector3(F(1), F(2), F(3)), Quaternion.Euler(F(4), F(5), F(6)));
                cam.fieldOfView = a.Length > 7 ? F(7) : 60f;
                cam.nearClipPlane = 0.03f;
                cam.targetTexture = rt;
                cam.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;

                File.WriteAllBytes(a[0], tex.EncodeToPNG());
                cam.targetTexture = null;
                return "wrote " + a[0];
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        static void WriteResult(string id, string result) => TryWrite(ResultPath, id + "\n" + result);

        static void TryWrite(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (IOException)
            {
                // The agent may be reading the file; the next write will succeed.
            }
        }
    }
}
