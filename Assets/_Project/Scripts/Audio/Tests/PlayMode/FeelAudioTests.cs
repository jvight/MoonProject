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
            yield return null;
            Assert.GreaterOrEqual(whine.pitch, 1f, "on D, gliding on toward A");
            Assert.Less(whine.pitch, 1.1f);
            yield return new WaitForSeconds(0.45f);
            _rig.Events.Publish(new CutChargeStaged(2, 2, false, 0.45f));
            Assert.AreEqual("charge_lock", LastClip, "the full lock's clunk-click");
            yield return null;
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

        [Test]
        public void APart_LiftsThenClunks_AndItsRewardStaysTheMelodys()
        {
            Beat(FeelKind.Collect, FeelPhase.Act);
            Assert.AreEqual("pickup_lift", LastClip);
            Beat(FeelKind.Collect, FeelPhase.Impact);
            Assert.AreEqual("pickup_clunk", LastClip);
            int plays = _rig.Director.PlayCount;
            Beat(FeelKind.Collect, FeelPhase.Reward);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "FeelAudio adds nothing to the pickup's reward");
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
        public void AFullHold_Toks_AndACrewCrate_Latches()
        {
            _rig.Events.Publish(new CargoOverflowed(SalvageMaterial.Metal, 2, Point));
            Assert.AreEqual("cargo_tok", LastClip);
            _rig.Events.Publish(new CrewCrateOpened("site", "ro", SalvageMaterial.Metal, 3, Point));
            Assert.AreEqual("crate_latch", LastClip);
        }

        [UnityTest]
        public IEnumerator ChargingOnThePad_Hums_AndAFullBatteryRingsItsChord()
        {
            _rig.Events.Publish(new BatteryChanged(1, 4, true, false));
            _rig.Events.Publish(new ChargingChanged(true, ChargerKind.Dock, Point));
            yield return new WaitForSeconds(_rig.FeelTuning.ChargeFadeIn + 0.2f);
            AudioSource hum = AudioTestRig.FindChildSource(_rig.Feel.transform, "ChargerHum");
            Assert.IsTrue(hum.isPlaying, "the charger's hum breathes while the cells fill");
            Assert.Greater(_rig.Feel.ChargerHum, 0.9f);

            _rig.Events.Publish(new BatteryChanged(4, 4, true, false));
            _rig.Events.Publish(new ChargingChanged(false, ChargerKind.Dock, Point));
            yield return null;
            Assert.AreEqual("dock_full", LastClip, "the last cell rings the full chord");
            yield return new WaitForSeconds(_rig.FeelTuning.ChargeFadeOut + 0.2f);
            Assert.IsFalse(hum.isPlaying, "and the hum settles away");
        }

        [UnityTest]
        public IEnumerator DrivingOffEarly_LetsTheHumGo_WithoutTheChord()
        {
            _rig.Events.Publish(new BatteryChanged(1, 4, true, false));
            _rig.Events.Publish(new ChargingChanged(true, ChargerKind.Dock, Point));
            yield return new WaitForSeconds(0.5f);
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new ChargingChanged(false, ChargerKind.Dock, Point));
            yield return new WaitForSeconds(_rig.FeelTuning.ChargeFadeOut + 0.2f);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no chord for a half charge");
            Assert.AreEqual(0f, _rig.Feel.ChargerHum, 1e-4f);
        }

        [Test]
        public void TheHoldBoots_InTheDark_SlotByRisingSlot_ThenItsItemsSlideIn()
        {
            _rig.Events.Publish(new HoldBootStarted());
            Assert.AreEqual("hold_boot", LastClip);
            _rig.Events.Publish(new HoldSlotLit(0, 8));
            Assert.AreEqual("hold_slot_01", LastClip);
            _rig.Events.Publish(new HoldSlotLit(7, 8));
            Assert.AreEqual("hold_slot_08", LastClip, "the eighth slot, highest");
            _rig.Events.Publish(new HoldItemArrived(0));
            Assert.AreEqual("item_slide", LastClip);
        }

        [UnityTest]
        public IEnumerator AScan_SweepsStepsUpEachQuarter_AndLiftsOffIntoTheLens()
        {
            AudioSource sweep = AudioTestRig.FindChildSource(_rig.Feel.transform, "ScanSweep");
            Beat(FeelKind.Scan, FeelPhase.Anticipate);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(sweep.isPlaying, "the lens's fan of light hums while it draws");
            Assert.AreEqual(1f, sweep.pitch, 1e-4f, "on D");
            Beat(FeelKind.Scan, FeelPhase.Act, 0.25f);
            Assert.AreEqual("scan_step_01", LastClip);
            Beat(FeelKind.Scan, FeelPhase.Act, 0.5f);
            Assert.AreEqual(1.1225f, sweep.pitch, 1e-4f, "a step up, on E");
            Beat(FeelKind.Scan, FeelPhase.Act, 1f);
            Assert.AreEqual("scan_step_04", LastClip);
            Beat(FeelKind.Scan, FeelPhase.Reward);
            Assert.AreEqual("scan_liftoff", LastClip, "the lines stream into the lens");
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(sweep.isPlaying);
        }

        [UnityTest]
        public IEnumerator ACompletedBlueprint_AnswersAfterTheChirp()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new BlueprintCompleted("rover.hover_jump"));
            yield return new WaitForSeconds(_rig.FeelTuning.SheetCompleteDelay * 0.5f);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "07's Proud chirp leads");
            yield return new WaitForSeconds(_rig.FeelTuning.SheetCompleteDelay);
            Assert.AreEqual("sheet_complete", LastClip, "then the sheet answers");
        }

        [Test]
        public void AContainer_PopsItsSeal_ThenOpensWithItsKindsHinge()
        {
            _rig.Events.Publish(new ContainerSealCut("relay.cabinet", ContainerKind.Cabinet, Point));
            Assert.AreEqual("seal_cut", LastClip);
            _rig.Events.Publish(new ContainerOpened("relay.cabinet", ContainerKind.Cabinet, Point));
            Assert.AreEqual("container_open_cabinet", LastClip);
            _rig.Events.Publish(new ContainerOpened("ro.case", ContainerKind.Case, Point));
            Assert.AreEqual("container_open_case", LastClip);
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

    }
}
