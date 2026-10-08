using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Boost Coils' gentle extra cruise, stepped once per physics step: engaged while 07 owns them, the player
    /// holds Drive forward at cruise with the wheel near straight, on flat-ish ground and nothing else is in charge; it
    /// lets go only after the conditions have failed for the release delay, so small bumps never make it flicker. The
    /// boost level eases in and out, and raises the top speed by up to the tuned extra.
    /// </summary>
    public sealed class BoostDrive
    {
        private readonly BoostSettings _settings;
        private float _failing;

        public BoostDrive(BoostSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The boost is on (the level eases toward 1), or off (toward 0).</summary>
        public bool Engaged { get; private set; }

        /// <summary>How much of the boost is in, 0..1 (eased).</summary>
        public float Level { get; private set; }

        /// <summary>Extra top speed (m/s) right now.</summary>
        public float ExtraSpeed => Level * _settings.ExtraSpeed;

        /// <summary>True when <paramref name="sample"/> asks for the boost.</summary>
        public static bool Wants(BoostSettings settings, in BoostSample sample)
        {
            return sample.Owned
                && sample.Free
                && sample.Throttle >= settings.Throttle
                && Mathf.Abs(sample.Steer) <= settings.MaxSteer
                && sample.Slope <= settings.MaxSlope
                && sample.ForwardSpeed >= settings.CruiseFraction * sample.TopSpeed;
        }

        /// <summary>Advances one step; true on the step <see cref="Engaged"/> changes.</summary>
        public bool Step(in BoostSample sample, float deltaTime)
        {
            bool was = Engaged;
            if (Wants(_settings, sample))
            {
                _failing = 0f;
                Engaged = true;
            }
            else if (!sample.Owned || !sample.Free)
            {
                _failing = 0f;
                Engaged = false;
            }
            else if (Engaged)
            {
                _failing += deltaTime;
                Engaged = _failing < _settings.ReleaseDelay;
            }

            float halfLife = Engaged ? _settings.RiseHalfLife : _settings.FallHalfLife;
            Level = Smoothing.Damp(Level, Engaged ? 1f : 0f, halfLife, deltaTime);
            return Engaged != was;
        }
    }
}
