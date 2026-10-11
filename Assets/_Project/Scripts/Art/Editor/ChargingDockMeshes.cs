using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's charging dock out on the apron in front of the habitat lander's belly (M3-19 D3; VISION rulings 8, 12 and
    /// 14): the pad 07 wakes on in a fresh game, so the dock stands exactly on 07's spawn, 4.5 m south of the base
    /// pad's centre, facing the way 07 first looks (bearing 355°, Earth and The Peak in frame). The pad is a worn plate flush with
    /// the dust, with faded wheel lanes, so 07 rests on it and drives off it either way with nothing in its lane; the
    /// squat charging pylon stands beside the pad ahead of 07's left front wheel, on the lander's side, its lead
    /// running over the dust to the lander's front right leg without crossing 07's lane. The pylon's lamp band and the pad's corner lenses are a separate glow layer, dark
    /// until 07 charges. Drawn in dock space (the origin under 07's pivot on the pad, +Z = 07's heading) and placed in
    /// base space by <see cref="Anchor"/> and <see cref="AnchorEuler"/>.
    /// </summary>
    internal static class ChargingDockMeshes
    {
        /// <summary>The pad's top: a plate a hair above the dust, so 07 settles onto it without a step.</summary>
        public const float PadTop = 0.012f;

        /// <summary>
        /// 07's rest point in base space: its pivot on the pad. The lander stands at (-10, 0, 8) facing the base pad's
        /// centre (Gameplay's BaseTuning), and 07 spawns at (0, 0, -4.5) (Rover's SpawnSettings), out of the lander's
        /// long earthlight shadow, which falls east across the pad 3 to 10 m north of the centre: that point, in the
        /// lander's frame, ahead of its belly and right of its front.
        /// </summary>
        public static readonly Vector3 Anchor = new Vector3(3.514f, PadTop, 15.617f);

        /// <summary>
        /// 07's heading on the dock in base space: Rover's spawn bearing 355° (RoverTuning) less the lander's own
        /// bearing (128.66°, facing the pad's centre from home).
        /// </summary>
        public static readonly Vector3 AnchorEuler = new Vector3(0f, 226.34f, 0f);

        public const float PadHalfWidth = 0.86f;

        // The pad's ends in dock space, behind 07's rear wheels and ahead of its front ones.
        private const float PadBack = -1.4f;
        private const float PadFront = 1.2f;

        private const float LaneX = 0.64f;

        // The pylon beside the pad, ahead of 07's left front wheel (the lander's side) and clear of its lane.
        private static readonly Vector3 Pylon = new Vector3(-PadHalfWidth - 0.42f, 0f, 0.7f);
        private static readonly Vector3 PylonSize = new Vector3(0.3f, 0.78f, 0.24f);

        // Where the pylon's lead reaches the lander, in base space: the foot of its front right leg.
        private static readonly Vector3 LeadEnd = new Vector3(3.85f, 0.03f, 4.75f);

        /// <summary>Dock space to base space.</summary>
        public static Matrix4x4 ToBase => Matrix4x4.TRS(Anchor - Vector3.up * PadTop, Rotation(AnchorEuler),
            Vector3.one);

        /// <summary>The pad, its lanes, the pylon and its lead back to the lander.</summary>
        public static void Dock(LowPolyMeshBuilder b)
        {
            var dock = new LowPolyMeshBuilder(300);
            DockParts(dock);
            b.Append(dock, ToBase);
            Vector3 lead = ToBase.MultiplyPoint3x4(Pylon + new Vector3(0f, 0.03f, PylonSize.z * 0.5f));
            SiteKit.Cable(b, SiteKit.Sag(lead, LeadEnd, 0f, 8), 0.02f, PaletteSwatch.DustShadow);
        }

        private static void DockParts(LowPolyMeshBuilder b)
        {
            float length = PadFront - PadBack;
            float middle = (PadBack + PadFront) * 0.5f;
            b.Box(At(0f, PadTop * 0.5f, middle), new Vector3(PadHalfWidth * 2f, PadTop, length),
                PaletteSwatch.WornPanel, 0.004f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * LaneX, PadTop + 0.002f, middle), new Vector3(0.1f, 0.004f, length - 0.5f),
                    PaletteSwatch.FadedAccent);
            }

            b.Box(At(Pylon + Vector3.up * (PylonSize.y * 0.5f)), PylonSize, PaletteSwatch.FadedPaint, 0.03f);
            b.Box(At(Pylon + Vector3.up * (PylonSize.y + 0.02f)), new Vector3(0.36f, 0.04f, 0.3f), PaletteSwatch.Metal,
                0.01f);
            b.Box(At(Pylon + new Vector3(PylonSize.x * 0.5f + 0.004f, 0.42f, 0f)), new Vector3(0.008f, 0.16f, 0.12f),
                PaletteSwatch.Honey);
            b.Box(At(Pylon + Vector3.up * 0.03f), new Vector3(0.42f, 0.06f, 0.36f), PaletteSwatch.Charcoal, 0.01f);
        }

        /// <summary>What glows while 07 charges: a band round the pylon's head and a lens at each pad corner.</summary>
        public static LowPolyMeshBuilder Glow()
        {
            var b = new LowPolyMeshBuilder(120);
            b.Box(At(Pylon + Vector3.up * (PylonSize.y - 0.09f)),
                new Vector3(PylonSize.x + 0.02f, 0.05f, PylonSize.z + 0.02f), PaletteSwatch.LampGlass);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (PadHalfWidth - 0.08f);
                float z = i < 2 ? PadFront - 0.08f : PadBack + 0.08f;
                b.Prism(At(x, PadTop + 0.004f, z), 0.045f, 0.008f, 6, PaletteSwatch.LampGlass);
            }

            b.Transform(new MeshRange(0, b.TriangleCount), ToBase);
            return b;
        }

        /// <summary>The dock's rust: a streak under the pylon's brass plate and a collar round its foot.</summary>
        public static void Rust(LowPolyMeshBuilder b)
        {
            var rust = new LowPolyMeshBuilder(60);
            Matrix4x4 face = SiteKit.Face(Pylon + new Vector3(PylonSize.x * 0.5f + Weathering.RustLift, 0f, 0f),
                Vector3.right, Vector3.up);
            SiteKit.RustStreak(rust, face, 0.02f, 0.34f, 0.25f, 0.03f);
            Weathering.Collar(rust, At(Pylon + Vector3.up * 0.08f), 0.22f, 0.1f);
            b.Append(rust, ToBase);
        }

        /// <summary>A point beside the dock (dock space) in base space: where the HOME sign stands.</summary>
        public static Vector3 Beside(Vector3 dockPoint)
        {
            return ToBase.MultiplyPoint3x4(dockPoint);
        }
    }
}
