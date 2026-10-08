using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The friends framework: progress and saves, placement, the repair timeline, flight and behaviour.
    /// </summary>
    public sealed class FriendTests
    {
        private readonly List<Object> _created = new List<Object>();
        private FriendTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<FriendTuning>();
            _created.Add(_tuning);
        }

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
        public void Progress_GoesDormantPartsGatheringRepairingAwake_AndNeverBack()
        {
            var tilly = new FriendProgress("tilly", 3);
            Assert.AreEqual(FriendState.Dormant, tilly.State);
            Assert.IsTrue(tilly.AnswersSonar);
            Assert.IsTrue(tilly.CollectPart(1));
            Assert.AreEqual(FriendState.PartsGathering, tilly.State);
            Assert.AreEqual(1, tilly.Collected);
            Assert.IsFalse(tilly.CollectPart(1), "a part is gathered once");
            Assert.IsFalse(tilly.CanRepair, "no repair before every part is home");
            Assert.Throws<InvalidOperationException>(tilly.BeginRepair);
            tilly.CollectPart(0);
            tilly.CollectPart(2);
            Assert.IsTrue(tilly.AllPartsGathered);
            Assert.IsTrue(tilly.CanRepair);
            tilly.BeginRepair();
            Assert.AreEqual(FriendState.Repairing, tilly.State);
            Assert.IsFalse(tilly.AnswersSonar);
            tilly.FinishRepair();
            Assert.AreEqual(FriendState.Awake, tilly.State);
            Assert.IsFalse(tilly.CollectPart(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => tilly.CollectPart(3));
        }

        [Test]
        public void Progress_RoundTripsThroughJson_AndAMidRepairSaveComesBackAwake()
        {
            var tilly = new FriendProgress("tilly", 3);
            tilly.CollectPart(0);
            tilly.CollectPart(2);
            tilly.MarkDiscovered();
            var json = JsonUtility.ToJson(new FriendsSaveData { friends = new[] { tilly.Capture() } });
            FriendsSaveData copy = JsonUtility.FromJson<FriendsSaveData>(json);
            var loaded = new FriendProgress("tilly", 3);
            loaded.Restore(copy.friends[0]);
            Assert.AreEqual(FriendState.PartsGathering, loaded.State);
            Assert.IsTrue(loaded.IsCollected(0));
            Assert.IsFalse(loaded.IsCollected(1));
            Assert.IsTrue(loaded.IsCollected(2));
            Assert.IsTrue(loaded.Discovered);

            var midRepair = new FriendProgress("tilly", 3);
            midRepair.Restore(new FriendSaveData { id = "tilly", state = (int)FriendState.Repairing, parts = 7 });
            Assert.AreEqual(FriendState.Awake, midRepair.State, "a repair can only end one way: it is finished");

            var inconsistent = new FriendProgress("tilly", 3);
            inconsistent.Restore(new FriendSaveData { id = "tilly", state = (int)FriendState.Dormant, parts = 0xFF });
            Assert.AreEqual(3, inconsistent.Collected, "foreign bits are ignored");
            Assert.AreEqual(FriendState.PartsGathering, inconsistent.State, "the state follows the parts");
        }

        [Test]
        public void Site_LiesInTheShallowCraterEast_InViewFromTheBaseEdge()
        {
            TestWorld world = TestWorld.WithCrater();
            FriendPlacement placement = Tilly();
            FriendSite site = FriendSitePlanner.Plan(world, world, placement, _tuning, Array.Empty<RelicSite>(), 3);
            Assert.IsTrue(site.InCrater, "in the shallow crater");
            Assert.IsTrue(site.Visible, "in view from the base edge");
            Assert.Less(SurfaceRules.HorizontalDistance(site.Position, TestWorld.CraterCentre), 10f);
            float distance = SurfaceRules.HorizontalDistance(site.Position, world.BasePosition);
            Assert.That(distance, Is.InRange(60f, 110f));
            Assert.AreEqual(world.SampleHeight(site.Position.x, site.Position.z), site.Position.y, 1e-4f);

            FriendSite again = FriendSitePlanner.Plan(world, world, placement, _tuning, Array.Empty<RelicSite>(), 3);
            Assert.AreEqual(site.Position, again.Position, "deterministic");
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(site.Parts[i], again.Parts[i]);
            }
        }

        [Test]
        public void Parts_AreSpreadAroundTheSite_OnGentleDrivableFloor()
        {
            TestWorld world = TestWorld.WithCrater();
            var relics = new[] { new RelicSite(new Vector3(85f, 0f, -40f), Vector3.up) };
            FriendSite site = FriendSitePlanner.Plan(world, world, Tilly(), _tuning, relics, 3);
            float minNormalY = SurfaceRules.MinNormalY(_tuning.PartMaxSlope);
            for (int i = 0; i < site.Parts.Length; i++)
            {
                Vector3 part = site.Parts[i];
                Assert.That(SurfaceRules.HorizontalDistance(part, site.Position), Is.InRange(30f - 1e-3f, 60f + 1e-3f));
                Assert.IsTrue(world.IsDrivable(part.x, part.z));
                Assert.GreaterOrEqual(world.SampleNormal(part.x, part.z).y, minNormalY);
                Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(part, relics[0].Position),
                    _tuning.PartClearance);
                for (int j = 0; j < i; j++)
                {
                    Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(part, site.Parts[j]),
                        _tuning.PartSpacing * 0.6f - 1e-3f);
                }
            }
        }

        [Test]
        public void Site_ReportsWhenNoCraterIsFound()
        {
            TestWorld flat = TestWorld.Flat();
            FriendSite site = FriendSitePlanner.Plan(flat, flat, Tilly(), _tuning, Array.Empty<RelicSite>(), 3);
            Assert.IsFalse(site.InCrater, "a flat world has no crater: the best spot is taken and the caller warned");
            Assert.IsTrue(site.Visible);
        }

        [Test]
        public void Repair_StitchesThenFlickersSpinsLiftsAndLooks_InOrder()
        {
            var repair = new RepairSequence(3.5f, 1f, 1.2f, 1.8f, 0.9f);
            Assert.IsTrue(repair.Stitching(1f));
            Assert.AreEqual(0f, repair.Eye(3f), "dark while stitching");
            Assert.AreEqual(0f, repair.Rotors(3f));
            Assert.AreEqual(1f, repair.Eye(3.5f + 1f), "the eye is steady after its flicker");
            Assert.AreEqual(3.5f + 1.2f, repair.LiftStart, 1e-5f, "lift once the eye is on and the rotors are up");
            Assert.AreEqual(0f, repair.Lift(repair.LiftStart), 1e-5f);
            Assert.AreEqual(1f, repair.Lift(repair.LookStart), 1e-5f);
            Assert.AreEqual(1f, repair.Look(repair.Duration), 1e-5f);
            Assert.IsTrue(repair.Done(repair.Duration));
            Assert.IsFalse(repair.Done(repair.Duration - 0.01f));
            bool flickered = false;
            for (float t = 3.5f; t < 4.5f; t += 0.01f)
            {
                flickered |= repair.Eye(t) == 0f && t > 3.6f;
            }

            Assert.IsTrue(flickered, "an old circuit catching: it goes dark again before it holds");
        }

        [Test]
        public void CameraClearance_PushesAFriendOutOfTheViewOf07()
        {
            var camera = new Vector3(0f, 3f, -8f);
            var rover = new Vector3(0f, 0.8f, 0f);
            Vector3 between = Vector3.Lerp(camera, rover, 0.5f);
            Vector3 pushed = CameraClearance.Apply(between, rover, camera, 12f);
            Assert.GreaterOrEqual(CameraClearance.AngleOff(pushed, rover, camera), 12f - 1e-3f);
            var aside = new Vector3(3f, 3f, -4f);
            Assert.AreEqual(aside, CameraClearance.Apply(aside, rover, camera, 12f), "already clear: untouched");
            var behindCamera = new Vector3(0f, 3f, -12f);
            Assert.AreEqual(behindCamera, CameraClearance.Apply(behindCamera, rover, camera, 12f));
        }

        [Test]
        public void Motion_EasesWithinItsSpeed_AndNeverDipsBelowTheFloor()
        {
            var motion = new FriendMotion();
            motion.Teleport(Vector3.zero, 0f);
            float fastest = 0f;
            for (int i = 0; i < 200; i++)
            {
                motion.Step(new Vector3(0f, -5f, 40f), new Vector3(0f, 0f, 50f), 6f, 1f, _tuning, 0.02f);
                fastest = Mathf.Max(fastest, motion.Velocity.magnitude);
                Assert.GreaterOrEqual(motion.Position.y, 1f - 1e-4f);
            }

            Assert.LessOrEqual(fastest, 6f + 1e-3f);
            Assert.Greater(motion.Position.z, 10f);
        }

        [Test]
        public void Behaviour_FollowsWhen07Leaves_GreetsOnReturn_AndSettlesAtHome()
        {
            TestWorld world = TestWorld.Flat();
            var spots = new FakeSpots();
            var brain = new FriendBehaviour(_tuning, world, spots, 3);
            FriendSenses senses = Senses(new Vector3(0f, 0f, 90f), 0f);
            brain.Wake(senses);
            Assert.AreEqual(FriendBehaviour.Mode.Following, brain.Current, "just repaired far away: it follows 07");
            FriendIntent intent = brain.Step(senses, 0.02f);
            Assert.Greater(intent.Target.y, senses.Rover.y + 1f, "above 07");
            Assert.GreaterOrEqual(CameraClearance.AngleOff(intent.Target, senses.Rover + Vector3.up * 0.8f,
                senses.Camera), _tuning.CameraClearAngle - 1e-2f, "never between the camera and 07");

            senses = Senses(new Vector3(0f, 0f, 10f), 1f);
            intent = brain.Step(senses, 0.02f);
            Assert.IsTrue(intent.Greeted, "home from away: a greeting");
            Assert.AreEqual(FriendBehaviour.Mode.Greeting, brain.Current);
            senses = Senses(new Vector3(0f, 0f, 10f), 1f + _tuning.GreetDuration + 0.1f);
            brain.Step(senses, 0.02f);
            Assert.AreEqual(FriendBehaviour.Mode.Perched, brain.Current, "then home to its perch");
            senses.Position = senses.Perch;
            intent = brain.Step(senses, 0.02f);
            Assert.AreEqual(0f, intent.Rotors, "napping on the perch");

            senses = Senses(new Vector3(0f, 0f, 48f), 5f);
            brain.Step(senses, 0.02f);
            Assert.AreEqual(FriendBehaviour.Mode.Following, brain.Current, "07 leaves: it comes along");
            senses = Senses(new Vector3(0f, 0f, 20f), 6f);
            intent = brain.Step(senses, 0.02f);
            Assert.IsFalse(intent.Greeted, "a short hop out (never far away) earns no greeting");
        }

        [Test]
        public void Behaviour_HomeLife_NapsFlitsAndInspectsTheShelf()
        {
            TestWorld world = TestWorld.Flat();
            var brain = new FriendBehaviour(_tuning, world, new FakeSpots(), 5);
            FriendSenses senses = Senses(new Vector3(0f, 0f, 5f), 0f);
            brain.Settle(senses);
            var seen = new HashSet<FriendBehaviour.Mode>();
            for (float t = 0f; t < 60f; t += 0.5f)
            {
                senses = Senses(new Vector3(0f, 0f, 5f), t);
                brain.Step(senses, 0.5f);
                seen.Add(brain.Current);
            }

            Assert.IsTrue(seen.Contains(FriendBehaviour.Mode.Perched));
            Assert.IsTrue(seen.Contains(FriendBehaviour.Mode.Flitting));
            Assert.IsTrue(seen.Contains(FriendBehaviour.Mode.Inspecting));
        }

        [Test]
        public void Behaviour_SpotsSomethingNearby_HoversAndPingsOnce()
        {
            TestWorld world = TestWorld.Flat();
            var spots = new FakeSpots { Next = new SpotTarget(SpotKind.Site, 2, -1, new Vector3(20f, 0f, 90f)) };
            var brain = new FriendBehaviour(_tuning, world, spots, 7);
            FriendSenses senses = Senses(new Vector3(0f, 0f, 90f), 0f);
            brain.Wake(senses);
            brain.Step(senses, 0.02f);
            Assert.AreEqual(FriendBehaviour.Mode.Spotting, brain.Current);
            Assert.AreEqual(_tuning.SpotRadius, spots.AskedRadius, "it looks within its spotting radius of 07");

            senses.Position = spots.Next.Position + Vector3.up * _tuning.SpotHover;
            FriendIntent intent = brain.Step(senses, 0.02f);
            Assert.IsTrue(intent.Spotted);
            Assert.AreEqual(2, intent.Spot.Index);
            senses.Now = 1f;
            intent = brain.Step(senses, 0.02f);
            Assert.IsFalse(intent.Spotted, "one soft ping per spot");
            Assert.Greater(intent.Cone, 0f, "its little light shines while it hovers");
            senses.Now = 1f + _tuning.SpotDuration;
            brain.Step(senses, 0.02f);
            Assert.AreEqual(FriendBehaviour.Mode.Following, brain.Current, "then back to 07");
        }

        private static FriendSenses Senses(Vector3 rover, float now)
        {
            return new FriendSenses
            {
                Now = now,
                Position = rover + new Vector3(2f, 3f, -4f),
                Rover = rover,
                RoverForward = Vector3.forward,
                Camera = rover + new Vector3(0f, 3.4f, -7.5f),
                Home = new Vector3(-10f, 0f, 8f),
                Perch = new Vector3(-10f, 3.3f, 8f),
                PerchForward = Vector3.right,
                Shelf = new Vector3(-12f, 0f, 3f),
                ShelfForward = Vector3.right,
            };
        }

        private static FriendPlacement Tilly()
        {
            return new FriendPlacement(41, new Vector2(60f, 110f), 90f, 55f, new Vector2(0.3f, 2.5f), 10f, true,
                new Vector2(30f, 60f));
        }

        private sealed class FakeSpots : ISpotTargets
        {
            public SpotTarget Next { get; set; }

            public float AskedRadius { get; private set; }

            public bool TryFind(Vector3 around, float radius, out SpotTarget target)
            {
                AskedRadius = radius;
                target = Next;
                return Next.Position != Vector3.zero;
            }

            public void MarkSpotted(SpotTarget target)
            {
            }
        }
    }
}
