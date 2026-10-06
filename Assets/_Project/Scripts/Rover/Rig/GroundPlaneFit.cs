using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Least-squares plane through wheel contact points, expressed in the rover's heading frame (x right, y up,
    /// z forward). Fitting y = a*x + b*z + c over all contacts averages out single rocks and facet edges, so the body
    /// settles on the overall ground shape instead of jittering between per-wheel normals.
    /// </summary>
    public static class GroundPlaneFit
    {
        private const float MinDeterminant = 1e-6f;

        /// <summary>
        /// Fits a plane to the first <paramref name="count"/> points. Returns false when fewer than 3 points or they
        /// are (nearly) collinear in x/z; <paramref name="normal"/> is then up.
        /// </summary>
        public static bool TryFit(Vector3[] points, int count, out Vector3 normal)
        {
            normal = Vector3.up;
            if (points == null || count < 3)
            {
                return false;
            }

            float meanX = 0f;
            float meanY = 0f;
            float meanZ = 0f;
            for (int i = 0; i < count; i++)
            {
                meanX += points[i].x;
                meanY += points[i].y;
                meanZ += points[i].z;
            }

            meanX /= count;
            meanY /= count;
            meanZ /= count;

            float xx = 0f;
            float xz = 0f;
            float zz = 0f;
            float xy = 0f;
            float zy = 0f;
            for (int i = 0; i < count; i++)
            {
                float x = points[i].x - meanX;
                float y = points[i].y - meanY;
                float z = points[i].z - meanZ;
                xx += x * x;
                xz += x * z;
                zz += z * z;
                xy += x * y;
                zy += z * y;
            }

            float determinant = xx * zz - xz * xz;
            if (Mathf.Abs(determinant) < MinDeterminant)
            {
                return false;
            }

            float a = (xy * zz - zy * xz) / determinant;
            float b = (zy * xx - xy * xz) / determinant;
            normal = new Vector3(-a, 1f, -b).normalized;
            return true;
        }

        /// <summary>Nose-up pitch (deg) of a heading-frame normal.</summary>
        public static float PitchOf(Vector3 localNormal)
        {
            return Mathf.Atan2(-localNormal.z, localNormal.y) * Mathf.Rad2Deg;
        }

        /// <summary>Right-side-up roll (deg) of a heading-frame normal.</summary>
        public static float RollOf(Vector3 localNormal)
        {
            return Mathf.Atan2(-localNormal.x, localNormal.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Rotation (relative to the heading) for a nose-up pitch and right-side-up roll in degrees. Consistent with
        /// <see cref="PitchOf"/>/<see cref="RollOf"/>: the rotated up vector is the plane normal for small angles.
        /// </summary>
        public static Quaternion Tilt(float pitch, float roll)
        {
            return Quaternion.Euler(-pitch, 0f, roll);
        }
    }
}
