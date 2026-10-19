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

        private void Run(float seconds, CallState state, bool listening)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _call.Step(Step, state, listening);
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
                _call.Step(Step, CallState.Broken, true);
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
            _call.Step(Step, CallState.Locked, true);
            Run(_settings.KeyDelay * 0.8f, CallState.Locked, true);
            Assert.Less(_call.LampGlow, 0.05f, "the lamp is dark between the stutter and the first key");
            Run(2f, CallState.Clear, true);
            Assert.Less(_call.LampGlow, 0.05f);
            Run(30f, CallState.Clear, true);
            Assert.AreEqual(0f, _call.LampGlow, 0.02f, "a clear call has no stutter and no pattern here");
        }

        [Test]
        public void TheLockedCall_KeysThreeShortAndOneLong_AfterItsDelay_OnceAndThenOnEachRepeat()
        {
            Run(5f, CallState.Broken, true);
            _call.Step(Step, CallState.Locked, true);
            var keys = new System.Collections.Generic.List<float>();
            float down = -1f;
            float time = 0f;
            float total = _settings.KeyDelay + _call.PatternSeconds + _settings.KeyRepeat + _call.PatternSeconds + 1f;
            for (; time < total; time += Step)
            {
                _call.Step(Step, CallState.Locked, true);
                bool lit = _call.LampGlow > Lit;
                if (lit && down < 0f)
                {
                    down = time;
                }
                else if (!lit && down >= 0f)
                {
                    keys.Add(time - down);
                    down = -1f;
                }
            }

            Assert.AreEqual(8, keys.Count, "two runs of four keys: the pattern, then its repeat");
            for (int run = 0; run < 2; run++)
            {
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(_settings.ShortSeconds, keys[run * 4 + i], 0.15f, "three short");
                }

                Assert.AreEqual(_settings.LongSeconds, keys[run * 4 + 3], 0.15f, "then one long");
            }
        }

        [Test]
        public void ACallThatIsAlreadyLocked_WhenFirstSeen_IsNotKeyedAtOnce()
        {
            float early = _settings.KeyRepeat - 1f;
            Run(early, CallState.Locked, true);
            Assert.Less(_call.LampGlow, 0.05f, "a loaded game's lamp stays quiet");
            Assert.IsFalse(_call.KeyingPattern, "not before the repeat interval");
            Run(2f, CallState.Locked, true);
            Assert.IsTrue(_call.KeyingPattern, "its first repeat comes after the repeat interval");
        }

        [Test]
        public void TheKeys_AreOnlyDown_WhileO7Listens()
        {
            Run(2f, CallState.Broken, true);
            _call.Step(Step, CallState.Locked, true);
            Run(0.5f, CallState.Locked, false);
            float highest = 0f;
            for (float t = 0f; t < _settings.KeyDelay + _call.PatternSeconds + 1f; t += Step)
            {
                _call.Step(Step, CallState.Locked, false);
                highest = System.Math.Max(highest, _call.LampGlow);
            }

            Assert.Less(highest, 0.05f, "driving, the lamp keeps quiet");
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
                _call.Step(Step, CallState.Broken, true);
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
                _call.Step(Step, CallState.Broken, true);
                fastest = System.Math.Max(fastest, System.Math.Abs(_call.Reach - previous));
                previous = _call.Reach;
            }

            Assert.Less(fastest, 0.05f, "a reach moves a few percent a frame at most");
        }
    }
}
