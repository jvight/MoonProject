using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07 gazing at the stars with the player. While the view looks up and 07 is still, its share of the gaze eases
    /// in (it lifts its head); after a rest the stargazing beat begins, and any drive input, or the view dropping,
    /// ends it.
    /// </summary>
    public sealed class Stargaze
    {
        private readonly RoverCameraTuning _tuning;

        public Stargaze(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>How far up the view looks, 0 (level) to 1 (fully up), on a smooth step.</summary>
        public float LookUp { get; private set; }

        /// <summary>How far 07 joins the gaze to the sky, 0..1, eased in slowly and out quicker.</summary>
        public float Lift { get; private set; }

        /// <summary>Whether the stargazing beat is on.</summary>
        public bool IsActive { get; private set; }

        /// <param name="elevation">The player's orbit elevation (deg).</param>
        /// <param name="stillSeconds">Seconds since 07 last moved or the player gave drive or look input.</param>
        /// <param name="moving">Drive input is held or 07 is moving.</param>
        /// <param name="free">The chase camera is the player's (no moment, wide shot or framed shot).</param>
        /// <returns>True when <see cref="IsActive"/> changed.</returns>
        public bool Step(float elevation, float stillSeconds, bool moving, bool free, float deltaTime)
        {
            float span = Mathf.InverseLerp(_tuning.StargazeStartElevation, _tuning.StargazeFullElevation, elevation);
            LookUp = free ? Smoothing.SmoothStep(0f, 1f, span) : 0f;
            float target = moving ? 0f : LookUp;
            float halfLife = target > Lift ? _tuning.StargazeRiseHalfLife : _tuning.StargazeFallHalfLife;
            Lift = Smoothing.Damp(Lift, target, halfLife, deltaTime);

            bool active = !moving && (IsActive
                ? LookUp >= _tuning.StargazeHoldShare
                : LookUp >= _tuning.StargazeBeginShare && stillSeconds >= _tuning.StargazeDelay);
            if (active == IsActive)
            {
                return false;
            }

            IsActive = active;
            return true;
        }

        /// <summary>Ends the beat and drops the gaze at once (07 was placed). True when the beat was on.</summary>
        public bool Reset()
        {
            bool wasActive = IsActive;
            IsActive = false;
            LookUp = 0f;
            Lift = 0f;
            return wasActive;
        }
    }
}
