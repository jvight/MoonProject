using UnityEngine;
using Unity.Cinemachine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Adds a small world-space offset to the camera position after the body stage, bypassing Cinemachine damping so
    /// the landing bump reads crisply yet stays eased (the offset itself comes from a spring in
    /// <see cref="RoverCameraRig"/>). Placed before the decollider/deoccluder so terrain avoidance still applies.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverCameraBump : CinemachineExtension
    {
        /// <summary>World-space offset applied this frame.</summary>
        public Vector3 Offset { get; set; }

        /// <summary>0..1 blend weight for aligning the camera eye, roll, and up-vector with 07's CRT Screen.</summary>
        public float FaceInspectWeight { get; set; }

        /// <summary>Solved camera eye position for face-inspect mode.</summary>
        public Vector3 FaceInspectEye { get; set; }

        /// <summary>Solved camera rotation (square to 07's CRT Screen with Screen.up as up-vector).</summary>
        public Quaternion FaceInspectRotation { get; set; } = Quaternion.identity;

        /// <summary>Solved up-vector of 07's CRT Screen in world space.</summary>
        public Vector3 FaceInspectUp { get; set; } = Vector3.up;

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                state.PositionCorrection += Offset;
                if (FaceInspectWeight > 0f)
                {
                    state.ReferenceUp = Vector3.Slerp(state.ReferenceUp, FaceInspectUp, FaceInspectWeight);
                    state.RawPosition = Vector3.Lerp(state.RawPosition, FaceInspectEye, FaceInspectWeight);
                }
            }
            else if (stage == CinemachineCore.Stage.Aim && FaceInspectWeight > 0f)
            {
                state.ReferenceUp = Vector3.Slerp(state.ReferenceUp, FaceInspectUp, FaceInspectWeight);
                state.RawOrientation = Quaternion.Slerp(state.RawOrientation, FaceInspectRotation, FaceInspectWeight);
            }
        }
    }
}
