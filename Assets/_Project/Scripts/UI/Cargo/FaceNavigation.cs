using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the arrows, WASD and the pad move the selection on 07's face (pure, EditMode-tested, see
    /// <see cref="FaceLayout"/>): across and down the hold's grid, turning its page when a move runs past the last
    /// row shown; right off the grid into the nearer left frame; between the four frames as they stand; and left off
    /// the frames back into the grid's row nearest them. Each step yields either a hold slot or a device, or neither
    /// when the edge leads nowhere and the selection stays.
    /// </summary>
    internal static class FaceNavigation
    {
        /// <summary>The step from hold slot <paramref name="cell"/> in <paramref name="direction"/>.</summary>
        public static void FromHoldCell(int cell, NavigationMoveEvent.Direction direction, int unlocked, out int nextCell,
            out int nextDevice)
        {
            nextCell = -1;
            nextDevice = -1;
            int col = cell % FaceLayout.Columns;
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Left:
                    nextCell = col > 0 ? cell - 1 : -1;
                    break;
                case NavigationMoveEvent.Direction.Right:
                    if (col + 1 < FaceLayout.Columns && cell + 1 < unlocked)
                    {
                        nextCell = cell + 1;
                    }
                    else
                    {
                        nextDevice = DeviceRightOf(cell);
                    }

                    break;
                case NavigationMoveEvent.Direction.Up:
                    nextCell = cell >= FaceLayout.Columns ? cell - FaceLayout.Columns : -1;
                    break;
                case NavigationMoveEvent.Direction.Down:
                    if (cell + FaceLayout.Columns < unlocked)
                    {
                        nextCell = cell + FaceLayout.Columns;
                    }
                    else if ((unlocked - 1) / FaceLayout.Columns > cell / FaceLayout.Columns)
                    {
                        nextCell = unlocked - 1;
                    }

                    break;
            }
        }

        /// <summary>
        /// The step from device <paramref name="device"/> in <paramref name="direction"/>, with the hold showing
        /// <paramref name="page"/>.
        /// </summary>
        public static void FromDevice(int device, NavigationMoveEvent.Direction direction, int unlocked, int page,
            out int nextCell, out int nextDevice)
        {
            nextCell = -1;
            nextDevice = FaceDevices.Step(device, direction);
            if (nextDevice < 0 && direction == NavigationMoveEvent.Direction.Left)
            {
                nextCell = HoldEntryLeftOf(device, unlocked, page);
            }
        }

        /// <summary>The left frame nearest hold slot <paramref name="cell"/>'s row, the one right of the grid.</summary>
        public static int DeviceRightOf(int cell)
        {
            float y = FaceLayout.HoldCellRect(cell).center.y;
            float upper = Mathf.Abs(FaceLayout.DeviceFrame(FaceDevices.Cutter).center.y - y);
            float lower = Mathf.Abs(FaceLayout.DeviceFrame(FaceDevices.SocketOne).center.y - y);
            return upper <= lower ? FaceDevices.Cutter : FaceDevices.SocketOne;
        }

        /// <summary>
        /// The hold slot the selection enters left of a frame: the last slot of the page's top row from the upper
        /// frame, of its bottom row from the lower one; -1 for a frame with nothing left of it or an empty hold.
        /// </summary>
        public static int HoldEntryLeftOf(int device, int unlocked, int page)
        {
            int rows = FaceLayout.RowsOnPage(page, unlocked);
            if (rows <= 0 || (device != FaceDevices.Cutter && device != FaceDevices.SocketOne))
            {
                return -1;
            }

            int row = FaceLayout.IsTopFrame(device) ? 0 : rows - 1;
            int first = page * FaceLayout.PageSize + row * FaceLayout.Columns;
            return Mathf.Min(first + FaceLayout.Columns - 1, unlocked - 1);
        }
    }
}
