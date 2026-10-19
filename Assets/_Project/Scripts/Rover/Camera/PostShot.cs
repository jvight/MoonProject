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
    /// The camera of 07's wake, as pure functions: the wide front three-quarter for the ready performance
    /// (<see cref="Wide"/>), the eased straight moves (<see cref="Glide"/>: the push-in from the title onto the face
    /// close-up, and back and out from it to the wide shot), and the one eased move round 07 to the chase frame
    /// (<see cref="Round"/>), never past it. The close-up itself is the inventory's (<see cref="RoverCameraRig.TryComposeFaceShot"/>). 07's position and
    /// heading are inputs, so every pose is testable.
    /// </summary>
    public sealed class PostShot
    {
        private readonly PostSettings _settings;

        public PostShot(PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>The wide front three-quarter for the ready performance: all of 07 in frame, a little from above.</summary>
        public ShotPose Wide(Vector3 body, float headingDeg)
        {
            Vector3 look = body + Vector3.up * _settings.WideLookHeight;
            Vector3 back = Quaternion.Euler(-_settings.WideElevation, headingDeg + _settings.WideSideYaw, 0f)
                * Vector3.forward;
            return new ShotPose(look + back * _settings.WideDistance, Quaternion.LookRotation(-back, Vector3.up),
                _settings.WideFov);
        }

        /// <summary>A move at <paramref name="t"/> (0..1): eased in a straight line from one pose to the other.</summary>
        public static ShotPose Glide(in ShotPose from, in ShotPose to, float t)
        {
            float ease = WakeOpening.SmoothStepC2(0f, 1f, Mathf.Clamp01(t));
            return new ShotPose(Vector3.Lerp(from.Position, to.Position, ease),
                Quaternion.Slerp(from.Rotation, to.Rotation, ease), Mathf.Lerp(from.Fov, to.Fov, ease));
        }

        /// <summary>
        /// The second move, at <paramref name="t"/> (0..1): eased round 07's upright axis through
        /// <paramref name="body"/> from the wide shot to <paramref name="chase"/> the short way, its distance and height
        /// blending on the way, looking at a point that slides from the wide shot's subject to the chase frame's. It
        /// ends exactly on the chase frame and never swings past it.
        /// </summary>
        public static ShotPose Round(in ShotPose wide, in ShotPose chase, Vector3 body, float t)
        {
            float ease = WakeOpening.SmoothStepC2(0f, 1f, Mathf.Clamp01(t));
            Vector3 from = wide.Position - body;
            Vector3 to = chase.Position - body;
            float fromYaw = Mathf.Atan2(from.x, from.z) * Mathf.Rad2Deg;
            float toYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float yaw = fromYaw + Mathf.DeltaAngle(fromYaw, toYaw) * ease;
            float radius = Mathf.Lerp(new Vector2(from.x, from.z).magnitude, new Vector2(to.x, to.z).magnitude, ease);
            float height = Mathf.Lerp(from.y, to.y, ease);
            Vector3 position = body + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * radius + Vector3.up * height;

            Vector3 wideLook = wide.Position + wide.Rotation * Vector3.forward * from.magnitude;
            Vector3 chaseLook = chase.Position + chase.Rotation * Vector3.forward * to.magnitude;
            Vector3 look = Vector3.Lerp(wideLook, chaseLook, ease) - position;
            Quaternion rotation = ease >= 1f ? chase.Rotation
                : look.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look, Vector3.up) : chase.Rotation;
            return new ShotPose(position, rotation, Mathf.Lerp(wide.Fov, chase.Fov, ease));
        }
    }
}
