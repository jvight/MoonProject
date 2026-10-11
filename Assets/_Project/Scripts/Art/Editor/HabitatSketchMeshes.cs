using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// A flat-shaded blockout of the habitat lander proposed in the M3-19 draft, for the owner to see before the
    /// design is adopted: sizes from the draft's table, palette swatches only, no weathering. Sketch space: the
    /// origin on the ground under the lander's centre, +Y up, +Z the front where 07 drives in under the belly and
    /// where the dock pad sits on the apron.
    /// </summary>
    internal static class HabitatSketchMeshes
    {
        public const float BellyClear = 2.6f;
        public const float StageHeight = 1f;
        public const float StageRadius = 3.8f;
        public const float FloorY = BellyClear + StageHeight;
        public const float InnerApothem = 3.2f;
        public const float CabinInnerHeight = 2.7f;
        public const float PlatformRadius = 1.7f;
        public const float PlatformThickness = 0.2f;
        public const float FootRadius = 6f;
        public const float DockZ = 7.8f;

        private const float WallThickness = 0.12f;
        private const float RoofRise = 0.6f;
        private const float RailRadius = 1.9f;
        private const float RailInset = 0.4f;
        private const int Sides = 8;

        private static float OuterApothem => InnerApothem + WallThickness;
        private static float OuterRadius => OuterApothem / Mathf.Cos(Mathf.PI / Sides);
        private static float CeilingY => FloorY + CabinInnerHeight;
        private static float CabinTop => CeilingY + 0.2f;

        /// <summary>Where the lift platform's top sits when it is down on the ground (sketch space y).</summary>
        public static float PlatformDownY => PlatformThickness + 0.05f;

        /// <summary>Where the lift platform's top sits when it is up, flush with the cabin floor.</summary>
        public static float PlatformUpY => FloorY + 0.02f;

        /// <summary>The camera ring rail's radius and height (sketch space), inset from the cabin wall.</summary>
        public static float RingRadius => InnerApothem - RailInset;

        public static float RingHeight => CeilingY - 0.25f;

        /// <summary>The descent stage, cabin shell and roof, legs, lift guide rails, ladder and dock pad.</summary>
        public static LowPolyMeshBuilder Hull()
        {
            var b = new LowPolyMeshBuilder(3000);
            b.Prism(At(0f, BellyClear + StageHeight * 0.5f, 0f), StageRadius, StageHeight, Sides,
                PaletteSwatch.WornPanel);
            b.Prism(At(0f, BellyClear + 0.04f, 0f), StageRadius - 0.1f, 0.06f, Sides, PaletteSwatch.Charcoal);
            b.Prism(At(0f, FloorY - 0.12f, 0f), StageRadius + 0.02f, 0.14f, Sides, PaletteSwatch.FadedAccent);
            b.Torus(At(0f, BellyClear - 0.02f, 0f), RailRadius, 0.08f, 16, 4, PaletteSwatch.Charcoal);

            float wall = CabinTop - FloorY;
            b.Prism(At(0f, FloorY + wall * 0.5f, 0f), OuterRadius, wall, Sides, PaletteSwatch.Enamel);
            b.Prism(At(0f, CabinTop - 0.35f, 0f), OuterRadius + 0.012f, 0.18f, Sides, PaletteSwatch.WarmAccent);
            b.Frustum(At(0f, CabinTop + RoofRise * 0.5f, 0f), OuterRadius, 1.6f, RoofRise, Sides,
                PaletteSwatch.WornPanel);
            b.Box(At(0.9f, CabinTop + RoofRise + 0.12f, -0.4f), new Vector3(0.9f, 0.24f, 0.9f), PaletteSwatch.Metal,
                0.03f);
            RecipeKit.Rod(b, new Vector3(-0.6f, CabinTop + RoofRise, 0.3f),
                new Vector3(-0.6f, CabinTop + RoofRise + 0.7f, 0.3f), 0.05f, 6, PaletteSwatch.Metal);
            SiteKit.Dish(b, At(new Vector3(-0.6f, CabinTop + RoofRise + 0.7f, 0.3f), new Vector3(-35f, 30f, 0f)),
                0.45f, PaletteSwatch.WornPanel);

            InnerShell(b);
            Legs(b);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle) * RailRadius, 0f, Mathf.Cos(angle) * RailRadius);
                SiteKit.Bar(b, at, at + Vector3.up * BellyClear, 0.1f, PaletteSwatch.Metal);
            }

            Ladder(b);
            DockPad(b);
            return b;
        }

        /// <summary>The cabin's insides: walls, ceiling, the four crew corners, the map table, the ring rail.</summary>
        private static void InnerShell(LowPolyMeshBuilder b)
        {
            float halfSide = InnerApothem * Mathf.Tan(Mathf.PI / Sides);
            var ceiling = new Vector3[Sides];
            for (int side = 0; side < Sides; side++)
            {
                Vector3 n = Normal(side);
                Vector3 t = Tangent(side);
                Vector3 centre = n * InnerApothem;
                b.Polygon(new[]
                {
                    centre - t * halfSide + Vector3.up * FloorY, centre + t * halfSide + Vector3.up * FloorY,
                    centre + t * halfSide + Vector3.up * CeilingY, centre - t * halfSide + Vector3.up * CeilingY,
                }, -n, side % 2 == 0 ? PaletteSwatch.WornPanel : PaletteSwatch.Wood);
                ceiling[side] = centre - t * halfSide + Vector3.up * CeilingY;
            }

            b.Polygon(ceiling, Vector3.down, PaletteSwatch.WornPanel);
            b.Torus(At(0f, RingHeight, 0f), RingRadius, 0.035f, 24, 3, PaletteSwatch.Metal);
            b.Torus(At(0f, FloorY + 0.005f, 0f), RailRadius, 0.06f, 16, 3, PaletteSwatch.Charcoal);
            for (int corner = 0; corner < 4; corner++)
            {
                CrewCorner(b, corner * 2 + 1);
            }

            MapStation(b, 2);
        }

        /// <summary>A crew corner on a diagonal wall: the keepsake cubby at 07's lens height, the bunk over it.</summary>
        private static void CrewCorner(LowPolyMeshBuilder b, int side)
        {
            b.Box(Wall(side, -0.35f, 1.25f, 0.3f), new Vector3(1.5f, 0.75f, 0.6f), PaletteSwatch.Charcoal, 0.02f);
            for (int k = 0; k < 3; k++)
            {
                b.Box(Wall(side, -0.35f - 0.75f + k * 0.75f, 1.25f, 0.28f), new Vector3(0.05f, 0.75f, 0.62f),
                    PaletteSwatch.WornPanel);
            }

            b.Box(Wall(side, 0f, 1.95f, 0.38f), new Vector3(2.4f, 0.18f, 0.76f), PaletteSwatch.WornPanel, 0.02f);
            b.Box(Wall(side, 0.1f, 2.08f, 0.38f), new Vector3(2.0f, 0.1f, 0.66f), PaletteSwatch.FadedAccent, 0.03f);
            b.Torus(Wall(side, 0.85f, 1.2f, -0.06f) * Matrix4x4.Rotate(Rotation(new Vector3(90f, 0f, 0f))), 0.32f,
                0.05f, 10, 3, PaletteSwatch.Charcoal);
        }

        /// <summary>Ines's map station: the light table under the cabin's biggest porthole.</summary>
        private static void MapStation(LowPolyMeshBuilder b, int side)
        {
            b.Box(Wall(side, 0f, 0.95f, 0.45f) * Matrix4x4.Rotate(Rotation(new Vector3(-35f, 0f, 0f))),
                new Vector3(1.3f, 0.06f, 0.8f), PaletteSwatch.WornPanel, 0.02f);
            for (int x = -1; x <= 1; x += 2)
            {
                b.Box(Wall(side, x * 0.55f, 0.45f, 0.45f), new Vector3(0.06f, 0.9f, 0.06f), PaletteSwatch.Charcoal);
            }

            b.Torus(Wall(side, 0f, 1.75f, -0.06f) * Matrix4x4.Rotate(Rotation(new Vector3(90f, 0f, 0f))), 0.62f,
                0.07f, 14, 3, PaletteSwatch.Charcoal);
        }

        /// <summary>The glass of every porthole (crew corners and the big one), dark until lit.</summary>
        public static LowPolyMeshBuilder Portholes()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int corner = 0; corner < 4; corner++)
            {
                Glass(b, corner * 2 + 1, 0.85f, 1.2f, 0.3f);
            }

            Glass(b, 2, 0f, 1.75f, 0.6f);
            return b;
        }

        /// <summary>A reading lamp over each crew corner's keepsake cubby.</summary>
        public static LowPolyMeshBuilder CornerLamps()
        {
            var b = new LowPolyMeshBuilder(120);
            for (int corner = 0; corner < 4; corner++)
            {
                b.Icosphere(Wall(corner * 2 + 1, -0.35f, 1.72f, 0.2f), 0.07f, 1, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>The light table's lit panel, under where the map will be pinned.</summary>
        public static LowPolyMeshBuilder LightTableGlow()
        {
            var b = new LowPolyMeshBuilder(12);
            b.Box(Wall(2, 0f, 0.99f, 0.45f) * Matrix4x4.Rotate(Rotation(new Vector3(-35f, 0f, 0f))),
                new Vector3(1.1f, 0.02f, 0.62f), PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>The round lift platform (platform space: the origin at the middle of its top).</summary>
        public static LowPolyMeshBuilder Platform()
        {
            var b = new LowPolyMeshBuilder(200);
            b.Prism(At(0f, -PlatformThickness * 0.5f, 0f), PlatformRadius, PlatformThickness, 16,
                PaletteSwatch.WornPanel);
            b.Torus(At(0f, 0.005f, 0f), PlatformRadius - 0.12f, 0.04f, 16, 3, PaletteSwatch.FadedAccent);
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (RailRadius - 0.12f);
                b.Box(At(at + Vector3.down * 0.1f), new Vector3(0.22f, 0.22f, 0.22f), PaletteSwatch.Charcoal, 0.02f);
            }

            return b;
        }

        /// <summary>The platform's rim lamps, at rover height on the rim (platform space).</summary>
        public static LowPolyMeshBuilder RimLamps()
        {
            var b = new LowPolyMeshBuilder(80);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                var at = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (PlatformRadius - 0.04f);
                b.Icosphere(At(at + Vector3.up * 0.04f), 0.05f, 1, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>The floor hatch's two closed leaves under the belly (sketch space).</summary>
        public static LowPolyMeshBuilder FloorHatch()
        {
            var b = new LowPolyMeshBuilder(40);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * RailRadius * 0.5f, BellyClear - 0.03f, 0f), new Vector3(RailRadius - 0.04f, 0.05f,
                    RailRadius * 2f), PaletteSwatch.Metal, 0.01f);
            }

            return b;
        }

        /// <summary>The dock pad's lamp strip (sketch space).</summary>
        public static LowPolyMeshBuilder DockGlow()
        {
            var b = new LowPolyMeshBuilder(12);
            b.Box(At(0f, 0.16f, DockZ + 1.75f), new Vector3(1.6f, 0.03f, 0.08f), PaletteSwatch.LampGlass);
            return b;
        }

        private static void Legs(LowPolyMeshBuilder b)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                var out1 = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var side = new Vector3(out1.z, 0f, -out1.x);
                Vector3 hip = out1 * (StageRadius - 0.4f) + Vector3.up * (BellyClear + 0.5f);
                Vector3 foot = out1 * FootRadius + Vector3.up * 0.3f;
                RecipeKit.Strut(b, hip, foot, new Vector2(0.18f, 0.18f), PaletteSwatch.Metal);
                Vector3 knee = Vector3.Lerp(hip, foot, 0.55f);
                RecipeKit.Strut(b, out1 * 2.4f + side * 0.9f + Vector3.up * BellyClear, knee, new Vector2(0.1f, 0.1f),
                    PaletteSwatch.Metal);
                RecipeKit.Strut(b, out1 * 2.4f - side * 0.9f + Vector3.up * BellyClear, knee, new Vector2(0.1f, 0.1f),
                    PaletteSwatch.Metal);
                b.Prism(At(foot + Vector3.down * 0.18f), 0.55f, 0.24f, 8, PaletteSwatch.WornPanel);
            }
        }

        /// <summary>The crew's ladder up the front-left leg to the stage, then up the cabin wall to the roof.</summary>
        private static void Ladder(LowPolyMeshBuilder b)
        {
            float angle = -45f * Mathf.Deg2Rad;
            var out1 = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            var side = new Vector3(out1.z, 0f, -out1.x);
            Vector3 low = out1 * (FootRadius - 0.6f) + Vector3.up * 0.05f;
            Vector3 high = out1 * (StageRadius + 0.15f) + Vector3.up * FloorY;
            Rungs(b, low, high, side);
            Vector3 wallFoot = Normal(7) * (OuterApothem + 0.12f) + Vector3.up * FloorY;
            Rungs(b, wallFoot, wallFoot + Vector3.up * (CabinTop + RoofRise - FloorY), Tangent(7));
        }

        private static void Rungs(LowPolyMeshBuilder b, Vector3 from, Vector3 to, Vector3 across)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                SiteKit.Bar(b, from + across * (s * 0.22f), to + across * (s * 0.22f), 0.05f, PaletteSwatch.Metal);
            }

            int rungs = Mathf.RoundToInt(Vector3.Distance(from, to) / 0.3f);
            for (int r = 1; r < rungs; r++)
            {
                Vector3 at = Vector3.Lerp(from, to, (float)r / rungs);
                SiteKit.Bar(b, at - across * 0.22f, at + across * 0.22f, 0.035f, PaletteSwatch.Metal);
            }
        }

        private static void DockPad(LowPolyMeshBuilder b)
        {
            b.Box(At(0f, 0.07f, DockZ), new Vector3(2.6f, 0.14f, 3.4f), PaletteSwatch.WornPanel, 0.02f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * 0.7f, 0.145f, DockZ), new Vector3(0.12f, 0.01f, 3f), PaletteSwatch.FadedAccent);
            }

            b.Box(At(0f, 0.55f, DockZ + 1.95f), new Vector3(0.3f, 1.1f, 0.24f), PaletteSwatch.WornPanel, 0.03f);
            b.Box(At(0f, 0.95f, DockZ + 1.8f), new Vector3(0.5f, 0.2f, 0.12f), PaletteSwatch.Charcoal, 0.02f);
        }

        private static void Glass(LowPolyMeshBuilder b, int side, float u, float h, float radius)
        {
            Matrix4x4 frame = Wall(side, u, h, -WallThickness * 0.5f) * Matrix4x4.Rotate(Rotation(AlongZ));
            b.Prism(frame, radius, WallThickness + 0.04f, 12, PaletteSwatch.LampGlass);
        }

        /// <summary>
        /// A frame on the inside of wall <paramref name="side"/> (0 = +Z, clockwise seen from above): +X along the
        /// wall, +Y up, +Z out of the wall into the cabin; <paramref name="u"/> along it, <paramref name="h"/> above
        /// the floor, <paramref name="d"/> out from the wall.
        /// </summary>
        private static Matrix4x4 Wall(int side, float u, float h, float d)
        {
            Vector3 n = Normal(side);
            Vector3 origin = n * (InnerApothem - d) + Tangent(side) * u + Vector3.up * (FloorY + h);
            return SiteKit.Face(origin, -n, Vector3.up);
        }

        private static Vector3 Normal(int side)
        {
            float angle = side * Mathf.PI * 2f / Sides;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        private static Vector3 Tangent(int side)
        {
            Vector3 n = Normal(side);
            return new Vector3(n.z, 0f, -n.x);
        }
    }
}
