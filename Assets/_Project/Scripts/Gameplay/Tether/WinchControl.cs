using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tether length. Winch input (a scroll notch for one frame, or a held d-pad every frame) moves a requested
    /// length that may lead the actual one by a small amount; the actual length follows at the reel speed. So a notch
    /// eases the relic in by a step, a held button reels steadily, and letting go never overshoots.
    /// </summary>
    public sealed class WinchControl
    {
        private readonly float _min;
        private readonly float _max;
        private readonly float _step;
        private readonly float _speed;
        private readonly float _lead;

        public WinchControl(float min, float max, float step, float speed, float lead)
        {
            _min = Mathf.Max(0f, min);
            _max = Mathf.Max(_min, max);
            _step = step;
            _speed = speed;
            _lead = lead;
            Reset(_min);
        }

        /// <summary>Current tether length (m).</summary>
        public float Length { get; private set; }

        /// <summary>Length the winch is heading for (m).</summary>
        public float Requested { get; private set; }

        /// <summary>Starts at the relic's distance (clamped to the winch range) when the tether latches.</summary>
        public void Reset(float distance)
        {
            Length = Mathf.Max(_min, distance);
            Requested = Mathf.Clamp(distance, _min, _max);
        }

        /// <param name="input">Reel in (+) / out (-), -1..1, as InputReader.Winch reports it.</param>
        public void Step(float input, float deltaTime)
        {
            if (input != 0f)
            {
                float requested = Requested - input * _step;
                Requested = Mathf.Clamp(Mathf.Clamp(requested, Length - _lead, Length + _lead), _min, _max);
            }

            Length = Mathf.MoveTowards(Length, Requested, _speed * deltaTime);
        }
    }
}
