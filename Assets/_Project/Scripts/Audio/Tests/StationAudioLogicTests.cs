using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Audio.Tests
{
    public sealed class StationAudioLogicTests
    {
        private const float Frame = 1f / 60f;

        private StationAudioTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<StationAudioTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [TestCase(StationCue.FeedStarted, null)]
        [TestCase(StationCue.BundleDropped, "hopper_clunk")]
        [TestCase(StationCue.Fed, null)]
        [TestCase(StationCue.HatchOpened, "port_hatch_open")]
        [TestCase(StationCue.StitchStarted, null)]
        [TestCase(StationCue.HatchClosed, "port_hatch_close")]
        public void EachStationBeat_PlaysItsSound(StationCue cue, string expected)
        {
            Assert.IsTrue(StationSounds.TryGetOneShot(cue, out string id));
            Assert.AreEqual(expected, id);
        }

        [Test]
        public void EveryStationBeat_HasASound_AndASlot()
        {
            int size = StationSounds.TableSize();
            foreach (StationCue cue in (StationCue[])Enum.GetValues(typeof(StationCue)))
            {
                Assert.IsTrue(StationSounds.TryGetOneShot(cue, out _), $"{cue} needs a sound in StationSounds");
                Assert.Less((int)cue, size, $"{cue} fits the beat table");
            }
        }

        [Test]
        public void AnUnknownBeat_IsSilent_NotAnError()
        {
            Assert.IsFalse(StationSounds.TryGetOneShot((StationCue)StationSounds.TableSize(), out string id));
            Assert.IsNull(id);
        }

        [Test]
        public void EachBundleOfAFeed_LandsALittleHigher_SoNoTwoDropsMatch()
        {
            var drops = new BundleDrops(_tuning, new AudioRandom(7u));
            float previous = 0f;
            for (int i = 0; i < 3; i++)
            {
                float pitch = drops.NextPitch();
                Assert.AreEqual(1f + i * _tuning.DropPitchStep, pitch, _tuning.DropPitchWobble + 1e-6f,
                    $"drop {i} sits on its step, wobbling a little");
                Assert.Greater(pitch, previous, $"drop {i} lands on the ones already in the bin");
                previous = pitch;
            }

            Assert.AreEqual(3, drops.Dropped);
            Assert.Less(previous, 1.1f, "a touch higher, never a squeak");
            drops.Restart();
            Assert.AreEqual(0, drops.Dropped);
            Assert.AreEqual(1f, drops.NextPitch(), _tuning.DropPitchWobble + 1e-6f, "the next feed starts low again");
        }

        [Test]
        public void TheDropWobble_IsUnderHalfTheStep_SoLaterDropsAlwaysLandHigher()
        {
            Assert.Less(2f * _tuning.DropPitchWobble, _tuning.DropPitchStep);
            Assert.Greater(_tuning.DropPitchWobble, 0f, "the same bundle twice still sounds a little different");
        }

        [Test]
        public void BellsKnobTap_IsACue()
        {
            Assert.AreEqual(5, (int)BellCue.DialTapped, "after DialTurned in Core's BellCue");
            Assert.AreEqual("bell_knob_tap", AudioCueIds.BellKnobTap);
        }

        private static float Move(BayArmVoice arm, float speed, float seconds, out bool sighed)
        {
            sighed = false;
            for (float t = 0f; t < seconds; t += Frame)
            {
                arm.Step(speed, Frame);
                sighed |= arm.TakeSigh();
            }

            return arm.Amount;
        }

        [Test]
        public void AnArm_WhirsWithItsTip_AndSighsAsItComesToRest()
        {
            var arm = new BayArmVoice(_tuning);
            Assert.AreEqual(0f, Move(arm, _tuning.ArmDeadSpeed * 0.5f, 1f, out _), "a still arm is silent");
            Assert.AreEqual(1f, Move(arm, _tuning.ArmFullSpeed * 2f, 1f, out bool sighed), 1e-3f);
            Assert.IsFalse(sighed, "no sigh while it moves");
            Assert.AreEqual(0f, Move(arm, 0f, 1f, out sighed), 1e-3f);
            Assert.IsTrue(sighed, "a soft hydraulic sigh as it settles");
        }

        [Test]
        public void ATwitch_OrAPlacement_IsNotAMove()
        {
            var arm = new BayArmVoice(_tuning);
            Move(arm, _tuning.ArmFullSpeed, 0.05f, out _);
            Move(arm, 0f, 1f, out bool sighed);
            Assert.IsFalse(sighed, "a twitch does not sigh");

            Move(arm, _tuning.ArmPlacedSpeed * 2f, 1f, out _);
            Assert.AreEqual(0f, arm.Amount, "the bay being placed is not the arm moving");
        }

        [Test]
        public void Charging_HumsSoftly_ThenIsFullOnce_AndTheHumSettles()
        {
            var charge = new ChargeModel(_tuning);
            charge.Dock();
            int fulls = 0;
            float fullAt = -1f;
            float t = 0f;
            float peak = 0f;
            while (t < _tuning.ChargeSeconds + _tuning.ChargeFadeOut + 1f)
            {
                charge.Step(Frame);
                t += Frame;
                peak = Mathf.Max(peak, charge.Hum);
                if (charge.TakeFull())
                {
                    fulls++;
                    fullAt = t;
                }
            }

            Assert.AreEqual(1f, peak, 1e-4f, "the hum came in");
            Assert.AreEqual(1, fulls);
            Assert.AreEqual(_tuning.ChargeSeconds, fullAt, 2f * Frame);
            Assert.IsFalse(charge.HumAudible, "full: the hum has settled away");
        }

        [Test]
        public void LeavingTheDockEarly_LetsTheHumGo_WithoutAFullTone()
        {
            var charge = new ChargeModel(_tuning);
            charge.Dock();
            for (int i = 0; i < 120; i++)
            {
                charge.Step(Frame);
            }

            charge.Undock();
            bool full = false;
            for (float t = 0f; t < _tuning.ChargeSeconds; t += Frame)
            {
                charge.Step(Frame);
                full |= charge.TakeFull();
            }

            Assert.IsFalse(full);
            Assert.IsFalse(charge.HumAudible);
        }
    }
}
