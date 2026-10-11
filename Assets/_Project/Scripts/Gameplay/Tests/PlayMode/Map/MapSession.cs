using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;
using MoonProject.Gameplay.Map;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests.Map
{
    /// <summary>
    /// The crew's map in the real Main scene (docs/features/M3-07), from a seeded mid-0.5 save: the lift restored, the
    /// mound's mast lit, the depot, the garage and the drill visited, Kestrel-3 and the canyon's mouth only seen. 07
    /// rides up; the lookout sweeps (the unvisited places in view blink and are pencilled as seen), the hatch swings
    /// open, the lookout breathes, and the view eases onto the map, where the chart pen inks home and then every
    /// visited place in reach, in order, and nothing else. Driving leaves, and a second ride opens straight onto the
    /// map with nothing new to ink. Frames with the UI composited over the game camera go to
    /// Logs/gameplay-captures/map-*.png with map.md recording the pen's order, frame times and the map's per-frame
    /// allocations. Any error in the log fails it. Slow and needs a GPU: run on demand with --category MapSession.
    /// </summary>
    [Explicit("Slow session in the real Main scene; run on demand with --category MapSession.")]
    [Category("MapSession")]
    public sealed class MapSession : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 3f;
        private const float PhaseTimeout = 40f;
        private const float OpeningFrame = 0.6f;
        private const float InkTimeout = 30f;
        private const float EdgeFrame = 4f;
        private const int MeasuredFrames = 120;

        // The tower at its third level, the lift restored, relay.0 restored and the map's own record (version 1).
        private const string SeededSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-11T00:00:00Z\"," +
            "\"sections\":[" +
            "{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":3}]}\"}," +
            "{\"key\":\"gameplay.lift\",\"version\":1,\"json\":\"{\\\"restored\\\":true}\"}," +
            "{\"key\":\"gameplay.relays\",\"version\":1,\"json\":\"{\\\"masts\\\":[{\\\"id\\\":\\\"relay.0\\\"," +
            "\\\"part\\\":true,\\\"paid\\\":1,\\\"restored\\\":true}]}\"}," +
            "{\"key\":\"gameplay.map\",\"version\":1,\"json\":\"{\\\"seen\\\":[\\\"site.kestrel\\\"," +
            "\\\"canyon.mouth\\\"],\\\"visited\\\":[\\\"place.lander\\\",\\\"site.depot\\\",\\\"site.garage\\\"," +
            "\\\"site.drill\\\"],\\\"inked\\\":[],\\\"hatchOpen\\\":false}\"}]}";

        private readonly List<string> _problems = new List<string>();
        private readonly StringBuilder _report = new StringBuilder();
        private readonly List<string> _inked = new List<string>();
        private readonly List<string> _seen = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private MapSystem _map;
        private UIDocument _ui;
        private RenderTexture _target;
        private Keyboard _keyboard;
        private bool _awake;
        private int _looks;
        private int _ended;
        private int _hatches;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            if (_ui != null && _ui.panelSettings != null)
            {
                _ui.panelSettings.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

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
        [Timeout(600000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheFirstLook_InksEveryVisitedPlaceInReach_InOrder_AndNothingElse()
        {
            yield return Boot();
            LiftStation lift = _gameplay.Lift;
            MapKnowledge knowledge = _map.Knowledge;
            _report.AppendLine("# The crew's map (real Main scene, seeded mid-0.5 save)");
            _report.AppendLine();
            Assert.AreEqual(LiftPhase.Ready, lift.Phase, "the seeded lift is restored");
            var expected = new List<string>();
            for (int i = 0; i < knowledge.PlaceCount; i++)
            {
                string id = knowledge.GetPlace(i).Id;
                if (knowledge.IsInked(id))
                {
                    expected.Add(id);
                }
            }

            Assert.AreEqual(MapPlaces.HomeId, expected[0], "home is inked first");
            _report.AppendLine("Inked before the look (visited and in reach): " + string.Join(", ", expected));

            using (_context.Events.Subscribe<MapInkCued>(cue => _inked.Add(cue.PlaceId)))
            using (_context.Events.Subscribe<MapPlaceSeen>(seen => _seen.Add(seen.PlaceId)))
            using (_context.Events.Subscribe<MapLookStarted>(_ => _looks++))
            using (_context.Events.Subscribe<MapLookEnded>(_ => _ended++))
            using (_context.Events.Subscribe<MapHatchOpened>(_ => _hatches++))
            {
                _context.Get<IRoverPlacement>().PlaceAt(lift.RideSpot, Quaternion.LookRotation(lift.RideHeading));
                yield return WaitUntil(() => lift.Phase == LiftPhase.Top, "07 rides up to the deck");
                yield return WaitUntil(() => _seen.Count > 0 || _map.Phase != DeckLookPhase.Sweeping,
                    "the sweep blinks");
                yield return Capture("map-0-sweep");
                yield return WaitUntil(() => _hatches == 1, "the hatch swings open after the sweep");
                yield return Capture("map-1-hatch");
                yield return WaitUntil(() => _looks == 1, "the view eases onto the map");
                yield return new WaitForSeconds(OpeningFrame);
                yield return Capture("map-2-opening");
                yield return WaitUntil(() => _inked.Count >= Mathf.Min(2, expected.Count), "the pen starts");
                yield return Capture("map-3-inking");
                yield return WaitUntil(() => knowledge.InkedSinceLastLook == 0 && _inked.Count == expected.Count,
                    "the pen inks every visited place in reach", InkTimeout);
                yield return new WaitForSeconds(1f);
                yield return Capture("map-4-inked");
                yield return new WaitForSeconds(EdgeFrame);
                yield return Capture("map-5-edge");
                yield return Measure();

                CollectionAssert.AreEqual(expected, _inked, "home, then every visited place in reach, in order");
                foreach (string id in _inked)
                {
                    Assert.AreEqual(MapPlaceState.Visited, knowledge.GetState(id), $"{id} was inked unvisited");
                }

                foreach (string id in _seen)
                {
                    Assert.AreNotEqual(MapPlaceState.Visited, knowledge.GetState(id), $"{id} was swept but visited");
                }

                Press(_keyboard.wKey);
                yield return WaitUntil(() => _ended == 1, "driving leaves the map");
                yield return WaitUntil(() => lift.Phase == LiftPhase.Lowering, "and the lift rides down");
                Release(_keyboard.wKey);
                yield return WaitUntil(() => lift.Phase == LiftPhase.Ready, "the platform settles at the bottom");

                _inked.Clear();
                _context.Get<IRoverPlacement>().PlaceAt(lift.RideSpot - lift.RideHeading * 4f,
                    Quaternion.LookRotation(lift.RideHeading));
                yield return new WaitForSeconds(SettleSeconds);
                _context.Get<IRoverPlacement>().PlaceAt(lift.RideSpot, Quaternion.LookRotation(lift.RideHeading));
                yield return WaitUntil(() => _looks == 2, "a second ride opens straight onto the map");
                Assert.AreEqual(1, _hatches, "the hatch opened once");
                yield return new WaitForSeconds(SettleSeconds);
                CollectionAssert.IsEmpty(_inked, "nothing new to ink on the second look");
                yield return Capture("map-6-again");
                Press(_keyboard.wKey);
                yield return WaitUntil(() => _ended == 2, "driving leaves again");
                Release(_keyboard.wKey);
            }

            _report.AppendLine("Pen order: " + string.Join(", ", expected));
            _report.AppendLine("Swept (pencilled as seen): " + string.Join(", ", _seen));
            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(Path.Combine(GameplayFixture.CaptureFolder, "map.md"), _report.ToString());
            Debug.Log("[map] " + _report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>Frame times and the main thread's managed allocations over whole frames while the map rests open.</summary>
        private IEnumerator Measure()
        {
            yield return null;
            long before = GC.GetAllocatedBytesForCurrentThread();
            float worst = 0f;
            float total = 0f;
            for (int i = 0; i < MeasuredFrames; i++)
            {
                yield return null;
                worst = Mathf.Max(worst, Time.unscaledDeltaTime);
                total += Time.unscaledDeltaTime;
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Map open, {0} frames: mean {1:F2} ms, worst {2:F2} ms; main thread allocated {3} bytes in all " +
                "(every system and the test's own coroutine)", MeasuredFrames, total / MeasuredFrames * 1000f,
                worst * 1000f, allocated));
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), SeededSave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The map session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
                _map = _map != null ? _map : root.GetComponentInChildren<MapSystem>();
                _ui = _ui != null ? _ui : root.GetComponentInChildren<UIDocument>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_map, "the scene has the crew's map");
            Assert.IsNotNull(_ui, "the scene has its UI document");
            _context = bootstrap.Context;
            _target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            _target.Create();
            _ui.panelSettings.targetTexture = _target;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float started = Time.time;
                while (!_awake)
                {
                    Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                    yield return null;
                }
            }

            yield return new WaitForSeconds(SettleSeconds);
        }

        private static IEnumerator WaitUntil(Func<bool> done, string what, float timeout = PhaseTimeout)
        {
            float deadline = Time.time + timeout;
            while (!done())
            {
                Assert.Less(Time.time, deadline, what);
                yield return null;
            }
        }

        /// <summary>The game camera's frame with the UI (premultiplied) laid over it.</summary>
        private IEnumerator Capture(string name)
        {
            yield return null;
            Texture2D world = FrameCapture.Render(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight);
            var ui = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            try
            {
                Color32[] background = world.GetPixels32();
                Color32[] overlay = ui.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    Color32 over = overlay[i];
                    Color32 under = background[i];
                    int keep = 255 - over.a;
                    background[i] = new Color32((byte)Mathf.Min(255, over.r + under.r * keep / 255),
                        (byte)Mathf.Min(255, over.g + under.g * keep / 255),
                        (byte)Mathf.Min(255, over.b + under.b * keep / 255), 255);
                }

                world.SetPixels32(background);
                world.Apply(false);
                FrameCapture.WritePng(world, Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
                _report.AppendLine(string.Format(CultureInfo.InvariantCulture, "- {0}: deck {1}, {2} inked, {3} seen",
                    name, _map.Phase, _inked.Count, _seen.Count));
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add(message);
            }
        }
    }
}
