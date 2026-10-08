using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The Rover Bay arm's analytic IK (shoulder frame: +Y up, zero yaw on +Z): it puts the tip's end exactly on a
    /// target with the tip straight down, bends the elbow back like the folded rest pose (such a pose solves back to
    /// itself), and refuses what is out of reach.
    /// </summary>
    public sealed class BayArmIkTests
    {
        private static readonly BayArmGeometry Arm = new BayArmGeometry(1.25f, 1.15f, 0.26f);

        [Test]
        public void Solve_PutsTheTipOnTheTarget_PointingStraightDown()
        {
            Vector3[] targets =
            {
                new Vector3(0f, -2.4f, 0.8f), new Vector3(0.6f, -2f, 1.1f), new Vector3(-0.9f, -1.5f, 0.4f),
                new Vector3(0.2f, -0.9f, -0.5f), new Vector3(0f, -2.5f, 0.3f),
            };

            foreach (Vector3 target in targets)
            {
                Assert.IsTrue(BayArmIk.TrySolve(Arm, target, out BayArmPose pose), target.ToString());
                Vector3 end = BayArmIk.EndOf(Arm, pose);
                Assert.Less(Vector3.Distance(target, end), 1e-4f, $"{target}: reached {end}");
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, pose.Upper + pose.Lower + pose.Tip), 1e-3f,
                    "The tip hangs straight down, so a held piece arrives level.");
            }
        }

        [Test]
        public void AnElbowBackPose_SolvesBackToItself()
        {
            var bent = new BayArmPose(25f, 40f, -100f, 60f);
            Vector3 end = BayArmIk.EndOf(Arm, bent);
            Assert.IsTrue(BayArmIk.TrySolve(Arm, end, out BayArmPose pose));
            Assert.AreEqual(bent.Yaw, pose.Yaw, 1e-2f);
            Assert.AreEqual(bent.Upper, pose.Upper, 1e-2f);
            Assert.AreEqual(bent.Lower, pose.Lower, 1e-2f);
            Assert.AreEqual(bent.Tip, pose.Tip, 1e-2f);
        }

        [Test]
        public void OutOfReach_IsRefused_WithNegativeSlack()
        {
            var far = new Vector3(0f, -3f, 1f);
            Assert.IsFalse(BayArmIk.TrySolve(Arm, far, out _));
            Assert.Less(BayArmIk.Slack(Arm, far), 0f);
            var near = new Vector3(0f, -1.8f, 0.5f);
            Assert.Greater(BayArmIk.Slack(Arm, near), 0f);
            Assert.IsFalse(BayArmIk.TrySolve(Arm, new Vector3(0f, -Arm.Tip, 0f), out _),
                "Right under the shoulder, closer than the links can fold.");
        }

        [Test]
        public void Blend_TakesTheShortWayRound()
        {
            var from = new BayArmPose(170f, 80f, -160f, 80f);
            var to = new BayArmPose(-170f, 20f, -40f, 20f);
            BayArmPose half = BayArmIk.Blend(from, to, 0.5f);
            Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(0f, half.Yaw)), 1e-3f, "Through 180, not through 0.");
            Assert.AreEqual(50f, half.Upper, 1e-3f);
        }
    }
}
