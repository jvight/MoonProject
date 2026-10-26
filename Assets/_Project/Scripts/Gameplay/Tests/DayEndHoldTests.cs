using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The opening's day-end beats keep 07 on the pad until it leaves full: the dusk until the cells are full, the
    /// nightfall until the night has fallen and the cells are full; a beat replayed from a save keeps nothing.
    /// </summary>
    public sealed class DayEndHoldTests
    {
        private EventBus _events;
        private DayEndHold _hold;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _hold = new DayEndHold(_events);
        }

        [TearDown]
        public void TearDown()
        {
            _hold.Dispose();
        }

        [Test]
        public void TheDusk_KeepsADocked07_UntilItsCellsAreFull()
        {
            _hold.Step(true, false, false);
            Assert.IsFalse(_hold.Keep, "an ordinary dock keeps nothing");

            _events.Publish(new OpeningDuskBegan(false));
            _hold.Step(true, false, false);
            Assert.IsTrue(_hold.Keep, "the dusk begins on the pad: 07 stays while it charges");
            _hold.Step(true, true, false);
            Assert.IsFalse(_hold.Keep, "full: the pad lets 07 go");
            _hold.Step(true, false, false);
            Assert.IsFalse(_hold.Keep, "once");
        }

        [Test]
        public void TheNightfall_KeepsADocked07_UntilTheNightHasFallen_AndItsCellsAreFull()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            _hold.Step(true, true, false);
            Assert.IsTrue(_hold.Keep, "full, but the night has not fallen: the sky turns with 07 on the pad");
            _hold.Step(true, false, true);
            Assert.IsTrue(_hold.Keep, "night, but not full yet");
            _hold.Step(true, true, true);
            Assert.IsFalse(_hold.Keep, "night and full: the pad lets 07 go");
        }

        [Test]
        public void ABeatReplayedFromASave_KeepsNothing_AndAnUndocked07IsNeverKept()
        {
            _events.Publish(new OpeningDuskBegan(true));
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, true));
            _hold.Step(true, false, false);
            Assert.IsFalse(_hold.Keep, "replayed beats hold nothing");

            _events.Publish(new OpeningDuskBegan(false));
            _hold.Step(false, false, false);
            Assert.IsFalse(_hold.Keep, "off the pad there is nothing to keep");
        }
    }
}
