using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Structures scanning uses (0.5.5 owner feedback §1.4): the relay.2 service hut, an open-fronted lean-to with an
    /// empty shelf; and the chasm lip's crashed test jumper, its fragments still on its broken deck. Each space: its
    /// root on the ground, +Z its front (the hut's open side, the jumper's nose toward the chasm).
    /// </summary>
    internal static class ScanSiteMeshes
    {
        private const float HutWidth = 2.2f;
        private const float HutDepth = 1.8f;
        private const float HutHeight = 2.1f;
        private const float ShelfHeight = 0.85f;
        private const float ShelfDepth = 0.42f;

        // The jumper's frame, nosed into the dust.
        private static Matrix4x4 JumperFrame => At(new Vector3(0f, 0.35f, 0f), new Vector3(-14f, 10f, 6f));

        // Its coils, coil plate and buckled leg still on its deck (frame space).
        private static readonly Vector3[] JumperDeck =
        {
            new Vector3(0.22f, 0.06f, 0.42f), new Vector3(-0.12f, 0.06f, -0.38f), new Vector3(0.25f, 0.06f, -0.1f),
        };

        /// <summary>
        /// Where the Hover-Jump's fragment <paramref name="index"/> lies on the jumper (wreck space) and how: its
        /// spring, its coil plate and its buckled leg still on its broken deck.
        /// </summary>
        public static Vector3 JumperSocket(int index, out Quaternion rotation)
        {
            Matrix4x4 frame = JumperFrame;
            rotation = frame.rotation * Quaternion.Euler(0f, index * 70f, 0f);
            return frame.MultiplyPoint3x4(JumperDeck[index]);
        }

        /// <summary>
        /// The relay.2 service hut: an open-fronted lean-to in worn crew panels on a concrete pad, its roof sloping
        /// back, an empty shelf along the back wall, and a tired door leaning beside the opening.
        /// </summary>
        public static LowPolyMeshBuilder Hut()
        {
            var b = new LowPolyMeshBuilder(900);
            b.Box(At(0f, 0.04f, 0f), new Vector3(HutWidth + 0.3f, 0.08f, HutDepth + 0.3f), PaletteSwatch.DustMid, 0.02f);
            MeshRange back = b.Box(At(0f, HutHeight * 0.45f, -HutDepth * 0.5f), new Vector3(HutWidth, HutHeight * 0.9f,
                0.06f), PaletteSwatch.WornPanel);
            SiteKit.DustCap(b, back);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * HutWidth * 0.5f, HutHeight * 0.5f, 0f), new Vector3(0.06f, HutHeight, HutDepth),
                    PaletteSwatch.WornPanel);
                b.Box(At(side * (HutWidth * 0.5f - 0.04f), HutHeight * 0.5f, HutDepth * 0.5f),
                    new Vector3(0.1f, HutHeight, 0.1f), PaletteSwatch.Metal);
            }

            MeshRange roof = b.Box(At(new Vector3(0f, HutHeight, 0f), new Vector3(-8f, 0f, 0f)),
                new Vector3(HutWidth + 0.3f, 0.06f, HutDepth + 0.4f), PaletteSwatch.FadedPaint, 0.01f);
            SiteKit.DustCap(b, roof);
            b.Box(At(0f, ShelfHeight, -HutDepth * 0.5f + ShelfDepth * 0.5f + 0.03f), new Vector3(HutWidth - 0.2f, 0.04f,
                ShelfDepth), PaletteSwatch.Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (HutWidth * 0.5f - 0.2f), ShelfHeight * 0.5f, -HutDepth * 0.5f + 0.25f),
                    new Vector3(0.05f, ShelfHeight, 0.05f), PaletteSwatch.Metal);
            }

            b.Box(At(new Vector3(HutWidth * 0.5f + 0.35f, 0.95f, HutDepth * 0.5f - 0.1f), new Vector3(0f, 25f, -8f)),
                new Vector3(0.9f, 1.8f, 0.05f), PaletteSwatch.Sage, 0.01f);
            b.Box(At(-0.5f, HutHeight * 0.75f, -HutDepth * 0.5f + 0.04f), new Vector3(0.5f, 0.2f, 0.01f),
                PaletteSwatch.FadedAccent);
            return b;
        }

        /// <summary>
        /// Kenji's crashed test jumper at the chasm's near lip: a small frame nosed into the dust short of the edge, one
        /// leg buckled, its coil plate torn away, rubble round it.
        /// </summary>
        public static LowPolyMeshBuilder JumperWreck()
        {
            var b = new LowPolyMeshBuilder(900);
            Matrix4x4 frame = JumperFrame;
            for (int side = -1; side <= 1; side += 2)
            {
                SiteKit.Bar(b, frame.MultiplyPoint3x4(new Vector3(side * 0.4f, 0f, -0.7f)),
                    frame.MultiplyPoint3x4(new Vector3(side * 0.4f, 0f, 0.7f)), 0.05f, PaletteSwatch.Metal);
                SiteKit.Bar(b, frame.MultiplyPoint3x4(new Vector3(-0.4f, 0f, side * 0.7f)),
                    frame.MultiplyPoint3x4(new Vector3(0.4f, 0f, side * 0.7f)), 0.05f, PaletteSwatch.Metal);
            }

            MeshRange deck = b.Box(frame * At(0f, 0.04f, -0.15f), new Vector3(0.78f, 0.03f, 0.9f), PaletteSwatch.Honey,
                0.006f);
            SiteKit.DustCap(b, deck);
            Vector3 hip = frame.MultiplyPoint3x4(new Vector3(0.4f, 0f, 0.7f));
            SiteKit.Bar(b, hip, hip + new Vector3(0.25f, -0.3f, 0.2f), 0.04f, PaletteSwatch.Metal);
            SiteKit.Bar(b, hip + new Vector3(0.25f, -0.3f, 0.2f), new Vector3(hip.x + 0.1f, 0.02f, hip.z + 0.5f), 0.04f,
                PaletteSwatch.Rust);
            Vector3 other = frame.MultiplyPoint3x4(new Vector3(-0.4f, 0f, -0.7f));
            SiteKit.Bar(b, other, new Vector3(other.x - 0.1f, 0.02f, other.z - 0.2f), 0.04f, PaletteSwatch.Metal);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 1.3f;
                b.Icosphere(At(new Vector3(Mathf.Cos(angle) * 0.9f, 0.04f, Mathf.Sin(angle) * 0.8f + 0.2f),
                    Quaternion.identity, new Vector3(1f, 0.6f, 1f)), 0.08f + i * 0.01f, 0, PaletteSwatch.RockDark);
            }

            return b;
        }
    }
}
