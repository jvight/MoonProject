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
    /// The one camera shot of 07's wake as pure functions: a fixed three-quarter front view aimed at 07's face from a
    /// little below, far enough that all of 07 is in frame as it wakes; once the self-test prints, the camera creeps
    /// straight in along its own view line until the face fills <see cref="PostSettings.FaceFill"/> of the frame's
    /// height, so the lines read. It never turns, cuts or follows a part. The face point and 07's heading are inputs,
    /// so the shot is testable.
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
