using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// 07's CRT face screen (<c>RoverModel/Neck/NeckLift/Head/Screen</c>), registered in the <see cref="GameContext"/>
    /// by the Rover domain so the UI domain can render the Tab inventory onto 07's face and map mouse rays through its
    /// <see cref="MeshCollider"/> to panel coordinates.
    /// </summary>
    public interface IRoverFaceScreen
    {
        /// <summary>CRT glass barrel curvature (<c>_Curvature</c>) for UV-to-panel coordinate mapping.</summary>
        float Curvature { get; }

        /// <summary>True while the face screen is open in inventory mode.</summary>
        bool IsOpen { get; }

        /// <summary>
        /// Opens or closes the face screen mode: drives the CRT channel-switch choreography, the face-inspect camera,
        /// 07's head lift and turn toward the camera, and gentle braking.
        /// </summary>
        void SetScreenOpen(bool open, Texture contentTexture);

        /// <summary>
        /// Shows <paramref name="contentTexture"/> on the CRT as a plain readout (no inspect camera, head lift or braking;
        /// 07's eyes are off), or returns the CRT to 07's own face when it is null. For the power-on self-test's lines.
        /// </summary>
        void SetReadout(Texture contentTexture);

        /// <summary>
        /// Casts <paramref name="ray"/> against the <c>Screen</c> <see cref="MeshCollider"/> and outputs its mesh UV
        /// (<see cref="RaycastHit.textureCoord"/>); false when the ray misses the screen.
        /// </summary>
        bool TryRaycastScreenUv(Ray ray, out Vector2 meshUv);
    }
}
