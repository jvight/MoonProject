using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Decades of neglect laid over home and over 07 (VISION ruling 12), as three removable layers so restoration can
    /// clean a part later by hiding its layer: <c>Weather_Paint</c> (sun-bleached chalky coats over the cream enamel
    /// and faded flaking stripes over the orange), <c>Weather_Rust</c> (patches and streaks running down from rivets
    /// and seams) and <c>Weather_Dust</c> (dust settled on every top face, a dust tide line round the foot of things
    /// and drifts piled against them). Each layer sits a few millimetres proud of the one under it, in that order.
    /// Big flat facets only, never texture grime.
    /// </summary>
    internal static class Weathering
    {
        public const string PaintName = "Weather_Paint";
        public const string RustName = "Weather_Rust";
        public const string DustName = "Weather_Dust";

        /// <summary>How far each layer stands off the surface under it (and so the order they stack in).</summary>
        public const float PaintLift = 0.006f;

        public const float RustLift = 0.012f;
        public const float DustLift = 0.016f;

        // Faces turned at most about 37 degrees from the sky hold dust.
        private const float DustFacing = 0.8f;

        /// <summary>
        /// The sun-bleached coat over a surface: cream enamel turns chalky, the worn orange fades. Only the
        /// painted faces are copied; metal, glass and wood are left as they are.
        /// </summary>
        public static LowPolyMeshBuilder Bleach(LowPolyMeshBuilder surface, float lift)
        {
            var coat = new LowPolyMeshBuilder(surface.TriangleCount / 2 + 1);
            coat.AppendRepainted(surface, PaletteSwatch.Enamel, PaletteSwatch.FadedPaint, lift);
            coat.AppendRepainted(surface, PaletteSwatch.Cream, PaletteSwatch.FadedPaint, lift);
            coat.AppendRepainted(surface, PaletteSwatch.WarmAccent, PaletteSwatch.FadedAccent, lift);
            return coat;
        }

        /// <summary>
        /// Dust settled on <paramref name="surface"/>: every top face and, when <paramref name="tide"/> is positive,
        /// everything below that height (the dust line round the foot of anything standing in the dust).
        /// </summary>
        public static LowPolyMeshBuilder Dust(LowPolyMeshBuilder surface, float tide, float lift)
        {
            var dust = new LowPolyMeshBuilder(surface.TriangleCount / 3 + 1);
            dust.AppendFacing(surface, Vector3.up, DustFacing, lift, PaletteSwatch.CakedDust);
            if (tide > 0f)
            {
                dust.AppendBelow(surface, tide, lift, PaletteSwatch.CakedDust);
            }

            return dust;
        }

        /// <summary>
        /// Adds the layers that have geometry to <paramref name="node"/>, their meshes named after it.
        /// </summary>
        public static void Attach(ModelNode node, string meshPrefix, LowPolyMeshBuilder paint, LowPolyMeshBuilder rust,
            LowPolyMeshBuilder dust)
        {
            Add(node, meshPrefix, PaintName, paint);
            Add(node, meshPrefix, RustName, rust);
            Add(node, meshPrefix, DustName, dust);
        }

        /// <summary>A rust collar round a joint or a foot (a short band round <paramref name="axis"/>'s Y).</summary>
        public static void Collar(LowPolyMeshBuilder b, Matrix4x4 axis, float radius, float height)
        {
            b.Prism(axis, radius, height, 8, PaletteSwatch.Rust, false);
        }

        private static void Add(ModelNode node, string meshPrefix, string layer, LowPolyMeshBuilder geometry)
        {
            if (geometry != null && geometry.TriangleCount > 0)
            {
                node.Add(new ModelNode(layer, Vector3.zero, new ModelMesh(meshPrefix + "_" + layer, geometry)));
            }
        }
    }
}
