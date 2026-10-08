using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// site.garage, Kenji's rover garage, half buried: an arched hut open to the way in, a rover ramp and a docking
    /// pad still stencilled 07 inside, its right side drowned in a dust drift and its back right ribs snapped where the
    /// roof caved in, Kenji's workbench against the left wall with a pegboard of empty tool outlines. Salvage: the
    /// caved-in roof section (the drag piece), a spare rocker arm, the charging post at the door, a spare headlamp,
    /// a cable coil hanging from a rib and the cracked skylight. The heart is under the workbench, where Kenji hid
    /// the spare wheel "for next time".
    /// </summary>
    internal static class GarageSite
    {
        private const float Radius = 3f;
        private const float Centre = -0.3f;
        private const float Floor = 0.12f;
        private const float Rib = 0.15f;
        private const int Segments = 8;

        private static readonly float[] Ribs = { -0.8f, 0.7f, 2.2f, 3.7f, 5.2f, 6.7f };

        private static readonly Vector3 Bench = new Vector3(-1.7f, 0.92f, 5.5f);

        public static SiteRecipe Create()
        {
            var site = new SiteRecipe();
            LowPolyMeshBuilder b = site.Skeleton;
            Floorplan(b);
            Arch(b);
            Panels(b);
            BackWall(b);
            Workbench(b);
            SiteKit.Cable(b, SiteKit.Sag(OnArch(122f, Ribs[0], -0.12f), OnArch(122f, Ribs[3], -0.12f), 0.45f, 6),
                0.03f, PaletteSwatch.Charcoal);
            SiteKit.Cable(b, new[]
            {
                OnArch(122f, Ribs[3], -0.12f), OnArch(122f, Ribs[3] + 0.6f, -0.5f),
                new Vector3(-1.2f, Floor + 0.6f, Ribs[3] + 0.8f),
            }, 0.03f, PaletteSwatch.Charcoal);
            SiteKit.Drift(b, new Vector3(3f, 0f, 2.2f), 6.4f, 3.4f, 1.25f, 4f, 61);
            SiteKit.Drift(b, new Vector3(3.3f, 0f, 5.4f), 4.2f, 2.6f, 0.85f, -10f, 65);
            SiteKit.Drift(b, new Vector3(-3.2f, 0f, 2.6f), 6.5f, 1.5f, 0.75f, 0f, 62);
            SiteKit.Drift(b, new Vector3(1.3f, 0f, 5.7f), 2.6f, 2f, 0.7f, 30f, 63);
            SiteKit.Drift(b, new Vector3(-2.7f, 0f, -0.9f), 1.5f, 1f, 0.4f, 60f, 64);

            FallenRoof(site);
            RockerArm(site);
            ChargingPost(site);
            Headlamp(site);
            HangingCoil(site);
            Skylight(site);
            site.Heart = new Vector3(Bench.x, Floor + 0.02f, Bench.z);
            return site;
        }

        private static Vector3 OnArch(float degrees, float z, float lift)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * (Radius + lift), Centre + Mathf.Sin(angle) * (Radius + lift), z);
        }

        private static Matrix4x4 Bay(int bay)
        {
            return At(0f, Centre, (Ribs[bay] + Ribs[bay + 1]) * 0.5f);
        }

        /// <summary>The slab, the ramp in for wheels and the docking pad, its 07 stencil faded.</summary>
        private static void Floorplan(LowPolyMeshBuilder b)
        {
            MeshRange slab = b.Box(At(0f, 0.04f, 2.95f), new Vector3(5.6f, 0.16f, 7.7f), PaletteSwatch.Metal, 0.03f);
            SiteKit.DustCap(b, slab);
            MeshRange ramp = b.Wedge(At(new Vector3(0f, 0.06f, -1.4f), new Vector3(0f, 180f, 0f)),
                new Vector3(3f, 0.12f, 1.2f), PaletteSwatch.Metal);
            SiteKit.DustCap(b, ramp);
            var pad = new Vector3(0f, Floor + 0.025f, 1.5f);
            b.Prism(At(pad), 1.1f, 0.05f, 12, PaletteSwatch.Metal);
            b.Torus(At(pad + Vector3.up * 0.03f), 1f, 0.04f, 12, 3, PaletteSwatch.Charcoal);
            Glyphs.Write(b, SiteKit.Face(pad + Vector3.up * 0.026f, Vector3.up, Vector3.forward), "07", 0.7f,
                PaletteSwatch.FadedPaint, PaletteSwatch.Metal);
        }

        /// <summary>
        /// The ribs: the front four whole, the back two snapped on the right where the roof caved in.
        /// </summary>
        private static void Arch(LowPolyMeshBuilder b)
        {
            for (int r = 0; r < Ribs.Length; r++)
            {
                bool snapped = r >= 4;
                int first = snapped ? 3 : 0;
                for (int s = first; s < Segments; s++)
                {
                    Vector3 from = OnArch(s * 180f / Segments, Ribs[r], 0f);
                    Vector3 to = OnArch((s + 1) * 180f / Segments, Ribs[r], 0f);
                    SiteKit.Bar(b, from, to, Rib, r == 3 && s == 2 ? PaletteSwatch.Rust : PaletteSwatch.Metal);
                }

                if (snapped)
                {
                    Vector3 broken = OnArch(3 * 180f / Segments, Ribs[r], 0f);
                    SiteKit.Bar(b, broken, broken + new Vector3(0.35f + r * 0.1f, -1.1f, 0.25f), Rib,
                        PaletteSwatch.Rust);
                }
            }

            SiteKit.Bar(b, OnArch(90f, Ribs[0], 0.02f), OnArch(90f, Ribs[4], 0.02f), 0.12f, PaletteSwatch.Metal);
            SiteKit.Bar(b, OnArch(135f, Ribs[0], 0.02f), OnArch(135f, Ribs[5], 0.02f), 0.1f, PaletteSwatch.Metal);
        }

        /// <summary>
        /// What is left of the skin: the left side down most of the hut, a canopy over the entrance, nothing over
        /// the caved-in back right.
        /// </summary>
        private static void Panels(LowPolyMeshBuilder b)
        {
            const float lift = 0.11f;
            for (int bay = 0; bay < Ribs.Length - 1; bay++)
            {
                float length = Ribs[bay + 1] - Ribs[bay] - 0.04f;
                if (bay == 2)
                {
                    SiteKit.ArcSheet(b, Bay(bay), Radius + lift, 90f, 112.5f, 1, length, PaletteSwatch.FadedPaint);
                    SiteKit.ArcSheet(b, Bay(bay), Radius + lift, 157.5f, 180f, 1, length, PaletteSwatch.FadedPaint);
                }
                else
                {
                    float top = bay == 4 ? 135f : 90f;
                    MeshRange skin = SiteKit.ArcSheet(b, Bay(bay), Radius + lift, top, 180f, bay == 4 ? 2 : 4, length,
                        PaletteSwatch.FadedPaint);
                    SiteKit.DustCap(b, skin);
                }
            }

            MeshRange canopy = SiteKit.ArcSheet(b, Bay(0), Radius + lift, 22.5f, 90f, 3, Ribs[1] - Ribs[0] - 0.04f,
                PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, canopy);
            Matrix4x4 left = SiteKit.Face(OnArch(140f, 2.95f, lift + 0.03f),
                OnArch(140f, 0f, 0f) - OnArch(140f, 0f, -1f),
                Vector3.forward);
            SiteKit.RustStreak(b, left, -0.5f, 0.3f, 0.9f);
            SiteKit.RustPatch(b, SiteKit.Face(OnArch(160f, 1.4f, lift + 0.03f),
                OnArch(160f, 0f, 0f) - OnArch(160f, 0f, -1f), Vector3.forward), 0f, 0f, 0.8f);
        }

        /// <summary>
        /// The back wall: the left strips and a crew door still standing, the right strips fallen in.
        /// </summary>
        private static void BackWall(LowPolyMeshBuilder b)
        {
            float z = Ribs[Ribs.Length - 1] + 0.1f;
            float[] edges = { 0f, -1f, -2f };
            for (int i = 0; i < edges.Length; i++)
            {
                float outer = edges[i] - 1f + (i == 2 ? 0.2f : 0f);
                float height = Centre + Mathf.Sqrt(Radius * Radius - outer * outer) - Floor;
                Matrix4x4 strip = SiteKit.Face(new Vector3(edges[i] - 0.5f, Floor + height * 0.5f, z), Vector3.forward,
                    Vector3.up);
                SiteKit.Sheet(b, strip, 0.98f, height, PaletteSwatch.FadedPaint);
            }

            Matrix4x4 door = SiteKit.Face(new Vector3(-0.95f, Floor + 0.95f, z - 0.06f), Vector3.back, Vector3.up);
            b.Box(door, new Vector3(0.84f, 1.9f, 0.04f), PaletteSwatch.Charcoal);
            b.Box(door * At(0f, 0f, 0.02f), new Vector3(0.74f, 1.8f, 0.04f), PaletteSwatch.FadedPaint);
            SiteKit.RustPatch(b, door * At(0f, 0f, 0.04f), 0.15f, -0.5f, 0.45f);

            Matrix4x4 fallen = SiteKit.Face(new Vector3(1.4f, 0.55f, z - 0.6f), new Vector3(0.2f, 1f, -0.9f),
                new Vector3(0f, 0.5f, 1f));
            SiteKit.Sheet(b, fallen, 1.9f, 1.2f, PaletteSwatch.FadedPaint);
        }

        /// <summary>
        /// Kenji's workbench: wood top, a vice and a pegboard of empty tool outlines; open underneath.
        /// </summary>
        private static void Workbench(LowPolyMeshBuilder b)
        {
            b.Box(At(Bench), new Vector3(0.7f, 0.06f, 1.7f), PaletteSwatch.Wood, 0.01f);
            foreach (float x in new[] { -0.3f, 0.3f })
            {
                foreach (float z in new[] { -0.78f, 0.78f })
                {
                    SiteKit.Bar(b, new Vector3(Bench.x + x, Floor, Bench.z + z),
                        new Vector3(Bench.x + x, Bench.y - 0.03f, Bench.z + z), 0.05f, PaletteSwatch.Metal);
                }
            }

            b.Box(At(Bench.x + 0.3f, Bench.y + 0.1f, Bench.z - 0.65f), new Vector3(0.18f, 0.14f, 0.22f),
                PaletteSwatch.Metal, 0.02f);

            Matrix4x4 board = SiteKit.Face(new Vector3(-2.05f, 1.38f, Bench.z), Vector3.right, Vector3.up);
            b.Box(board, new Vector3(1.5f, 0.72f, 0.03f), PaletteSwatch.FadedPaint);
            Matrix4x4 ink = board * At(0f, 0f, 0.018f);
            b.Box(ink * At(new Vector3(-0.45f, 0f, 0f), new Vector3(0f, 0f, 20f)), new Vector3(0.06f, 0.5f, 0.006f),
                PaletteSwatch.Charcoal);
            b.Box(ink * At(-0.05f, 0.05f, 0f), new Vector3(0.05f, 0.42f, 0.006f), PaletteSwatch.Charcoal);
            b.Box(ink * At(-0.05f, 0.22f, 0f), new Vector3(0.2f, 0.07f, 0.006f), PaletteSwatch.Charcoal);
            b.Box(ink * At(0.4f, 0f, 0f), new Vector3(0.3f, 0.3f, 0.006f), PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// The caved-in roof section, slid down the drift into the back right of the hut: tether it clear before
        /// cutting.
        /// </summary>
        private static void FallenRoof(SiteRecipe site)
        {
            var target = new Vector3(1.6f, 1.25f, 4.6f);
            Vector3 upper = target + new Vector3(1.3f, 0.35f, 0f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, target, upper,
                new Vector3(-0.4f, 1f, -0.5f), true);
            LowPolyMeshBuilder b = piece.Geometry;
            Vector3 mid = OnArch(120f, 0f, 0f) - new Vector3(0f, Centre, 0f);
            MeshRange skin = SiteKit.ArcSheet(b, At(target - mid), Radius, 90f, 150f, 3, 1.4f,
                PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, skin);
            SiteKit.RustPatch(b, SiteKit.Face(target + new Vector3(-0.25f, 0.42f, 0.1f), new Vector3(-0.45f, 1f, 0f),
                Vector3.forward), 0f, 0f, 0.7f);
            SiteKit.Bar(b, target + new Vector3(-1.05f, -1.0f, -0.66f), upper + new Vector3(0.1f, 0f, -0.66f), 0.1f,
                PaletteSwatch.Rust);
        }

        /// <summary>
        /// A spare rocker arm leaning on the left wall by the door: two faded arms, two hubs, a pivot boss.
        /// </summary>
        private static void RockerArm(SiteRecipe site)
        {
            var boss = new Vector3(-1.95f, 0.85f, 1.2f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, boss, boss + new Vector3(0.12f, 0f, -0.1f),
                new Vector3(0.6f, 0.2f, -0.8f));
            LowPolyMeshBuilder b = piece.Geometry;
            var low = new Vector3(-1.55f, Floor + 0.18f, 0.55f);
            var high = new Vector3(-2.2f, 1.45f, 1.75f);
            SiteKit.Bar(b, boss, low, 0.14f, PaletteSwatch.FadedPaint);
            SiteKit.Bar(b, boss, high, 0.14f, PaletteSwatch.FadedPaint);
            b.Prism(At(boss, AlongX), 0.15f, 0.26f, 8, PaletteSwatch.Charcoal);
            b.Prism(At(low, AlongX), 0.17f, 0.2f, 8, PaletteSwatch.Metal);
            b.Prism(At(high, AlongX), 0.17f, 0.2f, 8, PaletteSwatch.Metal);
        }

        /// <summary>The charging post at the door: a sage head, a thick copper lead coiled on its hook.</summary>
        private static void ChargingPost(SiteRecipe site)
        {
            var foot = new Vector3(2.3f, 0f, -1.4f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, foot + Vector3.up * 0.12f,
                foot + new Vector3(0f, 0.18f, -0.1f), new Vector3(-0.3f, 0.2f, -1f));
            LowPolyMeshBuilder b = piece.Geometry;
            b.Box(At(foot + Vector3.up * 0.05f), new Vector3(0.4f, 0.1f, 0.4f), PaletteSwatch.Metal);
            SiteKit.Bar(b, foot, foot + Vector3.up * 1.15f, 0.14f, PaletteSwatch.Metal);
            Matrix4x4 head = At(foot + new Vector3(0f, 1.3f, 0f), new Vector3(0f, 0f, 4f));
            b.Box(head, new Vector3(0.42f, 0.5f, 0.32f), PaletteSwatch.Sage, 0.03f);
            b.Box(head * At(0f, 0.05f, -0.165f), new Vector3(0.26f, 0.18f, 0.01f), PaletteSwatch.Charcoal);
            SiteKit.RustStreak(b, head * At(0f, 0f, -0.17f), 0.12f, -0.1f, 0.18f);
            b.Torus(At(foot + new Vector3(-0.17f, 0.75f, 0f), new Vector3(0f, 0f, 90f)), 0.2f, 0.045f, 10, 4,
                PaletteSwatch.WarmAccent);
            SiteKit.Cable(b, new[]
            {
                foot + new Vector3(-0.05f, 1.1f, -0.1f), foot + new Vector3(-0.2f, 0.75f, -0.25f),
                foot + new Vector3(-0.35f, 0.04f, -0.5f), foot + new Vector3(-0.6f, 0.04f, -0.6f),
            }, 0.045f, PaletteSwatch.WarmAccent);
            b.Box(At(foot + new Vector3(-0.7f, 0.07f, -0.62f), new Vector3(0f, 25f, 0f)),
                new Vector3(0.16f, 0.12f, 0.22f),
                PaletteSwatch.Charcoal);
        }

        /// <summary>A spare 07 headlamp on the floor by the bench: faded hood, big violet lens.</summary>
        private static void Headlamp(SiteRecipe site)
        {
            var centre = new Vector3(-0.75f, Floor + 0.24f, 4.3f);
            var facing = new Vector3(0.3f, 0.35f, -0.9f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, centre, centre + facing.normalized * 0.22f,
                facing);
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 lamp = SiteKit.Face(centre, facing, Vector3.up);
            b.Box(lamp * At(0f, 0f, -0.1f), new Vector3(0.5f, 0.42f, 0.3f), PaletteSwatch.FadedPaint, 0.05f);
            b.Prism(lamp * At(new Vector3(0f, 0f, 0.07f), AlongZ), 0.17f, 0.05f, 10, PaletteSwatch.SkyHorizon);
            b.Torus(lamp * At(new Vector3(0f, 0f, 0.08f), AlongZ), 0.19f, 0.03f, 10, 3, PaletteSwatch.Metal);
            b.Box(lamp * At(0f, 0.24f, 0.02f), new Vector3(0.48f, 0.04f, 0.2f), PaletteSwatch.FadedPaint);
        }

        /// <summary>A copper cable coil hanging from a hook on the third rib.</summary>
        private static void HangingCoil(SiteRecipe site)
        {
            Vector3 hook = OnArch(64f, Ribs[2], -0.1f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, hook + Vector3.down * 0.12f,
                hook + Vector3.down * 0.05f, new Vector3(-0.4f, -0.2f, -1f));
            LowPolyMeshBuilder b = piece.Geometry;
            SiteKit.Bar(b, hook, hook + Vector3.down * 0.2f, 0.04f, PaletteSwatch.Metal);
            Vector3 centre = hook + new Vector3(0f, -0.52f, -0.02f);
            b.Torus(At(centre, AlongZ), 0.32f, 0.06f, 12, 4, PaletteSwatch.WarmAccent);
            b.Torus(At(centre + new Vector3(0.03f, -0.02f, 0.08f), new Vector3(90f, 0f, 8f)), 0.3f, 0.05f, 12, 4,
                PaletteSwatch.WarmAccent);
            SiteKit.Cable(b, new[] { centre + new Vector3(0.2f, -0.25f, 0f), centre + new Vector3(0.3f, -0.8f, -0.1f) },
                0.04f, PaletteSwatch.WarmAccent);
        }

        /// <summary>
        /// The skylight in the left side of the third bay: violet glass in a metal frame, one pane cracked.
        /// </summary>
        private static void Skylight(SiteRecipe site)
        {
            const float lift = 0.12f;
            Vector3 hinge = OnArch(112.5f, (Ribs[2] + Ribs[3]) * 0.5f, lift);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics,
                OnArch(116f, (Ribs[2] + Ribs[3]) * 0.5f, lift),
                hinge + new Vector3(0f, 0.08f, -0.5f), OnArch(112.5f, 0f, 0f) - OnArch(112.5f, 0f, -1f));
            LowPolyMeshBuilder b = piece.Geometry;
            float length = Ribs[3] - Ribs[2] - 0.04f;
            SiteKit.ArcSheet(b, Bay(2), Radius + lift, 112.5f, 157.5f, 2, length, PaletteSwatch.Metal);
            SiteKit.ArcSheet(b, Bay(2) * At(0f, 0f, -length * 0.25f), Radius + lift + 0.03f, 116f, 154f, 2,
                length * 0.42f, PaletteSwatch.SkyHorizon);
            SiteKit.ArcSheet(b, Bay(2) * At(0f, 0f, length * 0.25f), Radius + lift + 0.03f, 116f, 133f, 1,
                length * 0.42f, PaletteSwatch.SkyHorizon);
            SiteKit.ArcSheet(b, Bay(2) * At(0f, 0f, length * 0.25f), Radius + lift + 0.03f, 136f, 154f, 1,
                length * 0.42f, PaletteSwatch.Charcoal);
        }
    }
}
