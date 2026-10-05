using System.IO;
using NUnit.Framework;
using MoonProject.Editor.Automation;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Editor.Tests
{
    public sealed class EditorCommandsTests
    {
        private string _reportPath;

        [SetUp]
        public void SetUp()
        {
            _reportPath = Path.Combine(Path.GetTempPath(), $"moon-editor-commands-{System.Guid.NewGuid():N}.txt");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_reportPath))
            {
                File.Delete(_reportPath);
            }
        }

        [Test]
        public void ListBuilders_ReportsTheSceneBuilderAndPasses()
        {
            string report = EditorCommands.ListBuilders(_reportPath);

            StringAssert.Contains(MainSceneBuilder.BuilderPath, report);
            StringAssert.Contains("[replaces open scene]", report);
            StringAssert.EndsWith("RESULT: PASS", report);
            Assert.AreEqual(report, File.ReadAllText(_reportPath), "the report is also written for timed-out callers");
        }

        [Test]
        public void Build_WithUnknownFilter_FailsWithoutRunningAnything()
        {
            string report = EditorCommands.Build("^No Such Builder$", _reportPath);

            StringAssert.Contains("No builder path matches", report);
            StringAssert.EndsWith("RESULT: FAIL", report);
        }
    }
}
