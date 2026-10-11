using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>Power comes back through the tower (docs/features/M3-16): L1 home, L2 the bay, L3 the lift.</summary>
    public sealed class BasePowerTests
    {
        private readonly List<BasePowerChanged> _changes = new List<BasePowerChanged>();
        private EventBus _events;
        private BasePower _power;

        [SetUp]
        public void SetUp()
        {
            _changes.Clear();
            _events = new EventBus();
            _events.Subscribe<BasePowerChanged>(_changes.Add);
            _power = new BasePower(_events);
        }

        [TestCase(0, BasePowerStage.Asleep)]
        [TestCase(1, BasePowerStage.Home)]
        [TestCase(2, BasePowerStage.Bay)]
        [TestCase(3, BasePowerStage.Chart)]
        [TestCase(4, BasePowerStage.Chart)]
        public void EachTowerLevel_PowersItsStage(int level, BasePowerStage stage)
        {
            Assert.AreEqual(stage, BasePower.StageFor(level));
        }

        [Test]
        public void AFreshGame_IsAsleep_AndIsAnnouncedAsRestored()
        {
            _power.Announce();

            Assert.AreEqual(BasePowerStage.Asleep, _power.Stage);
            Assert.IsFalse(_power.HasReached(BasePowerStage.Home));
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(BasePowerStage.Asleep, _changes[0].Stage);
            Assert.IsTrue(_changes[0].Restored);
        }

        [Test]
        public void ALevelLandingInPlay_WakesItsStageOnce()
        {
            _power.Show(0);
            _power.Show(1);
            _power.Show(1);
            _power.Show(2);

            Assert.AreEqual(2, _changes.Count, "one wake per new stage, nothing while it stays");
            Assert.AreEqual(BasePowerStage.Home, _changes[0].Stage);
            Assert.AreEqual(BasePowerStage.Bay, _changes[1].Stage);
            Assert.IsFalse(_changes[1].Restored);
            Assert.IsTrue(_power.HasReached(BasePowerStage.Home));
            Assert.IsTrue(_power.HasReached(BasePowerStage.Bay));
            Assert.IsFalse(_power.HasReached(BasePowerStage.Chart));
        }

        [Test]
        public void ALoadedTower_IsRestoredSilently_ThenAnnouncedOnce()
        {
            _power.Restore(2);
            Assert.IsEmpty(_changes, "no wake moment for a loaded stage");
            _power.Show(2);
            Assert.IsEmpty(_changes);

            _power.Announce();

            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(BasePowerStage.Bay, _changes[0].Stage);
            Assert.IsTrue(_changes[0].Restored);
        }
    }
}
