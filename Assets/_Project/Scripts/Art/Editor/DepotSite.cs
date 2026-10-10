using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// site.depot, the crew's supply shed near the base and the first site 07 salvages: a shed frame on a dusty slab,
    /// its left half collapsed onto the ground, a sagging awning over the open front with a tilted STORES sign, a
    /// stripped shelf inside, an emptied pod half sunk in a drift. Salvage: the corrugated sheet on the collapsed
    /// roof, a wall panel hanging by one bolt, a toppled cargo pod, a junction box on the front post, a cable drum
    /// under the roof and a solar panel on its rack. Friendly to read: open front, big shapes, no drag piece.
    /// </summary>
    internal static class DepotSite
    {
        private const float Floor = 0.12f;
        private const float Eave = 3f;
        private const float Front = 1f;
        private const float Back = 5f;
        private const float Side = 3.5f;
        private const float Post = 0.16f;
        private const float BeamSize = 0.14f;

        // The collapsed left roof, hinged on the middle posts and lying on the ground past the left posts (x, y).
        // Rust causes per metre along the wall panels' top seams (few, at random gaps).
        private const float PanelRunsPerMetre = 1.4f;

        private static readonly Vector2 SlopeTop = new Vector2(0f, 2.95f);
        private static readonly Vector2 SlopeFoot = new Vector2(-4.3f, 0.18f);

        public static SiteRecipe Create()
        {
            var site = new SiteRecipe();
            LowPolyMeshBuilder b = site.Skeleton;
            Slab(b);
            Frame(b);
            Walls(b);
            Awning(b);
            Shelf(b);
            Rack(b);
            SiteKit.Cable(b, SiteKit.Sag(new Vector3(-5.3f, 1.2f, 1.1f), new Vector3(-0.08f, 2.2f, 0.9f), 0.9f, 6),
                0.025f, PaletteSwatch.Charcoal);
            SiteKit.Bar(b, new Vector3(3.5f, 1.35f, Front - 0.09f), new Vector3(3.5f, 1.75f, Front - 0.09f), 0.06f,
                PaletteSwatch.Metal);
            SiteKit.Pod(b, At(new Vector3(-6.4f, 0.12f, 4.1f), new Vector3(-6f, 20f, 4f)), 0.6f, 1.9f, true);
            SiteKit.Drift(b, new Vector3(1.6f, 0f, 5.75f), 3.8f, 1.3f, 0.6f, 90f, 11);
            SiteKit.Drift(b, new Vector3(-4.75f, 0f, 3f), 4.6f, 1.2f, 0.42f, 0f, 12);
            SiteKit.Drift(b, new Vector3(-6.1f, 0f, 3.3f), 2.8f, 1.8f, 0.5f, 20f, 13);
            SiteKit.Drift(b, new Vector3(4.35f, 0f, 3.6f), 2.6f, 0.9f, 0.4f, 0f, 14);
            SiteKit.Drift(b, new Vector3(4.1f, 0f, -0.2f), 1.8f, 0.8f, 0.3f, 70f, 15);

            CollapsedSheet(site);
            HangingPanel(site);
            ToppledPod(site);
            JunctionBox(site);
            Drum(site);
            Solar(site);
            site.Heart = new Vector3(0.7f, Floor + 0.02f, 4.4f);
            return site;
        }

        private static Vector3 OnSlope(float t, float z, float lift)
        {
            Vector2 p = Vector2.Lerp(SlopeTop, SlopeFoot, t) + SlopeNormal * lift;
            return new Vector3(p.x, p.y, z);
        }

        private static Vector2 SlopeNormal
        {
            get
            {
                Vector2 down = (SlopeFoot - SlopeTop).normalized;
                return new Vector2(down.y, -down.x);
            }
        }

        /// <summary>The cracked slab with a low ramp at its front for wheels.</summary>
        private static void Slab(LowPolyMeshBuilder b)
        {
            MeshRange slab = b.Box(At(0.2f, 0.04f, 2.9f), new Vector3(8.6f, 0.16f, 6.2f), PaletteSwatch.Metal, 0.03f);
            SiteKit.DustCap(b, slab);
            b.Box(At(new Vector3(-1.4f, Floor + 0.003f, 2.4f), new Vector3(0f, 32f, 0f)),
                new Vector3(0.06f, 0.008f, 2.8f), PaletteSwatch.Charcoal);
            MeshRange ramp = b.Wedge(At(new Vector3(2f, 0.06f, -0.6f), new Vector3(0f, 180f, 0f)),
                new Vector3(2.2f, 0.12f, 0.8f), PaletteSwatch.Metal);
            SiteKit.DustCap(b, ramp);
        }

        /// <summary>Posts and beams: the right half standing, the left half folded down onto the ground.</summary>
        private static void Frame(LowPolyMeshBuilder b)
        {
            foreach (float z in new[] { Front, Back })
            {
                SiteKit.Bar(b, new Vector3(Side, Floor, z), new Vector3(Side, Eave, z), Post, PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(0f, Floor, z), new Vector3(0f, Eave, z), Post, PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(0f, Eave, z), new Vector3(Side, Eave, z), BeamSize, PaletteSwatch.Metal);
                SiteKit.Bar(b, OnSlope(0f, z, 0f), OnSlope(1f, z, 0f), BeamSize, PaletteSwatch.Metal);
                Matrix4x4 foot = SiteKit.Face(new Vector3(Side, Floor, z - Post * 0.5f - 0.002f), Vector3.back,
                    Vector3.up);
                SiteKit.RustStreak(b, foot, 0f, 0.5f, 0.35f);
            }

            SiteKit.Bar(b, new Vector3(-Side, Floor, Front), new Vector3(-Side, 0.5f, Front), Post,
                PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(-Side - 0.3f, 0.08f, Front - 0.4f),
                new Vector3(-Side - 1.6f, 0.08f, Front - 2.3f), Post, PaletteSwatch.Rust);
            SiteKit.Bar(b, new Vector3(-Side, Floor, Back), new Vector3(-Side, 0.4f, Back), Post, PaletteSwatch.Metal);
            foreach (float x in new[] { 0f, 1.75f, Side })
            {
                SiteKit.Bar(b, new Vector3(x, Eave + 0.13f, Front - 0.25f), new Vector3(x, Eave + 0.13f, Back + 0.25f),
                    0.12f, PaletteSwatch.Metal);
            }

            SiteKit.Bar(b, OnSlope(0.4f, Front - 0.2f, 0.13f), OnSlope(0.4f, Back + 0.25f, 0.13f), 0.12f,
                PaletteSwatch.Metal);

            // What is left of the right roof: one sheet over the back, rusted through.
            Matrix4x4 roof = SiteKit.Face(new Vector3(1.8f, Eave + 0.21f, 4.05f), Vector3.up, Vector3.back);
            int first = b.TriangleCount;
            SiteKit.Sheet(b, roof, 3.7f, 2.3f, PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, b.RangeFrom(first));
            SiteKit.RustPatch(b, roof * At(0f, 0f, 0.05f), -0.6f, 0.2f, 1.1f);
        }

        /// <summary>The right wall's back panels and the right half of the back wall; the rest is gone.</summary>
        private static void Walls(LowPolyMeshBuilder b)
        {
            const float panel = 4f / 3f;
            float height = Eave - Floor;
            for (int i = 1; i < 3; i++)
            {
                float z = Front + (i + 0.5f) * panel;
                Matrix4x4 wall = SiteKit.Face(new Vector3(Side + 0.1f, Floor + height * 0.5f, z), Vector3.right,
                    Vector3.up);
                SiteKit.Sheet(b, wall, panel - 0.02f, height, PaletteSwatch.FadedPaint);
                SiteKit.RustSeam(b, wall * At(0f, 0f, 0.05f), -panel * 0.5f, panel * 0.5f, height * 0.42f,
                    height * 0.85f, PanelRunsPerMetre, 300 + i);
            }

            Matrix4x4 back = SiteKit.Face(new Vector3(Side * 0.5f, Floor + height * 0.5f, Back + 0.1f), Vector3.forward,
                Vector3.up);
            SiteKit.Sheet(b, back, Side, height, PaletteSwatch.FadedPaint);
            SiteKit.RustPatch(b, back * At(0f, 0f, 0.05f), 0.6f, -0.4f, 0.8f);

            Matrix4x4 crumpled = SiteKit.Face(new Vector3(-1.6f, 0.75f, Back + 0.35f), new Vector3(0f, 0.55f, 1f),
                new Vector3(0.3f, 1f, 0f));
            SiteKit.Sheet(b, crumpled, 2.4f, 1.3f, PaletteSwatch.FadedPaint);
        }

        /// <summary>
        /// The awning over the right of the open front: one post bowed, the canvas sagging between them.
        /// </summary>
        private static void Awning(LowPolyMeshBuilder b)
        {
            var rightTop = new Vector3(3.3f, 2.35f, -0.5f);
            var leftTop = new Vector3(0.45f, 1.95f, -0.55f);
            SiteKit.Bar(b, new Vector3(3.3f, 0f, -0.5f), rightTop, 0.1f, PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(0.75f, 0f, -0.45f), new Vector3(0.68f, 1.2f, -0.5f), 0.1f, PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(0.68f, 1.2f, -0.5f), leftTop, 0.1f, PaletteSwatch.Metal);
            SiteKit.Bar(b, leftTop, rightTop, 0.09f, PaletteSwatch.Metal);
            SiteKit.Bar(b, rightTop, new Vector3(3.3f, Eave - 0.1f, Front), 0.08f, PaletteSwatch.Metal);
            SiteKit.Bar(b, leftTop, new Vector3(0.5f, Eave - 0.1f, Front), 0.08f, PaletteSwatch.Metal);

            var eave = new Vector3(1.9f, Eave - 0.05f, Front - 0.05f);
            var sag = new Vector3(1.9f, 2.2f, 0.2f);
            var lip = new Vector3(1.9f, 2.17f, -0.5f);
            Canvas(b, eave, sag, 2.75f);
            Canvas(b, sag, lip, 2.75f);

            // The STORES sign fell and leans back against the awning post, still readable from the way in.
            Matrix4x4 sign = At(new Vector3(2.75f, 0.2f, -0.68f), new Vector3(-30f, 192f, 0f));
            b.Box(sign, new Vector3(1.2f, 0.4f, 0.04f), PaletteSwatch.FadedPaint, 0.01f);
            Glyphs.Write(b, sign * At(0f, 0f, 0.021f), "STORES", 0.18f, PaletteSwatch.Rust, PaletteSwatch.FadedPaint);
        }

        private static void Canvas(LowPolyMeshBuilder b, Vector3 from, Vector3 to, float width)
        {
            Vector3 along = to - from;
            Vector3 normal = Vector3.Cross(Vector3.right, along);
            Matrix4x4 face = SiteKit.Face((from + to) * 0.5f, normal, along);
            b.Box(face, new Vector3(width, along.magnitude + 0.04f, 0.03f), PaletteSwatch.FadedPaint);
        }

        /// <summary>A crew shelf against the back wall, stripped bare (a human-scale remnant).</summary>
        private static void Shelf(LowPolyMeshBuilder b)
        {
            foreach (float x in new[] { 2.25f, 3.25f })
            {
                foreach (float z in new[] { 4.35f, 4.8f })
                {
                    SiteKit.Bar(b, new Vector3(x, Floor, z), new Vector3(x, 2f, z), 0.05f, PaletteSwatch.Metal);
                }
            }

            foreach (float y in new[] { 0.5f, 1.2f, 1.9f })
            {
                b.Box(At(2.75f, y, 4.575f), new Vector3(1.05f, 0.03f, 0.5f), PaletteSwatch.Metal);
            }

            b.Box(At(new Vector3(2.6f, 0.9f, 4.55f), new Vector3(0f, 0f, 24f)), new Vector3(0.9f, 0.03f, 0.48f),
                PaletteSwatch.Rust);
        }

        /// <summary>The solar panel's ground rack, left of the shed.</summary>
        private static void Rack(LowPolyMeshBuilder b)
        {
            foreach (float x in new[] { -6.7f, -5.3f })
            {
                SiteKit.Bar(b, new Vector3(x, 0f, 1.05f), new Vector3(x, 1.24f, 1.05f), 0.08f, PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(x, 0f, 0.15f), new Vector3(x, 0.66f, 0.15f), 0.08f, PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(x, 0.05f, 0.15f), new Vector3(x, 0.05f, 1.05f), 0.06f, PaletteSwatch.Rust);
            }

            SiteKit.Bar(b, new Vector3(-6.75f, 1.24f, 1.1f), new Vector3(-5.25f, 1.24f, 1.1f), 0.06f,
                PaletteSwatch.Metal);
        }

        private static void CollapsedSheet(SiteRecipe site)
        {
            Vector3 normal = new Vector3(SlopeNormal.x, SlopeNormal.y, 0f);
            Vector3 upSlope = new Vector3(SlopeTop.x - SlopeFoot.x, SlopeTop.y - SlopeFoot.y, 0f);
            Vector3 centre = OnSlope(0.38f, 3.75f, 0.21f);
            Vector3 pivot = OnSlope(0.16f, 3.75f, 0.22f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, pivot, OnSlope(0.14f, 3.1f, 0.26f), normal);
            Matrix4x4 face = SiteKit.Face(centre, normal,
                upSlope) * Matrix4x4.Rotate(Rotation(new Vector3(0f, 0f, 4f)));
            SiteKit.Sheet(piece.Geometry, face, 2.7f, 2.5f, PaletteSwatch.FadedPaint);
            SiteKit.DustCap(piece.Geometry, piece.Geometry.RangeFrom(0));
            SiteKit.RustStreak(piece.Geometry, face * At(0f, 0f, 0.05f), -0.7f, 1.05f, 0.8f);
            SiteKit.RustStreak(piece.Geometry, face * At(0f, 0f, 0.05f), 0.6f, 1.05f, 1.1f);
            SiteKit.RustPatch(piece.Geometry, face * At(0f, 0f, 0.05f), 0.2f, -0.3f, 0.6f);
        }

        /// <summary>
        /// The right wall's front panel, hanging from its top back bolt and resting a corner in the dust.
        /// </summary>
        private static void HangingPanel(SiteRecipe site)
        {
            var bolt = new Vector3(Side + 0.12f, Eave - 0.1f, Front + 4f / 3f - 0.08f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, bolt + new Vector3(0.05f, -0.1f, -0.1f), bolt,
                new Vector3(1f, 0f, -0.4f));
            float height = Eave - Floor;
            Matrix4x4 hang = At(bolt) * Matrix4x4.Rotate(Rotation(new Vector3(-15f, 0f, 18f)))
                * SiteKit.Face(new Vector3(0f, -height * 0.5f + 0.08f, -(4f / 3f) * 0.5f + 0.06f), Vector3.right,
                    Vector3.up);
            SiteKit.Sheet(piece.Geometry, hang, 4f / 3f - 0.02f, height, PaletteSwatch.FadedPaint);
            SiteKit.RustStreak(piece.Geometry, hang * At(0f, 0f, 0.05f), 0.2f, height * 0.4f, 1.2f);
            SiteKit.RustPatch(piece.Geometry, hang * At(0f, 0f, 0.05f), -0.2f, -0.6f, 0.5f);
        }

        private static void ToppledPod(SiteRecipe site)
        {
            var centre = new Vector3(4.5f, 0.5f, -1f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre,
                centre + new Vector3(-0.3f, 0.52f, -0.2f),
                new Vector3(-0.4f, 0.6f, -0.7f));
            Matrix4x4 frame = At(centre, new Vector3(0f, 65f, 8f));
            SiteKit.Pod(piece.Geometry, frame, 0.55f, 1.9f, false);
            Matrix4x4 axis = frame * Matrix4x4.Rotate(Rotation(AlongZ));
            piece.Geometry.Prism(axis * At(0f, -0.25f, 0f), 0.57f, 0.1f, 10, PaletteSwatch.Rust, false);
        }

        private static void JunctionBox(SiteRecipe site)
        {
            var back = new Vector3(Side, 1.3f, Front - Post * 0.5f - 0.02f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, back + new Vector3(0f, 0f, -0.08f),
                back + new Vector3(0f, 0.33f, -0.05f), new Vector3(0f, 0.3f, -1f));
            SiteKit.JunctionBox(piece.Geometry, SiteKit.Face(back, Vector3.back, Vector3.up),
                new Vector3(0.5f, 0.6f, 0.22f));
        }

        private static void Drum(SiteRecipe site)
        {
            var centre = new Vector3(1.6f, Floor + 0.56f, 3.7f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, centre, centre + new Vector3(0.45f, 0f, 0f),
                new Vector3(0.6f, 0.2f, -0.8f));
            SiteKit.Drum(piece.Geometry, At(centre, new Vector3(0f, 12f, 0f)), 0.55f, 0.75f, PaletteSwatch.WarmAccent);
        }

        private static void Solar(SiteRecipe site)
        {
            var normal = new Vector3(0f, 0.87f, -0.5f);
            var up = new Vector3(0f, 0.5f, 0.87f);
            var centre = new Vector3(-6f, 0.98f, 0.62f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, centre + up * 0.4f,
                centre + up * 0.48f + new Vector3(0.62f, 0f, 0f) + normal * 0.04f, normal);
            SiteKit.SolarPanel(piece.Geometry, SiteKit.Face(centre, normal, up), 1.6f, 1f, true);
        }
    }
}
