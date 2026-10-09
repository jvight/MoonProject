using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's charging dock at the foot of the crew's ladder, where the welcome mat used to lie (VISION rulings 12 and
    /// 14): a low pad 07 drives onto nose first, towards the lander, with faded wheel lanes, and a squat charging post
    /// whose sprung contacts meet 07's front at its own height. The post's lamp band and the pad's corner lenses are a
    /// separate glow layer, dark until 07 charges. Built in base space like the lander.
    /// </summary>
    internal static class ChargingDockMeshes
    {
        public const float PadTop = 0.05f;

        /// <summary>07's rest point: its pivot on the pad, facing the lander (-Z), its nose at the contacts.</summary>
        public static readonly Vector3 Anchor = new Vector3(0f, PadTop, 4.15f);

        public static readonly Vector3 AnchorEuler = new Vector3(0f, 180f, 0f);

        public const float PadHalfWidth = 0.86f;
        public const float PadBack = 2.95f;
        public const float PadFront = 5.55f;

        private const float LaneX = 0.64f;
        private const float PostHeight = 0.78f;
        // The contacts meet 07's front either side of its road lamp and radio slats, where its face is bare.
        private const float ContactHeight = 0.65f;
        private const float ContactSpread = 0.3f;

        // The contact faces stop this short of 07's nose (its painted front face) so nothing touches at rest.
        private const float ContactGap = 0.02f;

        private static readonly Vector3 Post = new Vector3(0f, 0f, 3.1f);
        private static readonly Vector3 PostSize = new Vector3(0.3f, PostHeight, 0.24f);

        private static float ContactFace =>
            Anchor.z - (RoverMeshes.BodyFront + RoverMeshes.PaintProud + RoverMeshes.PaintThickness) - ContactGap;

        /// <summary>The pad, its entry lip, the post with its contact head and the lead back under the stage.</summary>
        public static void Dock(LowPolyMeshBuilder b)
        {
            float length = PadFront - PadBack;
            float middle = (PadBack + PadFront) * 0.5f;
            b.Box(At(0f, PadTop * 0.5f, middle), new Vector3(PadHalfWidth * 2f, PadTop, length), PaletteSwatch.Metal,
                0.015f);
            b.Wedge(At(0f, PadTop * 0.5f, PadFront + 0.12f), new Vector3(PadHalfWidth * 2f - 0.1f, PadTop, 0.24f),
                PaletteSwatch.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * LaneX, PadTop + 0.003f, middle + 0.1f), new Vector3(0.1f, 0.006f, length - 0.6f),
                    PaletteSwatch.FadedAccent);
            }

            b.Box(At(Post + Vector3.up * (PostHeight * 0.5f)), PostSize, PaletteSwatch.FadedPaint, 0.03f);
            b.Box(At(Post + Vector3.up * (PostHeight + 0.02f)), new Vector3(0.36f, 0.04f, 0.3f), PaletteSwatch.Metal,
                0.01f);
            float yokeBack = Post.z + PostSize.z * 0.5f;
            const float yokeDepth = 0.08f;
            b.Box(At(0f, ContactHeight, yokeBack + yokeDepth * 0.5f),
                new Vector3(ContactSpread * 2f + 0.06f, 0.06f, yokeDepth), PaletteSwatch.Charcoal, 0.01f);
            float prongBack = yokeBack + yokeDepth;
            float prongFront = ContactFace - 0.015f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * ContactSpread;
                b.Box(At(x, ContactHeight, (prongBack + prongFront) * 0.5f),
                    new Vector3(0.06f, 0.08f, prongFront - prongBack + 0.01f), PaletteSwatch.Charcoal, 0.01f);
                b.Box(At(x, ContactHeight, ContactFace - 0.0075f), new Vector3(0.05f, 0.07f, 0.015f),
                    PaletteSwatch.Honey);
            }

            SiteKit.Cable(b, SiteKit.Sag(new Vector3(0.12f, 0.12f, Post.z - PostSize.z * 0.5f),
                new Vector3(0.9f, LanderMeshes.StageBottom, 1.6f), 0.25f, 5), 0.02f, PaletteSwatch.Charcoal);
        }

        /// <summary>What glows while 07 charges: a band round the post's head and a lens at each pad corner.</summary>
        public static LowPolyMeshBuilder Glow()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(Post + Vector3.up * (PostHeight - 0.09f)),
                new Vector3(PostSize.x + 0.02f, 0.05f, PostSize.z + 0.02f), PaletteSwatch.LampGlass);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (PadHalfWidth - 0.08f);
                float z = i < 2 ? PadBack + 0.08f : PadFront - 0.08f;
                b.Prism(At(x, PadTop + 0.01f, z), 0.045f, 0.02f, 6, PaletteSwatch.LampGlass);
            }

            return b;
        }

        /// <summary>The dock's rust: a streak under the contact yoke and a collar round the post's foot.</summary>
        public static void Rust(LowPolyMeshBuilder b)
        {
            Matrix4x4 front = SiteKit.Face(Post + new Vector3(0f, 0f, PostSize.z * 0.5f + Weathering.RustLift),
                Vector3.forward, Vector3.up);
            SiteKit.RustStreak(b, front, -0.11f, ContactHeight - 0.05f, 0.25f, 0.03f);
            Weathering.Collar(b, At(Post + Vector3.up * 0.08f), 0.22f, 0.1f);
        }
    }
}
