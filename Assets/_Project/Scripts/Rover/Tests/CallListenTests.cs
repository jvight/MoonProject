using NUnit.Framework;
using MoonProject.Core.Events;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07 listening to the call (opening v3 2.2): the head turns while it idles or charges, the lamp stutters only
    /// while the call is broken and keys three short and one long once it locks, and 07 reaches up now and then.
    /// </summary>
    public sealed class CallListenTests
    {
        private const float Step = 1f / 60f;
        private const float Lit = 0.5f;

        private CallListenSettings _settings;
        private CallListen _call;

        [SetUp]
        public void SetUp()
        {
            _settings = new CallListenSettings();
            _call = new CallListen(_settings);
        }

        private void Run(float seconds, CallState state, bool listening, float keying = 0f)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _call.Step(Step, state, listening, keying);
            }
        }

        [Test]
        public void TheHead_TurnsTowardTheCall_WhileListening_AndBackWhenItStops()
        {
            Run(0.1f, CallState.Broken, false);
            Assert.AreEqual(0f, _call.HeadWeight, 0.01f);
            Run(6f, CallState.Broken, true);
            Assert.Greater(_call.HeadWeight, 0.95f, "idle or charging, the head is on the call");
            Run(6f, CallState.Broken, false);
            Assert.Less(_call.HeadWeight, 0.05f, "and it lets go when 07 moves");
        }

        [Test]
        public void TheLamp_StuttersWhileBroken_WithSoftEdges_NeverFasterThanThreeATimeASecond()
        {
            float highest = 0f;
            float lowest = float.MaxValue;
            bool wasLit = false;
            var risings = new System.Collections.Generic.List<float>();
            float time = 0f;
            for (; time < 60f; time += Step)
            {
                _call.Step(Step, CallState.Broken, true, 0f);
                highest = System.Math.Max(highest, _call.LampGlow);
                lowest = System.Math.Min(lowest, _call.LampGlow);
                bool lit = _call.LampGlow > Lit;
                if (lit && !wasLit)
                {
                    risings.Add(time);
                }

                wasLit = lit;
            }

            Assert.Greater(highest, _settings.StutterGlow * 0.9f, "lit bursts");
            Assert.Less(lowest, _settings.StutterBase + 0.1f, "and dark gaps");
            Assert.Greater(risings.Count, 10, "it keeps stuttering");
            for (int i = 3; i < risings.Count; i++)
            {
                Assert.GreaterOrEqual(risings[i] - risings[i - 3], 1f, "no more than 3 bursts in any second");
            }
        }

        [Test]
        public void TheStutter_StopsWhenTheCallLocks_AndNeverComesBack()
        {
            Run(10f, CallState.Broken, true);
            _call.Step(Step, CallState.Locked, true, 0f);
            Run(1.2f, CallState.Locked, true);
            Assert.Less(_call.LampGlow, 0.05f, "the lamp is dark between the stutter and the first tone");
            Run(30f, CallState.Clear, true);
            Assert.AreEqual(_settings.ClearGlow, _call.LampGlow, 0.02f, "a clear call is a steady glow: no stutter, no pattern");
        }

        [Test]
        public void AFailedReply_LowersTheHead_ThenLiftsItAgain_AndDarkensTheLampForAWhile()
        {
            Run(5f, CallState.Clear, true);
            Assert.AreEqual(0f, _call.Dejection, 0.01f);
            Assert.AreEqual(_settings.ClearGlow, _call.LampGlow, 0.02f);
            _call.ReplyFailed();
            Run(2f, CallState.Clear, true);
            Assert.Greater(_call.Dejection, 0.9f, "the head hangs");
            Assert.Less(_call.LampGlow, 0.02f, "and the lamp has dropped");
            Run(_settings.DejectedHoldSeconds + 3f, CallState.Clear, true);
            Assert.Less(_call.Dejection, 0.05f, "it lifts after the hold");
            Run(_settings.LampDarkSeconds, CallState.Clear, true);
            Assert.AreEqual(_settings.ClearGlow, _call.LampGlow, 0.05f, "and the lamp relights, softly, in the end");
        }

        [Test]
        public void AFailedReply_NeverMakesTheHeadJump()
        {
            Run(5f, CallState.Clear, true);
            _call.ReplyFailed();
            float previous = _call.Dejection;
            float fastest = 0f;
            for (float t = 0f; t < 10f; t += Step)
            {
                _call.Step(Step, CallState.Clear, true, 0f);
                fastest = System.Math.Max(fastest, System.Math.Abs(_call.Dejection - previous));
                previous = _call.Dejection;
            }

            Assert.Less(fastest, 0.03f, "eased both ways");
        }

        [Test]
        public void TheLockedLamp_FollowsTheSignsKeying_InPhase_ToItsGlow()
        {
            Run(5f, CallState.Broken, true);
            Run(1f, CallState.Locked, true, 0f);
            Assert.Less(_call.LampGlow, 0.05f, "between the tones the lamp is dark");
            Run(0.6f, CallState.Locked, true, 1f);
            Assert.AreEqual(_settings.KeyGlow, _call.LampGlow, 0.05f, "a tone at full strength lights it fully");
            Run(0.6f, CallState.Locked, true, 0.5f);
            Assert.AreEqual(_settings.KeyGlow * 0.5f, _call.LampGlow, 0.05f, "and half a tone, half the glow");
            Run(0.6f, CallState.Locked, true, 0f);
            Assert.Less(_call.LampGlow, 0.05f, "back to dark with the tone");
        }

        [Test]
        public void TheLockedLamp_IsDark_WithNoSignal_AndRaisesNoError()
        {
            Run(60f, CallState.Locked, true, 0f);
            Assert.Less(_call.LampGlow, 0.02f, "no ICallSignal, no keying: the lamp stays dark");
        }

        [Test]
        public void TheLockedLamp_KeysInPhaseWhileDriving_Too()
        {
            Run(1f, CallState.Locked, false, 1f);
            Assert.AreEqual(_settings.KeyGlow, _call.LampGlow, 0.05f, "the sign is the sky's, not 07's idling");
        }

        [Test]
        public void TheLockedLamp_EasesBetweenTones_NeverJumping()
        {
            Run(1f, CallState.Locked, true, 0f);
            float previous = _call.LampGlow;
            float fastest = 0f;
            for (float t = 0f; t < 2f; t += Step)
            {
                _call.Step(Step, CallState.Locked, true, t < 1f ? 1f : 0f);
                fastest = System.Math.Max(fastest, System.Math.Abs(_call.LampGlow - previous));
                previous = _call.LampGlow;
            }

            Assert.Less(fastest, _settings.KeyGlow * 0.4f, "the lamp's own soft edge stays between the signal's");
        }

        [Test]
        public void TheReach_ComesAfterTheFirstSeconds_OnlyWhileBrokenAndListening_AndRepeats()
        {
            Run(_settings.ReachFirstSeconds * 0.5f, CallState.Broken, true);
            Assert.AreEqual(0f, _call.Reach, "not yet");
            float peak = 0f;
            int reaches = 0;
            bool up = false;
            for (float t = 0f; t < 60f; t += Step)
            {
                _call.Step(Step, CallState.Broken, true, 0f);
                peak = System.Math.Max(peak, _call.Reach);
                bool nowUp = _call.Reach > 0.9f;
                if (nowUp && !up)
                {
                    reaches++;
                }

                up = nowUp;
            }

            Assert.Greater(peak, 0.99f, "07 reaches all the way up");
            Assert.GreaterOrEqual(reaches, 3, "and does it again after the interval");
            Run(5f, CallState.Locked, true);
            Assert.AreEqual(0f, _call.Reach, 0.001f, "a locked call needs no reaching for");
        }

        [Test]
        public void TheReach_IsEased_NeverJumping()
        {
            Run(_settings.ReachFirstSeconds, CallState.Broken, true);
            float previous = _call.Reach;
            float fastest = 0f;
            for (float t = 0f; t < 6f; t += Step)
            {
                _call.Step(Step, CallState.Broken, true, 0f);
                fastest = System.Math.Max(fastest, System.Math.Abs(_call.Reach - previous));
                previous = _call.Reach;
            }

            Assert.Less(fastest, 0.05f, "a reach moves a few percent a frame at most");
        }
    }
}
