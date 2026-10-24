using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Calm chase-camera framing while 07 places a structure in build mode (<c>BuildModeState.Placing</c>):
    /// <list type="bullet">
    /// <item>On entering <c>Placing</c>, eases the chase camera's yaw and elevation (at most
    /// <see cref="RoverCameraTuning.BuildFrameMaxRate"/>, <c>&lt;= 20 deg/s</c>, modest pitch, never top-down) so both 07
    /// and the ground ghost or marked apron sit in frame together.</item>
    /// <item>Shifts the chase look point partway toward the ghost (<see cref="LookShift"/>) and follows the ghost smoothly
    /// as it moves across the ground or snaps onto an apron.</item>
    /// <item>Yields immediately to any player look input or external bearing change so free look works without
    /// resistance.</item>
    /// <item>On leaving <c>Placing</c>, eases the look shift, pull-back, and elevation offset back to zero.</item>
    /// </list>
    /// </summary>
    public sealed class BuildFraming
    {
        private const float LookEpsilon = 1e-4f;
        private const float WeightEpsilon = 1e-4f;
        private const float MinSubjectDistanceSq = 0.25f;
        private const float MinDuration = 0.05f;
        private const float MinEaseAngle = 1f;
        private const float ExternalBearingTolerance = 0.5f;

        private bool _active;
        private bool _snappedToApron;
        private bool _playerSteeredYaw;
        private bool _playerSteeredPitch;
        private bool _hasLastBearing;
        private float _lastOrbitBearing;
        private Vector3 _ghostPosition;
        private float _ease;
        private float _wantedElevationOffset;

        /// <summary>True while build mode is in <c>Placing</c>.</summary>
        public bool IsActive => _active;

        /// <summary>True while the placing ghost is snapped to a marked apron bay.</summary>
        public bool SnappedToApron => _snappedToApron;

        /// <summary>True when the player or a test has steered yaw during the current placing session.</summary>
        public bool PlayerSteeredYaw => _playerSteeredYaw;

        /// <summary>Smooth 0..1 weight of the build-mode framing.</summary>
        public float Weight { get; private set; }

        /// <summary>Extra chase elevation (deg) added to the orbit's base elevation, eased at &lt;= 20 deg/s.</summary>
        public float ElevationOffset { get; private set; }

        /// <summary>World-space horizontal shift added to 07's follow/look point to frame 07 and the ghost together.</summary>
        public Vector3 LookShift { get; private set; }

        /// <summary>Extra chase distance (share of normal distance) added while framing 07 and the ghost.</summary>
        public float PullBack { get; private set; }

        /// <summary>Starts framing 07 together with the placing ghost at <paramref name="ghostPosition"/>.</summary>
        public void Begin(Vector3 roverPosition, Vector3 ghostPosition, bool snappedToApron, float currentBearing)
        {
            _active = true;
            _ghostPosition = ghostPosition;
            _snappedToApron = snappedToApron;
            _playerSteeredYaw = false;
            _playerSteeredPitch = false;
            _lastOrbitBearing = currentBearing;
            _hasLastBearing = true;
        }

        /// <summary>Updates the placing ghost's world position while <c>Placing</c> continues.</summary>
        public void MoveGhost(Vector3 roverPosition, Vector3 ghostPosition, bool snappedToApron)
        {
            if (!_active)
            {
                Begin(roverPosition, ghostPosition, snappedToApron, _lastOrbitBearing);
                return;
            }

            if (snappedToApron && !_snappedToApron)
            {
                _playerSteeredYaw = false;
            }

            _ghostPosition = ghostPosition;
            _snappedToApron = snappedToApron;
        }

        /// <summary>Leaves <c>Placing</c> and eases the build-mode framing back to the normal chase camera.</summary>
        public void End()
        {
            _active = false;
            _snappedToApron = false;
        }

        /// <summary>Clears all build-mode framing state immediately (e.g. when 07 is teleported).</summary>
        public void Reset()
        {
            _active = false;
            _snappedToApron = false;
            _playerSteeredYaw = false;
            _playerSteeredPitch = false;
            _hasLastBearing = false;
            _ease = 0f;
            _wantedElevationOffset = 0f;
            Weight = 0f;
            ElevationOffset = 0f;
            LookShift = Vector3.zero;
            PullBack = 0f;
        }

        /// <summary>
        /// Checks whether <paramref name="currentOrbitBearing"/> was changed externally outside of <see cref="Step"/>
        /// since the previous frame (such as a test calling <see cref="CameraOrbit.AdoptBearing"/>).
        /// </summary>
        public bool DidBearingChangeExternally(float currentOrbitBearing)
        {
            return _hasLastBearing &&
                   Mathf.Abs(Mathf.DeltaAngle(_lastOrbitBearing, currentOrbitBearing)) > ExternalBearingTolerance;
        }

        /// <summary>Records the final orbit bearing at the end of the camera update step.</summary>
        public void RecordOrbitBearing(float orbitBearing)
        {
            _lastOrbitBearing = orbitBearing;
            _hasLastBearing = true;
        }

        /// <summary>
        /// Advances build-mode framing by <paramref name="deltaTime"/> seconds. Eases <paramref name="orbitBearing"/>
        /// and <see cref="ElevationOffset"/> at no more than <see cref="RoverCameraTuning.BuildFrameMaxRate"/>
        /// (<c>&lt;= 20 deg/s</c>) unless the player steers the view.
        /// </summary>
        public void Step(
            Vector3 roverPosition,
            Vector2 look,
            bool recenter,
            bool externalYawChange,
            ref float orbitBearing,
            float baseElevation,
            RoverCameraTuning tuning,
            float deltaTime)
        {
            if (deltaTime <= 0f || tuning == null)
            {
                return;
            }

            if (externalYawChange || Mathf.Abs(look.x) > LookEpsilon || recenter)
            {
                _playerSteeredYaw = true;
            }

            if (Mathf.Abs(look.y) > LookEpsilon || recenter)
            {
                _playerSteeredPitch = true;
            }

            float duration = _active
                ? Mathf.Max(MinDuration, tuning.BuildFrameInSeconds)
                : Mathf.Max(MinDuration, tuning.BuildFrameOutSeconds);
            _ease = Mathf.MoveTowards(_ease, _active ? 1f : 0f, deltaTime / duration);
            Weight = Smoothing.SmoothStep(0f, 1f, _ease);

            float maxRate = tuning.BuildFrameMaxRate;
            float easeAngle = Mathf.Max(MinEaseAngle, tuning.BuildFrameEaseAngle);

            if (_active && !_playerSteeredYaw)
            {
                Vector3 toGhost = new Vector3(_ghostPosition.x - roverPosition.x, 0f, _ghostPosition.z - roverPosition.z);
                if (toGhost.sqrMagnitude > MinSubjectDistanceSq)
                {
                    float targetBearing = Mathf.Atan2(toGhost.x, toGhost.z) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(orbitBearing, targetBearing);
                    float ease = Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(delta) / easeAngle));
                    float step = Mathf.Min(Mathf.Abs(delta), maxRate * ease * deltaTime);
                    orbitBearing = Mathf.DeltaAngle(0f, orbitBearing + Mathf.Sign(delta) * step);
                }
            }

            if (_active)
            {
                if (!_playerSteeredPitch)
                {
                    _wantedElevationOffset = Mathf.Max(0f, tuning.BuildFrameElevation - baseElevation);
                }
            }
            else
            {
                _wantedElevationOffset = 0f;
            }

            float elevDelta = _wantedElevationOffset - ElevationOffset;
            float elevEase = Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(elevDelta) / easeAngle));
            float maxElevStep = maxRate * elevEase * deltaTime;
            ElevationOffset = Mathf.MoveTowards(ElevationOffset, _wantedElevationOffset, maxElevStep);

            if (!_active && Weight <= WeightEpsilon && Mathf.Abs(ElevationOffset) <= WeightEpsilon)
            {
                ElevationOffset = 0f;
                LookShift = Vector3.zero;
                PullBack = 0f;
                _lastOrbitBearing = orbitBearing;
                _hasLastBearing = true;
                return;
            }

            Vector3 flatOffset = new Vector3(_ghostPosition.x - roverPosition.x, 0f, _ghostPosition.z - roverPosition.z);
            Vector3 wantedShift = Vector3.ClampMagnitude(
                flatOffset * tuning.BuildFrameLookShare,
                tuning.BuildFrameMaxLookShift) * Weight;
            LookShift = Smoothing.Damp(LookShift, wantedShift, tuning.BuildFrameFollowHalfLife, deltaTime);
            PullBack = tuning.BuildFramePullBack * Weight;
            _lastOrbitBearing = orbitBearing;
            _hasLastBearing = true;
        }
    }
}
