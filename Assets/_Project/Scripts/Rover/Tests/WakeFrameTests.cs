using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The first shot: Earth in the upper third, the lens in frame, then the lens face-on.</summary>
    public sealed class WakeFrameTests
    {
        private static readonly Vector3 EarthHigh = new Vector3(0.7f, 0.8f, 100f);
        private static readonly Vector3 LensLow = new Vector3(0.4f, 0.3f, 3f);

        [Test]
        public void FramedShots_BeatEveryMiss_AndPreferAFaceOnLens()
        {
            float faceOn = WakeFrame.Score(EarthHigh, LensLow, 0.9f);
            float profile = WakeFrame.Score(EarthHigh, LensLow, 0f);
            float earthLow = WakeFrame.Score(new Vector3(0.7f, 0.5f, 100f), LensLow, 1f);
            Assert.Greater(faceOn, profile);
            Assert.Greater(profile, earthLow, "Earth below the upper third misses, however well the lens faces.");
            Assert.IsTrue(WakeFrame.IsFramed(EarthHigh, LensLow));
        }

        [Test]
        public void TheModelledCamera_LooksAtItsAimPoint_RaisedByTheScreenOffset()
        {
            Pose level = WakeFrame.Camera(Vector3.zero, 90f, 0f, 5f, 0f, 0f, 50f);
            Assert.AreEqual(-5f, level.position.x, 1e-4f, "Bearing 90 looks east, from the west.");
            Vector3 centre = WakeFrame.Viewport(level, Vector3.zero, 50f, 16f / 9f);
            Assert.AreEqual(0.5f, centre.x, 1e-4f);
            Assert.AreEqual(0.5f, centre.y, 1e-4f);

            Pose raised = WakeFrame.Camera(Vector3.zero, 90f, 0f, 5f, 0f, 0.2f, 50f);
            Assert.AreEqual(0.3f, WakeFrame.Viewport(raised, Vector3.zero, 50f, 16f / 9f).y, 1e-3f,
                "The composer's screen offset (+ = lower) puts the aim point below the centre.");
        }

        [Test]
        public void EarthBehindTheCamera_OrLensOffScreen_Misses()
        {
            Assert.IsFalse(WakeFrame.IsFramed(new Vector3(0.7f, 0.8f, -100f), LensLow));
            Assert.IsFalse(WakeFrame.IsFramed(EarthHigh, new Vector3(1.2f, 0.3f, 3f)));
            float near = WakeFrame.Score(EarthHigh, new Vector3(0.95f, 0.3f, 3f), 0f);
            float far = WakeFrame.Score(EarthHigh, new Vector3(1.5f, 0.3f, 3f), 0f);
            Assert.Greater(near, far, "Closer misses score higher, so the search moves toward a framed shot.");
        }
    }
}
