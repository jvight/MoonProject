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
