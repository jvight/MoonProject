using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// site.drill, a survey drill rig tipped over on a crater rim: a weathered skid with a loading ramp for wheels, the
    /// A-frame the derrick was pinned to, the lattice derrick itself lying full length across the ground with its
    /// crown block, a bent drill pipe in the old borehole and the work lamp's leaning pole. Salvage: the drill head
    /// thrown past the crown, a strapped pipe bundle on chocks, the rusted generator and the control cabinet on the
    /// skid, a cable reel and the work lamp.
    /// </summary>
    internal static class DrillSite
    {
        private const float Deck = 0.42f;
        private const int Bays = 5;

        private static readonly Vector3 DerrickFoot = new Vector3(1f, 0.45f, 1.1f);
        private static readonly Vector3 DerrickCrown = new Vector3(6.8f, 0.25f, 3.9f);
        private const float FootHalfWidth = 0.45f;
        private const float CrownHalfWidth = 0.22f;

        private static readonly Vector3 PoleTop = new Vector3(-2.65f, 2.45f, 2.7f);

        public static SiteRecipe Create()
        {
            var site = new SiteRecipe();
            LowPolyMeshBuilder b = site.Skeleton;
            Skid(b);
            Derrick(b);
            Borehole(b);
            SiteKit.Bar(b, new Vector3(-2.9f, -0.2f, 2.65f), PoleTop, 0.12f, PaletteSwatch.Metal);
            b.Box(At(-3.65f, 0.1f, -1.45f), new Vector3(0.24f, 0.2f, 0.9f), PaletteSwatch.Rust);
            b.Box(At(-2.35f, 0.1f, -1.35f), new Vector3(0.24f, 0.2f, 0.9f), PaletteSwatch.Rust);
            SiteKit.Drift(b, new Vector3(-1.4f, 0f, -0.1f), 3.8f, 0.7f, 0.24f, 92f, 51);
            SiteKit.Drift(b, new Vector3(-1.4f, 0f, 2.5f), 4f, 0.9f, 0.45f, 86f, 52);
            SiteKit.Drift(b, new Vector3(4.4f, 0f, 3.2f), 3.4f, 1f, 0.35f, 64f, 53);
            SiteKit.Drift(b, new Vector3(5.4f, 0f, 5.3f), 1.6f, 1.2f, 0.3f, 20f, 54);

            DrillHead(site);
            Pipes(site);
            Generator(site);
            Cabinet(site);
            Reel(site);
            WorkLamp(site);
            site.Heart = new Vector3(2.74f, 0.02f, 1.94f);
            return site;
        }

        /// <summary>
        /// The skid: two runners, a dusty deck streaked with rust and a ramp at the left end for wheels.
        /// </summary>
        private static void Skid(LowPolyMeshBuilder b)
        {
            foreach (float z in new[] { 0.2f, 2f })
            {
                b.Box(At(new Vector3(-1.2f, 0.12f, z), new Vector3(0f, 0f, z < 1f ? -2f : 1f)),
                    new Vector3(4.4f, 0.24f, 0.22f), PaletteSwatch.Metal, 0.03f);
            }

            int first = b.TriangleCount;
            b.Box(At(new Vector3(-1.2f, Deck - 0.06f, 1.1f), new Vector3(0f, 0f, -1.2f)),
                new Vector3(4.2f, 0.12f, 2.1f),
                PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, b.RangeFrom(first));
            MeshRange ramp = b.Wedge(At(new Vector3(-3.95f, 0.2f, 1.1f), new Vector3(0f, -90f, 0f)),
                new Vector3(1.6f, 0.4f, 1.3f), PaletteSwatch.Metal);
            SiteKit.DustCap(b, ramp);
            Matrix4x4 side = SiteKit.Face(new Vector3(-1.2f, Deck - 0.06f, 0.04f), Vector3.back, Vector3.up);
            SiteKit.RustStreak(b, side, -1.4f, 0.04f, 0.3f);
            SiteKit.RustStreak(b, side, 0.6f, 0.04f, 0.24f);

            foreach (float z in new[] { 0.35f, 1.85f })
            {
                SiteKit.Bar(b, new Vector3(0.55f, Deck, z), new Vector3(0.85f, 1.75f, 1.1f), 0.12f,
                    PaletteSwatch.Metal);
            }

            SiteKit.Bar(b, new Vector3(0.85f, 1.75f, 0.9f), new Vector3(0.85f, 1.75f, 1.3f), 0.16f, PaletteSwatch.Rust);
            SiteKit.Bar(b, new Vector3(0.85f, 1.75f, 1.1f), new Vector3(1.2f, 0.95f, 1.1f), 0.08f, PaletteSwatch.Rust);
        }

        /// <summary>
        /// The lattice derrick lying where it fell, tapering to the crown block; the bottom face has no braces.
        /// </summary>
        private static void Derrick(LowPolyMeshBuilder b)
        {
            Vector3 axis = (DerrickCrown - DerrickFoot).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, axis).normalized;
            Vector3 up = Vector3.Cross(axis, side);
            var corners = new Vector3[Bays + 1, 4];
            for (int i = 0; i <= Bays; i++)
            {
                float t = (float)i / Bays;
                Vector3 centre = Vector3.Lerp(DerrickFoot, DerrickCrown, t);
                float half = Mathf.Lerp(FootHalfWidth, CrownHalfWidth, t);
                corners[i, 0] = centre - side * half - up * half;
                corners[i, 1] = centre + side * half - up * half;
                corners[i, 2] = centre + side * half + up * half;
                corners[i, 3] = centre - side * half + up * half;
            }

            for (int c = 0; c < 4; c++)
            {
                SiteKit.Bar(b, corners[0, c], corners[Bays, c], 0.13f, PaletteSwatch.Metal);
            }

            for (int i = 0; i <= Bays; i++)
            {
                for (int c = 0; c < 4; c++)
                {
                    SiteKit.Bar(b, corners[i, c], corners[i, (c + 1) % 4], 0.08f,
                        i % 3 == 1 ? PaletteSwatch.Rust : PaletteSwatch.Metal);
                }
            }

            for (int i = 0; i < Bays; i++)
            {
                for (int c = 1; c < 4; c++)
                {
                    int next = (c + 1) % 4;
                    bool flip = (i + c) % 2 == 0;
                    SiteKit.Bar(b, corners[i, flip ? c : next], corners[i + 1, flip ? next : c], 0.06f,
                        PaletteSwatch.Metal);
                }
            }

            Vector3 crown = DerrickCrown + axis * 0.35f + up * 0.05f;
            b.Box(At(crown, Rotation(new Vector3(0f, Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg, 6f))),
                new Vector3(0.8f, 0.6f, 0.6f), PaletteSwatch.FadedPaint, 0.05f);
            b.Prism(At(crown + up * 0.12f, Rotation(new Vector3(0f, Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg, 90f))),
                0.26f, 0.7f, 8, PaletteSwatch.Metal);
            SiteKit.Cable(b, SiteKit.Sag(crown + up * 0.3f, new Vector3(3.6f, 0.03f, 1.6f), 0.15f, 3), 0.03f,
                PaletteSwatch.Charcoal);
        }

        /// <summary>The old borehole: a casing collar in the dust and the bent stub of drill pipe.</summary>
        private static void Borehole(LowPolyMeshBuilder b)
        {
            var hole = new Vector3(1.6f, 0f, 0.15f);
            b.Prism(At(hole + Vector3.up * 0.05f), 0.28f, 0.14f, 8, PaletteSwatch.Rust);
            b.Prism(At(hole + Vector3.up * 0.12f), 0.16f, 0.04f, 8, PaletteSwatch.Charcoal);
            Vector3 kink = hole + new Vector3(0.1f, 0.6f, -0.05f);
            RecipeKit.Rod(b, hole, kink, 0.09f, 6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, kink, kink + new Vector3(0.45f, 0.2f, 0.15f), 0.09f, 6, PaletteSwatch.Rust);
        }

        /// <summary>The drill head, thrown past the crown when the derrick fell.</summary>
        private static void DrillHead(SiteRecipe site)
        {
            var centre = new Vector3(5.5f, 0.42f, 5.1f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre,
                centre + new Vector3(-0.55f, 0.15f, -0.2f),
                new Vector3(-0.8f, 0.3f, -0.5f));
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 axis = At(centre, new Vector3(0f, 55f, -84f));
            int first = b.TriangleCount;
            b.Prism(axis, 0.42f, 1f, 8, PaletteSwatch.FadedPaint);
            b.Prism(axis * At(0f, 0.25f, 0f), 0.46f, 0.12f, 8, PaletteSwatch.Metal);
            b.Prism(axis * At(0f, -0.25f, 0f), 0.46f, 0.12f, 8, PaletteSwatch.Metal);
            b.Frustum(axis * At(0f, -0.7f, 0f), 0.12f, 0.4f, 0.4f, 8, PaletteSwatch.Rust);
            b.Prism(axis * At(0f, 0.62f, 0f), 0.16f, 0.25f, 6, PaletteSwatch.Charcoal);
            SiteKit.DustCap(b, b.RangeFrom(first));
        }

        /// <summary>Six drill pipes strapped in a bundle on two chocks.</summary>
        private static void Pipes(SiteRecipe site)
        {
            var centre = new Vector3(-3f, 0.42f, -1.4f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre,
                centre + new Vector3(0.9f, 0.2f, -0.18f),
                new Vector3(0.2f, 0.4f, -1f));
            LowPolyMeshBuilder b = piece.Geometry;
            var alongX = new Vector3(0f, 0f, -90f);
            Vector2[] stack =
            {
                new Vector2(-0.24f, -0.1f), new Vector2(0f, -0.1f), new Vector2(0.24f, -0.1f),
                    new Vector2(-0.12f, 0.1f),
                new Vector2(0.12f, 0.1f), new Vector2(0f, 0.3f),
            };
            for (int i = 0; i < stack.Length; i++)
            {
                b.Prism(At(centre + new Vector3(0.04f * (i % 3), stack[i].y, stack[i].x), alongX), 0.11f, 2.4f, 6,
                    i == 4 ? PaletteSwatch.Rust : PaletteSwatch.Metal);
            }

            foreach (float x in new[] { -0.7f, 0.7f })
            {
                b.Torus(At(centre + new Vector3(x, 0.08f, 0f), alongX), 0.37f, 0.03f, 8, 3, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>The rusted generator on the skid: grille, exhaust stack, fuel cap.</summary>
        private static void Generator(SiteRecipe site)
        {
            var foot = new Vector3(-2.1f, Deck + 0.02f, 1.05f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, foot + Vector3.up * 0.1f,
                foot + new Vector3(0.72f, 0.12f, -0.3f), new Vector3(0.6f, 0.2f, -0.8f));
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 body = At(foot + Vector3.up * 0.46f, new Vector3(0f, 4f, -1.2f));
            int first = b.TriangleCount;
            b.Box(body, new Vector3(1.4f, 0.9f, 0.9f), PaletteSwatch.Rust, 0.05f);
            b.Box(body * At(0f, 0.5f, 0.1f), new Vector3(0.5f, 0.12f, 0.4f), PaletteSwatch.Rust, 0.03f);
            SiteKit.DustCap(b, b.RangeFrom(first));
            for (int i = 0; i < 4; i++)
            {
                b.Box(body * At(-0.35f + i * 0.12f, 0f, -0.455f), new Vector3(0.05f, 0.6f, 0.02f),
                    PaletteSwatch.Charcoal);
            }

            b.Box(body * At(0.3f, 0f, -0.452f), new Vector3(0.5f, 0.6f, 0.02f), PaletteSwatch.Metal);
            RecipeKit.Rod(b, body.MultiplyPoint3x4(new Vector3(0.5f, 0.45f, 0.25f)),
                body.MultiplyPoint3x4(new Vector3(0.55f, 1.1f, 0.3f)), 0.06f, 6, PaletteSwatch.Charcoal);
            b.Prism(body * At(-0.45f, 0.48f, -0.2f), 0.08f, 0.08f, 6, PaletteSwatch.Metal);
        }

        /// <summary>The control cabinet at the derrick end of the skid, its door hanging open, cables cut.</summary>
        private static void Cabinet(SiteRecipe site)
        {
            var foot = new Vector3(-0.45f, Deck, 1.75f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, foot + Vector3.up * 0.1f,
                foot + new Vector3(0f, 0.15f, -0.3f), new Vector3(0f, 0.3f, -1f));
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 body = At(foot + Vector3.up * 0.56f, new Vector3(0f, 0f, -1.2f));
            b.Box(body, new Vector3(0.75f, 1.1f, 0.45f), PaletteSwatch.Sage, 0.03f);
            b.Box(body * At(0f, 0f, -0.226f), new Vector3(0.6f, 0.9f, 0.01f), PaletteSwatch.Charcoal);
            b.Box(body * At(new Vector3(-0.62f, 0f, -0.42f), new Vector3(0f, 58f, 0f)),
                new Vector3(0.62f, 0.95f, 0.03f),
                PaletteSwatch.Sage);
            SiteKit.RustPatch(b, SiteKit.Face(body.MultiplyPoint3x4(new Vector3(0.15f, -0.2f, -0.24f)), Vector3.back,
                Vector3.up), 0f, 0f, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 start = body.MultiplyPoint3x4(new Vector3(-0.15f + i * 0.15f, -0.25f, -0.2f));
                SiteKit.Cable(b, new[]
                {
                    start, start + new Vector3(0.05f * i, -0.15f, -0.35f),
                    new Vector3(start.x + 0.1f * i, Deck + 0.03f, start.z - 0.75f - i * 0.1f),
                }, 0.03f, i == 1 ? PaletteSwatch.Charcoal : PaletteSwatch.WarmAccent);
            }
        }

        /// <summary>A cable reel on its side by the borehole, its copper end run out into the dust.</summary>
        private static void Reel(SiteRecipe site)
        {
            var centre = new Vector3(2.75f, 0.52f, -1.35f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, centre, centre + new Vector3(0f, 0f, -0.36f),
                new Vector3(-0.3f, 0.2f, -1f));
            SiteKit.Drum(piece.Geometry, At(centre, new Vector3(0f, 78f, 0f)), 0.52f, 0.62f, PaletteSwatch.WarmAccent);
        }

        /// <summary>
        /// The work lamp clamped to the top of its pole: a big lens in a hood, a small camera beside it.
        /// </summary>
        private static void WorkLamp(SiteRecipe site)
        {
            var normal = new Vector3(0.45f, -0.35f, -0.82f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, PoleTop + new Vector3(0.05f, 0.08f, -0.05f),
                PoleTop + new Vector3(0.12f, 0.02f, -0.14f), normal);
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 head = SiteKit.Face(PoleTop + new Vector3(0.18f, 0.22f, -0.2f), normal, Vector3.up);
            b.Box(head * At(0f, 0f, -0.12f), new Vector3(0.62f, 0.5f, 0.32f), PaletteSwatch.FadedPaint, 0.04f);
            b.Prism(head * At(new Vector3(0f, 0f, 0.05f), AlongZ), 0.22f, 0.04f, 10, PaletteSwatch.SkyHorizon);
            b.Torus(head * At(new Vector3(0f, 0f, 0.06f), AlongZ), 0.24f, 0.03f, 10, 3, PaletteSwatch.Metal);
            b.Box(head * At(0f, 0.3f, -0.02f), new Vector3(0.66f, 0.04f, 0.42f), PaletteSwatch.Metal);
            b.Box(head * At(0.42f, -0.12f, -0.08f), new Vector3(0.18f, 0.16f, 0.28f), PaletteSwatch.Metal, 0.02f);
            b.Prism(head * At(new Vector3(0.42f, -0.12f, 0.08f), AlongZ), 0.06f, 0.04f, 8, PaletteSwatch.SkyHorizon);
            b.Box(At(PoleTop + new Vector3(0f, 0.08f, 0f)), new Vector3(0.24f, 0.2f, 0.24f), PaletteSwatch.Metal);
        }
    }
}
