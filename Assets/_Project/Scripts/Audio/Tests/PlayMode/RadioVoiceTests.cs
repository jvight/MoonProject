using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>A voiced ticker line plays its recording, tells the ticker how long it is and ducks the music.</summary>
    public sealed class RadioVoiceTests
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

        [UnityTest]
        public IEnumerator AVoicedLine_PlaysItsRecording_AndTheMusicDucksUnderIt()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);
            float started = -1f;
            using (_rig.Events.Subscribe<VoiceLineStarted>(e => started = e.Seconds))
            {
                _rig.Events.Publish(new TickerLineShown(AudioTestRig.VoiceKey));
            }

            Assert.AreEqual(AudioTestRig.VoiceSeconds, started, 0.01f, "the ticker learns the recording's length");
            Assert.IsTrue(_rig.Voice.IsSpeaking);
            yield return new WaitForSecondsRealtime(1f);
            Assert.Less(_rig.Voice.MusicDuck, 0.9f, "the music steps back for the words");

            yield return new WaitForSecondsRealtime(AudioTestRig.VoiceSeconds + 3f);
            Assert.IsFalse(_rig.Voice.IsSpeaking);
            Assert.Greater(_rig.Voice.MusicDuck, 0.95f, "and comes back after");
        }

        [UnityTest]
        public IEnumerator ALineWithoutARecording_StaysSilent()
        {
            bool started = false;
            using (_rig.Events.Subscribe<VoiceLineStarted>(e => started = true))
            {
                _rig.Events.Publish(new TickerLineShown("test.ticker.unvoiced"));
            }

            yield return null;
            Assert.IsFalse(started);
            Assert.IsFalse(_rig.Voice.IsSpeaking);
        }
    }
}
