using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Music is earned (docs/features/M3-24): a fresh game's radio plays static only, tower level 1 clears it into the
    /// Earth signal's carrier, 07's tape deck plays the first tape softly, and Bell's dial brings the station.
    /// </summary>
    public sealed class RadioGateAudioTests
    {
        private const float WakeSeconds = 4f;
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig(stationUnlocked: false);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private AudioSource Carrier => AudioTestRig.FindChildSource(_rig.Radio.transform, "Carrier");

        [UnityTest]
        public IEnumerator AFreshGame_HearsStaticOnly_AndTheTowerBringsTheCarrierNotMusic()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(WakeSeconds);
            Assert.AreEqual(RadioGateStage.Static, _rig.Radio.GateStage);
            Assert.IsFalse(_rig.AnyDeckPlaying(), "no music before the tape deck");
            Assert.AreEqual(0f, Carrier.volume, 1e-4f, "no carrier before tower level 1");

            _rig.Events.Publish(new BasePowerChanged(BasePowerStage.Home, false));
            yield return new WaitForSecondsRealtime(WakeSeconds);
            Assert.IsTrue(Carrier.isPlaying);
            Assert.Greater(Carrier.volume, 0f, "the static has cleared into the Earth signal's carrier");
            Assert.IsFalse(_rig.AnyDeckPlaying(), "the tower earns no music");
        }

        [UnityTest]
        public IEnumerator TheTapeDeck_EasesInTheFirstTapeSoftly_AndTheDialIsStillIgnored()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(WakeSeconds);
            _rig.Events.Publish(new PartFitted(RoverSocket.Deck, RoverKitPiece.TapeDeck, false, false, default, 0));
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(RadioGateStage.Deck, _rig.Radio.GateStage);
            Assert.IsTrue(_rig.Radio.MusicStarted, "the first tape plays once the deck is fitted");
            Assert.AreEqual("t1", _rig.Radio.CurrentTrack.Id, "the first tape");
            float early = _rig.LoudestDeck().volume;
            yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(_rig.LoudestDeck().volume, early, "it eases in, slowly");
            Assert.IsTrue(_rig.LoudestDeck().loop, "the one tape plays on repeat");

            _rig.Program.Channel = RadioChannel.QuietHours;
            _rig.ProgramChanged();
            Assert.AreEqual(RadioGateStage.Deck, _rig.Radio.GateStage, "a locked dial changes nothing");
            Assert.AreEqual("t1", _rig.Radio.CurrentTrack.Id);
        }

        [UnityTest]
        public IEnumerator BellsDial_BringsTheStation_WithADialTurn()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(WakeSeconds);
            _rig.Events.Publish(new PartFitted(RoverSocket.Deck, RoverKitPiece.TapeDeck, false, false, default, 0));
            yield return new WaitForSecondsRealtime(1f);

            _rig.Program.DialUnlocked = true;
            _rig.ProgramChanged();
            Assert.AreEqual(RadioGateStage.Station, _rig.Radio.GateStage);
            Assert.IsTrue(AudioTestRig.FindChildSource(_rig.Radio.transform, "TuningSwish").isPlaying,
                "the station comes in with a dial turn");
            yield return new WaitForSecondsRealtime(3f);
            Assert.IsTrue(_rig.Radio.MusicStarted);
            Assert.AreEqual(RadioChannel.LumenAfterDark, _rig.Radio.Station);
        }

        [UnityTest]
        public IEnumerator ALoadedDeck_PlaysWithoutAFitMoment()
        {
            _rig.Dispose();
            _rig = new AudioTestRig(stationUnlocked: false, deckFitted: true);
            yield return null;
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(WakeSeconds);
            Assert.AreEqual(RadioGateStage.Deck, _rig.Radio.GateStage, "read from 07's equipment at boot");
            Assert.IsTrue(_rig.Radio.MusicStarted, "the wake-up finds the deck's tape");
        }

        [UnityTest]
        public IEnumerator TakingTheDeckOff_PausesItsTape()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(WakeSeconds);
            _rig.Events.Publish(new PartFitted(RoverSocket.Deck, RoverKitPiece.TapeDeck, false, false, default, 0));
            yield return new WaitForSecondsRealtime(2f);
            _rig.Events.Publish(new PartRemoved(RoverSocket.Deck, RoverKitPiece.TapeDeck));
            Assert.AreEqual(RadioGateStage.Static, _rig.Radio.GateStage);
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsFalse(_rig.AnyDeckPlaying(), "the tape stops with the deck off");
        }
    }
}
