using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The lab's turntable as plain logic (docs/features/M3-19 §5): the platform's world yaw, turned softly from stop
    /// to stop, eased at both ends, taking longer the further it turns.
    /// </summary>
    public sealed class Turntable
    {
        private const float QuarterTurn = 90f;

        /// <summary>A turn smaller than this (deg) is already there.</summary>
        private const float MinTurn = 0.01f;

        private readonly float _secondsPerQuarter;
        private readonly float _minSeconds;
        private float _from;
        private float _delta;
        private float _elapsed;
        private float _duration;

        /// <param name="secondsPerQuarter">Seconds a quarter turn takes.</param>
        /// <param name="minSeconds">Seconds the shortest turn still takes.</param>
        public Turntable(float secondsPerQuarter, float minSeconds)
        {
            _secondsPerQuarter = Mathf.Max(0f, secondsPerQuarter);
            _minSeconds = Mathf.Max(0f, minSeconds);
        }

        /// <summary>The platform's world yaw (deg, 0..360).</summary>
        public float Yaw { get; private set; }

        /// <summary>True while it turns toward a stop.</summary>
        public bool Turning => _duration > 0f;

        /// <summary>Rests at <paramref name="yaw"/> at once, forgetting any turn.</summary>
        public void Snap(float yaw)
        {
            Yaw = Mathf.Repeat(yaw, 360f);
            _duration = 0f;
        }

        /// <summary>
        /// Turns from where it is to <paramref name="yaw"/>: clockwise seen from above when
        /// <paramref name="direction"/> is positive, counter-clockwise when negative, the short way round when 0.
        /// False when there is nothing to turn (it rests there at once).
        /// </summary>
        public bool TurnTo(float yaw, int direction)
        {
            float delta = Delta(Yaw, yaw, direction);
            float duration = Mathf.Max(_minSeconds, _secondsPerQuarter * Mathf.Abs(delta) / QuarterTurn);
            if (Mathf.Abs(delta) < MinTurn || duration <= 0f)
            {
                Snap(yaw);
                return false;
            }

            _from = Yaw;
            _delta = delta;
            _elapsed = 0f;
            _duration = duration;
            return true;
        }

        /// <summary>Moves the turn on; true on the step it comes to rest at its stop.</summary>
        public bool Step(float deltaTime)
        {
            if (!Turning)
            {
                return false;
            }

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            Yaw = Mathf.Repeat(_from + _delta * (t * t * (3f - 2f * t)), 360f);
            if (t < 1f)
            {
                return false;
            }

            Snap(_from + _delta);
            return true;
        }

        /// <summary>
        /// The signed turn (deg) from <paramref name="from"/> to <paramref name="to"/> in a direction.
        /// </summary>
        public static float Delta(float from, float to, int direction)
        {
            float shortest = Mathf.DeltaAngle(from, to);
            if (direction > 0 && shortest < 0f)
            {
                return shortest + 360f;
            }

            return direction < 0 && shortest > 0f ? shortest - 360f : shortest;
        }
    }
}
