using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The 0.5.2 feel beats in sound: the cut, the dock, 07's chirps and the first hour's small moments.</summary>
    public sealed class FeelAudioTests
    {
        private static readonly Vector3 Point = new Vector3(3f, 0.5f, 4f);
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

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        private void Beat(FeelKind kind, FeelPhase phase, float strength = 0f, float seconds = 0f)
        {
            _rig.Events.Publish(new FeelBeat(kind, phase, Point, Vector3.up, strength, seconds));
        }

        [UnityTest]
        public IEnumerator TheCut_ChargesIgnitesSizzlesCracksAndStraps()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate, 0f, 0.65f);
            Assert.AreEqual("cut_charge_fresh", LastClip, "four glassy notes over a fresh charge");
            Beat(FeelKind.Cut, FeelPhase.Anticipate, 0f, 0.3f);
            Assert.AreEqual("cut_charge_resume", LastClip, "two over a resume");

            Beat(FeelKind.Cut, FeelPhase.Act);
            Assert.AreEqual("cut_ignite", LastClip);
            yield return new WaitForSeconds(0.6f);
            float early = _rig.Feel.SizzleLevel;
            Assert.Greater(early, 0f, "the beam sizzles through 07's body");
            Beat(FeelKind.Cut, FeelPhase.Act, 0.9f);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(_rig.Feel.SizzleLevel, early, "louder with progress");

            Beat(FeelKind.Cut, FeelPhase.Impact);
            Assert.AreEqual("cut_crack", LastClip);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(_rig.Feel.SizzleLevel, 0.05f, "the beam's sizzle lets go");

            Beat(FeelKind.Cut, FeelPhase.Follow, 0f);
            Assert.AreEqual("cut_crack", LastClip, "the tumble is silent");
            Beat(FeelKind.Cut, FeelPhase.Follow, 1f);
            Assert.AreEqual("strap_tuk", LastClip, "the fold straps the bundle");
            Beat(FeelKind.Cut, FeelPhase.Cancel);
            Assert.AreEqual("cut_cancel", LastClip);
        }

        [Test]
        public void TheDock_RisesClampsSettlesAndDrops()
        {
            Beat(FeelKind.Dock, FeelPhase.Anticipate);
            Assert.AreEqual("dock_walls_rise", LastClip);
            Beat(FeelKind.Dock, FeelPhase.Impact);
            Assert.AreEqual("dock_clamp", LastClip);
            Beat(FeelKind.Dock, FeelPhase.Reward);
            Assert.AreEqual("dock_walls_settle", LastClip);
            Beat(FeelKind.Dock, FeelPhase.Cancel);
            Assert.AreEqual("dock_walls_drop", LastClip);
        }

        [UnityTest]
        public IEnumerator TheTower_HumsRingsUpThoomsAndTheRadioComesBackIn()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f);
            float before = _rig.LoudestDeck().volume;
            Beat(FeelKind.Build, FeelPhase.Anticipate, 0f, 1.2f);
            Assert.AreEqual("tower_hum_rise", LastClip);
            for (int ring = 1; ring <= 4; ring++)
            {
                Beat(FeelKind.Build, FeelPhase.Act, ring * 0.25f);
                Assert.AreEqual($"tower_ring_0{ring}", LastClip, "one pentatonic step per lamp ring");
            }

            Beat(FeelKind.Build, FeelPhase.Impact);
            Assert.AreEqual("tower_thoom", LastClip);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(_rig.LoudestDeck().volume, before * 0.5f, "the radio steps back for the tower");

            Beat(FeelKind.Build, FeelPhase.Follow, 0f);
            Assert.IsTrue(AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish").isPlaying, "a crackle");
            yield return new WaitForSecondsRealtime(5f);
            Assert.Greater(_rig.LoudestDeck().volume, before * 0.8f, "and the music fades back in");
        }

        [Test]
        public void SavesInABurst_TickOnce()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new GameSaved());
            _rig.Events.Publish(new GameSaved());
            _rig.Events.Publish(new GameSaved());
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount);
            Assert.AreEqual("save_tick", LastClip);
        }

        [Test]
        public void ANewStep_TurnsThePage_ARepeatDoesNot()
        {
            _rig.Events.Publish(new NextStepChanged("test.step.one"));
            Assert.AreEqual("page_tick", LastClip);
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new NextStepChanged("test.step.one"));
            Assert.AreEqual(plays, _rig.Director.PlayCount);
        }

        [Test]
        public void ABigHaul_RollsOnSoftly_AndItsLastUnitAlwaysTicks()
        {
            int plays = _rig.Director.PlayCount;
            const int units = 30;
            for (int i = 0; i < units; i++)
            {
                _rig.Events.Publish(new StockCountTicked(SalvageMaterial.Metal, i, units - 1 - i));
            }

            int ticks = _rig.Director.PlayCount - plays;
            Assert.AreEqual(2, ticks, "within one frame: the first unit and the last one");
            StringAssert.StartsWith("count_tick", LastClip);
        }

        [Test]
        public void EachWindowStep_ClicksOnce()
        {
            int plays = _rig.Director.PlayCount;
            for (int step = 1; step <= 4; step++)
            {
                _rig.Events.Publish(new HomeWindowLit(step, 4));
            }

            Assert.AreEqual(plays + 4, _rig.Director.PlayCount);
            Assert.AreEqual("window_click", LastClip);
        }

        [UnityTest]
        public IEnumerator AChirp_PublishesEachSyllable_InOrder()
        {
            var syllables = new List<RoverChirpSyllable>();
            using (_rig.Events.Subscribe<RoverChirpSyllable>(syllables.Add))
            {
                _rig.Events.Publish(new RoverChirpRequested(ChirpMood.Proud, 0.5f));
                yield return new WaitForSecondsRealtime(1.5f);
            }

            Assert.AreEqual(5, syllables.Count, "Proud is a five-note flourish");
            for (int i = 0; i < syllables.Count; i++)
            {
                Assert.AreEqual(ChirpMood.Proud, syllables[i].Mood);
                Assert.AreEqual(i, syllables[i].Index);
                Assert.AreEqual(syllables.Count, syllables[i].Count);
            }

            Assert.Greater(syllables[4].Pitch, syllables[0].Pitch, "it rises");
            Assert.IsFalse(_rig.Chirps.IsSpeaking);
        }

        [UnityTest]
        public IEnumerator AChirp_WaitsForAVoiceLine()
        {
            _rig.Events.Publish(new TickerLineShown(AudioTestRig.VoiceKey));
            int heard = 0;
            using (_rig.Events.Subscribe<RoverChirpSyllable>(s => heard++))
            {
                _rig.Events.Publish(new RoverChirpRequested(ChirpMood.Affirm, 0f));
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.AreEqual(0, heard, "never over a voice");
                yield return new WaitForSecondsRealtime(AudioTestRig.VoiceSeconds + 0.5f);
            }

            Assert.AreEqual(2, heard, "it speaks once the voice is done");
        }
    }
}
