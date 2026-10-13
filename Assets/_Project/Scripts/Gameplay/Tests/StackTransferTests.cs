using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The docked transfer view's move (0.5.5 §3): one stack at a time between 07's hold and home's shelves, only while
    /// 07 rests on the dock and the dock's own unload has finished; the hold and the shelves announce it and the totals
    /// stay.
    /// </summary>
    public sealed class StackTransferTests
    {
        private EventBus _events;
        private MaterialCatalog _catalog;
        private CargoTuning _tuning;
        private MaterialStock _stock;
        private FakeDock _dock;
        private DockUnload _unload;
        private StackTransfer _transfer;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _catalog = CargoFixtures.Catalog();
            _stock = CargoFixtures.Stock(_events, _catalog);
            _tuning = ScriptableObject.CreateInstance<CargoTuning>();
            _dock = new FakeDock { IsDocked = true };
            _unload = new DockUnload(_events, _stock, _tuning, new MemorySave());
            _transfer = new StackTransfer(_stock, _catalog, _dock, _unload);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Docked_OnePressMovesOneStackHome_AndBackAboard()
        {
            var cargo = new List<CargoChanged>();
            var home = new List<HomeStorageChanged>();
            _events.Subscribe<CargoChanged>(cargo.Add);
            _events.Subscribe<HomeStorageChanged>(home.Add);
            _stock.Collect(SalvageMaterial.Metal, 6);

            Assert.IsTrue(_transfer.TryMove(SalvageMaterial.Metal, true, out int moved));
            Assert.AreEqual(_catalog.StackSize(SalvageMaterial.Metal), moved, "one stack");
            Assert.AreEqual(2, _stock.Hold.Units(SalvageMaterial.Metal));
            Assert.AreEqual(4, _stock.Home.Units(SalvageMaterial.Metal));
            Assert.Greater(cargo.Count, 0, "the hold announces it");
            Assert.Greater(home.Count, 0, "the shelves announce it");

            Assert.IsTrue(_transfer.TryMove(SalvageMaterial.Metal, true, out moved));
            Assert.AreEqual(2, moved, "the rest of the metal aboard");
            Assert.IsFalse(_transfer.TryMove(SalvageMaterial.Metal, true, out moved), "none left aboard");
            Assert.AreEqual(0, moved);

            Assert.IsTrue(_transfer.TryMove(SalvageMaterial.Metal, false, out moved));
            Assert.AreEqual(4, moved, "a stack back aboard for the trip");
            Assert.AreEqual(6, _stock.Metal, "the totals never change");
        }

        [Test]
        public void Undocked_NothingMoves()
        {
            _stock.Collect(SalvageMaterial.Wiring, 3);
            _dock.IsDocked = false;
            Assert.IsFalse(_transfer.TryMove(SalvageMaterial.Wiring, true, out int moved));
            Assert.AreEqual(0, moved);
            Assert.AreEqual(3, _stock.Hold.Units(SalvageMaterial.Wiring));
        }

        [Test]
        public void AFullHold_TakesNothingAboard()
        {
            _stock.Deliver(SalvageMaterial.Optics, 4);
            _stock.Collect(SalvageMaterial.Metal, CargoFixtures.HoldSlots * 4);
            Assert.IsFalse(_transfer.TryMove(SalvageMaterial.Optics, false, out int moved));
            Assert.AreEqual(0, moved);
            Assert.AreEqual(4, _stock.Home.Units(SalvageMaterial.Optics));
        }

        [Test]
        public void WhileTheDocksUnloadRuns_NothingMoves_ThenTheViewOpensOnItsResult()
        {
            _stock.Collect(SalvageMaterial.Metal, 3);
            _unload.Step(true, Vector3.zero, 0f);
            Assert.IsTrue(_unload.Running, "07 just docked: the hatch opens");
            Assert.IsFalse(_transfer.TryMove(SalvageMaterial.Metal, true, out int moved), "the unload runs first");
            Assert.AreEqual(0, moved);

            _unload.Step(true, Vector3.zero, _tuning.UnloadLeadSeconds + _tuning.UnloadStepSeconds);
            Assert.IsFalse(_unload.Running, "the one slot rode the belt home");
            Assert.AreEqual(3, _stock.Home.Units(SalvageMaterial.Metal));
            Assert.IsTrue(_transfer.TryMove(SalvageMaterial.Metal, false, out moved));
            Assert.AreEqual(3, moved, "taken aboard for the trip");
            _unload.Step(true, Vector3.zero, _tuning.UnloadLeadSeconds + _tuning.UnloadStepSeconds * 4f);
            Assert.AreEqual(3, _stock.Hold.Units(SalvageMaterial.Metal), "and it stays aboard while 07 rests");
        }

        private sealed class FakeDock : IDockState
        {
            public bool IsDocked { get; set; }
        }

        private sealed class MemorySave : ISaveService
        {
            public string FilePath => "memory";

            public bool IsLoaded => true;

            public SaveLoadResult LoadResult => SaveLoadResult.NoSave;

            public event Action Saved
            {
                add { }
                remove { }
            }

            public IDisposable Register(ISaveSection section)
            {
                throw new NotSupportedException("The transfer registers no sections.");
            }

            public bool SaveNow()
            {
                return true;
            }

            public string PutAway()
            {
                throw new NotSupportedException("The transfer never starts a new game.");
            }
        }
    }
}
