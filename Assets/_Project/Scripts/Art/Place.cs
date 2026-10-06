using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Placement matrices for <see cref="LowPolyMeshBuilder"/> primitives. Recipes typically
    /// <c>using static MoonProject.Art.Place;</c> and write <c>builder.Box(At(0f, 0.6f, 0f), size, swatch)</c>.
    /// Rotations are Euler degrees with Unity's convention (Z, then X, then Y).
    /// </summary>
    public static class Place
    {
        /// <summary>Euler angles that turn a primitive's +Y axis (prism/cone/lathe/torus axis) onto +X.</summary>
        public static Vector3 AlongX => new Vector3(0f, 0f, -90f);

        /// <summary>Euler angles that turn a primitive's +Y axis (prism/cone/lathe/torus axis) onto +Z.</summary>
        public static Vector3 AlongZ => new Vector3(90f, 0f, 0f);

        /// <summary>Mirror across the YZ plane (x -&gt; -x). The builder keeps faces outward under mirroring.</summary>
        public static Matrix4x4 MirrorX => Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));

        public static Matrix4x4 At(float x, float y, float z)
        {
            return Matrix4x4.Translate(new Vector3(x, y, z));
        }

        public static Matrix4x4 At(Vector3 position)
        {
            return Matrix4x4.Translate(position);
        }

        public static Matrix4x4 At(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.Translate(position) * Matrix4x4.Rotate(rotation);
        }

        public static Matrix4x4 At(Vector3 position, Vector3 eulerDegrees)
        {
            return At(position, Rotation(eulerDegrees));
        }

        public static Matrix4x4 At(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return Matrix4x4.Translate(position) * Matrix4x4.Rotate(rotation) * Matrix4x4.Scale(scale);
        }

        public static Matrix4x4 At(Vector3 position, Vector3 eulerDegrees, Vector3 scale)
        {
            return At(position, Rotation(eulerDegrees), scale);
        }

        /// <summary>
        /// Placement at the midpoint of a segment with local +Z pointing from <paramref name="from"/> to
        /// <paramref name="to"/> (local +Y stays as close to world up as possible). Size a box's Z by the segment
        /// length for struts and arms; compose with <see cref="AlongZ"/> for prisms.
        /// </summary>
        public static Matrix4x4 Along(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float length = direction.magnitude;
            if (length < 1e-6f)
            {
                throw new System.ArgumentException("Segment endpoints coincide.", nameof(to));
            }

            direction /= length;
            float pitch = Mathf.Asin(Mathf.Clamp(-direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return At((from + to) * 0.5f, new Vector3(pitch, yaw, 0f));
        }

        /// <summary>
        /// Same rotation as <c>Quaternion.Euler</c> (Z, then X, then Y), composed in managed code so recipe math makes
        /// no native engine calls and is bit-identical wherever it runs.
        /// </summary>
        public static Quaternion Rotation(Vector3 eulerDegrees)
        {
            return AxisAngle(Vector3.up, eulerDegrees.y) * AxisAngle(Vector3.right, eulerDegrees.x)
                * AxisAngle(Vector3.forward, eulerDegrees.z);
        }

        private static Quaternion AxisAngle(Vector3 unitAxis, float degrees)
        {
            float half = degrees * Mathf.Deg2Rad * 0.5f;
            float sin = Mathf.Sin(half);
            return new Quaternion(unitAxis.x * sin, unitAxis.y * sin, unitAxis.z * sin, Mathf.Cos(half));
        }
    }
}
