using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One pooled point light of the feel kit (docs/design/feel-pass.md §4 LightPulse): it eases from where it is to
    /// a target level over a time (ease-in or ease-out), optionally flickers round its level, and switches itself off
    /// at zero so an idle pulse costs nothing.
    /// </summary>
    public sealed class LightPulse
    {
        private const float TwoPi = Mathf.PI * 2f;
        private const float SecondHarmonic = 2.31f;
        private const float SecondShare = 0.35f;

        private readonly Light _light;
        private float _from;
        private float _to;
        private float _seconds;
        private float _elapsed;
        private bool _easeIn;
        private float _flicker;
        private float _flickerRate;
        private float _clock;

        public LightPulse(string name, Transform parent, Color color, float range)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            _light = host.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = color;
            _light.range = range;
            _light.intensity = 0f;
            _light.shadows = LightShadows.None;
            _light.enabled = false;
        }

        /// <summary>The level the pulse is easing toward or holding (before flicker).</summary>
        public float Level { get; private set; }

        public float Intensity => _light.intensity;

        public void MoveTo(Vector3 position)
        {
            _light.transform.position = position;
        }

        /// <summary>Eases to <paramref name="level"/> over <paramref name="seconds"/> (0 = at once).</summary>
        public void Ramp(float level, float seconds, bool easeIn)
        {
            _from = Level;
            _to = level;
            _seconds = seconds;
            _elapsed = 0f;
            _easeIn = easeIn;
            if (seconds <= 0f)
            {
                Level = level;
            }
        }

        /// <summary>
        /// Flickers by <paramref name="share"/> of the level at about <paramref name="rate"/> Hz (0 = steady).
        /// </summary>
        public void Flicker(float share, float rate)
        {
            _flicker = share;
            _flickerRate = rate;
        }

        public void Step(float deltaTime)
        {
            _clock += deltaTime;
            if (_elapsed < _seconds)
            {
                _elapsed += deltaTime;
                float t = Mathf.Clamp01(_elapsed / _seconds);
                float eased = _easeIn ? t * t : 1f - (1f - t) * (1f - t);
                Level = Mathf.Lerp(_from, _to, eased);
            }

            float phase = TwoPi * _flickerRate * _clock;
            float wobble = Mathf.Sin(phase) + SecondShare * Mathf.Sin(phase * SecondHarmonic);
            float intensity = Level * (1f + _flicker * wobble / (1f + SecondShare));
            _light.intensity = Mathf.Max(0f, intensity);
            _light.enabled = intensity > 0f;
        }
    }
}
