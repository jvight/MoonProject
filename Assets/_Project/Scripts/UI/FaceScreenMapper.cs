using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Maps a pointer position on the player's screen through a camera ray onto 07's curved CRT <c>Screen</c> mesh and
    /// into UI Toolkit panel coordinates (<c>0..1152</c>, <c>0..768</c>), matching the horizontal mirror and barrel
    /// warp of <c>R1CrtFaceCore.hlsl</c>.
    /// </summary>
    public static class FaceScreenMapper
    {
        public static readonly Vector2 DefaultPanelSize = new Vector2(1152f, 768f);
        public static readonly Vector2 OutsidePanel = new Vector2(float.NaN, float.NaN);

        /// <summary>
        /// Mirrors the horizontal UV (<c>1 - u</c>) to match <c>R1CrtFaceCore.hlsl</c>, because the <c>Screen</c>
        /// quad's UVs run right-to-left when viewed from in front of 07's face.
        /// </summary>
        public static Vector2 MirrorUv(Vector2 meshUv)
        {
            return new Vector2(1f - meshUv.x, meshUv.y);
        }

        /// <summary>
        /// Applies the CRT barrel distortion of <c>R1CrtFaceCore.hlsl</c> (<c>WarpUv</c>) so pointer hits land on the
        /// exact pixel the shader draws at that spot on the glass.
        /// </summary>
        public static Vector2 WarpUv(Vector2 uv, float curvature)
        {
            Vector2 centered = uv * 2f - Vector2.one;
            Vector2 offset = new Vector2(centered.y, centered.x);
            centered += Vector2.Scale(centered, Vector2.Scale(offset, offset)) * curvature;
            return centered * 0.5f + new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Converts a raycast hit UV on the <c>Screen</c> mesh (<c>0..1</c>) into UI Toolkit panel coordinates
        /// (<c>0..panelSize.x</c> left-to-right, <c>0..panelSize.y</c> top-to-bottom).
        /// </summary>
        public static bool TryUvToPanel(Vector2 meshUv, float curvature, Vector2 panelSize, out Vector2 panelPosition)
        {
            if (panelSize.x <= 0f || panelSize.y <= 0f ||
                float.IsNaN(meshUv.x) || float.IsNaN(meshUv.y) ||
                meshUv.x < 0f || meshUv.x > 1f || meshUv.y < 0f || meshUv.y > 1f)
            {
                panelPosition = OutsidePanel;
                return false;
            }

            Vector2 mirrored = MirrorUv(meshUv);
            Vector2 warped = WarpUv(mirrored, curvature);
            if (warped.x < 0f || warped.x > 1f || warped.y < 0f || warped.y > 1f)
            {
                panelPosition = OutsidePanel;
                return false;
            }

            panelPosition = new Vector2(warped.x * panelSize.x, (1f - warped.y) * panelSize.y);
            return true;
        }

        /// <summary>
        /// Converts a UI Toolkit screen-space pointer position (top-left origin) into face-panel coordinates via a
        /// raycast against <paramref name="faceScreen"/>, returning <see cref="OutsidePanel"/> (<c>NaN</c>) when closed
        /// or when the pointer misses the CRT screen.
        /// </summary>
        public static Vector2 ScreenToPanel(Vector2 uiScreenPosition, Camera camera, IRoverFaceScreen faceScreen,
            Vector2 panelSize, float screenHeight = 0f)
        {
            if (camera == null || faceScreen == null || !faceScreen.IsOpen)
            {
                return OutsidePanel;
            }

            float height = screenHeight > 0f
                ? screenHeight
                : camera.pixelHeight > 0 ? camera.pixelHeight : Screen.height;
            var cameraScreenPoint = new Vector3(uiScreenPosition.x, height - uiScreenPosition.y, 0f);
            Ray ray = camera.ScreenPointToRay(cameraScreenPoint);
            if (!faceScreen.TryRaycastScreenUv(ray, out Vector2 meshUv))
            {
                return OutsidePanel;
            }

            return TryUvToPanel(meshUv, faceScreen.Curvature, panelSize, out Vector2 panelPosition)
                ? panelPosition
                : OutsidePanel;
        }
    }
}
