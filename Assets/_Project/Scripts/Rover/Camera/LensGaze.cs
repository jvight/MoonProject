using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where 07's eye looks during the opening (docs/design/cutscenes.md §1.3), as yaw and pitch in degrees from its
    /// heading. The aim is set by the script; the gaze follows it on a low, critically damped spring, an old neck, and stays
    /// inside its limits.
    /// </summary>
    public sealed class LensGaze
    {
        private readonly OpeningSettings _settings;
        private DampedSpring _yaw;
        private DampedSpring _pitch;

        public LensGaze(OpeningSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public float Yaw => _yaw.Value;

        public float Pitch => _pitch.Value;

        public float AimYaw { get; private set; }

        public float AimPitch { get; private set; }

        /// <summary>Puts the gaze and its aim at rest on (<paramref name="yaw"/>, <paramref name="pitch"/>).</summary>
        public void Reset(float yaw, float pitch)
        {
            AimYaw = ClampYaw(yaw);
            AimPitch = ClampPitch(pitch);
            _yaw.Reset(AimYaw);
            _pitch.Reset(AimPitch);
        }

        /// <summary>The script's aim, held inside the limits.</summary>
        public void Aim(float yaw, float pitch)
        {
            AimYaw = ClampYaw(yaw);
            AimPitch = ClampPitch(pitch);
        }

        public void Step(float deltaTime)
        {
            float frequency = _settings.NeckFrequency;
            _yaw.Step(AimYaw, frequency, 1f, deltaTime);
            _pitch.Step(AimPitch, frequency, 1f, deltaTime);
        }

        /// <summary>Unit direction in 07's heading frame (+Z ahead) for a yaw and pitch in degrees.</summary>
        public static Vector3 Direction(float yaw, float pitch)
        {
            return Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
        }

        /// <summary>Yaw and pitch (deg) of a direction given in 07's heading frame.</summary>
        public static Vector2 Angles(Vector3 local)
        {
            float flat = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            return new Vector2(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, Mathf.Atan2(local.y, flat) * Mathf.Rad2Deg);
        }

        private float ClampYaw(float yaw)
        {
            return Mathf.Clamp(yaw, -_settings.YawLimit, _settings.YawLimit);
        }

        private float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, _settings.PitchDownLimit, _settings.PitchUpLimit);
        }
    }
}
