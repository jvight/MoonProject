using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The build ghost's grid link preview: a bank's ghost beside the pad shows a cable to the nearest hub, one far out
    /// shows the loose end, and wherever the preview says linked or loose the built grid agrees.
    /// </summary>
    public sealed class GridLinkPreviewTests
    {
        private const float ArrayEast = 6.7f;
        private const float BankBesidePad = 7.6f;
        private const float OffTheGrid = 40f;
        private const float SweepStep = 0.5f;
        private const float SweepEnd = 30f;

        private StructureCatalog _catalog;
        private BuildingTuning _tuning;
        private PlacedBase _base;

        [SetUp]
        public void SetUp()
        {
            _catalog = BaseStructureRecipes.CreateCatalog();
            _tuning = ScriptableObject.CreateInstance<BuildingTuning>();
            _base = new PlacedBase(_catalog, _tuning, new EventBus());
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
        }

        [Test]
        public void ABanksGhostBesideThePad_ShowsACableToThePad()
        {
            Assert.IsTrue(GridLinkPreview.TryFindLink(_base, Bank, Vector3.left * BankBesidePad, out Vector3 hub));
            Assert.AreEqual(Vector3.zero, hub, "the nearest hub in reach is the pad");
        }

        [Test]
        public void ABanksGhostFarOut_ShowsTheLooseEnd()
        {
            Assert.IsFalse(GridLinkPreview.TryFindLink(_base, Bank, Vector3.left * OffTheGrid, out _));
        }

        [Test]
        public void ThePreview_AgreesWithTheBuiltGrid_AtEveryDistance()
        {
            PlacedStructure pad = _base.StructureAt(0);
            for (float distance = SweepStep; distance <= SweepEnd; distance += SweepStep)
            {
                Vector3 at = Vector3.left * distance;
                bool previewed = GridLinkPreview.TryFindLink(_base, Bank, at, out _);
                var trial = new PlacedBase(_catalog, _tuning, new EventBus());
                int trialPad = trial.PlaceUnchecked(_catalog.Find(StructureIds.ChargingPad),
                    new StructurePose(pad.Pose.Position, 0f), 1f);
                trial.PlaceUnchecked(_catalog.Find(StructureIds.SolarArray),
                    new StructurePose(Vector3.right * ArrayEast, 0f), 1f);
                int bank = trial.PlaceUnchecked(Bank, new StructurePose(at, 0f), 1f);
                Assert.AreEqual(trial.PowerGrid.AreConnected(trialPad, bank), previewed, $"{distance} m from the pad");
            }
        }

        private StructureDefinition Bank => _catalog.Find(StructureIds.BatteryBank);

        private void Built(string id, Vector3 position)
        {
            _base.PlaceUnchecked(_catalog.Find(id), new StructurePose(position, 0f), 1f);
        }
    }
}
