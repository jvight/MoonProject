using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's eye through a scan (0.5.5 §1.2): the lid lifts fully, the fan sweeps and swells, a pulse per
    /// quarter, the single flare at the end, and everything settles on letting go.</summary>
    public sealed class ScanLookTests
    {
        private const float Frame = 1f / 120f;

        private readonly ScanLookSettings _settings = new ScanLookSettings();

        private static void Run(ScanLook look, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                look.Step(Frame);
            }
        }

        [Test]
        public void AScan_LiftsTheLidFully_AndSweepsTheFan_Eased()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            look.Step(Frame);
            Assert.Less(look.Open, 0.2f, "The eye wakes, not snaps.");
            Run(look, 1f);
            Assert.Greater(look.Open, 0.95f);
            Assert.Greater(look.Fan, 0.95f);
            Assert.Greater(look.EyeGlow, 1f, "Awake while learning.");

            float least = float.PositiveInfinity;
            float most = float.NegativeInfinity;
            for (float t = 0f; t < _settings.SweepSeconds; t += Frame)
            {
                look.Step(Frame);
                least = Mathf.Min(least, look.Sweep);
                most = Mathf.Max(most, look.Sweep);
            }

            Assert.AreEqual(_settings.SweepDegrees, most, 0.2f, "The fan sweeps across the fragment...");
            Assert.AreEqual(-_settings.SweepDegrees, least, 0.2f, "...and back.");
        }

        [Test]
        public void EachQuarter_PulsesTheEye_AndTheFanSwellsTowardTheEnd()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 3f);
            float steady = look.EyeGlow;
            float early = look.Fan;
            look.Quarter(0.75f);
            look.Step(Frame);
            Assert.Greater(look.EyeGlow, steady, "A pulse.");
            Assert.Greater(look.Fan, early, "Brighter as the drawing nears complete.");
            Run(look, _settings.PulseSeconds + 0.05f);
            Assert.AreEqual(steady, look.EyeGlow, 1e-3f, "The pulse fades.");
        }

        [Test]
        public void LettingGo_FadesTheFan_AndSettlesTheLid_WithoutAFlare()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 1f);
            look.LetGo();
            float peak = 0f;
            for (float t = 0f; t < 1.5f; t += Frame)
            {
                look.Step(Frame);
                peak = Mathf.Max(peak, look.EyeGlow);
            }

            Assert.LessOrEqual(peak, _settings.EyeGlow + 1e-3f, "No flare on a let-go.");
            Assert.Less(look.Open, 0.01f);
            Assert.Less(look.Fan, 0.01f);
            Assert.IsFalse(look.IsScanning);
        }

        [Test]
        public void Learning_FlaresTheLensOnce_ForATenthOfASecond()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 1f);
            look.Learned();
            float peak = 0f;
            for (float t = 0f; t < _settings.FlareSeconds; t += Frame)
            {
                look.Step(Frame);
                peak = Mathf.Max(peak, look.EyeGlow);
            }

            Assert.Greater(peak, _settings.EyeGlow + 0.5f * _settings.Flare, "The lens flares.");
            look.Step(Frame);
            Assert.LessOrEqual(look.EyeGlow, _settings.EyeGlow, "Once, and briefly.");
            Run(look, 1.5f);
            Assert.Less(look.Open, 0.01f, "Then the eye settles back.");
        }
    }
}
