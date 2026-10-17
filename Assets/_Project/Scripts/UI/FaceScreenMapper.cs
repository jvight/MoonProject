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

        /// <summary>
        /// Inverts <see cref="TryUvToPanel"/>: converts a UI Toolkit panel coordinate (<c>0..panelSize.x</c>,
        /// <c>0..panelSize.y</c>) back into the un-mirrored <c>Screen</c> mesh UV (<c>0..1</c>).
        /// </summary>
        public static bool TryPanelToUv(Vector2 panelPosition, float curvature, Vector2 panelSize, out Vector2 meshUv)
        {
            if (panelSize.x <= 0f || panelSize.y <= 0f ||
                float.IsNaN(panelPosition.x) || float.IsNaN(panelPosition.y) ||
                panelPosition.x < 0f || panelPosition.x > panelSize.x ||
                panelPosition.y < 0f || panelPosition.y > panelSize.y)
            {
                meshUv = OutsidePanel;
                return false;
            }

            var targetWarped = new Vector2(panelPosition.x / panelSize.x, 1f - panelPosition.y / panelSize.y);
            Vector2 mirrored = targetWarped;
            for (int i = 0; i < 8; i++)
            {
                Vector2 error = WarpUv(mirrored, curvature) - targetWarped;
                mirrored -= error;
            }

            meshUv = MirrorUv(mirrored);
            if (meshUv.x < 0f || meshUv.x > 1f || meshUv.y < 0f || meshUv.y > 1f)
            {
                meshUv = OutsidePanel;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds the camera screen-space pixel position (bottom-left origin, matching <c>Mouse.current.position</c> and
        /// <see cref="Camera.ScreenPointToRay(Vector3)"/>) whose ray through <paramref name="camera"/> hits
        /// <paramref name="faceScreen"/> at <paramref name="panelPosition"/> (<c>0..panelSize.x</c>, <c>0..panelSize.y</c>).
        /// </summary>
        public static bool TryPanelToScreen(Vector2 panelPosition, Camera camera, IRoverFaceScreen faceScreen,
            Vector2 panelSize, out Vector2 cameraScreenPosition, float screenHeight = 0f)
        {
            if (camera == null || faceScreen == null || !faceScreen.IsOpen ||
                panelSize.x <= 0f || panelSize.y <= 0f)
            {
                cameraScreenPosition = OutsidePanel;
                return false;
            }

            float width = camera.pixelWidth > 0 ? camera.pixelWidth : Screen.width;
            float height = screenHeight > 0f
                ? screenHeight
                : camera.pixelHeight > 0 ? camera.pixelHeight : Screen.height;
            var guess = new Vector2(width * 0.5f, height * 0.5f);
            const float deltaPixels = 4f;
            for (int iter = 0; iter < 8; iter++)
            {
                var uiGuess = new Vector2(guess.x, height - guess.y);
                Vector2 current = ScreenToPanel(uiGuess, camera, faceScreen, panelSize, height);
                if (float.IsNaN(current.x))
                {
                    break;
                }

                Vector2 err = panelPosition - current;
                if (err.sqrMagnitude < 0.04f)
                {
                    cameraScreenPosition = guess;
                    return true;
                }

                Vector2 px = ScreenToPanel(new Vector2(guess.x + deltaPixels, height - guess.y), camera, faceScreen,
                    panelSize, height);
                Vector2 py = ScreenToPanel(new Vector2(guess.x, height - (guess.y + deltaPixels)), camera, faceScreen,
                    panelSize, height);
                if (float.IsNaN(px.x) || float.IsNaN(py.x))
                {
                    break;
                }

                Vector2 dPdx = (px - current) / deltaPixels;
                Vector2 dPdy = (py - current) / deltaPixels;
                float det = dPdx.x * dPdy.y - dPdx.y * dPdy.x;
                if (Mathf.Abs(det) < 1e-5f)
                {
                    break;
                }

                float stepX = (err.x * dPdy.y - err.y * dPdy.x) / det;
                float stepY = (dPdx.x * err.y - dPdx.y * err.x) / det;
                guess.x += Mathf.Clamp(stepX, -width * 0.35f, width * 0.35f);
                guess.y += Mathf.Clamp(stepY, -height * 0.35f, height * 0.35f);
            }

            var finalUi = new Vector2(guess.x, height - guess.y);
            Vector2 finalPanel = ScreenToPanel(finalUi, camera, faceScreen, panelSize, height);
            if (!float.IsNaN(finalPanel.x) && (finalPanel - panelPosition).sqrMagnitude <= 4f)
            {
                cameraScreenPosition = guess;
                return true;
            }

            cameraScreenPosition = OutsidePanel;
            return false;
        }
    }
}
