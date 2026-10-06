using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MoonProject.Core.Save.Tests
{
    public sealed class SaveServiceTests
    {
        private const string Slot = "unit";

        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "moon-save-tests", Guid.NewGuid().ToString("N"));
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
        public void SaveThenLoad_RoundTripsEverySection()
        {
            var writer = new ProgressHolder { Data = { scrap = 42, name = "07", relics = 2 } };
            SaveService first = NewService();
            first.Register(writer.Section(3));
            Assert.AreEqual(SaveLoadResult.NoSave, first.Load());
            Assert.IsTrue(first.SaveNow());

            var reader = new ProgressHolder();
            SaveService second = NewService();
            second.Register(reader.Section(3));

            Assert.AreEqual(SaveLoadResult.Loaded, second.Load());
            Assert.AreEqual(42, reader.Data.scrap);
            Assert.AreEqual("07", reader.Data.name);
            Assert.AreEqual(2, reader.Data.relics);
            Assert.AreEqual(1, reader.RestoreCount);
        }

        [Test]
        public void SectionRegisteredAfterLoad_IsRestoredImmediately()
        {
            SaveWith(9, 3);
            SaveService service = NewService();
            service.Load();

            var late = new ProgressHolder();
            service.Register(late.Section(3));

            Assert.AreEqual(9, late.Data.scrap);
        }

        [Test]
        public void NoSavedData_LeavesTheSectionAlone()
        {
            var holder = new ProgressHolder { Data = { scrap = 3 } };
            SaveService service = NewService();
            service.Register(holder.Section(1));

            Assert.AreEqual(SaveLoadResult.NoSave, service.Load());
            Assert.AreEqual(3, holder.Data.scrap);
            Assert.AreEqual(0, holder.RestoreCount);
        }

        [Test]
        public void OlderVersion_IsMigratedStepByStepBeforeRestore()
        {
            WriteMain(Envelope(Entry(ProgressHolder.Key, 1, "{\"oldScrap\":5}")));
            var holder = new ProgressHolder();
            SaveService service = NewService();
            service.Register(holder.Section(3, (json, from) => from == 1
                ? json.Replace("oldScrap", "scrap")
                : json.Replace("}", ",\"relics\":1}")));

            service.Load();

            CollectionAssert.AreEqual(new[] { 1, 2 }, holder.MigratedFrom);
            Assert.AreEqual(5, holder.Data.scrap);
            Assert.AreEqual(1, holder.Data.relics);
        }

        [Test]
        public void MissingMigration_LogsAndKeepsTheSavedDataVerbatim()
        {
            string oldEntry = Entry(ProgressHolder.Key, 1, "{\"oldScrap\":5}");
            WriteMain(Envelope(oldEntry));
            var holder = new ProgressHolder { Data = { scrap = 1 } };
            SaveService service = NewService();
            service.Register(holder.Section(2));
            LogAssert.Expect(LogType.Exception, new Regex("no migration from version 1 to 2"));
            LogAssert.Expect(LogType.Error, new Regex("'test.progress' was not restored"));

            service.Load();
            Assert.IsTrue(service.SaveNow());

            Assert.AreEqual(1, holder.Data.scrap, "the section keeps its initial state");
            StringAssert.Contains("\"version\": 1", ReadMain());
            StringAssert.Contains("oldScrap", ReadMain(), "the unmigratable data is written back unchanged");
        }

        [Test]
        public void NewerSectionVersion_IsNotRestoredAndIsWrittenBackUnchanged()
        {
            WriteMain(Envelope(Entry(ProgressHolder.Key, 5, "{\"future\":true}")));
            var holder = new ProgressHolder { Data = { scrap = 1 } };
            SaveService service = NewService();
            service.Register(holder.Section(2));
            LogAssert.Expect(LogType.Error, new Regex("saved by a newer build \\(version 5 > 2\\)"));

            service.Load();
            Assert.IsTrue(service.SaveNow());

            Assert.AreEqual(0, holder.RestoreCount);
            StringAssert.Contains("\"version\": 5", ReadMain());
            StringAssert.Contains("future", ReadMain());
        }

        [Test]
        public void RestoreThatThrows_LogsAndKeepsTheSavedData()
        {
            WriteMain(Envelope(Entry(ProgressHolder.Key, 1, "{\"scrap\":4}")));
            SaveService service = NewService();
            service.Register(new SaveSection<ProgressData>(ProgressHolder.Key, 1, () => new ProgressData(),
                data => throw new InvalidOperationException("restore failed on purpose")));
            LogAssert.Expect(LogType.Exception, new Regex("restore failed on purpose"));
            LogAssert.Expect(LogType.Error, new Regex("was not restored"));

            service.Load();
            Assert.IsTrue(service.SaveNow());

            StringAssert.Contains("\\\"scrap\\\":4", ReadMain());
        }

        [Test]
        public void SectionsNotRegisteredThisSession_SurviveSaving()
        {
            WriteMain(Envelope(Entry("other.domain", 2, "{\"x\":7}"), Entry(ProgressHolder.Key, 1, "{\"scrap\":1}")));
            var holder = new ProgressHolder();
            SaveService service = NewService();
            service.Register(holder.Section(1));
            service.Load();
            holder.Data.scrap = 99;

            Assert.IsTrue(service.SaveNow());

            var reader = new ProgressHolder();
            SaveService again = NewService();
            again.Register(reader.Section(1));
            again.Load();
            Assert.AreEqual(99, reader.Data.scrap);
            StringAssert.Contains("other.domain", ReadMain());
            StringAssert.Contains("\\\"x\\\":7", ReadMain());
        }

        [Test]
        public void DisposingARegistration_KeepsTheSectionsLastState()
        {
            var holder = new ProgressHolder();
            SaveService service = NewService();
            IDisposable registration = service.Register(holder.Section(1));
            service.Load();
            holder.Data.scrap = 12;

            registration.Dispose();
            holder.Data.scrap = 13;
            service.SaveNow();

            var reader = new ProgressHolder();
            SaveService again = NewService();
            again.Register(reader.Section(1));
            again.Load();
            Assert.AreEqual(12, reader.Data.scrap);
        }

        [Test]
        public void CorruptSave_IsMovedAsideAndTheBackupIsLoaded()
        {
            SaveWith(1);
            SaveWith(2);
            WriteMain("{ this is not json");
            var holder = new ProgressHolder();
            SaveService service = NewService();
            service.Register(holder.Section(1));
            LogAssert.Expect(LogType.Error, new Regex("is unreadable .*moved to .*loading the backup"));
            LogAssert.Expect(LogType.Warning, new Regex("Recovered progress from the backup"));

            Assert.AreEqual(SaveLoadResult.RecoveredFromBackup, service.Load());

            Assert.AreEqual(1, holder.Data.scrap, "the backup holds the save before the last one");
            Assert.IsFalse(File.Exists(service.FilePath));
            string[] aside = Directory.GetFiles(_directory, Slot + ".json" + SaveService.CorruptMarker + "*");
            Assert.AreEqual(1, aside.Length, "the unreadable file is kept for inspection");
            Assert.AreEqual("{ this is not json", File.ReadAllText(aside[0]));
            Assert.IsTrue(service.SaveNow());
            Assert.IsTrue(File.Exists(service.FilePath));
        }

        [Test]
        public void CorruptSaveAndBackup_StartFreshAndKeepBothFiles()
        {
            SaveWith(1);
            SaveWith(2);
            WriteMain(string.Empty);
            File.WriteAllText(Path.Combine(_directory, Slot + ".json" + SaveService.BackupSuffix), "{\"nope\":1}");
            var holder = new ProgressHolder { Data = { scrap = 7 } };
            SaveService service = NewService();
            service.Register(holder.Section(1));
            LogAssert.Expect(LogType.Error, new Regex("is unreadable"));
            LogAssert.Expect(LogType.Error, new Regex("backup .* is unreadable too .*starting with fresh progress"));

            Assert.AreEqual(SaveLoadResult.Unreadable, service.Load());

            Assert.AreEqual(7, holder.Data.scrap);
            Assert.AreEqual(2, Directory.GetFiles(_directory, "*" + SaveService.CorruptMarker + "*").Length);
        }

        [Test]
        public void NewerFileFormat_IsLeftUntouchedAndSavingIsDisabled()
        {
            string future = "{\"formatVersion\":99,\"sections\":[]}";
            WriteMain(future);
            SaveService service = NewService();
            LogAssert.Expect(LogType.Error, new Regex("format version 99 is newer"));
            LogAssert.Expect(LogType.Error, new Regex("saving is disabled"));

            Assert.AreEqual(SaveLoadResult.NewerFormat, service.Load());
            Assert.IsFalse(service.SaveNow());

            Assert.AreEqual(future, ReadMain());
        }

        [Test]
        public void FailedCapture_WritesNothing()
        {
            SaveWith(1);
            string before = ReadMain();
            var holder = new ProgressHolder { ThrowOnCapture = true };
            SaveService service = NewService();
            service.Register(holder.Section(1));
            service.Load();
            LogAssert.Expect(LogType.Exception, new Regex("capture failed on purpose"));
            LogAssert.Expect(LogType.Error, new Regex("failed to capture; nothing was saved"));

            Assert.IsFalse(service.SaveNow());

            Assert.AreEqual(before, ReadMain());
            Assert.IsFalse(File.Exists(TempPath()));
        }

        [Test]
        public void Saving_ReplacesAtomicallyKeepsOneBackupAndIgnoresAStaleTempFile()
        {
            SaveWith(1);
            string firstSave = ReadMain();
            File.WriteAllText(TempPath(), "half-written garbage from a crashed save");
            var holder = new ProgressHolder();
            SaveService service = NewService();
            service.Register(holder.Section(1));

            Assert.AreEqual(SaveLoadResult.Loaded, service.Load(), "a leftover temp file is never read");
            holder.Data.scrap = 2;
            Assert.IsTrue(service.SaveNow());

            Assert.AreEqual(firstSave, File.ReadAllText(service.BackupPath), "the previous save is the backup");
            StringAssert.Contains("\\\"scrap\\\":2", ReadMain());
            Assert.IsFalse(File.Exists(TempPath()), "the temp file was swapped in");
            Assert.AreEqual(2, Directory.GetFiles(_directory).Length, "exactly the save and one backup");
        }

        [Test]
        public void SaveBeforeLoad_IsRefused()
        {
            SaveService service = NewService();
            service.Register(new ProgressHolder().Section(1));
            LogAssert.Expect(LogType.Error, new Regex("SaveNow before Load"));

            Assert.IsFalse(service.SaveNow());
            Assert.IsFalse(File.Exists(service.FilePath));
        }

        [Test]
        public void InvalidRegistrationsAndSlots_Throw()
        {
            SaveService service = NewService();
            service.Register(new ProgressHolder().Section(1));

            Assert.Throws<InvalidOperationException>(() => service.Register(new ProgressHolder().Section(1)));
            Assert.Throws<ArgumentNullException>(() => service.Register(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressHolder().Section(0));
            Assert.Throws<ArgumentException>(() => new SaveService(_directory, "../escape"));
            Assert.Throws<ArgumentException>(() => new SaveService(_directory, string.Empty));
            service.Load();
            Assert.Throws<InvalidOperationException>(() => service.Load());
        }

        private SaveService NewService()
        {
            return new SaveService(_directory, Slot);
        }

        private void SaveWith(int scrap, int version = 1)
        {
            var holder = new ProgressHolder();
            SaveService service = NewService();
            service.Register(holder.Section(version));
            service.Load();
            holder.Data.scrap = scrap;
            Assert.IsTrue(service.SaveNow());
        }

        private void WriteMain(string text)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, Slot + SaveService.Extension), text);
        }

        private string ReadMain()
        {
            return File.ReadAllText(Path.Combine(_directory, Slot + SaveService.Extension));
        }

        private string TempPath()
        {
            return Path.Combine(_directory, Slot + SaveService.Extension + ".tmp");
        }

        private static string Envelope(params string[] entries)
        {
            return "{\"formatVersion\":1,\"savedAtUtc\":\"test\",\"sections\":[" + string.Join(",", entries) + "]}";
        }

        private static string Entry(string key, int version, string json)
        {
            return "{\"key\":\"" + key + "\",\"version\":" + version + ",\"json\":\"" +
                   json.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
        }
    }
}
