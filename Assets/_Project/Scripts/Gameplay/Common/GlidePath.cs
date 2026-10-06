using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The eased float every carried object uses (a relic settling onto its shelf slot, a stray relic drifting back):
    /// it glides on a soft arc to a point <c>hover</c> metres above its destination, then sinks onto it with a small
    /// overshoot and settles. Pure maths: progress in, pose out.
    /// </summary>
    public static class GlidePath
    {
        /// <summary>Share of the time spent gliding; the rest is the settle.</summary>
        public const float GlideShare = 0.72f;

        public static Vector3 Position(Vector3 start, Vector3 end, float progress, float lift, float hover,
            float overshoot)
        {
            float p = Mathf.Clamp01(progress);
            Vector3 above = end + Vector3.up * hover;
            if (p < GlideShare)
            {
                float glide = p / GlideShare;
                Vector3 position = Vector3.LerpUnclamped(start, above, Ease.InOutSine(glide));
                return position + Vector3.up * (lift * Mathf.Sin(Mathf.PI * glide));
            }

            float settle = (p - GlideShare) / (1f - GlideShare);
            return Vector3.LerpUnclamped(above, end, Ease.OutBack(settle, overshoot));
        }

        public static Quaternion Rotation(Quaternion start, Quaternion end, float progress)
        {
            return Quaternion.Slerp(start, end, Ease.InOutSine(Mathf.Clamp01(progress) / GlideShare));
        }
    }
}
