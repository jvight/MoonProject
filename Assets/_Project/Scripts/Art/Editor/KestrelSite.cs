using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// site.kestrel, Kestrel-3, the relay satellite that came down along the fall line (+Z) and ploughed into the
    /// crater floor: its crumpled bus nose-down in a bow wave of dust, split open at the back, a bent antenna boom
    /// rising over 7 m with its dish hanging from the tip (the landmark the base sees, and it stays when the site is
    /// picked clean), a wing yoke stump on one side and a bare wing frame lying on the other. Salvage: the wing that
    /// tore off and stands on edge in the dust (the drag piece), a torn panel left in the frame, a small dish knocked
    /// off, a bent foil hull panel, a propellant tank, the antenna harness and the avionics box inside the split.
    /// </summary>
    internal static class KestrelSite
    {
        private const float HalfX = 1.2f;
        private const float HalfY = 1.1f;
        private const float HalfZ = 1.3f;
        private const float Wall = 0.09f;

        private static readonly Vector3 BusCentre = new Vector3(0.4f, 0.8f, 1.9f);

        // Nose down along the fall line, slewed and rolled by the impact.
        private static readonly Vector3 BusEuler = new Vector3(22f, 18f, 10f);

        private static Matrix4x4 Bus => At(BusCentre, BusEuler);

        public static SiteRecipe Create()
        {
            var site = new SiteRecipe();
            LowPolyMeshBuilder b = site.Skeleton;
            Body(b);
            Vector3 tip = Boom(b);
            WingStump(b);
            WingFrame(b);
            Scraps(b);
            SiteKit.Drift(b, new Vector3(0.7f, 0f, 3.9f), 4.4f, 1.8f, 0.75f, 108f, 31);
            SiteKit.Drift(b, new Vector3(-1.3f, 0f, 2.2f), 2.6f, 1.1f, 0.45f, 10f, 32);
            SiteKit.Drift(b, new Vector3(-2.6f, 0f, 4.7f), 1.8f, 1.6f, 0.35f, 40f, 33);

            StandingWing(site);
            FramePanel(site);
            SmallDish(site);
            FoilPanel(site);
            Tank(site);
            Harness(site, tip);
            Avionics(site);
            site.Heart = Bus.MultiplyPoint3x4(new Vector3(0.45f, -HalfY + Wall + 0.03f, -0.1f));
            return site;
        }

        /// <summary>The bus as a crumpled shell: five plates, the back torn open and one flap peeled out.</summary>
        private static void Body(LowPolyMeshBuilder b)
        {
            Matrix4x4 bus = Bus;
            int first = b.TriangleCount;
            b.Box(bus * At(0f, -HalfY + Wall * 0.5f, 0f), new Vector3(HalfX * 2f, Wall, HalfZ * 2f),
                PaletteSwatch.Metal);
            b.Box(bus * At(new Vector3(0f, HalfY - Wall * 0.5f, 0.05f), new Vector3(3f, 0f, -4f)),
                new Vector3(HalfX * 2f, Wall, HalfZ * 2f - 0.1f), PaletteSwatch.Metal, 0.03f,
                new Displacement(41, 0.08f, 1.3f));
            b.Box(bus * At(0f, 0f, HalfZ - Wall * 0.5f), new Vector3(HalfX * 2f, HalfY * 2f, Wall),
                PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 outward = bus.MultiplyVector(Vector3.right * side);
                Matrix4x4 plate = bus * At(new Vector3(side * (HalfX - Wall * 0.5f - (side > 0 ? 0.06f : 0f)), -0.03f,
                    0.04f), new Vector3(0f, side * 3f, side * 5f));
                b.Box(plate, new Vector3(Wall, HalfY * 2f - 0.08f, HalfZ * 2f - 0.1f),
                    Paint.Facing(PaletteSwatch.Honey, PaletteSwatch.Metal, outward, 0.6f), 0.03f,
                    new Displacement(42 + side, 0.07f, 1.4f));
            }

            Vector2[] flap =
            {
                new Vector2(-1.15f, 0f), new Vector2(0.2f, 0f), new Vector2(0.05f, -0.3f), new Vector2(-0.3f, -0.45f),
                new Vector2(-0.55f, -0.75f), new Vector2(-1.15f, -0.85f),
            };
            Matrix4x4 hinge = bus * At(new Vector3(0f, HalfY - 0.05f, -HalfZ + Wall * 0.5f), new Vector3(48f, 0f, 0f));
            b.Extrude(hinge, flap, Wall, PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, b.RangeFrom(first));

            for (int i = 0; i < 3; i++)
            {
                Vector3 root = bus.MultiplyPoint3x4(new Vector3(-0.7f + i * 0.65f, -0.3f + i * 0.25f, -HalfZ + 0.2f));
                SiteKit.Bar(b, root, root + bus.MultiplyVector(new Vector3(0.1f * (i - 1), 0.15f, -0.55f - i * 0.15f)),
                    0.07f, PaletteSwatch.Metal);
            }
        }

        /// <summary>
        /// The bent antenna boom from the bus top, its dish hanging from the tip. Returns the boom foot.
        /// </summary>
        private static Vector3 Boom(LowPolyMeshBuilder b)
        {
            Matrix4x4 bus = Bus;
            Vector3 foot = bus.MultiplyPoint3x4(new Vector3(-0.45f, HalfY, -0.55f));
            Vector3 knee = foot + new Vector3(0.1f, 2.85f, 0.85f);
            Vector3 tip = knee + new Vector3(-1.45f, 2.3f, 0.65f);
            b.Box(At(foot, BusEuler), new Vector3(0.6f, 0.2f, 0.6f), PaletteSwatch.Metal, 0.04f);
            RecipeKit.Rod(b, foot, knee, 0.2f, 8, PaletteSwatch.FadedPaint);
            b.Icosphere(At(knee), 0.27f, 0, PaletteSwatch.Metal);
            RecipeKit.Rod(b, knee, tip, 0.16f, 8, PaletteSwatch.FadedPaint);
            b.Box(At(tip, new Vector3(20f, 30f, 10f)), new Vector3(0.36f, 0.36f, 0.36f), PaletteSwatch.Metal, 0.04f);
            Vector3 mount = tip + new Vector3(-0.6f, -1.15f, -0.35f);
            RecipeKit.Rod(b, tip, mount, 0.07f, 6, PaletteSwatch.Metal);
            var opening = new Vector3(-0.35f, -0.45f, -0.82f);
            SiteKit.Dish(b, SiteKit.Face(mount, Vector3.Cross(opening, Vector3.right), opening), 1.15f,
                PaletteSwatch.FadedPaint);
            Matrix4x4 band = SiteKit.Face(Vector3.Lerp(foot, knee, 0.45f), Vector3.back, knee - foot);
            SiteKit.RustStreak(b, band * At(0f, 0f, 0.19f), 0f, 0.3f, 0.7f);
            return foot;
        }

        /// <summary>The left wing's yoke: a stub with the torn hinge fork where the standing wing tore off.</summary>
        private static void WingStump(LowPolyMeshBuilder b)
        {
            Matrix4x4 bus = Bus;
            Vector3 root = bus.MultiplyPoint3x4(new Vector3(-HalfX, 0.15f, -0.2f));
            Vector3 end = bus.MultiplyPoint3x4(new Vector3(-HalfX - 0.75f, 0.2f, -0.2f));
            RecipeKit.Rod(b, root, end, 0.11f, 8, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 prong = end + bus.MultiplyVector(new Vector3(-0.08f, 0f, side * 0.16f));
                SiteKit.Bar(b, prong, prong + bus.MultiplyVector(new Vector3(-0.32f, side * 0.06f, side * 0.05f)),
                    0.07f, PaletteSwatch.Rust);
            }
        }

        /// <summary>
        /// The right wing's bare frame, lying in the dust off the yoke: rails and cross bars, no panels.
        /// </summary>
        private static void WingFrame(LowPolyMeshBuilder b)
        {
            Matrix4x4 bus = Bus;
            Vector3 yoke = bus.MultiplyPoint3x4(new Vector3(HalfX + 0.05f, 0.1f, -0.2f));
            var nearA = new Vector3(2.4f, 0.45f, 1.05f);
            var nearB = new Vector3(2.75f, 0.5f, 2.25f);
            var farA = new Vector3(5.6f, 0.08f, -0.35f);
            var farB = new Vector3(6f, 0.1f, 0.85f);
            RecipeKit.Rod(b, yoke, (nearA + nearB) * 0.5f, 0.1f, 8, PaletteSwatch.Metal);
            SiteKit.Bar(b, nearA, farA, 0.09f, PaletteSwatch.Metal);
            SiteKit.Bar(b, nearB, farB, 0.09f, PaletteSwatch.Metal);
            for (int i = 0; i <= 3; i++)
            {
                float t = i / 3f;
                SiteKit.Bar(b, Vector3.Lerp(nearA, farA, t), Vector3.Lerp(nearB, farB, t), 0.07f,
                    i == 2 ? PaletteSwatch.Rust : PaletteSwatch.Metal);
            }

            SiteKit.Drift(b, new Vector3(4.7f, 0f, 0.9f), 2.2f, 0.8f, 0.25f, 72f, 34);
        }

        /// <summary>Bits of hull skin half buried around the bus and a snapped cable in the dust.</summary>
        private static void Scraps(LowPolyMeshBuilder b)
        {
            b.Box(At(new Vector3(-3.1f, 0.15f, -0.4f), new Vector3(35f, 40f, 15f)), new Vector3(0.9f, 0.05f, 0.6f),
                PaletteSwatch.Metal);
            b.Box(At(new Vector3(1.9f, 0.12f, -1.9f), new Vector3(-28f, -20f, 8f)), new Vector3(0.7f, 0.05f, 0.5f),
                PaletteSwatch.Honey);
            b.Box(At(new Vector3(-1.6f, 0.1f, 5.6f), new Vector3(20f, 70f, -25f)), new Vector3(1f, 0.05f, 0.5f),
                PaletteSwatch.Metal);
            SiteKit.Cable(b, new[]
            {
                new Vector3(-1f, 0.03f, 0.1f), new Vector3(-1.8f, 0.03f, -0.6f), new Vector3(-2.3f, 0.03f, -0.4f),
                new Vector3(-2.9f, 0.03f, -1.1f),
            }, 0.03f, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// The left wing, torn off and stuck root-first in the dust, standing on edge and leaning away: the tall
        /// violet blade the base sees beside the boom. It must be tethered clear before it can be cut.
        /// </summary>
        private static void StandingWing(SiteRecipe site)
        {
            var root = new Vector3(-3.6f, 0.3f, 2.4f);
            Vector3 along = new Vector3(-0.32f, 0.94f, 0.08f).normalized;
            var normal = new Vector3(0.2f, 0f, -1f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, root,
                root + new Vector3(0.05f, 0.15f, -0.25f), new Vector3(0.2f, 0.25f, -1f), true);
            Matrix4x4 face = SiteKit.Face(root + along * 2.25f, normal, along);
            SiteKit.SolarPanel(piece.Geometry, face, 1.6f, 4.4f, true);
            piece.Geometry.Box(At(root, Rotation(new Vector3(0f, -11f, 18f))), new Vector3(0.5f, 0.6f, 0.4f),
                PaletteSwatch.Metal, 0.04f);
            Vector3 behind = -normal.normalized * 0.07f;
            SiteKit.Bar(piece.Geometry, root + along * 0.2f + behind, root + along * 4.4f + behind, 0.1f,
                PaletteSwatch.Metal);
        }

        /// <summary>The last panel still in the bare frame's far bay, hanging on one hinge.</summary>
        private static void FramePanel(SiteRecipe site)
        {
            var centre = new Vector3(5.25f, 0.24f, 0.35f);
            var normal = new Vector3(-0.1f, 1f, -0.12f);
            var hinge = new Vector3(4.75f, 0.24f, -0.1f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, hinge, hinge + new Vector3(0f, 0.05f, 0f),
                new Vector3(-0.3f, 0.6f, -0.75f));
            Matrix4x4 face = SiteKit.Face(centre, normal, new Vector3(0.95f, 0f, -0.32f)) *
                Matrix4x4.Rotate(Rotation(new Vector3(8f, 0f, 0f)));
            SiteKit.SolarPanel(piece.Geometry, face, 1.1f, 1.05f, false);
        }

        /// <summary>A small dish knocked off the bus, leaning against its right side.</summary>
        private static void SmallDish(SiteRecipe site)
        {
            var back = new Vector3(1.75f, 0.55f, 0f);
            var opening = new Vector3(0.55f, 0.45f, -0.7f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, back + opening.normalized * 0.12f,
                back + opening.normalized * 0.45f, opening);
            SiteKit.Dish(piece.Geometry, SiteKit.Face(back, Vector3.Cross(opening, Vector3.up), opening), 0.62f,
                PaletteSwatch.SkyHorizon);
        }

        /// <summary>A hull panel bent double, gold foil up, thrown ahead of the bus.</summary>
        private static void FoilPanel(SiteRecipe site)
        {
            var crease = new Vector3(3.3f, 0.22f, 3.7f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, crease, crease + new Vector3(0f, 0.1f, -0.1f),
                new Vector3(-0.3f, 0.7f, -0.6f));
            LowPolyMeshBuilder b = piece.Geometry;
            int first = b.TriangleCount;
            Paint foilUp = Paint.Facing(PaletteSwatch.Honey, PaletteSwatch.Metal, Vector3.up, 0.3f);
            Paint foilFront = Paint.Facing(PaletteSwatch.Honey, PaletteSwatch.Metal, Vector3.back, 0.3f);
            b.Box(At(crease + new Vector3(0.05f, -0.1f, -0.45f), new Vector3(-12f, 25f, 4f)),
                new Vector3(1.3f, 0.06f, 0.95f), foilUp);
            b.Box(At(crease + new Vector3(0.1f, 0.25f, 0.3f), new Vector3(-58f, 25f, 4f)),
                new Vector3(1.3f, 0.06f, 0.8f), foilFront);
            b.Prism(At(crease, new Vector3(0f, 25f, 90f)), 0.07f, 1.32f, 6, PaletteSwatch.Metal);
            SiteKit.DustCap(b, b.RangeFrom(first));
        }

        private static void Tank(SiteRecipe site)
        {
            var centre = new Vector3(-2.5f, 0.42f, 4.7f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre,
                centre + new Vector3(0.4f, 0.35f, -0.25f),
                new Vector3(0.5f, 0.5f, -0.7f));
            LowPolyMeshBuilder b = piece.Geometry;
            int first = b.TriangleCount;
            b.Icosphere(At(centre), 0.62f, 1, PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, b.RangeFrom(first));
            Matrix4x4 tilt = At(centre, new Vector3(18f, 0f, -12f));
            b.Torus(tilt, 0.62f, 0.05f, 12, 4, PaletteSwatch.Metal);
            RecipeKit.Rod(b, tilt.MultiplyPoint3x4(new Vector3(0f, 0.55f, 0f)),
                tilt.MultiplyPoint3x4(new Vector3(0.1f, 0.85f, 0.05f)), 0.06f, 6, PaletteSwatch.Metal);
            SiteKit.RustPatch(b, SiteKit.Face(centre + new Vector3(0.18f, 0.1f, -0.58f), new Vector3(0.3f, 0.15f, -1f),
                Vector3.up), 0f, 0f, 0.35f);
        }

        /// <summary>
        /// The antenna harness: a copper bundle looping down the boom foot to its connector on the bus.
        /// </summary>
        private static void Harness(SiteRecipe site, Vector3 foot)
        {
            Matrix4x4 bus = Bus;
            Vector3 connector = bus.MultiplyPoint3x4(new Vector3(0.35f, HalfY + 0.12f, -0.75f));
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, connector,
                connector + new Vector3(0f, 0.15f, -0.05f), new Vector3(0f, 0.6f, -0.8f));
            LowPolyMeshBuilder b = piece.Geometry;
            b.Box(At(connector, BusEuler), new Vector3(0.42f, 0.24f, 0.32f), PaletteSwatch.Sage, 0.03f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(0.04f * (i - 1), 0f, 0.05f * (i - 1));
                SiteKit.Cable(b, new[]
                {
                    connector + offset + new Vector3(-0.15f, 0.08f, 0f),
                    foot + offset + new Vector3(0.45f, 0.35f, -0.35f + i * 0.04f),
                    foot + offset + new Vector3(0.2f, 1.1f, -0.1f),
                    foot + offset + new Vector3(0.05f, 1.6f, 0.3f),
                }, 0.05f, i == 1 ? PaletteSwatch.Charcoal : PaletteSwatch.WarmAccent);
            }
        }

        /// <summary>The avionics box in the split, its cables spilling out over the lip into the dust.</summary>
        private static void Avionics(SiteRecipe site)
        {
            Matrix4x4 bus = Bus;
            Vector3 mount = bus.MultiplyPoint3x4(new Vector3(-0.45f, -HalfY + Wall + 0.25f, -0.75f));
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, mount,
                bus.MultiplyPoint3x4(new Vector3(-0.45f, -HalfY + Wall + 0.52f, -1.05f)),
                bus.MultiplyVector(new Vector3(0f, 0.5f, -1f)));
            LowPolyMeshBuilder b = piece.Geometry;
            b.Box(At(mount, BusEuler), new Vector3(0.75f, 0.48f, 0.55f), PaletteSwatch.Sage, 0.03f);
            Matrix4x4 front = bus * At(new Vector3(-0.45f, -HalfY + Wall + 0.25f, -1.03f));
            b.Box(front, new Vector3(0.6f, 0.34f, 0.02f), PaletteSwatch.Charcoal);
            Vector3 lip = bus.MultiplyPoint3x4(new Vector3(-0.3f, -HalfY + 0.05f, -HalfZ - 0.05f));
            for (int i = 0; i < 3; i++)
            {
                Vector3 start = front.MultiplyPoint3x4(new Vector3(-0.2f + i * 0.2f, -0.1f, 0.02f));
                Vector3 end = new Vector3(lip.x - 0.6f + i * 0.5f, 0.04f, lip.z - 0.7f - i * 0.25f);
                SiteKit.Cable(b, new[] { start, lip + new Vector3(i * 0.15f, 0.04f, 0f), end }, 0.035f,
                    i == 1 ? PaletteSwatch.Charcoal : PaletteSwatch.WarmAccent);
            }
        }
    }
}
