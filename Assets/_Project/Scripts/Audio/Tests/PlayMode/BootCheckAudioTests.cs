using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The New Game self-test heard from 07: single motions as their check starts, the others on their pulses,
    /// the fault on its result, and one rush instead of them all when the player fast-forwards the readout.</summary>
    public sealed class BootCheckAudioTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            _rig.CreateWakeAudio();
            _rig.Rover.Position = new Vector3(4f, 0f, -3f);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private void AssertLastFrom07(string clip)
        {
            AudioSource voice = _rig.Director.LastVoice;
            Assert.IsNotNull(voice);
            Assert.AreEqual(clip, _rig.Director.LastClip.name);
            Assert.IsTrue(voice.isPlaying);
            Assert.AreEqual(1f, voice.spatialBlend, "heard from 07");
            Assert.Less(Vector3.Distance(_rig.Rover.Position, voice.transform.position), 1e-4f);
        }

        [TestCase(BootCheckStep.Lens, "boot_lens")]
        [TestCase(BootCheckStep.Battery, "boot_battery")]
        [TestCase(BootCheckStep.Assembly, "boot_assembly")]
        public void ASingleMotionCheck_PlaysAsItStarts(BootCheckStep step, string clip)
        {
            _rig.Events.Publish(new BootCheckStarted(step));
            AssertLastFrom07(clip);
        }

        [TestCase(BootCheckStep.Carrier)]
        [TestCase(BootCheckStep.Resume)]
        [TestCase(BootCheckStep.Agenda)]
        [TestCase(BootCheckStep.Lamps)]
        [TestCase(BootCheckStep.Wheels)]
        [TestCase(BootCheckStep.Cutter)]
        [TestCase(BootCheckStep.Begin)]
        public void TheBlacksChecks_AndThePulsedOnes_AddNothingAsTheyStart(BootCheckStep step)
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BootCheckStarted(step));
            Assert.AreEqual(plays, _rig.Director.PlayCount);
        }

        [TestCase(BootCheckStep.Lamps, 0, "boot_lamp_flicker_01")]
        [TestCase(BootCheckStep.Lamps, 2, "boot_lamp_flicker_03")]
        [TestCase(BootCheckStep.Lamps, 3, "boot_lamp_ramp")]
        [TestCase(BootCheckStep.Head, 0, "boot_head_sweep")]
        [TestCase(BootCheckStep.Head, 1, "boot_head_nod")]
        [TestCase(BootCheckStep.Wheels, 3, "boot_wheel_04")]
        [TestCase(BootCheckStep.Motor, 0, "boot_motor_shudder")]
        [TestCase(BootCheckStep.Motor, 1, "boot_motor_creep")]
        [TestCase(BootCheckStep.Cutter, 1, "boot_cutter_spark_02")]
        [TestCase(BootCheckStep.SolarWing, 0, "boot_wing_jerk")]
        [TestCase(BootCheckStep.SolarWing, 1, "boot_wing_stall")]
        [TestCase(BootCheckStep.SolarWing, 2, "boot_wing_fold")]
        [TestCase(BootCheckStep.Antenna, 0, "boot_antenna_rise")]
        [TestCase(BootCheckStep.Antenna, 1, "boot_antenna_signal")]
        [TestCase(BootCheckStep.Begin, 0, "boot_begin_turn")]
        [TestCase(BootCheckStep.Begin, 2, "boot_begin_lamps")]
        public void EachMotion_PlaysOnItsPulse_From07(BootCheckStep step, int index, string clip)
        {
            _rig.Events.Publish(new BootCheckStarted(step));
            _rig.Events.Publish(new BootCheckPulse(step, index, 0f));
            AssertLastFrom07(clip);
        }

        [Test]
        public void AContinuousPulse_SoundsOnce_AndTheNextCheckStartsFresh()
        {
            _rig.Events.Publish(new BootCheckStarted(BootCheckStep.Antenna));
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Antenna, 1, 0f));
            int plays = _rig.Director.PlayCount;
            for (int i = 1; i <= 10; i++)
            {
                _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Antenna, 1, i * 0.1f));
            }

            Assert.AreEqual(plays, _rig.Director.PlayCount, "the 10 Hz progress pulses add nothing");

            _rig.Events.Publish(new BootCheckStarted(BootCheckStep.Begin));
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Begin, 0, 0f));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount);
        }

        [Test]
        public void TheBeaconsBlinks_AreTheLandersLight_NotA07Sound()
        {
            _rig.Events.Publish(new BootCheckStarted(BootCheckStep.Cutter));
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Cutter, 3, 0f));
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Cutter, 5, 0f));
            Assert.AreEqual(plays, _rig.Director.PlayCount);
        }

        [Test]
        public void OnlyAFault_SpeaksOnItsResult()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BootCheckCompleted(BootCheckStep.Head, BootCheckResult.Ok));
            _rig.Events.Publish(new BootCheckCompleted(BootCheckStep.SolarWing, BootCheckResult.Jammed));
            _rig.Events.Publish(new BootCheckCompleted(BootCheckStep.Antenna, BootCheckResult.ReceiveOnly));
            _rig.Events.Publish(new BootCheckCompleted(BootCheckStep.Assembly, BootCheckResult.Info));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "the jam and the hiss are in the motions");

            _rig.Events.Publish(new BootCheckCompleted(BootCheckStep.Cutter, BootCheckResult.Fault));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount);
            Assert.AreEqual("boot_fault", _rig.Director.LastClip.name);
        }

        [UnityTest]
        public IEnumerator AFastForward_PlaysOneRush_NoBurstOfCues_ThenTheReadyPerformanceSounds()
        {
            _rig.Events.Publish(new BootCheckStarted(BootCheckStep.Lens));
            int plays = _rig.Director.PlayCount;
            var playedAt = new List<float>();

            _rig.Events.Publish(new BootCheckFastForwarded(6f));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount);
            Assert.AreEqual("boot_rush", _rig.Director.LastClip.name);
            playedAt.Add(Time.realtimeSinceStartup);
            plays = _rig.Director.PlayCount;

            // The remaining lines at 6x: a start, its motions and its result every frame or so, a fault among them.
            BootCheckStep[] remaining =
            {
                BootCheckStep.Lamps, BootCheckStep.Head, BootCheckStep.Wheels, BootCheckStep.Motor,
                BootCheckStep.Cutter, BootCheckStep.SolarWing, BootCheckStep.Antenna, BootCheckStep.Battery,
                BootCheckStep.Assembly,
            };
            foreach (BootCheckStep step in remaining)
            {
                _rig.Events.Publish(new BootCheckStarted(step));
                for (int index = 0; index < 4; index++)
                {
                    _rig.Events.Publish(new BootCheckPulse(step, index, 0f));
                }

                BootCheckResult result = step == BootCheckStep.Cutter ? BootCheckResult.Fault : BootCheckResult.Ok;
                _rig.Events.Publish(new BootCheckCompleted(step, result));
                if (_rig.Director.PlayCount != plays)
                {
                    playedAt.Add(Time.realtimeSinceStartup);
                    plays = _rig.Director.PlayCount;
                }

                yield return null;
            }

            _rig.Events.Publish(new BootCheckFinished());
            for (int i = 2; i < playedAt.Count; i++)
            {
                Assert.Greater(playedAt[i] - playedAt[i - 2], 0.1f, "no more than 2 cues within 100 ms");
            }

            Assert.AreEqual(1, playedAt.Count, "the rush stands in for every remaining line, the fault too");

            _rig.Events.Publish(new BootCheckStarted(BootCheckStep.Begin));
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Begin, 0, 0f));
            AssertLastFrom07("boot_begin_turn");
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Begin, 2, 0f));
            AssertLastFrom07("boot_begin_lamps");
        }
    }
}
