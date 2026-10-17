using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// An old power cable lying on the dust (opening v2 §4: from 07's dead charging post to the lander's port): a thin
    /// tube that follows the ground from one end to the other, wandering gently side to side the way a dragged cable
    /// settles. Plain logic: builds the mesh once, at boot.
    /// </summary>
    public static class GroundCable
    {
        // Sides of the tube's cross-section: round enough at a cable's few centimetres.
        private const int Sides = 6;

        /// <summary>
        /// The cable's tube from <paramref name="from"/> to <paramref name="to"/> (world XZ) on the ground given by
        /// <paramref name="groundHeight"/>, shaped by <paramref name="settings"/>.
        /// </summary>
        public static Mesh Build(Vector2 from, Vector2 to, GroundCableSettings settings,
            Func<float, float, float> groundHeight)
        {
            if (settings == null || groundHeight == null)
            {
                throw new ArgumentNullException(settings == null ? nameof(settings) : nameof(groundHeight));
            }

            Vector2 along = to - from;
            float length = along.magnitude;
            Vector2 forward = along / Mathf.Max(length, 1e-4f);
            var side = new Vector2(forward.y, -forward.x);
            int points = Mathf.Max(2, Mathf.CeilToInt(length / settings.Step) + 1);
            var centres = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points - 1);
                float wander = Mathf.Sin(t * Mathf.PI * settings.Bends) * Mathf.Sin(t * Mathf.PI) * settings.Wander;
                Vector2 flat = from + along * t + side * wander;
                centres[i] = new Vector3(flat.x, groundHeight(flat.x, flat.y) + settings.Radius, flat.y);
            }

            var vertices = new List<Vector3>(points * Sides);
            var normals = new List<Vector3>(points * Sides);
            var triangles = new List<int>((points - 1) * Sides * 6);
            for (int i = 0; i < points; i++)
            {
                Vector3 tangent = (centres[Mathf.Min(i + 1, points - 1)] - centres[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                Vector3 up = Vector3.Cross(tangent, right);
                for (int s = 0; s < Sides; s++)
                {
                    float angle = s * Mathf.PI * 2f / Sides;
                    Vector3 normal = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    vertices.Add(centres[i] + normal * settings.Radius);
                    normals.Add(normal);
                }

                if (i == 0)
                {
                    continue;
                }

                int ring = i * Sides;
                int previous = ring - Sides;
                for (int s = 0; s < Sides; s++)
                {
                    int next = (s + 1) % Sides;
                    triangles.Add(previous + s);
                    triangles.Add(ring + next);
                    triangles.Add(ring + s);
                    triangles.Add(previous + s);
                    triangles.Add(previous + next);
                    triangles.Add(ring + next);
                }
            }

            var mesh = new Mesh { name = "GroundCable" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
