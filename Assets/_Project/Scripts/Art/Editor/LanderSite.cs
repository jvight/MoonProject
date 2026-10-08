using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// site.lander, a crashed cargo lander lying on its side in the canyon bay: a sister of the crew's home lander,
    /// its honey octagonal descent stage rusted and dusted, the engine bell pointing back up the canyon, two legs
    /// sticking up bent and one crushed under it, the cargo module split open toward the way in (ribs and a spine
    /// left, the front skin gone) and a broken crate spilled in front. Salvage: a peeled hull panel, the snapped leg,
    /// the avionics box on the stage, the docking camera, a spilled crate, a crate of cable spools and the torn cargo
    /// hatch (the drag piece).
    /// </summary>
    internal static class LanderSite
    {
        private const float StageRadius = 1.6f;
        private const float StageLength = 1.4f;
        private const float ModuleRadius = 1.15f;
        private const float ModuleLength = 3.4f;
        private const float Lift = 0.06f;

        private static readonly Vector3 Stage = new Vector3(-2f, 1.33f, 0.9f);
        private static readonly Vector3 Module = new Vector3(0.45f, 1.18f, 0.9f);

        // The module's frame: its axis (frame Z) along +X, dipping a little toward its nose.
        private static Matrix4x4 ModuleFrame => At(Module, new Vector3(0f, 90f, 0f))
            * Matrix4x4.Rotate(Rotation(new Vector3(3f, 0f, 0f)));

        public static SiteRecipe Create()
        {
            var site = new SiteRecipe();
            LowPolyMeshBuilder b = site.Skeleton;
            DescentStage(b);
            Legs(b);
            SiteKit.Cable(b, new[]
            {
                Stage + new Vector3(0.55f, 0.9f, -1.38f), Stage + new Vector3(0.75f, 0.2f, -1.75f),
                new Vector3(Stage.x + 0.9f, 0.03f, Stage.z - 2.3f), new Vector3(Stage.x + 1.4f, 0.03f, Stage.z - 2.6f),
            }, 0.035f, PaletteSwatch.Charcoal);
            CargoModule(b);
            SiteKit.Crate(b, At(new Vector3(2.7f, 0.3f, -2.1f), new Vector3(-8f, 32f, 14f)),
                new Vector3(0.9f, 0.6f, 0.7f));
            b.Box(At(new Vector3(2.62f, 0.62f, -2.1f), new Vector3(-8f, 32f, 14f)), new Vector3(0.7f, 0.02f, 0.5f),
                PaletteSwatch.Charcoal);
            SiteKit.Crate(b, At(new Vector3(3.3f, 0.12f, 3.1f), new Vector3(4f, -20f, -6f)),
                new Vector3(1f, 0.6f, 0.8f));
            SiteKit.Drift(b, new Vector3(0.4f, 0f, 2.3f), 4.8f, 1.4f, 0.55f, 90f, 71);
            SiteKit.Drift(b, new Vector3(-2.1f, 0f, 2.6f), 2.4f, 1.4f, 0.45f, 80f, 72);
            SiteKit.Drift(b, new Vector3(-0.2f, 0f, -0.5f), 3f, 0.9f, 0.3f, 88f, 73);

            PeeledPanel(site);
            BrokenLeg(site);
            Avionics(site);
            Camera(site);
            SpilledCrate(site);
            Spools(site);
            Hatch(site);
            site.Heart = new Vector3(0.6f, 0.04f, 1.15f);
            return site;
        }

        /// <summary>
        /// The descent stage on its side: rusted honey octagon, dusty top flats, the engine bell up the canyon.
        /// </summary>
        private static void DescentStage(LowPolyMeshBuilder b)
        {
            Matrix4x4 stage = At(Stage, new Vector3(0f, 0f, 90f));
            int first = b.TriangleCount;
            b.Prism(stage, StageRadius, StageLength, 8, Paint.WithCaps(PaletteSwatch.Honey, PaletteSwatch.Metal));
            SiteKit.DustCap(b, b.RangeFrom(first));
            b.Frustum(At(Stage + new Vector3(-StageLength * 0.5f - 0.5f, 0.1f, 0f), new Vector3(0f, 0f, -86f)), 0.75f,
                0.35f, 1f, 10, PaletteSwatch.Charcoal);
            b.Prism(At(Stage + new Vector3(-StageLength * 0.5f - 0.05f, 0.05f, 0f), new Vector3(0f, 0f, 90f)), 0.42f,
                0.12f, 8, PaletteSwatch.Metal);

            Matrix4x4 front = SiteKit.Face(Stage + new Vector3(0.15f, 0f, -StageRadius * 0.924f - 0.01f), Vector3.back,
                Vector3.up);
            SiteKit.RustPatch(b, front, 0.25f, -0.55f, 0.75f);
            SiteKit.RustStreak(b, front, -0.35f, 0.45f, 0.9f);
            SiteKit.RustStreak(b, front, 0.45f, 0.5f, 0.7f);
        }

        private static Vector3 Around(float x, float degrees, float radius)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector3(x, Stage.y + Mathf.Sin(angle) * radius, Stage.z - Mathf.Cos(angle) * radius);
        }

        /// <summary>
        /// Two legs bent up into the air, one crushed flat under the stage, the stub of the snapped one.
        /// </summary>
        private static void Legs(LowPolyMeshBuilder b)
        {
            foreach (float degrees in new[] { 45f, 135f })
            {
                Vector3 hip = Around(-1.5f, degrees, StageRadius - 0.1f);
                Vector3 knee = Around(-2.6f, degrees + (degrees < 90f ? -8f : 8f), StageRadius + 0.75f);
                Vector3 foot = Around(-3.5f, degrees + (degrees < 90f ? 6f : -14f), StageRadius + 1.1f);
                SiteKit.Bar(b, hip, knee, 0.16f, PaletteSwatch.Metal);
                SiteKit.Bar(b, knee, foot, 0.13f, PaletteSwatch.Metal);
                SiteKit.Bar(b, Around(-1.1f, degrees + 12f, StageRadius - 0.1f), knee, 0.08f, PaletteSwatch.Rust);
                b.Prism(At(foot, new Vector3(0f, 0f, 70f)), 0.32f, 0.08f, 8, PaletteSwatch.Metal);
            }

            Vector3 crushedHip = Around(-1.5f, 315f, StageRadius - 0.2f);
            SiteKit.Bar(b, crushedHip, new Vector3(-2.6f, 0.1f, -1.2f), 0.15f, PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(-2.6f, 0.1f, -1.2f), new Vector3(-3.4f, 0.08f, -0.8f), 0.12f,
                PaletteSwatch.Rust);
            b.Prism(At(new Vector3(-3.55f, 0.06f, -0.75f)), 0.32f, 0.08f, 8, PaletteSwatch.Metal);
            Vector3 stubHip = Around(-1.5f, 225f, StageRadius - 0.15f);
            SiteKit.Bar(b, stubHip, stubHip + new Vector3(-0.45f, -0.2f, 0.45f), 0.16f, PaletteSwatch.Rust);
        }

        /// <summary>
        /// The cargo module split open toward the way in: ring frames, a spine, the back and bottom skin kept, the
        /// nose cap on, the front skin gone.
        /// </summary>
        private static void CargoModule(LowPolyMeshBuilder b)
        {
            Matrix4x4 module = ModuleFrame;
            for (int i = 0; i < 5; i++)
            {
                float z = -ModuleLength * 0.5f + 0.1f + i * (ModuleLength - 0.2f) / 4f;
                b.Torus(module * At(new Vector3(0f, 0f, z), new Vector3(90f, 0f, 0f)), ModuleRadius, 0.07f, 12, 4,
                    i == 3 ? PaletteSwatch.Rust : PaletteSwatch.Metal);
            }

            foreach (float degrees in new[] { 90f, 200f, 270f })
            {
                float angle = degrees * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ModuleRadius;
                SiteKit.Bar(b, module.MultiplyPoint3x4(at + Vector3.back * ModuleLength * 0.5f),
                    module.MultiplyPoint3x4(at + Vector3.forward * ModuleLength * 0.5f), 0.1f, PaletteSwatch.Metal);
            }

            // Frame X is world -Z, so 0 degrees faces home (the way in) and 180 degrees faces away.
            MeshRange skin = SiteKit.ArcSheet(b, module, ModuleRadius + Lift, 100f, 300f, 7, ModuleLength - 0.1f,
                PaletteSwatch.FadedPaint);
            SiteKit.DustCap(b, skin);
            SiteKit.ArcSheet(b, module, ModuleRadius + Lift, 300f, 325f, 1, ModuleLength * 0.4f,
                PaletteSwatch.FadedPaint);
            Matrix4x4 back = SiteKit.Face(module.MultiplyPoint3x4(new Vector3(-0.9f, 0.7f, 0.4f)),
                module.MultiplyVector(new Vector3(-0.8f, 0.6f, 0f)), Vector3.up);
            SiteKit.RustStreak(b, back, 0f, 0.1f, 0.6f);

            Matrix4x4 nose = module * At(new Vector3(0f, 0f, ModuleLength * 0.5f + 0.05f), new Vector3(90f, 0f, 0f));
            b.Frustum(nose, ModuleRadius + 0.05f, ModuleRadius * 0.55f, 0.35f, 12,
                Paint.WithCaps(PaletteSwatch.FadedPaint, PaletteSwatch.Metal));
            b.Torus(module * At(new Vector3(0f, 0f, -ModuleLength * 0.5f), new Vector3(90f, 0f, 0f)),
                ModuleRadius + 0.02f, 0.12f, 12, 4, PaletteSwatch.Rust);
        }

        /// <summary>A skin panel peeled off the module's top front, sprung up and out on its last seam.</summary>
        private static void PeeledPanel(SiteRecipe site)
        {
            const float radius = ModuleRadius + Lift + 0.02f;
            const float along = 0.4f;
            Matrix4x4 module = ModuleFrame;
            Vector3 seam = module.MultiplyPoint3x4(Arc(100f) * radius + Vector3.forward * along);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, seam + new Vector3(0f, -0.04f, -0.05f),
                seam + new Vector3(0.1f, 0.05f, -0.05f), new Vector3(0f, 0.6f, -0.8f));
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 peel = At(seam) * Matrix4x4.Rotate(Rotation(new Vector3(65f, 0f, 0f))) * At(-seam);
            MeshRange skin = SiteKit.ArcSheet(b, module * At(0f, 0f, along), radius, 45f, 100f, 2, 1.4f,
                PaletteSwatch.FadedPaint);
            b.Transform(skin, peel);
            Vector3 middle = peel.MultiplyPoint3x4(module.MultiplyPoint3x4(Arc(70f) * (radius + 0.025f)
                + Vector3.forward * along));
            Vector3 normal = peel.MultiplyVector(module.MultiplyVector(Arc(70f)));
            SiteKit.RustPatch(b, SiteKit.Face(middle, normal, Vector3.right), 0f, 0f, 0.5f);
        }

        /// <summary>
        /// The unit radial at an angle around the module (0 faces home, 90 up), in the module's frame.
        /// </summary>
        private static Vector3 Arc(float degrees)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        /// <summary>The snapped leg lying behind the stage: strut, knee, footpad.</summary>
        private static void BrokenLeg(SiteRecipe site)
        {
            var snap = new Vector3(-1.4f, 0.18f, 3.55f);
            var knee = new Vector3(-2.45f, 0.16f, 4.15f);
            var foot = new Vector3(-3.3f, 0.12f, 3.6f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, snap + new Vector3(-0.08f, 0f, 0.04f),
                snap + new Vector3(0.05f, 0.08f, -0.05f), new Vector3(0.6f, 0.4f, -0.7f));
            LowPolyMeshBuilder b = piece.Geometry;
            SiteKit.Bar(b, snap, knee, 0.16f, PaletteSwatch.Metal);
            SiteKit.Bar(b, knee, foot, 0.13f, PaletteSwatch.Metal);
            b.Box(At(knee), new Vector3(0.26f, 0.24f, 0.26f), PaletteSwatch.Rust, 0.03f);
            b.Prism(At(foot + new Vector3(-0.15f, 0.04f, -0.1f), new Vector3(0f, 0f, 12f)), 0.32f, 0.08f, 8,
                PaletteSwatch.Metal);
        }

        /// <summary>The avionics box on the stage's front flat, its cables spilling down into the dust.</summary>
        private static void Avionics(SiteRecipe site)
        {
            Vector3 back = Stage + new Vector3(0.1f, 0.25f, -StageRadius * 0.924f - 0.02f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, back + new Vector3(0f, 0f, -0.08f),
                back + new Vector3(0f, 0.36f, -0.06f), new Vector3(0f, 0.4f, -1f));
            SiteKit.JunctionBox(piece.Geometry, SiteKit.Face(back, Vector3.back, Vector3.up),
                new Vector3(0.62f, 0.58f, 0.26f));
        }

        /// <summary>The docking camera on its stub boom at the module's nose: a hood and a big violet lens.</summary>
        private static void Camera(SiteRecipe site)
        {
            Matrix4x4 module = ModuleFrame;
            Vector3 root = module.MultiplyPoint3x4(new Vector3(0.35f, 0.55f, ModuleLength * 0.5f + 0.3f));
            var facing = new Vector3(0.5f, 0.3f, -0.8f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Optics, root + facing.normalized * 0.1f,
                root + new Vector3(0f, -0.05f, 0f), new Vector3(0.7f, 0.2f, -0.7f));
            LowPolyMeshBuilder b = piece.Geometry;
            Vector3 head = root + facing.normalized * 0.4f;
            RecipeKit.Rod(b, root, head, 0.07f, 6, PaletteSwatch.Metal);
            Matrix4x4 camera = SiteKit.Face(head, facing, Vector3.up);
            b.Box(camera, new Vector3(0.38f, 0.32f, 0.42f), PaletteSwatch.FadedPaint, 0.03f);
            b.Prism(camera * At(new Vector3(0f, 0f, 0.22f), AlongZ), 0.13f, 0.04f, 10, PaletteSwatch.SkyHorizon);
            b.Torus(camera * At(new Vector3(0f, 0f, 0.23f), AlongZ), 0.14f, 0.025f, 10, 3, PaletteSwatch.Metal);
        }

        private static void SpilledCrate(SiteRecipe site)
        {
            var centre = new Vector3(3.6f, 0.38f, -0.9f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre,
                centre + new Vector3(-0.2f, 0.42f, -0.3f),
                new Vector3(-0.3f, 0.6f, -0.7f));
            SiteKit.Crate(piece.Geometry, At(centre, new Vector3(6f, -38f, -9f)), new Vector3(1f, 0.8f, 0.8f));
        }

        /// <summary>A crate tipped open, its cable spools rolled out beside it.</summary>
        private static void Spools(SiteRecipe site)
        {
            var centre = new Vector3(-3.2f, 0.32f, -1.9f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Wiring, centre,
                centre + new Vector3(0.45f, 0.3f, -0.1f),
                new Vector3(0.4f, 0.5f, -0.8f));
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 crate = At(centre, new Vector3(0f, 24f, -62f));
            b.Box(crate * At(0f, -0.3f, 0f), new Vector3(0.8f, 0.05f, 0.6f), PaletteSwatch.FadedPaint);
            b.Box(crate * At(0f, 0f, -0.3f), new Vector3(0.8f, 0.6f, 0.05f), PaletteSwatch.FadedPaint);
            b.Box(crate * At(0f, 0f, 0.3f), new Vector3(0.8f, 0.6f, 0.05f), PaletteSwatch.FadedPaint);
            b.Box(crate * At(-0.4f, 0f, 0f), new Vector3(0.05f, 0.6f, 0.6f), PaletteSwatch.Metal);
            for (int i = 0; i < 3; i++)
            {
                Vector3 at = centre + new Vector3(0.55f + i * 0.32f, -0.12f, -0.25f + i * 0.22f);
                Matrix4x4 spool = At(at, new Vector3(0f, 30f + i * 25f, 90f));
                b.Prism(spool, 0.2f, 0.26f, 8, PaletteSwatch.WarmAccent);
                b.Prism(spool * At(0f, 0.15f, 0f), 0.24f, 0.04f, 8, PaletteSwatch.Charcoal);
                b.Prism(spool * At(0f, -0.15f, 0f), 0.24f, 0.04f, 8, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>The cargo hatch, torn off and lying behind the module: tether it clear before cutting.</summary>
        private static void Hatch(SiteRecipe site)
        {
            var centre = new Vector3(1f, 0.14f, 3.6f);
            Vector3 knuckle = centre + new Vector3(-0.85f, 0.08f, -0.7f);
            SalvagePieceRecipe piece = site.Piece(SalvageMaterial.Metal, centre, knuckle,
                new Vector3(-0.4f, 0.6f, -0.7f),
                true);
            LowPolyMeshBuilder b = piece.Geometry;
            Matrix4x4 plate = At(centre, new Vector3(-6f, 14f, 4f));
            int first = b.TriangleCount;
            b.Box(plate, new Vector3(2f, 0.1f, 1.5f), PaletteSwatch.FadedPaint, 0.03f);
            b.Box(plate * At(0f, 0.06f, 0f), new Vector3(1.6f, 0.03f, 1.1f), PaletteSwatch.Metal);
            SiteKit.DustCap(b, b.RangeFrom(first));
            b.Torus(plate * At(0.2f, 0.12f, 0.1f), 0.24f, 0.03f, 10, 3, PaletteSwatch.Charcoal);
            for (int i = 0; i < 2; i++)
            {
                b.Prism(plate * At(new Vector3(-0.6f + i * 1.2f, 0.05f, -0.8f), AlongX), 0.08f, 0.3f, 6,
                    PaletteSwatch.Rust);
            }

            SiteKit.RustPatch(b, plate * At(new Vector3(-0.5f, 0.08f, 0.35f), new Vector3(-90f, 0f, 0f)), 0f, 0f, 0.5f);
        }
    }
}
