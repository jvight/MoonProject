using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using MoonProject.App;
using MoonProject.Testing;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Makes every boot during an editor test run skip the title (M3-09), the way a boot straight into play does:
    /// PlayMode fixtures load Main and drive 07 from the first frame. Sets
    /// <see cref="GameBootstrap.TestRunSkipsTitleKey"/> in SessionState when a run starts (batch -runTests, the Test
    /// Runner window or MCP) and erases it when the run ends, so pressing Play outside a run still shows the title. A
    /// test of the title itself clears the key for its own boot. It marks the run for
    /// <see cref="TestRunInputFocus"/> the same way, so input reaches the game whatever the window focus.
    /// </summary>
    [InitializeOnLoad]
    public sealed class TestRunTitleSkip : ICallbacks
    {
        static TestRunTitleSkip()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new TestRunTitleSkip());
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, true);
            SessionState.SetBool(TestRunInputFocus.SessionKey, true);
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            SessionState.EraseBool(GameBootstrap.TestRunSkipsTitleKey);
            SessionState.EraseBool(TestRunInputFocus.SessionKey);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }
    }
}
