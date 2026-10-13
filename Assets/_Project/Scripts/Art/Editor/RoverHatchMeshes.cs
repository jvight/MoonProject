using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's cargo hatch (M3-25 T1): the rear leaf, hinged along its top so it lifts out between the Cargo Cradle's
    /// rails, a shallow worn panel with a window strip lit warm from inside, where the hold's crates stand in dark
    /// silhouette against the glow, two rows, one per hold slot, so full or empty reads from the chase camera. Leaf space: the origin on the hinge, -Y down the leaf, -Z out of 07's back.
    /// </summary>
    internal static class RoverHatchMeshes
    {
        /// <summary>Hold slots the window shows (CargoTuning's hold, 07's own body).</summary>
        public const int Crates = 8;

        /// <summary>How far the leaf hangs below its hinge.</summary>
        public const float Height = 0.26f;

        private const int Columns = 4;
        private const float HalfWidth = 0.22f;
        private const float Depth = 0.028f;
        private const float Bar = 0.014f;
        private static readonly Vector3 CrateSize = new Vector3(0.09f, 0.054f, 0.02f);
        private const float CrateGap = 0.008f;

        // The window strip sits high on the leaf, clear of the cradle's lower rail.
        private const float WindowTop = 0.02f;

        private static float WindowWidth => Columns * CrateSize.x + (Columns + 1) * CrateGap;

        private static float WindowHeight => 2f * CrateSize.y + 3f * CrateGap;

        /// <summary>The window's centre (leaf space), where HatchWindow sits.</summary>
        public static Vector3 Window => new Vector3(0f, -WindowTop - WindowHeight * 0.5f, -Depth * 0.5f);

        /// <summary>Where crate <paramref name="index"/> sits in the window (window space), filling left to right,
        /// the top row first.</summary>
        public static Vector3 Crate(int index)
        {
            int column = index % Columns;
            int row = index / Columns;
            float x = -WindowWidth * 0.5f + CrateGap + CrateSize.x * 0.5f + column * (CrateSize.x + CrateGap);
            float y = WindowHeight * 0.5f - CrateGap - CrateSize.y * 0.5f - row * (CrateSize.y + CrateGap);
            return new Vector3(x, y, -0.002f);
        }

        /// <summary>The leaf: a shallow tray in worn crew paint framing the window, a grab handle low on it and the
        /// hinge's knuckle along its top.</summary>
        public static LowPolyMeshBuilder Leaf()
        {
            var b = new LowPolyMeshBuilder(200);
            float below = WindowTop + WindowHeight;
            b.Box(At(0f, -(below + Height) * 0.5f, -Depth * 0.5f), new Vector3(HalfWidth * 2f, Height - below, Depth),
                PaletteSwatch.WornPanel);
            b.Box(At(0f, -WindowTop * 0.5f, -Depth * 0.5f), new Vector3(HalfWidth * 2f, WindowTop, Depth),
                PaletteSwatch.WornPanel);
            float side = (HalfWidth * 2f - WindowWidth) * 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                b.Box(At(s * (HalfWidth - side * 0.5f), Window.y, -Depth * 0.5f),
                    new Vector3(side, WindowHeight, Depth), PaletteSwatch.WornPanel);
            }

            b.Box(At(0f, Window.y, -Depth + Bar * 0.25f), new Vector3(WindowWidth, Bar * 0.5f, Bar * 0.5f),
                PaletteSwatch.Charcoal);
            b.Box(At(0f, -(below + Height) * 0.5f - 0.03f, -Depth - 0.003f), new Vector3(0.09f, 0.016f, 0.006f),
                PaletteSwatch.Charcoal);
            b.Prism(At(Vector3.zero, AlongX), 0.012f, HalfWidth * 2f - 0.04f, 6, PaletteSwatch.Metal);
            return b;
        }

        /// <summary>
        /// The hold behind the window (window space): warm lamp glass on 07's own palette, so it always glows a notch
        /// under the lamps and an empty slot reads as a lit gap, a full one as a crate's dark shape against it.
        /// </summary>
        public static LowPolyMeshBuilder Hold()
        {
            var b = new LowPolyMeshBuilder(12);
            b.Box(At(0f, 0f, 0.011f), new Vector3(WindowWidth, WindowHeight, 0.004f), PaletteSwatch.LampGlass);
            return b;
        }

        /// <summary>
        /// One small crate (crate space: its centre), dark against the lit hold: wood or charcoal-stained, with a dark
        /// band across it.
        /// </summary>
        public static LowPolyMeshBuilder CrateMesh(bool stained)
        {
            var b = new LowPolyMeshBuilder(24);
            b.Box(Matrix4x4.identity, CrateSize, stained ? PaletteSwatch.RockDark : PaletteSwatch.Wood);
            b.Box(At(0f, 0f, -CrateSize.z * 0.5f - 0.001f), new Vector3(CrateSize.x, 0.008f, 0.002f),
                PaletteSwatch.Charcoal);
            return b;
        }
    }
}
