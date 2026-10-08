using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Pitch and roll of the visual rover relative to its heading. On the ground both springs chase the plane fitted
    /// through the wheel contacts; in the air they drift toward level with the nose following the flight path a little,
    /// so every landing starts from a near-upright pose. Targets are clamped to the max tilt: the model cannot flip.
    /// </summary>
    public sealed class BodyAttitude
    {
        private readonly RoverRigTuning _tuning;
        private DampedSpring _pitch;
        private DampedSpring _roll;

        public BodyAttitude(RoverRigTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Nose-up pitch in degrees.</summary>
        public float Pitch => _pitch.Value;

        /// <summary>Right-side-up roll in degrees.</summary>
        public float Roll => _roll.Value;

        public void Reset(float pitch, float roll)
        {
            _pitch.Reset(pitch);
            _roll.Reset(roll);
        }

        /// <summary>At rest on the ground plane given as a normal in the heading frame (a placement).</summary>
        public void ResetTo(Vector3 localGroundNormal)
        {
            float limit = _tuning.MaxTilt;
            Reset(Mathf.Clamp(GroundPlaneFit.PitchOf(localGroundNormal), -limit, limit),
                Mathf.Clamp(GroundPlaneFit.RollOf(localGroundNormal), -limit, limit));
        }

        /// <summary>Settles toward the ground plane given as a normal in the heading frame.</summary>
        public void StepGrounded(Vector3 localGroundNormal, float deltaTime)
        {
            float limit = _tuning.MaxTilt;
            float pitch = Mathf.Clamp(GroundPlaneFit.PitchOf(localGroundNormal), -limit, limit);
            float roll = Mathf.Clamp(GroundPlaneFit.RollOf(localGroundNormal), -limit, limit);
            Step(pitch, roll, _tuning.AlignFrequency, _tuning.AlignDamping, deltaTime);
        }

        /// <summary>
        /// Drifts toward level, nose partly following the flight path. The follow fades out at low forward speed so a
        /// near-vertical drop stays level instead of nosing over; flying backwards tips the tail along the path.
        /// </summary>
        public void StepAirborne(float verticalSpeed, float forwardSpeed, float deltaTime)
        {
            float along = Mathf.Abs(forwardSpeed);
            float pathPitch = Mathf.Atan2(verticalSpeed, along) * Mathf.Rad2Deg * Mathf.Sign(forwardSpeed);
            float follow = _tuning.AirPitchFollow * Smoothing.SmoothStep(0f, _tuning.AirPitchFullSpeed, along);
            float pitch = Mathf.Clamp(pathPitch * follow, -_tuning.MaxAirPitch, _tuning.MaxAirPitch);
            Step(pitch, 0f, _tuning.AirAlignFrequency, _tuning.AirAlignDamping, deltaTime);
        }

        private void Step(float pitch, float roll, float frequency, float damping, float deltaTime)
        {
            float limit = _tuning.MaxTilt;
            _pitch.Step(pitch, frequency, damping, deltaTime);
            _pitch.Clamp(-limit, limit);
            _roll.Step(roll, frequency, damping, deltaTime);
            _roll.Clamp(-limit, limit);
        }
    }
}
