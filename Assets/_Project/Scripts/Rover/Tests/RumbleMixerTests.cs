using NUnit.Framework;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>Gamepad rumble: two voices mixed by max, the player's strength, the 0.35 cap, off, and fades.</summary>
    public sealed class RumbleMixerTests
    {
        private const float Frame = 1f / 60f;
        private const float Tolerance = 1e-4f;

        private static readonly RumbleProfile Soft = new RumbleProfile(0.1f, 0.1f, 0.05f, 0.05f, 1f, false);
        private static readonly RumbleProfile Strong = new RumbleProfile(0.3f, 0.3f, 0.02f, 0.02f, 1f, false);
        private static readonly RumbleProfile Hard = new RumbleProfile(0.9f, 0.9f, 0.9f, 0.9f, 1f, false);
        private static readonly RumbleProfile Held = new RumbleProfile(0f, 0.08f, 0f, 0.08f, 0.5f, true);

        [Test]
        public void TwoProfiles_MixByMax_PerMotor()
        {
            var mixer = new RumbleMixer();
            mixer.Play(Soft);
            mixer.Play(Strong);
            mixer.Step(Frame);
            Assert.AreEqual(0.3f, mixer.Low, Tolerance, "Low takes the stronger profile's level.");
            Assert.AreEqual(0.05f, mixer.High, Tolerance, "High takes the other profile's, not a sum.");
        }

        [Test]
        public void AThirdProfile_ReplacesOne_NeverThreeAtOnce()
        {
            var mixer = new RumbleMixer();
            mixer.Play(Hard);
            mixer.Play(Soft);
            mixer.Step(0.5f);
            mixer.Play(Strong);
            mixer.Step(Frame);
            Assert.AreEqual(0.3f, mixer.Low, Tolerance, "The oldest-ending voice was replaced.");
        }

        [Test]
        public void Everything_IsCappedAt035()
        {
            var mixer = new RumbleMixer();
            mixer.Play(Hard, 2f);
            mixer.Step(Frame);
            Assert.AreEqual(RumbleMixer.HardCap, mixer.Low, Tolerance);
            Assert.AreEqual(RumbleMixer.HardCap, mixer.High, Tolerance);
        }

        [Test]
        public void Strength_ScalesBelowTheCap()
        {
            var mixer = new RumbleMixer { Strength = 0.5f };
            mixer.Play(Strong);
            mixer.Step(Frame);
            Assert.AreEqual(0.15f, mixer.Low, Tolerance);
        }

        [Test]
        public void Disabled_IsSilent_AndBackOnWhenEnabled()
        {
            var mixer = new RumbleMixer { Enabled = false };
            mixer.Play(Strong);
            mixer.Step(Frame);
            Assert.AreEqual(0f, mixer.Low);
            mixer.Enabled = true;
            mixer.Step(Frame);
            Assert.AreEqual(0.3f, mixer.Low, Tolerance);
        }

        [Test]
        public void ATimedProfile_Ends_AHeldOne_Holds_UntilFadedOut()
        {
            var mixer = new RumbleMixer();
            mixer.Play(Soft);
            mixer.Play(Held);
            mixer.Step(1.2f);
            Assert.AreEqual(0.08f, mixer.Low, Tolerance, "The soft one ended; the held one sits at its end level.");
            mixer.FadeOut(0.15f);
            mixer.Step(0.1f);
            Assert.Greater(mixer.Low, 0f, "Fading, not cut.");
            mixer.Step(0.1f);
            Assert.AreEqual(0f, mixer.Low);
            Assert.IsFalse(mixer.IsPlaying);
        }

        [Test]
        public void AHeldVoice_FollowsItsGain()
        {
            var mixer = new RumbleMixer();
            int voice = mixer.Play(Held, 1f, 0f);
            mixer.SetGain(voice, 2f);
            mixer.Step(Frame);
            Assert.AreEqual(0.16f, mixer.Low, Tolerance);
        }
    }
}
