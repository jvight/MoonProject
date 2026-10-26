using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's two day-end beats happen on the pad (opening-v3 balance, GD's re-derivation): the first dusk begins
    /// as 07 docks after power-on, and the first nightfall plays as 07 docks with the base night-ready. Each holds 07 on
    /// the pad, its dock walls locked: the dusk until 07's cells are full, the nightfall until night has fallen and the
    /// cells are full, so 07 leaves both full. A beat replayed from a save holds nothing. Plain logic.
    /// </summary>
    public sealed class DayEndHold : IDisposable
    {
        private readonly IDisposable _dusk;
        private readonly IDisposable _milestones;
        private bool _duskHeld;
        private bool _nightHeld;

        public DayEndHold(EventBus events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _dusk = events.Subscribe<OpeningDuskBegan>(began => _duskHeld |= !began.Restored);
            _milestones = events.Subscribe<OpeningMilestoneReached>(OnMilestone);
        }

        /// <summary>The pad keeps 07: a day-end beat runs while 07 is docked.</summary>
        public bool Keep { get; private set; }

        /// <param name="docked">07 rests docked on a pad.</param>
        /// <param name="cellsFull">07's cells are full.</param>
        /// <param name="nightFallen">The first night has fallen.</param>
        public void Step(bool docked, bool cellsFull, bool nightFallen)
        {
            _duskHeld &= !cellsFull;
            _nightHeld &= !(cellsFull && nightFallen);
            Keep = docked && (_duskHeld || _nightHeld);
        }

        public void Dispose()
        {
            _dusk.Dispose();
            _milestones.Dispose();
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            _nightHeld |= reached.Milestone == OpeningMilestone.NightReady && !reached.Restored;
        }
    }
}
