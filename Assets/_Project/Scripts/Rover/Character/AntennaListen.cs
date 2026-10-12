using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The antenna's part in 07's listening (docs/design/first-hour-v2.md §3): the tip lights cyan once the tower is
    /// awake (eased in, or at once on a load), and each listen lifts the antenna (ease out, hold, ease back) while the
    /// tip blinks softly. The call sign keeps playing on top, in the tip's colour. Pure logic.
    /// </summary>
    public sealed class AntennaListen
    {
        private readonly AntennaListenSettings _settings;
        private bool _awake;
        private float _since = float.PositiveInfinity;

        public AntennaListen(AntennaListenSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>How lit the tip is, 0 (asleep) to 1 (the tower is awake).</summary>
        public float Lit { get; private set; }

        /// <summary>Metres the antenna is raised along its own axis now.</summary>
        public float Lift { get; private set; }

        /// <summary>The listen blink now, 0 to 1.</summary>
        public float Blink { get; private set; }

        /// <param name="awake">The tower has reached level 1.</param>
        /// <param name="instantly">A loaded game: the tip is simply lit, with no wake moment.</param>
        public void SetAwake(bool awake, bool instantly)
        {
            _awake = awake;
            if (instantly)
            {
                Lit = awake ? 1f : 0f;
            }
        }

        /// <summary>07 listens: the antenna lifts and the tip blinks from now.</summary>
        public void Listen()
        {
            _since = 0f;
        }

        public void Step(float deltaTime)
        {
            Lit = Smoothing.Damp(Lit, _awake ? 1f : 0f, _settings.LitHalfLife, deltaTime);
            _since += deltaTime;
            Lift = _settings.LiftHeight * LiftShape(_since);
            Blink = BlinkShape(_since);
        }

        /// <summary>
        /// The tip's emission multiplier: the call sign's <paramref name="callSignGlow"/>, the steady lit glow and the
        /// blink, tinted from white toward cyan as the tip lights.
        /// </summary>
        public Color TipEmission(float callSignGlow)
        {
            float glow = callSignGlow + Lit * _settings.LitGlow + Blink * _settings.BlinkGlow;
            Color tint = Color.Lerp(Color.white, _settings.LitTint, Lit);
            return new Color(tint.r * glow, tint.g * glow, tint.b * glow, 1f);
        }

        private float LiftShape(float time)
        {
            float rise = _settings.LiftRise;
            if (time < rise)
            {
                float t = 1f - time / rise;
                return 1f - t * t * t;
            }

            float fallStart = rise + _settings.LiftHold;
            if (time < fallStart)
            {
                return 1f;
            }

            return 1f - Smoothing.SmoothStep(fallStart, fallStart + _settings.LiftFall, time);
        }

        private float BlinkShape(float time)
        {
            float period = _settings.BlinkPeriod;
            if (time >= period * _settings.BlinkCount)
            {
                return 0f;
            }

            float s = Mathf.Sin(Mathf.PI * (time % period) / period);
            return s * s;
        }
    }
}
