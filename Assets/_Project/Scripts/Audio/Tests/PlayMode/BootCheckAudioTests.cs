using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The New Game self-test heard from 07: single motions as their check starts, the others on their pulses,
    /// the fault on its result.</summary>
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
        [TestCase(BootCheckStep.Begin, 1, "boot_begin_shiver")]
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
            _rig.Events.Publish(new BootCheckPulse(BootCheckStep.Begin, 1, 0f));
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
    }
}
