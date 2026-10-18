using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// B9 night-ready (docs/design/opening-v2.md): a built battery bank wired into the powered pad's grid, never the
    /// charge it holds. At last light the array yields nothing, so a stored-charge condition could never be met; the
    /// bank built and connected is enough, and a bank standing off the grid is not. The pad and the bank reach the grid
    /// themselves, so a bank set down beside its pad (the autoplay bot's 7.6 m, past the array's 14 m) connects.
    /// </summary>
    public sealed class NightReadyTests
    {
        // The autoplay bot's B9 base (2026-10-18): the array 6.7 m east of the pad, the bank 7.6 m west of it.
        private const float ArrayEast = 6.7f;
        private const float BankBesidePad = 7.6f;
        private const float OffTheGrid = 40f;
        private const float AMinute = 60f;
        private const float ReserveCells = 1f;

        private StructureCatalog _catalog;
        private BuildingTuning _tuning;
        private OpeningTuning _opening;
        private PlacedBase _base;
        private PadChargers _pads;

        [SetUp]
        public void SetUp()
        {
            _catalog = BaseStructureRecipes.CreateCatalog();
            _tuning = ScriptableObject.CreateInstance<BuildingTuning>();
            _opening = ScriptableObject.CreateInstance<OpeningTuning>();
            var events = new EventBus();
            _base = new PlacedBase(_catalog, _tuning, events);
            _pads = new PadChargers(_base, new LanderPort(events, ReserveCells), Vector3.zero, _opening);
            Built(StructureIds.ChargingPad, Vector3.zero);
            Built(StructureIds.SolarArray, Vector3.right * ArrayEast);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                Object.DestroyImmediate(_catalog.StructureAt(i));
            }

            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_opening);
        }

        [Test]
        public void ABankSetDownBesideThePad_IsNightReady_WithNoSupplyAndNoCharge()
        {
            Built(StructureIds.BatteryBank, Vector3.left * BankBesidePad);
            _base.StepPower(AMinute, 0f);

            Assert.IsTrue(_pads.PowerOn, "the array is wired to the pad");
            Assert.IsTrue(_pads.NightReady, "the bank is built and wired in: night-ready, whatever it holds");
            for (int i = 0; i < _base.Count; i++)
            {
                PlacedStructure structure = _base.StructureAt(i);
                Assert.AreEqual(0f, structure.StoredWattHours, $"{structure.Definition.Id} holds no charge");
            }
        }

        [Test]
        public void ABankOffTheGrid_IsNotNightReady()
        {
            Built(StructureIds.BatteryBank, Vector3.forward * OffTheGrid);
            _base.StepPower(AMinute, 0f);

            Assert.IsTrue(_pads.PowerOn);
            Assert.IsFalse(_pads.NightReady, "a bank that feeds nothing does not make the base ready for the night");
        }

        [Test]
        public void ABankStillBeingWelded_IsNotNightReady()
        {
            _base.PlaceUnchecked(_catalog.Find(StructureIds.BatteryBank),
                new StructurePose(Vector3.left * BankBesidePad, 0f));
            _base.StepPower(AMinute, 0f);

            Assert.IsFalse(_pads.NightReady, "only a built bank counts");
        }

        private void Built(string id, Vector3 position)
        {
            _base.PlaceUnchecked(_catalog.Find(id), new StructurePose(position, 0f), 1f);
        }
    }
}
