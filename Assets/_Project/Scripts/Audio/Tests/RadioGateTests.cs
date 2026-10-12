using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    /// <summary>Music is earned (M3-24): static, then 07's tape deck, then Bell's station; the tower only clears
    /// the static into the carrier.</summary>
    public sealed class RadioGateTests
    {
        [Test]
        public void AFreshGame_HearsOnlyStatic()
        {
            var gate = new RadioGate();
            Assert.AreEqual(RadioGateStage.Static, gate.Stage);
            Assert.IsFalse(gate.CarrierHeard);
        }

        [Test]
        public void TowerLevelOne_ClearsTheStaticIntoTheCarrier_WithoutMusic()
        {
            var gate = new RadioGate();
            Assert.IsTrue(gate.ClearCarrier());
            Assert.IsFalse(gate.ClearCarrier(), "only the first time counts");
            Assert.AreEqual(RadioGateStage.Static, gate.Stage, "the tower earns no music");
            Assert.IsTrue(gate.CarrierHeard);
        }

        [Test]
        public void TheTapeDeck_EarnsTheFirstTape_AndTheCarrierMakesWay()
        {
            var gate = new RadioGate();
            gate.ClearCarrier();
            Assert.IsTrue(gate.FitDeck());
            Assert.IsFalse(gate.FitDeck(), "a second fit changes nothing");
            Assert.AreEqual(RadioGateStage.Deck, gate.Stage);
            Assert.IsFalse(gate.CarrierHeard, "music replaces the carrier");
        }

        [Test]
        public void BellsDial_BringsTheStation_WithOrWithoutTheDeck()
        {
            var withDeck = new RadioGate();
            withDeck.FitDeck();
            Assert.IsTrue(withDeck.SetDialUnlocked(true));
            Assert.AreEqual(RadioGateStage.Station, withDeck.Stage);

            var withoutDeck = new RadioGate();
            Assert.IsTrue(withoutDeck.SetDialUnlocked(true));
            Assert.AreEqual(RadioGateStage.Station, withoutDeck.Stage);
            Assert.IsFalse(withoutDeck.FitDeck(), "the deck after the station changes what is heard no more");
        }

        [Test]
        public void TheGate_NeverClosesAgain()
        {
            var gate = new RadioGate();
            gate.SetDialUnlocked(true);
            Assert.IsFalse(gate.SetDialUnlocked(false));
            Assert.AreEqual(RadioGateStage.Station, gate.Stage);
        }
    }
}
