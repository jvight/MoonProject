using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The cargo hatch's swing as plain logic (M3-25 T1): it opens now or after a delay (a bundle's flight less the
    /// lead), eases on a spring, and on shutting meets the frame with a small bounce, the soft thunk
    /// (<see cref="Step"/> says when). A bump shakes it a few degrees. Angles in degrees, 0 = shut, + = open.
    /// </summary>
    public sealed class CargoHatchMotion
    {
        private readonly CargoHatchSettings _settings;
        private DampedSpring _swing;
        private DampedSpring _shake;
        private float _target;
        private float _openIn = -1f;

        public CargoHatchMotion(CargoHatchSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The hatch's angle now (deg, 0 = shut), shake included.</summary>
        public float Angle => Mathf.Max(0f, _swing.Value) + _shake.Value;

        /// <summary>Whether the hatch is open or opening (or about to).</summary>
        public bool IsOpening => _target > 0f || _openIn >= 0f;

        /// <summary>Opens once <paramref name="seconds"/> less the lead have passed (at once when that is past).</summary>
        public void OpenBefore(float seconds)
        {
            _openIn = Mathf.Max(0f, seconds - _settings.OpenLead);
        }

        public void Open()
        {
            _openIn = -1f;
            _target = _settings.OpenAngle;
        }

        /// <summary>Swings shut, aiming a little past the frame so it meets it moving (the thunk), then rests.</summary>
        public void Close()
        {
            _openIn = -1f;
            _target = _swing.Value > 0f ? -_settings.ClosePush : 0f;
        }

        /// <summary>Something bumps the hatch: a short shake.</summary>
        public void Bump()
        {
            _shake.AddVelocity(_settings.BumpShake * 2f * Mathf.PI * _settings.ShakeFrequency);
        }

        /// <summary>Back shut at once (07 was placed, a game loaded).</summary>
        public void Reset()
        {
            _openIn = -1f;
            _target = 0f;
            _swing.Reset(0f);
            _shake.Reset(0f);
        }

        /// <returns>True on the step the closing hatch meets its frame (the thunk).</returns>
        public bool Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            if (_openIn >= 0f)
            {
                _openIn -= step;
                if (_openIn < 0f)
                {
                    Open();
                }
            }

            _swing.Step(_target, _settings.SwingFrequency, _settings.SwingDamping, step);
            _shake.Step(0f, _settings.ShakeFrequency, _settings.ShakeDamping, step);
            if (_swing.Value >= 0f || _swing.Velocity >= 0f)
            {
                return false;
            }

            float speed = -_swing.Velocity;
            _target = 0f;
            _swing.Reset(0f);
            _swing.AddVelocity(speed * _settings.ThunkBounce);
            return speed >= _settings.ThunkMinSpeed;
        }
    }
}
