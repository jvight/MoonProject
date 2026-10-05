using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>Turns a look direction into neck yaw and head pitch, limited to what 07's neck can reach.</summary>
    public static class HeadAim
    {
        /// <summary>
        /// Yaw (x, deg, + = right) and pitch (y, deg, + = up) of <paramref name="localDirection"/> given in the frame
        /// the neck is mounted in (+Z forward, +Y up). Targets behind the reach limit turn the head as far as it goes.
        /// </summary>
        public static Vector2 Angles(Vector3 localDirection, float yawLimit, float pitchUpLimit, float pitchDownLimit)
        {
            float horizontal = Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z);
            if (horizontal < 1e-5f && Mathf.Abs(localDirection.y) < 1e-5f)
            {
                return Vector2.zero;
            }

            float yaw = horizontal < 1e-5f ? 0f : Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(localDirection.y, horizontal) * Mathf.Rad2Deg;
            return new Vector2(Mathf.Clamp(yaw, -yawLimit, yawLimit),
                Mathf.Clamp(pitch, -pitchDownLimit, pitchUpLimit));
        }
    }
}
