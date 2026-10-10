using System.IO;
using NUnit.Framework;
using MoonProject.Core.Save;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode
{
    public sealed class BootstrapHarnessTests
    {
        [Test]
        public void DeleteSaveFiles_RemovesTheSlotsSaveAndSettingsFiles_AndNothingElse()
        {
            string slot = BootstrapHarness.NewTestSlot();
            string other = BootstrapHarness.NewTestSlot();
            string directory = SaveService.DefaultDirectory;
            Directory.CreateDirectory(directory);
            string[] owned =
            {
                slot + SaveService.Extension,
                slot + SaveService.Extension + SaveService.BackupSuffix,
                slot + SettingsFile.Suffix + SaveService.Extension,
                slot + SettingsFile.Suffix + SaveService.Extension + SaveService.BackupSuffix,
            };
            string kept = other + SettingsFile.Suffix + SaveService.Extension;
            try
            {
                foreach (string name in owned)
                {
                    File.WriteAllText(Path.Combine(directory, name), "{}");
                }

                File.WriteAllText(Path.Combine(directory, kept), "{}");
                BootstrapHarness.DeleteSaveFiles(slot);
                foreach (string name in owned)
                {
                    Assert.IsFalse(File.Exists(Path.Combine(directory, name)), $"{name} is deleted");
                }

                Assert.IsTrue(File.Exists(Path.Combine(directory, kept)), "another slot's settings stay");
            }
            finally
            {
                BootstrapHarness.DeleteSaveFiles(slot);
                BootstrapHarness.DeleteSaveFiles(other);
            }
        }
    }
}
