using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonProject.Autoplay.Tests
{
    public sealed class AutoplayLogicTests
    {
        [Test]
        public void Options_ReadTheScenarioAndItsOutputFolder()
        {
            bool asked = AutoplayOptions.TryParse(
                new[] { "Lunarion.exe", "-autoplay", "opening", "-autoplay-out", "C:/out", "-autoplay-until", "B3" },
                out AutoplayOptions options, out string problem);
            Assert.IsTrue(asked);
            Assert.IsNull(problem);
            Assert.AreEqual("opening", options.Scenario);
            Assert.AreEqual("C:/out", options.OutDirectory);
            Assert.AreEqual("B3", options.UntilBeat);
            Assert.AreEqual(AutoplayOptions.DefaultShotSeconds, options.ShotSeconds);
            Assert.AreEqual(AutoplayOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
            Assert.IsFalse(options.Mute);
        }

        [Test]
        public void Options_MuteFlagIsOffByDefault_AndOnWhenPassed()
        {
            Assert.IsTrue(AutoplayOptions.TryParse(
                new[] { "Lunarion.exe", "-autoplay", "opening", "-autoplay-out", "C:/out" },
                out AutoplayOptions unmuted, out string problem));
            Assert.IsNull(problem);
            Assert.IsFalse(unmuted.Mute);

            Assert.IsTrue(AutoplayOptions.TryParse(
                new[] { "Lunarion.exe", "-autoplay", "opening", "-autoplay-out", "C:/out", "-autoplay-mute" },
                out AutoplayOptions muted, out problem));
            Assert.IsNull(problem);
            Assert.IsTrue(muted.Mute);
        }

        [Test]
        public void Options_AreNotAskedForWithoutTheFlag_AndAreAProblemWithoutAnOutputFolder()
        {
            Assert.IsFalse(AutoplayOptions.TryParse(new[] { "Lunarion.exe" }, out AutoplayOptions none, out _));
            Assert.IsNull(none);
            Assert.IsTrue(AutoplayOptions.TryParse(new[] { "Lunarion.exe", "-autoplay", "opening" },
                out AutoplayOptions broken, out string problem));
            Assert.IsNull(broken);
            StringAssert.Contains(AutoplayOptions.OutFlag, problem);
            Assert.IsTrue(AutoplayOptions.TryParse(
                new[] { "-autoplay", "opening", "-autoplay-out", "o", "-autoplay-timeout", "-3" }, out _, out problem));
            StringAssert.Contains(AutoplayOptions.TimeoutFlag, problem);
        }

        [Test]
        public void Steering_TurnsOnTheSpotWhenTheTargetIsBehind_AndDrivesWhenItIsAhead()
        {
            var forward = Vector3.forward;
            DriveSteering.Command behind = DriveSteering.Decide(Vector3.zero, forward, new Vector3(0f, 0f, -30f), 2f);
            Assert.AreEqual(0f, behind.Throttle);
            Assert.AreNotEqual(0f, behind.Turn);
            DriveSteering.Command ahead = DriveSteering.Decide(Vector3.zero, forward, new Vector3(0f, 0f, 30f), 2f);
            Assert.AreEqual(1f, ahead.Throttle);
            Assert.AreEqual(0f, ahead.Turn);
            DriveSteering.Command near = DriveSteering.Decide(Vector3.zero, forward, new Vector3(0f, 0f, 5f), 2f);
            Assert.AreEqual(DriveSteering.CreepThrottle, near.Throttle);
            Assert.IsTrue(DriveSteering.Decide(Vector3.zero, forward, new Vector3(0f, 0f, 1f), 2f).Arrived);
        }

        [Test]
        public void Steering_TurnsRightForATargetOnTheRight()
        {
            DriveSteering.Command right = DriveSteering.Decide(Vector3.zero, Vector3.forward, new Vector3(20f, 0f, 20f), 2f);
            Assert.AreEqual(1f, right.Turn);
            DriveSteering.Command left = DriveSteering.Decide(Vector3.zero, Vector3.forward, new Vector3(-20f, 0f, 20f), 2f);
            Assert.AreEqual(-1f, left.Turn);
        }

        [Test]
        public void ThrottleGate_PressesForTheGivenShareOfTheFrames()
        {
            var gate = new ThrottleGate();
            int down = 0;
            for (int i = 0; i < 100; i++)
            {
                down += gate.Next(0.4f) ? 1 : 0;
            }

            Assert.AreEqual(40, down);
            Assert.IsFalse(gate.Next(0f));
        }

        [Test]
        public void LookSteering_StaysStillWithinTheToleranceAndClampsALongMove()
        {
            Assert.AreEqual(0f, LookSteering.Pixels(LookSteering.ToleranceDegrees - 0.1f));
            Assert.AreEqual(LookSteering.MaxPixels, LookSteering.Pixels(500f));
            Assert.AreEqual(-LookSteering.MaxPixels, LookSteering.Pixels(-500f));
        }

        [Test]
        public void GlyphKeys_ResolveKeyCaps()
        {
            Assert.IsTrue(GlyphKeys.TryResolve("E", out Key e));
            Assert.AreEqual(Key.E, e);
            Assert.IsTrue(GlyphKeys.TryResolve("Tab", out Key tab));
            Assert.AreEqual(Key.Tab, tab);
            Assert.IsFalse(GlyphKeys.TryResolve("LS", out _));
            Assert.IsFalse(GlyphKeys.TryResolve("", out _));
        }

        [Test]
        public void FrameTimeLog_KeepsOnlyTheSlowFramesWithTheirActivity()
        {
            var log = new FrameTimeLog();
            log.Record(1f, 16f, string.Empty);
            log.Record(2f, 80f, "driving to the lander");
            Assert.AreEqual(2, log.Frames);
            Assert.AreEqual(1, log.SlowCount);
            Assert.AreEqual(80f, log.WorstMilliseconds);
            Assert.AreEqual("driving to the lander", log.Slow[0].Activity);
        }

        [Test]
        public void BeatLog_PassesOnlyWhenEveryBeatDid_AndTheReportSaysWhy()
        {
            var beats = new BeatLog();
            Assert.IsFalse(beats.Passed, "no beats is not a pass");
            beats.Add(new BeatResult("B0", "Wake", true, "woke", 5f, "line", "E Scan", "Sunlit"));
            Assert.IsTrue(beats.Passed);
            beats.Add(new BeatResult("B2", "Drive", false, "stuck at the ridge", 30f, string.Empty, string.Empty, "Sunset"));
            Assert.IsFalse(beats.Passed);
            string report = beats.Render("opening", "build 1", 40f, new FrameTimeLog(), new[] { "screenshots/00-B0.png" }, string.Empty);
            StringAssert.Contains("**Result: FAIL**", report);
            StringAssert.Contains("stuck at the ridge", report);
            StringAssert.Contains("screenshots/00-B0.png", report);
        }

        [Test]
        public void ABeatTrip_CountsTheLowestCellsAndTheMetresDriven_AndTheReportListsThem()
        {
            var trip = new BeatTrip();
            trip.Begin(0.8f, new Vector3(0f, 5f, 0f));
            trip.Step(0.5f, new Vector3(3f, 9f, 4f));
            trip.Step(0.2f, new Vector3(3f, 0f, 4f));
            trip.Step(0.4f, new Vector3(3f, 0f, 14f));
            Assert.AreEqual(0.8f, trip.StartCharge, 1e-4f);
            Assert.AreEqual(0.2f, trip.MinCharge, 1e-4f);
            Assert.AreEqual(0.4f, trip.EndCharge, 1e-4f);
            Assert.AreEqual(15f, trip.Metres, 1e-3f, "5 m, then 0 m flat, then 10 m: height is not driven distance");

            var beats = new BeatLog();
            beats.Add(new BeatResult("R10", "Relay", true, "lit", 55f, string.Empty, string.Empty, "Earthlit", trip));
            string report = beats.Render("opening", "build 1", 60f, new FrameTimeLog(), new string[0], string.Empty);
            StringAssert.Contains("| R10 | 80 | 20 | 40 | 15 |", report);
        }
    }
}
