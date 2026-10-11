using System.Collections.Generic;
using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The hand-off's way from the lab to Kenji's plotter (docs/features/M3-19 §5): a conduit from the reader's slot up
    /// over the lab's ceiling and out through the wall behind the keepsake shelf, down the back-right leg and along
    /// the dust to the Rover Bay's back wall, with a row of glass beads along it that a lamp pulse lights one after
    /// another (World, from <c>DecodeHandOff</c>). Lander space, metres.
    /// </summary>
    internal static class HandOffMeshes
    {
        // The leg the conduit runs down: the back right, nearest the bay.
        private const int Leg = 1;
        private const float ConduitRadius = 0.028f;
        private const float BeadLift = 0.03f;
        private const float WallGap = 0.06f;
        private const int GroundSeed = 9;

        private static readonly float ExitYaw = LabMeshes.StationYaws[3];

        // The ground run from the leg's foot to the bay's back wall (WorkshopAnchor's bay, its back at BackZ).
        private static readonly Vector3[] GroundRoute =
        {
            new Vector3(4.8f, 0f, -4.85f), new Vector3(8f, 0f, -5.5f), new Vector3(11.1f, 0f, -4.95f),
            new Vector3(12.3f, 0f, -4.78f),
        };

        /// <summary>
        /// The pulse's waypoints in order: the reader's slot, the top of the reader, along the ceiling to the far wall,
        /// out through it, down the cabin wall and the stage's side, down the leg to its foot, along the ground and up
        /// to the junction box on the bay's back wall (HandOffPath/Point_n).
        /// </summary>
        public static IReadOnlyList<Vector3> Points()
        {
            var points = new List<Vector3>();
            Vector3 lab = Vector3.up * LanderMeshes.FloorY;
            Matrix4x4 reader = Matrix4x4.TRS(lab + LabMeshes.StationPosition(1), LabMeshes.StationRotation(1),
                Vector3.one);
            points.Add(reader.MultiplyPoint3x4(LabStationMeshes.ReaderSlot));
            points.Add(reader.MultiplyPoint3x4(LabStationMeshes.ReaderHood));
            float ceiling = LanderMeshes.CeilingY - 0.08f;
            float nearWall = LabMeshes.LiningApothem - 0.1f;
            points.Add(Out(LabMeshes.StationYaws[1], nearWall, ceiling));
            points.Add(new Vector3(0f, ceiling, 0f));
            points.Add(Out(ExitYaw, nearWall, ceiling));
            points.Add(Out(ExitYaw, nearWall, LanderMeshes.FloorY + 2.2f));
            float outside = LanderMeshes.CabinApothem + WallGap;
            points.Add(Out(ExitYaw, outside, LanderMeshes.FloorY + 2.2f));
            points.Add(Out(ExitYaw, outside, LanderMeshes.FloorY + WallGap));
            float edge = LanderMeshes.StageApothem + WallGap;
            points.Add(Out(ExitYaw, edge, LanderMeshes.FloorY + WallGap));
            points.Add(Out(ExitYaw, edge, LanderMeshes.StageBottom + 0.2f));
            LanderMeshes.LegPoints(Leg, out Vector3 hip, out Vector3 foot, out Vector3 knee);
            Vector3 up = Vector3.up * 0.16f;
            points.Add(Vector3.Lerp(hip, foot, 0.12f) + up);
            points.Add(knee + up);
            points.Add(foot + up);
            foreach (Vector3 step in GroundRoute)
            {
                points.Add(step);
            }

            Vector3 last = GroundRoute[GroundRoute.Length - 1];
            points.Add(new Vector3(last.x, 0.9f, last.z));
            return points;
        }

        /// <summary>
        /// The conduit: a dark tube along the points inside and down the lander, then half sunk and wandering a little
        /// over the dust like the power cables, and the junction box on the bay's back wall.
        /// </summary>
        public static LowPolyMeshBuilder Sheath()
        {
            var b = new LowPolyMeshBuilder(900);
            List<Vector3> path = Path();
            for (int i = 1; i < path.Count; i++)
            {
                bool ground = path[i - 1].y < 0.05f && path[i].y < 0.05f;
                Vector3 lift = ground ? Vector3.up * PowerCableMeshes.SheathHeight : Vector3.zero;
                RecipeKit.Rod(b, path[i - 1] + lift, path[i] + lift,
                    ground ? PowerCableMeshes.SheathRadius : ConduitRadius, 5,
                    ground ? PaletteSwatch.DustShadow : PaletteSwatch.Charcoal);
            }

            Vector3 box = path[path.Count - 1];
            b.Box(At(box + new Vector3(0f, 0f, -0.08f)), new Vector3(0.3f, 0.36f, 0.16f), PaletteSwatch.Metal, 0.02f);
            return b;
        }

        /// <summary>The pulse's glass beads along the conduit from the reader out, one per segment, in order.</summary>
        public static List<LowPolyMeshBuilder> Beads()
        {
            return PowerCableMeshes.Beads(Path(), BeadLift);
        }

        /// <summary>The conduit's full path: the points, with the ground run wandering between its waypoints.</summary>
        private static List<Vector3> Path()
        {
            IReadOnlyList<Vector3> points = Points();
            var path = new List<Vector3>();
            int groundStart = points.Count - GroundRoute.Length - 1;
            for (int i = 0; i < groundStart; i++)
            {
                path.Add(points[i]);
            }

            var ground = new List<Vector3> { new Vector3(points[groundStart - 1].x, 0f, points[groundStart - 1].z) };
            ground.AddRange(GroundRoute);
            path.AddRange(PowerCableMeshes.Wander(ground, GroundSeed));
            path.Add(points[points.Count - 1]);
            return path;
        }

        private static Vector3 Out(float yaw, float distance, float y)
        {
            return Rotation(new Vector3(0f, yaw, 0f)) * Vector3.forward * distance + Vector3.up * y;
        }
    }
}
