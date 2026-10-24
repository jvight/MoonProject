using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>The scan beam leaves along the lens axis and reaches its target only as the head aims at it.</summary>
    public sealed class ScanBeamReachTests
    {
        private const float Aligned = 20f;
        private const float Lost = 60f;
        private const float Short = 0.2f;
        private const float HalfLife = 0.1f;
        private const float Frame = 1f / 60f;

        private static readonly Vector3 Lens = new Vector3(0f, 1f, 0f);
        private static readonly Vector3 Contact = new Vector3(0f, 1f, 5f);

        private static Vector3 Settle(ScanBeamReach reach, Vector3 axis, float seconds)
        {
            Vector3 end = Vector3.zero;
            for (float t = 0f; t < seconds; t += Frame)
            {
                end = reach.End(Frame, Lens, axis, Contact, Aligned, Lost, Short, HalfLife);
            }

            return end;
        }

        [Test]
        public void AHeadAimedAtTheTarget_ReachesItExactly()
        {
            var reach = new ScanBeamReach();
            Vector3 end = Settle(reach, Vector3.forward, 3f);
            Assert.AreEqual(1f, reach.Aligned);
            Assert.AreEqual(0f, Vector3.Distance(Contact, end), 1e-5f);
        }

        [Test]
        public void AHeadTurnedAway_KeepsTheBeamShortAndAlongTheLensAxis()
        {
            var reach = new ScanBeamReach();
            Vector3 axis = Quaternion.Euler(0f, 90f, 0f) * Vector3.forward;
            Vector3 end = Settle(reach, axis, 3f);
            Vector3 beam = end - Lens;
            Assert.Less(Vector3.Angle(axis, beam), 1f, "the beam leaves the lens along its axis");
            Assert.AreEqual(5f * Short, beam.magnitude, 0.01f, "and runs only a short way");
        }

        [Test]
        public void TheHeadTurningOnto_TheTarget_BendsAndGrowsTheBeamWithoutAJump()
        {
            var reach = new ScanBeamReach();
            Settle(reach, Quaternion.Euler(0f, 90f, 0f) * Vector3.forward, 1f);
            Vector3 previous = reach.End(Frame, Lens, Quaternion.Euler(0f, 90f, 0f) * Vector3.forward, Contact, Aligned, Lost, Short, HalfLife);
            float widest = 0f;
            for (float yaw = 90f; yaw >= 0f; yaw -= 90f * Frame / 1.2f)
            {
                Vector3 axis = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Vector3 end = reach.End(Frame, Lens, axis, Contact, Aligned, Lost, Short, HalfLife);
                widest = Mathf.Max(widest, Vector3.Distance(previous, end));
                previous = end;
            }

            Assert.Less(widest, 0.35f, "the far end glides, one frame never moves it more than a third of a metre");
            Vector3 settled = Settle(reach, Vector3.forward, 2f);
            Assert.AreEqual(0f, Vector3.Distance(Contact, settled), 1e-4f, "and it ends on the target");
        }

        [Test]
        public void ANewBeam_StartsClosedAtTheLens()
        {
            var reach = new ScanBeamReach();
            Settle(reach, Vector3.forward, 2f);
            reach.Reset();
            Vector3 end = reach.End(Frame, Lens, Quaternion.Euler(0f, 60f, 0f) * Vector3.forward, Contact, Aligned, Lost, Short, HalfLife);
            Assert.AreEqual(5f * Short, (end - Lens).magnitude, 0.05f);
        }
    }
}
