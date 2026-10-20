using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's winch cable (opening v3 R6): the tether key hooks the nearest load whose hitch is in reach of the winch and
    /// lets it go again; a load towed home to the apron is unhooked there; one that fell far behind is let go softly;
    /// each hook and release is published with the load's id; a load stands fast off the cable and slides free on it;
    /// a load is found by its tow id, the load the key would hook is known before it is pressed, and a save puts each
    /// load back where it was left, off the cable.
    /// </summary>
    public sealed class TowLineTests
    {
        private static readonly Vector3 Home = new Vector3(0f, 0f, 100f);

        private readonly List<TowAttached> _attached = new List<TowAttached>();
        private readonly List<TowReleased> _released = new List<TowReleased>();
        private readonly List<GameObject> _made = new List<GameObject>();
        private EventBus _events;
        private TowSettings _settings;
        private Transform _winch;
        private TowLine _tow;

        [SetUp]
        public void SetUp()
        {
            _attached.Clear();
            _released.Clear();
            _events = new EventBus();
            _events.Subscribe<TowAttached>(_attached.Add);
            _events.Subscribe<TowReleased>(_released.Add);
            _settings = new TowSettings();
            _winch = Make("Winch").transform;
            _winch.position = new Vector3(0f, 0.6f, 0f);
            _tow = new TowLine(_events, _settings, _winch, new StillRover(), Home);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void TheTetherKey_HooksTheNearestLoadInReach_AndSaysWhich()
        {
            TowableBody far = Load("module.far", new Vector3(0f, 0f, -(_settings.HookReach - 0.2f)));
            TowableBody near = Load("module.near", new Vector3(0.5f, 0f, -1f));
            Assert.IsTrue(near.StandsFast, "off the cable a load stands fast");

            _tow.Step(false);
            Assert.IsFalse(_tow.IsTowing, "nothing without the key");
            _tow.Step(true);
            Assert.IsTrue(_tow.IsTowing);
            Assert.AreEqual("module.near", _tow.TowingId, "the nearest hitch");
            Assert.IsTrue(near.IsTowed);
            Assert.IsFalse(near.StandsFast, "on the cable it slides free");
            Assert.IsFalse(far.IsTowed);
            Assert.AreEqual(1, _attached.Count);
            Assert.AreEqual("module.near", _attached[0].TowId);
            Assert.AreEqual(near.Body.mass, _attached[0].Mass);
            Assert.AreEqual(near.Hitch, _tow.LoadHitch);
        }

        [Test]
        public void ALoadBeyondTheHookReach_IsNotHooked()
        {
            Load("module", new Vector3(0f, 0f, -(_settings.HookReach + 0.5f)));
            _tow.Step(true);
            Assert.IsFalse(_tow.IsTowing);
            Assert.IsEmpty(_attached);
        }

        [Test]
        public void TheTetherKeyAgain_LetsGo_AndTheLoadStandsFastOnceAtRest()
        {
            TowableBody load = Load("module", new Vector3(0f, 0f, -1f));
            _tow.Step(true);
            _tow.Step(true);
            Assert.IsFalse(_tow.IsTowing);
            Assert.AreEqual(1, _released.Count);
            Assert.AreEqual("module", _released[0].TowId);
            Assert.AreEqual(TowRelease.Player, _released[0].Reason);
            Assert.IsFalse(load.StandsFast, "it slides to rest first");

            int frames = Mathf.CeilToInt(_settings.RestSeconds / Time.fixedDeltaTime) + 1;
            for (int i = 0; i < frames; i++)
            {
                _tow.FixedStep(Time.fixedDeltaTime);
            }

            Assert.IsTrue(load.StandsFast, "at rest, it stands fast where it is");
        }

        [Test]
        public void ALoadTowedHome_IsUnhookedAtTheApron()
        {
            TowableBody load = Load("module", new Vector3(0f, 0f, -1f));
            _tow.Step(true);
            load.Body.transform.position = Home + new Vector3(_settings.HomeRadius - 0.5f, 0f, 0f);
            _tow.Step(false);
            Assert.IsFalse(_tow.IsTowing);
            Assert.AreEqual(TowRelease.Arrived, _released[0].Reason);
        }

        [Test]
        public void ALoadThatFellFarBehind_IsLetGoSoftly()
        {
            TowableBody load = Load("module", new Vector3(0f, 0f, -1f));
            _tow.Step(true);
            load.Body.transform.position = new Vector3(0f, 0f, -(_settings.LetGoDistance + 1f));
            _tow.FixedStep(Time.fixedDeltaTime);
            Assert.IsFalse(_tow.IsTowing);
            Assert.AreEqual(TowRelease.Snapped, _released[0].Reason);
            Assert.AreEqual(0f, _tow.Strain01);
        }

        [Test]
        public void TheLoadToHook_IsTheNearestInReach_AndNoneWhileTowing()
        {
            Assert.IsFalse(_tow.TryGetHookable(out _), "no load stands");
            Load("module.far", new Vector3(0f, 0f, -(_settings.HookReach + 1f)));
            Assert.IsFalse(_tow.TryGetHookable(out _), "out of hook reach");

            TowableBody near = Load("module.near", new Vector3(0.5f, 0f, -1f));
            Assert.IsTrue(_tow.TryGetHookable(out Vector3 hitch), "a load in reach is where the tether key is taught");
            Assert.AreEqual(near.Hitch, hitch);

            _tow.Step(true);
            Assert.IsTrue(_tow.IsTowing);
            Assert.IsFalse(_tow.TryGetHookable(out _), "nothing to hook while the cable holds a load");
        }

        [Test]
        public void ALoad_IsFoundByItsTowId_TowedOrNot()
        {
            TowableBody load = Load("habitat.power_module", new Vector3(0f, 0f, -1f));
            Assert.IsTrue(_tow.TryFindLoad("habitat.power_module", out Transform root));
            Assert.AreEqual(load.Root, root);
            _tow.Step(true);
            Assert.IsTrue(_tow.TryFindLoad("habitat.power_module", out root), "on the cable too");
            Assert.IsFalse(_tow.TryFindLoad("no.such.load", out root));
            Assert.IsNull(root);
            Assert.Throws<System.ArgumentException>(() => _tow.Add(load), "a tow id is one load's");
        }

        [Test]
        public void ASave_PutsEachLoadBackWhereItWasLeft_OffTheCable()
        {
            TowableBody load = Load("habitat.power_module", new Vector3(0f, 0f, -1f));
            _tow.Step(true);
            var left = new Vector3(12f, 0.5f, 30f);
            Quaternion turned = Quaternion.Euler(0f, 40f, 0f);
            load.Root.SetPositionAndRotation(left, turned);
            TowSaveData data = _tow.Capture();
            Assert.AreEqual(new[] { "habitat.power_module" }, data.ids);

            load.Root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _tow.Restore(data);
            Assert.IsFalse(_tow.IsTowing, "a loaded game finds it off the cable");
            Assert.IsFalse(load.IsTowed);
            Assert.IsTrue(load.StandsFast);
            Assert.AreEqual(left, load.Root.position);
            Assert.AreEqual(turned.eulerAngles.y, load.Root.rotation.eulerAngles.y, 1e-3f);
        }

        private TowableBody Load(string id, Vector3 at)
        {
            GameObject load = Make(id);
            load.transform.position = at;
            load.AddComponent<BoxCollider>();
            var body = load.AddComponent<Rigidbody>();
            Transform hitch = Make(id + "_Hitch").transform;
            hitch.SetParent(load.transform, false);
            var towable = new TowableBody(id, body, hitch, 300f, _settings);
            _tow.Add(towable);
            return towable;
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
