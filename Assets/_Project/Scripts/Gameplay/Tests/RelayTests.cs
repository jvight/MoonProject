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
    /// The relay network's pure logic (docs/features/M3-06): the station's reach over the real chain, the scrap cost
    /// and VISION ruling 5, where a mast's part lies, the restoration and hop timelines, the radio-hop's list and
    /// sequence, and the mast rig.
    /// </summary>
    public sealed class RelayTests
    {
        // The real basin's relay pads (World seed, M3-06 world) relative to the base pad at the origin.
        private static readonly Vector3 Relay0 = new Vector3(62.63f, 1.68f, 92.85f);
        private static readonly Vector3 Relay1 = new Vector3(-204.85f, 7.97f, -74.56f);
        private static readonly Vector3 Relay2 = new Vector3(268.1f, 5.74f, 74.49f);
        private static readonly Vector3 Relay3 = new Vector3(456.57f, 36.35f, -16.66f);
        private static readonly string[] Ids = { "relay.0", "relay.1", "relay.2", "relay.3" };

        // Home's lamp (the tower's beacon) and how high a mast's lamp stands, as the field passes them in.
        private static readonly Vector3 HomeBeacon = new Vector3(-16f, 6.6f, 7f);
        private const float MastLampHeight = 8.25f;

        // The radio tower's clear-signal radius before any level and at level 1 (the content builder's tower).
        private const float DarkTower = 60f;
        private const float TowerLevel1 = 110f;

        // The other sinks of VISION ruling 5, as in the content builder: the tower's three levels and Hover-Jump.
        private const int TowerCost = 15 + 40 + 80;
        private const int HoverJumpCost = 150;
        private const int Relics = 6;

        private const float Frame = 1f / 60f;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Reach_GrowsOutFromHome_OnlyThroughLitNeighbours()
        {
            StationReach reach = RealChain(TowerLevel1, out RelayTuning tuning);
            Assert.AreEqual(5, reach.NodeCount);
            Assert.AreEqual(1, reach.LitCount, "home alone");
            Assert.AreEqual(StationReach.HomeId, reach.GetNode(0).Id);
            Assert.IsTrue(reach.IsInReach(new Vector3(100f, 0f, 0f)));
            Assert.IsFalse(reach.IsInReach(new Vector3(150f, 0f, 0f)));
            Assert.AreEqual(150f, reach.DistanceToNearestNode(new Vector3(150f, 3f, 0f)), 1e-3f);

            reach.SetRestored(3, true);
            Assert.IsFalse(reach.IsLit(3), "relay.2 restored first waits: nothing lit reaches it");
            reach.SetRestored(4, true);
            Assert.IsFalse(reach.IsLit(4), "relay.3 waits behind relay.2");
            reach.SetRestored(1, true);
            Assert.IsTrue(reach.IsLit(1), "relay.0 links home");
            Assert.IsTrue(reach.IsLit(3), "...and relay.0 links relay.2 (206 m < 220 m)");
            Assert.IsTrue(reach.IsLit(4), "...and relay.2 links relay.3 (209 m < 220 m)");
            Assert.AreEqual(4, reach.LitCount);
            Assert.AreEqual(3, reach.LitMasts);
            Assert.AreEqual(1, reach.Depth(1));
            Assert.AreEqual(2, reach.Depth(3));
            Assert.AreEqual(3, reach.Depth(4));
            Assert.AreEqual(0, reach.NearestLink(1), "relay.0's pulse runs home");
            Assert.AreEqual(3, reach.NearestLink(4), "relay.3's runs to relay.2");
            Assert.IsTrue(reach.IsInReach(Relay3 + new Vector3(-80f, 0f, 30f)), "deep in the canyon is home now");
            Assert.AreEqual(0f, reach.DistanceToNearestNode(Relay2), 1e-3f);
            Assert.IsTrue(reach.GetNode(4).Lit);
            Assert.AreEqual(Relay3, reach.GetNode(4).Position);
            Assert.AreEqual(tuning.MastReach, reach.GetNode(4).Radius, "audio reads a mast's reach from the node");
            Assert.AreEqual(Lamp(Relay3), reach.GetNode(4).LampPosition);
            Assert.AreEqual(TowerLevel1, reach.GetNode(0).Radius);
            Assert.AreEqual(HomeBeacon, reach.GetNode(0).LampPosition, "home's lamp is the tower's beacon");
            reach.SetHomeLamp(HomeBeacon + Vector3.up * 3.4f);
            Assert.AreEqual(HomeBeacon + Vector3.up * 3.4f, reach.GetNode(0).LampPosition, "a taller stage");
            Assert.GreaterOrEqual(tuning.MastReach, 105f, "the world's chain needs at least 105 m per mast");
        }

        [Test]
        public void Reach_RimShoulder_LinksOnceTheTowerWidensHome()
        {
            StationReach reach = RealChain(DarkTower, out _);
            reach.SetRestored(2, true);
            Assert.IsFalse(reach.IsLit(2), "218 m out: the dark tower's 60 m and the mast's 110 m do not meet");
            reach.SetHomeRadius(TowerLevel1);
            Assert.IsTrue(reach.IsLit(2), "the tower's first level reaches it");
            Assert.AreEqual(TowerLevel1, reach.HomeRadius);
            Assert.AreEqual(TowerLevel1, reach.GetNode(0).Radius, "home's node follows the tower's level");
            Assert.Throws<ArgumentException>(() => reach.SetRestored(StationReach.Home, false));
        }

        [Test]
        public void Reach_Queries_AllocateNothing()
        {
            StationReach reach = RealChain(TowerLevel1, out _);
            reach.SetRestored(1, true);
            var probe = new Vector3(120f, 0f, 40f);
            bool inReach = false;
            float distance = 0f;
            int lit = 0;
            for (int warm = 0; warm < 10; warm++)
            {
                inReach |= reach.IsInReach(probe);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                inReach |= reach.IsInReach(probe);
                distance += reach.DistanceToNearestNode(probe);
                for (int node = 0; node < reach.NodeCount; node++)
                {
                    lit += reach.GetNode(node).Lit ? 1 : 0;
                }
            }

            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before, "polled every frame by Audio and UI");
            Assert.IsTrue(inReach);
            Assert.Greater(distance, 0f);
            Assert.AreEqual(2000, lit);
        }

        [Test]
        public void Cost_Escalates_AndTheBasinStillFundsTwiceEverySink()
        {
            var relays = Create<RelayTuning>();
            Assert.AreEqual(60, relays.CostAfter(0));
            Assert.AreEqual(90, relays.CostAfter(1));
            Assert.AreEqual(120, relays.CostAfter(2));
            Assert.AreEqual(150, relays.CostAfter(3));
            Assert.AreEqual(420, relays.TotalCost(Ids.Length));
            Assert.IsNull(relays.Validate(Ids.Length));
            StringAssert.Contains("prices", relays.Validate(Ids.Length + 1));

            var scrap = Create<ScrapTuning>();
            var home = Create<BaseTuning>();
            int income = scrap.MinTotalValue + Relics * home.DepositGift;
            int sinks = TowerCost + HoverJumpCost + relays.TotalCost(Ids.Length);
            Assert.GreaterOrEqual(income, 2 * sinks, $"VISION ruling 5: income {income} vs sinks {sinks}");
        }

        [Test]
        public void Part_LiesInRange_OnGround_07_CanDriveTo_NeverBelowALedge()
        {
            TestWorld world = TestWorld.WithStep();
            var relays = Create<RelayTuning>();
            var paths = Create<FriendTuning>();
            var mast = new WorldAnchor("relay.9", new Vector3(-2f, 2.5f, 0f), Vector3.left, 3f);
            for (int seed = 0; seed < 6; seed++)
            {
                Assert.IsTrue(RelayPartPlanner.TryPlan(world, mast, relays, paths, seed, out Vector3 spot,
                    out string problem), problem);
                float distance = SurfaceRules.HorizontalDistance(spot, mast.Position);
                Assert.That(distance, Is.InRange(relays.PartDistance.x, relays.PartDistance.y));
                Assert.LessOrEqual(distance, 40f, "the design caps it at 40 m");
                Assert.Less(spot.x, 10f, $"seed {seed}: on the mast's ledge, not below the step");
                Assert.AreEqual(world.SampleHeight(spot.x, spot.z), spot.y, 1e-4f, "on the surface");
                Assert.IsNotNull(WalkPathPlanner.Plan(world, mast.Position, spot, paths), "07 can drive there");
                RelayPartPlanner.TryPlan(world, mast, relays, paths, seed, out Vector3 again, out _);
                Assert.AreEqual(spot, again, "deterministic");
            }

            var lost = new WorldAnchor("relay.9", new Vector3(0f, 0f, 400f), Vector3.back, 3f);
            Assert.IsFalse(RelayPartPlanner.TryPlan(world, lost, relays, paths, 0, out _, out string none));
            StringAssert.Contains("relay.9", none);
        }

        [Test]
        public void Beat_Stitches_SlotsThePart_Straightens_ThenWarms()
        {
            var relays = Create<RelayTuning>();
            RelayBeat beat = RelayBeat.For(relays);
            Assert.IsTrue(beat.Beaming(0f));
            Assert.IsFalse(beat.Beaming(beat.StitchEnd));
            Assert.LessOrEqual(beat.PartIn, beat.StitchEnd, "the part is in before the beam lets go");
            Assert.AreEqual(1f, beat.Part(beat.PartIn), 1e-5f);
            Assert.AreEqual(0f, beat.Upright(beat.StitchEnd * 0.5f), "leaning while stitched");
            float peak = 0f;
            for (float t = beat.StitchEnd; t <= beat.StraightenEnd; t += Frame)
            {
                peak = Mathf.Max(peak, beat.Upright(t));
            }

            Assert.Greater(peak, 1f, "it rocks once past upright, with a creak");
            Assert.AreEqual(1f, beat.Upright(beat.StraightenEnd), 1e-5f);
            Assert.AreEqual(0f, beat.Warm(beat.StraightenEnd), 1e-5f);
            Assert.AreEqual(1f, beat.Warm(beat.Duration), 1e-5f);
            Assert.IsFalse(beat.Done(beat.Duration - Frame));
            Assert.IsTrue(beat.Done(beat.Duration));
            Assert.Less(beat.Duration, 8f, "shorter than a friend's repair beat and its walk");
        }

        [Test]
        public void HopSequence_EasesOutAndBackIn_InAboutTwoSeconds()
        {
            HopSequence hop = HopSequence.For(Create<RelayTuning>());
            Assert.That(hop.Duration, Is.InRange(1.5f, 2.5f));
            Assert.AreEqual(0f, hop.Fade(0f), 1e-5f, "never a cut");
            Assert.AreEqual(1f, hop.Fade(hop.PlaceAt), 1e-5f, "07 moves only while the view is dark");
            Assert.AreEqual(1f, hop.Fade(hop.FinishAt - Frame), 1e-5f);
            Assert.AreEqual(0f, hop.Fade(hop.Duration), 1e-5f);
            Assert.Less(hop.Fade(hop.PlaceAt * 0.5f), 0.9f);
            Assert.AreEqual(1f, hop.Progress(hop.Duration), 1e-5f);
            Assert.Throws<ArgumentException>(() => new HopSequence(0f, 0.4f, 0.8f), "never instant");
        }

        [Test]
        public void Hop_OpensOnALitPad_TapCycles_HoldHops_ThroughTheDark()
        {
            var bus = new EventBus();
            var opened = new List<bool>();
            var started = new List<RadioHopStarted>();
            var finished = new List<RadioHopFinished>();
            using IDisposable a = bus.Subscribe<RadioHopListChanged>(e => opened.Add(e.Open));
            using IDisposable b = bus.Subscribe<RadioHopStarted>(e => started.Add(e));
            using IDisposable c = bus.Subscribe<RadioHopFinished>(e => finished.Add(e));
            StationReach reach = RealChain(TowerLevel1, out RelayTuning tuning);
            var rover = new FakeRover { Position = Relay0 };
            var tether = new FakeTether();
            RadioHop hop = Hop(bus, reach, tuning, rover, rover, tether);

            hop.Step(Frame, false, Vector2.zero);
            Assert.IsFalse(hop.CanOpen, "relay.0 is dark: no hop from it");
            reach.SetRestored(1, true);
            reach.SetRestored(3, true);
            hop.Step(Frame, false, Vector2.zero);
            Assert.IsTrue(hop.CanOpen);
            Assert.AreEqual(1, hop.Here);
            rover.Speed = 3f;
            hop.Step(Frame, false, Vector2.zero);
            Assert.IsFalse(hop.CanOpen, "only parked");
            rover.Speed = 0f;
            tether.State = TetherAimState.Towing;
            hop.Step(Frame, false, Vector2.zero);
            Assert.IsFalse(hop.CanOpen, "relics come home by road");
            tether.State = TetherAimState.Idle;

            hop.Step(Frame, true, Vector2.zero);
            Assert.AreEqual(RadioHopPhase.Choosing, hop.Phase);
            CollectionAssert.AreEqual(new[] { true }, opened);
            Assert.AreEqual(2, hop.ChoiceCount, "home and relay.2, not relay.0 itself");
            Assert.AreEqual(0, hop.ChoiceNode(0));
            Assert.AreEqual("hop.node.home", hop.ChoiceLabelKey(0));
            Assert.AreEqual("hop.node.relay.2", hop.ChoiceLabelKey(1));
            Step(hop, 1f, true);
            Assert.AreEqual(RadioHopPhase.Choosing, hop.Phase, "the press that opened it never hops");
            hop.Step(Frame, false, Vector2.zero);
            hop.Step(Frame, true, Vector2.zero);
            hop.Step(Frame, false, Vector2.zero);
            Assert.AreEqual(1, hop.Selected, "a tap picks the next node");
            hop.Previous();
            Assert.AreEqual(0, hop.Selected);
            Assert.IsTrue(rover.Gazes > 0, "07 glances toward the highlighted node");

            Step(hop, tuning.HopConfirmHold + Frame, true);
            Assert.AreEqual(RadioHopPhase.Leaving, hop.Phase);
            CollectionAssert.AreEqual(new[] { true, false }, opened, "the list closes as the hop begins");
            Assert.AreEqual("relay.0", started[0].FromId);
            Assert.AreEqual(StationReach.HomeId, started[0].ToId);
            Assert.AreEqual(1, rover.Holds, "07 holds still");
            HopSequence sequence = HopSequence.For(tuning);
            Step(hop, sequence.PlaceAt - 0.05f, false);
            Assert.AreEqual(0, rover.Placements, "not before the view is dark");
            Assert.Greater(hop.Fade, 0.9f);
            Step(hop, 0.1f, false);
            Assert.AreEqual(RadioHopPhase.Dark, hop.Phase);
            Assert.AreEqual(1, rover.Placements);
            Assert.AreEqual(reach.Position(0), rover.Position, "on home's pad");
            Assert.IsEmpty(finished);
            Step(hop, sequence.FinishAt - sequence.PlaceAt, false);
            Assert.AreEqual(StationReach.HomeId, finished[0].ToId);
            Assert.AreEqual(RadioHopPhase.Arriving, hop.Phase);
            Step(hop, sequence.Duration, false);
            Assert.AreEqual(RadioHopPhase.Closed, hop.Phase);
            Assert.AreEqual(0, rover.Holds, "07 is free again");
            Assert.AreEqual(0f, hop.Fade);

            hop.Step(Frame, false, Vector2.zero);
            hop.Step(Frame, true, Vector2.zero);
            Assert.AreEqual(RadioHopPhase.Choosing, hop.Phase, "home is a pad too");
            hop.Step(Frame, false, new Vector2(0f, 1f));
            Assert.AreEqual(RadioHopPhase.Closed, hop.Phase, "driving off closes the list");
            Assert.AreEqual(false, opened[opened.Count - 1]);
        }

        [Test]
        public void Hop_WithoutARoverPlacement_FailsLoudly_AndNeverStarts()
        {
            var bus = new EventBus();
            var started = new List<RadioHopStarted>();
            using IDisposable a = bus.Subscribe<RadioHopStarted>(e => started.Add(e));
            StationReach reach = RealChain(TowerLevel1, out RelayTuning tuning);
            reach.SetRestored(1, true);
            var rover = new FakeRover { Position = reach.Position(0) };
            RadioHop hop = Hop(bus, reach, tuning, rover, null, new FakeTether());
            hop.Step(Frame, false, Vector2.zero);
            Assert.IsTrue(hop.Open());
            LogAssert.Expect(LogType.Error, new Regex("no IRoverPlacement is registered"));
            Assert.IsFalse(hop.Confirm());
            Assert.AreEqual(RadioHopPhase.Closed, hop.Phase);
            Assert.IsEmpty(started);
            Assert.AreEqual(0, rover.Holds);
        }

        [Test]
        public void Rig_StraightensFromTheBrokenLean_AndIsSolidToRover()
        {
            var restored = new RelayRig(Track(Model("RelayMast", false)));
            var broken = new RelayRig(Track(Model("RelayMast_Broken", true)));
            Transform mast = restored.Root.Find(RelayRig.MastNode);
            Transform leaning = broken.Root.Find(RelayRig.MastNode);
            restored.CapturePoseFrom(broken);
            restored.Straighten(0f);
            Assert.Less(Quaternion.Angle(leaning.localRotation, mast.localRotation), 0.01f, "starts leaning");
            restored.Straighten(1.1f);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, mast.localRotation), 0.5f, "rocks past upright");
            restored.Straighten(1f);
            Assert.Less(Quaternion.Angle(Quaternion.identity, mast.localRotation), 0.01f, "stands upright");

            restored.MakeSolid();
            foreach (string node in new[] { RelayRig.BaseNode, RelayRig.MastNode })
            {
                Transform solid = restored.Root.Find(node);
                Assert.AreEqual(Layers.Prop, solid.gameObject.layer, node);
                Assert.IsNotNull(solid.GetComponent<MeshCollider>().sharedMesh, node);
            }

            restored.SetLamp(0.8f);
            Assert.AreEqual(0.8f, restored.LampLevel, 1e-5f);
            Assert.AreEqual(RelayRig.PartSocketNode, restored.PartSocket.name);
            Object.DestroyImmediate(restored.Root.Find(RelayRig.BeamPointNode).gameObject);
            Assert.Throws<InvalidOperationException>(() => new RelayRig(restored.Root.gameObject));
        }

        private StationReach RealChain(float homeRadius, out RelayTuning tuning)
        {
            tuning = Create<RelayTuning>();
            var pads = new[] { Relay0, Relay1, Relay2, Relay3 };
            var lamps = new[] { Lamp(Relay0), Lamp(Relay1), Lamp(Relay2), Lamp(Relay3) };
            return new StationReach(Vector3.zero, homeRadius, HomeBeacon, Ids, pads, lamps, tuning.MastReach);
        }

        private static Vector3 Lamp(Vector3 pad)
        {
            return pad + new Vector3(0f, MastLampHeight, -2.2f);
        }

        private static RadioHop Hop(EventBus bus, StationReach reach, RelayTuning tuning, FakeRover rover,
            IRoverPlacement placement, FakeTether tether)
        {
            var facings = new Quaternion[reach.NodeCount];
            var labels = new string[reach.NodeCount];
            for (int node = 0; node < reach.NodeCount; node++)
            {
                facings[node] = Quaternion.identity;
                labels[node] = "hop.node." + reach.Id(node);
            }

            return new RadioHop(bus, reach, tuning, rover, rover, placement, tether, facings, labels);
        }

        private static void Step(RadioHop hop, float seconds, bool interact)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                hop.Step(Frame, interact, Vector2.zero);
            }
        }

        /// <summary>A stand-in mast with the contract's nodes (the fixture's shape).</summary>
        private static GameObject Model(string name, bool broken)
        {
            var root = new GameObject(name);
            Block(root.transform, RelayRig.BaseNode, new Vector3(0f, 0.15f, -2.2f), new Vector3(1.9f, 0.3f, 1.9f));
            Transform mast = Block(root.transform, RelayRig.MastNode, new Vector3(0f, 0.3f, -2.2f), Vector3.one);
            new GameObject(RelayRig.DishNode).transform.SetParent(mast, false);
            Block(mast, RelayRig.LampNode, new Vector3(0f, 7.95f, 0f), Vector3.one * 0.5f);
            new GameObject(RelayRig.PartSocketNode).transform.SetParent(root.transform, false);
            new GameObject(RelayRig.BeamPointNode).transform.SetParent(root.transform, false);
            if (broken)
            {
                mast.localRotation = Quaternion.Euler(-9f, 0f, 10f);
            }

            return root;
        }

        private static Transform Block(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            return block.transform;
        }

        private T Create<T>() where T : ScriptableObject
        {
            return Track(ScriptableObject.CreateInstance<T>());
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private sealed class FakeRover : IRoverState, IRoverRig, IRoverPlacement
        {
            private readonly HashSet<object> _holders = new HashSet<object>();

            public Vector3 Position { get; set; }

            public Quaternion Rotation { get; private set; } = Quaternion.identity;

            public Vector3 Velocity => Vector3.zero;

            public float Speed { get; set; }

            public float NormalizedSpeed => 0f;

            public Vector2 DriveInput => Vector2.zero;

            public bool IsGrounded => true;

            public float AirTime => 0f;

            public Vector3 GroundNormal => Vector3.up;

            public Transform TetherOrigin => null;

            public Transform CargoSocket => null;

            public Rigidbody PhysicsBody => null;

            public int Holds => _holders.Count;

            public int Gazes { get; private set; }

            public int Placements { get; private set; }

            public void SetGazeTarget(object owner, Vector3 worldPosition, int priority)
            {
                Gazes++;
            }

            public void ClearGazeTarget(object owner)
            {
            }

            public void SetHoldStill(object owner, bool hold)
            {
                if (hold)
                {
                    _holders.Add(owner);
                }
                else
                {
                    _holders.Remove(owner);
                }
            }

            public void PlaceAt(Vector3 position, Quaternion rotation)
            {
                Placements++;
                Position = position;
                Rotation = rotation;
            }
        }

        private sealed class FakeTether : ITetherAim
        {
            public TetherAimState State { get; set; }

            public Vector3 TargetPosition => Vector3.zero;

            public float Strain => 0f;

            public float Length => 0f;
        }
    }
}
