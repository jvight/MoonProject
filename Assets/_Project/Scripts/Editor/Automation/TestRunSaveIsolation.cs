using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using MoonProject.Core.Save;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Keeps editor test runs out of the player's real save folder. Fixtures boot the game on throwaway slots
    /// ("test-&lt;guid&gt;", "playthrough-&lt;pid&gt;") and a forgotten cleanup left hundreds of files in
    /// persistentDataPath. While a run is on (batch -runTests, the Test Runner window or MCP),
    /// <see cref="SaveService.DefaultDirectory"/> points at a scratch folder this hook creates when the run starts and
    /// deletes when it ends, whatever the tests did. The real folder is listed at both ends; a file that appeared
    /// there during the run is logged as an error naming it, so a code path that bypasses the seam fails loudly.
    /// </summary>
    [InitializeOnLoad]
    public sealed class TestRunSaveIsolation : ICallbacks
    {
        private const string ScratchFolder = "MoonProject-test-saves";
        private const string BeforeKey = "MoonProject.TestRunRealSavesBefore";
        private const char Separator = '|';

        static TestRunSaveIsolation()
        {
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new TestRunSaveIsolation());
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            string scratch = Path.Combine(Path.GetTempPath(), ScratchFolder, Guid.NewGuid().ToString("N"),
                SaveService.FolderName);
            Directory.CreateDirectory(scratch);
            SessionState.SetString(SaveService.TestRunDirectoryKey, scratch);
            SessionState.SetString(BeforeKey, string.Join(Separator, RealFiles()));
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string scratch = SessionState.GetString(SaveService.TestRunDirectoryKey, string.Empty);
            string[] before = SessionState.GetString(BeforeKey, string.Empty).Split(Separator);
            string[] leaked = RealFiles().Except(before).ToArray();
            SessionState.EraseString(SaveService.TestRunDirectoryKey);
            SessionState.EraseString(BeforeKey);
            if (scratch.Length > 0 && Directory.Exists(scratch))
            {
                Directory.Delete(Path.GetDirectoryName(scratch), true);
            }

            if (leaked.Length > 0)
            {
                Debug.LogError($"{nameof(TestRunSaveIsolation)}: {leaked.Length} file(s) appeared in the real save " +
                               $"folder {SaveService.RealDirectory} during the test run: {string.Join(", ", leaked)}. " +
                               "A path reached it without SaveService.DefaultDirectory.");
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        private static string[] RealFiles()
        {
            return Directory.Exists(SaveService.RealDirectory)
                ? Directory.GetFiles(SaveService.RealDirectory).Select(Path.GetFileName).ToArray()
                : Array.Empty<string>();
        }
    }
}
