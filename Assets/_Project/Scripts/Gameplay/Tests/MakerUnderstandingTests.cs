using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Maker understanding (docs/design/maker-understanding.md §1, §2): points derived from completed, non-seeded
    /// databank entries per maker; levels at rising thresholds unlock adapted blueprints; gains held through the
    /// opening until the west signal (rule 6); a loaded game past the signal starts silent; the tracks validate.
    /// </summary>
    public sealed class MakerUnderstandingTests
    {
        private const string Lamp = MakerUnderstandingSettings.Ssr3Headlamp;
        private const string Rings = "adapted.grip_rings";
        private const string Rack = "adapted.crate_rack";

        private EventBus _events;
        private FakeSources _sources;
        private DatabankCatalogue _catalogue;
        private Databank _databank;
        private MakerUnderstanding _understanding;
        private List<UnderstandingGained> _gained;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _gained = new List<UnderstandingGained>();
            _events.Subscribe<UnderstandingGained>(_gained.Add);
            _sources = new FakeSources();
            _sources.Sizes["Array"] = 2;
            _catalogue = ScriptableObject.CreateInstance<DatabankCatalogue>();
            _catalogue.Populate(new[]
            {
                Entry("db.unit07", DatabankMaker.KesslerArai, DatabankUnlock.Seeded),
                Entry("db.six", DatabankMaker.KesslerArai, DatabankUnlock.StoryScan("six")),
                Entry("db.post", DatabankMaker.KesslerArai, DatabankUnlock.StoryScan("post")),
                Entry("db.one", DatabankMaker.KesslerArai, DatabankUnlock.StoryScan("one")),
                Entry("db.ten", DatabankMaker.KesslerArai, DatabankUnlock.StoryScan("ten")),
                Entry("db.array", DatabankMaker.VIS, DatabankUnlock.FragmentOf("Array")),
                Entry("db.flag", DatabankMaker.None, DatabankUnlock.StoryScan("flag")),
            });
            _databank = new Databank(_catalogue, _events, _sources);
            _understanding = new MakerUnderstanding(_events, _databank, _catalogue, new MakerUnderstandingSettings(
                new MakerTrack(DatabankMaker.KesslerArai, new[] { 2, 4 }, new[] { Lamp, Rings }),
                new MakerTrack(DatabankMaker.VIS, new[] { 1 }, new[] { Rack })));
            _understanding.Announce();
        }

        [TearDown]
        public void TearDown()
        {
            _understanding.Dispose();
            _databank.Dispose();
            Object.DestroyImmediate(_catalogue);
        }

        [Test]
        public void SeededAndMakerlessEntries_GiveNoPoints()
        {
            Story("flag");

            Assert.AreEqual(0, _understanding.PointsOf(DatabankMaker.KesslerArai), "07's own seeded entry is free.");
            Assert.AreEqual(0, _understanding.PointsOf(DatabankMaker.None));
        }

        [Test]
        public void ACompletedEntry_GivesItsMakerOnePoint_AndASetCountsOnlyWhenWhole()
        {
            Story("six");
            Fragment("ruin.Array.0", 0);

            Assert.AreEqual(1, _understanding.PointsOf(DatabankMaker.KesslerArai));
            Assert.AreEqual(0, _understanding.PointsOf(DatabankMaker.VIS), "Half a set teaches nothing yet.");

            Fragment("ruin.Array.1", 1);
            Story("six");

            Assert.AreEqual(1, _understanding.PointsOf(DatabankMaker.VIS));
            Assert.AreEqual(1, _understanding.PointsOf(DatabankMaker.KesslerArai), "A re-scan counts nothing.");
        }

        [Test]
        public void WhatItReadsAsItWakes_CountsLikeAScan()
        {
            DatabankCatalogue catalogue = ScriptableObject.CreateInstance<DatabankCatalogue>();
            catalogue.Populate(new[] { Entry("db.post", DatabankMaker.KesslerArai, DatabankUnlock.OnWake) });
            var databank = new Databank(catalogue, _events, _sources);
            var understanding = new MakerUnderstanding(_events, databank, catalogue, new MakerUnderstandingSettings());
            try
            {
                _events.Publish(new BootCheckFinished());
                understanding.Announce();

                Assert.AreEqual(1, understanding.PointsOf(DatabankMaker.KesslerArai), "The trickle post is on path.");
            }
            finally
            {
                understanding.Dispose();
                databank.Dispose();
                Object.DestroyImmediate(catalogue);
            }
        }

        [Test]
        public void ThroughTheOpening_GainsAreHeld_ThenTheWestSignalAnnouncesEachOnce()
        {
            Story("post");
            Story("six");

            Assert.AreEqual(1, _understanding.LevelOf(DatabankMaker.KesslerArai));
            Assert.IsFalse(_understanding.IsUnlocked(Lamp), "Rule 6: the opening holds the unlock.");
            Assert.IsEmpty(_gained);

            Signal(false);

            Assert.IsTrue(_understanding.IsUnlocked(Lamp));
            Assert.AreEqual(1, _gained.Count, "One line per maker, not one per point.");
            UnderstandingGained gain = _gained[0];
            Assert.AreEqual(DatabankMaker.KesslerArai, gain.Maker);
            Assert.AreEqual(2, gain.Points);
            Assert.AreEqual(1, gain.Level);
            Assert.AreEqual(4, gain.NextThreshold);
            Assert.AreEqual(Lamp, gain.AdaptedId);
            Assert.IsTrue(gain.First);

            Signal(false);
            Assert.AreEqual(1, _gained.Count, "The signal releases once.");
        }

        [Test]
        public void AfterTheSignal_EachGainIsAnnounced_AndALevelNamesItsPartOnce()
        {
            Signal(false);

            Story("six");
            Assert.AreEqual(1, _gained.Count);
            Assert.AreEqual(1, _gained[0].Points);
            Assert.AreEqual(0, _gained[0].Level);
            Assert.AreEqual(2, _gained[0].NextThreshold);
            Assert.AreEqual(string.Empty, _gained[0].AdaptedId);
            Assert.IsTrue(_gained[0].First);
            Assert.IsFalse(_understanding.IsUnlocked(Lamp));

            Story("post");
            Assert.AreEqual(Lamp, _gained[1].AdaptedId);
            Assert.IsFalse(_gained[1].First);

            Story("one");
            Assert.AreEqual(string.Empty, _gained[2].AdaptedId);
            Story("ten");
            Assert.AreEqual(Rings, _gained[3].AdaptedId);
            Assert.AreEqual(2, _gained[3].Level);
            Assert.AreEqual(0, _gained[3].NextThreshold, "Past the last level there is no next.");
            Assert.IsTrue(_understanding.IsUnlocked(Rings));
            Assert.AreEqual(4, _gained.Count);
        }

        [Test]
        public void ALoadedGamePastTheSignal_StartsSilent_WithItsUnlocks()
        {
            _sources.Stories.Add("six");
            _sources.Stories.Add("post");
            _databank.Restore(new DatabankSaveData());
            _understanding.Announce();

            Signal(true);

            Assert.IsEmpty(_gained, "Nothing new to say.");
            Assert.AreEqual(2, _understanding.PointsOf(DatabankMaker.KesslerArai));
            Assert.IsTrue(_understanding.IsUnlocked(Lamp));

            Story("one");
            Assert.AreEqual(1, _gained.Count);
            Assert.AreEqual(3, _gained[0].Points);
            Assert.IsFalse(_gained[0].First);
        }

        [Test]
        public void ALoadedGameBeforeTheSignal_StillAnnouncesItsHeldGainAtTheSignal()
        {
            _sources.Stories.Add("six");
            _databank.Restore(new DatabankSaveData());
            _understanding.Announce();

            Signal(false);

            Assert.AreEqual(1, _gained.Count);
            Assert.AreEqual(1, _gained[0].Points);
            Assert.IsTrue(_gained[0].First);
        }

        [Test]
        public void AnUnknownBlueprint_IsNeverUnlocked()
        {
            Signal(false);
            Story("six");
            Story("post");

            Assert.IsFalse(_understanding.IsUnlocked("adapted.nothing"));
            Assert.IsFalse(_understanding.IsUnlocked(null));
        }

        [Test]
        public void TheShippedTracks_Validate_AndKesslerAraiLevelOneIsTheHeadlamp()
        {
            var shipped = new MakerUnderstandingSettings();
            Assert.IsNull(shipped.Validate());
            MakerTrack ka = shipped.Tracks[0];
            Assert.AreEqual(DatabankMaker.KesslerArai, ka.Maker);
            Assert.AreEqual(2, ka.Threshold(1));
            Assert.AreEqual(Lamp, ka.Adapted(1));
        }

        [Test]
        public void BrokenTracks_FailValidation()
        {
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.None, new[] { 1 }, new[] { Lamp })));
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.VIS, new[] { 2, 2 }, new[] { Lamp, Rings })),
                "Thresholds must rise.");
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.VIS, new[] { 0 }, new[] { Lamp })));
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.VIS, new[] { 1, 2 }, new[] { Lamp })),
                "One blueprint per level.");
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.VIS, new[] { 1 }, new[] { "ssr3_lamp" })));
            Assert.IsNotNull(Settings(new MakerTrack(DatabankMaker.VIS, new[] { 1 }, new[] { Lamp }),
                new MakerTrack(DatabankMaker.VIS, new[] { 2 }, new[] { Rings })), "One track per maker.");
        }

        private static string Settings(params MakerTrack[] tracks)
        {
            return new MakerUnderstandingSettings(tracks).Validate();
        }

        private void Story(string id)
        {
            _events.Publish(new StoryScanned(StoryScan.IdPrefix + id, "k", Vector3.zero));
        }

        private void Fragment(string fragmentId, int before)
        {
            _events.Publish(new FragmentScanned(fragmentId, "structure", before, 2, false, 0));
        }

        private void Signal(bool restored)
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, restored));
        }

        private static DatabankEntry Entry(string id, DatabankMaker maker, DatabankUnlock unlock)
        {
            return new DatabankEntry(id, DatabankCategory.Machines, id + ".name", maker, "1983", DatabankEra.Lumen,
                id + ".readout", id + ".history", DatabankViewKind.Print, null, null, 0f, null, unlock);
        }

        private sealed class FakeSources : IDatabankSources
        {
            public readonly HashSet<string> Stories = new HashSet<string>();
            public readonly Dictionary<string, int> Sizes = new Dictionary<string, int>();

            public bool IsStoryScanned(string storyId)
            {
                return Stories.Contains(storyId);
            }

            public int ScannedInSet(string setId)
            {
                return 0;
            }

            public int SetSize(string setId)
            {
                return Sizes.TryGetValue(setId, out int size) ? size : 0;
            }

            public bool HasReached(OpeningMilestone milestone)
            {
                return false;
            }
        }
    }
}
