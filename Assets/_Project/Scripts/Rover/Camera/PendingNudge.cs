using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// A next-action nudge (G5) asked for while another shot still had the camera: held, and given back once the
    /// camera is free, unless it has gone stale or another line has replaced it. Without it a line shown the moment a
    /// shot ends (the charge's close shot, the dock) never turned the camera toward its target at all. Pure.
    /// </summary>
    public sealed class PendingNudge
    {
        private NextStepChanged _step;
        private float _heldAt;

        /// <summary>A request is waiting for the camera.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Holds <paramref name="step"/> from <paramref name="time"/>, replacing any request already held.</summary>
        public void Hold(NextStepChanged step, float time)
        {
            _step = step;
            _heldAt = time;
            IsHeld = true;
        }

        /// <summary>Drops the held request (another line came, or the player took the camera).</summary>
        public void Clear()
        {
            IsHeld = false;
        }

        /// <summary>
        /// Gives the held request back when the camera <paramref name="free"/> and it is no older than
        /// <paramref name="maxSeconds"/>; a stale one is dropped quietly.
        /// </summary>
        public bool TryRelease(bool free, float time, float maxSeconds, out NextStepChanged step)
        {
            step = _step;
            if (!IsHeld)
            {
                return false;
            }

            if (time - _heldAt > maxSeconds)
            {
                IsHeld = false;
                return false;
            }

            if (!free)
            {
                return false;
            }

            IsHeld = false;
            return true;
        }
    }
}
