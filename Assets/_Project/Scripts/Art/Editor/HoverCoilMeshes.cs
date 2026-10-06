using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The Hover-Jump upgrade for 07: four compact springs hanging from a plate under the chassis, each ending in a
    /// metal foot pad ringed with dark glass that glows cyan while the jump charges. Built in the mount's space:
    /// origin = the CoilSocket (bottom of 07's belly plate), +Y up, the coils hanging below.
    /// </summary>
    internal static class HoverCoilMeshes
    {
        public const int CoilCount = 4;

        /// <summary>Length of a relaxed spring below its pivot (the foot pad sits just under it).</summary>
        public const float SpringLength = 0.1f;

        private const float MountThickness = 0.025f;
        private const float SpringRadius = 0.058f;
        private const float WireRadius = 0.0095f;
        private const float Turns = 2.5f;
        private const int SegmentsPerTurn = 6;
        private const float PadRadius = 0.066f;
        private const float PadThickness = 0.014f;

        /// <summary>Pivot of coil <paramref name="index"/> (0 FL, 1 FR, 2 RL, 3 RR): the top of its spring.</summary>
        public static Vector3 CoilTop(int index)
        {
            if (index < 0 || index >= CoilCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Hover coils are 0..3.");
            }

            float x = index % 2 == 0 ? -0.19f : 0.19f;
            float z = index < 2 ? 0.3f : -0.3f;
            return new Vector3(x, -MountThickness, z);
        }

        /// <summary>Glow ring position in its coil's space: around the foot pad.</summary>
        public static Vector3 GlowCentre => new Vector3(0f, -SpringLength - PadThickness * 0.5f, 0f);

        /// <summary>Plate bolted under the belly and a bracket cup over each spring.</summary>
        public static LowPolyMeshBuilder Mount()
        {
            var b = new LowPolyMeshBuilder(300);
            b.Box(At(0f, -MountThickness * 0.5f, 0f), new Vector3(0.52f, MountThickness, 0.8f), PaletteSwatch.Charcoal,
                0.008f);
            for (int i = 0; i < CoilCount; i++)
            {
                Vector3 top = CoilTop(i);
                b.Frustum(At(top - Vector3.up * 0.012f), 0.05f, 0.072f, 0.024f, 10, PaletteSwatch.Metal);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.19f, -MountThickness - 0.006f, 0f), new Vector3(0.05f, 0.012f, 0.5f),
                    PaletteSwatch.WarmAccent);
            }

            return b;
        }

        /// <summary>A spring hanging from its pivot (origin) down to a foot pad.</summary>
        public static LowPolyMeshBuilder Coil()
        {
            var b = new LowPolyMeshBuilder(360);
            int segments = Mathf.RoundToInt(Turns * SegmentsPerTurn);
            Vector3 previous = HelixPoint(0f);
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = HelixPoint((float)i / segments);
                Vector3 overlap = (next - previous).normalized * (WireRadius * 0.6f);
                RecipeKit.Rod(b, previous - overlap, next + overlap, WireRadius, 5, PaletteSwatch.Metal);
                previous = next;
            }

            b.Prism(At(0f, -SpringLength - PadThickness * 0.5f, 0f), PadRadius, PadThickness, 10, PaletteSwatch.Metal);
            b.Prism(At(0f, -SpringLength - PadThickness - 0.003f, 0f), PadRadius * 0.7f, 0.006f, 10,
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>Dark glass ring around the foot pad that glows cyan while charging (glow-off by default).</summary>
        public static LowPolyMeshBuilder GlowRing()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Torus(Matrix4x4.identity, PadRadius + 0.006f, 0.009f, 12, 3, PaletteSwatch.EyeGlass);
            return b;
        }

        private static Vector3 HelixPoint(float t)
        {
            float angle = t * Turns * Mathf.PI * 2f;
            float y = -Mathf.Lerp(0.016f, SpringLength - 0.004f, t);
            return new Vector3(SpringRadius * Mathf.Cos(angle), y, SpringRadius * Mathf.Sin(angle));
        }
    }
}
