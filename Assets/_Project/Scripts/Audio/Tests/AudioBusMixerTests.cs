using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class AudioBusMixerTests
    {
        [Test]
        public void Effective_MultipliesMasterIntoEveryBus()
        {
            var mixer = new AudioBusMixer();
            mixer.SetVolume(AudioBus.Master, 0.5f);
            mixer.SetVolume(AudioBus.Music, 0.8f);

            Assert.AreEqual(0.4f, mixer.Effective(AudioBus.Music), 1e-6f);
            Assert.AreEqual(0.5f, mixer.Effective(AudioBus.Sfx), 1e-6f);
            Assert.AreEqual(0.5f, mixer.Effective(AudioBus.Master), 1e-6f);
        }

        [Test]
        public void Mixer_IsTheAudioSettingsService()
        {
            IAudioSettings settings = new AudioBusMixer();
            settings.SetVolume(AudioBus.Music, 0.25f);
            Assert.AreEqual(0.25f, settings.GetVolume(AudioBus.Music));
        }

        [Test]
        public void SetVolume_ClampsAndBumpsVersionOnlyOnChange()
        {
            var mixer = new AudioBusMixer();
            int start = mixer.Version;

            mixer.SetVolume(AudioBus.Ambience, 3f);
            mixer.SetVolume(AudioBus.Ambience, 1f);
            mixer.SetVolume(AudioBus.Sfx, -1f);

            Assert.AreEqual(1f, mixer.GetVolume(AudioBus.Ambience));
            Assert.AreEqual(0f, mixer.GetVolume(AudioBus.Sfx));
            Assert.AreEqual(start + 1, mixer.Version);
        }
    }
}
