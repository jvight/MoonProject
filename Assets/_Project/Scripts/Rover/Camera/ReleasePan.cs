using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The release's view as one continuous, slow pan (docs/design/opening-v3-plan.md 4.4, pure and EditMode-tested): the
    /// subject the camera moment frames glides from relay.1's lamp to the Peak's dish to Kestrel-3, its bearing and
    /// elevation chasing each lure in turn at no more than a tuned rate, with soft starts and stops, so nothing cuts and
    /// the camera never whips round however far apart the lures lie. 07 stays the camera's centre the whole time (the
    /// moment orbits it); only the point it looks toward moves.
    /// </summary>
    public sealed class ReleasePan
    {
        private const float Epsilon = 1e-4f;
        private const float FarthestRate = 4f;

        private readonly Vector3[] _lures = new Vector3[3];
        private Vector3 _origin;
        private float _lead;
        private float _slack;
        private float _dishLeg;
        private float _kestrelLeg;
        private float _maxSweep;
        private float _maxRise;
        private float _halfLife;
        private float _bearing;
        private float _elevation;
        private float _distance;
        private float _sweepSpeed;
        private float _riseSpeed;

        /// <summary>The pan is running.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Seconds since the pan began.</summary>
        public float Elapsed { get; private set; }

        /// <summary>The lure the pan is heading for now: 0 relay.1's lamp, 1 the Peak's dish, 2 Kestrel-3.</summary>
        public int Target { get; private set; }

        /// <summary>The pan has run its course, its last lure held for its time (the camera may let go).</summary>
        public bool Arrived { get; private set; }

        /// <summary>The point the camera looks toward now.</summary>
        public Vector3 Subject => _origin + Direction(_bearing, _elevation) * _distance;

        /// <summary>The bearing (deg) the pan faces now.</summary>
        public float Bearing => _bearing;

        /// <summary>
        /// Starts the pan from 07's place, from where the camera already looks, to relay.1's lamp first. Each lure is
        /// brought within <c>slack</c> degrees of the view's centre, not onto it: it is in frame well before then, and the
        /// camera has that much less to turn.
        /// </summary>
        /// <param name="origin">07's place.</param>
        /// <param name="startBearing">The bearing (deg) the camera looks along now.</param>
        /// <param name="slack">How far (deg) short of a lure's bearing the pan may settle.</param>
        /// <param name="lead">Seconds the camera holds the first lure before the pan sets off for the next.</param>
        /// <param name="dishLeg">Seconds the pan gives the way to the dish and its time there.</param>
        /// <param name="kestrelLeg">Seconds the pan gives the way on to Kestrel-3 and its time there.</param>
        /// <param name="maxSweep">Fastest the bearing turns (deg/s).</param>
        /// <param name="maxRise">Fastest the elevation changes (deg/s).</param>
        /// <param name="halfLife">Half-life (s) of the speed's easing: soft starts and stops.</param>
        public void Begin(Vector3 origin, float startBearing, Vector3 relay, Vector3 dish, Vector3 kestrel, float lead, float dishLeg,
            float kestrelLeg, float maxSweep, float maxRise, float halfLife, float slack)
        {
            _slack = slack;
            _origin = origin;
            _lures[0] = relay;
            _lures[1] = dish;
            _lures[2] = kestrel;
            _lead = lead;
            _dishLeg = dishLeg;
            _kestrelLeg = kestrelLeg;
            _maxSweep = maxSweep;
            _maxRise = maxRise;
            _halfLife = Mathf.Max(0.05f, halfLife);
            Elapsed = 0f;
            Target = 0;
            Arrived = false;
            IsActive = true;
            Vector3 first = _lures[0] - origin;
            _bearing = Mathf.Repeat(startBearing, 360f);
            _elevation = 0f;
            _distance = first.magnitude;
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
            Target = Elapsed < _lead + _dishLeg ? Elapsed < _lead ? 0 : 1 : 2;
            Vector3 toward = _lures[Target] - _origin;
            float bearingWanted = BearingOf(toward);
            float elevationWanted = ElevationOf(toward);
            float bearingGap = Mathf.DeltaAngle(_bearing, bearingWanted);
            bearingGap = Mathf.Sign(bearingGap) * Mathf.Max(0f, Mathf.Abs(bearingGap) - _slack);
            float elevationGap = elevationWanted - _elevation;

            float sweepWanted = Mathf.Clamp(bearingGap * FarthestRate, -_maxSweep, _maxSweep);
            float riseWanted = Mathf.Clamp(elevationGap * FarthestRate, -_maxRise, _maxRise);
            _sweepSpeed = Smoothing.Damp(_sweepSpeed, sweepWanted, _halfLife, deltaTime);
            _riseSpeed = Smoothing.Damp(_riseSpeed, riseWanted, _halfLife, deltaTime);
            _bearing = Mathf.Repeat(_bearing + _sweepSpeed * deltaTime, 360f);
            _elevation += _riseSpeed * deltaTime;
            _distance = Smoothing.Damp(_distance, toward.magnitude, _halfLife, deltaTime);

            Arrived = Elapsed >= _lead + _dishLeg + _kestrelLeg;
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
