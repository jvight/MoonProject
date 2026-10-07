using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Whispering Canyon: whisper and trough beds, the echo on 07's sounds and the radio thinning.</summary>
    public sealed class CanyonAudioTests
    {
        // The canyon follows 07 with a 2.5 s time constant: 8 s is > 95 % of the way.
        private const float SettleSeconds = 8f;

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

        private CanyonAmbience Canyon => _rig.Canyon;

        [UnityTest]
        public IEnumerator AtTheBase_TheCanyonIsSilent()
        {
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(0f, Canyon.Inside);
            Assert.IsFalse(Canyon.WhisperSource.isPlaying);
            Assert.IsFalse(Canyon.TroughSource.isPlaying);
            Assert.IsFalse(Canyon.Echo.enabled);
            Assert.AreEqual(1f, _rig.Soundscape.BasinBedGain, 1e-4f);
            Assert.AreEqual(1f, _rig.Soundscape.RadioGain, 1e-4f);
            Assert.AreEqual(22000f, _rig.Soundscape.RadioCutoff(22000f));
        }

        [UnityTest]
        public IEnumerator DeepInside_TheWhisperRises_TheBasinRecedes_TheEchoComesOn_AndTheRadioThins()
        {
            _rig.Rover.Position = FakeWorldAnchors.DeepInside;
            yield return new WaitForSecondsRealtime(SettleSeconds);

            Assert.Greater(Canyon.Inside, 0.9f);
            Assert.IsTrue(Canyon.WhisperSource.isPlaying, "the canyon's breathy air");
            Assert.Greater(Canyon.WhisperSource.volume, 0f);
            Assert.IsFalse(Canyon.TroughSource.isPlaying, "the trough bed is only for the chasm");
            Assert.IsTrue(Canyon.Echo.enabled, "a gentle echo on 07's sounds");
            Assert.Greater(Canyon.Echo.room, -1500);
            Assert.Less(_rig.Soundscape.BasinBedGain, 0.7f, "the open basin recedes");
            Assert.Less(_rig.Soundscape.RadioGain, 0.7f, "home's radio is thinner in here");
            Assert.Less(_rig.Soundscape.RadioCutoff(22000f), 3000f);

            _rig.Rover.Position = Vector3.zero;
            yield return new WaitForSecondsRealtime(SettleSeconds);
            Assert.Less(Canyon.Inside, 0.1f, "coming home gives it all back, gently");
            Assert.Greater(Canyon.Inside, 0f, "but never all at once");
        }

        [UnityTest]
        public IEnumerator DownInTheChasm_TheDarkerTroughBedTakesOver()
        {
            _rig.Rover.Position = FakeWorldAnchors.TroughFloor;
            yield return new WaitForSecondsRealtime(SettleSeconds);

            Assert.Greater(Canyon.Trough, 0.9f);
            Assert.IsTrue(Canyon.TroughSource.isPlaying);
            Assert.Greater(Canyon.TroughSource.volume, 4f * Canyon.WhisperSource.volume,
                "the whisper gives way to the deep");
        }

        [Test]
        public void TheEcho_IsFor07sWorld_TheRadioUiAndBedsStayDry()
        {
            Assert.IsTrue(_rig.Radio.GetDeck(0).bypassReverbZones, "radio music");
            Assert.IsTrue(AudioTestRig.FindChildSource(_rig.Radio.transform, "Static").bypassReverbZones);
            Assert.IsTrue(Canyon.WhisperSource.bypassReverbZones);
            Assert.IsTrue(AudioTestRig.FindChildSource(_rig.Ambience.transform, "AmbienceLoop").bypassReverbZones);

            _rig.Events.Publish(new SonarPinged(Vector3.zero, 80f));
            Assert.IsFalse(_rig.Director.LastVoice.bypassReverbZones, "07's sonar rings off the walls");
            _rig.Events.Publish(new UpgradePurchased("radio_tower", 2));
            Assert.IsTrue(_rig.Director.LastVoice.bypassReverbZones, "a 2D stinger stays dry");
        }
    }
}
