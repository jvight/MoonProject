using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// relay.1's lamp from answered to relit (<see cref="RelayLampState"/>): it keys with the sign until relit; on
    /// relighting it flickers through its warm-up, never above its steady glow, and then holds the steady glow
    /// whatever the keying; the network's reply blinks 3 short and 1 long; a relit relay loaded from a save is steady at
    /// once.
    /// </summary>
    public sealed class RelayLampStateTests
    {
        private const float Frame = 1f / 60f;

        private readonly RelayLampSettings _lamp = new RelayLampSettings();

        [Test]
        public void UntilRelit_TheLampKeysWithTheSign()
        {
            var state = new RelayLampState();
            Assert.AreEqual(0f, state.Level(0f, _lamp));
            Assert.AreEqual(1f, state.Level(1f, _lamp));
            Assert.AreEqual(0.4f, state.Level(0.4f, _lamp), 1e-5f);
        }

        [Test]
        public void OnRelighting_ItFlickersThroughItsWarmUp_ThenHoldsSteady()
        {
            var state = new RelayLampState();
            state.OnRelit(false);
            bool sawOff = false;
            bool sawOn = false;
            for (float t = 0f; t < _lamp.WarmUpSeconds - Frame; t += Frame)
            {
                float level = state.Level(1f, _lamp);
                Assert.LessOrEqual(level, _lamp.RelitLevel + 1e-5f, "the warm-up never outshines the steady glow");
                sawOff |= level <= _lamp.RelitLevel * _lamp.FlickerOffShare + 1e-5f;
                sawOn |= level > _lamp.RelitLevel * _lamp.FlickerOffShare + 1e-5f;
                state.Step(Frame);
            }

            Assert.IsTrue(sawOff && sawOn, "the warm-up flickers between nearly off and on");
            state.Step(Frame * 2f);
            for (int i = 0; i < 120; i++)
            {
                Assert.AreEqual(_lamp.RelitLevel, state.Level(i % 2, _lamp), 1e-5f, "steady, whatever the keying");
                state.Step(Frame);
            }
        }

        [Test]
        public void TheReply_BlinksThreeShortAndOneLong_ThenHoldsSteady()
        {
            var state = new RelayLampState();
            state.OnRelit(true);
            state.OnReply(false);
            float low = _lamp.RelitLevel * _lamp.ReplyLowShare;
            int blinks = 0;
            bool lit = false;
            float longest = 0f;
            float run = 0f;
            for (float t = 0f; t < RelayLampState.ReplySeconds(_lamp); t += Frame)
            {
                bool on = state.Level(1f, _lamp) > low + 1e-5f;
                blinks += on && !lit ? 1 : 0;
                run = on ? run + Frame : 0f;
                longest = Mathf.Max(longest, run);
                lit = on;
                state.Step(Frame);
            }

            Assert.AreEqual(_lamp.ReplyShorts + 1, blinks, "3 short blinks and 1 long");
            Assert.Greater(longest, _lamp.ReplyShortSeconds * 2f, "the last blink is the long one");
            state.Step(Frame);
            Assert.AreEqual(_lamp.RelitLevel, state.Level(0f, _lamp), 1e-5f, "then steady");
        }

        [Test]
        public void ARelitRelayFromASave_IsSteadyAtOnce()
        {
            var state = new RelayLampState();
            state.OnRelit(true);
            Assert.AreEqual(_lamp.RelitLevel, state.Level(0f, _lamp), 1e-5f);
        }
    }
}
