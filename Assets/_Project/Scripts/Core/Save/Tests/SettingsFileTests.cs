using System;
using System.IO;
using NUnit.Framework;

namespace MoonProject.Core.Save.Tests
{
    public sealed class SettingsFileTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "moon-settings-tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void Nothing_IsReadUntilSomethingIsWritten_ThenTheLatestComesBack()
        {
            var file = new SettingsFile(_directory, "main");
            Assert.IsFalse(file.TryRead(out _));
            Assert.IsTrue(file.Write("{\"language\":\"en\"}"));
            Assert.IsTrue(file.Write("{\"language\":\"vi\"}"));
            Assert.IsTrue(file.TryRead(out string json));
            Assert.AreEqual("{\"language\":\"vi\"}", json);
            Assert.AreEqual(Path.Combine(_directory, "main.settings.json"), file.FilePath);
        }

        [Test]
        public void ALostFile_FallsBackToItsBackup()
        {
            var file = new SettingsFile(_directory, "main");
            file.Write("{\"language\":\"en\"}");
            file.Write("{\"language\":\"vi\"}");
            File.Delete(file.FilePath);
            Assert.IsTrue(file.TryRead(out string json));
            Assert.AreEqual("{\"language\":\"en\"}", json, "the backup holds the write before");
        }

        [Test]
        public void PuttingTheSaveAway_LeavesTheSettingsFileAlone()
        {
            var save = new SaveService(_directory, "main");
            save.Load();
            Assert.IsTrue(save.SaveNow());
            var file = new SettingsFile(_directory, "main");
            file.Write("{\"language\":\"vi\"}");
            save.PutAway();
            Assert.IsFalse(File.Exists(save.FilePath), "the progress went away");
            Assert.IsTrue(file.TryRead(out string json), "the settings stayed");
            Assert.AreEqual("{\"language\":\"vi\"}", json);
        }

        [Test]
        public void AnInvalidSlot_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => new SettingsFile(_directory, "../escape"));
        }
    }
}
