using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The cut line's geometry (docs/design/feel-pass.md §3.2): where along a piece's seam the hot point is at a given
    /// progress, measured by length so it crawls at an even pace, and the default seam the content builder writes for
    /// a site until Art authors one: straight across the piece's face through its cut point.
    /// </summary>
    public static class SalvageCutLines
    {
        /// <summary>The default seam spans this share of the piece's width across its face.</summary>
        private const float SpanShare = 0.7f;

        private const string CutPointName = "CutPoint";

        /// <summary>The point <paramref name="progress"/> (0..1) of the way along the polyline by length.</summary>
        public static Vector3 PointAt(IReadOnlyList<Vector3> points, float progress)
        {
            int count = points.Count;
            if (count == 1)
            {
                return points[0];
            }

            float length = 0f;
            for (int i = 1; i < count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }

            float remaining = Mathf.Clamp01(progress) * length;
            for (int i = 1; i < count; i++)
            {
                float segment = Vector3.Distance(points[i - 1], points[i]);
                if (remaining <= segment && segment > 0f)
                {
                    return Vector3.Lerp(points[i - 1], points[i], remaining / segment);
                }

                remaining -= segment;
            }

            return points[count - 1];
        }

        /// <summary>
        /// A seam for every Salvage_&lt;n&gt;_&lt;Material&gt; piece of the site prefab: three points across its
        /// face (its CutPoint's x axis), through the cut point, spanning most of the piece's width there.
        /// </summary>
        public static PieceCutLine[] Across(GameObject sitePrefab)
        {
            var lines = new List<PieceCutLine>();
            Transform root = sitePrefab.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform node = root.GetChild(i);
                Transform cut = node.Find(CutPointName);
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out _, out _) || cut == null ||
                    !node.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds bounds = filter.sharedMesh.bounds;
                float left = 0f;
                float right = 0f;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                            (corner & 4) == 0 ? -1f : 1f));
                    float x = cut.InverseTransformPoint(node.TransformPoint(local)).x;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                }

                lines.Add(new PieceCutLine(number, new[]
                {
                    new Vector3(left * SpanShare, 0f, 0f), Vector3.zero, new Vector3(right * SpanShare, 0f, 0f),
                }));
            }

            return lines.ToArray();
        }
    }
}
