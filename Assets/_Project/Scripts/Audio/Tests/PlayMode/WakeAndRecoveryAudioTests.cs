using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The world before and after 07 wakes, and the recovery lift, published on the bus.</summary>
    public sealed class WakeAndRecoveryAudioTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private AudioSource Deck(string name)
        {
            return AudioTestRig.FindChildSource(_rig.Radio.transform, name);
        }

        [UnityTest]
        public IEnumerator BeforeWaking_OnlyTheAmbiencePlays()
        {
            yield return new WaitForSecondsRealtime(1f);

            Assert.IsFalse(_rig.Radio.IsOn);
            Assert.IsFalse(_rig.AnyDeckPlaying(), "no music before 07 wakes, not even silently");
            Assert.IsFalse(Deck("Static").isPlaying);
            Assert.IsFalse(Deck("TuningSwish").isPlaying);
            Assert.AreEqual(0f, _rig.RoverAudio.HumSource.volume, "07 sleeps: no motor hum");
            AudioSource ambience = AudioTestRig.FindChildSource(_rig.Ambience.transform, "AmbienceLoop");
            Assert.IsTrue(ambience.isPlaying);
            Assert.Greater(ambience.volume, 0f);
        }

        [UnityTest]
        public IEnumerator WakingOnItsOwn_TunesIn_ThroughStatic_ThenResolvesIntoMusic()
        {
            _rig.Wake(false);
            yield return null;
            Assert.IsTrue(_rig.Radio.IsOn);
            Assert.IsTrue(Deck("TuningSwish").isPlaying, "the dial-tuning swish starts at once");
            Assert.IsTrue(Deck("Static").isPlaying);

            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsFalse(_rig.Radio.MusicStarted, "static first, music after the wake delay");
            float crackle = Deck("Static").volume;
            Assert.Greater(crackle, 0f);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted);
            yield return new WaitForSecondsRealtime(5f);
            float music = _rig.LoudestDeck().volume;
            Assert.Greater(music, 0.3f, "the music has resolved");
            Assert.Less(Deck("Static").volume, crackle * 0.5f, "the wake-up static melted away");
        }

        [UnityTest]
        public IEnumerator WokenByThePlayer_TheMusicArrivesSooner()
        {
            _rig.Wake(true);
            float start = Time.realtimeSinceStartup;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup - start < 3f)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted);
            Assert.Less(Time.realtimeSinceStartup - start, 1.2f);
        }

        [UnityTest]
        public IEnumerator Waking_PowersUpTheMotorHum()
        {
            _rig.Wake(false);
            yield return null;
            AudioSource hum = _rig.RoverAudio.HumSource;
            float startPitch = hum.pitch;

            yield return new WaitForSecondsRealtime(2f);

            Assert.IsTrue(_rig.RoverAudio.IsAwake);
            Assert.Greater(hum.volume, 0f);
            Assert.Greater(hum.pitch, startPitch, "the motor glides up from its wake pitch");
        }

        [UnityTest]
        public IEnumerator RecoveryLift_RisesOn07_ThenSettlesWhenTheLiftEnds()
        {
            _rig.Rover.Position = new Vector3(10f, 0f, 10f);
            _rig.Events.Publish(new RoverRecovering(_rig.Rover.Position, new Vector3(14f, 2f, 10f), 1.2f));
            AudioSource lift = _rig.RoverAudio.LiftSource;
            Assert.IsTrue(lift.isPlaying);
            Assert.IsTrue(_rig.RoverAudio.Lifting);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.Greater(lift.volume, 0f);
            Assert.Greater(lift.pitch, 1.01f, "the servo glides up as 07 rises");
            _rig.Rover.Position = new Vector3(12f, 1f, 10f);
            yield return null;
            Assert.Less(Vector3.Distance(lift.transform.position, _rig.Rover.Position), 1e-4f, "the lift sounds on 07");

            _rig.Rover.Position = new Vector3(14f, 2f, 10f);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(lift.isPlaying);
            Assert.IsFalse(_rig.RoverAudio.Lifting);
            AudioSource settle = _rig.FindPlayingVoice("recovery_settle");
            Assert.IsNotNull(settle, "the settle plays when the lift ends");
            Assert.Less(Vector3.Distance(settle.transform.position, _rig.Rover.Position), 1e-4f);
        }
    }
}
