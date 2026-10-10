using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Grounds a model that stands on the dust (M3-18 V1): every vertex in the lowest <see cref="FootHeight"/> of the
    /// root's space fades toward DustShadow (the per-vertex occlusion LofiLit and LofiWeather read), and a low dust
    /// skirt, a shade darker than the lit dust, rings each place where the model touches the ground. Together with
    /// World's SSAO this gives every leg, post and wall a dark foot, so nothing sits on the ground like a cut-out.
    /// </summary>
    public static class GroundOcclusion
    {
        /// <summary>Name of the child holding the dust skirts.</summary>
        public const string SkirtName = "DustSkirt";

        /// <summary>How high above the ground the foot's darkening reaches (metres, root space).</summary>
        public const float FootHeight = 0.4f;

        /// <summary>How far a vertex right on the ground leans toward DustShadow (0..1).</summary>
        public const float FootDarkness = 0.55f;

        /// <summary>
        /// The share of the foot's darkening an up-facing face keeps: it sees the open sky, so a mat or a step lying
        /// on the dust stays lit while the walls and legs beside it go dark.
        /// </summary>
        public const float UpFaceShare = 0.25f;

        /// <summary>Vertices at most this high count as touching the ground when the skirts are laid.</summary>
        private const float ContactHeight = 0.06f;

        /// <summary>Contact points closer than this share one skirt (metres).</summary>
        private const float ContactMerge = 0.7f;

        /// <summary>How far a skirt reaches past the footprint it rings (metres).</summary>
        private const float SkirtMargin = 0.2f;

        private const float SkirtMinRadius = 0.22f;
        private const float SkirtHeight = 0.035f;
        private const int SkirtSegments = 14;

        private static readonly Vector2[] SkirtProfile =
        {
            new Vector2(1f, -0.01f), new Vector2(0.7f, SkirtHeight * 0.85f), new Vector2(0f, SkirtHeight),
        };

        /// <summary>
        /// Bakes the foot's occlusion into every mesh under <paramref name="root"/> and adds the
        /// <see cref="SkirtName"/> child; returns <paramref name="root"/> for chaining into the prefab writer.
        /// </summary>
        public static ModelNode Ground(ModelNode root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var contacts = new List<Vector3>();
            var baked = new HashSet<ModelMesh>();
            Walk(root, Matrix4x4.identity, baked, contacts);
            LowPolyMeshBuilder skirts = Skirts(contacts);
            if (skirts.VertexCount > 0)
            {
                root.Add(new ModelNode(SkirtName, Vector3.zero, new ModelMesh(root.Name + "_" + SkirtName, skirts)));
            }

            return root;
        }

        /// <summary>
        /// How much a side-facing vertex at <paramref name="height"/> above the ground leans toward DustShadow.
        /// </summary>
        public static float FootOcclusion(float height)
        {
            float t = Mathf.Clamp01(height / FootHeight);
            return FootDarkness * (1f - t * t * (3f - 2f * t));
        }

        private static void Walk(ModelNode node, Matrix4x4 toRoot, HashSet<ModelMesh> baked, List<Vector3> contacts)
        {
            if (node.Mesh != null && node.Active)
            {
                // A mesh shared by several nodes is darkened once (from its first node), but each node's feet
                // touch the ground on their own.
                bool first = baked.Add(node.Mesh);
                LowPolyMeshBuilder geometry = node.Mesh.Geometry;
                IReadOnlyList<Vector3> positions = geometry.Positions;
                IReadOnlyList<Vector3> normals = geometry.Normals;
                for (int v = 0; v < positions.Count; v++)
                {
                    Vector3 p = toRoot.MultiplyPoint3x4(positions[v]);
                    if (first)
                    {
                        float up = Mathf.Clamp01(toRoot.MultiplyVector(normals[v]).normalized.y);
                        geometry.Occlude(v, FootOcclusion(p.y) * Mathf.Lerp(1f, UpFaceShare, up));
                    }

                    if (p.y <= ContactHeight && node.Material != ModelMaterial.PaletteWeather)
                    {
                        contacts.Add(p);
                    }
                }
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                ModelNode child = node.Children[i];
                Walk(child, toRoot * child.LocalMatrix, baked, contacts);
            }
        }

        /// <summary>One low drift ring per cluster of ground contacts, each sized to its cluster's footprint.</summary>
        private static LowPolyMeshBuilder Skirts(List<Vector3> contacts)
        {
            var b = new LowPolyMeshBuilder(64);
            var min = new List<Vector2>();
            var max = new List<Vector2>();
            for (int i = 0; i < contacts.Count; i++)
            {
                var p = new Vector2(contacts[i].x, contacts[i].z);
                int cluster = Nearest(min, max, p);
                if (cluster < 0)
                {
                    min.Add(p);
                    max.Add(p);
                    continue;
                }

                min[cluster] = Vector2.Min(min[cluster], p);
                max[cluster] = Vector2.Max(max[cluster], p);
            }

            for (int c = 0; c < min.Count; c++)
            {
                Vector2 centre = (min[c] + max[c]) * 0.5f;
                Vector2 half = (max[c] - min[c]) * 0.5f;
                var radii = new Vector3(Mathf.Max(half.x + SkirtMargin, SkirtMinRadius), 1f,
                    Mathf.Max(half.y + SkirtMargin, SkirtMinRadius));
                Matrix4x4 placement = Matrix4x4.TRS(new Vector3(centre.x, 0f, centre.y), Quaternion.identity, radii);
                b.Lathe(placement, SkirtProfile, SkirtSegments, PaletteSwatch.DustMid);
            }

            return b;
        }

        /// <summary>The cluster whose footprint lies within <see cref="ContactMerge"/> of <paramref name="p"/>.</summary>
        private static int Nearest(List<Vector2> min, List<Vector2> max, Vector2 p)
        {
            for (int c = 0; c < min.Count; c++)
            {
                Vector2 closest = Vector2.Max(min[c], Vector2.Min(max[c], p));
                if ((closest - p).sqrMagnitude <= ContactMerge * ContactMerge)
                {
                    return c;
                }
            }

            return -1;
        }
    }
}
