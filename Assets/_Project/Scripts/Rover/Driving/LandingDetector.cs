using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Grounded/airborne state machine fed once per physics step with the raw ground-contact result.
    /// <list type="bullet">
    /// <item>Grounded -> Airborne only after contact has been missing for CoyoteTime, so bumpy ground never flickers.
    /// Air time is counted from the first step without contact.</item>
    /// <item>Airborne -> Grounded on the first step with contact. The touchdown is announced when air time and impact
    /// speed both pass their thresholds. Impact speed is the larger of this and the previous step's speed into the
    /// ground, because the contact solver may already have absorbed the impact by the time the probe reports it.</item>
    /// </list>
    /// </summary>
    public sealed class LandingDetector
    {
        private readonly LandingSettings _settings;
        private float _ungroundedTime;
        private float _previousSpeedIntoGround;

        public LandingDetector(LandingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            IsGrounded = true;
        }

        public bool IsGrounded { get; private set; }

        /// <summary>Seconds since contact was lost, once airborne; 0 while grounded (including coyote time).</summary>
        public float AirTime => IsGrounded ? 0f : _ungroundedTime;

        /// <summary>Air time of the most recent touchdown (announced or not).</summary>
        public float LastAirTime { get; private set; }

        /// <summary>Speed into the ground (m/s, positive) of the most recent touchdown.</summary>
        public float LastImpactSpeed { get; private set; }

        /// <summary>Resets to grounded at rest (spawn, teleport).</summary>
        public void Reset()
        {
            IsGrounded = true;
            _ungroundedTime = 0f;
            _previousSpeedIntoGround = 0f;
        }

        /// <summary>
        /// Advances one physics step. <paramref name="speedIntoGround"/> is the velocity component pointing into the
        /// ground (m/s, positive when descending). Returns true when a touchdown worth announcing happened this step;
        /// read <see cref="LastImpactSpeed"/> and <see cref="LastAirTime"/> for its details.
        /// </summary>
        public bool Step(bool hasContact, float speedIntoGround, float deltaTime)
        {
            bool announce = false;

            if (hasContact)
            {
                if (!IsGrounded)
                {
                    LastAirTime = _ungroundedTime;
                    LastImpactSpeed = Mathf.Max(0f, Mathf.Max(speedIntoGround, _previousSpeedIntoGround));
                    announce = LastAirTime >= _settings.MinAirTime && LastImpactSpeed >= _settings.MinImpactSpeed;
                    IsGrounded = true;
                }

                _ungroundedTime = 0f;
            }
            else
            {
                _ungroundedTime += deltaTime;
                if (IsGrounded && _ungroundedTime > _settings.CoyoteTime)
                {
                    IsGrounded = false;
                }
            }

            _previousSpeedIntoGround = speedIntoGround;
            return announce;
        }
    }
}
