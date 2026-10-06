using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The chassis riding on soft springs: pitch, roll and heave follow the rover's acceleration (heading frame) with
    /// underdamped springs, so speeding up squats the nose up, braking dips it, turns roll outward, landings squash
    /// and everything settles with one soft overshoot. Also drives the antenna, which is lighter and wobblier.
    /// </summary>
    public sealed class ChassisJelly
    {
        private readonly RoverRigTuning _tuning;
        private DampedSpring _pitch;
        private DampedSpring _roll;
        private DampedSpring _heave;
        private DampedSpring _antennaPitch;
        private DampedSpring _antennaRoll;

        public ChassisJelly(RoverRigTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Nose-up lean in degrees.</summary>
        public float Pitch => _pitch.Value;

        /// <summary>Right-side-up lean in degrees (positive in right turns: rolls outward, left side down).</summary>
        public float Roll => _roll.Value;

        /// <summary>Vertical offset in metres (negative = squashed down).</summary>
        public float Heave => _heave.Value;

        /// <summary>Antenna tilt backwards in degrees.</summary>
        public float AntennaPitch => _antennaPitch.Value;

        /// <summary>Antenna tilt toward the left in degrees (outward in right turns).</summary>
        public float AntennaRoll => _antennaRoll.Value;

        public void Reset()
        {
            _pitch.Reset(0f);
            _roll.Reset(0f);
            _heave.Reset(0f);
            _antennaPitch.Reset(0f);
            _antennaRoll.Reset(0f);
        }

        /// <summary>Kicks the antenna sideways (deg/s): a quick wiggle that rings out on its own.</summary>
        public void KickAntenna(float degreesPerSecond)
        {
            _antennaRoll.AddVelocity(degreesPerSecond);
        }

        /// <summary>Kicks the chassis bob (m/s, negative = down): an extra squash on top of the physics.</summary>
        public void KickHeave(float metresPerSecond)
        {
            _heave.AddVelocity(metresPerSecond);
        }

        /// <summary>
        /// Advances all springs. <paramref name="localAcceleration"/>: x right, y up, z forward.
        /// <paramref name="crouch"/> (0..1, the Hover-Jump charge) sinks and squats the chassis onto its springs.
        /// </summary>
        public void Step(Vector3 localAcceleration, float crouch, float deltaTime)
        {
            Vector3 a = Vector3.ClampMagnitude(localAcceleration, _tuning.AccelerationLimit);
            crouch = Mathf.Clamp01(crouch);

            _pitch.Step(a.z * _tuning.PitchPerAcceleration + crouch * _tuning.CrouchLean, _tuning.LeanFrequency,
                _tuning.LeanDamping, deltaTime);
            _pitch.Clamp(-_tuning.MaxLean, _tuning.MaxLean);
            _roll.Step(a.x * _tuning.RollPerAcceleration, _tuning.LeanFrequency, _tuning.LeanDamping, deltaTime);
            _roll.Clamp(-_tuning.MaxLean, _tuning.MaxLean);
            _heave.Step(-a.y * _tuning.HeavePerAcceleration - crouch * _tuning.CrouchDepth, _tuning.HeaveFrequency,
                _tuning.HeaveDamping, deltaTime);
            _heave.Clamp(-_tuning.MaxHeave, _tuning.MaxHeave);

            float antenna = _tuning.AntennaDegreesPerAcceleration;
            float limit = _tuning.AntennaMaxAngle;
            _antennaPitch.Step(a.z * antenna, _tuning.AntennaFrequency, _tuning.AntennaDamping, deltaTime);
            _antennaPitch.Clamp(-limit, limit);
            _antennaRoll.Step(a.x * antenna, _tuning.AntennaFrequency, _tuning.AntennaDamping, deltaTime);
            _antennaRoll.Clamp(-limit, limit);
        }
    }
}
