using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The station moving from track to track (short generated tracks).</summary>
    public sealed class RadioPlaylistTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig(AudioTestRig.ShortTrackSeconds);
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

        [UnityTest]
        public IEnumerator Radio_TunesToTheOtherTrack_WhenOneEnds()
        {
            RadioTrack first = _rig.Radio.CurrentTrack;
            AudioSource swish = AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");
            Assert.IsNotNull(first);
            bool swishPlayed = false;
            RadioTrack next = first;
            float deadline = Time.realtimeSinceStartup + AudioTestRig.ShortTrackSeconds + 4f;
            while ((next == first || next == null) && Time.realtimeSinceStartup < deadline)
            {
                swishPlayed |= swish.isPlaying;
                yield return null;
                next = _rig.Radio.CurrentTrack;
            }

            Assert.IsNotNull(next);
            Assert.AreNotSame(first, next, "the station moves on to the other track (no immediate repeat)");
            Assert.IsTrue(swishPlayed, "the dial-tuning swish plays during the change");
        }
    }
}
