using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's garage service port (docs/design/first-hour-v2.md §3), made for wheels (rulings 12 and 14): a low steel
    /// frame at rover height with a folded fitting arm on a post, a parts tray on a short leg where 07 sets things down
    /// with its tether, and a sealed spare-parts crate with a honey stripe. Built in port space: +Y up, +Z toward where
    /// 07 parks (the pad lies in front, Gameplay's), the frame's back on the origin.
    /// </summary>
    internal static class GaragePortMeshes
    {
        private const float FrameWidth = 2.6f;
        private const float FrameHeight = 1.6f;
        private const float FrameDepth = 0.12f;
        private const float PostX = 0.9f;
        private const float PostHeight = 1.25f;
        private const float PostRadius = 0.07f;
        private const float ArmLength = 0.7f;
        private const float ArmWidth = 0.09f;
        private const float TrayX = -0.85f;
        private const float TrayHeight = 0.62f;
        private const float TraySize = 0.55f;

        /// <summary>The tray's top, where a set-down piece rests (port space).</summary>
        public static Vector3 TrayTop => new Vector3(TrayX, TrayHeight + 0.03f, 0.45f);

        /// <summary>The folded arm's tip, where a fitted piece leaves the port (port space).</summary>
        public static Vector3 ArmTip => new Vector3(PostX, PostHeight + 0.1f, 0.2f + ArmLength * 1.6f);

        public static LowPolyMeshBuilder Port()
        {
            var b = new LowPolyMeshBuilder(900);
            b.Box(At(0f, FrameHeight * 0.5f, 0f), new Vector3(FrameWidth, FrameHeight, FrameDepth),
                PaletteSwatch.WornPanel, 0.02f);
            b.Box(At(0f, FrameHeight + 0.04f, 0.1f), new Vector3(FrameWidth + 0.1f, 0.08f, 0.3f), PaletteSwatch.Metal,
                0.01f);
            b.Prism(At(new Vector3(PostX, PostHeight * 0.5f, 0.2f)), PostRadius, PostHeight, 8, PaletteSwatch.Metal);
            b.Box(At(new Vector3(PostX, PostHeight, 0.2f + ArmLength * 0.5f), new Vector3(-20f, 0f, 0f)),
                new Vector3(ArmWidth, ArmWidth, ArmLength), PaletteSwatch.FadedAccent, 0.01f);
            b.Icosphere(At(new Vector3(PostX, PostHeight, 0.2f)), ArmWidth * 0.8f, 1, PaletteSwatch.Honey);
            b.Box(At(new Vector3(PostX, PostHeight + 0.12f, 0.2f + ArmLength * 1.25f), new Vector3(30f, 0f, 0f)),
                new Vector3(ArmWidth * 0.8f, ArmWidth * 0.8f, ArmLength * 0.6f), PaletteSwatch.FadedAccent, 0.01f);
            b.Box(At(new Vector3(TrayX, TrayHeight * 0.5f, 0.45f)), new Vector3(0.08f, TrayHeight, 0.08f),
                PaletteSwatch.Metal);
            b.Box(At(new Vector3(TrayX, TrayHeight, 0.45f)), new Vector3(TraySize, 0.04f, TraySize),
                PaletteSwatch.Metal, 0.01f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(new Vector3(TrayX + side * TraySize * 0.5f, TrayHeight + 0.04f, 0.45f)),
                    new Vector3(0.03f, 0.06f, TraySize), PaletteSwatch.Metal);
            }

            b.Box(At(new Vector3(-0.1f, 0.3f, 0.35f)), new Vector3(0.7f, 0.6f, 0.5f), PaletteSwatch.FadedPaint, 0.03f);
            b.Box(At(new Vector3(-0.1f, 0.42f, 0.605f)), new Vector3(0.7f, 0.1f, 0.01f), PaletteSwatch.Honey);
            return b;
        }
    }
}
