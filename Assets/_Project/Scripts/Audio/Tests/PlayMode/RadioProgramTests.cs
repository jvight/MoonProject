using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The radio following <see cref="IRadioProgram"/>: Bell's dial, the Tape Deck, Quiet Hours and the
    /// tapes joining Lumen After Dark.</summary>
    public sealed class RadioProgramTests
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

        private FakeRadioProgram Program => _rig.Program;

        private AudioSource Swish => AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish");

        [UnityTest]
        public IEnumerator BeforeBellIsRepaired_TheDialIsIgnored_ButANewTapeJoinsTheShow()
        {
            yield return WakeAndWaitForMusic();
            Program.Channel = RadioChannel.QuietHours;
            Program.Own(AudioTestRig.TapeA);
            _rig.ProgramChanged();

            Assert.AreEqual(RadioChannel.LumenAfterDark, _rig.Radio.Station);
            Assert.AreEqual(3, _rig.Radio.ShufflePoolCount, "two base tracks and the new tape");
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(_rig.Radio.MusicStarted, "the music plays on");
            Assert.IsFalse(LiveDeck().loop, "with more than one track the show moves on");
        }

        [UnityTest]
        public IEnumerator TurningTheDial_ToTheTapeDeck_SwishesBellCrackles_AndTheTapeLoops()
        {
            yield return WakeAndWaitForMusic();
            yield return new WaitWhile(() => Swish.isPlaying);
            _rig.Rover.Bell.Activity = FriendActivity.Home;
            Program.DialUnlocked = true;
            Program.Own(AudioTestRig.TapeB);
            Program.SelectedTape = AudioTestRig.TapeB;
            Program.Channel = RadioChannel.TapeDeck;
            _rig.ProgramChanged();

            Assert.IsTrue(Swish.isPlaying, "a short static swish between stations");
            Assert.IsTrue(RecentlyPlayed("bell_tune_"), "Bell crackles along with her dial");
            yield return new WaitForSecondsRealtime(2f);

            Assert.AreEqual(RadioChannel.TapeDeck, _rig.Radio.Station);
            Assert.AreEqual(AudioTestRig.TapeB, _rig.Radio.CurrentTrack.TapeId);
            Assert.IsTrue(LiveDeck().loop, "the chosen tape plays on repeat");
            Assert.Greater(LiveDeck().volume, 0f);
            Assert.AreEqual(1, PlayingDecks(), "the show's deck has faded out and stopped");
        }

        [UnityTest]
        public IEnumerator QuietHours_LetsTheMusicAndStaticGo_AndTheMoonFillsIn_ThenTheShowComesBack()
        {
            yield return WakeAndWaitForMusic();
            AudioSource staticSource = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            Assert.AreEqual(1f, _rig.Ambience.QuietLift);

            Program.DialUnlocked = true;
            Program.Channel = RadioChannel.QuietHours;
            _rig.ProgramChanged();
            yield return new WaitForSecondsRealtime(8f);

            Assert.IsFalse(_rig.Radio.MusicStarted, "no music on Quiet Hours");
            Assert.Less(staticSource.volume, 1e-3f, "and no static either");
            Assert.Greater(_rig.Ambience.QuietLift, 1.2f, "the moon's own sound swells a little");

            float quietLift = _rig.Ambience.QuietLift;
            yield return new WaitWhile(() => Swish.isPlaying);
            Program.Channel = RadioChannel.LumenAfterDark;
            _rig.ProgramChanged();
            Assert.IsTrue(Swish.isPlaying);
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(_rig.Radio.MusicStarted, "the show comes back");
            Assert.AreEqual(string.Empty, _rig.Radio.CurrentTrack.TapeId);
            Assert.Less(_rig.Ambience.QuietLift, quietLift, "and the moon settles back under it");
        }

        [UnityTest]
        public IEnumerator ChangesBeforeTheRadioComesOn_AreSilent_AndItWakesOnTheChosenStation()
        {
            _rig.Rover.Bell.Activity = FriendActivity.Home;
            Program.DialUnlocked = true;
            Program.Own(AudioTestRig.TapeA);
            Program.SelectedTape = AudioTestRig.TapeA;
            Program.Channel = RadioChannel.TapeDeck;
            int plays = _rig.Director.PlayCount;
            _rig.ProgramChanged();

            Assert.IsFalse(Swish.isPlaying, "no swish at load");
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no crackle either");
            yield return WakeAndWaitForMusic();
            Assert.AreEqual(AudioTestRig.TapeA, _rig.Radio.CurrentTrack.TapeId);
            Assert.IsTrue(LiveDeck().loop);
        }

        [UnityTest]
        public IEnumerator AnUnknownTape_IsReported_AndTheDeckFallsSilent()
        {
            yield return WakeAndWaitForMusic();
            LogAssert.Expect(LogType.Error, new Regex("no radio track for cassette 'ghost'"));
            Program.DialUnlocked = true;
            Program.SelectedTape = "ghost";
            Program.Channel = RadioChannel.TapeDeck;
            _rig.ProgramChanged();
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsFalse(_rig.Radio.MusicStarted);
        }

        private IEnumerator WakeAndWaitForMusic()
        {
            _rig.Wake(true);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music after 07 wakes");
        }

        private AudioSource LiveDeck()
        {
            Assert.GreaterOrEqual(_rig.Radio.Mixer.Live, 0, "a deck is live");
            return _rig.Radio.GetDeck(_rig.Radio.Mixer.Live);
        }

        private int PlayingDecks()
        {
            int playing = 0;
            for (int i = 0; i < RadioDeckMixer.DeckCount; i++)
            {
                playing += _rig.Radio.GetDeck(i).isPlaying ? 1 : 0;
            }

            return playing;
        }

        private bool RecentlyPlayed(string clipPrefix)
        {
            for (int i = 0; i < 4; i++)
            {
                AudioClip clip = _rig.Director.RecentClip(i);
                if (clip != null && clip.name.StartsWith(clipPrefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
