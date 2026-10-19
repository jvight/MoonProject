using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The wake's light comes in as the camera settles onto 07's face, stays through the performance, and eases out on
    /// the way behind; the screen's own fill grows from the moment the tube wakes.
    /// </summary>
    public sealed class PostLightingTests
    {
        private const float Ramp = 2.5f;

        [Test]
        public void TheRim_ComesInAsTheCameraSettlesOnTheFace_HoldsThroughThePerformance_AndEasesOutBehind()
        {
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.FadeOut, 1f), 1e-5f, "no light under the cover");
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.PushIn, 0f), 1e-5f, "the title's own light at first");
            Assert.That(PostLighting.Rim(OpeningPhase.PushIn, 0.5f), Is.InRange(0.3f, 0.7f), "eased in, no pop");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.PushIn, 1f), 1e-5f, "in by the face");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Dark, 1f), 1e-5f, "after the reload, in as the cover opens");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.PowerOn, 0f), 1e-5f, "on as the tube wakes");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Logo, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Post, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Ready, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.PullBack, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Perform, 0.5f), 1e-5f, "the performance reads in its light");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.MoveBehind, 0f), 1e-5f);
            Assert.That(PostLighting.Rim(OpeningPhase.MoveBehind, 0.5f), Is.InRange(0.3f, 0.7f), "eased out, no pop");
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.MoveBehind, 1f), 1e-5f, "gone by the chase frame");
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.Idle, 0f), 1e-5f);
        }

        [Test]
        public void TheFill_GrowsInFromTheTubeWaking_AndFollowsTheRimOut()
        {
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.FadeOut, 1f, 0f, Ramp), 1e-5f, "nothing lit under the cover");
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.PushIn, 1f, 0f, Ramp), 1e-5f, "the tube is still dark");
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.PowerOn, 0f, 0f, Ramp), 1e-5f);
            float half = PostLighting.Fill(OpeningPhase.Logo, 0.3f, Ramp * 0.5f, Ramp);
            Assert.That(half, Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(1f, PostLighting.Fill(OpeningPhase.Post, 0.5f, Ramp, Ramp), 1e-5f);
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.MoveBehind, 1f, Ramp, Ramp), 1e-5f);
        }
    }
}
