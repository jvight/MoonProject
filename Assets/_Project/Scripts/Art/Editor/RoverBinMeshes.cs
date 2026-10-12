using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's side bins (M3-25, RoverAbility.SideBins): an open-topped stowage bin hung on each front flank above the
    /// front wheel, a worn crew-paint box on two dark brackets with a rolled lip, so the extra room reads as somewhere
    /// to put things. Bin space: the origin on the body's flank, +X out of it, +Y up, +Z forward.
    /// </summary>
    internal static class RoverBinMeshes
    {
        /// <summary>The bin's outside: how far it stands out, how tall, how long.</summary>
        public static readonly Vector3 Size = new Vector3(0.16f, 0.15f, 0.3f);

        private const float Gap = 0.025f;
        private const float Wall = 0.014f;
        private const float Lip = 0.012f;

        public static LowPolyMeshBuilder Bin()
        {
            var b = new LowPolyMeshBuilder(300);
            float x = Gap + Size.x * 0.5f;
            b.Box(At(x, Wall * 0.5f, 0f), new Vector3(Size.x, Wall, Size.z), PaletteSwatch.WornPanel, 0.004f);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(x + side * (Size.x - Wall) * 0.5f, Size.y * 0.5f, 0f), new Vector3(Wall, Size.y, Size.z),
                    PaletteSwatch.WornPanel, 0.004f);
                b.Box(At(x, Size.y * 0.5f, side * (Size.z - Wall) * 0.5f), new Vector3(Size.x - 2f * Wall, Size.y, Wall),
                    PaletteSwatch.WornPanel, 0.004f);
                b.Box(At(Gap * 0.5f, Size.y * 0.55f, side * Size.z * 0.3f), new Vector3(Gap + 0.01f, 0.05f, 0.03f),
                    PaletteSwatch.Charcoal);
            }

            b.Box(At(x, Size.y + Lip * 0.5f, 0f), new Vector3(Size.x + Lip, Lip, Size.z + Lip), PaletteSwatch.FadedAccent,
                0.004f);
            b.Box(At(Gap + Size.x + 0.002f, Size.y * 0.45f, 0f), new Vector3(0.006f, 0.035f, Size.z * 0.6f),
                PaletteSwatch.Charcoal);
            Matrix4x4 face = SiteKit.Face(new Vector3(Gap + Size.x + 0.003f, Size.y * 0.5f, 0f), Vector3.right,
                Vector3.up);
            SiteKit.RustStreak(b, face, -0.08f, Size.y * 0.3f, 0.08f);
            return b;
        }
    }
}
