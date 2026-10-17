using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// A camera pose composed for 07's CRT screen in face-inspect mode: the look target on the screen, the camera eye
    /// position, the camera distance and vertical FOV that keep the screen at ~75% of view height, the neck yaw offset
    /// 07 turns to meet the camera, and the occlusion probe choice.
    /// </summary>
    public readonly struct FaceInspectShot
    {
        public FaceInspectShot(
            Vector3 focus,
            Vector3 eye,
            float distance,
            float verticalFov,
            float neckYawOffset,
            FaceInspectProbeChoice choice,
            Vector3 screenUp = default)
        {
            Focus = focus;
            Eye = eye;
            Distance = distance;
            VerticalFov = verticalFov;
            NeckYawOffset = neckYawOffset;
            Choice = choice;
            ScreenUp = screenUp.sqrMagnitude > 1e-6f ? screenUp.normalized : Vector3.up;
            Vector3 forward = focus - eye;
            Rotation = forward.sqrMagnitude > 1e-8f
                ? Quaternion.LookRotation(forward.normalized, ScreenUp)
                : Quaternion.identity;
        }

        public Vector3 Focus { get; }

        public Vector3 Eye { get; }

        public float Distance { get; }

        public float VerticalFov { get; }

        public float NeckYawOffset { get; }

        public FaceInspectProbeChoice Choice { get; }

        public Vector3 ScreenUp { get; }

        public Quaternion Rotation { get; }
    }
}
