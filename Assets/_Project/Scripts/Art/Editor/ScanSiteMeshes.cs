using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Where scannable fragments rest (0.5.5 owner feedback §1.4): a fallen panel on a little rubble at a site, sized
    /// to its fragment, so nothing scannable lies on clean regolith; the relay.2 service hut, an open-fronted lean-to
    /// with the spare cells' shelf; and the chasm lip's crashed test jumper. Each space: its root on the ground, +Z its
    /// front (the hut's open side, the jumper's nose toward the chasm).
    /// </summary>
    internal static class ScanSiteMeshes
    {
        /// <summary>How thick a rest slab is: the fragment's socket sits on its top.</summary>
        public const float RestHeight = 0.06f;

        private const float HutWidth = 2.2f;
        private const float HutDepth = 1.8f;
        private const float HutHeight = 2.1f;
        private const float ShelfHeight = 0.85f;
        private const float ShelfDepth = 0.42f;

        /// <summary>The hut's shelf sockets (hut space), left to right along the back wall.</summary>
        public static Vector3 HutShelfSocket(int index)
        {
            return new Vector3(-0.6f + index * 0.6f, ShelfHeight + 0.02f, -HutDepth * 0.5f + ShelfDepth * 0.5f + 0.05f);
        }

        /// <summary>The jumper wreck's fragment sockets (wreck space): on its broken frame and in the debris.</summary>
        public static readonly Vector3[] JumperSockets =
        {
            new Vector3(-0.55f, RestHeight, 0.6f), new Vector3(0.6f, 0.44f, -0.05f),
            new Vector3(0.15f, RestHeight, -0.9f),
        };

        /// <summary>A rest slab for a fragment of <paramref name="footprint"/> (x, z): a worn panel on two rubble
        /// stones, its top at <see cref="RestHeight"/>.</summary>
        public static LowPolyMeshBuilder Rest(Vector2 footprint, int seed)
        {
            var b = new LowPolyMeshBuilder(160);
            var size = new Vector3(footprint.x + 0.18f, 0.03f, footprint.y + 0.18f);
            MeshRange plate = b.Box(At(0f, RestHeight - 0.015f, 0f), size, PaletteSwatch.WornPanel, 0.006f);
            SiteKit.DustCap(b, plate);
            var random = new System.Random(seed);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Icosphere(At(new Vector3(side * size.x * 0.35f, 0.02f, size.z * 0.3f * side), Quaternion.identity,
                    new Vector3(1.3f, 0.5f, 1f)), 0.06f + 0.03f * (float)random.NextDouble(), 0, PaletteSwatch.RockDark);
            }

            return b;
        }

        /// <summary>
        /// The relay.2 service hut: an open-fronted lean-to in worn crew panels on a concrete pad, its roof sloping
        /// back, a shelf along the back wall for the spare cells, and a tired door leaning beside the opening.
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
            Matrix4x4 frame = At(new Vector3(0f, 0.35f, 0f), new Vector3(-14f, 10f, 6f));
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

            b.Append(Rest(new Vector2(0.4f, 0.4f), 3), Matrix4x4.Translate(new Vector3(JumperSockets[0].x, 0f,
                JumperSockets[0].z)));
            b.Append(Rest(new Vector2(0.5f, 0.4f), 5), Matrix4x4.Translate(new Vector3(JumperSockets[2].x, 0f,
                JumperSockets[2].z)));
            return b;
        }
    }
}
