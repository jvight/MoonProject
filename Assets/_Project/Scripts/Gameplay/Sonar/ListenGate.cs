using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// When 07 listens (docs/design/first-hour-v2.md §3 "Listening"): once the tower is awake, after 07 has rested for
    /// a while, and only after it has driven far enough since its last listen, so stopping in a new place listens once
    /// and sitting still never repeats it. The first listen of a session needs no distance. Pure logic.
    /// </summary>
    public sealed class ListenGate
    {
        private Vector3 _last;
        private bool _hasLast;
        private float _rested;
        private float _driven;
        private bool _listened;

        /// <summary>Metres driven (on the ground plane) since the last listen.</summary>
        public float Driven => _driven;

        /// <summary>Seconds 07 has rested so far.</summary>
        public float Rested => _rested;

        /// <summary>
        /// Notes 07's move this frame; true when 07 listens now.
        /// </summary>
        /// <param name="awake">The tower has reached level 1: there is something to listen for.</param>
        /// <param name="position">07's position now.</param>
        /// <param name="speed">07's horizontal speed (m/s).</param>
        /// <param name="deltaTime">Seconds since the last tick.</param>
        /// <param name="tuning">Rest seconds, rest speed and the distance between listens.</param>
        public bool Tick(bool awake, Vector3 position, float speed, float deltaTime, SonarTuning tuning)
        {
            if (_hasLast)
            {
                _driven += SurfaceRules.HorizontalDistance(_last, position);
            }

            _last = position;
            _hasLast = true;
            _rested = speed <= tuning.RestSpeed ? _rested + deltaTime : 0f;
            if (!awake || _rested < tuning.RestSeconds || (_listened && _driven < tuning.ListenSpacing))
            {
                return false;
            }

            _listened = true;
            _driven = 0f;
            return true;
        }
    }
}
