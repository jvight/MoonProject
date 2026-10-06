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

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                state.PositionCorrection += Offset;
            }
        }
    }
}
