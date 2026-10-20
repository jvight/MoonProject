using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The socket rack in the real Main scene (opening v3 4.3): it is registered, held through a dock before the west
    /// signal, and comes out at 07's dock on its pad after it, with the outlines of the two parts 07 has seen.
    /// </summary>
    [Category("Rack")]
    public sealed class RackSessions
    {
        private const float WakeTimeout = 20f;
        private const float DockTimeout = 8f;
        private const float PadYaw = 180f;
        private const float AwayMetres = 12f;
        private const float SettleSeconds = 1f;

        private readonly List<RackChanged> _changes = new List<RackChanged>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private IDisposable _subscription;
        private IDisposable _dockSubscription;
        private bool _awake;
        private bool _docked;

        [SetUp]
        public void SetUp()
        {
            _changes.Clear();
            _awake = false;
            _docked = false;
            _gameplay = null;
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
            _dockSubscription?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator AfterTheWestSignal_TheRackComesOutAtTheDock_WithTwoOutlines()
        {
            yield return Boot();
            IRackStatus rack = _context.Get<IRackStatus>();
            Assert.IsFalse(rack.Shown, "a new game has no rack");

            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                new StructurePose(centre, PadYaw), 1f);
            // A full battery set down on the pad is taken in all the same (PadLock): the dock without a drive in.
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells });
            yield return new WaitForSeconds(SettleSeconds);
            yield return DockAt(centre);
            Assert.IsFalse(rack.Shown, "held through a dock before the west signal");
            Assert.AreEqual(0, _changes.Count);

            yield return LeavePad(centre);
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
            yield return DockAt(centre);
            Assert.IsTrue(rack.Shown, "out at the first dock after the west signal");
            Assert.AreEqual(2, _changes.Count, "the SSR-3 headlamp's and the cell pack's outlines");
            for (int i = 0; i < _changes.Count; i++)
            {
                Assert.AreEqual(RackBayState.Outline, _changes[i].State);
                Assert.IsFalse(_changes[i].Restored);
            }

            Assert.AreEqual(RackBayState.Hidden, rack.StateOf(2), "the hex adapter is not seen yet");
        }

        private IEnumerator DockAt(Vector3 centre)
        {
            _docked = false;
            _context.Get<IRoverPlacement>().PlaceAt(centre, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            float deadline = Time.time + DockTimeout;
            while (!_docked && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_docked, "07 docks on its pad");
            yield return null;
        }

        private IEnumerator LeavePad(Vector3 centre)
        {
            _context.Get<IRoverPlacement>().PlaceAt(centre + Vector3.right * AwayMetres, Quaternion.identity);
            yield return null;
            yield return null;
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The rack sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            _subscription = _context.Events.Subscribe<RackChanged>(_changes.Add);
            _dockSubscription = _context.Events.Subscribe<RoverDockChanged>(dock => _docked |= dock.Docked);
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
        }
    }
}
