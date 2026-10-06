using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>Small composite shapes shared by the model recipes (rods, struts, pivot placement).</summary>
    internal static class RecipeKit
    {
        /// <summary>Thin prism from <paramref name="from"/> to <paramref name="to"/> (cables, whips, axles).</summary>
        public static MeshRange Rod(LowPolyMeshBuilder b, Vector3 from, Vector3 to, float radius, int sides,
            PaletteSwatch swatch)
        {
            return b.Prism(Place.Along(from, to) * Matrix4x4.Rotate(Place.Rotation(Place.AlongZ)), radius,
                Vector3.Distance(from, to), sides, swatch);
        }

        /// <summary>A lightly chamfered bar from <paramref name="from"/> to <paramref name="to"/>, overlapping
        /// its end joints by half its height.</summary>
        public static MeshRange Strut(LowPolyMeshBuilder b, Vector3 from, Vector3 to, Vector2 section,
            PaletteSwatch swatch)
        {
            float length = Vector3.Distance(from, to) + section.y;
            return b.Box(Place.Along(from, to), new Vector3(section.x, section.y, length), swatch, 0.008f);
        }

        /// <summary>Moves everything in <paramref name="b"/> so the origin is its centre of mass.</summary>
        public static LowPolyMeshBuilder CentredOnMass(LowPolyMeshBuilder b)
        {
            b.Transform(b.RangeFrom(0), Matrix4x4.Translate(-b.VolumeCentroid()));
            return b;
        }

        /// <summary>Point of a profile plane at an angle measured from +Z towards +Y, at a radius.</summary>
        public static Vector2 Polar(float degrees, float radius)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(-radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
        }
    }
}
