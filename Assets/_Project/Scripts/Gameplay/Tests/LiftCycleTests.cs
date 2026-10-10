using NUnit.Framework;
using UnityEngine;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>The crew's lift: dormant until powered, fed and stitched once, then ridden up and down at rest.</summary>
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
        private LiftStep Run(float seconds, bool powered = true, bool affordable = true, bool onFeedPad = false,
            bool onPlatform = false, bool feedDone = false, bool anyInput = false)
        {
            LiftStep first = LiftStep.None;
            for (float t = 0f; t < seconds; t += Frame)
            {
                _still = anyInput ? 0f : _still + Frame;
                var input = new LiftInput(powered, affordable, onFeedPad, onPlatform, _still, feedDone, anyInput);
                LiftStep step = _lift.Step(input, Frame);
                first = first == LiftStep.None ? step : first;
            }

            return first;
        }

        private void Restore()
        {
            Run(Frame);
            Run(_tuning.FeedDelay + 0.1f, onFeedPad: true);
            Run(Frame, feedDone: true);
            Run(_tuning.StitchSeconds + 0.1f);
        }

        [Test]
        public void Unpowered_StaysDormant_EvenWith07OnThePad()
        {
            Assert.AreEqual(LiftStep.None, Run(5f, false, onFeedPad: true));
            Assert.AreEqual(LiftPhase.Dormant, _lift.Phase);
        }

        [Test]
        public void Powered_FeedsOnlyOnceRestedOnThePad_WithTheMaterials()
        {
            Assert.AreEqual(LiftStep.Powered, Run(Frame));
            Assert.AreEqual(LiftStep.None, Run(5f, affordable: false, onFeedPad: true), "Nothing to feed it.");
            Assert.AreEqual(LiftStep.None, Run(5f), "Not on the pad.");
            _still = 0f;
            Assert.AreEqual(LiftStep.None, Run(_tuning.FeedDelay - 0.1f, onFeedPad: true), "Not rested yet.");
            Assert.AreEqual(LiftStep.FeedStarted, Run(0.2f, onFeedPad: true));
            Assert.AreEqual(LiftPhase.Feeding, _lift.Phase);
        }

        [Test]
        public void TheStitch_EasesThePlatformLevel_ThenRestores()
        {
            Run(Frame);
            Run(_tuning.FeedDelay + 0.1f, onFeedPad: true);
            Run(Frame, feedDone: true);
            Assert.AreEqual(LiftPhase.Stitching, _lift.Phase);
            Run(0.5f * _tuning.StitchSeconds);
            Assert.That(_lift.Level, Is.InRange(0.3f, 0.7f), "Halfway level, eased.");
            Assert.AreEqual(LiftStep.Restored, Run(0.5f * _tuning.StitchSeconds + 0.1f));
            Assert.IsTrue(_lift.IsRestored);
            Assert.AreEqual(1f, _lift.Level);
        }

        [Test]
        public void TheRide_RisesAtRest_HoldsAtTheTop_AndAnyInputBringsItDown()
        {
            Restore();
            _still = 0f;
            Assert.AreEqual(LiftStep.None, Run(_tuning.BoardDelay - 0.1f, onPlatform: true), "Not rested yet.");
            Assert.AreEqual(LiftStep.RideStarted, Run(0.1f + Frame, onPlatform: true));
            Assert.IsTrue(_lift.Holds07);
            Run(0.5f * _tuning.RideSeconds, onPlatform: true);
            Assert.That(_lift.Height, Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(LiftStep.Arrived, Run(0.5f * _tuning.RideSeconds + Frame, onPlatform: true),
                "The platform reaches the top stop ...");
            Assert.AreEqual(LiftStep.ReachedTop, Run(_tuning.SettleSeconds + 0.1f, onPlatform: true),
                "... and settles there before the lookout opens.");
            Assert.AreEqual(LiftStep.None, Run(20f, onPlatform: true), "The lookout holds while 07 rests.");
            Assert.AreEqual(1f, _lift.Height);

            Assert.AreEqual(LiftStep.Descending, Run(Frame, onPlatform: true, anyInput: true));
            Assert.AreEqual(LiftStep.Arrived, Run(_tuning.RideSeconds + Frame, onPlatform: true));
            Assert.AreEqual(LiftStep.ReachedBottom, Run(_tuning.SettleSeconds + 0.1f, onPlatform: true));
            Assert.AreEqual(0f, _lift.Height);
            Assert.IsFalse(_lift.Holds07);
        }

        [Test]
        public void AfterARideDown_07MustLeaveThePlatform_BeforeItRisesAgain()
        {
            _lift.Restore(true);
            Run(_tuning.BoardDelay + 0.1f, onPlatform: true);
            Run(_tuning.RideSeconds + _tuning.SettleSeconds + 0.1f, onPlatform: true);
            Run(Frame, onPlatform: true, anyInput: true);
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
    }
}
