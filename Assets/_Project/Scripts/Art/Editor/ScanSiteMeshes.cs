using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Structures scanning uses (0.5.5 owner feedback §1.4): the relay.2 service hut, an open-fronted lean-to with an
    /// empty shelf: its root on the ground, +Z its open side.
    /// </summary>
    internal static class ScanSiteMeshes
    {
        private const float HutWidth = 2.2f;
        private const float HutDepth = 1.8f;
        private const float HutHeight = 2.1f;
        private const float ShelfHeight = 0.85f;
        private const float ShelfDepth = 0.42f;

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

    }
}
