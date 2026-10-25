using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// When the first dusk begins (docs/design/opening-v3-payoff-audit.md §4, R5 merged into B8): after power-on the
    /// sky holds in the afternoon wherever 07 roams, and dusk follows progress, never a clock. It begins once, as 07
    /// docks on a pad after power-on, so the day ends on the pad (<see cref="DayEndHold"/> keeps 07 there until full).
    /// Publishes <see cref="OpeningDuskBegan"/> then, and again with Restored at each session start of a game where it
    /// has begun. Plain logic; allocation-free per step.
    /// </summary>
    public sealed class DuskCue : IDisposable
    {
        private readonly EventBus _events;
        private readonly IDisposable _docks;

        // 07 docked since the last step: only a dock while the power is on counts.
        private bool _docked;

        public DuskCue(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _docks = events.Subscribe<RoverDockChanged>(OnDockChanged);
        }

        /// <summary>The first dusk has begun.</summary>
        public bool Begun { get; private set; }

        /// <summary>One frame of the opening: <paramref name="powerOn"/>, a built array powers the pad.</summary>
        public void Step(bool powerOn)
        {
            bool docked = _docked;
            _docked = false;
            if (Begun || !powerOn || !docked)
            {
                return;
            }

            Begun = true;
            _events.Publish(new OpeningDuskBegan(false));
        }

        /// <summary>The session starts (after the save is applied): a dusk already begun is stated, Restored.</summary>
        public void Announce()
        {
            if (Begun)
            {
                _events.Publish(new OpeningDuskBegan(true));
            }
        }

        public void Capture(OpeningSaveData data)
        {
            data.duskBegun = Begun;
        }

        public void Restore(OpeningSaveData data)
        {
            Begun = data.duskBegun;
            _docked = false;
        }

        public void Dispose()
        {
            _docks.Dispose();
        }

        private void OnDockChanged(RoverDockChanged changed)
        {
            _docked |= changed.Docked;
        }
    }
}
