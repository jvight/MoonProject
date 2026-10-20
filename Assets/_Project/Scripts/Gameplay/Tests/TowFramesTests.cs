using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The battery bank on the habitat's power module (opening v3 R6, slice 3.2c): its recipe is the towed module and
    /// Wiring 3; the module is home only on the apron, off the cable, and still in the world; the bank's frame placed
    /// on it takes it in at the frame's pose (a loaded game's too), and taking the bank down gives it back.
    /// </summary>
    public sealed class TowFramesTests
    {
        private static readonly Vector3 Home = new Vector3(0f, 0f, 100f);

        private readonly List<GameObject> _made = new List<GameObject>();
        private EventBus _events;
        private TowSettings _settings;
        private StructureCatalog _catalog;
        private TowLine _tow;
        private TowFrames _frames;
        private TowableBody _module;
        private Transform _winch;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _settings = new TowSettings();
            _catalog = BaseStructureRecipes.CreateCatalog();
            _winch = Make("Winch").transform;
            _tow = new TowLine(_events, _settings, _winch, new StillRover(), Home);
            _module = Module(Home + new Vector3(_settings.HomeRadius - 1f, 0f, 0f));
            _tow.Add(_module);
            _frames = new TowFrames(_events, _catalog, _tow);
        }

        [TearDown]
        public void TearDown()
        {
            _frames.Dispose();
            foreach (GameObject made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
            for (int i = 0; i < _catalog.Count; i++)
            {
                Object.DestroyImmediate(_catalog.StructureAt(i));
            }

            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void TheBank_IsBuiltOnThePowerModule_WithWiring3()
        {
            StructureDefinition bank = _catalog.Find(StructureIds.BatteryBank);
            Assert.AreEqual(TowLoads.PowerModule, bank.FrameLoad, "the towed module is the bank's frame");
            Assert.AreEqual(new Recipe(0, 3, 0), bank.BuildCost, "and Wiring 3 wires it in: no metal");
            Assert.AreEqual(string.Empty, _catalog.Find(StructureIds.ChargingPad).FrameLoad, "the pad needs none");
        }

        [Test]
        public void TheModule_IsHomeOnlyOnTheApron_OffTheCable()
        {
            Assert.IsTrue(_tow.TryFindHome(TowLoads.PowerModule, out Transform root));
            Assert.AreEqual(_module.Root, root);

            _winch.position = _module.Hitch;
            _tow.Step(true);
            Assert.IsFalse(_tow.TryFindHome(TowLoads.PowerModule, out _), "still on the cable");
            _tow.Step(true);
            Assert.IsTrue(_tow.TryFindHome(TowLoads.PowerModule, out _), "let go on the apron");

            _module.Place(Home + new Vector3(_settings.HomeRadius + 2f, 0f, 0f), Quaternion.identity);
            Assert.IsFalse(_tow.TryFindHome(TowLoads.PowerModule, out _), "beyond the apron");
            Assert.IsFalse(_tow.TryFindHome("no.such.load", out _));
        }

        [Test]
        public void TheBanksFrame_TakesTheModuleIn_AndATakeDownGivesItBack()
        {
            var at = new Vector3(2f, 0f, 101f);
            _events.Publish(new StructurePlaced(7, StructureIds.BatteryBank, at, 30f));
            Assert.IsFalse(_module.Root.gameObject.activeSelf, "set into the bank");
            Assert.AreEqual(at, _module.Root.position, "where the bank stands");
            Assert.AreEqual(30f, _module.Root.rotation.eulerAngles.y, 1e-3f);
            Assert.IsFalse(_tow.TryFindHome(TowLoads.PowerModule, out _), "no second bank on it");
            _winch.position = _module.Hitch;
            _tow.Step(true);
            Assert.IsFalse(_tow.IsTowing, "nor is it hooked again");

            _events.Publish(new StructureRemoved(7, StructureIds.BatteryBank, at));
            Assert.IsTrue(_module.Root.gameObject.activeSelf, "taken down: the module stands where the bank stood");
            Assert.IsTrue(_tow.TryFindHome(TowLoads.PowerModule, out _));
        }

        [Test]
        public void ALoadedGamesBank_TakesItsModuleIn_AndOtherStructuresLeaveItBe()
        {
            _events.Publish(new StructurePlaced(3, StructureIds.ChargingPad, Home, 0f, true));
            Assert.IsTrue(_module.Root.gameObject.activeSelf, "the pad is built on no load");
            _events.Publish(new StructurePlaced(4, StructureIds.BatteryBank, Home, 0f, true));
            Assert.IsFalse(_module.Root.gameObject.activeSelf, "a restored bank has its module in it");
        }

        private TowableBody Module(Vector3 at)
        {
            GameObject module = Make(TowLoads.PowerModule);
            module.transform.position = at;
            module.AddComponent<BoxCollider>();
            var body = module.AddComponent<Rigidbody>();
            Transform hitch = Make(TowLoads.HitchNode).transform;
            hitch.SetParent(module.transform, false);
            return new TowableBody(TowLoads.PowerModule, body, hitch, 250f, _settings);
        }

        private GameObject Make(string name)
        {
            var made = new GameObject(name);
            _made.Add(made);
            return made;
        }

        private sealed class StillRover : IRoverState
        {
            public Vector3 Position => Vector3.zero;
            public Quaternion Rotation => Quaternion.identity;
            public Vector3 Velocity => Vector3.zero;
            public float Speed => 0f;
            public float NormalizedSpeed => 0f;
            public Vector2 DriveInput => Vector2.zero;
            public bool IsGrounded => true;
            public float AirTime => 0f;
            public Vector3 GroundNormal => Vector3.up;
        }
    }
}
