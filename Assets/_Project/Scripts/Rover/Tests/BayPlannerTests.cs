using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The Rover Bay's arms as Rover reads and drives them (<see cref="BayArm"/>) and the plan that picks a turntable
    /// turn and an arm per part (<see cref="BayPlanner"/>), on a bare contract bay (<see cref="StandInBay"/>).
    /// </summary>
    public sealed class BayPlannerTests
    {
        private const float Upper = 1.25f;
        private const float Lower = 1.15f;
        private const float Tip = 0.26f;
        private const float Hover = 0.4f;
        private const float Align = 0.25f;

        /// <summary>07 parks facing -Z: the bay's open front, where the camera looks in from, is +Z.</summary>
        private static readonly Vector3 Front = Vector3.forward;

        private StandInBay _bay;
        private BayArm[] _arms;

        [SetUp]
        public void SetUp()
        {
            _bay = new StandInBay(Upper, Lower, Tip);
            _arms = new BayArm[_bay.ArmCount];
            for (int i = 0; i < _arms.Length; i++)
            {
                _arms[i] = BayArm.Read(_bay, i, out string problem);
                Assert.IsNotNull(_arms[i], problem);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _bay.Dispose();
        }

        [Test]
        public void Arms_AreReadFromTheContractJoints()
        {
            BayArm arm = _arms[1];
            Assert.AreEqual(Upper, arm.Geometry.Upper, 1e-5f);
            Assert.AreEqual(Lower, arm.Geometry.Lower, 1e-5f);
            Assert.AreEqual(Tip, arm.Geometry.Tip, 1e-5f);
            Assert.AreEqual(0f, arm.Rest.Yaw);
            Assert.AreEqual(80f, arm.Rest.Upper, 1e-3f);
            Assert.AreEqual(-160f, arm.Rest.Lower, 1e-3f);
            Assert.AreEqual(80f, arm.Rest.Tip, 1e-3f);
            Assert.Less(Vector3.Distance(_bay.GetArmJoint(1, RoverBayJoint.SparkSocket).position, arm.TipEnd), 1e-4f);
        }

        [Test]
        public void ABrokenContract_IsReported_NeverRead()
        {
            _bay.GetArmJoint(0, RoverBayJoint.Upper).localPosition = new Vector3(0f, 0.1f, 0f);
            Assert.IsNull(BayArm.Read(_bay, 0, out string offset));
            StringAssert.Contains("do not hang", offset);

            _bay.Remove(2, RoverBayJoint.Yaw);
            Assert.IsNull(BayArm.Read(_bay, 2, out string missing));
            StringAssert.Contains("lacks a joint", missing);
        }

        [Test]
        public void ASolvedPose_PutsTheArmsTipOnTheTarget()
        {
            var targets = new[] { new Vector3(-0.5f, 0.9f, 0.2f), new Vector3(-0.2f, 1.4f, -0.6f) };
            foreach (Vector3 target in targets)
            {
                BayArm arm = _arms[0];
                Assert.IsTrue(BayArmIk.TrySolve(arm.Geometry, arm.ToShoulder(target), out BayArmPose pose));
                arm.Apply(pose);
                Assert.Less(Vector3.Distance(target, arm.TipEnd), 1e-4f, target.ToString());
            }
        }

        [Test]
        public void TwoParts_GetAnArmEach()
        {
            Vector3[] drums = { new Vector3(-0.47f, 1.07f, 0.1f), new Vector3(0.47f, 1.07f, 0.1f) };
            int[] armOf = new int[2];
            Assert.IsTrue(BayPlanner.TryPlan(_arms, Vector3.zero, Vector3.up, Front, drums, 2, Hover, armOf,
                out float turn));
            Assert.AreEqual(0f, turn, "Both in reach and in view as 07 parked.");
            Assert.AreNotEqual(armOf[0], armOf[1], "One arm per drum.");
            Assert.AreEqual(0, armOf[0], "Each from its own side.");
            Assert.AreEqual(2, armOf[1]);
        }

        [Test]
        public void ASocketFacingTheOpenFront_IsTurnedToASideArm_StillInView()
        {
            Vector3[] rack = { new Vector3(0f, 0.8f, 1.2f) };
            int[] armOf = new int[1];
            Assert.IsTrue(BayPlanner.TryPlan(_arms, Vector3.zero, Vector3.up, Front, rack, 1, Hover, armOf,
                out float turn));
            Assert.AreEqual(90f, turn, "A quarter turn brings it under a side arm, seen side-on from the front...");
            Assert.AreEqual(2, armOf[0]);
        }

        [Test]
        public void ASocketDeepInTheBay_IsTurnedToTheSide_WhereTheFrontSeesIt()
        {
            Vector3[] lampBar = { new Vector3(0f, 0.8f, -1f) };
            int[] armOf = new int[1];
            Assert.IsTrue(BayPlanner.TryPlan(_arms, Vector3.zero, Vector3.up, Front, lampBar, 1, Hover, armOf,
                out float turn));
            Assert.AreEqual(90f, turn, "The back arm could reach it, hidden behind 07; a side arm shows it.");
            Assert.AreEqual(0, armOf[0]);
        }

        [Test]
        public void ASocketNoArmReaches_IsRefused()
        {
            Vector3[] low = { new Vector3(0f, 0.2f, 1.8f) };
            Assert.IsFalse(BayPlanner.TryPlan(_arms, Vector3.zero, Vector3.up, Front, low, 1, Hover, new int[1],
                out _));
        }

        [Test]
        public void ABellySocket_IsReachedByTheFloorArmRising()
        {
            Assert.IsTrue(BayPlanner.TryPlanFloor(_bay.FloorLift, _bay.FloorTip, Vector3.zero, Vector3.up,
                new Vector3(0.03f, 0.28f, 0f), Align, out float turn, out float rise));
            Assert.AreEqual(0f, turn);
            Assert.AreEqual(0.28f + StandInBay.FloorTipDepth, rise, 1e-4f);
            Assert.IsFalse(BayPlanner.TryPlanFloor(_bay.FloorLift, _bay.FloorTip, Vector3.zero, Vector3.up,
                new Vector3(0.6f, 0.28f, 0.6f), Align, out _, out _), "Never over the lift, however it turns.");
        }
    }
}
