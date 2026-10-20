using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The power module status lamp's dusk blink (<see cref="StatusLampBlink"/>): dark by day, waking over the tuned
    /// time at sunset, blinking softly within its lit share, asleep again at sunrise; a loaded night snaps awake.
    /// </summary>
    public sealed class StatusLampBlinkTests
    {
        private const float Frame = 1f / 60f;
        private const float Wake = 4f;
        private const float Blink = 3.2f;
        private const float OnShare = 0.3f;
        private const float Ease = 0.6f;

        [Test]
        public void ByDay_TheLampStaysDark()
        {
            var blink = new StatusLampBlink();
            foreach (SkyPhase phase in new[] { SkyPhase.Eclipse, SkyPhase.Sunrise, SkyPhase.Sunlit })
            {
                blink.OnSky(phase, false);
                Run(blink, 10f);
                Assert.AreEqual(0f, Peak(blink), $"dark in {phase}");
            }
        }

        [Test]
        public void AtDusk_TheBlinkWakesOverItsTime_AndSleepsAtSunrise()
        {
            var blink = new StatusLampBlink();
            blink.OnSky(SkyPhase.Sunset, false);
            Run(blink, Wake * 0.5f);
            Assert.AreEqual(0.5f, blink.Awake, 0.02f, "half awake halfway through its wake");
            Run(blink, Wake);
            Assert.AreEqual(1f, Peak(blink), 1e-3f, "fully lit at the top of a blink");

            blink.OnSky(SkyPhase.Earthlit, false);
            Run(blink, 1f);
            Assert.AreEqual(1f, blink.Awake, "still blinking through the night");

            blink.OnSky(SkyPhase.Sunrise, false);
            Run(blink, Wake + 1f);
            Assert.AreEqual(0f, Peak(blink));
        }

        [Test]
        public void ALoadedNight_SnapsTheBlinkAwake()
        {
            var blink = new StatusLampBlink();
            blink.OnSky(SkyPhase.Earthlit, true);
            Assert.AreEqual(1f, blink.Awake);
        }

        [Test]
        public void EachBlink_EasesUpAndDown_AndIsDarkOutsideItsLitShare()
        {
            Assert.AreEqual(0f, StatusLampBlink.Pulse(0f, Blink, OnShare, Ease), 1e-4f, "starts from dark");
            Assert.AreEqual(1f, StatusLampBlink.Pulse(Blink * OnShare * 0.5f, Blink, OnShare, Ease), 1e-4f);
            float easing = StatusLampBlink.Pulse(Blink * OnShare * Ease * 0.25f, Blink, OnShare, Ease);
            Assert.That(easing, Is.InRange(0.05f, 0.95f), "partway up its ease");
            for (float t = Blink * OnShare; t < Blink; t += 0.05f)
            {
                Assert.AreEqual(0f, StatusLampBlink.Pulse(t, Blink, OnShare, Ease), $"dark at {t:F2} s");
            }

            Assert.AreEqual(StatusLampBlink.Pulse(0.3f, Blink, OnShare, Ease),
                StatusLampBlink.Pulse(0.3f + Blink * 2f, Blink, OnShare, Ease), 1e-4f, "every blink alike");
        }

        private static void Run(StatusLampBlink blink, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                blink.Step(Frame, Wake);
            }
        }

        private static float Peak(StatusLampBlink blink)
        {
            return blink.Level(Blink * OnShare * 0.5f, Blink, OnShare, Ease);
        }
    }
}
