using NUnit.Framework;
using UnityEngine;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The crew's belly lift (docs/features/M3-19 §4): jammed with home's power until 07 comes near, shudders to show
    /// it is stuck and asks for the stitch with no carrying condition, stitched once, then ridden up into the lab and
    /// down again at rest.
    /// </summary>
    public sealed class LiftCycleTests
    {
        private const float Frame = 1f / 60f;

        private LiftTuning _tuning;
        private LiftCycle _lift;
        private float _still;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<LiftTuning>();
            _lift = new LiftCycle(_tuning);
            _lift.Restore(false);
            _still = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        /// <summary>Steps for <paramref name="seconds"/>; returns the first step other than None (or None).</summary>
        private LiftStep Run(float seconds, bool powered = true, bool nearHome = false, bool atStitchSpot = false,
            bool onPlatform = false, bool down = false, bool moving = false)
        {
            LiftStep first = LiftStep.None;
            for (float t = 0f; t < seconds; t += Frame)
            {
                _still = moving ? 0f : _still + Frame;
                var input = new LiftInput(powered, nearHome, atStitchSpot, onPlatform, _still, down);
                LiftStep step = _lift.Step(input, Frame);
                first = first == LiftStep.None ? step : first;
            }

            return first;
        }

        private void Restore()
        {
            Run(Frame);
            Run(_tuning.JamCueSeconds + Frame, nearHome: true);
            _still = 0f;
            Run(_tuning.StitchDelay + Frame, nearHome: true, atStitchSpot: true);
            Run(_tuning.StitchSeconds, nearHome: true, atStitchSpot: true);
        }

        [Test]
        public void WithoutHomesPower_StaysDormant_EvenNearHomeOnThePlatform()
        {
            Assert.AreEqual(LiftStep.None, Run(5f, false, true, true, true));
            Assert.AreEqual(LiftPhase.Dormant, _lift.Phase);
        }

        [Test]
        public void HomesPower_WakesItJammed_ShuddersWhen07IsNear_AndThenAsksForTheStitch_EmptyHanded()
        {
            Assert.AreEqual(LiftStep.Powered, Run(Frame));
            Assert.AreEqual(LiftStep.None, Run(5f, nearHome: false), "Far from home: still jammed and waiting.");
            Assert.AreEqual(LiftPhase.Jammed, _lift.Phase);
            Assert.AreEqual(0f, _lift.Shudder);

            Assert.AreEqual(LiftStep.None, Run(0.5f * _tuning.JamCueSeconds, nearHome: true),
                "07 is near: the jammed platform shudders first before asking for the stitch.");
            Assert.AreEqual(LiftPhase.Jammed, _lift.Phase);
            Assert.Greater(_lift.Shudder, 0f, "The platform shudders visibly during the jam cue.");

            Assert.AreEqual(LiftStep.Called, Run(_tuning.JamCueSeconds, nearHome: true));
            Assert.AreEqual(LiftPhase.Called, _lift.Phase);
            Assert.AreEqual(0f, _lift.Shudder, "The shudder stops once the jam cue finishes.");
            Assert.AreEqual(LiftStep.None, Run(5f), "Called, it stays so after 07 drives off.");
            Assert.AreEqual(LiftPhase.Called, _lift.Phase);
        }

        [Test]
        public void TheStitch_StartsOnceRestedByThePlatform_EasesItLevel_ThenRestores()
        {
            Run(Frame);
            Run(_tuning.JamCueSeconds + 0.1f, nearHome: true);
            _still = 0f;
            Assert.AreEqual(LiftStep.None, Run(5f, nearHome: true, onPlatform: true),
                "On the jammed platform is not where it stitches.");
            _still = 0f;
            Assert.AreEqual(LiftStep.None, Run(_tuning.StitchDelay - 0.1f, nearHome: true, atStitchSpot: true),
                "Not rested yet.");
            Assert.AreEqual(LiftStep.StitchStarted, Run(0.2f, nearHome: true, atStitchSpot: true));
            Assert.AreEqual(LiftPhase.Stitching, _lift.Phase);
            Run(0.5f * _tuning.StitchSeconds - 0.1f, nearHome: true, atStitchSpot: true);
            Assert.That(_lift.Level, Is.InRange(0.3f, 0.7f), "Halfway level, eased.");
            Assert.AreEqual(LiftStep.Restored, Run(0.5f * _tuning.StitchSeconds + 0.1f, nearHome: true, atStitchSpot: true));
            Assert.IsTrue(_lift.IsRestored);
            Assert.AreEqual(1f, _lift.Level);
        }

        [Test]
        public void AfterTheStitch_TheFirstRideWaitsABreath()
        {
            Restore();
            Assert.AreEqual(LiftPhase.Ready, _lift.Phase);
            Assert.AreEqual(LiftStep.None, Run(0.5f * _tuning.BoardDelay, onPlatform: true),
                "07 has been still all along, but the platform breathes first.");
            Assert.AreEqual(LiftStep.RideStarted, Run(_tuning.BoardDelay, onPlatform: true));
        }

        [Test]
        public void TheRide_RisesAtRest_HoldsInTheLab_AndOnlyDownBringsItDown()
        {
            Restore();
            Run(Frame);
            _still = 0f;
            Run(_tuning.BoardDelay + 0.1f);
            _still = 0f;
            Assert.AreEqual(LiftStep.None, Run(_tuning.BoardDelay - 0.1f, onPlatform: true), "Not rested yet.");
            Assert.AreEqual(LiftStep.RideStarted, Run(0.1f + Frame, onPlatform: true));
            Assert.IsTrue(_lift.Holds07);
            Run(0.5f * _tuning.RideSeconds, onPlatform: true);
            Assert.That(_lift.Height, Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(LiftStep.Arrived, Run(0.5f * _tuning.RideSeconds + Frame, onPlatform: true),
                "The platform reaches the cabin floor ...");
            Assert.AreEqual(LiftStep.ReachedInside, Run(_tuning.SettleSeconds + 0.1f, onPlatform: true),
                "... and settles there before the lab opens.");
            Assert.AreEqual(LiftStep.None, Run(20f, onPlatform: true, moving: true),
                "Turning and looking round the lab never brings it down.");
            Assert.AreEqual(1f, _lift.Height);

            Assert.AreEqual(LiftStep.Descending, Run(Frame, onPlatform: true, down: true));
            Run(0.5f * _tuning.RideSeconds, onPlatform: true);
            Assert.That(_lift.Descent, Is.InRange(0.3f, 0.7f), "the turn toward the dock follows the descent");
            Assert.AreEqual(LiftStep.Arrived, Run(0.5f * _tuning.RideSeconds + Frame, onPlatform: true));
            Assert.AreEqual(LiftStep.ReachedBottom, Run(_tuning.SettleSeconds + 0.1f, onPlatform: true));
            Assert.AreEqual(0f, _lift.Height);
            Assert.AreEqual(0f, _lift.Descent);
            Assert.IsFalse(_lift.Holds07);
        }

        [Test]
        public void AfterARideDown_07MustLeaveThePlatform_BeforeItRisesAgain()
        {
            _lift.Restore(true);
            Run(_tuning.BoardDelay + 0.1f, onPlatform: true);
            Run(_tuning.RideSeconds + _tuning.SettleSeconds + 0.1f, onPlatform: true);
            Run(Frame, onPlatform: true, down: true);
            Run(_tuning.RideSeconds + _tuning.SettleSeconds + 0.1f, onPlatform: true);
            Assert.AreEqual(LiftStep.None, Run(10f, onPlatform: true), "Still on it after the ride: stays down.");
            Run(Frame);
            _still = 0f;
            Assert.AreEqual(LiftStep.RideStarted, Run(_tuning.BoardDelay + 0.1f, onPlatform: true));
        }

        [Test]
        public void ALoadedRestoredLift_IsReadyAtTheBottom()
        {
            _lift.Restore(true);
            Assert.AreEqual(LiftPhase.Ready, _lift.Phase);
            Assert.AreEqual(1f, _lift.Level);
            Assert.AreEqual(0f, _lift.Height);
        }

        [Test]
        public void ALoadedUnrestoredSave_WithHomesPower_ShowsTheJamAndBecomesCallable()
        {
            _lift.Restore(false);
            Assert.AreEqual(LiftPhase.Dormant, _lift.Phase);
            Assert.AreEqual(LiftStep.Powered, Run(Frame, powered: true, nearHome: true));
            Assert.AreEqual(LiftPhase.Jammed, _lift.Phase);
            Run(0.5f * _tuning.JamCueSeconds, powered: true, nearHome: true);
            Assert.Greater(_lift.Shudder, 0f);
            Assert.AreEqual(LiftStep.Called, Run(_tuning.JamCueSeconds, powered: true, nearHome: true));
            Assert.AreEqual(LiftPhase.Called, _lift.Phase);
        }
    }
}
