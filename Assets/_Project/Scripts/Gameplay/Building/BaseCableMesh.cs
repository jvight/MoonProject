using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Writes one base cable into one mesh: a round rubber lead along its path (<see cref="BaseCablePath"/>) and a plug
    /// sleeve at each end, seated on the port and lined up with the cable. Its buffers are reused from cable to cable.
    /// </summary>
    public sealed class BaseCableMesh
    {
        // Sides round the lead and round each plug sleeve.
        private const int LeadSides = 6;
        private const int SleeveSides = 8;

        // R1Lit reads vertex colour as R: open to the sky, G: edge wear, B: cavity. A plain rubber lead: open, unworn.
        private static readonly Color32 Rubber = new Color32(255, 0, 0, 255);

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();

        /// <summary>Writes the cable through <paramref name="count"/> <paramref name="points"/> into <paramref name="mesh"/>.</summary>
        public void Write(Mesh mesh, Vector3[] points, int count, BuildingTuning tuning)
        {
            _positions.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();
            Lead(points, count, tuning.CableRadius);
            Sleeve(points[0], points[0] - points[1], tuning);
            Sleeve(points[count - 1], points[count - 1] - points[count - 2], tuning);
            mesh.Clear();
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        private void Lead(Vector3[] points, int count, float radius)
        {
            float along = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 forward = (points[Mathf.Min(i + 1, count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                Frame(forward, out Vector3 side, out Vector3 up);
                along += i > 0 ? Vector3.Distance(points[i], points[i - 1]) : 0f;
                for (int k = 0; k < LeadSides; k++)
                {
                    float angle = k * Mathf.PI * 2f / LeadSides;
                    Vector3 normal = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    _positions.Add(points[i] + normal * radius);
                    _normals.Add(normal);
                    _uvs.Add(new Vector2(along, k / (float)LeadSides));
                    _colors.Add(Rubber);
                }
            }

            for (int i = 0; i < count - 1; i++)
            {
                Band(i * LeadSides, (i + 1) * LeadSides, LeadSides);
            }
        }

        /// <summary>A short capped cylinder centred on <paramref name="end"/>, its axis along <paramref name="outward"/>.</summary>
        private void Sleeve(Vector3 end, Vector3 outward, BuildingTuning tuning)
        {
            Vector3 axis = outward.sqrMagnitude > 1e-8f ? outward.normalized : Vector3.up;
            Frame(axis, out Vector3 side, out Vector3 up);
            float half = tuning.ConnectorLength * 0.5f;
            float radius = tuning.ConnectorRadius;
            int ring = _positions.Count;
            for (int r = 0; r < 2; r++)
            {
                Vector3 middle = end + axis * (r == 0 ? -half : half);
                for (int k = 0; k < SleeveSides; k++)
                {
                    float angle = k * Mathf.PI * 2f / SleeveSides;
                    Vector3 normal = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    _positions.Add(middle + normal * radius);
                    _normals.Add(normal);
                    _uvs.Add(new Vector2(r, k / (float)SleeveSides));
                    _colors.Add(Rubber);
                }
            }

            Band(ring, ring + SleeveSides, SleeveSides);
            for (int r = 0; r < 2; r++)
            {
                Vector3 facing = r == 0 ? -axis : axis;
                int centre = _positions.Count;
                _positions.Add(end + facing * half);
                _normals.Add(facing);
                _uvs.Add(new Vector2(r, 0.5f));
                _colors.Add(Rubber);
                int rim = ring + r * SleeveSides;
                for (int k = 0; k < SleeveSides; k++)
                {
                    int a = rim + k;
                    int b = rim + (k + 1) % SleeveSides;
                    if (r == 0)
                    {
                        Triangle(centre, b, a);
                    }
                    else
                    {
                        Triangle(centre, a, b);
                    }
                }
            }
        }

        /// <summary>Quads between ring <paramref name="from"/> and ring <paramref name="to"/>, facing outward.</summary>
        private void Band(int from, int to, int sides)
        {
            for (int k = 0; k < sides; k++)
            {
                int a = from + k;
                int b = from + (k + 1) % sides;
                int c = to + k;
                int d = to + (k + 1) % sides;
                Triangle(a, c, b);
                Triangle(b, c, d);
            }
        }

        private void Triangle(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }

        private static void Frame(Vector3 forward, out Vector3 side, out Vector3 up)
        {
            Vector3 reference = Mathf.Abs(forward.y) > 0.95f ? Vector3.forward : Vector3.up;
            side = Vector3.Cross(reference, forward).normalized;
            up = Vector3.Cross(forward, side);
        }
    }
}
