using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Bell's tape rack: a small caramel-wood cubby shelf on four spindly tripod legs (Bell's own leg style), four
    /// rows of two face-out cubbies sized to the true-size tapes against a violet back, honey lips to keep the tapes
    /// standing and a dark groove on every empty slot so the missing tapes read from the base. A piece of furniture
    /// for people, a little over a metre tall. Origin on the ground at the centre of its base, +Z = the open front.
    /// </summary>
    internal static class CassetteShelfMeshes
    {
        public const int Rows = 4;
        public const int Columns = 2;
        public const int SlotCount = Rows * Columns;
        public const float Width = 0.56f;
        public const float Depth = 0.14f;

        private const float BaseHeight = 0.42f;
        private const float RowPitch = 0.16f;
        private const float TopRowY = BaseHeight + 0.1f + (Rows - 1) * RowPitch;
        private const float ColumnPitch = 0.24f;
        private const float SlotZ = 0.006f;
        private const float Board = 0.025f;
        private const float Top = TopRowY + RowPitch;
        private const float Groove = 0.003f;
        private const float LegSplay = 0.1f;

        /// <summary>
        /// Where tape <paramref name="index"/> stands (its bottom edge, on the slot's groove, label facing +Z), filled
        /// in reading order from the front: the top row first, the left cubby (+X) before the right.
        /// </summary>
        public static Vector3 SlotPosition(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Cassette slots are 0..7.");
            }

            int row = index / Columns;
            float x = index % Columns == 0 ? ColumnPitch * 0.5f : -ColumnPitch * 0.5f;
            return new Vector3(x, RowY(row) + Groove, SlotZ);
        }

        public static LowPolyMeshBuilder Rack()
        {
            var b = new LowPolyMeshBuilder(1200);
            float inner = Width - 2f * Board;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * (Width - Board) * 0.5f, (BaseHeight + Top) * 0.5f, 0f),
                    new Vector3(Board, Top - BaseHeight, Depth), PaletteSwatch.Wood, 0.008f);
                Vector3 hip = new Vector3(side * (Width * 0.5f - 0.04f), BaseHeight, 0f);
                for (int end = -1; end <= 1; end += 2)
                {
                    Vector3 from = hip + Vector3.forward * (end * Depth * 0.3f);
                    Vector3 foot = from + new Vector3(side * LegSplay, -BaseHeight + 0.012f, end * LegSplay * 0.8f);
                    RecipeKit.Rod(b, from + Vector3.up * 0.01f, foot, 0.013f, 6, PaletteSwatch.Metal);
                    b.Frustum(At(foot), 0.022f, 0.015f, 0.024f, 6, PaletteSwatch.Charcoal);
                }
            }

            b.Box(At(0f, Top + 0.022f, 0.005f), new Vector3(Width + 0.04f, 0.045f, Depth + 0.03f), PaletteSwatch.Wood,
                0.01f);
            b.Box(At(0f, Top + 0.022f, Depth * 0.5f + 0.021f), new Vector3(Width - 0.02f, 0.012f, 0.004f),
                PaletteSwatch.WarmAccent);
            b.Extrude(At(new Vector3(-Width * 0.32f, Top + 0.0465f, 0.012f), new Vector3(90f, 0f, 0f)),
                Glyphs.Star(0.04f, 0.018f), 0.004f, PaletteSwatch.Honey);
            b.Box(At(0f, (BaseHeight + Top) * 0.5f, -Depth * 0.5f + 0.006f),
                new Vector3(inner, Top - BaseHeight, 0.012f), PaletteSwatch.SkyHorizon);
            b.Box(At(0f, BaseHeight + 0.02f, 0f), new Vector3(inner, 0.04f, Depth), PaletteSwatch.Wood);
            b.Box(At(0f, BaseHeight + 0.06f, Depth * 0.5f - 0.01f), new Vector3(inner, 0.08f, 0.02f),
                PaletteSwatch.Wood);

            for (int row = 0; row < Rows; row++)
            {
                float y = RowY(row);
                b.Box(At(0f, y - 0.015f, 0f), new Vector3(inner, 0.03f, Depth - 0.012f), PaletteSwatch.Wood);
                b.Box(At(0f, y + 0.008f, Depth * 0.5f - 0.01f), new Vector3(inner, 0.03f, 0.012f),
                    PaletteSwatch.Honey);
                b.Box(At(0f, y + RowPitch * 0.5f - 0.02f, 0f), new Vector3(0.014f, RowPitch - 0.04f, Depth - 0.02f),
                    PaletteSwatch.Wood);
            }

            for (int i = 0; i < SlotCount; i++)
            {
                Vector3 slot = SlotPosition(i);
                b.Box(At(slot.x, slot.y - Groove * 0.5f, slot.z), new Vector3(CassetteMeshes.Width - 0.01f, Groove,
                    CassetteMeshes.Thickness + 0.01f), PaletteSwatch.Charcoal);
            }

            return b;
        }

        private static float RowY(int row)
        {
            return TopRowY - row * RowPitch;
        }
    }
}
