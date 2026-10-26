using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The one gentle turn of the chase view while 07 places a structure (<c>BuildModeState.Placing</c>): only when the
    /// placing ghost is off screen as Placing begins, the view eases round (at most
    /// <see cref="RoverCameraTuning.BuildFrameMaxRate"/>, <c>&lt;= 20 deg/s</c>, slowing into the last degrees) until
    /// the ghost is in frame, and no further. The player's orbit is theirs: any look, recenter or drive input, or any
    /// other change of the view's bearing, ends the turn at once.
    /// </summary>
    public sealed class BuildFraming
    {
        private const float LookEpsilon = 1e-4f;
        private const float MinSubjectDistanceSq = 0.25f;
        private const float MinEaseAngle = 1f;
        private const float ExternalBearingTolerance = 0.5f;

        private bool _active;
        private bool _playerSteeredYaw;
        private bool _hasLastBearing;
        private float _lastOrbitBearing;
        private Vector3 _ghostPosition;

        /// <summary>True while the view is turning toward an off-screen ghost.</summary>
        public bool IsActive => _active;

        /// <summary>Where the ghost is now (world).</summary>
        public Vector3 GhostPosition => _ghostPosition;

        /// <summary>True when the player (or a test) took the view during the current turn.</summary>
        public bool PlayerSteeredYaw => _playerSteeredYaw;

        /// <summary>
        /// True when <paramref name="viewport"/> (a point's viewport position) lies behind the camera or outside the
        /// frame inset by <paramref name="margin"/> on every side.
        /// </summary>
        public static bool IsOffScreen(Vector3 viewport, float margin)
        {
            return viewport.z <= 0f || viewport.x < margin || viewport.x > 1f - margin || viewport.y < margin
                   || viewport.y > 1f - margin;
        }

        /// <summary>Starts the turn toward the ghost at <paramref name="ghostPosition"/>.</summary>
        public void Begin(Vector3 ghostPosition, float currentBearing)
        {
            _active = true;
            _ghostPosition = ghostPosition;
            _playerSteeredYaw = false;
            _lastOrbitBearing = currentBearing;
            _hasLastBearing = true;
        }

        /// <summary>Follows the ghost while a turn is under way; a ghost moved after it ended starts nothing.</summary>
        public void MoveGhost(Vector3 ghostPosition)
        {
            if (_active)
            {
                _ghostPosition = ghostPosition;
            }
        }

        /// <summary>Ends the turn where it stands.</summary>
        public void End()
        {
            _active = false;
        }

        /// <summary>Forgets everything at once (07 was placed somewhere else).</summary>
        public void Reset()
        {
            _active = false;
            _playerSteeredYaw = false;
            _hasLastBearing = false;
        }

        /// <summary>
        /// Whether <paramref name="currentOrbitBearing"/> was changed by something else since this framing last
        /// recorded it (a test, a held shot handing the view back).
        /// </summary>
        public bool DidBearingChangeExternally(float currentOrbitBearing)
        {
            return _hasLastBearing &&
                   Mathf.Abs(Mathf.DeltaAngle(_lastOrbitBearing, currentOrbitBearing)) > ExternalBearingTolerance;
        }

        /// <summary>Records the orbit's bearing at the end of the camera's update.</summary>
        public void RecordOrbitBearing(float orbitBearing)
        {
            _lastOrbitBearing = orbitBearing;
            _hasLastBearing = true;
        }

        /// <summary>
        /// Advances the turn by <paramref name="deltaTime"/> seconds, easing <paramref name="orbitBearing"/> toward the
        /// ghost unless the player took the view.
        /// </summary>
        public void Step(
            Vector3 roverPosition,
            Vector2 look,
            bool recenter,
            bool driving,
            bool externalYawChange,
            ref float orbitBearing,
            RoverCameraTuning tuning,
            float deltaTime)
        {
            if (deltaTime <= 0f || tuning == null)
            {
                return;
            }

            if (externalYawChange || recenter || driving || look.sqrMagnitude > LookEpsilon * LookEpsilon)
            {
                _playerSteeredYaw = true;
                _active = false;
            }

            if (_active)
            {
                Vector3 toGhost = new Vector3(_ghostPosition.x - roverPosition.x, 0f, _ghostPosition.z - roverPosition.z);
                if (toGhost.sqrMagnitude > MinSubjectDistanceSq)
                {
                    float targetBearing = Mathf.Atan2(toGhost.x, toGhost.z) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(orbitBearing, targetBearing);
                    float easeAngle = Mathf.Max(MinEaseAngle, tuning.BuildFrameEaseAngle);
                    float ease = Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(delta) / easeAngle));
                    float step = Mathf.Min(Mathf.Abs(delta), tuning.BuildFrameMaxRate * ease * deltaTime);
                    orbitBearing = Mathf.DeltaAngle(0f, orbitBearing + Mathf.Sign(delta) * step);
                }
            }

            _lastOrbitBearing = orbitBearing;
            _hasLastBearing = true;
        }
    }
}
