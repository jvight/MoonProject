using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Scores a candidate first shot of a fresh game from where Earth and 07's lens land on screen: Earth must be in
    /// the frame's upper third and the lens in the frame; among such shots, the one that sees the lens most nearly
    /// face-on wins. Shots that miss score below every framed one, closer misses higher.
    /// </summary>
    public static class WakeFrame
    {
        /// <summary>Earth's lowest viewport y: the upper third.</summary>
        public const float EarthMinY = 2f / 3f;

        /// <summary>Earth's highest viewport y, clear of the top edge.</summary>
        public const float EarthMaxY = 0.95f;

        /// <summary>How far in from the left and right edges Earth and the lens must be (viewport).</summary>
        public const float SideMargin = 0.08f;

        /// <summary>How far in from the top and bottom edges the lens must be (viewport).</summary>
        public const float LensMargin = 0.1f;

        /// <summary>Lifts every framed shot above every miss (facing is at least -1).</summary>
        private const float Framed = 2f;

        /// <param name="earth">Earth's viewport point (z = depth, negative behind the camera).</param>
        /// <param name="lens">The lens centre's viewport point.</param>
        /// <param name="facing">Cosine between the lens's gaze and the way to the camera (1 = face-on).</param>
        public static float Score(Vector3 earth, Vector3 lens, float facing)
        {
            float miss = Outside(earth.x, SideMargin, 1f - SideMargin) + Outside(earth.y, EarthMinY, EarthMaxY)
                + Outside(lens.x, SideMargin, 1f - SideMargin) + Outside(lens.y, LensMargin, 1f - LensMargin)
                + (earth.z > 0f ? 0f : 1f) + (lens.z > 0f ? 0f : 1f);
            return miss > 0f ? -miss : Framed + facing;
        }

        /// <summary>Whether Earth is in the upper third and the lens in the frame.</summary>
        public static bool IsFramed(Vector3 earth, Vector3 lens)
        {
            return Score(earth, lens, -1f) > 0f;
        }

        /// <summary>
        /// Where the chase camera would sit and look for an orbit: <paramref name="bearing"/> (deg, the way it looks)
        /// and <paramref name="elevation"/> (deg) at <paramref name="radius"/> m round <paramref name="follow"/>,
        /// aiming <paramref name="aimLift"/> m above it with the aim point <paramref name="screenY"/> below the frame's
        /// centre (Cinemachine's orbital follow and rotation composer, without damping or terrain avoidance).
        /// </summary>
        public static Pose Camera(Vector3 follow, float bearing, float elevation, float radius, float aimLift,
            float screenY, float verticalFov)
        {
            Vector3 eye = follow + Quaternion.Euler(elevation, bearing, 0f) * new Vector3(0f, 0f, -radius);
            Quaternion look = Quaternion.LookRotation(follow + Vector3.up * aimLift - eye, Vector3.up);
            float halfHeight = Mathf.Tan(0.5f * verticalFov * Mathf.Deg2Rad);
            float raise = Mathf.Atan(2f * screenY * halfHeight) * Mathf.Rad2Deg;
            return new Pose(eye, look * Quaternion.Euler(-raise, 0f, 0f));
        }

        /// <summary>Viewport point of <paramref name="point"/> from <paramref name="camera"/> (z = depth).</summary>
        public static Vector3 Viewport(Pose camera, Vector3 point, float verticalFov, float aspect)
        {
            Vector3 local = Quaternion.Inverse(camera.rotation) * (point - camera.position);
            float halfHeight = Mathf.Tan(0.5f * verticalFov * Mathf.Deg2Rad);
            float depth = Mathf.Abs(local.z) > Mathf.Epsilon ? local.z : Mathf.Epsilon;
            return new Vector3(0.5f + 0.5f * local.x / (depth * halfHeight * aspect),
                0.5f + 0.5f * local.y / (depth * halfHeight), local.z);
        }

        private static float Outside(float value, float min, float max)
        {
            return value < min ? min - value : value > max ? value - max : 0f;
        }
    }
}
