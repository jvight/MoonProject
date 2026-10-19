using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The databank page's geometry in the 1152 x 768 face panel (docs/design/databank.md §4), inside the same glass
    /// margins as the hold page: the category column on the left, the model or print on the right with the field row,
    /// readout, set line and history card under it, the era strip and the footer along the bottom.
    /// </summary>
    internal static class DatabankLayout
    {
        public const float Margin = FaceLayout.Margin;
        public const float RightEdge = FaceLayout.RightEdge;
        public const float TitleY = FaceLayout.TitleY;
        public const float TitleHeight = FaceLayout.TitleHeight;
        public const float TitleRuleY = FaceLayout.TitleRuleY;
        public const float ListTop = 92f;
        public const float ListWidth = 220f;
        public const float ListBottom = 690f;
        public const float RowPitch = 30f;
        public const float EntryIndent = 18f;
        public const float UnreadBarGap = 8f;
        public const float UnreadBarWidth = 4f;
        public const float UnreadBarInset = 6f;
        public const float ColumnGap = 16f;
        public const float RightX = Margin + ListWidth + ColumnGap;
        public const float RightWidth = RightEdge - RightX;
        public const float ModelTop = 92f;
        public const float ModelHeight = 330f;
        public const float FieldY = 430f;
        public const float FieldHeight = 26f;
        public const float ReadoutY = 460f;
        public const float ReadoutHeight = 52f;
        public const float SetLineY = 514f;
        public const float SetLineHeight = 24f;
        public const float HistoryY = 544f;
        public const float HistoryHeight = 122f;
        public const float StripY = 672f;
        public const float StripHeight = 22f;
        public const float StripMarkPitch = 44f;
        public const float FooterY = FaceLayout.FooterY;
        public const float FooterHeight = FaceLayout.FooterHeight;

        public static Rect TitleRect => new Rect(Margin, TitleY, RightEdge - Margin, TitleHeight);

        public static Rect ModelRect => new Rect(RightX, ModelTop, RightWidth, ModelHeight);

        public static Rect FieldRect => new Rect(RightX, FieldY, RightWidth, FieldHeight);

        public static Rect ReadoutRect => new Rect(RightX, ReadoutY, RightWidth, ReadoutHeight);

        public static Rect SetLineRect => new Rect(RightX, SetLineY, RightWidth, SetLineHeight);

        public static Rect HistoryRect => new Rect(RightX, HistoryY, RightWidth, HistoryHeight);

        public static Rect StripRect => new Rect(RightX, StripY, RightWidth, StripHeight);

        public static Rect FooterRect => new Rect(Margin, FooterY, RightEdge - Margin, FooterHeight);

        /// <summary>Most rows the category column shows at once.</summary>
        public static int VisibleRows => Mathf.FloorToInt((ListBottom - ListTop) / RowPitch);

        /// <summary>The rect of list row <paramref name="row"/> (categories and the open category's entries).</summary>
        public static Rect RowRect(int row, bool entry)
        {
            float indent = entry ? EntryIndent : 0f;
            return new Rect(Margin + indent, ListTop + row * RowPitch, ListWidth - indent, RowPitch);
        }

        /// <summary>The rect of era mark <paramref name="index"/> on the strip.</summary>
        public static Rect StripMarkRect(int index)
        {
            return new Rect(RightX + index * StripMarkPitch, StripY, StripMarkPitch, StripHeight);
        }
    }
}
