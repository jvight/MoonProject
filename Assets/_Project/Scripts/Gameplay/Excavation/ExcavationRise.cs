using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How a relic rises under the beam. Progress (0..1) is cumulative and kept between holds; each hold continues
    /// from where the relic is toward the point ahead of 07's eye: it comes up out of the ground first (height eases
    /// out quickly) and then drifts across to 07 (horizontal eases in and out), so it never slides through the soil.
    /// </summary>
    public static class ExcavationRise
    {
        /// <summary>
        /// Progress of the current hold, 0..1, given the cumulative <paramref name="progress"/> and the progress the
        /// hold started at.
        /// </summary>
        public static float Segment(float progress, float startProgress)
        {
            if (startProgress >= 1f)
            {
                return 1f;
            }

            return Mathf.Clamp01((progress - startProgress) / (1f - startProgress));
        }

        public static Vector3 Position(Vector3 from, Vector3 to, float segment)
        {
            float across = Ease.InOutSine(segment);
            float up = Ease.OutCubic(segment);
            return new Vector3(Mathf.LerpUnclamped(from.x, to.x, across), Mathf.LerpUnclamped(from.y, to.y, up),
                Mathf.LerpUnclamped(from.z, to.z, across));
        }

        /// <summary>
        /// The point <paramref name="distance"/> metres ahead of <paramref name="eye"/> along 07's horizontal
        /// heading (its height is the eye's; the caller sets it above the ground there).
        /// </summary>
        public static Vector3 Ahead(Vector3 eye, Vector3 forward, float distance)
        {
            var flat = new Vector3(forward.x, 0f, forward.z);
            flat = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
            return eye + flat * distance;
        }

        /// <summary>Cumulative progress after lifting for <paramref name="deltaTime"/> seconds.</summary>
        public static float Advance(float progress, float deltaTime, float duration)
        {
            return duration <= 0f ? 1f : Mathf.Clamp01(progress + deltaTime / duration);
        }
    }
}
