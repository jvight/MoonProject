using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The socket rack at the pad (opening v3 4.3, <see cref="SocketRack"/>): hidden until the release reveals it, then
    /// an outline per part seen and a filled bay per part fitted, the bays kept by a save.
    /// </summary>
    public sealed class SocketRackTests
    {
        private const int Headlamp = 0;
        private const int CellPack = 1;
        private const int HexAdapter = 2;
        private const string HexBlueprint = "rover.hex_adapter";

        private static readonly Vector3 Dock = new Vector3(12f, 1f, -30f);
        private static readonly Quaternion Heading = Quaternion.Euler(0f, 180f, 0f);

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
        public void BeforeTheReveal_EveryBayIsHidden()
        {
            Assert.IsFalse(_rack.Shown);
            AssertStates(RackBayState.Hidden, RackBayState.Hidden, RackBayState.Hidden);
        }

        [Test]
        public void TheReveal_ShowsTheOutlinesOfThePartsSeen_Once()
        {
            Assert.IsTrue(_rack.Reveal(Dock, Heading), "it appears");
            AssertStates(RackBayState.Outline, RackBayState.Outline, RackBayState.Hidden);
            Assert.AreEqual(Dock, _rack.Position, "beside the dock the release revealed it at");
            Assert.AreEqual(Heading, _rack.Rotation);
            Assert.IsFalse(_rack.Reveal(Vector3.zero, Quaternion.identity), "and only once, where it first stood");
            Assert.AreEqual(Dock, _rack.Position);
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
        public void ASave_KeepsWhatWasSeenAndFitted_AndLeavesTheRevealToTheRelease()
        {
            ShowTheRack();
            _rack.BlueprintScanned(HexBlueprint);
            _rack.Fitted(RoverKitPiece.BatterySpine);
            RackSaveData saved = _rack.Capture();

            SetUp();
            _rack.Restore(saved);
            Assert.IsFalse(_rack.Shown, "out or not is the release's to say");
            Assert.IsTrue(_rack.RevealSaved(), "a done release brings it back");
            Assert.AreEqual(Dock, _rack.Position, "where the save placed it");
            Assert.AreEqual(Heading, _rack.Rotation);
            AssertStates(RackBayState.Outline, RackBayState.Filled, RackBayState.Outline);
        }

        [Test]
        public void ASaveThatPlacedTheRackNowhere_LeavesItHidden()
        {
            _rack.Restore(new RackSaveData { seen = new string[0], filled = new string[0] });
            Assert.IsFalse(_rack.RevealSaved(), "no other place it may stand");
            Assert.IsFalse(_rack.Shown);
        }

        [Test]
        public void ANewGame_RestoresNothing()
        {
            _rack.Restore(null);
            ShowTheRack();
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
            Assert.IsTrue(_rack.Reveal(Dock, Heading));
        }

        private void AssertStates(RackBayState headlamp, RackBayState cellPack, RackBayState hexAdapter)
        {
            Assert.AreEqual(headlamp, _rack.StateOf(Headlamp), "the SSR-3 headlamp");
            Assert.AreEqual(cellPack, _rack.StateOf(CellPack), "the cell pack");
            Assert.AreEqual(hexAdapter, _rack.StateOf(HexAdapter), "the hex adapter");
        }
    }
}
