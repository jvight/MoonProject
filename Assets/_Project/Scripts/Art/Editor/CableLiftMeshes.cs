using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The crew's cable lift beside the ladder, built so 07 could reach the hatch deck (VISION ruling 12): two posts
    /// braced back to the deck, a winch drum under their crossbeam, and a platform sized for 07 (railed on both sides,
    /// a carriage at the back that rolls up the posts, open at the front with a ramp lip). It has hung jammed halfway
    /// for decades, a loop of cable snarled off the drum. The posts and the winch belong to the lander hull; the
    /// platform and the two hoist cables are their own meshes so gameplay can run the lift. Base space; the platform
    /// mesh is in its own space (origin on the middle of its deck, +Z towards its open front, the lander's +Z).
    /// </summary>
    internal static class CableLiftMeshes
    {
        public const float PlatformWidth = 1.8f;
        public const float PlatformLength = 2.7f;

        /// <summary>The platform's deck top at its lowest: its underside just clear of the dust.</summary>
        public const float BottomStop = 0.2f;

        /// <summary>The platform's deck top at its highest: level with the lander's deck.</summary>
        public const float TopStop = LanderMeshes.DeckTop;

        /// <summary>Height of the carriage's top bar over the deck, where the hoist cables hook on.</summary>
        public const float CarriageHeight = 1f;

        /// <summary>Height of the winch drum's axle: the hoist cables hang from it.</summary>
        public const float DrumHeight = 4.28f;

        /// <summary>Where 07 stands on the platform (platform space): centred, facing the lander (-Z).</summary>
        public static readonly Vector3 RoverSpot = new Vector3(0f, 0f, 0.05f);

        public static readonly Vector3 RoverSpotEuler = new Vector3(0f, 180f, 0f);

        private const float PostThickness = 0.14f;
        private const float PostHeight = 4.6f;
        private const float DrumRadius = 0.16f;
        private const float RailHeight = 0.65f;
        private const float CarriageHalfWidth = 0.84f;

        // The posts stand just outside the platform's back corners; the carriage rolls up their front faces.
        private static readonly float PostHalfSpan = PlatformWidth * 0.5f + PostThickness * 0.5f;
        private static readonly float PostZ = -PlatformLength * 0.5f - 0.1f;
        private static readonly float CarriageZ = -PlatformLength * 0.5f + 0.06f;

        private static readonly float[] RailPosts = { -1.2f, -0.05f, 1.1f };

        /// <summary>The middle of the platform's footprint in base space.</summary>
        private static readonly Vector3 Centre = new Vector3(2f, 0f, 4.45f);

        /// <summary>The platform's place at its lowest stop (base space).</summary>
        public static Vector3 Bottom => new Vector3(Centre.x, BottomStop, Centre.z);

        /// <summary>The platform's place at its highest stop (base space).</summary>
        public static Vector3 Top => new Vector3(Centre.x, TopStop, Centre.z);

        /// <summary>Where the platform has hung jammed for decades: halfway up.</summary>
        public static Vector3 Jammed => Vector3.Lerp(Bottom, Top, 0.5f);

        /// <summary>
        /// Where hoist cable <paramref name="side"/> (-1 left, +1 right) leaves the drum (base space): it hangs
        /// straight down from here to the carriage's top bar.
        /// </summary>
        public static Vector3 CableTop(int side)
        {
            return new Vector3(Centre.x + side * CarriageHalfWidth, DrumHeight, Centre.z + CarriageZ);
        }

        /// <summary>The length of each hoist cable with the platform at <paramref name="platform"/>'s height.</summary>
        public static float CableLength(Vector3 platform)
        {
            return DrumHeight - (platform.y + CarriageHeight);
        }

        /// <summary>The fixed part (base space): posts, crossbeam, braces to the deck, the winch, its snarl.</summary>
        public static void Frame(LowPolyMeshBuilder b)
        {
            float z = Centre.z + PostZ;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = Centre.x + side * PostHalfSpan;
                SiteKit.Bar(b, new Vector3(x, PostThickness * 0.5f, z),
                    new Vector3(x, PostHeight - PostThickness * 0.5f, z), PostThickness, PaletteSwatch.FadedPaint);
                b.Box(At(x, 0.02f, z - 0.05f), new Vector3(0.36f, 0.04f, 0.26f), PaletteSwatch.Metal, 0.01f);
            }

            float left = Centre.x - PostHalfSpan;
            float right = Centre.x + PostHalfSpan;
            float beam = PostHeight - PostThickness * 0.5f;
            SiteKit.Bar(b, new Vector3(left, beam, z), new Vector3(right, beam, z), PostThickness, PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(left, 3.9f, z - 0.04f), new Vector3(0.55f, LanderMeshes.DeckTop + 0.03f, 1.97f),
                0.08f, PaletteSwatch.Metal);
            SiteKit.Bar(b, new Vector3(right, 3.9f, z - 0.04f), new Vector3(1.75f, LanderMeshes.DeckTop + 0.03f, 1.05f),
                0.08f, PaletteSwatch.Metal);
            Winch(b, z);
        }

        /// <summary>The platform in its own space (see the class summary).</summary>
        public static LowPolyMeshBuilder Platform()
        {
            var b = new LowPolyMeshBuilder(900);
            float halfWidth = PlatformWidth * 0.5f;
            float halfLength = PlatformLength * 0.5f;
            b.Box(At(0f, -0.04f, 0f), new Vector3(PlatformWidth, 0.08f, PlatformLength), PaletteSwatch.Metal, 0.015f);
            b.Wedge(At(0f, -BottomStop * 0.5f, halfLength + 0.225f),
                new Vector3(PlatformWidth - 0.1f, BottomStop, 0.45f), PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (halfWidth - 0.04f), -0.13f, 0f), new Vector3(0.08f, 0.1f, PlatformLength),
                    PaletteSwatch.FadedPaint);
                b.Box(At(side * 0.64f, 0.003f, 0.1f), new Vector3(0.1f, 0.006f, PlatformLength - 0.5f),
                    PaletteSwatch.FadedAccent);
                Railing(b, side * (halfWidth - 0.04f));
                Carriage(b, side);
            }

            for (int i = -1; i <= 1; i++)
            {
                b.Box(At(0f, -0.12f, i * 1f), new Vector3(PlatformWidth - 0.16f, 0.08f, 0.08f), PaletteSwatch.Metal);
            }

            var barLeft = new Vector3(-CarriageHalfWidth, CarriageHeight, CarriageZ);
            var barRight = new Vector3(CarriageHalfWidth, CarriageHeight, CarriageZ);
            SiteKit.Bar(b, barLeft, barRight, 0.06f, PaletteSwatch.FadedPaint);
            RecipeKit.Rod(b, new Vector3(-CarriageHalfWidth, 0.5f, CarriageZ), new Vector3(CarriageHalfWidth, 0.5f,
                CarriageZ), 0.025f, 6, PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>A hoist cable in its own space: from the drum at the origin straight down by its length.</summary>
        public static LowPolyMeshBuilder Cable(float length)
        {
            var b = new LowPolyMeshBuilder(40);
            RecipeKit.Rod(b, Vector3.zero, Vector3.down * length, 0.014f, 6, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The fixed part's rust: streaks down the posts' outer faces (their front faces carry the rollers), collars
        /// at their feet and a patch on the motor housing.
        /// </summary>
        public static void FrameRust(LowPolyMeshBuilder b)
        {
            float z = Centre.z + PostZ;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = Centre.x + side * PostHalfSpan;
                Matrix4x4 post = SiteKit.Face(new Vector3(x + side * (PostThickness * 0.5f + Weathering.RustLift), 0f,
                    z), Vector3.right * side, Vector3.up);
                SiteKit.RustStreak(b, post, 0f, PostHeight - 0.25f, side < 0 ? 0.7f : 0.45f, 0.04f);
                Weathering.Collar(b, At(new Vector3(x, 0.12f, z)), 0.12f, 0.16f);
            }

            Matrix4x4 motor = SiteKit.Face(new Vector3(Centre.x + PostHalfSpan + 0.25f, 0f,
                Centre.z + PostZ + 0.2f + Weathering.RustLift), Vector3.forward, Vector3.up);
            SiteKit.RustPatch(b, motor, 0.05f, DrumHeight - 0.05f, 0.12f);
        }

        /// <summary>The platform's rust (platform space): streaks down its side beams and its carriage.</summary>
        public static LowPolyMeshBuilder PlatformRust()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int side = -1; side <= 1; side += 2)
            {
                Matrix4x4 beam = SiteKit.Face(new Vector3(side * (PlatformWidth * 0.5f + Weathering.RustLift), 0f, 0f),
                    Vector3.right * side, Vector3.up);
                SiteKit.RustStreak(b, beam, 0.6f, -0.09f, 0.06f, 0.03f);
                SiteKit.RustPatch(b, beam, -0.7f, -0.13f, 0.08f);
                Matrix4x4 upright = SiteKit.Face(new Vector3(side * CarriageHalfWidth, 0f,
                    CarriageZ + 0.03f + Weathering.RustLift), Vector3.forward, Vector3.up);
                SiteKit.RustStreak(b, upright, 0f, CarriageHeight - 0.08f, 0.35f, 0.03f);
            }

            return b;
        }

        /// <summary>Dust on the platform's top faces and a small drift against the carriage (platform space).</summary>
        public static LowPolyMeshBuilder PlatformDust(LowPolyMeshBuilder platform)
        {
            LowPolyMeshBuilder dust = Weathering.Dust(platform, 0f, Weathering.DustLift);
            SiteKit.Drift(dust, new Vector3(-0.45f, 0f, CarriageZ + 0.25f), 0.8f, 0.35f, 0.07f, 90f, 121);
            return dust;
        }

        /// <summary>A side's railing: three posts with a top rail in faded orange and a mid rail.</summary>
        private static void Railing(LowPolyMeshBuilder b, float x)
        {
            for (int i = 0; i < RailPosts.Length; i++)
            {
                RecipeKit.Rod(b, new Vector3(x, 0f, RailPosts[i]), new Vector3(x, RailHeight, RailPosts[i]), 0.025f, 6,
                    PaletteSwatch.Metal);
            }

            float back = RailPosts[0];
            float front = RailPosts[RailPosts.Length - 1];
            RecipeKit.Rod(b, new Vector3(x, RailHeight, back), new Vector3(x, RailHeight, front), 0.025f, 6,
                PaletteSwatch.FadedAccent);
            RecipeKit.Rod(b, new Vector3(x, RailHeight * 0.5f, back), new Vector3(x, RailHeight * 0.5f, front), 0.018f,
                6, PaletteSwatch.Metal);
            RecipeKit.Rod(b, new Vector3(x, RailHeight, front), new Vector3(Mathf.Sign(x) * CarriageHalfWidth,
                CarriageHeight, CarriageZ), 0.012f, 6, PaletteSwatch.Charcoal);
        }

        /// <summary>
        /// A side of the carriage: an upright from the deck's back corner to the top bar, a shackle where the hoist
        /// cable hooks on, and two guide rollers bearing on the post's front face.
        /// </summary>
        private static void Carriage(LowPolyMeshBuilder b, int side)
        {
            float x = side * CarriageHalfWidth;
            SiteKit.Bar(b, new Vector3(x, 0.03f, CarriageZ), new Vector3(x, CarriageHeight, CarriageZ), 0.06f,
                PaletteSwatch.FadedPaint);
            b.Torus(At(new Vector3(x, CarriageHeight + 0.06f, CarriageZ), AlongX), 0.04f, 0.012f, 6, 3,
                PaletteSwatch.Charcoal);
            float post = side * PostHalfSpan;
            float roller = PostZ + PostThickness * 0.5f + 0.03f;
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? 0.2f : 0.85f;
                b.Box(At(side * (CarriageHalfWidth + 0.04f), y, (roller + CarriageZ) * 0.5f),
                    new Vector3(0.14f, 0.03f, CarriageZ - roller + 0.03f), PaletteSwatch.Metal);
                b.Prism(At(new Vector3(post, y, roller), AlongX), 0.025f, 0.1f, 8, PaletteSwatch.Charcoal);
            }
        }

        /// <summary>
        /// The winch: a drum on the posts' line under the crossbeam, its motor housing outside the right post, the
        /// two cables wound at its ends and a snarled loop hanging off the back of the left wrap (behind the posts,
        /// clear of the carriage's travel).
        /// </summary>
        private static void Winch(LowPolyMeshBuilder b, float z)
        {
            var drum = new Vector3(Centre.x, DrumHeight, z);
            float length = 2f * (PostHalfSpan - PostThickness * 0.5f) - 0.02f;
            b.Prism(At(drum, AlongX), DrumRadius, length, 10, PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(drum + Vector3.right * (side * (length * 0.5f - 0.02f)), AlongX), DrumRadius + 0.05f, 0.03f,
                    10, PaletteSwatch.FadedPaint);
                for (int i = 0; i < 3; i++)
                {
                    float x = side * (CarriageHalfWidth - 0.08f + i * 0.04f);
                    b.Torus(At(drum + Vector3.right * x, AlongX), DrumRadius + 0.012f, 0.014f, 10, 3,
                        PaletteSwatch.Charcoal);
                }
            }

            var motor = new Vector3(Centre.x + PostHalfSpan + 0.25f, DrumHeight, z);
            b.Box(At(motor), new Vector3(0.36f, 0.4f, 0.4f), PaletteSwatch.FadedPaint, 0.03f);
            b.Box(At(motor + new Vector3(0.19f, 0f, 0f)), new Vector3(0.02f, 0.24f, 0.26f), PaletteSwatch.Charcoal);
            SiteKit.Cable(b, SiteKit.Sag(drum + new Vector3(-0.66f, -0.1f, -DrumRadius),
                drum + new Vector3(-0.38f, -0.12f, -DrumRadius - 0.02f), 0.75f, 6), 0.014f, PaletteSwatch.Charcoal);
        }
    }
}
