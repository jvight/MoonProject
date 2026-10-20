using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The socket rack at the pad (opening v3 4.3, <see cref="SocketRack"/>): held until the west signal answers, shown
    /// at the next dock, an outline per part seen and a filled bay per part fitted, and all of it kept by a save.
    /// </summary>
    public sealed class SocketRackTests
    {
        private const int Headlamp = 0;
        private const int CellPack = 1;
        private const int HexAdapter = 2;
        private const string HexBlueprint = "rover.hex_adapter";

        private SocketRack _rack;

        [SetUp]
        public void SetUp()
        {
            _rack = new SocketRack(new[]
            {
                new RackBaySettings("rack.headlamp", true, string.Empty, RoverKitPiece.LampBar),
                new RackBaySettings("rack.cell_pack", true, string.Empty, RoverKitPiece.BatterySpine),
                new RackBaySettings("rack.hex_adapter", false, HexBlueprint, RoverKitPiece.TapeDeck),
            });
        }

        [Test]
        public void BeforeTheWestSignal_TheRackIsHeld_EvenAtADock()
        {
            Assert.IsFalse(_rack.Docked(), "a dock before the release shows nothing");
            Assert.IsFalse(_rack.Shown);
            AssertStates(RackBayState.Hidden, RackBayState.Hidden, RackBayState.Hidden);
        }

        [Test]
        public void AfterTheWestSignal_TheRackShowsAtTheNextDock_WithTheOutlinesOfThePartsSeen()
        {
            _rack.Release();
            Assert.IsFalse(_rack.Shown, "the release alone does not show it: the next dock does");
            AssertStates(RackBayState.Hidden, RackBayState.Hidden, RackBayState.Hidden);

            Assert.IsTrue(_rack.Docked(), "it appears at the dock");
            AssertStates(RackBayState.Outline, RackBayState.Outline, RackBayState.Hidden);
            Assert.IsFalse(_rack.Docked(), "and only once");
        }

        [Test]
        public void TheHexAdapter_ShowsItsOutline_OnceAFragmentOfItIsScanned()
        {
            ShowTheRack();
            _rack.BlueprintScanned("rover.something_else");
            Assert.AreEqual(RackBayState.Hidden, _rack.StateOf(HexAdapter));

            int version = _rack.Version;
            _rack.BlueprintScanned(HexBlueprint);
            Assert.AreEqual(RackBayState.Outline, _rack.StateOf(HexAdapter));
            Assert.Greater(_rack.Version, version, "views redraw");
        }

        [Test]
        public void APartSeenBeforeTheRackIsOut_ShowsItsOutlineWhenTheRackAppears()
        {
            _rack.BlueprintScanned(HexBlueprint);
            Assert.AreEqual(RackBayState.Hidden, _rack.StateOf(HexAdapter), "no rack yet");
            ShowTheRack();
            Assert.AreEqual(RackBayState.Outline, _rack.StateOf(HexAdapter));
        }

        [Test]
        public void FittingAPart_FillsItsBay_AndOnlyItsBay()
        {
            ShowTheRack();
            _rack.Fitted(RoverKitPiece.LampBar);
            AssertStates(RackBayState.Filled, RackBayState.Outline, RackBayState.Hidden);

            _rack.Fitted(RoverKitPiece.CargoRack);
            AssertStates(RackBayState.Filled, RackBayState.Outline, RackBayState.Hidden);

            _rack.Fitted(RoverKitPiece.TapeDeck);
            Assert.AreEqual(RackBayState.Filled, _rack.StateOf(HexAdapter), "a fitted part is seen too");
        }

        [Test]
        public void ASave_KeepsTheRackOut_AndWhatWasSeenAndFitted()
        {
            ShowTheRack();
            _rack.BlueprintScanned(HexBlueprint);
            _rack.Fitted(RoverKitPiece.BatterySpine);
            RackSaveData saved = _rack.Capture();

            SetUp();
            _rack.Restore(saved);
            AssertStates(RackBayState.Outline, RackBayState.Filled, RackBayState.Outline);
            Assert.IsFalse(_rack.Docked(), "a loaded rack that is out does not appear again");
        }

        [Test]
        public void ANewGame_RestoresNothing()
        {
            _rack.Restore(null);
            _rack.Release();
            Assert.IsTrue(_rack.Docked());
            AssertStates(RackBayState.Outline, RackBayState.Outline, RackBayState.Hidden);
        }

        [Test]
        public void EveryBay_IsSeenSomehow()
        {
            Assert.IsNull(new RackBaySettings("rack.a", true, string.Empty, RoverKitPiece.LampBar).Validate());
            Assert.IsNull(new RackBaySettings("rack.b", false, HexBlueprint, RoverKitPiece.LampBar).Validate());
            Assert.IsNotNull(new RackBaySettings("rack.c", false, string.Empty, RoverKitPiece.LampBar).Validate());
            Assert.IsNotNull(new RackBaySettings(string.Empty, true, string.Empty, RoverKitPiece.LampBar).Validate());
        }

        private void ShowTheRack()
        {
            _rack.Release();
            Assert.IsTrue(_rack.Docked());
        }

        private void AssertStates(RackBayState headlamp, RackBayState cellPack, RackBayState hexAdapter)
        {
            Assert.AreEqual(headlamp, _rack.StateOf(Headlamp), "the SSR-3 headlamp");
            Assert.AreEqual(cellPack, _rack.StateOf(CellPack), "the cell pack");
            Assert.AreEqual(hexAdapter, _rack.StateOf(HexAdapter), "the hex adapter");
        }
    }
}
