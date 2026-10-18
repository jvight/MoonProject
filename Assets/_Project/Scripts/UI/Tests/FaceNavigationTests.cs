using NUnit.Framework;
using UnityEngine.UIElements;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The arrows on 07's face: across and down the hold grid (turning its page past the last row shown), right off
    /// the grid into the nearer left frame, around the four frames, and left off them back into the grid's nearest
    /// row on the page shown; an edge that leads nowhere keeps the selection.
    /// </summary>
    public sealed class FaceNavigationTests
    {
        private const NavigationMoveEvent.Direction Left = NavigationMoveEvent.Direction.Left;
        private const NavigationMoveEvent.Direction Right = NavigationMoveEvent.Direction.Right;
        private const NavigationMoveEvent.Direction Up = NavigationMoveEvent.Direction.Up;
        private const NavigationMoveEvent.Direction Down = NavigationMoveEvent.Direction.Down;

        [TestCase(0, Right, 8, 1, -1)]
        [TestCase(1, Left, 8, 0, -1)]
        [TestCase(0, Left, 8, -1, -1)]
        [TestCase(0, Down, 8, 4, -1)]
        [TestCase(4, Up, 8, 0, -1)]
        [TestCase(2, Up, 8, -1, -1)]
        [TestCase(5, Down, 8, -1, -1)]
        [TestCase(3, Right, 8, -1, FaceDevices.Cutter)]
        [TestCase(7, Right, 8, -1, FaceDevices.Cutter)]
        [TestCase(11, Right, 12, -1, FaceDevices.SocketOne)]
        [TestCase(9, Down, 16, 13, -1)]
        [TestCase(13, Up, 16, 9, -1)]
        [TestCase(3, Down, 6, 5, -1)]
        [TestCase(5, Right, 6, -1, FaceDevices.Cutter)]
        public void FromAHoldSlot_TheArrowsWalkTheGrid_AndRightLeavesItForTheNearerFrame(int cell,
            NavigationMoveEvent.Direction direction, int unlocked, int expectedCell, int expectedDevice)
        {
            FaceNavigation.FromHoldCell(cell, direction, unlocked, out int nextCell, out int nextDevice);
            Assert.AreEqual(expectedCell, nextCell, "the slot");
            Assert.AreEqual(expectedDevice, nextDevice, "the device");
        }

        [Test]
        public void DownPastAPagesLastRow_TurnsThePage_AndUpComesBack()
        {
            int last = FaceLayout.PageSize - 1;
            FaceNavigation.FromHoldCell(last, Down, 24, out int down, out _);
            Assert.AreEqual(last + FaceLayout.Columns, down);
            Assert.AreEqual(1, FaceLayout.PageOf(down), "the next page");
            FaceNavigation.FromHoldCell(down, Up, 24, out int up, out _);
            Assert.AreEqual(last, up);
            Assert.AreEqual(0, FaceLayout.PageOf(up));
        }

        [TestCase(FaceDevices.Cutter, Right, 8, 0, -1, FaceDevices.Scan)]
        [TestCase(FaceDevices.Cutter, Down, 8, 0, -1, FaceDevices.SocketOne)]
        [TestCase(FaceDevices.Scan, Up, 8, 0, -1, -1)]
        [TestCase(FaceDevices.Cutter, Left, 8, 0, 3, -1)]
        [TestCase(FaceDevices.SocketOne, Left, 8, 0, 7, -1)]
        [TestCase(FaceDevices.SocketOne, Left, 16, 0, 11, -1)]
        [TestCase(FaceDevices.SocketOne, Left, 16, 1, 15, -1)]
        [TestCase(FaceDevices.Cutter, Left, 16, 1, 15, -1)]
        [TestCase(FaceDevices.SocketOne, Left, 6, 0, 5, -1)]
        [TestCase(FaceDevices.Cutter, Left, 0, 0, -1, -1)]
        [TestCase(FaceDevices.Scan, Left, 8, 0, -1, FaceDevices.Cutter)]
        [TestCase(FaceDevices.SocketTwo, Right, 8, 0, -1, -1)]
        public void FromAFrame_TheArrowsWalkTheFrames_AndLeftReturnsToTheGridsNearestRow(int device,
            NavigationMoveEvent.Direction direction, int unlocked, int page, int expectedCell, int expectedDevice)
        {
            FaceNavigation.FromDevice(device, direction, unlocked, page, out int nextCell, out int nextDevice);
            Assert.AreEqual(expectedCell, nextCell, "the slot");
            Assert.AreEqual(expectedDevice, nextDevice, "the device");
        }

        [Test]
        public void RightThenLeft_ReturnsToTheSameRowOfTheGrid()
        {
            foreach (int cell in new[] { 3, 7, 11 })
            {
                FaceNavigation.FromHoldCell(cell, Right, 12, out _, out int device);
                FaceNavigation.FromDevice(device, Left, 12, 0, out int back, out _);
                int row = cell / FaceLayout.Columns;
                Assert.AreEqual(device == FaceDevices.Cutter ? 0 : 2, back / FaceLayout.Columns,
                    $"slot {cell} (row {row}) leaves to device {device}");
            }
        }
    }
}
