using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where everything on 07's face screen sits in the 1152x768 reference frame (pure, EditMode-tested), on one grid
    /// with no rules or frames that carry nothing. The title row runs the full width (the page tabs at its right), the
    /// key hints run along the foot. The left half is the hold: a grid of four columns with each item's name under its
    /// cell, only the unlocked rows shown and one hint row where the next row will grow, paged three rows at a time (a
    /// full hold of 24 is two pages), the page count right of that row, then a status line and the info box, which says
    /// what the selection is and where it goes. The right half is 07's line drawing with its four devices as bays, two
    /// above it and two below, each a drop area big enough to hit with a leader to its part.
    /// </summary>
    internal static class FaceLayout
    {
        public const float Margin = 72f;
        public const float RightEdge = FaceInventoryView.ReferenceWidth - Margin;
        public const float TitleY = 40f;
        public const float TitleHeight = 32f;

        public const int Columns = 4;
        public const int RowsPerPage = 3;
        public const int PageSize = Columns * RowsPerPage;
        public const float ColumnPitch = 120f;
        public const float HoldWidth = Columns * ColumnPitch;
        public const float HoldRight = Margin + HoldWidth;
        public const float PageWidth = 140f;
        public const float GridTop = 112f;
        public const float CellSize = 76f;
        public const float NameGap = 4f;
        public const float NameHeight = 46f;
        public const float RowGap = 6f;
        public const float RowPitch = CellSize + NameGap + NameHeight + RowGap;
        public const float HintGap = 4f;
        public const float HintHeight = 24f;
        public const float StatusY = GridTop + RowsPerPage * RowPitch - RowGap + HintGap;
        public const float ToastY = StatusY + HintHeight + HintGap;
        public const float ToastHeight = 24f;
        public const float InfoX = Margin;
        public const float InfoY = 566f;
        public const float InfoWidth = HoldWidth;
        public const float InfoHeight = 124f;
        public const float InfoNameHeight = 36f;
        public const float InfoDescHeight = 60f;
        public const float InfoUseHeight = 28f;
        public const float FooterY = 700f;
        public const float FooterHeight = 28f;

        public const float DeviceX = 600f;
        public const float FrameWidth = 232f;
        public const float FrameHeight = 104f;
        public const float FrameGap = RightEdge - DeviceX - 2f * FrameWidth;
        public const float FrameTopY = 104f;
        public const float FrameBottomY = 588f;
        public const float FramePadX = 14f;
        public const float FrameNameY = 12f;
        public const float FrameNameHeight = 28f;
        public const float FrameStateY = 44f;
        public const float FrameStateHeight = 48f;
        public const float BracketLength = 16f;
        public const float DeviceHitPadding = 8f;
        public const float ArtY = 220f;
        public const float ArtHeight = 356f;
        public const float LeaderEdgeInset = 24f;

        public const float ReleaseOffsetX = 40f;
        public const float ReleaseOffsetY = -36f;
        public const float ReleaseWidth = 200f;
        public const float ReleaseHeight = 26f;

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

        /// <summary>The page count on the status row under the grid, right-aligned: a click turns the page.</summary>
        public static Rect PageRect => new Rect(HoldRight - PageWidth, StatusY, PageWidth, HintHeight);

        /// <summary>
        /// The one hint row that stands for every locked slot: on the last page, right under its last row, where the
        /// next row will grow.
        /// </summary>
        public static Rect HintRect(int unlocked)
        {
            int rows = RowsOnPage(PageCount(unlocked) - 1, unlocked);
            return new Rect(Margin, GridTop + rows * RowPitch - RowGap + HintGap, HoldWidth - PageWidth, HintHeight);
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

        /// <summary>The whole right-hand half: 07 and its devices. A part dropped anywhere on it goes to its device.</summary>
        public static Rect DevicePanel => new Rect(DeviceX, 0f, FaceInventoryView.ReferenceWidth - DeviceX,
            FaceInventoryView.ReferenceHeight);

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

        /// <summary>The bend of the leader: it runs straight out of the frame to the part's height, then across to it.</summary>
        public static Vector2 LeaderKneeOf(int device, Vector2 target)
        {
            return new Vector2(LeaderStart(device, target).x, target.y);
        }
    }
}
