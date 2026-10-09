using System.Collections.Generic;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// A pinhole camera that rasterises model triangles into a coverage mask, so tests can measure how a model reads in
    /// silhouette from a given view (pixels covered, pixels one part adds to another's outline).
    /// </summary>
    internal sealed class SilhouetteCamera
    {
        private const float Near = 0.05f;

        private readonly Vector3 _position;
        private readonly Vector3 _right;
        private readonly Vector3 _up;
        private readonly Vector3 _forward;
        private readonly float _tanHalf;
        private readonly float _aspect;

        public SilhouetteCamera(Vector3 position, Vector3 target, float verticalFov, int width, int height)
        {
            _position = position;
            _forward = (target - position).normalized;
            _right = Vector3.Cross(Vector3.up, _forward).normalized;
            _up = Vector3.Cross(_forward, _right);
            _tanHalf = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            Width = width;
            Height = height;
            _aspect = (float)width / height;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// A camera <paramref name="distance"/> from <paramref name="target"/>, raised <paramref name="pitch"/>
        /// degrees and swung <paramref name="yaw"/> degrees from straight behind (+ = to the model's right).
        /// </summary>
        public static SilhouetteCamera Orbit(Vector3 target, float distance, float pitch, float yaw, float fov,
            int width, int height)
        {
            float pitchRad = pitch * Mathf.Deg2Rad;
            float yawRad = (180f - yaw) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Sin(yawRad) * Mathf.Cos(pitchRad), Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad));
            return new SilhouetteCamera(target + offset * distance, target, fov, width, height);
        }

        /// <summary>
        /// Every visible triangle under <paramref name="node"/> in <paramref name="parent"/>'s space.
        /// </summary>
        public static void CollectTriangles(ModelNode node, Matrix4x4 parent, List<Vector3> triangles)
        {
            if (!node.Active)
            {
                return;
            }

            Matrix4x4 local = parent * node.LocalMatrix;
            if (node.Mesh != null)
            {
                IReadOnlyList<Vector3> positions = node.Mesh.Geometry.Positions;
                for (int v = 0; v < positions.Count; v++)
                {
                    triangles.Add(local.MultiplyPoint3x4(positions[v]));
                }
            }

            foreach (ModelNode child in node.Children)
            {
                CollectTriangles(child, local, triangles);
            }
        }

        /// <summary>
        /// The world matrix of the descendant at <paramref name="path"/> ('/'-separated) under the root.
        /// </summary>
        public static Matrix4x4 WorldOf(ModelNode root, string path)
        {
            Matrix4x4 matrix = root.LocalMatrix;
            ModelNode node = root;
            foreach (string name in path.Split('/'))
            {
                node = Child(node, name);
                matrix *= node.LocalMatrix;
            }

            return matrix;
        }

        /// <summary>Marks every pixel a triangle of <paramref name="triangles"/> covers.</summary>
        public void Rasterize(List<Vector3> triangles, bool[] mask)
        {
            for (int t = 0; t + 2 < triangles.Count; t += 3)
            {
                if (Project(triangles[t], out Vector2 a) && Project(triangles[t + 1], out Vector2 b)
                    && Project(triangles[t + 2], out Vector2 c))
                {
                    Fill(a, b, c, mask);
                }
            }
        }

        public bool[] NewMask()
        {
            return new bool[Width * Height];
        }

        /// <summary>Pixels set in <paramref name="mask"/> but not in <paramref name="without"/>.</summary>
        public static int Added(bool[] mask, bool[] without)
        {
            int count = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] && !without[i])
                {
                    count++;
                }
            }

            return count;
        }

        private static ModelNode Child(ModelNode node, string name)
        {
            foreach (ModelNode child in node.Children)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }

            throw new KeyNotFoundException($"{node.Name} has no child {name}");
        }

        private bool Project(Vector3 world, out Vector2 pixel)
        {
            Vector3 d = world - _position;
            float z = Vector3.Dot(d, _forward);
            pixel = default;
            if (z < Near)
            {
                return false;
            }

            float x = Vector3.Dot(d, _right) / (z * _tanHalf * _aspect);
            float y = Vector3.Dot(d, _up) / (z * _tanHalf);
            pixel = new Vector2((x * 0.5f + 0.5f) * Width, (0.5f - y * 0.5f) * Height);
            return true;
        }

        private void Fill(Vector2 a, Vector2 b, Vector2 c, bool[] mask)
        {
            float area = Edge(a, b, c);
            if (Mathf.Abs(area) < 1e-9f)
            {
                return;
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = Edge(b, c, p) / area;
                    float w1 = Edge(c, a, p) / area;
                    float w2 = Edge(a, b, p) / area;
                    if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                    {
                        mask[y * Width + x] = true;
                    }
                }
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }
    }
}
