using System.Collections.Generic;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The base's ground power cables (M3-16, "power comes back through the tower"): from the radio tower's foot to
    /// home (the ladder's leg), Kenji's Rover Bay and the survey chart (the leg under the big porthole, M3-19 D8),
    /// half sunk in the dust and wandering a little, each ending in a junction box. Along each sits a row of small glass beads, dark until power reaches them, so the cable can
    /// light along its length from the tower outwards. Base (lander) space, metres, on the ground.
    /// </summary>
    internal static class PowerCableMeshes
    {
        /// <summary>
        /// The cables, by name of the place they feed, with the waypoints of their routes from the tower.
        /// </summary>
        public static readonly string[] Targets = { "Home", "Bay", "Chart" };

        public const float SegmentLength = 1.2f;

        public const float SheathRadius = 0.032f;
        public const float SheathHeight = 0.012f;
        private const float BeadRadius = 0.016f;
        private const float BeadLength = 0.1f;
        public const float BeadHeight = 0.038f;
        private const float WanderWidth = 0.12f;
        private const float StepLength = 1.5f;

        private static readonly Vector3[][] Routes =
        {
            new[]
            {
                new Vector3(-10.65f, 0f, -0.9f), new Vector3(-8.2f, 0f, 0.9f), new Vector3(-6.1f, 0f, 3.2f),
                new Vector3(-4.75f, 0f, 3.95f),
            },
            new[]
            {
                new Vector3(-11.5f, 0f, -2.35f), new Vector3(-9.4f, 0f, -6.4f), new Vector3(-3f, 0f, -7.9f),
                new Vector3(4f, 0f, -7.7f), new Vector3(8.6f, 0f, -6.2f), new Vector3(10.15f, 0f, -4.75f),
            },
            new[]
            {
                new Vector3(-10.75f, 0f, -2.2f), new Vector3(-8.4f, 0f, -3.6f), new Vector3(-5.75f, 0f, -4.55f),
                new Vector3(-4.9f, 0f, -4.75f),
            },
        };

        /// <summary>
        /// The ground path of cable <paramref name="index"/>: its waypoints, wandering a little between.
        /// </summary>
        public static List<Vector3> Path(int index)
        {
            return Wander(Routes[index], index);
        }

        /// <summary>
        /// A path through <paramref name="route"/>'s waypoints in steps of about 1.5 m, each step nudged sideways a
        /// little (its own pattern per <paramref name="seed"/>) so the cable never runs ruler-straight.
        /// </summary>
        public static List<Vector3> Wander(IReadOnlyList<Vector3> route, int seed)
        {
            var path = new List<Vector3> { route[0] };
            for (int i = 1; i < route.Count; i++)
            {
                Vector3 from = route[i - 1];
                Vector3 to = route[i];
                int steps = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(from, to) / StepLength));
                Vector3 side = Vector3.Cross(Vector3.up, (to - from).normalized);
                for (int k = 1; k <= steps; k++)
                {
                    float t = (float)k / steps;
                    float wander = k == steps ? 0f : Mathf.Sin((seed * 7 + i * 3 + k) * 1.7f) * WanderWidth;
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
            return Beads(Path(index), BeadHeight);
        }

        /// <summary>
        /// Glass beads one every <see cref="SegmentLength"/> along <paramref name="path"/> from its start, each a
        /// small pill <paramref name="lift"/> above the path, in order.
        /// </summary>
        public static List<LowPolyMeshBuilder> Beads(IReadOnlyList<Vector3> path, float lift)
        {
            var beads = new List<LowPolyMeshBuilder>();
            float travelled = 0f;
            float next = SegmentLength * 0.5f;
            for (int i = 1; i < path.Count; i++)
            {
                float length = Vector3.Distance(path[i - 1], path[i]);
                Vector3 along = (path[i] - path[i - 1]) / length;
                while (next <= travelled + length)
                {
                    Vector3 at = path[i - 1] + along * (next - travelled) + Vector3.up * lift;
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
