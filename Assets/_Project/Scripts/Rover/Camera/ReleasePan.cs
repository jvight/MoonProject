using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The release's view as one slow turn toward relay.1's lamp, the next goal (docs/design/opening-v3-plan.md 4.4, pure
    /// and EditMode-tested): the subject the camera moment frames glides from where the camera already looks toward the
    /// lamp, by the short way, its bearing and elevation chasing it at no more than a tuned rate with soft starts and
    /// stops, then holds there. 07 stays the camera's centre the whole time (the moment orbits it); only the point it
    /// looks toward moves. It stops short of the lamp's bearing, so the lamp sits off-centre, on a third of the frame.
    /// </summary>
    public sealed class ReleasePan
    {
        private const float Epsilon = 1e-4f;
        private const float FarthestRate = 1.2f;

        private Vector3 _origin;
        private Vector3 _lure;
        private float _travel;
        private float _hold;
        private float _maxSweep;
        private float _maxRise;
        private float _halfLife;
        private float _slack;
        private float _bearing;
        private float _elevation;
        private float _distance;
        private float _sweepSpeed;
        private float _riseSpeed;

        /// <summary>The pan is running.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Seconds since the pan began.</summary>
        public float Elapsed { get; private set; }

        /// <summary>The pan has run its travel and its hold (the camera may let go).</summary>
        public bool Arrived { get; private set; }

        /// <summary>The point the camera looks toward now.</summary>
        public Vector3 Subject => _origin + Direction(_bearing, _elevation) * _distance;

        /// <summary>The bearing (deg) the pan faces now.</summary>
        public float Bearing => _bearing;

        /// <summary>The bearing (deg) from <paramref name="origin"/> to <paramref name="lure"/>.</summary>
        public static float BearingTo(Vector3 origin, Vector3 lure)
        {
            return BearingOf(lure - origin);
        }

        /// <summary>
        /// Starts the pan from 07's place, from where the camera already looks, toward the lure.
        /// </summary>
        /// <param name="origin">07's place.</param>
        /// <param name="startBearing">The bearing (deg) the camera looks along now.</param>
        /// <param name="lure">relay.1's lamp.</param>
        /// <param name="travel">Seconds the pan gives the turn.</param>
        /// <param name="hold">Seconds the pan then holds the lamp.</param>
        /// <param name="maxSweep">Fastest the bearing turns (deg/s).</param>
        /// <param name="maxRise">Fastest the elevation changes (deg/s).</param>
        /// <param name="halfLife">Half-life (s) of the speed's easing: soft starts and stops.</param>
        /// <param name="slack">How far (deg) short of the lamp's bearing the pan settles.</param>
        public void Begin(Vector3 origin, float startBearing, Vector3 lure, float travel, float hold, float maxSweep,
            float maxRise, float halfLife, float slack)
        {
            _origin = origin;
            _lure = lure;
            _travel = travel;
            _hold = hold;
            _maxSweep = maxSweep;
            _maxRise = maxRise;
            _halfLife = Mathf.Max(0.05f, halfLife);
            _slack = slack;
            Elapsed = 0f;
            Arrived = false;
            IsActive = true;
            _bearing = Mathf.Repeat(startBearing, 360f);
            _elevation = 0f;
            _distance = (lure - origin).magnitude;
            _sweepSpeed = 0f;
            _riseSpeed = 0f;
        }

        /// <summary>Ends the pan (the player took the view back, or the camera let go).</summary>
        public void End()
        {
            IsActive = false;
        }

        public void Step(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return;
            }

            Elapsed += deltaTime;
            Vector3 toward = _lure - _origin;
            float bearingGap = Mathf.DeltaAngle(_bearing, BearingOf(toward));
            bearingGap = Mathf.Sign(bearingGap) * Mathf.Max(0f, Mathf.Abs(bearingGap) - _slack);
            float elevationGap = ElevationOf(toward) - _elevation;

            float sweepWanted = Mathf.Clamp(bearingGap * FarthestRate, -_maxSweep, _maxSweep);
            float riseWanted = Mathf.Clamp(elevationGap * FarthestRate, -_maxRise, _maxRise);
            _sweepSpeed = Smoothing.Damp(_sweepSpeed, sweepWanted, _halfLife, deltaTime);
            _riseSpeed = Smoothing.Damp(_riseSpeed, riseWanted, _halfLife, deltaTime);
            _bearing = Mathf.Repeat(_bearing + _sweepSpeed * deltaTime, 360f);
            _elevation += _riseSpeed * deltaTime;
            Arrived = Elapsed >= _travel + _hold;
        }

        private static float BearingOf(Vector3 direction)
        {
            return Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f);
        }

        private static float ElevationOf(Vector3 direction)
        {
            float flat = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
            return Mathf.Atan2(direction.y, Mathf.Max(Epsilon, flat)) * Mathf.Rad2Deg;
        }

        private static Vector3 Direction(float bearing, float elevation)
        {
            float cos = Mathf.Cos(elevation * Mathf.Deg2Rad);
            return new Vector3(Mathf.Sin(bearing * Mathf.Deg2Rad) * cos, Mathf.Sin(elevation * Mathf.Deg2Rad),
                Mathf.Cos(bearing * Mathf.Deg2Rad) * cos);
        }
    }
}
