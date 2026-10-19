using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The databank page's geometry in the 1152 x 768 face panel (docs/design/databank.md §4), on the hold page's grid
    /// and inside the same glass margins: the category column on the left, the model or print in the middle, and the
    /// words beside it, read top-down in the order a first look needs them: the name, what it is (maker, year, era),
    /// why it matters (the crew's card), then the scan line and 07's readout; the key row along the bottom.
    /// </summary>
    internal static class DatabankLayout
    {
        public const float Margin = FaceLayout.Margin;
        public const float RightEdge = FaceLayout.RightEdge;
        public const float TitleY = FaceLayout.TitleY;
        public const float TitleHeight = FaceLayout.TitleHeight;
        public const float ListTop = FaceLayout.GridTop;
        public const float ListWidth = 192f;
        public const float ListBottom = 680f;
        public const float RowPitch = 36f;
        public const float EntryIndent = 20f;
        public const float UnreadBarGap = 8f;
        public const float UnreadBarWidth = 3f;
        public const float UnreadBarInset = 8f;
        public const float ColumnGap = 40f;
        public const float ModelX = Margin + ListWidth + ColumnGap;
        public const float ModelTop = FaceLayout.GridTop;
        public const float ModelWidth = 416f;
        public const float ModelHeight = 444f;
        public const float TextGap = 24f;
        public const float TextX = ModelX + ModelWidth + TextGap;
        public const float TextWidth = RightEdge - TextX;
        public const float NameY = ModelTop;
        public const float NameHeight = 36f;
        public const float FieldY = 160f;
        public const float FieldHeight = 52f;
        public const float HistoryY = 224f;
        public const float HistoryHeight = 236f;
        public const float SetLineY = 472f;
        public const float SetLineHeight = 24f;
        public const float ReadoutY = 504f;
        public const float ReadoutHeight = 56f;
        public const float TabWidth = 150f;
        public const float TabGap = 12f;
        public const float TabsX = RightEdge - 2f * TabWidth;
        public const float FooterY = FaceLayout.FooterY;
        public const float FooterHeight = FaceLayout.FooterHeight;

        /// <summary>The title row left of the page tabs.</summary>
        public static Rect TitleRect => new Rect(Margin, TitleY, TabsX - TabGap - Margin, TitleHeight);

        /// <summary>Page tab <paramref name="index"/> (0 HOLD, 1 DATABANK) at the right of the title row.</summary>
        public static Rect TabRect(int index) => new Rect(TabsX + index * TabWidth, TitleY, TabWidth, TitleHeight);

        public static Rect ModelRect => new Rect(ModelX, ModelTop, ModelWidth, ModelHeight);

        public static Rect NameRect => new Rect(TextX, NameY, TextWidth, NameHeight);

        public static Rect FieldRect => new Rect(TextX, FieldY, TextWidth, FieldHeight);

        public static Rect HistoryRect => new Rect(TextX, HistoryY, TextWidth, HistoryHeight);

        public static Rect SetLineRect => new Rect(TextX, SetLineY, TextWidth, SetLineHeight);

        public static Rect ReadoutRect => new Rect(TextX, ReadoutY, TextWidth, ReadoutHeight);

        public static Rect FooterRect => new Rect(Margin, FooterY, RightEdge - Margin, FooterHeight);

        /// <summary>Most rows the category column shows at once.</summary>
        public static int VisibleRows => Mathf.FloorToInt((ListBottom - ListTop) / RowPitch);

        /// <summary>The rect of list row <paramref name="row"/> (categories and the open category's entries).</summary>
        public static Rect RowRect(int row, bool entry)
        {
            float indent = entry ? EntryIndent : 0f;
            return new Rect(Margin + indent, ListTop + row * RowPitch, ListWidth - indent, RowPitch);
        }
    }
}
