using System.IO;
using NUnit.Framework;

namespace MoonProject.Core.Save.Tests
{
    public sealed class LegacySaveMigrationTests
    {
        private string _root;
        private string _legacy;
        private string _current;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "moon-migration-" + Path.GetRandomFileName());
            _legacy = Path.Combine(_root, LegacySaveMigration.LegacyProductFolder);
            _current = Path.Combine(_root, "Lunarion");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private static string Saves(string root) => Path.Combine(root, SaveService.FolderName);

        private static void Write(string root, string name, string text)
        {
            Directory.CreateDirectory(Saves(root));
            File.WriteAllText(Path.Combine(Saves(root), name), text);
        }

        [Test]
        public void CopiesSavesAndSettingsWhenTheNewFolderIsEmpty_AndKeepsTheOldOnes()
        {
            Write(_legacy, "default.json", "save");
            Write(_legacy, "default.json.bak", "backup");
            Write(_legacy, "default.settings.json", "settings");

            Assert.AreEqual(3, LegacySaveMigration.Run(_legacy, _current));

            Assert.AreEqual("save", File.ReadAllText(Path.Combine(Saves(_current), "default.json")));
            Assert.AreEqual("settings", File.ReadAllText(Path.Combine(Saves(_current), "default.settings.json")));
            Assert.AreEqual(3, Directory.GetFiles(Saves(_legacy)).Length, "the old folder is never touched");
        }

        [Test]
        public void LeavesANewFolderThatAlreadyHasFilesAlone()
        {
            Write(_legacy, "default.json", "old");
            Write(_current, "fresh.json", "new");

            Assert.AreEqual(0, LegacySaveMigration.Run(_legacy, _current));

            Assert.IsFalse(File.Exists(Path.Combine(Saves(_current), "default.json")));
            Assert.AreEqual("new", File.ReadAllText(Path.Combine(Saves(_current), "fresh.json")));
        }

        [Test]
        public void DoesNothingWithoutAnOldFolder_AndRunsOnlyOnce()
        {
            Assert.AreEqual(0, LegacySaveMigration.Run(_legacy, _current));
            Assert.IsFalse(Directory.Exists(Saves(_current)));

            Write(_legacy, "default.json", "old");
            Assert.AreEqual(1, LegacySaveMigration.Run(_legacy, _current));
            File.Delete(Path.Combine(Saves(_current), "default.json"));
            Write(_current, "later.json", "later");
            Assert.AreEqual(0, LegacySaveMigration.Run(_legacy, _current), "a populated folder is not migrated again");
        }
    }
}
