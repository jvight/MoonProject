using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// <see cref="IRoverStillness"/>: seconds 07 has been resting. A frame is still when 07 is grounded, slower than
    /// the tuned speed, the drive input is inside its dead zone, there is no look input beyond sensor noise, and
    /// nothing is happening to 07 (no recovery lift, no Hover-Jump charge, no interaction holding it parked). Any other
    /// frame resets the count to 0. Stepped once per rendered frame by <see cref="RoverController"/>; while the game is
    /// paused (no game time) the count holds.
    /// </summary>
    public sealed class RoverStillness : IRoverStillness
    {
        private readonly StillnessSettings _settings;

        public RoverStillness(StillnessSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public float StillSeconds { get; private set; }

        /// <summary>True when <paramref name="sample"/> describes a resting 07 and idle hands.</summary>
        public static bool IsStill(StillnessSettings settings, in StillnessSample sample)
        {
            float driveZone = settings.DriveDeadZone;
            float mouseZone = settings.LookMouseDeadZone;
            float stickZone = settings.LookStickDeadZone;
            return sample.Grounded
                && !sample.Engaged
                && sample.Speed < settings.MaxSpeed
                && sample.Drive.sqrMagnitude <= driveZone * driveZone
                && sample.LookDelta.sqrMagnitude <= mouseZone * mouseZone
                && sample.LookStick.sqrMagnitude <= stickZone * stickZone;
        }

        public void Step(in StillnessSample sample, float deltaTime)
        {
            StillSeconds = IsStill(_settings, sample) ? StillSeconds + Mathf.Max(deltaTime, 0f) : 0f;
        }

        /// <summary>Back to 0, as if 07 had just moved (it was placed somewhere else).</summary>
        public void Reset()
        {
            StillSeconds = 0f;
        }
    }
}
