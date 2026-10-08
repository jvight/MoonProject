using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How 07's own controls pick at Kenji's bench (docs/features/M3-11), pure logic, EditMode-tested: a tap of
    /// Interact steps to the next choice, a hold crafts the chosen one, and the Winch (mouse wheel, d-pad up/down)
    /// steps back (up) or on (down), except while something is on the tether, where the Winch reels.
    /// <list type="bullet">
    /// <item>A tap is a press let go within <see cref="TowerPanelSettings.TapSeconds"/>. The hold ring only sees the
    /// press once it outlasts that (<see cref="HoldHeld"/>), so a tap never starts the ring or its sound.</item>
    /// <item>A press that began before the choosing was open (arriving with the button down, or still down from the
    /// last craft) is neither a tap nor a hold: the ring sees it held, so it never arms.</item>
    /// <item>The Winch steps once per notch or d-pad press, and never while Interact is down, so the choice cannot
    /// change under a filling ring.</item>
    /// </list>
    /// </summary>
    internal sealed class BenchPick
    {
        private readonly TowerPanelSettings _settings;
        private bool _wasHeld;
        private bool _pressCounts;
        private float _heldFor;
        private bool _winched;

        public BenchPick(TowerPanelSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>What the hold ring should treat as "held" this step (see the class summary).</summary>
        public bool HoldHeld { get; private set; }

        /// <param name="open">The choices are on screen and can be picked (not fading, not celebrating).</param>
        /// <param name="held">Interact is down.</param>
        /// <param name="winch">The Winch axis, -1..1 (+ up).</param>
        /// <param name="towing">Something is on the tether: the Winch reels instead.</param>
        /// <param name="deltaTime">Unscaled seconds since the last step.</param>
        /// <returns>+1 for the next choice, -1 for the previous one, 0 for none.</returns>
        public int Step(bool open, bool held, float winch, bool towing, float deltaTime)
        {
            int step = 0;
            if (held && !_wasHeld)
            {
                _pressCounts = open;
                _heldFor = 0f;
            }
            else if (held)
            {
                _heldFor += deltaTime;
            }
            else if (_wasHeld && _pressCounts && open && _heldFor < _settings.TapSeconds)
            {
                step = 1;
            }

            _wasHeld = held;
            HoldHeld = held && (!_pressCounts || _heldFor >= _settings.TapSeconds);

            bool winched = !towing && Mathf.Abs(winch) >= _settings.WinchStep;
            if (winched && !_winched && open && !held && step == 0)
            {
                step = winch > 0f ? -1 : 1;
            }

            _winched = winched;
            return step;
        }
    }
}
