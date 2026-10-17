using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's start kit (docs/design/opening-v2.md §10): what works and what is broken at the wake, the B6 igniter fit
    /// (only with the igniter in the hold and 07 on its pad), the cutter's refusal once per press, and the save.
    /// </summary>
    public sealed class RoverStartKitTests
    {
        private const string Igniter = "part.igniter";

        private readonly List<RoverSystemChanged> _changes = new List<RoverSystemChanged>();
        private readonly List<CutterRefused> _refused = new List<CutterRefused>();
        private EventBus _events;
        private MaterialCatalog _catalog;
        private CargoHold _hold;
        private bool _docked;
        private RoverStartKit _kit;

        [SetUp]
        public void SetUp()
        {
            _changes.Clear();
            _refused.Clear();
            _events = new EventBus();
            _events.Subscribe<RoverSystemChanged>(_changes.Add);
            _events.Subscribe<CutterRefused>(_refused.Add);
            _catalog = CargoFixtures.Catalog();
            _hold = CargoFixtures.Stock(_events, _catalog).Hold;
            _docked = false;
            _kit = new RoverStartKit(_events, _hold, () => _docked, Igniter);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void AtTheWake_DriveLampsScanAndBuildWork_TheRestIsBroken()
        {
            Assert.AreEqual(RoverSystemState.Ok, _kit.StateOf(RoverSystem.Wheels));
            Assert.AreEqual(RoverSystemState.Ok, _kit.StateOf(RoverSystem.Lamps));
            Assert.AreEqual(RoverSystemState.Ok, _kit.StateOf(RoverSystem.Scan));
            Assert.AreEqual(RoverSystemState.Ok, _kit.StateOf(RoverSystem.Assembly));
            Assert.AreEqual(RoverSystemState.Fault, _kit.StateOf(RoverSystem.Cutter), "no igniter");
            Assert.AreEqual(RoverSystemState.Jammed, _kit.StateOf(RoverSystem.SolarWing));
            Assert.AreEqual(RoverSystemState.ReceiveOnly, _kit.StateOf(RoverSystem.Antenna));
            Assert.AreEqual(RoverSystemState.Empty, _kit.StateOf(RoverSystem.Sockets));

            _kit.Announce();
            Assert.AreEqual(8, _changes.Count, "the readout states every line once");
            Assert.IsTrue(_changes.TrueForAll(changed => changed.Restored));
        }

        [Test]
        public void TheIgniter_FitsOnlyFromTheHold_On07sPad_AndTurnsTheCutterOk()
        {
            Assert.IsFalse(_kit.TryFitIgniter(), "no igniter in the hold");
            _docked = true;
            Assert.IsFalse(_kit.CanFitIgniter, "docked, but still no igniter");
            Assert.IsTrue(_hold.TryAddPart(Igniter));
            _docked = false;
            Assert.IsFalse(_kit.TryFitIgniter(), "the igniter, but off the pad");
            Assert.AreEqual(RoverSystemState.Fault, _kit.StateOf(RoverSystem.Cutter));

            _docked = true;
            Assert.IsTrue(_kit.CanFitIgniter);
            Assert.IsTrue(_kit.TryFitIgniter());
            Assert.AreEqual(RoverSystemState.Ok, _kit.StateOf(RoverSystem.Cutter));
            Assert.IsFalse(_hold.HasPart(Igniter), "it left the hold for the cutter");
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(RoverSystem.Cutter, _changes[0].System);
            Assert.AreEqual(RoverSystemState.Ok, _changes[0].State);
            Assert.IsFalse(_changes[0].Restored, "live: the pilot light and the done tone");
            Assert.IsFalse(_kit.CanFitIgniter, "fitted once");
            Assert.AreEqual(RoverSystemState.Jammed, _kit.StateOf(RoverSystem.SolarWing), "the wing stays jammed");
        }

        [Test]
        public void TheFaultedCutter_RefusesEachPressOnce()
        {
            var tuning = ScriptableObject.CreateInstance<CutterTuning>();
            var cutter = new Cutter(_events, new NoAbilities(), tuning);
            cutter.ConnectSystems(_kit);
            Assert.IsFalse(cutter.Ready);
            for (int frame = 0; frame < 5; frame++)
            {
                cutter.Refuse(Vector3.zero);
                cutter.Step();
            }

            Assert.AreEqual(1, _refused.Count, "a held press is refused once");
            cutter.Step();
            cutter.Refuse(Vector3.zero);
            Assert.AreEqual(2, _refused.Count, "a new press is refused again");

            _hold.TryAddPart(Igniter);
            _docked = true;
            _kit.TryFitIgniter();
            Assert.IsTrue(cutter.Ready, "the igniter fitted, the cutter cuts");
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void ASave_KeepsTheFittedIgniter_AndLaysACarriedOneBackIntoTheHold()
        {
            _hold.TryAddPart(Igniter);
            StartKitSaveData carried = _kit.Capture();
            Assert.IsTrue(carried.igniterCarried);
            Assert.IsFalse(carried.igniterFitted);

            _hold.RemovePart(Igniter);
            var loaded = new RoverStartKit(_events, _hold, () => _docked, Igniter);
            loaded.Restore(carried);
            loaded.Announce();
            Assert.IsTrue(_hold.HasPart(Igniter), "the carried igniter is back in the hold as the session starts");

            _docked = true;
            loaded.TryFitIgniter();
            var reloaded = new RoverStartKit(_events, _hold, () => _docked, Igniter);
            reloaded.Restore(loaded.Capture());
            Assert.IsTrue(reloaded.IgniterFitted);
            Assert.AreEqual(RoverSystemState.Ok, reloaded.StateOf(RoverSystem.Cutter));
        }

        private sealed class NoAbilities : IRoverAbilities
        {
            public bool Has(RoverAbility ability)
            {
                return false;
            }

            public void Grant(RoverAbility ability)
            {
            }

            public void Revoke(RoverAbility ability)
            {
            }
        }
    }
}
