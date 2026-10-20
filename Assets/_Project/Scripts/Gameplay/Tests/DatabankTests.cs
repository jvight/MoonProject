using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's databank (docs/design/databank.md §3): every unlock rule (Seeded, StoryScan, FragmentOf with its set
    /// progress, SetComplete, Milestone, Wake), the unread dots, and a save that round-trips and survives a catalogue
    /// that gained or lost entries.
    /// </summary>
    public sealed class DatabankTests
    {
        private EventBus _events;
        private FakeSources _sources;
        private DatabankCatalogue _catalogue;
        private Databank _databank;
        private List<DatabankEntryAdded> _added;
        private List<DatabankEntryProgressed> _progressed;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _added = new List<DatabankEntryAdded>();
            _progressed = new List<DatabankEntryProgressed>();
            _events.Subscribe<DatabankEntryAdded>(_added.Add);
            _events.Subscribe<DatabankEntryProgressed>(_progressed.Add);
            _sources = new FakeSources();
            _sources.Sizes["Array"] = 3;
            _sources.Sizes["Cargo"] = 2;
            _sources.Sizes["Tower"] = 2;
            _catalogue = Catalogue(
                Entry("db.me", DatabankUnlock.Seeded),
                Entry("db.panel", DatabankUnlock.StoryScan("airlock")),
                Entry("db.array", DatabankUnlock.FragmentOf("Array")),
                Entry("db.lumen", DatabankUnlock.FragmentOf("Array,Tower")),
                Entry("db.cargo", DatabankUnlock.SetComplete("Cargo")),
                Entry("db.signal", DatabankUnlock.OnMilestone(OpeningMilestone.SignalWest)));
            _databank = new Databank(_catalogue, _events, _sources);
        }

        [TearDown]
        public void TearDown()
        {
            _databank.Dispose();
            Object.DestroyImmediate(_catalogue);
        }

        [Test]
        public void SeededEntries_ExistFromTheStart_WithoutAToast()
        {
            CollectionAssert.AreEqual(new[] { "db.me" }, _databank.UnlockedIds);
            Assert.IsEmpty(_added);
            Assert.IsTrue(_databank.Progress("db.me").IsComplete);
            Assert.IsTrue(_databank.IsUnread("db.me"));
        }

        [Test]
        public void AStoryScan_UnlocksItsEntryWhole_Once()
        {
            _events.Publish(new StoryScanned("story.other", "k", Vector3.zero));
            Assert.IsEmpty(_added);

            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));
            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));

            Assert.AreEqual(1, _added.Count);
            Assert.AreEqual("db.panel", _added[0].EntryId);
            Assert.AreEqual(DatabankCategory.Machines, _added[0].Category);
            Assert.AreEqual(1, _databank.Progress("db.panel").Found);
            Assert.AreEqual(1, _databank.Progress("db.panel").Count);
            Assert.IsEmpty(_progressed);
        }

        [Test]
        public void ThePiecesOfAScanSet_UnlockItsEntryOnTheFirst_AndCountUpToFull()
        {
            Fragment("ruin.Array.0", 0, 3);
            Assert.AreEqual(1, CountAdded("db.array"));
            Assert.AreEqual(1, CountAdded("db.lumen"), "The place entry names this set too.");
            AssertProgress("db.array", 1, 3);
            Assert.IsFalse(_databank.Progress("db.array").IsComplete);

            Fragment("ruin.Array.2", 1, 3);
            AssertProgress("db.array", 2, 3);
            Fragment("ruin.Array.1", 2, 3);
            AssertProgress("db.array", 3, 3);

            Assert.IsTrue(_databank.Progress("db.array").IsComplete);
            Assert.AreEqual(1, CountAdded("db.array"), "The entry is added once, on the first piece.");
            Assert.AreEqual(3, _progressed.Count);
            Assert.AreEqual(1, _progressed[0].Found);
            Assert.AreEqual(3, _progressed[2].Found);
            Assert.AreEqual(3, _progressed[2].Count);
        }

        [Test]
        public void ARepeatedPiece_DoesNotCountTwice()
        {
            Fragment("ruin.Array.0", 0, 3);
            Fragment("ruin.Array.0", 0, 3);

            AssertProgress("db.array", 1, 3);
            Assert.AreEqual(1, _progressed.Count);
        }

        [Test]
        public void AnEntryOfSeveralSets_UnlocksWholeOnAnyPiece()
        {
            Fragment("ruin.Tower.0", 0, 2);

            Assert.AreEqual("db.lumen", _added[0].EntryId);
            Assert.IsTrue(_databank.Progress("db.lumen").IsComplete);
            Fragment("ruin.Array.0", 0, 3);
            Assert.AreEqual(1, CountAdded("db.lumen"));
        }

        [Test]
        public void ASetCompleteEntry_WaitsForTheLastPiece()
        {
            Fragment("ruin.Cargo.0", 0, 2);
            Assert.AreEqual(0, CountAdded("db.cargo"));
            Assert.AreEqual(0, _databank.Progress("db.cargo").Count, "Not unlocked: 0 of 0.");

            Fragment("ruin.Cargo.1", 1, 2);
            Assert.AreEqual(1, CountAdded("db.cargo"));
            Assert.IsTrue(_databank.Progress("db.cargo").IsComplete);
        }

        [Test]
        public void AMilestone_UnlocksItsEntry_ButAReplayedOneIsSilent()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, false));
            Assert.AreEqual(0, CountAdded("db.signal"));

            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, true));
            CollectionAssert.Contains(new List<string>(_databank.UnlockedIds), "db.signal");
            Assert.AreEqual(0, CountAdded("db.signal"), "A replay on a load never toasts.");
        }

        [Test]
        public void ALiveMilestone_ToastsOnce()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, true));

            Assert.AreEqual(1, CountAdded("db.signal"));
        }

        [Test]
        public void AnEntryIsUnreadUntilTheyReadIt_AndANewPieceMakesItUnreadAgain()
        {
            Fragment("ruin.Array.0", 0, 3);
            Assert.IsTrue(_databank.IsUnread("db.array"));

            _databank.MarkRead("db.array");
            Assert.IsFalse(_databank.IsUnread("db.array"));

            Fragment("ruin.Array.1", 1, 3);
            Assert.IsTrue(_databank.IsUnread("db.array"), "A new piece is news.");
            Assert.IsFalse(_databank.IsUnread("db.panel"), "Not unlocked, so not unread.");
        }

        [Test]
        public void TheUnlockedIds_AreInTheOrderTheyUnlocked()
        {
            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));
            Fragment("ruin.Array.0", 0, 3);

            CollectionAssert.AreEqual(new[] { "db.me", "db.panel", "db.array", "db.lumen" }, _databank.UnlockedIds);
        }

        [Test]
        public void TheEntriesInfo_IsTheCatalogues()
        {
            Assert.IsTrue(_databank.TryGetEntry("db.array", out DatabankEntryInfo info));
            Assert.AreEqual("db.array", info.Id);
            Assert.AreEqual("db.array.name", info.NameKey);
            Assert.IsFalse(_databank.TryGetEntry("db.nope", out _));
        }

        [Test]
        public void ASave_RoundTrips_WithTheOrderTheCountsAndTheReadMarks()
        {
            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));
            Fragment("ruin.Array.0", 0, 3);
            Fragment("ruin.Array.1", 1, 3);
            _databank.MarkRead("db.panel");
            DatabankSaveData saved = _databank.Capture();
            string json = JsonUtility.ToJson(saved);

            var events = new EventBus();
            var loaded = new Databank(_catalogue, events, _sources);
            loaded.Restore(JsonUtility.FromJson<DatabankSaveData>(json));

            CollectionAssert.AreEqual(_databank.UnlockedIds, loaded.UnlockedIds);
            Assert.AreEqual(2, loaded.Progress("db.array").Found);
            Assert.AreEqual(3, loaded.Progress("db.array").Count);
            Assert.IsFalse(loaded.IsUnread("db.panel"));
            Assert.IsTrue(loaded.IsUnread("db.array"));
            loaded.Dispose();
        }

        [Test]
        public void ALoad_IsSilent()
        {
            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));
            DatabankSaveData saved = _databank.Capture();
            _added.Clear();
            _progressed.Clear();

            _databank.Restore(saved);
            _ = _databank.UnlockedIds;

            Assert.IsEmpty(_added);
            Assert.IsEmpty(_progressed);
        }

        [Test]
        public void ASaveWithIdsTheCatalogueDoesNotKnow_DropsThem_AndNewEntriesStayLocked()
        {
            var data = new DatabankSaveData
            {
                unlocked = new[] { "db.gone", "db.panel" },
                progressIds = new[] { "db.gone" },
                progressFound = new[] { 4 },
                read = new[] { "db.gone" },
            };

            _databank.Restore(data);

            CollectionAssert.AreEqual(new[] { "db.panel", "db.me" }, _databank.UnlockedIds);
            Assert.IsFalse(_databank.TryGetEntry("db.gone", out _));
            Assert.AreEqual(0, _databank.Progress("db.array").Count, "An entry the save never had is locked.");
        }

        [Test]
        public void AnOlderSave_UnlocksWhatItsScansAndMilestonesAlreadySay_Silently()
        {
            _sources.Stories.Add("airlock");
            _sources.Scanned["Array"] = 2;
            _sources.Scanned["Cargo"] = 2;
            _sources.Milestones.Add(OpeningMilestone.SignalWest);

            _databank.Restore(new DatabankSaveData());

            var unlocked = new List<string>(_databank.UnlockedIds);
            CollectionAssert.Contains(unlocked, "db.panel");
            CollectionAssert.Contains(unlocked, "db.array");
            CollectionAssert.Contains(unlocked, "db.lumen");
            CollectionAssert.Contains(unlocked, "db.cargo");
            CollectionAssert.Contains(unlocked, "db.signal");
            AssertProgress("db.array", 2, 3);
            Assert.IsEmpty(_added);
        }

        [Test]
        public void WhatItReadsAsItWakes_JoinsAtTheEndOfTheSelfTest_Silently_AndALoadedGameHasWoken()
        {
            DatabankCatalogue catalogue = Catalogue(Entry("db.post", DatabankUnlock.OnWake));
            var databank = new Databank(catalogue, _events, _sources);
            try
            {
                Assert.IsEmpty(databank.UnlockedIds);

                _events.Publish(new BootCheckFinished());
                CollectionAssert.AreEqual(new[] { "db.post" }, databank.UnlockedIds);
                Assert.IsTrue(databank.Progress("db.post").IsComplete);
                Assert.IsTrue(databank.IsUnread("db.post"), "Found on the page, not announced.");
                Assert.IsEmpty(_added);

                databank.Restore(new DatabankSaveData());
                CollectionAssert.AreEqual(new[] { "db.post" }, databank.UnlockedIds, "An older save woke too.");
                Assert.IsEmpty(_added);
            }
            finally
            {
                databank.Dispose();
                Object.DestroyImmediate(catalogue);
            }
        }

        [Test]
        public void ADisposedDatabank_HearsNothing()
        {
            _databank.Dispose();
            _events.Publish(new StoryScanned("story.airlock", "k", Vector3.zero));

            Assert.IsEmpty(_added);
        }

        [Test]
        public void TheCatalogue_RejectsADuplicateId_AndAModelEntryWithoutAModel()
        {
            DatabankCatalogue twice = Catalogue(Entry("db.a", DatabankUnlock.Seeded), Entry("db.a", DatabankUnlock.Seeded));
            Assert.That(twice.Validate(), Does.Contain("twice"));
            Object.DestroyImmediate(twice);

            var model = new DatabankEntry("db.b", DatabankCategory.Machines, "k", DatabankMaker.None, "1967",
                DatabankEra.Probes, "r", "h", DatabankViewKind.Model, null, null, 0f, null, DatabankUnlock.Seeded);
            DatabankCatalogue bare = Catalogue(model);
            Assert.That(bare.Validate(), Does.Contain("no model"));
            Object.DestroyImmediate(bare);
        }

        private void Fragment(string fragmentId, int before, int count)
        {
            _events.Publish(new FragmentScanned(fragmentId, "structure", before, count, false, 0));
        }

        private void AssertProgress(string id, int found, int count)
        {
            Assert.AreEqual(found, _databank.Progress(id).Found, id + " found");
            Assert.AreEqual(count, _databank.Progress(id).Count, id + " count");
        }

        private int CountAdded(string id)
        {
            int count = 0;
            foreach (DatabankEntryAdded added in _added)
            {
                count += added.EntryId == id ? 1 : 0;
            }

            return count;
        }

        private static DatabankEntry Entry(string id, DatabankUnlock unlock)
        {
            return new DatabankEntry(id, id == "db.panel" ? DatabankCategory.Machines : DatabankCategory.Structures,
                id + ".name", DatabankMaker.None, "1984", DatabankEra.Lumen, id + ".readout", id + ".history",
                DatabankViewKind.Print, null, null, 0f, null, unlock);
        }

        private static DatabankCatalogue Catalogue(params DatabankEntry[] entries)
        {
            var catalogue = ScriptableObject.CreateInstance<DatabankCatalogue>();
            catalogue.Populate(entries);
            return catalogue;
        }

        private sealed class FakeSources : IDatabankSources
        {
            public readonly HashSet<string> Stories = new HashSet<string>();
            public readonly Dictionary<string, int> Scanned = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Sizes = new Dictionary<string, int>();
            public readonly HashSet<OpeningMilestone> Milestones = new HashSet<OpeningMilestone>();

            public bool IsStoryScanned(string storyId)
            {
                return Stories.Contains(storyId);
            }

            public int ScannedInSet(string setId)
            {
                return Scanned.TryGetValue(setId, out int scanned) ? scanned : 0;
            }

            public int SetSize(string setId)
            {
                return Sizes.TryGetValue(setId, out int size) ? size : 0;
            }

            public bool HasReached(OpeningMilestone milestone)
            {
                return Milestones.Contains(milestone);
            }
        }
    }
}
