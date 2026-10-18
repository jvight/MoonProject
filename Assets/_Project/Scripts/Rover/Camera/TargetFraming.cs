using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>What <see cref="TargetFraming"/> reads each frame.</summary>
    public readonly struct TargetFramingInput
    {
        /// <param name="roverPosition">07's position on the ground (world).</param>
        /// <param name="roverYaw">07's heading (deg, world).</param>
        /// <param name="targets">The points to keep in view (world), the main one first.</param>
        /// <param name="targetCount">How many of <paramref name="targets"/> are set; 0 for none.</param>
        /// <param name="followPoint">The point the camera orbits and looks at (world).</param>
        /// <param name="viewYaw">The world yaw (deg) the camera looks along, before any framing.</param>
        /// <param name="elevation">The camera's elevation (deg), before any framing.</param>
        /// <param name="radius">The camera's distance (m) from the follow point, before any framing.</param>
        /// <param name="fov">Vertical field of view (deg).</param>
        /// <param name="aspect">Width over height of the view.</param>
        /// <param name="speed">07's speed (m/s).</param>
        /// <param name="looking">The player is moving the view this frame.</param>
        /// <param name="mayFrame">Nothing else frames the view.</param>
        public TargetFramingInput(Vector3 roverPosition, float roverYaw, Vector3[] targets, int targetCount,
            Vector3 followPoint, float viewYaw, float elevation, float radius, float fov, float aspect, float speed,
            bool looking, bool mayFrame)
        {
            RoverPosition = roverPosition;
            RoverYaw = roverYaw;
            Targets = targets;
            TargetCount = targetCount;
            FollowPoint = followPoint;
            ViewYaw = viewYaw;
            Elevation = elevation;
            Radius = radius;
            Fov = fov;
            Aspect = aspect;
            Speed = speed;
            Looking = looking;
            MayFrame = mayFrame;
        }

        public Vector3 RoverPosition { get; }

        public float RoverYaw { get; }

        public Vector3[] Targets { get; }

        public int TargetCount { get; }

        public bool HasTarget => TargetCount > 0;

        public Vector3 FollowPoint { get; }

        public float ViewYaw { get; }

        public float Elevation { get; }

        public float Radius { get; }

        public float Fov { get; }

        public float Aspect { get; }

        public float Speed { get; }

        public bool Looking { get; }

        public bool MayFrame { get; }
    }

    /// <summary>
    /// Moves the chase camera, while 07 is stopped or slow near something to scan or use, to the nearest view in which
    /// 07's own body does not hide it: it searches small steps of sideways swing, lift and pull-in, cheapest first,
    /// for a sight line that clears 07's body and keeps the target well inside the screen, and eases there. It only
    /// adds to the player's view: the mouse is never overridden, it just starts the camera easing back, and driving
    /// off releases it.
    /// </summary>
    public sealed class TargetFraming
    {
        private const float SideChangeCost = 0.25f;
        private const float AgainstTargetSideCost = 0.1f;

        /// <summary>Weight of the cost of moving from where the camera already is, so a new goal is a near one.</summary>
        private const float MoveCost = 0.5f;

        /// <summary>A new goal clears the body by this many times the tuned clearance, so holding it survives jitter.</summary>
        private const float PickClearanceFactor = 1.5f;

        private readonly RoverCameraTuning _tuning;
        private float _calmTime;
        private float _raw;
        private float _yaw;
        private float _lift;
        private float _scale = 1f;
        private float _lastSide;
        private Vector3 _goal = new Vector3(0f, 0f, 1f);
        private bool _hasGoal;
        private Vector3 _shift;

        public TargetFraming(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>How strongly the framing applies, 0..1 (eased).</summary>
        public float Weight => Smoothing.SmoothStep(0f, 1f, _raw);

        /// <summary>Extra camera elevation (deg).</summary>
        public float Elevation => _lift * Weight;

        /// <summary>Extra yaw (deg) toward the side the camera swings to.</summary>
        public float YawOffset => _yaw * Weight;

        /// <summary>Multiplier on the camera distance (1 = unchanged, less = pulled in).</summary>
        public float RadiusScale => Mathf.Lerp(1f, _scale, Weight);

        /// <summary>
        /// How far the point the camera looks at moves toward the target (m), so the view rests on what 07 scans or
        /// uses and not on whatever else lies nearer the view's centre.
        /// </summary>
        public Vector3 Shift => _shift * Weight;

        /// <summary>The swing (deg), lift (deg) and distance scale the framing is easing toward (for tests).</summary>
        public Vector3 Goal => _goal;

        /// <summary>Where the camera sits for the given orbit angles around <paramref name="follow"/>.</summary>
        public static Vector3 EyePosition(Vector3 follow, float yaw, float elevation, float radius)
        {
            float pitch = elevation * Mathf.Deg2Rad;
            Vector3 back = Quaternion.Euler(0f, yaw, 0f) * Vector3.back;
            return follow + (back * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)) * radius;
        }

        /// <summary>
        /// Whether the sight line from <paramref name="eye"/> to <paramref name="target"/> misses 07's body, a box of
        /// the tuned size grown by the tuned clearance, standing on <paramref name="rover"/> and turned to
        /// <paramref name="roverYaw"/>.
        /// </summary>
        public static bool BodyClear(RoverCameraTuning tuning, Vector3 rover, float roverYaw, Vector3 eye,
            Vector3 target)
        {
            return BodyClear(tuning, tuning.FramingBodyClearance, rover, roverYaw, eye, target);
        }

        private static bool BodyClear(RoverCameraTuning tuning, float clearance, Vector3 rover, float roverYaw,
            Vector3 eye, Vector3 target)
        {
            var half = new Vector3(0.5f * tuning.FramingBodyWidth + clearance,
                0.5f * tuning.FramingBodyHeight + clearance, 0.5f * tuning.FramingBodyLength + clearance);
            Vector3 centre = rover + Vector3.up * (0.5f * tuning.FramingBodyHeight);
            Quaternion toLocal = Quaternion.Inverse(Quaternion.Euler(0f, roverYaw, 0f));
            Vector3 from = toLocal * (eye - centre);
            Vector3 along = toLocal * (target - centre) - from;
            float enter = 0f;
            float exit = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                float start = from[axis];
                float step = along[axis];
                if (Mathf.Abs(step) < 1e-6f)
                {
                    if (Mathf.Abs(start) > half[axis])
                    {
                        return true;
                    }

                    continue;
                }

                float near = (-half[axis] - start) / step;
                float far = (half[axis] - start) / step;
                enter = Mathf.Max(enter, Mathf.Min(near, far));
                exit = Mathf.Min(exit, Mathf.Max(near, far));
                if (enter > exit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether the sight line misses 07's body as this framing sees it (for tests and tooling).</summary>
        public bool BodyClear(Vector3 rover, float roverYaw, Vector3 eye, Vector3 target)
        {
            return BodyClear(_tuning, rover, roverYaw, eye, target);
        }

        public void Step(in TargetFramingInput input, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _calmTime = input.Speed <= _tuning.FramingMaxSpeed && !input.Looking ? _calmTime + deltaTime : 0f;
            bool wants = input.HasTarget && input.MayFrame && _calmTime >= _tuning.FramingEngageDelay
                && InRange(input);
            if (wants)
            {
                Solve(input, deltaTime);
            }
            else if (_raw <= 0f)
            {
                _yaw = 0f;
                _lift = 0f;
                _scale = 1f;
                _goal = new Vector3(0f, 0f, 1f);
                _hasGoal = false;
                _shift = Vector3.zero;
            }

            float seconds = wants ? _tuning.FramingRampSeconds : _tuning.FramingReleaseSeconds;
            _raw = Mathf.MoveTowards(_raw, wants ? 1f : 0f, deltaTime / seconds);
        }

        private bool InRange(in TargetFramingInput input)
        {
            Vector3 toTarget = input.Targets[0] - input.RoverPosition;
            toTarget.y = 0f;
            float bearing = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            return toTarget.magnitude <= _tuning.FramingRange
                && Mathf.Abs(Mathf.DeltaAngle(input.ViewYaw, bearing)) <= _tuning.FramingViewCone;
        }

        /// <summary>Finds the cheapest swing, lift and pull-in that show the target, and eases toward it.</summary>
        private void Solve(in TargetFramingInput input, float deltaTime)
        {
            _shift = Smoothing.Damp(_shift, LookShift(input), _tuning.FramingSolveHalfLife, deltaTime);
            if (_hasGoal && Shows(input, _goal.x, _goal.y, _goal.z, _tuning.FramingBodyClearance))
            {
                EaseToGoal(deltaTime);
                return;
            }

            int steps = _tuning.FramingSearchSteps;
            Vector3 toTarget = input.Targets[0] - input.RoverPosition;
            float targetSide = Mathf.Sign(Mathf.DeltaAngle(input.ViewYaw,
                Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg));
            if (_lastSide == 0f)
            {
                _lastSide = targetSide;
            }

            float pickClearance = _tuning.FramingBodyClearance * PickClearanceFactor;
            float bestCost = float.MaxValue;
            float bestYaw = _tuning.FramingYaw * _lastSide;
            float bestLift = _tuning.FramingLift;
            float bestScale = _tuning.FramingMinRadiusScale;
            for (int s = 0; s <= steps; s++)
            {
                float scale = Mathf.Lerp(1f, _tuning.FramingMinRadiusScale, (float)s / steps);
                for (int l = 0; l <= steps; l++)
                {
                    float lift = _tuning.FramingLift * l / steps;
                    for (int y = -steps; y <= steps; y++)
                    {
                        float yaw = _tuning.FramingYaw * y / steps;
                        float cost = Mathf.Abs(y) / (float)steps + (float)l / steps + (float)s / steps
                            + MoveCost * Mathf.Abs(yaw - _yaw) / _tuning.FramingYaw;
                        if (y != 0 && Mathf.Sign(y) != _lastSide)
                        {
                            cost += SideChangeCost;
                        }

                        if (y != 0 && Mathf.Sign(y) != targetSide)
                        {
                            cost += AgainstTargetSideCost;
                        }

                        if (cost < bestCost && Shows(input, yaw, lift, scale, pickClearance))
                        {
                            bestCost = cost;
                            bestYaw = yaw;
                            bestLift = lift;
                            bestScale = scale;
                        }
                    }
                }
            }

            if (bestYaw != 0f)
            {
                _lastSide = Mathf.Sign(bestYaw);
            }

            _goal = new Vector3(bestYaw, bestLift, bestScale);
            _hasGoal = true;
            EaseToGoal(deltaTime);
        }

        private void EaseToGoal(float deltaTime)
        {
            float halfLife = _tuning.FramingSolveHalfLife;
            _yaw = Smoothing.Damp(_yaw, _goal.x, halfLife, deltaTime);
            _lift = Smoothing.Damp(_lift, _goal.y, halfLife, deltaTime);
            _scale = Smoothing.Damp(_scale, _goal.z, halfLife, deltaTime);
        }

        private Vector3 LookShift(in TargetFramingInput input)
        {
            return (input.Targets[0] - input.FollowPoint) * _tuning.FramingLookShare;
        }

        private bool Shows(in TargetFramingInput input, float yaw, float lift, float scale, float clearance)
        {
            Vector3 look = input.FollowPoint + LookShift(input);
            Vector3 eye = EyePosition(look, input.ViewYaw + yaw, input.Elevation + lift,
                input.Radius * scale);
            Vector3 forward = (look - eye).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);
            float tanVertical = Mathf.Tan(0.5f * input.Fov * Mathf.Deg2Rad) * _tuning.FramingCentralShare;
            for (int i = 0; i < input.TargetCount; i++)
            {
                Vector3 target = input.Targets[i];
                if (!BodyClear(_tuning, clearance, input.RoverPosition, input.RoverYaw, eye, target))
                {
                    return false;
                }

                Vector3 sight = target - eye;
                float depth = Vector3.Dot(sight, forward);
                if (depth <= 0f || Mathf.Abs(Vector3.Dot(sight, right) / depth) > tanVertical * input.Aspect
                    || Mathf.Abs(Vector3.Dot(sight, up) / depth) > tanVertical)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
