using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PhysicsSandbox.Editor
{
    /// <summary>
    /// Records the narrated scripted demo to a video file. Called through the AgentBridge
    /// while in Play mode with "ffmpegPath|outputPath". Writes "&lt;output&gt;.status.txt" as it
    /// goes and the segment timeline when it finishes.
    /// </summary>
    public static class SandboxRecorder
    {
        const int k_Fps = 30;

        static string s_FfmpegPath;
        static string s_OutputPath;
        static GameViewRecorder s_Recorder;
        static DemoDirector s_Director;
        static int s_WarmupFrames;
        static int s_PreviousSizeIndex = -1;
        static string s_SizeNote;

        public static string RecordNarratedDemo(string args)
        {
            if (!Application.isPlaying)
                return "enter Play mode first";
            if (s_Recorder != null)
                return "already recording";

            var split = args.Split('|');
            s_FfmpegPath = split[0].Trim();
            s_OutputPath = split[1].Trim();
            if (!File.Exists(s_FfmpegPath))
                return "ffmpeg not found at " + s_FfmpegPath;

            s_Director = UnityEngine.Object.FindAnyObjectByType<DemoDirector>();
            if (s_Director == null)
                return "no DemoDirector in the scene";

            Directory.CreateDirectory(Path.GetDirectoryName(s_OutputPath));
            s_SizeNote = TrySetGameViewSize(1920, 1080);
            s_Recorder = new GameObject("Game View Recorder").AddComponent<GameViewRecorder>();
            s_WarmupFrames = 20;
            EditorApplication.update += Tick;
            WriteStatus("starting; game view: " + s_SizeNote);
            return "recording to " + s_OutputPath + "; game view: " + s_SizeNote;
        }

        public static string Abort()
        {
            if (s_Recorder == null)
                return "not recording";
            Finish("aborted");
            return "aborted";
        }

        static void Tick()
        {
            if (!Application.isPlaying || s_Recorder == null)
            {
                Finish("play mode ended");
                return;
            }

            // Give the resized Game view a few frames, and the simulator time to bring up the hands.
            if (s_WarmupFrames > 0)
            {
                s_WarmupFrames--;
                return;
            }

            if (!s_Recorder.IsRecording && s_Recorder.FrameCount == 0 && s_Recorder.Error == null)
            {
                s_Director.StartDemo(true);
                if (!s_Director.IsRunning)
                {
                    s_WarmupFrames = 30;
                    return;
                }

                s_Recorder.Begin(s_FfmpegPath, s_OutputPath, k_Fps);
                return;
            }

            if (s_Recorder.Error != null)
            {
                Finish("ERROR " + s_Recorder.Error);
                return;
            }

            if (s_Director.SequenceDone)
                Finish("done");
            else if (s_Recorder.FrameCount % 150 == 0)
                WriteStatus($"recording frame {s_Recorder.FrameCount} ({s_Recorder.FrameCount / (float)k_Fps:0}s) at {s_Recorder.Resolution}");
        }

        static void Finish(string outcome)
        {
            EditorApplication.update -= Tick;
            if (s_Recorder == null && s_Director == null)
                return;

            var frames = 0;
            var resolution = "";
            var timeline = "";
            if (s_Recorder != null)
            {
                s_Recorder.End();
                frames = s_Recorder.FrameCount;
                resolution = s_Recorder.Resolution;
                if (s_Recorder.Error != null)
                    outcome += " / " + s_Recorder.Error;
                UnityEngine.Object.Destroy(s_Recorder.gameObject);
            }

            if (s_Director != null)
            {
                timeline = string.Join("\n", s_Director.Timeline);
                s_Director.StopDemo();
            }

            s_Recorder = null;
            s_Director = null;
            RestoreGameViewSize();
            WriteStatus($"FINISHED {outcome}: {frames} frames, {frames / (float)k_Fps:0.0}s, captured at {resolution}\n{timeline}");
        }

        static void WriteStatus(string text)
        {
            try
            {
                File.WriteAllText(s_OutputPath + ".status.txt", text);
            }
            catch (IOException)
            {
            }
        }

        // The Game view has no public API for its resolution; this uses the editor's internals
        // and falls back to whatever size the view already has.
        static string TrySetGameViewSize(int width, int height)
        {
            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var gameViewType = assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                var group = CurrentSizeGroup(assembly);
                var groupType = group.GetType();

                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
                var constructor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
                var size = constructor.Invoke(new object[] { Enum.Parse(sizeKind, "FixedResolution"), width, height, "Recording" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });

                var index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                var selected = gameViewType.GetProperty("selectedSizeIndex", flags);
                s_PreviousSizeIndex = (int)selected.GetValue(gameView);
                selected.SetValue(gameView, index);
                gameView.Repaint();
                return $"{width}x{height}";
            }
            catch (Exception e)
            {
                s_PreviousSizeIndex = -1;
                return "unchanged (" + e.GetType().Name + ")";
            }
        }

        static void RestoreGameViewSize()
        {
            if (s_PreviousSizeIndex < 0)
                return;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var gameViewType = assembly.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                gameViewType.GetProperty("selectedSizeIndex", flags).SetValue(gameView, s_PreviousSizeIndex);

                var group = CurrentSizeGroup(assembly);
                var groupType = group.GetType();
                var index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                groupType.GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
                gameView.Repaint();
            }
            catch (Exception)
            {
                // Leaving an extra entry in the Game view's size list is harmless.
            }

            s_PreviousSizeIndex = -1;
        }

        static object CurrentSizeGroup(Assembly assembly)
        {
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            return sizesType.GetProperty("currentGroup").GetValue(instance);
        }
    }
}
