using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PhysicsSandbox.Editor
{
    /// <summary>
    /// Runs the test suites from the AgentBridge and writes a plain-text summary to
    /// Library/AgentBridge/tests.txt, since the Test Runner window is not readable from outside.
    /// </summary>
    [InitializeOnLoad]
    public static class SandboxTestRunner
    {
        static readonly string s_ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/AgentBridge/tests.txt"));
        static readonly TestRunnerApi s_Api;

        static SandboxTestRunner()
        {
            s_Api = ScriptableObject.CreateInstance<TestRunnerApi>();
            s_Api.RegisterCallbacks(new Callbacks());
        }

        public static string RunEditMode() => Run(TestMode.EditMode);

        public static string RunPlayMode() => Run(TestMode.PlayMode);

        static string Run(TestMode mode)
        {
            File.Delete(s_ResultPath);
            s_Api.Execute(new ExecutionSettings(new Filter { testMode = mode }));
            return $"started {mode} tests";
        }

        class Callbacks : ICallbacks
        {
            readonly StringBuilder m_Failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) => m_Failures.Clear();

            public void RunFinished(ITestResultAdaptor result)
            {
                File.WriteAllText(s_ResultPath,
                    $"{result.TestStatus}: passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} " +
                    $"duration={result.Duration:F2}s\n{m_Failures}");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                    m_Failures.AppendLine($"FAILED {result.FullName}: {result.Message}");
            }
        }
    }
}
