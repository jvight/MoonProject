using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>Directions in 07's heading frame (+Z ahead) as yaw and pitch angles, for where its head looks.</summary>
    public static class LensGaze
    {
        /// <summary>Yaw and pitch (deg) of a direction given in 07's heading frame.</summary>
        public static Vector2 Angles(Vector3 local)
        {
            float flat = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            return new Vector2(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, Mathf.Atan2(local.y, flat) * Mathf.Rad2Deg);
        }
    }
}
