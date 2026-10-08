using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The relay network's pure logic (docs/features/M3-06): the station's reach over the real chain, the scrap cost,
    /// where a mast's part lies, the restoration timeline and the mast rig.
    /// </summary>
    public sealed class RelayTests
    {
        // The real basin's relay pads (World seed, M3-06 world) relative to the base pad at the origin.
        private static readonly Vector3 Relay0 = new Vector3(62.63f, 1.68f, 92.85f);
        private static readonly Vector3 Relay1 = new Vector3(-204.85f, 7.97f, -74.56f);
        private static readonly Vector3 Relay2 = new Vector3(268.1f, 5.74f, 74.49f);
        private static readonly Vector3 Relay3 = new Vector3(456.57f, 36.35f, -16.66f);
        private static readonly string[] Ids = { "relay.0", "relay.1", "relay.2", "relay.3" };

        // The radio tower's clear-signal radius before any level and at level 1 (the content builder's tower).
        private const float DarkTower = 60f;
        private const float TowerLevel1 = 110f;

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
        public void Cost_Escalates()
        {
            var relays = Create<RelayTuning>();
            Assert.AreEqual(60, relays.CostAfter(0));
            Assert.AreEqual(90, relays.CostAfter(1));
            Assert.AreEqual(120, relays.CostAfter(2));
            Assert.AreEqual(150, relays.CostAfter(3));
            Assert.AreEqual(420, relays.TotalCost(Ids.Length));
            Assert.IsNull(relays.Validate(Ids.Length));
            StringAssert.Contains("prices", relays.Validate(Ids.Length + 1));

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
            return new StationReach(Vector3.zero, homeRadius, Ids, new[] { Relay0, Relay1, Relay2, Relay3 },
                tuning.MastReach);
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
    }
}
