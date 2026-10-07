using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// When the lonely wide shot opens and hands back, and how its weight eases: slowly out, quickly but smoothly home.
    /// </summary>
    public sealed class WideShotTests
    {
        private const float Frame = 1f / 60f;

        private WideShotSettings _settings;
        private WideShot _wide;
        private float _still;

        [SetUp]
        public void SetUp()
        {
            _settings = new WideShotSettings();
            _wide = new WideShot(_settings);
            _still = 0f;
        }

        /// <summary>Steps with 07 resting (or not), returning the first cue other than None.</summary>
        private WideShotCue Run(float seconds, bool resting = true, bool busy = false, bool held = false,
            float frame = Frame)
        {
            WideShotCue first = WideShotCue.None;
            for (float t = 0f; t < seconds - 1e-4f; t += frame)
            {
                _still = resting ? _still + frame : 0f;
                WideShotCue cue = _wide.Step(_still, busy, held, frame);
                Assert.LessOrEqual(_wide.QuietSeconds, _still + 1e-4f, "Quiet time never exceeds the rest.");
                if (first == WideShotCue.None)
                {
                    first = cue;
                }
            }

            return first;
        }

        private void OpenFully()
        {
            Assert.AreEqual(WideShotCue.Open, Run(_settings.Delay + 0.1f));
            _wide.Open(new WideShotFrame(0f, 7f, 28f, 0.19f));
            Run(3f * _settings.OpenSeconds);
        }

        [Test]
        public void Rest_OpensTheFrame_OnlyAfterTheDelay()
        {
            Assert.AreEqual(WideShotCue.None, Run(_settings.Delay - 0.1f));
            Assert.AreEqual(WideShotCue.Open, Run(0.2f));
        }

        [Test]
        public void Busy_KeepsItClosed_AndTheCountStartsOverAfterwards()
        {
            Assert.AreEqual(WideShotCue.None, Run(3f * _settings.Delay, busy: true));
            Assert.AreEqual(WideShotCue.None, Run(_settings.Delay - 0.1f), "The quiet count started over.");
            Assert.AreEqual(WideShotCue.Open, Run(0.2f));
        }

        [Test]
        public void HoldBack_DelaysOpening()
        {
            _wide.HoldBack(5f);
            Assert.AreEqual(WideShotCue.None, Run(_settings.Delay + 4.8f));
            Assert.AreEqual(WideShotCue.Open, Run(0.4f));
        }

        [Test]
        public void RestEndingOrSomethingBusy_HandsBackAnOpenFrame()
        {
            OpenFully();
            Assert.AreEqual(WideShotCue.HandBack, Run(Frame, resting: false), "Drive or look input hands back.");

            _wide.HandBack();
            OpenFully();
            Assert.AreEqual(WideShotCue.HandBack, Run(Frame, busy: true), "A moment or the tether hands back.");
        }

        [Test]
        public void Held_DelaysOpening_ButLeavesAnOpenFrameAlone()
        {
            Assert.AreEqual(WideShotCue.None, Run(3f * _settings.Delay, held: true), "Paused: it waits.");
            _wide = new WideShot(_settings);
            OpenFully();
            Assert.AreEqual(WideShotCue.None, Run(5f, held: true), "Pausing an open frame keeps it.");
            Assert.AreEqual(1f, _wide.Weight, 0.01f);
        }

        [Test]
        public void Opening_DriftsOutSlowly_AndSettlesWithinTheOpenTime()
        {
            Run(_settings.Delay + 0.1f);
            _wide.Open(new WideShotFrame(0f, 7f, 28f, 0.19f));
            float previous = _wide.Weight;
            float largestStep = 0f;
            float atOneSecond = 0f;
            for (float t = Frame; t <= _settings.OpenSeconds + 1e-4f; t += Frame)
            {
                Run(Frame);
                float weight = _wide.Weight;
                Assert.GreaterOrEqual(weight, previous - 1e-6f, "Opening never wavers back.");
                largestStep = Mathf.Max(largestStep, weight - previous);
                previous = weight;
                if (Mathf.Abs(t - 1f) < 0.5f * Frame)
                {
                    atOneSecond = weight;
                }
            }

            Assert.Less(atOneSecond, 0.15f, "It eases in: the first second barely moves.");
            Assert.Greater(_wide.Weight, 0.94f, "All but settled within the open time.");
            Assert.Less(largestStep, 2f * Frame / _settings.OpenSeconds * 1.5f, "Never faster than a gentle drift.");
        }

        [Test]
        public void HandBack_ComesHomeWithinTheHandBackTime_EasedAtBothEnds()
        {
            OpenFully();
            _wide.HandBack();
            float previous = _wide.Weight;
            Run(Frame);
            float firstStep = previous - _wide.Weight;
            previous = _wide.Weight;
            for (float t = Frame; t < _settings.HandBackSeconds; t += Frame)
            {
                Run(Frame);
                Assert.LessOrEqual(_wide.Weight, previous + 1e-6f, "Straight home, no bounce.");
                previous = _wide.Weight;
            }

            Assert.Less(firstStep, 0.02f, "It starts from rest: no cut.");
            Assert.Less(_wide.Weight, 0.06f, "All but home within the hand-back time.");
        }

        /// <summary>
        /// Handing back halfway out turns round with continuous speed: the largest frame-to-frame change of speed
        /// shrinks with the frame time (a smooth turn), where an eased restart from rest would jolt by the same amount
        /// at any frame rate.
        /// </summary>
        [Test]
        public void HandBack_MidOpening_TurnsRoundWithoutAJolt()
        {
            float coarse = LargestSpeedChangeAcrossHandBack(1f / 60f);
            float fine = LargestSpeedChangeAcrossHandBack(1f / 240f);
            Assert.Less(fine, 0.4f * coarse, $"Speed changes {coarse:0.000} at 60 fps, {fine:0.000} at 240 fps.");
        }

        private float LargestSpeedChangeAcrossHandBack(float frame)
        {
            var wide = new WideShot(_settings);
            float still = 0f;
            for (float t = 0f; t < _settings.Delay + 0.1f; t += frame)
            {
                still += frame;
                wide.Step(still, false, false, frame);
            }

            wide.Open(new WideShotFrame(0f, 7f, 28f, 0.19f));
            for (float t = 0f; t < 0.35f * _settings.OpenSeconds; t += frame)
            {
                still += frame;
                wide.Step(still, false, false, frame);
            }

            float before = wide.Weight;
            still += frame;
            wide.Step(still, false, false, frame);
            float speed = (wide.Weight - before) / frame;
            wide.HandBack();
            float largest = 0f;
            for (int i = 0; i < 4; i++)
            {
                before = wide.Weight;
                wide.Step(0f, false, false, frame);
                float next = (wide.Weight - before) / frame;
                largest = Mathf.Max(largest, Mathf.Abs(next - speed));
                speed = next;
            }

            return largest;
        }
    }
}
