using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// A music moment (short generated tracks): one track, then silence; a request while one plays is ignored, and the
    /// next moment plays the other track.
    /// </summary>
    public sealed class RadioPlaylistTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig(AudioTestRig.ShortTrackSeconds);
            yield return null;
            _rig.Wake(true);
            _rig.Answer();
            Assert.IsTrue(_rig.Radio.MomentPending, "asked before the radio is on: the wake-up starts it");
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music once relay.1 answers");
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [UnityTest]
        public IEnumerator AMoment_PlaysOneTrack_ThenFallsSilent()
        {
            RadioTrack first = _rig.Radio.CurrentTrack;
            Assert.IsNotNull(first);
            float deadline = Time.realtimeSinceStartup + AudioTestRig.ShortTrackSeconds + 3f;
            while (_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                Assert.IsTrue(_rig.Radio.CurrentTrack == null || _rig.Radio.CurrentTrack == first,
                    "no second track follows");
                yield return null;
            }

            Assert.IsFalse(_rig.Radio.MusicStarted, "the set falls silent after the one track");
            Assert.IsNull(_rig.Radio.CurrentTrack);
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsFalse(_rig.Radio.MusicStarted, "and stays silent");
        }

        [UnityTest]
        public IEnumerator ARequestWhilePlaying_IsIgnored_AndTheNextMomentPlaysTheOtherTrack()
        {
            RadioTrack first = _rig.Radio.CurrentTrack;
            _rig.RequestMusic();
            Assert.AreSame(first, _rig.Radio.CurrentTrack, "a moment already playing is not interrupted");
            float deadline = Time.realtimeSinceStartup + AudioTestRig.ShortTrackSeconds + 3f;
            while (_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsFalse(_rig.Radio.MusicStarted, "the request during the first track did not queue another");
            _rig.RequestMusic();
            AudioSource swish = AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");
            Assert.IsTrue(swish.isPlaying, "the dial finds the station");
            deadline = Time.realtimeSinceStartup + 4f;
            while (_rig.Radio.CurrentTrack == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(_rig.Radio.CurrentTrack);
            Assert.AreNotSame(first, _rig.Radio.CurrentTrack, "the next moment plays the other track");
        }
    }
}
