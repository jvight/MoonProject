using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Core.Save.Tests
{
    /// <summary>
    /// Guard for the test-run save isolation (MoonProject.Editor.Automation.TestRunSaveIsolation): during a test run no
    /// save the tests make may land in the player's real folder. A run started some other way, with no hook, fails here.
    /// </summary>
    public sealed class TestRunSaveFolderTests
    {
        [Test]
        public void DefaultDirectory_InATestRun_IsAScratchFolderOutsideThePersistentDataPath()
        {
            string real = Path.GetFullPath(Application.persistentDataPath);
            string scratch = Path.GetFullPath(SaveService.DefaultDirectory);
            Assert.AreNotEqual(Path.GetFullPath(SaveService.RealDirectory), scratch, "tests save to a scratch folder");
            Assert.IsFalse(scratch.StartsWith(real, System.StringComparison.OrdinalIgnoreCase),
                $"{scratch} is inside the player's data folder {real}");
            Assert.IsTrue(Directory.Exists(scratch), "the run's hook created it");
        }

        [Test]
        public void ASaveWrittenByATest_LandsInTheScratchFolderOnly()
        {
            string slot = "isolation-" + System.Guid.NewGuid().ToString("N");
            var save = new SaveService(SaveService.DefaultDirectory, slot);
            try
            {
                save.Load();
                Assert.IsTrue(save.SaveNow(), "the save is written");
                Assert.IsTrue(File.Exists(save.FilePath), "the save exists in the scratch folder");
                Assert.IsFalse(File.Exists(Path.Combine(SaveService.RealDirectory, slot + SaveService.Extension)),
                    "and not in the real folder");
            }
            finally
            {
                File.Delete(save.FilePath);
            }
        }
    }
}
