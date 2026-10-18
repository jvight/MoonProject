using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where everything on 07's face screen sits in the 1152x768 reference frame (pure, EditMode-tested): the title
    /// row, 07 in the middle with its four devices in labelled frames at the corners, the hold as a strip along the
    /// foot with each item's name under it, the info box and the key hints. The hold's bottom row is fixed and the
    /// rows a bigger hold adds grow upwards, so no item ever moves when a row unlocks.
    /// </summary>
    internal static class FaceLayout
    {
        public const float Margin = 72f;
        public const float RightEdge = FaceInventoryView.ReferenceWidth - Margin;
        public const float TitleY = 40f;
        public const float TitleHeight = 32f;
        public const float TitleRuleY = 78f;

        public const float FrameWidth = 288f;
        public const float FrameHeight = 96f;
        public const float FrameNameHeight = 34f;
        public const float FrameIconX = 32f;
        public const float FrameTextX = 62f;
        public const float FrameEdgeInset = 12f;
        public const float DeviceHitPadding = 8f;
        public const float LeaderKnee = 36f;

        public const float CellSize = 60f;
        public const float CellGapX = 20f;
        public const float NameGap = 2f;
        public const float NameHeight = 44f;
        public const float RowPitch = 116f;
        public const float BottomRowY = 584f;
        public const float StripHeadingHeight = 28f;
        public const float StripHeadingGap = 30f;
        public const float StripRuleGap = 8f;
        public const float MoreRowWidth = 296f;

        public const float InfoX = 744f;
        public const float InfoY = 530f;
        public const float InfoWidth = RightEdge - InfoX;
        public const float InfoHeight = 164f;
        public const float ToastY = 478f;
        public const float ToastHeight = 44f;
        public const float FooterY = 700f;
        public const float FooterHeight = 28f;

        private static readonly Vector2[] FrameOrigins =
        {
            new Vector2(Margin, 92f),
            new Vector2(RightEdge - FrameWidth, 92f),
            new Vector2(Margin, 214f),
            new Vector2(RightEdge - FrameWidth, 214f),
        };

        /// <summary>Width of the hold strip: one full row of cells.</summary>
        public static float StripWidth => FaceInventoryView.GridColumns * (CellSize + CellGapX) - CellGapX;

        /// <summary>Rows of the hold that hold at least one unlocked slot.</summary>
        public static int RowsOf(int unlocked)
        {
            return (unlocked + FaceInventoryView.GridColumns - 1) / FaceInventoryView.GridColumns;
        }

        /// <summary>True while some slots are still locked: the hint on the heading row says more come later.</summary>
        public static bool HasMoreRow(int unlocked)
        {
            return unlocked < FaceInventoryView.MaxHoldCells;
        }

        /// <summary>The cell of hold slot <paramref name="index"/>: row 0 is the bottom row, and more rows stack upwards.</summary>
        public static Rect HoldCellRect(int index)
        {
            int col = index % FaceInventoryView.GridColumns;
            int row = index / FaceInventoryView.GridColumns;
            return new Rect(Margin + col * (CellSize + CellGapX), BottomRowY - row * RowPitch, CellSize, CellSize);
        }

        /// <summary>The line of words under hold slot <paramref name="index"/>, as wide as the slot's column.</summary>
        public static Rect HoldNameRect(int index)
        {
            Rect cell = HoldCellRect(index);
            return new Rect(cell.x - CellGapX * 0.5f, cell.yMax + NameGap, CellSize + CellGapX, NameHeight);
        }

        /// <summary>The area that selects hold slot <paramref name="index"/>: its cell, its name and the gaps around them.</summary>
        public static Rect HoldHitRect(int index)
        {
            Rect cell = HoldCellRect(index);
            float gapY = (RowPitch - CellSize - NameGap - NameHeight) * 0.5f;
            return new Rect(cell.x - CellGapX * 0.5f, cell.y - gapY, CellSize + CellGapX, RowPitch);
        }

        /// <summary>Top of the highest unlocked row.</summary>
        public static float StripTop(int unlocked)
        {
            return BottomRowY - (Mathf.Max(1, RowsOf(unlocked)) - 1) * RowPitch;
        }

        /// <summary>The heading over the hold strip.</summary>
        public static Rect StripHeadingRect(int unlocked)
        {
            return new Rect(Margin, StripTop(unlocked) - StripHeadingGap - StripRuleGap, StripWidth, StripHeadingHeight);
        }

        /// <summary>Y of the rule under the strip's heading.</summary>
        public static float StripRuleY(int unlocked)
        {
            return StripTop(unlocked) - StripRuleGap;
        }

        /// <summary>The one hint on the heading row, right-aligned, that stands for every locked slot.</summary>
        public static Rect MoreRowRect(int unlocked)
        {
            Rect heading = StripHeadingRect(unlocked);
            return new Rect(heading.xMax - MoreRowWidth, heading.y, MoreRowWidth, heading.height);
        }

        /// <summary>The labelled frame of device <paramref name="device"/>.</summary>
        public static Rect DeviceFrame(int device)
        {
            Vector2 origin = FrameOrigins[device];
            return new Rect(origin.x, origin.y, FrameWidth, FrameHeight);
        }

        /// <summary>True for the frames left of 07: their leaders leave from the right edge.</summary>
        public static bool IsLeftFrame(int device)
        {
            return FrameOrigins[device].x < FaceInventoryView.ReferenceWidth * 0.5f;
        }

        /// <summary>Where device <paramref name="device"/>'s leader leaves its frame: the inner edge, level with the name.</summary>
        public static Vector2 LeaderAnchor(int device)
        {
            Rect frame = DeviceFrame(device);
            float x = IsLeftFrame(device) ? frame.xMax : frame.xMin;
            return new Vector2(x, frame.y + FrameNameHeight * 0.5f);
        }

        /// <summary>The bend of the leader: it runs level out of the frame, then straight to the part it names.</summary>
        public static Vector2 LeaderKneeOf(int device)
        {
            Vector2 anchor = LeaderAnchor(device);
            return new Vector2(anchor.x + (IsLeftFrame(device) ? LeaderKnee : -LeaderKnee), anchor.y);
        }

        /// <summary>Centre of the status disc in device <paramref name="device"/>'s frame.</summary>
        public static Vector2 DeviceIconCenter(int device)
        {
            Rect frame = DeviceFrame(device);
            return new Vector2(frame.x + FrameIconX, frame.y + FrameNameHeight + (FrameHeight - FrameNameHeight) * 0.5f);
        }

        /// <summary>The area that selects a device: its frame and a little around it.</summary>
        public static Rect DeviceHitRect(int device)
        {
            Rect frame = DeviceFrame(device);
            return new Rect(frame.x - DeviceHitPadding, frame.y - DeviceHitPadding, frame.width + 2f * DeviceHitPadding,
                frame.height + 2f * DeviceHitPadding);
        }

        /// <summary>The hold slot the selection enters from below a device frame: the top row, nearest the frame.</summary>
        public static int HoldEntryBelow(int device, int unlocked)
        {
            if (unlocked <= 0)
            {
                return -1;
            }

            int topRow = RowsOf(unlocked) - 1;
            float x = DeviceFrame(device).center.x;
            int col = Mathf.Clamp(Mathf.FloorToInt((x - Margin) / (CellSize + CellGapX)), 0,
                FaceInventoryView.GridColumns - 1);
            return Mathf.Min(unlocked - 1, topRow * FaceInventoryView.GridColumns + col);
        }

        /// <summary>The device the selection enters from the top row of the hold: the frame above that column.</summary>
        public static int DeviceAbove(int index)
        {
            return HoldCellRect(index).center.x < DeviceFrame(FaceDevices.SocketOne).xMax ? FaceDevices.SocketOne
                : FaceDevices.SocketTwo;
        }
    }
}
