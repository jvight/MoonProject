using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The orbit the drone camera keeps around 07, independent of Cinemachine so it can be tested:
    /// <list type="bullet">
    /// <item>Look input is smoothed in velocity space (it glides, and never lags behind once the mouse stops).</item>
    /// <item>After RecenterDelay seconds without look input, while moving, recentering fades in over RecenterRampTime
    /// and eases the orbit back behind the rover and to the default elevation. Any look input cancels it at once
    /// (the camera just stops easing; nothing jumps).</item>
    /// <item>Driving downhill eases in extra elevation; the field of view widens a little with speed.</item>
    /// </list>
    /// </summary>
    public sealed class CameraOrbit
    {
        private const float InputEpsilon = 1e-6f;

        private readonly RoverCameraTuning _tuning;
        private Vector2 _lookVelocity;
        private float _idleTime;
        private float _recenterWeight;

        public CameraOrbit(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Reset();
        }

        /// <summary>Orbit angle (deg) around the rover relative to straight behind it, -180..180.</summary>
        public float YawOffset { get; private set; }

        /// <summary>Player-chosen elevation (deg).</summary>
        public float Pitch { get; private set; }

        /// <summary>Automatic downhill elevation (deg).</summary>
        public float Lift { get; private set; }

        /// <summary>Vertical field of view (deg).</summary>
        public float FieldOfView { get; private set; }

        /// <summary>How strongly recentering currently pulls, 0..1 (eased).</summary>
        public float RecenterWeight => Smoothing.SmoothStep(0f, 1f, _recenterWeight);

        /// <summary>Final elevation for the orbital follow's vertical axis.</summary>
        public float Elevation => Mathf.Clamp(Pitch + Lift, _tuning.MinPitch, _tuning.MaxPitch);

        /// <summary>
        /// The opening shot: straight behind 07 at the low opening elevation. Recentering later eases it to the
        /// resting elevation once 07 drives.
        /// </summary>
        public void Reset()
        {
            _lookVelocity = Vector2.zero;
            _idleTime = 0f;
            _recenterWeight = 0f;
            YawOffset = 0f;
            Pitch = _tuning.OpeningPitch;
            Lift = 0f;
            FieldOfView = _tuning.BaseFov;
        }

        /// <param name="lookDegrees">Orbit change requested this frame: x = orbit right, y = camera up (deg).</param>
        /// <param name="speed">Rover horizontal speed (m/s).</param>
        /// <param name="normalizedSpeed">Speed as a fraction of top speed.</param>
        /// <param name="descentAngle">Angle (deg) of travel below the horizon; negative when climbing.</param>
        /// <param name="grounded">Whether the rover is on the ground (the lift holds steady in the air).</param>
        public void Step(Vector2 lookDegrees, float speed, float normalizedSpeed, float descentAngle, bool grounded,
            float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float follow = Smoothing.Factor(_tuning.LookHalfLife, deltaTime);
            _lookVelocity += (lookDegrees / deltaTime - _lookVelocity) * follow;
            YawOffset = Mathf.DeltaAngle(0f, YawOffset + _lookVelocity.x * deltaTime);
            Pitch = Mathf.Clamp(Pitch + _lookVelocity.y * deltaTime, _tuning.MinPitch, _tuning.MaxPitch);

            bool looking = lookDegrees.sqrMagnitude > InputEpsilon;
            _idleTime = looking ? 0f : _idleTime + deltaTime;
            bool recenter = _idleTime >= _tuning.RecenterDelay && speed >= _tuning.RecenterMinSpeed;
            _recenterWeight = recenter ? Mathf.Min(1f, _recenterWeight + deltaTime / _tuning.RecenterRampTime) : 0f;

            float pull = RecenterWeight;
            if (pull > 0f)
            {
                float easedTime = deltaTime * pull;
                YawOffset = Smoothing.DampAngle(YawOffset, 0f, _tuning.RecenterHalfLife, easedTime);
                Pitch = Smoothing.Damp(Pitch, _tuning.DefaultPitch, _tuning.RecenterHalfLife, easedTime);
            }

            if (grounded)
            {
                float downhill = Mathf.Clamp01(descentAngle / _tuning.DownhillFullAngle);
                Lift = Smoothing.Damp(Lift, downhill * _tuning.DownhillLift, _tuning.LiftHalfLife, deltaTime);
            }

            float fov = _tuning.BaseFov + _tuning.SpeedFovBoost * Smoothing.SmoothStep(0f, 1f, normalizedSpeed);
            FieldOfView = Smoothing.Damp(FieldOfView, fov, _tuning.FovHalfLife, deltaTime);
        }
    }
}
