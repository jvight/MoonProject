using NUnit.Framework;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The call on 07's face (opening-v3 2.4): it shows once 07 has stood still for 2 s and for 3 s on a live change,
    /// never on a replayed one, its letters hide what has not decoded, and through the guidance slot it is always the
    /// one element: a chip never shows beside it.
    /// </summary>
    public sealed class CallDisplayTests
    {
        private const float Frame = 1f / 60f;
        private const float FadeSeconds = 0.4f;
        private const string Sender = "ARK IV";

        private CallDisplaySettings _settings;
        private CallDisplay _call;

        [SetUp]
        public void SetUp()
        {
            _settings = new CallDisplaySettings();
            _call = new CallDisplay(_settings);
            _call.SetSender(Sender);
            _call.StateChanged(CallState.Broken, true);
            _call.Decode(0, true);
        }

        [Test]
        public void TheLetters_ShowOnlyWhatHasDecoded_AndKeepTheSpace()
        {
            Assert.AreEqual("??? ??", CallDisplay.Mask(Sender, 0));
            Assert.AreEqual("A?? ??", CallDisplay.Mask(Sender, 1));
            Assert.AreEqual("ARK ??", CallDisplay.Mask(Sender, 3));
            Assert.AreEqual("ARK IV", CallDisplay.Mask(Sender, 5));
            Assert.AreEqual("ARK IV", CallDisplay.Mask(Sender, 9), "more than the sender decodes nothing extra");

            _call.Decode(1, false);
            Assert.AreEqual("A?? ??", _call.Letters);
        }

        [Test]
        public void StandingStill_ShowsTheCall_OnlyAfterTwoSeconds_AndMovingHidesIt()
        {
            Run(_settings.StillSeconds - 0.1f, still: true);
            Assert.IsFalse(_call.WantsStill, "not before 07 has stood still for 2 s");

            Run(0.2f, still: true);
            Assert.IsTrue(_call.WantsStill, "after 2 s still, the face shows the call");

            _call.Step(Frame, false, false);
            Assert.IsFalse(_call.WantsStill, "the moment 07 moves, it goes");

            Run(_settings.StillSeconds - 0.1f, still: true);
            Assert.IsFalse(_call.WantsStill, "and stopping again waits the whole 2 s");
        }

        [Test]
        public void ALiveChange_ShowsForThreeSecondsOnScreen_EvenWhileDriving()
        {
            _call.StateChanged(CallState.Locked, false);
            Assert.IsTrue(_call.WantsChange);

            Run(5f, still: false, changeOnScreen: false);
            Assert.IsTrue(_call.WantsChange, "while it waits for the slot, its seconds do not run");

            Run(_settings.ChangeSeconds - 0.1f, still: false, changeOnScreen: true);
            Assert.IsTrue(_call.WantsChange, "on screen it lasts its 3 s");

            Run(0.2f, still: false, changeOnScreen: true);
            Assert.IsFalse(_call.WantsChange, "then lets go");
            Assert.IsFalse(_call.WantsStill, "and 07 is driving, so nothing else holds it");
        }

        [Test]
        public void ALetterDecoding_IsAChangeToo()
        {
            _call.Decode(1, false);
            Assert.IsTrue(_call.WantsChange);
        }

        [Test]
        public void AReplayedChange_AtSessionStart_ShowsNothing()
        {
            _call.StateChanged(CallState.Clear, true);
            _call.Decode(3, true);
            Assert.IsFalse(_call.WantsChange);
            Assert.AreEqual(CallState.Clear, _call.State);
            Assert.AreEqual("ARK ??", _call.Letters);
        }

        [Test]
        public void TheSameStateAgain_IsNoChange()
        {
            _call.StateChanged(CallState.Broken, false);
            _call.Decode(0, false);
            Assert.IsFalse(_call.WantsChange);
        }

        [Test]
        public void ACallNeverHeard_NeverShows()
        {
            var unheard = new CallDisplay(_settings);
            unheard.SetSender(Sender);
            for (float t = 0f; t < 5f; t += Frame)
            {
                unheard.Step(Frame, true, false);
            }

            Assert.IsFalse(unheard.WantsStill);
            Assert.IsFalse(unheard.WantsChange);
        }

        [Test]
        public void TheWaveform_KeysThreeShortAndOneLong()
        {
            bool[] expected = { true, false, true, false, true, false, true, true, true, false, false, false, false };
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int unit = 0; unit < expected.Length; unit++)
                {
                    float at = cycle * expected.Length + unit + 0.5f;
                    Assert.AreEqual(expected[unit], CallWavePainter.IsKeyed(at), $"unit {unit} of call {cycle}");
                }
            }

            Assert.IsTrue(CallWavePainter.IsKeyed(-13f + 0.5f), "it wraps before the clock's start too");
        }

        [Test]
        public void TheStillCall_WaitsForAChip_AndAChipThatWantsIn_FadesItOutFirst()
        {
            var slot = new GuidanceSlot();
            var wants = new bool[GuidanceSlot.KindCount];
            var visible = new bool[GuidanceSlot.KindCount];
            wants[(int)GuidanceKind.CallStatus] = true;
            wants[(int)GuidanceKind.Prompt] = true;
            slot.Step(wants, visible);
            Assert.IsTrue(slot.Allows(GuidanceKind.Prompt), "a chip outranks the still call");
            Assert.IsFalse(slot.Allows(GuidanceKind.CallStatus));

            wants[(int)GuidanceKind.Objective] = true;
            slot.Step(wants, visible);
            Assert.IsTrue(slot.Allows(GuidanceKind.Prompt), "the objective line waits too");

            wants[(int)GuidanceKind.Prompt] = false;
            slot.Step(wants, visible);
            Assert.IsTrue(slot.Allows(GuidanceKind.Objective), "the objective line outranks the still call");

            wants[(int)GuidanceKind.Objective] = false;
            slot.Step(wants, visible);
            Assert.IsTrue(slot.Allows(GuidanceKind.CallStatus), "which shows when nothing else guides");

            visible[(int)GuidanceKind.CallStatus] = true;
            wants[(int)GuidanceKind.Prompt] = true;
            slot.Step(wants, visible);
            Assert.IsNull(slot.Granted, "the chip waits until the call has faded");
        }

        [Test]
        public void OverAWholeStop_TheCallAndAChip_AreNeverOnScreenTogether()
        {
            var slot = new GuidanceSlot();
            var wants = new bool[GuidanceSlot.KindCount];
            var visible = new bool[GuidanceSlot.KindCount];
            var openness = new float[GuidanceSlot.KindCount];
            _call.StateChanged(CallState.Locked, false);
            int frames = (int)(12f / Frame);
            bool callSeen = false;
            bool chipSeen = false;
            for (int f = 0; f < frames; f++)
            {
                float t = f * Frame;
                bool chipWants = (t > 1f && t < 2.5f) || (t > 6f && t < 7f);
                bool callAsLine = _call.WantsChange;
                _call.Step(Frame, true, callAsLine && openness[(int)GuidanceKind.CallLine] > 0f &&
                                        slot.Allows(GuidanceKind.CallLine));
                wants[(int)GuidanceKind.CallLine] = _call.WantsChange;
                wants[(int)GuidanceKind.CallStatus] = _call.WantsStill;
                wants[(int)GuidanceKind.Prompt] = chipWants;
                for (int k = 0; k < GuidanceSlot.KindCount; k++)
                {
                    visible[k] = openness[k] > 0f;
                }

                slot.Step(wants, visible);
                int onScreen = 0;
                for (int k = 0; k < GuidanceSlot.KindCount; k++)
                {
                    bool granted = slot.Allows((GuidanceKind)k);
                    openness[k] = Mathf.MoveTowards(openness[k], granted ? 1f : 0f, Frame / FadeSeconds);
                    onScreen += openness[k] > 0f ? 1 : 0;
                }

                Assert.LessOrEqual(onScreen, 1, $"one element at {t:0.00} s");
                callSeen |= openness[(int)GuidanceKind.CallLine] > 0f || openness[(int)GuidanceKind.CallStatus] > 0f;
                chipSeen |= openness[(int)GuidanceKind.Prompt] > 0f;
            }

            Assert.IsTrue(callSeen && chipSeen, "both had their turn");
        }

        private void Run(float seconds, bool still, bool changeOnScreen = false)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _call.Step(Frame, still, changeOnScreen);
            }
        }
    }
}
