using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Title screen theme playback: 2 s fade-in on <see cref="TitleShown"/>, uninterrupted playback through Settings
    /// and Credits, 0.8 s fade-out on Continue (<see cref="SessionStarted"/>) or New Game
    /// (<see cref="NewGameRequested"/>), data-driven variant selection, and complete silence from the radio and static
    /// while on the title screen.
    /// </summary>
    public sealed class TitleMusicAudioTests
    {
        private AudioTestRig _rig;
        private WakeAudio _wakeAudio;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            _wakeAudio = _rig.CreateWakeAudio();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            _rig.Dispose();
        }

        [UnityTest]
        public IEnumerator TitleShown_FadesInDefaultThemeOverTwoSeconds_AndRadioAndStaticNeverPlay()
        {
            AudioSource radioStatic = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            AudioSource radioSwish = AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");

            _rig.Events.Publish(new TitleShown(false));
            yield return null;

            Assert.IsTrue(_rig.TitleMusic.IsPlaying, "title theme starts playing when the title appears");
            Assert.AreEqual(TitleMusicLibrary.DefaultTrackId, _rig.TitleMusic.CurrentTrack.Id);
            Assert.IsTrue(_rig.TitleMusic.Source.loop);
            Assert.Less(_rig.TitleMusic.EnvelopeLevel, 0.2f, "starts near silence");

            yield return new WaitForSecondsRealtime(1f);
            float midVolume = _rig.TitleMusic.Source.volume;
            Assert.Greater(midVolume, 0.2f, "fading in at 1 s");
            Assert.Less(_rig.TitleMusic.EnvelopeLevel, 0.85f, "still mid-fade at 1 s");

            // Check that neither the fresh-install title nor a saved-game title wakes the radio or plays static.
            _rig.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(1.3f);

            float fullExpected = _rig.Director.Buses.Effective(AudioBus.Music) * _rig.TitleMusicLibrary.Volume;
            Assert.AreEqual(1f, _rig.TitleMusic.EnvelopeLevel, 1e-3f, "fade-in completes at ~2 s");
            Assert.AreEqual(fullExpected, _rig.TitleMusic.Source.volume, 1e-3f);
            Assert.IsFalse(_rig.Radio.IsOn, "the radio never turns on under the title");
            Assert.IsFalse(_rig.AnyDeckPlaying(), "no radio deck plays under the title");
            Assert.AreEqual(0f, radioStatic.volume, "no radio static under the title");
            Assert.IsFalse(radioSwish.isPlaying, "no tuning swish under the title");
            Assert.AreEqual(0f, _wakeAudio.StaticSource.volume, "WakeAudio AsleepStatic never plays under the title");
        }

        [UnityTest]
        public IEnumerator SettingsAndCredits_KeepTheTitleThemePlaying()
        {
            _rig.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(2.2f);

            float fullVolume = _rig.TitleMusic.Source.volume;
            Assert.Greater(fullVolume, 0.5f);

            // Opening Settings / Credits on the title screen fires PauseMenu UI cues and PauseChanged.
            _rig.Events.Publish(new PauseChanged(true));
            _rig.Events.Publish(new UiCue(UiCueKind.MenuOpen));
            _rig.Events.Publish(new UiCue(UiCueKind.FocusMove));
            _rig.Events.Publish(new UiCue(UiCueKind.Confirm));
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.IsTrue(_rig.TitleMusic.IsPlaying, "Settings and Credits keep the title theme playing");
            Assert.AreEqual(fullVolume, _rig.TitleMusic.Source.volume, 1e-4f);

            _rig.Events.Publish(new UiCue(UiCueKind.Back));
            _rig.Events.Publish(new UiCue(UiCueKind.MenuClose));
            _rig.Events.Publish(new PauseChanged(false));
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsTrue(_rig.TitleMusic.IsPlaying);
            Assert.AreEqual(fullVolume, _rig.TitleMusic.Source.volume, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Continue_DucksTheTitleThemeUnderTheCrtTock_AndGameAudioTakesOver()
        {
            _rig.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.AreEqual(1f, _rig.TitleMusic.EnvelopeLevel, 1e-3f);

            // Player selects Continue: SessionStarted(false) fires and 07 wakes.
            _rig.Events.Publish(new SessionStarted(false));
            Assert.AreEqual("title_crt_off", _rig.Director.LastVoice.clip.name, "the tube's tock as the CRT switches off");
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            _rig.Wake(true);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(_rig.TitleMusic.IsPlaying, "still fading out midway through the 0.8 s duck");
            Assert.Greater(_rig.TitleMusic.EnvelopeLevel, 0.2f);
            Assert.Less(_rig.TitleMusic.EnvelopeLevel, 0.8f);
            Assert.IsTrue(_rig.Radio.IsOn, "the game's own radio rules have taken over");

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(0f, _rig.TitleMusic.EnvelopeLevel, 1e-4f);
            Assert.IsFalse(_rig.TitleMusic.IsPlaying, "title theme is silent once the 0.8 s duck finishes");
            Assert.IsTrue(_rig.Radio.MusicStarted, "the game's radio station is now playing");
        }

        [UnityTest]
        public IEnumerator NewGame_FadesOutTitleThemeOverTwoPointFiveSeconds()
        {
            _rig.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.AreEqual(1f, _rig.TitleMusic.EnvelopeLevel, 1e-3f);

            _rig.Events.Publish(new NewGameRequested());
            yield return new WaitForSecondsRealtime(1.25f);
            Assert.IsTrue(_rig.TitleMusic.IsPlaying);
            Assert.Greater(_rig.TitleMusic.EnvelopeLevel, 0.2f);
            Assert.Less(_rig.TitleMusic.EnvelopeLevel, 0.8f);

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(0f, _rig.TitleMusic.EnvelopeLevel, 1e-4f);
            Assert.IsFalse(_rig.TitleMusic.IsPlaying);
        }

        [UnityTest]
        public IEnumerator SelectedVariantInData_ControlsWhichTitleThemePlays()
        {
            _rig.TitleMusicLibrary.SelectVariant("title_b_earthlight_cradle");
            _rig.Events.Publish(new TitleShown(false));
            yield return null;

            Assert.IsTrue(_rig.TitleMusic.IsPlaying);
            Assert.AreEqual("title_b_earthlight_cradle", _rig.TitleMusic.CurrentTrack.Id);

            _rig.TitleMusicLibrary.SelectVariant("title_c_quiet_horizon");
            _rig.Events.Publish(new TitleShown(false));
            yield return null;

            Assert.AreEqual("title_c_quiet_horizon", _rig.TitleMusic.CurrentTrack.Id);
        }
    }
}
