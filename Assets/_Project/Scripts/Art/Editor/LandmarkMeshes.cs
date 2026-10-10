using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The basin's crew remnants that World places (M3-18 L2, L4, L7), each in its own space with the origin on the
    /// ground, +Y up, +Z forward: the route post on the crew's road to the canyon, the broken cable bridge over the
    /// chasm (near anchors, far anchor, the snapped cable in the trough) and the antenna farm's toppled dish frames.
    /// </summary>
    public static class LandmarkMeshes
    {
        /// <summary>The route post's height: rover height, so 07's lamp finds the reflector on the way.</summary>
        public const float PostHeight = 1.35f;

        /// <summary>Where the post's reflector sits (post space), facing +Z and -Z along the road.</summary>
        public static readonly Vector3 Reflector = new Vector3(0f, PostHeight - 0.18f, 0f);

        /// <summary>The near lip's two anchor posts stand this far apart (metres, along X).</summary>
        public const float AnchorSpan = 2.4f;

        /// <summary>The anchors' height above the lip.</summary>
        public const float AnchorHeight = 2.6f;

        /// <summary>Where the far anchor's lamp hangs (far anchor space).</summary>
        public static readonly Vector3 FarLamp = new Vector3(0f, AnchorHeight + 0.12f, -0.38f);

        /// <summary>The snapped cable's length as it lies in the trough (along X).</summary>
        public const float TroughCableLength = 14f;

        /// <summary>A toppled dish's radius.</summary>
        public const float DishRadius = 3.2f;

        /// <summary>How many toppled dish frame variants there are.</summary>
        public const int DishVariants = 3;

        private const float PostSide = 0.16f;
        private const float AnchorSide = 0.32f;
        private const float CableRadius = 0.035f;
        private const float DrapeDepth = 3.2f;
        private const float LampRadius = 0.12f;

        /// <summary>The route post: a square post in worn paint with a faded orange band, a rusted foot.</summary>
        public static LowPolyMeshBuilder RoutePost()
        {
            var b = new LowPolyMeshBuilder(160);
            b.Box(At(0f, PostHeight * 0.5f, 0f), new Vector3(PostSide, PostHeight, PostSide), PaletteSwatch.WornPanel,
                0.015f);
            b.Box(At(0f, PostHeight - 0.42f, 0f), new Vector3(PostSide + 0.01f, 0.12f, PostSide + 0.01f),
                PaletteSwatch.FadedAccent);
            b.Box(At(0f, 0.06f, 0f), new Vector3(PostSide + 0.06f, 0.12f, PostSide + 0.06f), PaletteSwatch.Rust,
                0.01f);
            Matrix4x4 face = SiteKit.Face(new Vector3(0f, 0f, PostSide * 0.5f), Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, face, 0.03f, PostHeight - 0.5f, 0.35f);
            return b;
        }

        /// <summary>The reflector: a dark glass disc on each face along the road (centred on its node).</summary>
        public static LowPolyMeshBuilder RouteReflector()
        {
            var b = new LowPolyMeshBuilder(40);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Prism(At(new Vector3(0f, 0f, side * (PostSide * 0.5f + 0.01f)), AlongZ), 0.05f, 0.02f, 8,
                    PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>
        /// The near lip's two anchor posts, braced back to deadmen in the dust, their cables snapped and hanging over
        /// the lip (+Z is across the chasm; the lip runs along X at z = 0.6).
        /// </summary>
        public static LowPolyMeshBuilder NearAnchors()
        {
            var b = new LowPolyMeshBuilder(700);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * AnchorSpan * 0.5f;
                Anchor(b, new Vector3(x, 0f, 0f), -1f);
                Vector3 top = new Vector3(x, AnchorHeight - 0.15f, AnchorSide * 0.5f);
                var lip = new Vector3(x + side * 0.1f, 0.05f, 0.65f);
                float hang = side < 0 ? DrapeDepth : DrapeDepth * 0.6f;
                SiteKit.Cable(b, SiteKit.Sag(top, lip, 0.25f, 4), CableRadius, PaletteSwatch.Charcoal);
                SiteKit.Cable(b, new[] { lip, lip + new Vector3(0.05f, -hang, 0.15f) }, CableRadius,
                    PaletteSwatch.Charcoal);
                Frayed(b, lip + new Vector3(0.05f, -hang, 0.15f));
            }

            SiteKit.Bar(b, new Vector3(-AnchorSpan * 0.5f, AnchorHeight - 0.4f, 0f),
                new Vector3(AnchorSpan * 0.5f, AnchorHeight - 0.4f, 0f), 0.1f, PaletteSwatch.WornPanel);
            return b;
        }

        /// <summary>
        /// The far lip's anchor, still standing, its lamp on a bracket reaching out over the chasm and its cable end
        /// hanging back toward the basin (-Z).
        /// </summary>
        public static LowPolyMeshBuilder FarAnchor()
        {
            var b = new LowPolyMeshBuilder(400);
            Anchor(b, Vector3.zero, 1f);
            var bracket = new Vector3(0f, AnchorHeight + 0.22f, 0f);
            SiteKit.Bar(b, new Vector3(0f, AnchorHeight - 0.1f, 0f), bracket, 0.06f, PaletteSwatch.WornPanel);
            SiteKit.Bar(b, bracket, FarLamp + new Vector3(0f, 0.1f, 0f), 0.05f, PaletteSwatch.WornPanel);
            Vector3 top = new Vector3(0f, AnchorHeight - 0.15f, -AnchorSide * 0.5f);
            var lip = new Vector3(0.1f, 0.05f, -0.65f);
            SiteKit.Cable(b, SiteKit.Sag(top, lip, 0.2f, 4), CableRadius, PaletteSwatch.Charcoal);
            SiteKit.Cable(b, new[] { lip, lip + new Vector3(0f, -DrapeDepth * 0.8f, -0.1f) }, CableRadius,
                PaletteSwatch.Charcoal);
            Frayed(b, lip + new Vector3(0f, -DrapeDepth * 0.8f, -0.1f));
            return b;
        }

        /// <summary>The far anchor's lamp: dark glass under a little hood (centred on its node).</summary>
        public static LowPolyMeshBuilder FarAnchorLamp()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Icosphere(Matrix4x4.identity, LampRadius, 1, PaletteSwatch.LampGlass);
            b.Frustum(At(0f, LampRadius * 0.6f, 0f), LampRadius * 1.5f, LampRadius * 0.5f, 0.08f, 8,
                PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The snapped cable lying along the chasm's trough (along X), slack loops and two deck planks still clamped
        /// to it, both ends frayed.
        /// </summary>
        public static LowPolyMeshBuilder TroughCable()
        {
            var b = new LowPolyMeshBuilder(500);
            const int points = 9;
            var run = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                run[i] = new Vector3((t - 0.5f) * TroughCableLength, 0.04f + 0.08f * Mathf.Abs(Mathf.Sin(t * 9f)),
                    Mathf.Sin(t * 5.3f) * 0.9f);
            }

            SiteKit.Cable(b, run, CableRadius, PaletteSwatch.Charcoal);
            for (int plank = 0; plank < 2; plank++)
            {
                Vector3 at = Vector3.Lerp(run[2 + plank * 3], run[3 + plank * 3], 0.5f);
                b.Box(At(at + new Vector3(0f, 0.03f, 0f), new Vector3(0f, 25f + plank * 40f, 6f)),
                    new Vector3(0.35f, 0.05f, 1.6f), PaletteSwatch.WornPanel, 0.01f);
            }

            Frayed(b, run[0]);
            Frayed(b, run[points - 1]);
            return b;
        }

        /// <summary>
        /// A toppled dish frame of the antenna farm: the lattice backing of a <see cref="DishRadius"/> dish lying
        /// tipped on its rim, its pedestal snapped. Variants turn the tip and how much of the frame is left.
        /// </summary>
        public static LowPolyMeshBuilder DishFrame(int variant)
        {
            if (variant < 0 || variant >= DishVariants)
            {
                throw new System.ArgumentOutOfRangeException(nameof(variant));
            }

            var b = new LowPolyMeshBuilder(1200);
            float tip = 62f + variant * 9f;
            Matrix4x4 frame = At(new Vector3(0f, DishRadius * Mathf.Sin(tip * Mathf.Deg2Rad) * 0.42f, 0f),
                new Vector3(tip, variant * 37f, variant == 1 ? 8f : -5f));
            int spokes = variant == 2 ? 5 : 8;
            for (int s = 0; s < spokes; s++)
            {
                float angle = s * Mathf.PI * 2f / 8f;
                var rim = new Vector3(Mathf.Cos(angle) * DishRadius, 0.55f, Mathf.Sin(angle) * DishRadius);
                SiteKit.Bar(b, frame.MultiplyPoint3x4(Vector3.zero), frame.MultiplyPoint3x4(rim), 0.08f,
                    PaletteSwatch.Metal);
            }

            int rimSegments = variant == 2 ? 5 : 8;
            for (int s = 0; s < rimSegments; s++)
            {
                float a0 = s * Mathf.PI * 2f / 8f;
                float a1 = (s + 1) * Mathf.PI * 2f / 8f;
                var p0 = new Vector3(Mathf.Cos(a0) * DishRadius, 0.55f, Mathf.Sin(a0) * DishRadius);
                var p1 = new Vector3(Mathf.Cos(a1) * DishRadius, 0.55f, Mathf.Sin(a1) * DishRadius);
                SiteKit.Bar(b, frame.MultiplyPoint3x4(p0), frame.MultiplyPoint3x4(p1), 0.09f,
                    s % 3 == 0 ? PaletteSwatch.FadedAccent : PaletteSwatch.Metal);
                SiteKit.Bar(b, frame.MultiplyPoint3x4(p0 * 0.55f), frame.MultiplyPoint3x4(p1 * 0.55f), 0.06f,
                    PaletteSwatch.Metal);
            }

            b.Prism(frame * At(0f, -0.15f, 0f), 0.45f, 0.3f, 8, PaletteSwatch.WornPanel);
            var pedestal = new Vector3(DishRadius * 0.9f, 0f, -1.2f);
            b.Prism(At(pedestal + new Vector3(0f, 0.55f, 0f)), 0.3f, 1.1f, 8, PaletteSwatch.WornPanel);
            b.Frustum(At(pedestal + new Vector3(0f, 1.15f, 0f), new Vector3(0f, 0f, 14f)), 0.3f, 0.18f, 0.12f, 6,
                PaletteSwatch.Rust);
            return b;
        }

        /// <summary>
        /// An anchor post with a back stay to a deadman block in the dust, on the side away from the chasm
        /// (<paramref name="back"/>: -1 for -Z, +1 for +Z).
        /// </summary>
        private static void Anchor(LowPolyMeshBuilder b, Vector3 foot, float back)
        {
            b.Box(At(foot + new Vector3(0f, AnchorHeight * 0.5f, 0f)),
                new Vector3(AnchorSide, AnchorHeight, AnchorSide), PaletteSwatch.WornPanel, 0.03f);
            b.Box(At(foot + new Vector3(0f, 0.12f, 0f)), new Vector3(AnchorSide + 0.2f, 0.24f, AnchorSide + 0.2f),
                PaletteSwatch.WornPanel, 0.03f);
            var deadman = foot + new Vector3(0f, 0.1f, 2.2f * back);
            b.Box(At(deadman), new Vector3(0.7f, 0.2f, 0.4f), PaletteSwatch.WornPanel, 0.02f);
            SiteKit.Bar(b, foot + new Vector3(0f, AnchorHeight - 0.3f, 0f), deadman, 0.05f, PaletteSwatch.Charcoal);
            Matrix4x4 face = SiteKit.Face(foot + new Vector3(0f, 0f, AnchorSide * 0.5f), Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, face, 0.05f, AnchorHeight - 0.4f, 0.6f);
        }

        /// <summary>A cable's broken end: three rusted strands splayed downward.</summary>
        private static void Frayed(LowPolyMeshBuilder b, Vector3 end)
        {
            for (int k = -1; k <= 1; k++)
            {
                RecipeKit.Rod(b, end, end + new Vector3(0.03f * k, -0.12f, 0.025f * k), 0.008f, 4,
                    PaletteSwatch.Rust);
            }
        }
    }
}
