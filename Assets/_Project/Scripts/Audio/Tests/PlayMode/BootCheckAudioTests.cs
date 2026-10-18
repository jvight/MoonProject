using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The New Game self-test heard from 07: each check's motion when it starts, the fault on its result.</summary>
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

        [TestCase(BootCheckStep.Lens, "boot_lens")]
        [TestCase(BootCheckStep.Lamps, "boot_lamps")]
        [TestCase(BootCheckStep.Head, "boot_head")]
        [TestCase(BootCheckStep.Wheels, "boot_wheels")]
        [TestCase(BootCheckStep.Motor, "boot_motor")]
        [TestCase(BootCheckStep.Cutter, "boot_cutter")]
        [TestCase(BootCheckStep.SolarWing, "boot_wing_jam")]
        [TestCase(BootCheckStep.Antenna, "boot_antenna")]
        [TestCase(BootCheckStep.Battery, "boot_battery")]
        [TestCase(BootCheckStep.Assembly, "boot_assembly")]
        [TestCase(BootCheckStep.Begin, "boot_begin")]
        public void EachCheck_PlaysItsMotion_From07_AsItStarts(BootCheckStep step, string clip)
        {
            _rig.Events.Publish(new BootCheckStarted(step));

            AudioSource voice = _rig.Director.LastVoice;
            Assert.IsNotNull(voice);
            Assert.AreEqual(clip, _rig.Director.LastClip.name);
            Assert.IsTrue(voice.isPlaying);
            Assert.AreEqual(1f, voice.spatialBlend, "heard from 07");
            Assert.Less(Vector3.Distance(_rig.Rover.Position, voice.transform.position), 1e-4f);
        }

        [TestCase(BootCheckStep.Carrier)]
        [TestCase(BootCheckStep.Resume)]
        [TestCase(BootCheckStep.Agenda)]
        public void TheBlacksChecks_AreTheWakesOwnBeats_AndAddNothing(BootCheckStep step)
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BootCheckStarted(step));
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
