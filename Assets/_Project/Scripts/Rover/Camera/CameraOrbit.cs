using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The orbit the drone camera keeps around 07, independent of Cinemachine so it can be tested
    /// (docs/design/first-hour-v2.md §7.3):
    /// <list type="bullet">
    /// <item>The view's yaw is a world <see cref="Bearing"/>: 07 turning under a view the player chose never turns it.
    /// Look input is smoothed in velocity space (it glides, and never lags behind once the mouse stops).</item>
    /// <item>Sitting behind 07 with no free look since, the camera follows 07's turns, damped and capped, so a pivot
    /// slides into view and never whips round.</item>
    /// <item>Once look input has stopped for its idle delay (shorter on the stick) and 07 has driven forward above
    /// the minimum speed for a while, a view more than the deadzone off 07's back recenters slowly: it fades in, its
    /// turn rate is capped and eases out over the last degrees, and the elevation returns at its own capped rate.
    /// Any look input cancels it at once (the camera just stops easing; nothing jumps).</item>
    /// <item>The recenter button eases behind 07 over a short, eased time, whatever else is going on.</item>
    /// <item>The Gentle camera never recenters by itself and keeps the field of view still.</item>
    /// <item>Driving downhill eases in extra elevation; the field of view widens a little with speed.</item>
    /// </list>
    /// </summary>
    public sealed class CameraOrbit
    {
        private const float InputEpsilon = 1e-6f;

        /// <summary>Throttle below this is drift on the stick, not the player driving.</summary>
        private const float ThrottleEpsilon = 0.05f;

        /// <summary>A recenter this close (deg) to straight behind 07 has arrived; following takes over.</summary>
        private const float HomeAngle = 0.01f;

        private readonly RoverCameraTuning _tuning;
        private Vector2 _lookVelocity;
        private float _idleTime;
        private float _idleDelay;
        private float _driveTime;
        private float _recenterWeight;
        private bool _recenteringYaw;
        private bool _following;
        private bool _buttonEasing;
        private float _buttonTime;
        private float _buttonFromOffset;
        private float _buttonFromPitch;
        private float _roverYaw;

        public CameraOrbit(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Reset(0f);
        }

        /// <summary>World yaw (deg) the view looks along, -180..180.</summary>
        public float Bearing { get; private set; }

        /// <summary>The view's angle (deg) from straight behind 07, -180..180.</summary>
        public float YawOffset => Mathf.DeltaAngle(_roverYaw, Bearing);

        /// <summary>Player-chosen elevation (deg).</summary>
        public float Pitch { get; private set; }

        /// <summary>Automatic downhill elevation (deg).</summary>
        public float Lift { get; private set; }

        /// <summary>Vertical field of view (deg).</summary>
        public float FieldOfView { get; private set; }

        /// <summary>The player's Gentle camera: no recentering by itself and no speed widening.</summary>
        public bool Gentle { get; set; }

        /// <summary>The camera sits behind 07 and follows its turns (no free look since it got there).</summary>
        public bool IsFollowing => _following;

        /// <summary>An automatic recenter is bringing the view back behind 07 (it may be fading in or out).</summary>
        public bool IsRecentering => _recenteringYaw;

        /// <summary>How strongly recentering currently pulls, 0..1 (eased).</summary>
        public float RecenterWeight => Smoothing.SmoothStep(0f, 1f, _recenterWeight);

        /// <summary>Final elevation for the orbital follow's vertical axis.</summary>
        public float Elevation => Mathf.Clamp(Pitch + Lift, _tuning.MinPitch, _tuning.MaxPitch);

        /// <summary>
        /// The opening shot: straight behind 07 (heading <paramref name="roverYaw"/>) at the low opening elevation,
        /// following. Recentering later eases it to the resting elevation once 07 drives.
        /// </summary>
        public void Reset(float roverYaw)
        {
            _lookVelocity = Vector2.zero;
            _idleTime = 0f;
            _idleDelay = _tuning.RecenterDelay;
            _driveTime = 0f;
            _recenterWeight = 0f;
            _recenteringYaw = false;
            _buttonEasing = false;
            _following = true;
            _roverYaw = Mathf.DeltaAngle(0f, roverYaw);
            Bearing = _roverYaw;
            Pitch = _tuning.OpeningPitch;
            Lift = 0f;
            FieldOfView = _tuning.BaseFov;
        }

        /// <summary>
        /// Takes over <paramref name="bearing"/> (deg, world) as the player's view, so a view the wide shot turned to
        /// stays where it is when control comes back instead of swinging round again.
        /// </summary>
        public void AdoptBearing(float bearing)
        {
            Bearing = Mathf.DeltaAngle(0f, bearing);
            _following = false;
            _recenteringYaw = false;
            _buttonEasing = false;
        }

        public void Step(in CameraOrbitInput input, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            _roverYaw = Mathf.DeltaAngle(0f, input.RoverYaw);
            bool looking = input.Look.sqrMagnitude > InputEpsilon;
            if (looking)
            {
                _idleTime = 0f;
                _idleDelay = input.LookFromStick ? _tuning.StickRecenterDelay : _tuning.RecenterDelay;
                _recenterWeight = 0f;
                _recenteringYaw = false;
            }
            else
            {
                _idleTime += deltaTime;
            }

            if (input.Recenter)
            {
                _buttonEasing = true;
                _buttonTime = 0f;
                _buttonFromOffset = YawOffset;
                _buttonFromPitch = Pitch;
                _recenteringYaw = false;
            }

            if (_buttonEasing)
            {
                StepButton(deltaTime);
            }
            else
            {
                StepLook(input.Look, looking, deltaTime);
            }

            StepRecenter(input, looking, deltaTime);
            if (_following && !_buttonEasing)
            {
                StepFollow(deltaTime);
            }

            if (input.Grounded)
            {
                float downhill = Mathf.Clamp01(input.DescentAngle / _tuning.DownhillFullAngle);
                float targetLift = downhill * _tuning.DownhillLift;
                if (input.Throttle <= ThrottleEpsilon && input.ForwardSpeed >= -0.5f
                    && input.DescentAngle >= _tuning.DownhillSlopeMinAngle)
                {
                    targetLift = Mathf.Max(targetLift, Lift);
                }

                Lift = Smoothing.Damp(Lift, targetLift, _tuning.LiftHalfLife, deltaTime);
            }
            else
            {
                float airDownhill = Mathf.Clamp(input.DescentAngle / _tuning.DownhillFullAngle, -0.5f, 1f);
                Lift = Smoothing.Damp(Lift, airDownhill * _tuning.DownhillLift, _tuning.AirborneLiftHalfLife, deltaTime);
            }

            float boost = Gentle ? 0f : _tuning.SpeedFovBoost;
            float fov = _tuning.BaseFov + boost * Smoothing.SmoothStep(0f, 1f, input.NormalizedSpeed);
            FieldOfView = Smoothing.Damp(FieldOfView, fov, _tuning.FovHalfLife, deltaTime);
        }

        private void StepLook(Vector2 lookDegrees, bool looking, float deltaTime)
        {
            float follow = Smoothing.Factor(_tuning.LookHalfLife, deltaTime);
            _lookVelocity += (lookDegrees / deltaTime - _lookVelocity) * follow;
            Bearing = Mathf.DeltaAngle(0f, Bearing + _lookVelocity.x * deltaTime);
            Pitch = Mathf.Clamp(Pitch + _lookVelocity.y * deltaTime, _tuning.MinPitch, _tuning.MaxPitch);
            _following &= !looking;
        }

        /// <summary>
        /// The recenter button's ease behind 07. It holds the view for its short time, so the stick's own nudge as it
        /// is clicked does not cancel it, and leaves the camera following.
        /// </summary>
        private void StepButton(float deltaTime)
        {
            _lookVelocity = Vector2.zero;
            _buttonTime += deltaTime;
            float t = Mathf.Clamp01(_buttonTime / _tuning.RecenterButtonSeconds);
            float eased = Smoothing.SmoothStep(0f, 1f, t);
            Bearing = Mathf.DeltaAngle(0f, _roverYaw + _buttonFromOffset * (1f - eased));
            Pitch = Mathf.Lerp(_buttonFromPitch, _tuning.DefaultPitch, eased);
            if (t >= 1f)
            {
                _buttonEasing = false;
                _following = true;
            }
        }

        private void StepRecenter(in CameraOrbitInput input, bool looking, float deltaTime)
        {
            bool forward = input.Throttle > ThrottleEpsilon && input.ForwardSpeed > _tuning.RecenterMinSpeed;
            _driveTime = forward ? _driveTime + deltaTime : 0f;
            if (looking)
            {
                return;
            }

            float speedSpan = Mathf.Max(0.1f, _tuning.RecenterFullSpeed - _tuning.RecenterMinSpeed);
            float speed01 = Mathf.Clamp01((input.ForwardSpeed - _tuning.RecenterMinSpeed) / speedSpan);
            float delayScale = Mathf.Lerp(_tuning.RecenterSlowDelayScale, 1f, speed01);
            float rateShare = Mathf.Lerp(_tuning.RecenterSlowRateShare, 1f, speed01);

            bool may = !Gentle && !_buttonEasing && input.MayRecenter && _idleTime >= _idleDelay * delayScale
                && _driveTime >= _tuning.RecenterDriveSeconds;
            _recenterWeight = may
                ? Mathf.Min(1f, _recenterWeight + deltaTime / _tuning.RecenterRampTime)
                : Mathf.Max(0f, _recenterWeight - deltaTime / _tuning.RecenterFadeOut);
            if (may && !_following && Mathf.Abs(YawOffset) > _tuning.RecenterDeadzone)
            {
                _recenteringYaw = true;
            }

            float pull = RecenterWeight;
            if (pull <= 0f)
            {
                return;
            }

            if (_recenteringYaw)
            {
                float offset = YawOffset;
                float ease = Mathf.Sqrt(Mathf.Clamp01(Mathf.Abs(offset) / _tuning.RecenterEaseAngle));
                float next = Mathf.MoveTowards(offset, 0f, _tuning.RecenterMaxRate * rateShare * ease * pull * deltaTime);
                Bearing = Mathf.DeltaAngle(0f, _roverYaw + next);
                if (Mathf.Abs(next) <= HomeAngle)
                {
                    _recenteringYaw = false;
                    _following = true;
                }
            }

            Pitch = Mathf.MoveTowards(Pitch, _tuning.DefaultPitch, _tuning.RecenterPitchRate * rateShare * pull * deltaTime);
        }

        /// <summary>Behind 07: its turns are followed with the tuned damping, never faster than the cap.</summary>
        private void StepFollow(float deltaTime)
        {
            float offset = YawOffset;
            float damped = Smoothing.DampAngle(offset, 0f, _tuning.FollowHalfLife, deltaTime);
            float maxStep = _tuning.FollowMaxRate * deltaTime;
            float next = offset + Mathf.Clamp(Mathf.DeltaAngle(offset, damped), -maxStep, maxStep);
            Bearing = Mathf.DeltaAngle(0f, _roverYaw + next);
        }
    }
}
