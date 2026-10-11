using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's charging dock out on the apron in front of the habitat lander's belly (M3-19 D3; VISION rulings 8, 12 and
    /// 14): the pad 07 wakes on in a fresh game, so the dock stands exactly on 07's spawn at the base pad's centre,
    /// facing the way 07 first looks (bearing 355°, Earth and The Peak in frame). A low pad with faded wheel lanes, and
    /// a squat charging post ahead of 07's nose whose sprung contacts meet its front at its own height; the post's
    /// lead runs back over the dust to the lander's front right leg. The post's lamp band and the pad's corner lenses
    /// are a separate glow layer, dark until 07 charges. Drawn in dock space (the origin under 07's pivot on the pad,
    /// +Z = 07's heading) and placed in base space by <see cref="Anchor"/> and <see cref="AnchorEuler"/>.
    /// </summary>
    internal static class ChargingDockMeshes
    {
        public const float PadTop = 0.05f;

        /// <summary>
        /// 07's rest point in base space: its pivot on the pad. Home stands 12.81 m from the base pad's centre (the
        /// lander at (-10, 0, 8) facing it, Gameplay's BaseTuning), so the centre, where 07 spawns, lies straight
        /// ahead of the lander's front at that distance.
        /// </summary>
        public static readonly Vector3 Anchor = new Vector3(0f, PadTop, 12.81f);

        /// <summary>
        /// 07's heading on the dock in base space: Rover's spawn bearing 355° (RoverTuning) less the lander's own
        /// bearing (128.66°, facing the pad's centre from home).
        /// </summary>
        public static readonly Vector3 AnchorEuler = new Vector3(0f, 226.34f, 0f);

        public const float PadHalfWidth = 0.86f;

        // The pad's ends in dock space: behind 07's wheels and just past the post ahead of its nose.
        private const float PadBack = -1.4f;
        private const float PadFront = 1.2f;

        private const float LaneX = 0.64f;
        private const float PostHeight = 0.78f;

        // The contacts meet 07's front either side of its road lamp and radio slats, where its face is bare.
        private const float ContactHeight = 0.65f;
        private const float ContactSpread = 0.3f;

        // The contact faces stop this short of 07's nose (its painted front face) so nothing touches at rest.
        private const float ContactGap = 0.02f;

        private static readonly Vector3 Post = new Vector3(0f, 0f, 1.05f);
        private static readonly Vector3 PostSize = new Vector3(0.3f, PostHeight, 0.24f);

        // Where the post's lead reaches the lander, in base space: the foot of its front right leg.
        private static readonly Vector3 LeadEnd = new Vector3(3.85f, 0.03f, 4.75f);

        private static float ContactFace =>
            RoverMeshes.BodyFront + RoverMeshes.PaintProud + RoverMeshes.PaintThickness + ContactGap;

        /// <summary>Dock space to base space.</summary>
        public static Matrix4x4 ToBase => Matrix4x4.TRS(Anchor - Vector3.up * PadTop, Rotation(AnchorEuler),
            Vector3.one);

        /// <summary>The pad, its entry lip, the post with its contact head and the lead back to the lander.</summary>
        public static void Dock(LowPolyMeshBuilder b)
        {
            var dock = new LowPolyMeshBuilder(400);
            DockParts(dock);
            b.Append(dock, ToBase);
            Vector3 lead = ToBase.MultiplyPoint3x4(new Vector3(0.12f, 0.03f, Post.z + PostSize.z * 0.5f));
            SiteKit.Cable(b, SiteKit.Sag(lead, LeadEnd, 0f, 8), 0.02f, PaletteSwatch.DustShadow);
        }

        private static void DockParts(LowPolyMeshBuilder b)
        {
            float length = PadFront - PadBack;
            float middle = (PadBack + PadFront) * 0.5f;
            b.Box(At(0f, PadTop * 0.5f, middle), new Vector3(PadHalfWidth * 2f, PadTop, length),
                PaletteSwatch.WornPanel, 0.015f);
            b.Wedge(At(new Vector3(0f, PadTop * 0.5f, PadBack - 0.12f), new Vector3(0f, 180f, 0f)),
                new Vector3(PadHalfWidth * 2f - 0.1f, PadTop, 0.24f), PaletteSwatch.WornPanel);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * LaneX, PadTop + 0.003f, middle - 0.1f), new Vector3(0.1f, 0.006f, length - 0.6f),
                    PaletteSwatch.FadedAccent);
            }

            b.Box(At(Post + Vector3.up * (PostHeight * 0.5f)), PostSize, PaletteSwatch.FadedPaint, 0.03f);
            b.Box(At(Post + Vector3.up * (PostHeight + 0.02f)), new Vector3(0.36f, 0.04f, 0.3f), PaletteSwatch.Metal,
                0.01f);
            float yokeBack = Post.z - PostSize.z * 0.5f;
            const float yokeDepth = 0.08f;
            b.Box(At(0f, ContactHeight, yokeBack - yokeDepth * 0.5f),
                new Vector3(ContactSpread * 2f + 0.06f, 0.06f, yokeDepth), PaletteSwatch.Charcoal, 0.01f);
            float prongBack = yokeBack - yokeDepth;
            float prongFront = ContactFace + 0.015f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * ContactSpread;
                b.Box(At(x, ContactHeight, (prongBack + prongFront) * 0.5f),
                    new Vector3(0.06f, 0.08f, prongBack - prongFront + 0.01f), PaletteSwatch.Charcoal, 0.01f);
                b.Box(At(x, ContactHeight, ContactFace + 0.0075f), new Vector3(0.05f, 0.07f, 0.015f),
                    PaletteSwatch.Honey);
            }
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
                float z = i < 2 ? PadFront - 0.08f : PadBack + 0.08f;
                b.Prism(At(x, PadTop + 0.01f, z), 0.045f, 0.02f, 6, PaletteSwatch.LampGlass);
            }

            b.Transform(new MeshRange(0, b.TriangleCount), ToBase);
            return b;
        }

        /// <summary>The dock's rust: a streak under the contact yoke and a collar round the post's foot.</summary>
        public static void Rust(LowPolyMeshBuilder b)
        {
            var rust = new LowPolyMeshBuilder(60);
            Matrix4x4 face = SiteKit.Face(Post - new Vector3(0f, 0f, PostSize.z * 0.5f + Weathering.RustLift),
                Vector3.back, Vector3.up);
            SiteKit.RustStreak(rust, face, 0.11f, ContactHeight - 0.05f, 0.25f, 0.03f);
            Weathering.Collar(rust, At(Post + Vector3.up * 0.08f), 0.22f, 0.1f);
            b.Append(rust, ToBase);
        }

        /// <summary>A point beside the dock (dock space) in base space: where the HOME sign stands.</summary>
        public static Vector3 Beside(Vector3 dockPoint)
        {
            return ToBase.MultiplyPoint3x4(dockPoint);
        }
    }
}
