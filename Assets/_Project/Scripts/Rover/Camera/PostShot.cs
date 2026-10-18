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
        private readonly PostSettings _settings;

        public PostShot(PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>The whole-body three-quarter shot the pull-out arrives at and the first check starts from.</summary>
        public ShotPose Base(Vector3 body, float headingDeg)
        {
            PostFocus start = _settings.BaseFocus;
            return Compose(body, headingDeg, start.Local, start.Distance, start.Height, _settings.ShotSideYaw + start.SideYaw);
        }

        /// <summary>
        /// The shot at progress <paramref name="p"/> of the check <paramref name="step"/>. The camera is on a check's
        /// part at the middle of the check and eases from one part to the next between the middles, so it is always
        /// drifting slowly and never has to hurry across a short check.
        /// </summary>
        public ShotPose At(Vector3 body, float headingDeg, BootCheckStep step, float p)
        {
            float length = _settings.SecondsOf(step);
            PostFocus here = _settings.FocusOf(step);
            PostFocus other;
            float u;
            if (p < 0.5f)
            {
                // The pull-out arrives on the base shot exactly where the first check begins.
                float previousLength = step == BootCheckStep.Lens ? 0f : _settings.SecondsOf(step - 1);
                other = step == BootCheckStep.Lens ? _settings.BaseFocus : _settings.FocusOf(step - 1);
                u = (p * length + 0.5f * previousLength) / (0.5f * (previousLength + length));
                u = 1f - Mathf.Clamp01(u);
            }
            else
            {
                bool last = step == BootCheckStep.Begin;
                float nextLength = last ? length : _settings.SecondsOf(step + 1);
                other = last ? here : _settings.FocusOf(step + 1);
                u = Mathf.Clamp01((p - 0.5f) * length / (0.5f * (length + nextLength)));
            }

            float ease = WakeOpening.SmoothStepC2(0f, 1f, u);
            Vector3 local = Vector3.Lerp(here.Local, other.Local, ease);
            float distance = Mathf.Lerp(here.Distance, other.Distance, ease);
            float height = Mathf.Lerp(here.Height, other.Height, ease);
            float side = _settings.ShotSideYaw + Mathf.Lerp(here.SideYaw, other.SideYaw, ease);

            return Compose(body, headingDeg, local, distance, height, side);
        }

        /// <summary>
        /// The start: as the head turns toward the beacon the camera leaves the part it was on and goes over 07's
        /// shoulder, so the beacon is in frame when the head has finished turning.
        /// </summary>
        public ShotPose AtBegin(Vector3 body, float headingDeg, Vector3 beacon, float p)
        {
            float turn = WakeOpening.SmoothStepC2(0f, 1f, PostMotions.BeaconTurn(p));
            return Blend(At(body, headingDeg, BootCheckStep.Begin, p), OverShoulder(body, headingDeg, beacon), turn);
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
