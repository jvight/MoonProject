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
        public IEnumerator TheCut_BracesBuildsLocksIgnitesSizzlesCracksAndStraps()
        {
            AudioSource whine = AudioTestRig.FindChildSource(_rig.Feel.transform, "ChargeWhine");
            Beat(FeelKind.Cut, FeelPhase.Anticipate, 0f, 1.4f);
            _rig.Events.Publish(new CutChargeStaged(0, 2, false, 0.45f));
            Assert.AreEqual("charge_brace", LastClip, "07 plants itself first");
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(whine.isPlaying, "the whine waits out the brace");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(whine.isPlaying, "then the capacitor whine rises");
            _rig.Events.Publish(new CutChargeStaged(1, 2, false, 0.45f));
            Assert.AreEqual("charge_detent", LastClip, "a stage locks with a detent click");
            Assert.AreEqual(1f, whine.pitch, 1e-3f, "on D");
            yield return new WaitForSeconds(0.45f);
            _rig.Events.Publish(new CutChargeStaged(2, 2, false, 0.45f));
            Assert.AreEqual("charge_lock", LastClip, "the full lock's clunk-click");
            Assert.AreEqual(1.4983f, whine.pitch, 1e-3f, "held on A through the breath");

            _rig.Rover.Cutter.Tier = 3;
            Beat(FeelKind.Cut, FeelPhase.Act);
            Assert.AreEqual("cut_ignite_tier_tier3", LastClip, "the hottest ignition at tier 3");
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
        }

        [UnityTest]
        public IEnumerator AWarmCharge_SkipsTheBrace_AndARelease_DrainsTheWhine()
        {
            AudioSource whine = AudioTestRig.FindChildSource(_rig.Feel.transform, "ChargeWhine");
            int plays = _rig.Director.PlayCount;
            Beat(FeelKind.Cut, FeelPhase.Anticipate, 0f, 0.27f);
            _rig.Events.Publish(new CutChargeStaged(0, 1, true, 0.27f));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no servo brace on a warm emitter");
            yield return null;
            Assert.IsTrue(whine.isPlaying, "the whine starts at once");
            Beat(FeelKind.Cut, FeelPhase.Cancel);
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(whine.isPlaying, "it glides down and fades");
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
        public void APart_LiftsThenClunks_AndTheBundlesRewardStaysTheMelodys()
        {
            Beat(FeelKind.Collect, FeelPhase.Act);
            Assert.AreEqual("pickup_lift", LastClip);
            Beat(FeelKind.Collect, FeelPhase.Impact);
            Assert.AreEqual("pickup_clunk", LastClip);
            int plays = _rig.Director.PlayCount;
            Beat(FeelKind.Collect, FeelPhase.Reward);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "FeelAudio adds nothing to the bundle's reward");
        }

        [Test]
        public void ASegmentChange_TicksAndSlidesUpAStep()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate, 0f, 0.65f);
            Beat(FeelKind.Cut, FeelPhase.Follow, 0.5f);
            Assert.AreEqual("segment_slide_02", LastClip);
            Beat(FeelKind.Cut, FeelPhase.Follow, 0.5f);
            Assert.AreEqual("segment_slide_03", LastClip);
        }

        [Test]
        public void CargoAndStorage_SoundTheirMoments()
        {
            _rig.Events.Publish(new CargoOverflowed(SalvageMaterial.Metal, 2, Point));
            Assert.AreEqual("cargo_tok", LastClip);
            Beat(FeelKind.Cargo, FeelPhase.Act, 0f);
            Assert.AreEqual("count_tick_01", LastClip, "the first crate on the belt");
            Beat(FeelKind.Cargo, FeelPhase.Act, 1f);
            Assert.AreEqual("count_tick_06", LastClip, "the last one, highest");
            Beat(FeelKind.Cargo, FeelPhase.Reward, 1f);
            Assert.AreEqual("node_light_01", LastClip, "the hold empty: a bell on D");
            int plays = _rig.Director.PlayCount;
            Beat(FeelKind.Cargo, FeelPhase.Impact, 1f);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "a full hold's Impact is CargoOverflowed's tok");
            Beat(FeelKind.Build, FeelPhase.Impact);
            Assert.AreEqual("shelf_chord", LastClip, "a lone Build Impact is a shelf built");
            Beat(FeelKind.Build, FeelPhase.Act, 0.5f);
            Assert.AreEqual("shelf_chord", LastClip, "no tower ring outside the tower's chain");
            _rig.Events.Publish(new CrewCrateOpened("site", "ro", SalvageMaterial.Metal, 3, Point));
            Assert.AreEqual("crate_latch", LastClip);
            _rig.Events.Publish(new BundleFetched(SalvageMaterial.Metal, 2));
            Assert.AreEqual("tilly_fetch", LastClip);
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
