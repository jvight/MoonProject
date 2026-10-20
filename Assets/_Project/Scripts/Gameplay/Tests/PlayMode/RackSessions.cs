using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    /// The socket rack in the real Main scene (opening v3 4.3, 4.4): it is registered, stays hidden through a dock before
    /// the release, and comes out when the release reveals it, at 07's first dock after the view, with the outlines of
    /// the two parts 07 has seen.
    /// </summary>
    [Category("Rack")]
    public sealed class RackSessions
    {
        private const float WakeTimeout = 20f;
        private const float DockTimeout = 8f;
        private const float ReleaseTimeout = 90f;
        private const float PadYaw = 180f;
        private const float AwayMetres = 12f;
        private const float SettleSeconds = 1f;
        private const float DockReach = 0.6f;
        private const int Width = 1920;
        private const int Height = 1080;

        // The sky clock's sunlit day and earthlit night (as the face-inventory captures).
        private const float DayClockSeconds = 900f;
        private const float NightClockSeconds = 4800f;

        private static readonly string Folder = Path.Combine(GameplayFixture.CaptureFolder, "rack");

        private readonly List<RackChanged> _changes = new List<RackChanged>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private IDisposable _subscription;
        private IDisposable _dockSubscription;
        private bool _awake;
        private bool _docked;
        private RoverDockChanged _dock;

        private RackViewSettings ViewSettings => _gameplay.RackView.Settings;

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
        public IEnumerator AfterTheReleasesView_TheRackComesOutAtTheDock_WithTwoOutlines()
        {
            yield return Boot();
            IRackStatus rack = _context.Get<IRackStatus>();
            Assert.IsFalse(rack.Shown, "a new game has no rack");

            Vector3 centre = PlacePad();
            yield return new WaitForSeconds(SettleSeconds);
            yield return DockAt(centre);
            Assert.IsFalse(rack.Shown, "hidden through a dock before the release");
            Assert.AreEqual(0, _changes.Count);

            yield return LeavePad(centre);
            _context.Events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Done, false));
            float deadline = Time.time + ReleaseTimeout;
            while (_gameplay.Release.Stage != ReleaseStage.RackPending)
            {
                Assert.Less(Time.time, deadline, "the release runs its ring and its view");
                yield return null;
            }

            Assert.IsFalse(rack.Shown, "the view alone does not bring it out: the next dock does");
            yield return DockAt(centre);
            Assert.IsTrue(rack.Shown, "out at the first dock after the release's view");
            Vector3 off = rack.Position - centre;
            off.y = 0f;
            Assert.Less(off.magnitude, DockReach, "placed at the pad 07 docked on");
            Assert.AreEqual(2, _changes.Count, "the SSR-3 headlamp's and the cell pack's outlines");
            for (int i = 0; i < _changes.Count; i++)
            {
                Assert.AreEqual(RackBayState.Outline, _changes[i].State);
                Assert.IsFalse(_changes[i].Restored);
            }

            Assert.AreEqual(RackBayState.Hidden, rack.StateOf(2), "the hex adapter is not seen yet");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheRack_RisesBesideThePad_LightsItsOutlines_AndClosesItsJawsWhenFitted_Captured()
        {
            Directory.CreateDirectory(Folder);
            yield return Boot();
            Vector3 centre = PlacePad();
            SetSky(DayClockSeconds);
            yield return new WaitForSeconds(SettleSeconds);
            yield return DockAt(centre);
            Assert.IsNull(_gameplay.RackView.Instance, "no rack before the release");
            Capture("rack_hidden_day");

            _context.Events.Publish(new SocketRackRevealed(_dock.Position, _dock.Rotation));
            yield return new WaitForSeconds(ViewSettings.RiseSeconds * 0.5f);
            GameObject rack = _gameplay.RackView.Instance;
            Assert.IsNotNull(rack, "the release stands Art's rack");
            Assert.Less(rack.transform.position.y, _context.Get<IRackStatus>().Position.y, "still rising");
            Capture("rack_reveal_rising_day");

            yield return new WaitForSeconds(ViewSettings.RiseSeconds + ViewSettings.BayStaggerSeconds * 3f + ViewSettings.GlowSeconds);
            Assert.IsTrue(_gameplay.RackView.Look.Standing);
            Vector3 beside = rack.transform.position - _dock.Position;
            beside.y = 0f;
            Assert.AreEqual(ViewSettings.Offset.magnitude, beside.magnitude, 0.05f, "beside the dock");
            Assert.Greater(_gameplay.RackView.Look.Outline(0), 0f, "the headlamp's outline glows");
            Assert.AreEqual(0f, _gameplay.RackView.Look.Outline(2), "the hex adapter is not seen yet");
            Capture("rack_outline_day");
            SetSky(NightClockSeconds);
            yield return new WaitForSeconds(SettleSeconds);
            Capture("rack_outline_night");

            _context.Events.Publish(new PartFitted(RoverSocket.Lamp, RoverKitPiece.LampBar, false, false,
                RoverKitPiece.LampBar, 0));
            _context.Events.Publish(new PartFitted(RoverSocket.Back, RoverKitPiece.BatterySpine, false, false,
                RoverKitPiece.BatterySpine, 0));
            yield return new WaitForSeconds(Mathf.Max(ViewSettings.JawSeconds, ViewSettings.GlowSeconds) + SettleSeconds);
            Assert.AreEqual(1f, _gameplay.RackView.Look.Jaw(0), 1e-3f, "the headlamp's jaws closed");
            Assert.Less(Quaternion.Angle(Quaternion.identity, rack.transform.Find("Bay_0/Clamp_L_0").localRotation),
                0.1f, "to the closed pose");
            Capture("rack_filled_night");
            SetSky(DayClockSeconds);
            yield return new WaitForSeconds(SettleSeconds);
            Capture("rack_filled_day");
        }

        /// <summary>The pad built on the apron, and 07's battery full: set down on it, the pad takes 07 in.</summary>
        private Vector3 PlacePad()
        {
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                new StructurePose(centre, PadYaw), 1f);
            // A full battery set down on the pad is taken in all the same (PadLock): the dock without a drive in.
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells });
            return centre;
        }

        private void OnDock(RoverDockChanged dock)
        {
            if (dock.Docked)
            {
                _docked = true;
                _dock = dock;
            }
        }

        /// <summary>The sky clock set for a capture (World's sky exposes it; no Core setter).</summary>
        private void SetSky(float seconds)
        {
            ISunlight sun = _context.Get<ISunlight>();
            MethodInfo set = sun.GetType().GetMethod("SetClockTime", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(set, "World's sky clock can be set for the tests");
            set.Invoke(sun, new object[] { seconds, false, true });
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, Width, Height, Path.Combine(Folder, name + ".png"));
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
            _dockSubscription = _context.Events.Subscribe<RoverDockChanged>(OnDock);
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
