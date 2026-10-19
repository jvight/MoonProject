using NUnit.Framework;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Audio.Tests
{
    /// <summary>The call's carrier: its facing swell, its mode per state and its break-up while on reserve.</summary>
    public sealed class CallCarrierModelTests
    {
        private const float Step = 1f / 60f;

        private static CallCarrierSettings Settings => new CallCarrierSettings();

        [Test]
        public void FacingGain_NeverFallsAs07TurnsTowardTheLight_FromTheFloorToFull()
        {
            CallCarrierSettings settings = Settings;
            float previous = float.NegativeInfinity;
            for (int i = 0; i <= 200; i++)
            {
                float cos = -1f + i * 0.01f;
                float gain = CallCarrierModel.FacingGain(cos, settings.AwayFloor, settings.FacingSharpness);
                Assert.GreaterOrEqual(gain, previous, $"monotonic at cos {cos}");
                previous = gain;
            }

            Assert.AreEqual(settings.AwayFloor, CallCarrierModel.FacingGain(-1f, settings.AwayFloor, 2.5f), 1e-5f);
            Assert.AreEqual(1f, CallCarrierModel.FacingGain(1f, settings.AwayFloor, 2.5f), 1e-5f);
            Assert.Less(CallCarrierModel.FacingGain(0f, settings.AwayFloor, 2.5f), 0.5f,
                "the swell gathers toward the end of the turn, like tuning in");
        }

        [TestCase(CallState.Broken, true, false)]
        [TestCase(CallState.Locked, true, false)]
        [TestCase(CallState.Clear, false, false)]
        [TestCase(CallState.Answered, false, true)]
        public void EachState_HasItsCarrierMode(CallState state, bool hasStatic, bool answered)
        {
            CallCarrierSettings settings = Settings;
            Assert.AreEqual(hasStatic, CallCarrierModel.StaticFor(state, settings) > 0f);
            var model = new CallCarrierModel(settings, 7u);
            model.SetState(state);
            Assert.AreEqual(answered, model.Answered);
        }

        [Test]
        public void Broken_DropsOutOnlyWithinItsTunedRange_AndWanders()
        {
            CallCarrierSettings settings = Settings;
            var model = new CallCarrierModel(settings, 11u);
            model.SetState(CallState.Broken);
            for (int i = 0; i < 120; i++)
            {
                model.Step(Step, 1f);
            }

            int dropouts = 0;
            bool wasIn = false;
            float minGain = float.PositiveInfinity;
            float pitchSpread = 0f;
            for (int i = 0; i < 60 * 60; i++)
            {
                model.Step(Step, 1f);
                if (model.InDropout && !wasIn)
                {
                    dropouts++;
                    Assert.GreaterOrEqual(model.LastDropoutLength, settings.DropoutMin);
                    Assert.LessOrEqual(model.LastDropoutLength, settings.DropoutMax);
                }

                wasIn = model.InDropout;
                minGain = Mathf.Min(minGain, model.Gain);
                pitchSpread = Mathf.Max(pitchSpread, Mathf.Abs(model.Pitch - 1f));
            }

            Assert.Greater(dropouts, 20, "a minute on reserve breaks up many times");
            Assert.GreaterOrEqual(minGain, settings.Level * settings.DropoutGain - 1e-4f, "never deeper than tuned");
            Assert.Greater(pitchSpread, settings.WowDepth * 0.5f);
            Assert.LessOrEqual(pitchSpread, settings.WowDepth + 1e-4f);
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(3)]
        public void TheDecodedName_ClimbsAndLandsOnD(int total)
        {
            const int steps = 5;
            int previous = -1;
            for (int decoded = 1; decoded <= total; decoded++)
            {
                int step = CallCarrierModel.DecodeStep(decoded, total, steps);
                Assert.GreaterOrEqual(step, previous, "the notes climb as the name completes");
                Assert.That(step, Is.InRange(0, steps - 1));
                previous = step;
            }

            Assert.AreEqual(0, CallCarrierModel.DecodeStep(1, total, steps), "the first character starts low");
            Assert.AreEqual(steps - 1, CallCarrierModel.DecodeStep(total, total, steps), "the whole name lands on D");
        }

        [TestCase(CallState.Locked)]
        [TestCase(CallState.Clear)]
        [TestCase(CallState.Answered)]
        public void OnceLocked_TheCarrierIsSteady(CallState state)
        {
            CallCarrierSettings settings = Settings;
            var model = new CallCarrierModel(settings, 3u);
            model.SetState(state);
            for (int i = 0; i < 60 * 30; i++)
            {
                model.Step(Step, 1f);
                Assert.IsFalse(model.InDropout);
            }

            Assert.AreEqual(settings.Level, model.Gain, 1e-3f, "full level facing the light");
            Assert.AreEqual(1f, model.Pitch, 1e-5f);
        }
    }
}
