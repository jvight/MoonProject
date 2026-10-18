using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>The wake's light comes in as the picture opens, stays through the performance, and eases out on the way behind.</summary>
    public sealed class PostLightingTests
    {
        private const float Ramp = 2.5f;

        [Test]
        public void TheRim_ComesInAsThePictureOpens_HoldsThroughThePerformance_AndEasesOutBehind()
        {
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.Dark, 1f), 1e-5f, "no light in the black");
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Post, 0f), 1e-5f, "on as the picture opens on the face");
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
        public void TheFill_GrowsInAsThePictureOpensOnTheFace_AndFollowsTheRimOut()
        {
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.Dark, 1f, Ramp, Ramp), 1e-5f, "nothing lit in the black");
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.Post, 0f, 0f, Ramp), 1e-5f);
            float half = PostLighting.Fill(OpeningPhase.Post, 0.05f, Ramp * 0.5f, Ramp);
            Assert.That(half, Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(1f, PostLighting.Fill(OpeningPhase.Post, 0.5f, Ramp, Ramp), 1e-5f);
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.MoveBehind, 1f, Ramp, Ramp), 1e-5f);
        }
    }
}
