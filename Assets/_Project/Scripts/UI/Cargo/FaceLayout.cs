using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where everything on 07's face screen sits in the 1152x768 reference frame (pure, EditMode-tested). The title row
    /// and the key hints run the full width. The left half is the hold: a grid of four columns with each item's name
    /// under its cell, only the unlocked rows shown and one hint row where the next row will grow, paged three rows at
    /// a time (a full hold of 24 is two pages), then a status line and the info box. The right half is 07's line
    /// drawing with its four devices in labelled frames, two above it and two below, each with a leader to its part.
    /// </summary>
    internal static class FaceLayout
    {
        public const float Margin = 72f;
        public const float RightEdge = FaceInventoryView.ReferenceWidth - Margin;
        public const float TitleY = 40f;
        public const float TitleHeight = 32f;
        public const float TitleRuleY = 78f;

        public const int Columns = 4;
        public const int RowsPerPage = 3;
        public const int PageSize = Columns * RowsPerPage;
        public const float ColumnPitch = 128f;
        public const float HoldWidth = Columns * ColumnPitch;
        public const float HoldRight = Margin + HoldWidth;
        public const float HeadingY = 92f;
        public const float HeadingHeight = 28f;
        public const float HeadingRuleY = 126f;
        public const float PageWidth = 176f;
        public const float GridTop = 132f;
        public const float CellSize = 70f;
        public const float NameGap = 2f;
        public const float NameHeight = 46f;
        public const float RowGap = 2f;
        public const float RowPitch = CellSize + NameGap + NameHeight + RowGap;
        public const float HintGap = 4f;
        public const float HintHeight = 26f;
        public const float HintPadlockX = 14f;
        public const float ToastY = 522f;
        public const float ToastHeight = 28f;
        public const float InfoX = Margin;
        public const float InfoY = 556f;
        public const float InfoWidth = HoldWidth;
        public const float InfoHeight = 132f;
        public const float FooterY = 700f;
        public const float FooterHeight = 28f;

        public const float DeviceX = 600f;
        public const float FrameWidth = 234f;
        public const float FrameHeight = 96f;
        public const float FrameGap = RightEdge - DeviceX - 2f * FrameWidth;
        public const float FrameTopY = 92f;
        public const float FrameBottomY = 592f;
        public const float FrameNameHeight = 34f;
        public const float FrameNamePad = 12f;
        public const float FrameGlyphRoom = 36f;
        public const float FrameIconX = 32f;
        public const float FrameTextX = 62f;
        public const float FrameEdgeInset = 12f;
        public const float DeviceHitPadding = 8f;
        public const float ArtY = 204f;
        public const float ArtHeight = 372f;
        public const float LeaderKnee = 36f;
        public const float LeaderEdgeInset = 24f;

        private static readonly Vector2[] FrameOrigins =
        {
            new Vector2(RightEdge - FrameWidth, FrameTopY),
            new Vector2(DeviceX, FrameTopY),
            new Vector2(DeviceX, FrameBottomY),
            new Vector2(RightEdge - FrameWidth, FrameBottomY),
        };

        /// <summary>The box 07's drawing is fitted into, between the upper and the lower frames.</summary>
        public static Rect ArtBox => new Rect(DeviceX, ArtY, RightEdge - DeviceX, ArtHeight);

        /// <summary>Rows of the hold that hold at least one unlocked slot.</summary>
        public static int RowsOf(int unlocked)
        {
            return (unlocked + Columns - 1) / Columns;
        }

        /// <summary>Pages of the hold: one for every three rows, and always one.</summary>
        public static int PageCount(int unlocked)
        {
            return Mathf.Max(1, (unlocked + PageSize - 1) / PageSize);
        }

        /// <summary>The page hold slot <paramref name="index"/> is shown on.</summary>
        public static int PageOf(int index)
        {
            return index / PageSize;
        }

        /// <summary>The unlocked rows shown on <paramref name="page"/>.</summary>
        public static int RowsOnPage(int page, int unlocked)
        {
            return Mathf.Clamp(RowsOf(unlocked - page * PageSize), 0, RowsPerPage);
        }

        /// <summary>True while some slots are still locked: one hint row says more come later.</summary>
        public static bool HasMoreRow(int unlocked)
        {
            return unlocked < FaceInventoryView.MaxHoldCells;
        }

        /// <summary>The cell of hold slot <paramref name="index"/> on its page: rows run down from the grid's top.</summary>
        public static Rect HoldCellRect(int index)
        {
            int col = index % Columns;
            int row = index / Columns % RowsPerPage;
            return new Rect(Margin + col * ColumnPitch + (ColumnPitch - CellSize) * 0.5f, GridTop + row * RowPitch,
                CellSize, CellSize);
        }

        /// <summary>The line of words under hold slot <paramref name="index"/>, as wide as the slot's column.</summary>
        public static Rect HoldNameRect(int index)
        {
            Rect cell = HoldCellRect(index);
            return new Rect(Margin + index % Columns * ColumnPitch, cell.yMax + NameGap, ColumnPitch, NameHeight);
        }

        /// <summary>The area that selects hold slot <paramref name="index"/>: its cell, its name and the gaps around them.</summary>
        public static Rect HoldHitRect(int index)
        {
            Rect cell = HoldCellRect(index);
            return new Rect(Margin + index % Columns * ColumnPitch, cell.y - RowGap * 0.5f, ColumnPitch, RowPitch);
        }

        /// <summary>The heading over the hold, left of the page count.</summary>
        public static Rect HeadingRect => new Rect(Margin, HeadingY, HoldWidth - PageWidth, HeadingHeight);

        /// <summary>The page count on the heading row, right-aligned over the grid: a click turns the page.</summary>
        public static Rect PageRect => new Rect(HoldRight - PageWidth, HeadingY, PageWidth, HeadingHeight);

        /// <summary>
        /// The one hint row that stands for every locked slot: on the last page, right under its last row, where the
        /// next row will grow.
        /// </summary>
        public static Rect HintRect(int unlocked)
        {
            int rows = RowsOnPage(PageCount(unlocked) - 1, unlocked);
            return new Rect(Margin, GridTop + rows * RowPitch + HintGap, HoldWidth, HintHeight);
        }

        /// <summary>The labelled frame of device <paramref name="device"/>.</summary>
        public static Rect DeviceFrame(int device)
        {
            Vector2 origin = FrameOrigins[device];
            return new Rect(origin.x, origin.y, FrameWidth, FrameHeight);
        }

        /// <summary>True for the frames above 07: their leaders leave from the bottom edge.</summary>
        public static bool IsTopFrame(int device)
        {
            return FrameOrigins[device].y < ArtY;
        }

        /// <summary>Centre of the status disc in device <paramref name="device"/>'s frame.</summary>
        public static Vector2 DeviceIconCenter(int device)
        {
            Rect frame = DeviceFrame(device);
            return new Vector2(frame.x + FrameIconX, frame.y + FrameNameHeight + (FrameHeight - FrameNameHeight) * 0.5f);
        }

        /// <summary>Centre of the warning mark on the name row of device <paramref name="device"/>'s frame.</summary>
        public static Vector2 WarningCenter(int device)
        {
            Rect frame = DeviceFrame(device);
            return new Vector2(frame.xMax - FrameGlyphRoom * 0.5f - FrameEdgeInset * 0.5f,
                frame.y + FrameNameHeight * 0.5f);
        }

        /// <summary>The area that selects a device: its frame and a little around it.</summary>
        public static Rect DeviceHitRect(int device)
        {
            Rect frame = DeviceFrame(device);
            return new Rect(frame.x - DeviceHitPadding, frame.y - DeviceHitPadding, frame.width + 2f * DeviceHitPadding,
                frame.height + 2f * DeviceHitPadding);
        }

        /// <summary>
        /// The whole image of 07's drawing, placed so its lines (<paramref name="contentUv"/>, UV with a top-left
        /// origin) fill <see cref="ArtBox"/> as far as their shape allows, centred.
        /// </summary>
        public static Rect ArtImageRect(Rect contentUv)
        {
            Rect box = ArtBox;
            float scale = Mathf.Min(box.width / contentUv.width, box.height / contentUv.height);
            Vector2 content = contentUv.size * scale;
            Vector2 contentMin = box.center - content * 0.5f;
            return new Rect(contentMin - contentUv.min * scale, new Vector2(scale, scale));
        }

        /// <summary>The point on the face of <paramref name="uv"/> (top-left origin) on 07's drawing.</summary>
        public static Vector2 ArtPoint(Vector2 uv, Rect contentUv)
        {
            Rect image = ArtImageRect(contentUv);
            return image.position + Vector2.Scale(uv, image.size);
        }

        /// <summary>Where device <paramref name="device"/>'s leader leaves its frame: the edge facing 07, over the part.</summary>
        public static Vector2 LeaderStart(int device, Vector2 target)
        {
            Rect frame = DeviceFrame(device);
            float x = Mathf.Clamp(target.x, frame.xMin + LeaderEdgeInset, frame.xMax - LeaderEdgeInset);
            return new Vector2(x, IsTopFrame(device) ? frame.yMax : frame.yMin);
        }

        /// <summary>The bend of the leader: it runs straight out of the frame, then on to the part it names.</summary>
        public static Vector2 LeaderKneeOf(int device, Vector2 target)
        {
            Vector2 start = LeaderStart(device, target);
            return new Vector2(start.x, start.y + (IsTopFrame(device) ? LeaderKnee : -LeaderKnee));
        }
    }
}
