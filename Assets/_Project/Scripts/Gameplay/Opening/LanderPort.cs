using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The landed lander's service port and its old cells (docs/design/opening-v2.md B2, B6, §8 decision 2): 07
    /// reaching the lander, and the one charge the cells still hold, drawn through a built pad on the port's cable
    /// until it is spent (<see cref="LanderReserveChanged"/>, the CRT's <c>LANDER RESERVE · n%</c>). Plain state,
    /// saved with the opening.
    /// </summary>
    public sealed class LanderPort
    {
        private readonly EventBus _events;
        private readonly float _fullCells;
        private float _lastPublished = -1f;

        public LanderPort(EventBus events, float fullCells)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (fullCells <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fullCells), fullCells, "The reserve needs some charge.");
            }

            _fullCells = fullCells;
            ReserveCells = fullCells;
        }

        /// <summary>07 has come within the lander's reach this save.</summary>
        public bool Reached { get; private set; }

        public float ReserveCells { get; private set; }

        public float Reserve01 => ReserveCells / _fullCells;

        /// <summary>Some of the reserve has been drawn: 07 has charged on its own pad (B6).</summary>
        public bool Drawn => ReserveCells < _fullCells;

        public bool Spent => ReserveCells <= 0f;

        public void Reach()
        {
            Reached = true;
        }

        /// <summary>A pad on the port's cable drew <paramref name="cells"/>; returns the cells it actually got.</summary>
        public float Draw(float cells)
        {
            float drawn = Mathf.Clamp(cells, 0f, ReserveCells);
            ReserveCells -= drawn;
            if (ReserveCells <= 0f)
            {
                ReserveCells = 0f;
            }

            Publish(false);
            return drawn;
        }

        /// <summary>
        /// The charge the reserve fed has ended (07 full, or it drove off): the old cells give out with it, whatever
        /// is left (opening-v2 §8 decision 2: one charge, then the base needs its own power).
        /// </summary>
        public void GiveOut()
        {
            if (ReserveCells > 0f)
            {
                ReserveCells = 0f;
                Publish(false);
            }
        }

        /// <summary>The session starts: the reserve is stated once (Restored), so the CRT starts from it.</summary>
        public void Announce()
        {
            _lastPublished = -1f;
            Publish(true);
        }

        public void Capture(OpeningSaveData data)
        {
            data.reachedLander = Reached;
            data.reserveCells = ReserveCells;
        }

        public void Restore(OpeningSaveData data)
        {
            Reached = data.reachedLander;
            ReserveCells = data.reserveCells < 0f ? _fullCells : Mathf.Min(data.reserveCells, _fullCells);
        }

        /// <summary>Publishes on whole-percent changes only, so a draw over many frames reads as steps.</summary>
        private void Publish(bool restored)
        {
            float percent = Mathf.Ceil(Reserve01 * 100f);
            if (!restored && Mathf.Approximately(percent, _lastPublished))
            {
                return;
            }

            _lastPublished = percent;
            _events.Publish(new LanderReserveChanged(percent / 100f, restored));
        }
    }
}
