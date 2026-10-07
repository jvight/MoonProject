using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The radio program (owned tapes, Bell's dial, saves, events), cassette content, anchored spots and the basin
    /// cassette planner.
    /// </summary>
    public sealed class RadioAndCassetteTests
    {
        private const string AfterDark = "after_dark_1";
        private const string DustAndHoney = "dust_and_honey";
        private const string SlowOrbit = "slow_orbit";

        private static readonly string[] Tapes = { AfterDark, DustAndHoney, SlowOrbit };

        private readonly List<Object> _created = new List<Object>();
        private EventBus _events;
        private int _changes;
        private IDisposable _subscription;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _changes = 0;
            _subscription = _events.Subscribe<RadioProgramChanged>(_ => _changes++);
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Radio_BeforeBell_PlaysLumenAfterDark_AndOwnsWhatWasCollected()
        {
            var radio = new RadioProgram(_events, Tapes);
            Assert.IsFalse(radio.DialUnlocked);
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel);
            Assert.AreEqual(string.Empty, radio.SelectedTape);
            Assert.AreEqual(0, radio.OwnedTapeCount);
            Assert.AreEqual(3, radio.TotalTapeCount, "the catalog's tapes");
            Assert.IsFalse(radio.TurnDial(), "no dial before Bell");
            Assert.AreEqual(0, _changes);

            Assert.IsTrue(radio.AddTape(DustAndHoney));
            Assert.AreEqual(1, _changes, "a new tape changes the program");
            Assert.IsTrue(radio.Owns(DustAndHoney));
            Assert.IsTrue(radio.Holds(DustAndHoney), "friends see owned tapes as held items");
            Assert.IsFalse(radio.Holds(AfterDark));
            Assert.IsFalse(radio.AddTape(DustAndHoney), "a tape is collected once");
            Assert.AreEqual(1, _changes);
            Assert.Throws<ArgumentException>(() => radio.AddTape("not_a_tape"));
            Assert.IsTrue(radio.AddTape(AfterDark));
            Assert.AreEqual(DustAndHoney, radio.GetOwnedTape(0), "collection order");
            Assert.AreEqual(AfterDark, radio.GetOwnedTape(1));
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel, "still Ro's show, tapes in the shuffle");
        }

        [Test]
        public void Radio_Dial_ClicksThroughLumenEachTapeAndQuietHours()
        {
            var radio = new RadioProgram(_events, Tapes);
            radio.AddTape(AfterDark);
            radio.AddTape(SlowOrbit);
            Assert.IsTrue(radio.UnlockDial());
            Assert.IsFalse(radio.UnlockDial(), "once");
            Assert.AreEqual(3, _changes);

            Assert.IsTrue(radio.TurnDial());
            Assert.AreEqual(RadioChannel.TapeDeck, radio.Channel);
            Assert.AreEqual(AfterDark, radio.SelectedTape, "the deck starts with the first tape found");
            radio.TurnDial();
            Assert.AreEqual(RadioChannel.TapeDeck, radio.Channel);
            Assert.AreEqual(SlowOrbit, radio.SelectedTape);
            radio.TurnDial();
            Assert.AreEqual(RadioChannel.QuietHours, radio.Channel);
            Assert.AreEqual(SlowOrbit, radio.SelectedTape, "the deck remembers its tape");
            radio.TurnDial();
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel, "and round again");
            Assert.AreEqual(7, _changes, "every click is a change");
        }

        [Test]
        public void Radio_WithoutTapes_TheDialSkipsTheTapeDeck()
        {
            var radio = new RadioProgram(_events, Tapes);
            radio.UnlockDial();
            radio.TurnDial();
            Assert.AreEqual(RadioChannel.QuietHours, radio.Channel);
            radio.TurnDial();
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel);
        }

        [Test]
        public void Radio_RoundTripsThroughJson_QuietlyUntilAnnounced()
        {
            var radio = new RadioProgram(_events, Tapes);
            radio.AddTape(SlowOrbit);
            radio.AddTape(AfterDark);
            radio.UnlockDial();
            radio.TurnDial();
            radio.TurnDial();
            string json = JsonUtility.ToJson(radio.Capture());

            _changes = 0;
            var loaded = new RadioProgram(_events, Tapes);
            loaded.Restore(JsonUtility.FromJson<RadioSaveData>(json));
            Assert.AreEqual(0, _changes, "a restore waits for the whole save");
            Assert.IsTrue(loaded.DialUnlocked);
            Assert.AreEqual(RadioChannel.TapeDeck, loaded.Channel);
            Assert.AreEqual(AfterDark, loaded.SelectedTape);
            Assert.AreEqual(2, loaded.OwnedTapeCount);
            Assert.AreEqual(SlowOrbit, loaded.GetOwnedTape(0));
            Assert.AreEqual(AfterDark, loaded.GetOwnedTape(1));
            loaded.Announce();
            Assert.AreEqual(1, _changes, "then it is announced once");
        }

        [Test]
        public void Radio_Restore_KeepsTheProgramConsistent_AndNeverLosesATape()
        {
            var radio = new RadioProgram(_events, Tapes);
            radio.AddTape(DustAndHoney);
            LogAssert.Expect(LogType.Warning, new Regex("unknown cassette 'gone_tape'"));
            radio.Restore(new RadioSaveData
            {
                tapes = new[] { SlowOrbit, "gone_tape", SlowOrbit },
                channel = (int)RadioChannel.TapeDeck,
                selectedTape = "gone_tape",
                dialUnlocked = false,
            });
            Assert.AreEqual(2, radio.OwnedTapeCount, "the unknown and the double are dropped, the held tape kept");
            Assert.AreEqual(SlowOrbit, radio.GetOwnedTape(0), "saved tapes first");
            Assert.AreEqual(DustAndHoney, radio.GetOwnedTape(1));
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel, "no dial, no other channel");
            Assert.AreEqual(string.Empty, radio.SelectedTape);

            var unlocked = new RadioProgram(_events, Tapes);
            unlocked.Restore(new RadioSaveData
            {
                tapes = new[] { AfterDark, SlowOrbit },
                channel = (int)RadioChannel.TapeDeck,
                selectedTape = DustAndHoney,
                dialUnlocked = true,
            });
            Assert.AreEqual(RadioChannel.TapeDeck, unlocked.Channel);
            Assert.AreEqual(AfterDark, unlocked.SelectedTape, "a deck with no owned tape chosen takes the first");

            var odd = new RadioProgram(_events, Tapes);
            odd.UnlockDial();
            odd.Restore(new RadioSaveData { channel = 7, dialUnlocked = false });
            Assert.IsTrue(odd.DialUnlocked, "an unlocked dial stays unlocked");
            Assert.AreEqual(RadioChannel.LumenAfterDark, odd.Channel, "an unknown channel is Ro's show");
        }

        [Test]
        public void AnchorSpot_IsOffsetInTheAnchorsFrame_OnTheSurface_FacingBackTo07()
        {
            TestWorld world = TestWorld.Basin();
            var anchors = new FakeAnchors(new WorldAnchor(WorldAnchorIds.CanyonTerminus, new Vector3(10f, 3f, 20f),
                Vector3.forward, 6f));
            var spot = new AnchorSpot(WorldAnchorIds.CanyonTerminus, new Vector2(2f, 3f));
            Assert.IsTrue(spot.TryResolve(anchors, world, out Vector3 position, out Vector3 facing));
            Assert.AreEqual(12f, position.x, 1e-4f, "x is to the anchor's right");
            Assert.AreEqual(23f, position.z, 1e-4f, "y is along its forward");
            Assert.AreEqual(world.SampleHeight(12f, 23f), position.y, 1e-4f, "on the surface");
            Assert.AreEqual(Vector3.back, facing, "facing 07, who arrives along the anchor's forward");

            var turned = new FakeAnchors(new WorldAnchor(WorldAnchorIds.CanyonLedge, Vector3.zero, Vector3.right, 4f));
            new AnchorSpot(WorldAnchorIds.CanyonLedge, new Vector2(1f, 2f)).TryResolve(turned, world, out position,
                out facing);
            Assert.AreEqual(2f, position.x, 1e-4f);
            Assert.AreEqual(-1f, position.z, 1e-4f, "right of an east-facing anchor is south");
            Assert.IsFalse(new AnchorSpot("canyon.nowhere", Vector2.zero).TryResolve(anchors, world, out _, out _),
                "a missing anchor is reported, never invented");
        }

        [Test]
        public void AbilityGate_OpensWithItsAbility()
        {
            var abilities = new FakeAbilities();
            var gate = new AbilityGate(true, RoverAbility.HoverJump);
            Assert.IsTrue(AbilityGate.Open.IsOpen(abilities));
            Assert.IsFalse(gate.IsOpen(abilities));
            abilities.Grant(RoverAbility.HoverJump);
            Assert.IsTrue(gate.IsOpen(abilities));
        }

        [Test]
        public void CassetteCatalog_ReportsIncompleteContent()
        {
            GameObject prefab = Track(new GameObject("Cassette"));
            var catalog = Track(ScriptableObject.CreateInstance<CassetteCatalog>());
            var anchored = Cassette("a", prefab, CassetteSiteRule.Anchor, new AnchorSpot(WorldAnchorIds.CanyonLedge,
                Vector2.zero));
            var planned = Cassette("b", prefab, CassetteSiteRule.BasinPlanner, new AnchorSpot(string.Empty,
                Vector2.zero));
            catalog.Populate(new[] { anchored, planned });
            Assert.IsNull(catalog.Validate());
            CollectionAssert.AreEqual(new[] { "a", "b" }, catalog.Ids());

            catalog.Populate(new[] { anchored, Cassette("a", prefab, CassetteSiteRule.BasinPlanner,
                new AnchorSpot(string.Empty, Vector2.zero)) });
            StringAssert.Contains("twice", catalog.Validate());
            catalog.Populate(new[] { Cassette("c", prefab, CassetteSiteRule.Anchor, new AnchorSpot(" ",
                Vector2.zero)) });
            StringAssert.Contains("names none", catalog.Validate());
            catalog.Populate(new[] { Cassette("d", null, CassetteSiteRule.BasinPlanner, new AnchorSpot(string.Empty,
                Vector2.zero)) });
            StringAssert.Contains("prefab", catalog.Validate());
        }

        [Test]
        public void BasinCassette_TucksAgainstASmallCraterRim_InItsBand_Deterministically()
        {
            TestWorld world = TestWorld.WithCrater();
            var tuning = Track(ScriptableObject.CreateInstance<CassetteTuning>());
            CassetteSite site = CassetteSitePlanner.Plan(world, world, tuning, 73, new List<Vector3>());
            Assert.IsTrue(site.Tucked, "the flat basin's only rim is the small crater's");
            Assert.Less(SurfaceRules.HorizontalDistance(site.Position, TestWorld.CraterCentre), 15f, "inside it");
            float fromHome = SurfaceRules.HorizontalDistance(site.Position, world.BasePosition);
            Assert.That(fromHome, Is.InRange(tuning.Distance.x, tuning.Distance.y));
            Assert.IsTrue(world.PlayableArea.Contains(new Vector2(site.Position.x, site.Position.z)));
            Assert.AreEqual(world.SampleHeight(site.Position.x, site.Position.z), site.Position.y, 1e-4f);
            float rise = CassetteSitePlanner.Rim(world, site.Position.x, site.Position.z, tuning.RimProbe,
                tuning.RimDirections, out Vector3 toRim);
            Assert.That(rise, Is.InRange(tuning.RimRise.x, tuning.RimRise.y));
            Assert.AreEqual(-1f, Vector3.Dot(toRim, site.Facing), 1e-4f, "its label faces away from the rim");

            CassetteSite again = CassetteSitePlanner.Plan(world, world, tuning, 73, new List<Vector3>());
            Assert.AreEqual(site.Position, again.Position, "deterministic");
        }

        [Test]
        public void BasinCassette_KeepsClearOfRelicsAndFriends()
        {
            TestWorld world = TestWorld.WithCrater();
            var tuning = Track(ScriptableObject.CreateInstance<CassetteTuning>());
            var keepClear = new List<Vector3> { TestWorld.CraterCentre, new Vector3(60f, 0f, -30f) };
            CassetteSite site = CassetteSitePlanner.Plan(world, world, tuning, 73, keepClear);
            foreach (Vector3 point in keepClear)
            {
                Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site.Position, point), tuning.Clearance);
            }

            Assert.IsFalse(site.Tucked, "the crater belongs to a friend: the best open spot instead");
            Assert.IsTrue(SurfaceRules.InsideDrivable(world, site.Position.x, site.Position.z, tuning.EdgeMargin));
        }

        [Test]
        public void BasinCassette_ThrowsWhenTheBasinLeavesNoSpot()
        {
            TestWorld world = TestWorld.Flat();
            var tuning = Track(ScriptableObject.CreateInstance<CassetteTuning>());
            var everywhere = new List<Vector3>();
            for (float x = -300f; x <= 300f; x += 30f)
            {
                for (float z = -300f; z <= 300f; z += 30f)
                {
                    everywhere.Add(new Vector3(x, 0f, z));
                }
            }

            Assert.Throws<InvalidOperationException>(() =>
                CassetteSitePlanner.Plan(world, world, tuning, 73, everywhere));
        }

        private CassetteDefinition Cassette(string id, GameObject prefab, CassetteSiteRule site, AnchorSpot anchor)
        {
            var cassette = Track(ScriptableObject.CreateInstance<CassetteDefinition>());
            cassette.Populate(id, prefab, site, anchor, 1, AbilityGate.Open);
            return cassette;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private sealed class FakeAnchors : IWorldAnchors
        {
            private readonly WorldAnchor[] _anchors;

            public FakeAnchors(params WorldAnchor[] anchors)
            {
                _anchors = anchors;
            }

            public int Count => _anchors.Length;

            public WorldAnchor Get(int index)
            {
                return _anchors[index];
            }

            public bool TryGet(string id, out WorldAnchor anchor)
            {
                foreach (WorldAnchor candidate in _anchors)
                {
                    if (candidate.Id == id)
                    {
                        anchor = candidate;
                        return true;
                    }
                }

                anchor = default;
                return false;
            }
        }

        private sealed class FakeAbilities : IRoverAbilities
        {
            private readonly HashSet<RoverAbility> _owned = new HashSet<RoverAbility>();

            public bool Has(RoverAbility ability)
            {
                return _owned.Contains(ability);
            }

            public void Grant(RoverAbility ability)
            {
                _owned.Add(ability);
            }
        }
    }
}
