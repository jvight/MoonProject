using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The soundscape of solitude in the running mix: distance, stillness, the room tone and 07's own small
    /// sounds.</summary>
    public sealed class SoundscapeTests
    {
        // Far east of the base, away from the test canyon (which runs north): well past the radio's signal.
        private static readonly Vector3 FarAway = new Vector3(600f, 0f, 0f);

        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
            _rig.Wake(true);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music after 07 wakes");
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private Soundscape Mix => _rig.Soundscape;

        private void Drive(float speed)
        {
            _rig.Rover.Speed = speed;
            _rig.Rover.NormalizedSpeed = Mathf.Clamp01(speed / 6f);
            _rig.Rover.DriveInput = speed > 0f ? Vector2.up : Vector2.zero;
        }

        [UnityTest]
        public IEnumerator FarFromHome_TheRadioFadesToNearSilence_AndTheRoomToneTakesOver_ThenHomeGivesItBack()
        {
            Drive(4f);
            yield return new WaitForSecondsRealtime(1f);
            AudioSource room = Mix.RoomToneSource;
            float roomAtHome = room.volume;
            Assert.AreEqual(1f, Mix.RadioGain, 1e-3f);

            _rig.Rover.Position = FarAway;
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(1f, Mix.Farness, 1e-3f);
            Assert.Less(Mix.RadioGain, 0.04f, "near-silence: about -30 dB");
            Assert.Less(Mix.BasinBedGain, 0.4f, "the basin bed recedes");
            Assert.Greater(room.volume, roomAtHome * 4f, "the room tone takes over");
            Assert.IsTrue(room.isPlaying);

            _rig.Rover.Position = Vector3.zero;
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(1f, Mix.RadioGain, 1e-3f, "coming home warms it all back in");
        }

        [UnityTest]
        public IEnumerator Stillness_PullsTheWorldBack_AndMovingRestoresItQuickly()
        {
            Drive(0f);
            yield return new WaitForSeconds(7f);
            Assert.Greater(Mix.Stillness, 0.9f, "settled about six seconds after stopping");
            Assert.Greater(Mix.StillSeconds, 6f);
            Assert.Less(SoundscapeModel.ToDb(Mix.RadioGain), -3.5f, "the world pulls back a few dB");

            Drive(3f);
            yield return new WaitForSeconds(1f);
            Assert.Less(Mix.Stillness, 0.05f);
            Assert.AreEqual(1f, Mix.RadioGain, 0.03f);
        }

        [UnityTest]
        public IEnumerator TheLampHums_FromWaking_AtTheEye()
        {
            yield return new WaitForSeconds(2.5f);
            AudioSource lamp = _rig.SmallSounds.LampSource;
            Assert.IsTrue(lamp.isPlaying);
            Assert.Greater(lamp.volume, 0f);
            Assert.AreEqual(1f, lamp.spatialBlend);
            Assert.Less(Vector3.Distance(lamp.transform.position, _rig.Rover.TetherOrigin.position), 1e-4f);
        }

        [UnityTest]
        public IEnumerator Servos_WhirWhileTheWheelsTurnAndTheNeckLooks_QuietOtherwise()
        {
            AudioSource steering = _rig.SmallSounds.SteeringServoSource;
            AudioSource neck = _rig.SmallSounds.NeckServoSource;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(steering.isPlaying);
            Assert.IsFalse(neck.isPlaying);

            bool steered = false;
            bool looked = false;
            float start = Time.time;
            while (Time.time - start < 1f)
            {
                float t = Time.time - start;
                _rig.Rover.DriveInput = new Vector2(Mathf.Sin(t * 12f), 0f);
                _rig.Rover.TetherOrigin.localRotation = Quaternion.Euler(0f, 70f * Mathf.Sin(t * 5f), 0f);
                yield return null;
                steered |= steering.isPlaying && steering.volume > 0f;
                looked |= neck.isPlaying && neck.volume > 0f;
            }

            Assert.IsTrue(steered, "the steering servo whirs as the wheels turn");
            Assert.IsTrue(looked, "the neck servo whirs as 07 looks around");
            Assert.Greater(neck.pitch, 1f, "the neck's smaller motor sings higher");

            _rig.Rover.DriveInput = Vector2.zero;
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(steering.isPlaying, "quiet once the servos rest");
            Assert.IsFalse(neck.isPlaying);
        }

        [UnityTest]
        public IEnumerator AfterADrive_07sMetalTicksAsItCools()
        {
            Drive(6f);
            yield return new WaitForSeconds(3f);
            Assert.Greater(_rig.SmallSounds.Heat, 0.1f);
            int plays = _rig.Director.PlayCount;
            Drive(0f);
            yield return new WaitForSeconds(3.6f);

            bool ticked = false;
            for (int i = 0; i < Math.Min(16, _rig.Director.PlayCount - plays); i++)
            {
                AudioClip clip = _rig.Director.RecentClip(i);
                ticked |= clip != null && clip.name.StartsWith("rover_metal_tick", StringComparison.Ordinal);
            }

            Assert.IsTrue(ticked, "a tick a few seconds after the motor stops");
        }
    }
}
