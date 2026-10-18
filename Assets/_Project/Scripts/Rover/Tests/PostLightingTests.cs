using NUnit.Framework;

namespace MoonProject.Rover.Tests
{
    /// <summary>The self-test's light comes in with the pull-out, stays through the checks, and is gone by the chase frame.</summary>
    public sealed class PostLightingTests
    {
        private const float Ramp = 2.5f;

        [Test]
        public void TheRim_ComesInOverThePullOut_HoldsThroughTheChecks_AndGoesOutInTheChaseEase()
        {
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.Hunt, 1f), 1e-5f, "no light before the pull-out");
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.PullOut, 0f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.PullOut, 1f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.Post, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.OverShoulder, 0.5f), 1e-5f);
            Assert.AreEqual(1f, PostLighting.Rim(OpeningPhase.ChaseEase, 0f), 1e-5f);
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.ChaseEase, 1f), 1e-5f);
            Assert.AreEqual(0f, PostLighting.Rim(OpeningPhase.Idle, 0f), 1e-5f);
        }

        [Test]
        public void TheFill_GrowsInWithTheChecks_AndFollowsTheRimOut()
        {
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.PullOut, 1f, 0f, Ramp), 1e-5f, "the screen is not lit yet");
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.Post, 0.1f, 0f, Ramp), 1e-5f);
            float half = PostLighting.Fill(OpeningPhase.Post, 0.1f, Ramp * 0.5f, Ramp);
            Assert.That(half, Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(1f, PostLighting.Fill(OpeningPhase.Post, 0.5f, Ramp, Ramp), 1e-5f);
            Assert.AreEqual(0f, PostLighting.Fill(OpeningPhase.ChaseEase, 1f, Ramp, Ramp), 1e-5f);
        }
    }
}
