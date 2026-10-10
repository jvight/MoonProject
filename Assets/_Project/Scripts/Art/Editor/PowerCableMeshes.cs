using System.Collections.Generic;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The base's ground power cables (M3-16, "power comes back through the tower"): from the radio tower's foot to
    /// home, Kenji's Rover Bay and the crew's lift, half sunk in the dust and wandering a little, each ending in a
    /// junction box. Along each sits a row of small glass beads, dark until power reaches them, so the cable can
    /// light along its length from the tower outwards. Base (lander) space, metres, on the ground.
    /// </summary>
    internal static class PowerCableMeshes
    {
        /// <summary>
        /// The cables, by name of the place they feed, with the waypoints of their routes from the tower.
        /// </summary>
        public static readonly string[] Targets = { "Home", "Bay", "Lift" };

        public const float SegmentLength = 1.2f;

        private const float SheathRadius = 0.032f;
        private const float SheathHeight = 0.012f;
        private const float BeadRadius = 0.016f;
        private const float BeadLength = 0.1f;
        private const float BeadHeight = 0.038f;
        private const float Wander = 0.12f;
        private const float StepLength = 1.5f;

        private static readonly Vector3[][] Routes =
        {
            new[] { new Vector3(-5.15f, 0f, -0.6f), new Vector3(-4.1f, 0f, -0.7f), new Vector3(-3.05f, 0f, -0.9f) },
            new[]
            {
                new Vector3(-6f, 0f, -1.85f), new Vector3(-5.3f, 0f, -4f), new Vector3(0f, 0f, -4.6f),
                new Vector3(6f, 0f, -4.4f), new Vector3(9.4f, 0f, -2.8f), new Vector3(10.1f, 0f, -2.4f),
            },
            new[]
            {
                new Vector3(-6f, 0f, -0.15f), new Vector3(-5.6f, 0f, 2.6f), new Vector3(-3.4f, 0f, 5.6f),
                new Vector3(-0.8f, 0f, 7f), new Vector3(3.6f, 0f, 7f), new Vector3(3.75f, 0f, 3.95f),
            },
        };

        /// <summary>
        /// The ground path of cable <paramref name="index"/>: its waypoints, wandering a little between.
        /// </summary>
        public static List<Vector3> Path(int index)
        {
            Vector3[] route = Routes[index];
            var path = new List<Vector3> { route[0] };
            for (int i = 1; i < route.Length; i++)
            {
                Vector3 from = route[i - 1];
                Vector3 to = route[i];
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(from, to) / StepLength));
                Vector3 side = Vector3.Cross(Vector3.up, (to - from).normalized);
                for (int k = 1; k <= steps; k++)
                {
                    float t = (float)k / steps;
                    float wander = k == steps ? 0f : Mathf.Sin((index * 7 + i * 3 + k) * 1.7f) * Wander;
                    path.Add(Vector3.Lerp(from, to, t) + side * wander);
                }
            }

            return path;
        }

        /// <summary>Cable <paramref name="index"/>'s sheath, half sunk along its path, and its junction box.</summary>
        public static LowPolyMeshBuilder Sheath(int index)
        {
            var b = new LowPolyMeshBuilder(800);
            List<Vector3> path = Path(index);
            for (int i = 1; i < path.Count; i++)
            {
                RecipeKit.Rod(b, path[i - 1] + Vector3.up * SheathHeight, path[i] + Vector3.up * SheathHeight,
                    SheathRadius, 5, PaletteSwatch.DustShadow);
            }

            Vector3 end = path[path.Count - 1];
            Vector3 along = (end - path[path.Count - 2]).normalized;
            b.Box(At(end + along * 0.12f + Vector3.up * 0.14f, new Vector3(0f, Mathf.Atan2(along.x, along.z)
                * Mathf.Rad2Deg, 0f)), new Vector3(0.3f, 0.28f, 0.2f), PaletteSwatch.Metal);
            b.Box(At(end + along * 0.12f + Vector3.up * 0.02f), new Vector3(0.36f, 0.04f, 0.26f),
                PaletteSwatch.Rust);
            return b;
        }

        /// <summary>
        /// The glass beads along cable <paramref name="index"/>, one every <see cref="SegmentLength"/> from the tower
        /// out, each a small pill on the half-sunk sheath (base space): lit one after another they read as power
        /// running home, never as a neon line.
        /// </summary>
        public static List<LowPolyMeshBuilder> Glow(int index)
        {
            List<Vector3> path = Path(index);
            var beads = new List<LowPolyMeshBuilder>();
            float travelled = 0f;
            float next = SegmentLength * 0.5f;
            for (int i = 1; i < path.Count; i++)
            {
                float length = Vector3.Distance(path[i - 1], path[i]);
                Vector3 along = (path[i] - path[i - 1]) / length;
                while (next <= travelled + length)
                {
                    Vector3 at = path[i - 1] + along * (next - travelled) + Vector3.up * BeadHeight;
                    var bead = new LowPolyMeshBuilder(40);
                    RecipeKit.Rod(bead, at - along * (BeadLength * 0.5f), at + along * (BeadLength * 0.5f), BeadRadius,
                        5, PaletteSwatch.LampGlass);
                    beads.Add(bead);
                    next += SegmentLength;
                }

                travelled += length;
            }

            return beads;
        }
    }
}
