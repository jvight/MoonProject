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
    /// The camera of 07's wake as pure functions. First one fixed three-quarter front view aimed at 07's face from a
    /// little below, far enough that all of 07 is in frame as it wakes; once the self-test prints, the camera creeps
    /// straight in along its own view line until the face fills <see cref="PostSettings.FaceFill"/> of the frame's
    /// height, so the lines read. It never turns, cuts or follows a part there. After the checks it makes exactly two
    /// eased moves: back and out diagonally to a wide front three-quarter (<see cref="Wide"/>, <see cref="PullBack"/>),
    /// and round 07 to the chase frame (<see cref="Round"/>), never past it. 07's position, face point and heading are
    /// inputs, so every pose is testable.
    /// </summary>
    public sealed class PostShot
    {
        private readonly PostSettings _settings;
        private readonly float _faceHeight;

        /// <param name="faceHeight">Height (m) of 07's CRT face screen, the thing the push-in frames.</param>
        public PostShot(PostSettings settings, float faceHeight)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _faceHeight = faceHeight > 0f
                ? faceHeight
                : throw new System.ArgumentOutOfRangeException(nameof(faceHeight), "the face needs a height");
        }

        /// <summary>How far (m) the camera stands from the face at the end of the push-in.</summary>
        public float CloseDistance =>
            _faceHeight / (_settings.FaceFill * 2f * Mathf.Tan(0.5f * _settings.ShotFov * Mathf.Deg2Rad));

        /// <summary>
        /// The shot at <paramref name="push01"/> of the push-in (0 wide on all of 07, 1 close on the face), looking at
        /// <paramref name="face"/>: the rotation and the lens never change, only the distance along the view line.
        /// </summary>
        public ShotPose At(Vector3 face, float headingDeg, float push01)
        {
            Vector3 back = Back(headingDeg);
            float ease = WakeOpening.SmoothStepC2(0f, 1f, Mathf.Clamp01(push01));
            float distance = Mathf.Lerp(_settings.ShotDistance, Mathf.Min(CloseDistance, _settings.ShotDistance), ease);
            return new ShotPose(face + back * distance, Quaternion.LookRotation(-back, Vector3.up), _settings.ShotFov);
        }

        /// <summary>The wide front three-quarter for the ready performance: all of 07 in frame, a little from above.</summary>
        public ShotPose Wide(Vector3 body, float headingDeg)
        {
            Vector3 look = body + Vector3.up * _settings.WideLookHeight;
            Vector3 back = Quaternion.Euler(-_settings.WideElevation, headingDeg + _settings.WideSideYaw, 0f)
                * Vector3.forward;
            return new ShotPose(look + back * _settings.WideDistance, Quaternion.LookRotation(-back, Vector3.up),
                _settings.ShotFov);
        }

        /// <summary>The first move, at <paramref name="t"/> (0..1): eased in a straight line from the close-up to the wide shot.</summary>
        public static ShotPose PullBack(in ShotPose close, in ShotPose wide, float t)
        {
            float ease = WakeOpening.SmoothStepC2(0f, 1f, Mathf.Clamp01(t));
            return new ShotPose(Vector3.Lerp(close.Position, wide.Position, ease),
                Quaternion.Slerp(close.Rotation, wide.Rotation, ease), Mathf.Lerp(close.Fov, wide.Fov, ease));
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

        /// <summary>
        /// Where the shot's camera is as (yaw, pitch) deg from 07's heading, seen from <paramref name="face"/>: where
        /// 07's head turns to look into the shot.
        /// </summary>
        public Vector2 FaceAngles(float headingDeg)
        {
            Quaternion toLocal = Quaternion.Inverse(Quaternion.Euler(0f, headingDeg, 0f));
            return LensGaze.Angles(toLocal * Back(headingDeg));
        }

        /// <summary>The direction from the face back to the camera: round from 07's front, a little below.</summary>
        private Vector3 Back(float headingDeg)
        {
            return Quaternion.Euler(-_settings.ShotElevation, headingDeg + _settings.ShotSideYaw, 0f) * Vector3.forward;
        }
    }
}
