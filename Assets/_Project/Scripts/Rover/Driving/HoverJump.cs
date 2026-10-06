using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Hover-Jump, stepped once per physics step:
    /// <list type="bullet">
    /// <item>Ready -> Charging when Jump is pressed on the ground (and the post-landing cooldown is over). The charge
    /// reports progress at its start and at each of ChargeSteps equal steps until full.</item>
    /// <item>Charging -> Ready with a leap when Jump is released: strength eases with the charge (a tap is a small
    /// hop, a full charge a big leap). Leaving the ground while charging, or losing the ability, cancels
    /// quietly.</item>
    /// </list>
    /// The caller reports touchdowns with <see cref="NotifyLanded"/> to start the cooldown.
    /// </summary>
    public sealed class HoverJump
    {
        private readonly HoverJumpSettings _settings;
        private float _held;
        private bool _wasHeld;
        private float _cooldown;
        private int _reportedSteps;

        public HoverJump(HoverJumpSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsCharging { get; private set; }

        /// <summary>Charge progress 0..1 (0 when not charging).</summary>
        public float Charge => IsCharging ? Mathf.Clamp01(_held / _settings.ChargeTime) : 0f;

        /// <summary>Strength of the last charge report or leap, 0..1.</summary>
        public float LastStrength { get; private set; }

        /// <summary>Eased strength (0..1) for a charge: gentle at both ends.</summary>
        public static float StrengthFor(float charge)
        {
            return Smoothing.SmoothStep(0f, 1f, charge);
        }

        /// <summary>Apex height (m) of a leap of <paramref name="strength"/>.</summary>
        public float HeightFor(float strength)
        {
            return Mathf.Lerp(_settings.TapHeight, _settings.FullHeight, Mathf.Clamp01(strength));
        }

        /// <summary>Take-off speed (m/s) reaching <see cref="HeightFor"/> under the rising gravity.</summary>
        public float TakeOffSpeed(float strength, float riseGravity)
        {
            return Mathf.Sqrt(2f * riseGravity * HeightFor(strength));
        }

        public void NotifyLanded()
        {
            _cooldown = _settings.Cooldown;
        }

        public void Reset()
        {
            IsCharging = false;
            _held = 0f;
            _wasHeld = false;
            _cooldown = 0f;
        }

        /// <summary>
        /// Advances one physics step. <paramref name="enabled"/>: 07 owns the ability. Returns what happened; read
        /// <see cref="LastStrength"/> for the charge level reported or the strength of the leap.
        /// </summary>
        public HoverJumpEvent Step(bool enabled, bool held, bool grounded, float deltaTime)
        {
            bool pressed = held && !_wasHeld;
            bool released = !held && _wasHeld;
            _wasHeld = held;

            if (!enabled || !grounded)
            {
                bool wasCharging = IsCharging;
                IsCharging = false;
                return wasCharging ? HoverJumpEvent.Cancelled : HoverJumpEvent.None;
            }

            _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
            if (!IsCharging)
            {
                if (!pressed || _cooldown > 0f)
                {
                    return HoverJumpEvent.None;
                }

                IsCharging = true;
                _held = 0f;
                _reportedSteps = 0;
                LastStrength = 0f;
                return HoverJumpEvent.ChargeProgress;
            }

            if (released)
            {
                LastStrength = StrengthFor(Charge);
                IsCharging = false;
                return HoverJumpEvent.Leap;
            }

            _held += deltaTime;
            int steps = Mathf.FloorToInt(Charge * _settings.ChargeSteps + 1e-4f);
            if (steps > _reportedSteps)
            {
                _reportedSteps = steps;
                LastStrength = (float)steps / _settings.ChargeSteps;
                return HoverJumpEvent.ChargeProgress;
            }

            return HoverJumpEvent.None;
        }

        /// <summary>
        /// Upward velocity change (m/s, never negative) that softens a descent: the fall speed allowed at
        /// <paramref name="height"/> above the ground shrinks so that a steady deceleration brings 07 down at the
        /// cushioned landing speed.
        /// </summary>
        public float CushionVelocityChange(float verticalSpeed, float height)
        {
            if (verticalSpeed >= 0f || height > _settings.CushionProbe)
            {
                return 0f;
            }

            float landing = _settings.CushionLandingSpeed;
            float allowed = Mathf.Sqrt(landing * landing + 2f * _settings.CushionDeceleration * Mathf.Max(0f, height));
            return Mathf.Max(0f, -allowed - verticalSpeed);
        }
    }
}
