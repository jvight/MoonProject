using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// "Over there": while the most relevant action's first-use chip stands on 07 because its place is off screen
    /// (<see cref="InteractionHint.OnRover"/>), 07 turns its head toward that place, and the first time each kind of chip
    /// does so, 07 asks with a short chirp. The chip says which key, 07's look says where. Stepped once a frame;
    /// allocation-free.
    /// </summary>
    public sealed class FirstUsePointer
    {
        private readonly EventBus _events;
        private readonly InteractionHints _hints;
        private readonly IRoverRig _rig;
        private readonly int _gazePriority;
        private readonly float _askIntensity;
        private readonly bool[] _asked;
        private bool _looking;

        /// <param name="gazePriority">The head turn's priority on 07's gaze (below a scan's aim).</param>
        /// <param name="askIntensity">How strongly 07 asks (0..1).</param>
        public FirstUsePointer(EventBus events, InteractionHints hints, IRoverRig rig, int gazePriority,
            float askIntensity)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _rig = rig ?? throw new ArgumentNullException(nameof(rig));
            _gazePriority = gazePriority;
            _askIntensity = askIntensity;
            int kinds = 0;
            foreach (InteractionKind kind in Enum.GetValues(typeof(InteractionKind)))
            {
                kinds = Math.Max(kinds, (int)kind + 1);
            }

            _asked = new bool[kinds];
        }

        /// <summary>07 looks where the chip on it points, or stops looking once it no longer points.</summary>
        public void Step()
        {
            InteractionHint hint = _hints.Primary;
            if (!hint.OnRover)
            {
                if (_looking)
                {
                    _rig.ClearGazeTarget(this);
                    _looking = false;
                }

                return;
            }

            _rig.SetGazeTarget(this, hint.Target, _gazePriority);
            _looking = true;
            if (!_asked[(int)hint.Kind])
            {
                _asked[(int)hint.Kind] = true;
                _events.Publish(new RoverChirpRequested(ChirpMood.Ask, _askIntensity));
            }
        }
    }
}
