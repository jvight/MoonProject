using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// 07 parks on the pad it built to charge (guidance audit row 11), seen from the chase camera and heard through the
    /// events: the approach, the dock lock's wheel chocks and clamp arms, the lock's thunk, 07's chirp, the charging hum
    /// and the charge filling. A frame every 0.5 s and a timeline go to Logs/gameplay-captures/dock/ (dock-timeline.md).
    /// </summary>
    [Category("PadDock")]
    public sealed class PadDockCaptureSessions : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float StartCharge = 0.4f;
        private const float DriveTimeout = 24f;
        private const float AfterDockSeconds = 17f;
        private const float StepSeconds = 0.5f;
        private const float StartOffsetMetres = 9f;
        private const float AheadMetres = 4.6f;
        private const float SideDistance = 4f;
        private const float SideHeight = 1.2f;
        private const float SideOpenAfter = 0.8f;
        private const float SideLockedAfter = 0.8f;
        private const float WeldTimeout = 30f;
        private const int Width = 1600;
        private const int Tall = 900;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures", "dock"));

        private readonly StringBuilder _timeline = new StringBuilder();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private Keyboard _keyboard;
        private bool _awake;
        private float _t0;
        private float _closedAt = -1f;
        private int _movedParts;
        private bool _fullChirp;
        private bool _fullBeat;

        public override void Setup()
        {
            base.Setup();
            _gameplay = null;
            _awake = false;
            _timeline.Clear();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator DrivingOntoTheBuiltPad_LocksIt_ChirpsAndShowsTheChargeFilling()
        {
            Directory.CreateDirectory(Folder);
            yield return Boot();
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            Assert.IsTrue(_gameplay.KnownStructures.Learn(StructureIds.ChargingPad));
            BuildField build = _gameplay.Building;
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition padDef));
            _gameplay.Materials.Stow(padDef.BuildCost);
            Vector3 stand = centre + Vector3.back * AheadMetres;
            stand.y = terrain.SampleHeight(stand.x, stand.z);
            _context.Get<IRoverPlacement>().PlaceAt(stand, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return Wait(1f);
            Assert.IsTrue(build.TryOpen());
            Assert.IsTrue(build.TryPick(0));
            build.SetAimPointOverride(centre);
            yield return Wait(0.3f);
            yield return Tap(_keyboard.eKey);
            Press(_keyboard.eKey);
            float weldDeadline = Time.time + WeldTimeout;
            while (!_gameplay.PlacedBase.StructureAt(0).IsBuilt && Time.time < weldDeadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            build.SetAimPointOverride(null);
            Assert.IsTrue(_gameplay.PlacedBase.StructureAt(0).IsBuilt, "07 welded the pad");
            yield return Wait(1f);
            _gameplay.Battery.Model.Restore(new BatterySaveData { cells = 3, charge = StartCharge });

            Subscribe();
            Vector3 start = centre + Vector3.back * StartOffsetMetres;
            start.y = terrain.SampleHeight(start.x, start.z);
            _context.Get<IRoverPlacement>().PlaceAt(start, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return Wait(1.5f);
            Camera view = _context.Get<IViewCamera>().Camera;

            _t0 = Time.time;
            PlacedStructure def = _gameplay.PlacedBase.StructureAt(0);
            Line($"pad {def.Definition.Width} x {def.Definition.Depth}, plate margin check, pad y {def.Pose.Position.y:0.00}");
            Line("drive onto the pad begins (charge " + _gameplay.Battery.Model.Charge.ToString("0.00") + " of " +
                 _gameplay.Battery.Model.Cells + " cells)");
            Press(_keyboard.wKey);
            float next = 0f;
            int shot = 0;
            float deadline = Time.time + DriveTimeout;
            float dockedAt = -1f;
            bool sideOpen = false;
            bool sideLocked = false;
            while (Time.time < deadline && (dockedAt < 0f || Time.time - dockedAt < AfterDockSeconds))
            {
                yield return null;
                if (dockedAt < 0f && _gameplay.PadLock.OnPad)
                {
                    dockedAt = Time.time;
                    Release(_keyboard.wKey);
                    Line("07 is on the pad");
                }

                if (!sideOpen && dockedAt >= 0f && Time.time - dockedAt > SideOpenAfter)
                {
                    sideOpen = true;
                    SideShot("dock-side-1-settled");
                    Line("lock parts before the lock: " + Lock());
                }

                if (!sideLocked && _closedAt >= 0f && Time.time - _closedAt > SideLockedAfter)
                {
                    sideLocked = true;
                    SideShot("dock-side-2-locked");
                    Line("lock parts locked: " + Lock());
                    Assert.AreEqual(6, _movedParts, "the four chocks and the two clamp arms have moved from their rest");
                }

                if (Time.time - _t0 >= next)
                {
                    PlacedStructure placed = _gameplay.PlacedBase.StructureAt(0);
                    Line($"built {placed.IsBuilt}, 07 {_context.Get<IRoverState>().Position - placed.Pose.Position}, " +
                         $"speed {_context.Get<IRoverState>().Speed:0.0}, on pad {_gameplay.PadLock.OnPad}");
                    FrameCapture.SavePng(view, Width, Tall, Path.Combine(Folder, $"dock-{shot++:00}-t{Time.time - _t0:0.0}s.png"));
                    next += StepSeconds;
                }
            }

            Release(_keyboard.wKey);
            Line("charge now " + _gameplay.Battery.Model.Charge.ToString("0.00") + ", raised " + _gameplay.PadLock.Raised);
            File.WriteAllText(Path.Combine(Folder, "dock-timeline.md"), _timeline.ToString());
            Assert.IsTrue(_closedAt >= 0f, "the lock's clamp closed on 07");
            Assert.IsTrue(_fullBeat, "the charge filled");
            Assert.IsTrue(_fullChirp, "07 chirps Happy when the charge is full, though the build's Happy was just before");
        }

        /// <summary>07 and its pad from the side, low: the wheel chocks and the clamp arms.</summary>
        private void SideShot(string name)
        {
            Vector3 rover = _context.Get<IRoverState>().Position;
            var lens = new GameObject("DockSideLens");
            Camera camera = lens.AddComponent<Camera>();
            camera.CopyFrom(_context.Get<IViewCamera>().Camera);
            camera.enabled = false;
            lens.transform.position = rover + Vector3.right * SideDistance + Vector3.up * SideHeight;
            lens.transform.rotation = Quaternion.LookRotation(rover + Vector3.up * 0.5f - lens.transform.position,
                Vector3.up);
            FrameCapture.SavePng(camera, Width, Tall, Path.Combine(Folder, name + ".png"));
            Object.Destroy(lens);
        }

        private string Lock()
        {
            var text = new StringBuilder();
            _movedParts = 0;
            PlacedStructure pad = _gameplay.PlacedBase.StructureAt(0);
            if (!_gameplay.Building.TryGetView(pad.InstanceId, out StructureView view))
            {
                return "no view";
            }

            foreach (Transform found in view.GetComponentsInChildren<Transform>(true))
            {
                if (found.name.StartsWith("Chock_", StringComparison.Ordinal) ||
                    found.name.StartsWith("ClampArm_", StringComparison.Ordinal))
                {
                    text.Append(found.name).Append(' ').Append(found.localEulerAngles.ToString("0")).Append("; ");
                    _movedParts += found.localRotation != Quaternion.identity ? 1 : 0;
                }
            }

            return text.ToString();
        }

        private void Subscribe()
        {
            _context.Events.Subscribe<RoverDockChanged>(d => Line("RoverDockChanged docked " + d.Docked));
            _context.Events.Subscribe<PadLockClosed>(_ =>
            {
                _closedAt = Time.time;
                Line("PadLockClosed (clamp thunk)");
            });
            _context.Events.Subscribe<PadLockOpened>(_ => Line("PadLockOpened"));
            _context.Events.Subscribe<ChargingChanged>(c => Line("ChargingChanged " + c.Charging + " " + c.Charger));
            _context.Events.Subscribe<RoverChirpSyllable>(c =>
            {
                _fullChirp |= _fullBeat && c.Mood == ChirpMood.Happy;
                Line("chirp " + c.Mood + " " + (c.Index + 1) + "/" + c.Count);
            });
            _context.Events.Subscribe<FeelBeat>(b =>
            {
                _fullBeat |= b.Kind == FeelKind.Dock && b.Phase == FeelPhase.Reward;
                Line("feel " + b.Kind + " " + b.Phase + " " + b.Strength.ToString("0.0"));
            });
            _context.Events.Subscribe<BatteryChanged>(b => Line("BatteryChanged full cells " + b.FullCells));
        }

        private void Line(string text)
        {
            _timeline.Append("- t+").Append((Time.time - _t0).ToString("0.00")).Append(" s: ").AppendLine(text);
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
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
            throw new NotSupportedException("The pad dock captures load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap);
            Assert.IsNotNull(_gameplay);
            _context = bootstrap.Context;
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
