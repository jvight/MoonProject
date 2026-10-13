using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The "Tab · Fit" chip: right after the first craft, where the part fits, until the hold is opened or a while
    /// passes, and for good after the first fit.
    /// </summary>
    public sealed class FitHintTests
    {
        private const float Frame = 1f / 60f;

        private readonly StubEquipment _equipment = new StubEquipment();
        private StargazeSettings _settings;
        private PromptLedger _ledger;
        private FitHint _hint;

        [SetUp]
        public void SetUp()
        {
            _settings = new StargazeSettings();
            _ledger = new PromptLedger(new PromptSettings());
            _equipment.Parts = 0;
            _equipment.AtStation = false;
            _hint = new FitHint(_settings, _ledger, _equipment);
        }

        [Test]
        public void NothingWaiting_NoChip()
        {
            _equipment.AtStation = true;
            _hint.Tick(Frame, true, false);
            Assert.IsFalse(_hint.WantsShown);
        }

        [Test]
        public void ACraftedPartAtTheBay_ShowsTheChip_UntilTabIsOpened()
        {
            _equipment.Parts = 1;
            _equipment.AtStation = true;
            _hint.Tick(Frame, true, false);
            Assert.IsTrue(_hint.WantsShown);
            _hint.Tick(Frame, true, true);
            Assert.IsFalse(_hint.WantsShown, "opening Tab answers it");
        }

        [Test]
        public void AwayFromTheBay_TheChipWaits()
        {
            _equipment.Parts = 1;
            _equipment.AtStation = false;
            _hint.Tick(Frame, true, false);
            Assert.IsFalse(_hint.WantsShown);
            _equipment.AtStation = true;
            _hint.Tick(Frame, true, false);
            Assert.IsTrue(_hint.WantsShown, "it comes the next time 07 is at the bay with the part");
        }

        [Test]
        public void TheChip_LeavesAfterItsHoldTime_AndRetiresForGoodAfterTheFirstFit()
        {
            _equipment.Parts = 1;
            _equipment.AtStation = true;
            _hint.Tick(Frame, true, false);
            _hint.Tick(_settings.HintHoldSeconds + 1f, true, false);
            Assert.IsFalse(_hint.WantsShown);

            _ledger.RecordFitTaught();
            _hint.Tick(Frame, true, false);
            Assert.IsFalse(_hint.WantsShown, "retired after the first fit");
            Assert.IsFalse(_hint.Wants());
        }

        [Test]
        public void FitTaught_IsSaved()
        {
            _ledger.RecordFitTaught();
            var restored = new PromptLedger(new PromptSettings());
            restored.Restore(_ledger.Capture());
            Assert.IsTrue(restored.FitTaught);
        }

        private sealed class StubEquipment : IRoverEquipment
        {
            public int Parts { get; set; }

            public bool AtStation { get; set; }

            public int RowCapacity => 4;

            public int RowCount => Parts;

            public RoverSocket SocketOf(RoverKitPiece part)
            {
                return RoverSocket.Belly;
            }

            public bool TryGetFitted(RoverSocket socket, out RoverKitPiece part)
            {
                part = default;
                return false;
            }

            public bool IsKnown(RoverSocket socket)
            {
                return false;
            }

            public RoverKitPiece RowPart(int index)
            {
                return RoverKitPiece.HoverCoils;
            }

            public bool CanFit(RoverKitPiece part)
            {
                return AtStation;
            }

            public bool TryFit(int rowIndex)
            {
                return false;
            }

            public bool TryUnequip(RoverSocket socket)
            {
                return false;
            }
        }
    }
}
