using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>A camera pose: where it is, which way it looks and its lens.</summary>
    public readonly struct ShotPose
    {
        public ShotPose(Vector3 position, Quaternion rotation, float fov)
        {
            Position = position;
            Rotation = rotation;
            Fov = fov;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public float Fov { get; }
    }

    /// <summary>
    /// The one camera shot of 07's wake as pure functions: a fixed three-quarter front view of 07's face, a little low,
    /// that only creeps in a few percent over the whole wake. It never cuts, orbits or follows a part; each check's
    /// motion is seen from here. 07's position and heading are inputs, so the shot is testable.
    /// </summary>
    public sealed class PostShot
    {
        private readonly PostSettings _settings;

        public PostShot(PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// The shot at <paramref name="push01"/> of the wake's push-in (0 at the black, 1 at the end of the checks):
        /// the same pose throughout but for the eased creep toward the point it looks at.
        /// </summary>
        public ShotPose At(Vector3 body, float headingDeg, float push01)
        {
            Quaternion heading = Quaternion.Euler(0f, headingDeg, 0f);
            Vector3 look = body + heading * _settings.ShotLook;
            Quaternion around = Quaternion.Euler(0f, headingDeg + _settings.ShotSideYaw, 0f);
            Vector3 start = new Vector3(look.x, body.y + _settings.ShotHeight, look.z)
                + around * (Vector3.forward * _settings.ShotDistance);
            float creep = _settings.PushIn * WakeOpening.SmoothStepC2(0f, 1f, Mathf.Clamp01(push01));
            Vector3 position = Vector3.Lerp(start, look, creep);
            return new ShotPose(position, Quaternion.LookRotation(look - start, Vector3.up), _settings.ShotFov);
        }

        /// <summary>
        /// Where the shot's camera is as (yaw, pitch) deg from 07's heading, seen from <paramref name="face"/>: where
        /// 07's head turns to look into the shot.
        /// </summary>
        public Vector2 FaceAngles(Vector3 body, float headingDeg, Vector3 face)
        {
            Quaternion toLocal = Quaternion.Inverse(Quaternion.Euler(0f, headingDeg, 0f));
            return LensGaze.Angles(toLocal * (At(body, headingDeg, 0f).Position - face));
        }
    }
}
