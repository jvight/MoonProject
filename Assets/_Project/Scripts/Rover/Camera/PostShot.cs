using UnityEngine;
using MoonProject.Core;

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
    /// The camera of 07's power-on self-test as pure functions: a three-quarter front close shot that keeps the CRT
    /// face and the body in frame, eased toward the part each check is testing, then (at the start) round toward the
    /// way the head looks at the beacon. 07's position and heading are inputs, so the shot is testable.
    /// </summary>
    public sealed class PostShot
    {
        /// <summary>Share of a check over which the focus eases from the previous check's part to its own.</summary>
        private const float EaseShare = 0.5f;

        private readonly PostSettings _settings;

        public PostShot(PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>The whole-body three-quarter shot the pull-out arrives at and the first check starts from.</summary>
        public ShotPose Base(Vector3 body, float headingDeg)
        {
            PostFocus start = _settings.BaseFocus;
            return Compose(body, headingDeg, start.Local, start.Distance, start.Height, _settings.ShotSideYaw);
        }

        /// <summary>The shot at progress <paramref name="p"/> of the check <paramref name="step"/>.</summary>
        public ShotPose At(Vector3 body, float headingDeg, BootCheckStep step, float p)
        {
            PostFocus from = step == BootCheckStep.Lens ? _settings.BaseFocus : _settings.FocusOf(step - 1);
            PostFocus to = _settings.FocusOf(step);
            float ease = WakeOpening.SmoothStepC2(0f, EaseShare, p);
            Vector3 local = Vector3.Lerp(from.Local, to.Local, ease);
            float distance = Mathf.Lerp(from.Distance, to.Distance, ease);
            float height = Mathf.Lerp(from.Height, to.Height, ease);
            float side = _settings.ShotSideYaw;
            if (step == BootCheckStep.Begin)
            {
                // The camera swings round with the head as it turns toward the beacon.
                side = Mathf.Lerp(side, _settings.BeginSideYaw, PostMotions.BeaconTurn(p));
            }

            return Compose(body, headingDeg, local, distance, height, side);
        }

        /// <summary>
        /// The shot after the checks: behind and beside 07, looking at <paramref name="beacon"/>, where the camera
        /// settles before it eases into the chase frame.
        /// </summary>
        public ShotPose OverShoulder(Vector3 body, float headingDeg, Vector3 beacon)
        {
            Quaternion heading = Quaternion.Euler(0f, headingDeg, 0f);
            Vector3 position = body + heading * new Vector3(_settings.OverShoulderSide, _settings.OverShoulderHeight,
                -_settings.OverShoulderBack);
            Vector3 look = beacon - position;
            Quaternion rotation = look.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look, Vector3.up) : heading;
            return new ShotPose(position, rotation, _settings.ShotFov);
        }

        /// <summary>Blends two poses with an eased weight; the rotation takes the short way.</summary>
        public static ShotPose Blend(in ShotPose from, in ShotPose to, float t)
        {
            return new ShotPose(Vector3.Lerp(from.Position, to.Position, t), Quaternion.Slerp(from.Rotation, to.Rotation, t),
                Mathf.Lerp(from.Fov, to.Fov, t));
        }

        private ShotPose Compose(Vector3 body, float headingDeg, Vector3 focusLocal, float distanceScale, float height,
            float sideYaw)
        {
            Quaternion heading = Quaternion.Euler(0f, headingDeg, 0f);
            Vector3 focus = body + heading * focusLocal;
            Quaternion around = Quaternion.Euler(0f, headingDeg + sideYaw, 0f);
            float distance = _settings.ShotDistance * distanceScale;
            Vector3 position = new Vector3(focus.x, body.y + height, focus.z) + around * (Vector3.forward * distance);
            return new ShotPose(position, Quaternion.LookRotation(focus - position, Vector3.up), _settings.ShotFov);
        }
    }
}
