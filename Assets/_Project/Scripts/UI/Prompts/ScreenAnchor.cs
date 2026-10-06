using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>Maps a camera viewport point to panel coordinates (pure, EditMode-tested).</summary>
    internal static class ScreenAnchor
    {
        /// <summary>
        /// Converts <paramref name="viewport"/> (x, y in 0..1 from the bottom-left, z = depth in front of the camera)
        /// to a point in a panel of <paramref name="panelSize"/> (origin top-left), kept at least
        /// <paramref name="margin"/> from every edge. Returns false when the point is behind the camera.
        /// </summary>
        public static bool TryToPanel(Vector3 viewport, Vector2 panelSize, float margin, out Vector2 position)
        {
            if (viewport.z <= 0f)
            {
                position = default;
                return false;
            }

            float halfWidth = panelSize.x * 0.5f;
            float halfHeight = panelSize.y * 0.5f;
            float marginX = Mathf.Min(margin, halfWidth);
            float marginY = Mathf.Min(margin, halfHeight);
            position = new Vector2(
                Mathf.Clamp(viewport.x * panelSize.x, marginX, panelSize.x - marginX),
                Mathf.Clamp((1f - viewport.y) * panelSize.y, marginY, panelSize.y - marginY));
            return true;
        }

        /// <summary>
        /// Frame-rate independent smoothing: after <paramref name="halfLife"/> seconds half the gap is closed.
        /// </summary>
        public static Vector2 Follow(Vector2 current, Vector2 target, float halfLife, float deltaTime)
        {
            if (halfLife <= 0f)
            {
                return target;
            }

            return Vector2.Lerp(target, current, Mathf.Pow(0.5f, deltaTime / halfLife));
        }
    }
}
