using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the pieces of 07's face screen sit in the 1152x768 reference frame beyond the hold cells and device
    /// bubbles (pure, EditMode-tested): the one hint row that stands for the locked slots, the info box that names the
    /// selection, the key hints at the foot, and the hit areas, which are the cells and bubbles plus the gaps around
    /// them so the pointer never crosses a dead strip.
    /// </summary>
    internal static class FaceLayout
    {
        public const float InfoX = 72f;
        public const float InfoY = 632f;
        public const float InfoWidth = 1008f;
        public const float InfoHeight = 66f;
        public const float FooterY = 708f;
        public const float FooterHeight = 28f;
        public const float MoreRowHeight = 44f;
        public const float MoreRowGap = 8f;
        public const float DividerBottom = 622f;
        public const float HeadingY = 32f;
        public const float HeadingRuleY = 72f;
        public const float ToastY = 566f;
        public const float ToastHeight = 60f;
        public const float ToastWidth = 376f;
        public const float DeviceStateMaxHalfWidth = 100f;
        public const float DeviceStateEdgeMargin = 24f;
        public const float DeviceHitScale = 1.8f;

        /// <summary>Rows of the hold grid that hold at least one unlocked slot.</summary>
        public static int RowsOf(int unlocked)
        {
            return (unlocked + FaceInventoryView.GridColumns - 1) / FaceInventoryView.GridColumns;
        }

        /// <summary>True while some slots are still locked: the hint row says more come later.</summary>
        public static bool HasMoreRow(int unlocked)
        {
            return unlocked < FaceInventoryView.MaxHoldCells;
        }

        /// <summary>The single hint row under the last row of unlocked slots.</summary>
        public static Rect MoreRowRect(int unlocked)
        {
            float width = FaceInventoryView.GridColumns * (FaceInventoryView.CellSize + FaceInventoryView.CellGapX) -
                          FaceInventoryView.CellGapX;
            float top = FaceInventoryView.HoldGridY +
                        RowsOf(unlocked) * (FaceInventoryView.CellSize + FaceInventoryView.CellGapY) + MoreRowGap;
            return new Rect(FaceInventoryView.HoldGridX, top, width, MoreRowHeight);
        }

        /// <summary>The area that selects hold cell <paramref name="index"/>: the cell and half the gaps around it.</summary>
        public static Rect HoldHitRect(int index)
        {
            Rect cell = FaceInventoryView.GetHoldCellReferenceRect(index);
            float halfX = FaceInventoryView.CellGapX * 0.5f;
            float halfY = FaceInventoryView.CellGapY * 0.5f;
            return new Rect(cell.x - halfX, cell.y - halfY, cell.width + 2f * halfX, cell.height + 2f * halfY);
        }

        /// <summary>The half width of the words under a device bubble, kept inside the screen.</summary>
        public static float DeviceStateHalfWidth(float centerX, bool leftSide)
        {
            float room = leftSide
                ? centerX - DeviceStateEdgeMargin
                : FaceInventoryView.ReferenceWidth - DeviceStateEdgeMargin - centerX;
            return Mathf.Min(DeviceStateMaxHalfWidth, room);
        }
    }
}
