using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How strongly a flat glassy face throws the Sun at an eye: 1 when the eye sits on the Sun's mirror in the face,
    /// falling off as it moves away (a cosine lobe of the given sharpness), 0 when the Sun or the eye is behind the
    /// face. Either side of the face catches the light. Plain logic.
    /// </summary>
    public static class SunGlint
    {
        /// <param name="towardSun">Unit direction from the face toward the Sun.</param>
        /// <param name="normal">Unit normal of the face (either side).</param>
        /// <param name="towardEye">Unit direction from the face toward the eye.</param>
        public static float Level(Vector3 towardSun, Vector3 normal, Vector3 towardEye, float sharpness)
        {
            Vector3 facing = Vector3.Dot(normal, towardSun) >= 0f ? normal : -normal;
            if (Vector3.Dot(facing, towardSun) <= 0f || Vector3.Dot(facing, towardEye) <= 0f)
            {
                return 0f;
            }

            Vector3 mirrored = Vector3.Reflect(-towardSun, facing);
            return Mathf.Pow(Mathf.Max(0f, Vector3.Dot(mirrored, towardEye)), sharpness);
        }

        /// <summary>The glint's slow shimmer at <paramref name="time"/>: 1 - depth .. 1, never two wings alike.</summary>
        public static float Shimmer(float time, float hertz, float depth, float phase)
        {
            float wave = 0.5f + 0.25f * Mathf.Sin((time * hertz + phase) * Mathf.PI * 2f)
                + 0.25f * Mathf.Sin((time * hertz * 1.73f + phase * 2.3f) * Mathf.PI * 2f);
            return 1f - depth * wave;
        }
    }
}
