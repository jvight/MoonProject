using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The portrait camera's pose and projection in 07's own frame, as plain logic: where the camera sits and how a
    /// point on 07 lands in the render (0..1, origin bottom-left, as Unity's viewport). The camera rides 07, so a socket
    /// keeps its place in the picture wherever 07 is.
    /// </summary>
    public static class PortraitFraming
    {
        /// <summary>The camera's local position and rotation under 07's root.</summary>
        public static void Pose(PortraitSettings settings, out Vector3 position, out Quaternion rotation)
        {
            Vector3 target = Vector3.up * settings.LookHeight;
            Quaternion orbit = Quaternion.Euler(settings.Elevation, settings.Yaw + 180f, 0f);
            position = target - orbit * Vector3.forward * settings.Distance;
            rotation = orbit;
        }

        /// <summary>
        /// Where <paramref name="point"/> (in 07's root frame) lands in the render; false behind the camera or
        /// outside the frame.
        /// </summary>
        public static bool TryViewport(PortraitSettings settings, Vector3 point, out Vector2 uv)
        {
            Pose(settings, out Vector3 position, out Quaternion rotation);
            Vector3 view = Quaternion.Inverse(rotation) * (point - position);
            if (view.z <= settings.Near)
            {
                uv = default;
                return false;
            }

            float halfHeight = Mathf.Tan(0.5f * settings.FieldOfView * Mathf.Deg2Rad) * view.z;
            float halfWidth = halfHeight * settings.Aspect;
            uv = new Vector2(0.5f + 0.5f * view.x / halfWidth, 0.5f + 0.5f * view.y / halfHeight);
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }
    }
}
