using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Real Main-scene capture session for Task RUINS-WORLD:
    /// 1. Ruins in the basin from the start area (day + night);
    /// 2. 07 scanning the fallen array with the beam from its scan lens;
    /// 3. The learned toast;
    /// 4. The build strip before and after (a new icon unlocked).
    /// </summary>
    [Explicit("On-demand capture session for Task RUINS-WORLD.")]
    [Category("RuinsBasinCaptureSession")]
    public sealed class RuinsBasinCaptureSession : InputTestFixture
    {
        private const string FallenArrayFirstFragmentId = "ruin.FallenSolarArray.0";
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 25f;
        private const float WakeEaseTimeout = 8f;
        private const float StickProbeSeconds = 0.3f;
        private const float LookStick = 0.8f;
        private const float SettleSeconds = 0.6f;
        private const float SunlitDaySeconds = 210f;
        private const float EarthlitNightSeconds = 4816f;
        private const float ScanTimeout = 10f;
        private const float ParkClearance = 1.4f;
        private const float CameraBackCheck = 5.5f;
        private const float CameraHeightCheck = 2.1f;

        private static readonly float[] CandidateDistances = { 3.2f, 3.8f, 4.4f, 5.0f };
        private static readonly float[] CandidateYawOffsets = { -18f, 18f, -10f, 10f, 0f };

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private UIDocument _document;
        private PanelSettings _originalPanelSettings;
        private PanelSettings _capturePanelSettings;
        private RenderTexture _uiTarget;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private IDisposable _awoke;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Application.logMessageReceived -= OnLog;
            _awoke?.Dispose();

            if (_document != null && _originalPanelSettings != null)
            {
                _document.panelSettings = _originalPanelSettings;
            }

            if (_capturePanelSettings != null)
            {
                _capturePanelSettings.targetTexture = null;
                Object.DestroyImmediate(_capturePanelSettings);
                _capturePanelSettings = null;
            }

            if (_uiTarget != null)
            {
                _uiTarget.Release();
                Object.DestroyImmediate(_uiTarget);
                _uiTarget = null;
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
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator CaptureRuinsBasin_Scanning_Toast_AndBuildStripBeforeAfter()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return Boot();

            BuildField build = _gameplay.Building;
            ScanField scans = _gameplay.Scans;
            Assert.IsNotNull(build, "BuildField must be initialized.");
            Assert.IsNotNull(scans, "ScanField must be initialized.");

            Assert.AreEqual(0, build.Known.Count, "A new game knows no structure (opening-v2 B3).");
            Assert.IsTrue(build.Known.Learn(StructureIds.ChargingPad), "the pad, as the lander's cargo scan teaches it");
            Assert.IsFalse(build.Known.IsKnown(StructureIds.SolarArray));

            ScanFragment fallenArrayFrag = scans.Find(FallenArrayFirstFragmentId);
            Assert.IsNotNull(fallenArrayFrag, "FallenSolarArray's Scan_1 target must be registered in ScanField.");

            var terrain = _context.Get<ITerrainQuery>();
            var placement = _context.Get<IRoverPlacement>();
            Vector3 basePos = _context.Get<IWorldLayout>().BasePosition;

            // Compute the center of the placed ruins cluster from FallenSolarArray's parent ([World]/Ruins).
            Transform ruinsRoot = fallenArrayFrag.Root.parent != null ? fallenArrayFrag.Root.parent.parent : null;
            Vector3 ruinsCenter = Vector3.zero;
            if (ruinsRoot != null && ruinsRoot.childCount > 0)
            {
                for (int i = 0; i < ruinsRoot.childCount; i++)
                {
                    ruinsCenter += ruinsRoot.GetChild(i).position;
                }

                ruinsCenter /= ruinsRoot.childCount;
            }
            else
            {
                ruinsCenter = fallenArrayFrag.Point;
            }

            Vector3 toRuinsDir = Flat(ruinsCenter - basePos).normalized;

            // Warm up FrameCapture once so URP post-processing history buffers are settled.
            Texture2D warmup = FrameCapture.Render(_view, CaptureWidth, CaptureHeight);
            Object.Destroy(warmup);
            yield return null;

            // -------------------------------------------------------------------------
            // -------------------------------------------------------------------------
            // 1. RUINS IN THE BASIN FROM THE START AREA (DAY + NIGHT)
            // -------------------------------------------------------------------------
            Vector3 startApronSpot = basePos + toRuinsDir * 16f;
            startApronSpot.y = terrain.SampleHeight(startApronSpot.x, startApronSpot.z);
            Quaternion lookTowardRuins = Quaternion.LookRotation(toRuinsDir, Vector3.up);
            placement.PlaceAt(startApronSpot, lookTowardRuins);
            yield return null;
            yield return null;

            DisableCameraFollowBehaviours(_view);
            Vector3 startCamEye = startApronSpot - toRuinsDir * 4.4f + Vector3.up * 2.3f;
            Vector3 startCamLook = ruinsCenter + Vector3.up * 1.5f;
            _view.transform.SetPositionAndRotation(
                startCamEye,
                Quaternion.LookRotation(startCamLook - startCamEye, Vector3.up));
            _view.fieldOfView = 46f;

            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            yield return Wait(0.5f);
            yield return CaptureComposite(_view, "01a-ruins-from-start-day");

            SetSkyClock(EarthlitNightSeconds, holdingEclipse: false);
            yield return Wait(0.5f);
            yield return CaptureComposite(_view, "01b-ruins-from-start-night");

            // Return to sunlit day for build strip before/after and scanning shots.
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            yield return Wait(0.35f);

            // -------------------------------------------------------------------------
            // 4a. BUILD STRIP BEFORE (ONLY CHARGING PAD UNLOCKED)
            // -------------------------------------------------------------------------
            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State, "Pressing B opens the build strip.");
            Assert.AreEqual(1, build.Controller.RefreshAvailable(), "Only ChargingPad is available before scanning.");
            yield return Wait(0.65f);
            yield return CaptureComposite(_view, "04a-build-strip-before");

            build.Controller.Close();
            Assert.AreEqual(BuildModeState.Closed, build.State);
            yield return Wait(0.35f);

            // -------------------------------------------------------------------------
            // 2. 07 SCANNING THE FALLEN ARRAY WITH THE BEAM FROM ITS SCAN LENS
            // -------------------------------------------------------------------------
            EnableCameraFollowBehaviours(_view);
            yield return PlaceRoverForCleanScanShot(fallenArrayFrag);
            yield return Wait(SettleSeconds);

            Assert.AreSame(fallenArrayFrag, scans.Candidate, "FallenSolarArray Scan_1 is the active scan candidate.");
            Assert.AreEqual(InteractionKind.Scan, _gameplay.Hints.Primary.Kind, "Primary hint is Scan.");
            Assert.IsNotNull(_rover.ScanOrigin, "IRoverRig.ScanOrigin is wired.");

            var learnedStructures = new List<StructureLearned>();
            var tickerLines = new List<TickerLine>();
            using (_context.Events.Subscribe<StructureLearned>(learnedStructures.Add))
            using (_context.Events.Subscribe<TickerLine>(tickerLines.Add))
            {
                Press(_keyboard.eKey);
                yield return Until(() => scans.Scanning == fallenArrayFrag, 2f, "holding E starts scanning FallenSolarArray");
                yield return Until(() => fallenArrayFrag.Progress >= 0.45f, ScanTimeout, "scan reaches 45%");

                // Frame 07's cheek scan lens (ScanOrigin), the active cyan ScanBeam, and FallenSolarArray in 3/4 profile.
                DisableCameraFollowBehaviours(_view);
                Vector3 emitter = _rover.ScanOrigin.position;
                Vector3 target = fallenArrayFrag.Point;
                Vector3 beamMid = Vector3.Lerp(emitter, target, 0.48f);
                Vector3 beamDir = Flat(target - emitter).normalized;
                Vector3 sideRight = new Vector3(beamDir.z, 0f, -beamDir.x);
                Vector3 scanCamEye = beamMid - sideRight * 2.5f - beamDir * 0.95f + Vector3.up * 0.95f;
                Vector3 scanCamLook = beamMid + Vector3.up * 0.08f;
                _view.transform.SetPositionAndRotation(
                    scanCamEye,
                    Quaternion.LookRotation(scanCamLook - scanCamEye, Vector3.up));
                _view.fieldOfView = 42f;
                yield return null;
                yield return null;

                yield return CaptureComposite(_view, "02-scanning-fallen-array-beam");

                // ---------------------------------------------------------------------
                // 3. THE LEARNED TOAST
                // ---------------------------------------------------------------------
                yield return Until(() => fallenArrayFrag.Scanned, ScanTimeout, "scan completes");
                Release(_keyboard.eKey);

                Assert.IsTrue(build.Known.IsKnown(StructureIds.SolarArray), "Scanning FallenSolarArray teaches SolarArray.");
                Assert.AreEqual(1, learnedStructures.Count);
                Assert.AreEqual(StructureIds.SolarArray, learnedStructures[0].StructureId);
                Assert.IsTrue(
                    tickerLines.Exists(t => t.Key == "scan.complete" && t.Argument == StructureIds.NameKey(StructureIds.SolarArray)),
                    "Scan completion publishes the scan.complete toast with SolarArray's localized name key.");

                // Return 07 to the open start apron and clear wide-shot look hints so the toast ticker holds the guidance slot.
                placement.PlaceAt(startApronSpot, lookTowardRuins);
                _context.Events.Publish(new RoverWideShotChanged(false));
                _context.Events.Publish(new NextStepChanged(string.Empty));
                _view.transform.SetPositionAndRotation(
                    startCamEye,
                    Quaternion.LookRotation(startCamLook - startCamEye, Vector3.up));
                _view.fieldOfView = 46f;

                VisualElement ticker = _document.rootVisualElement.Q<VisualElement>("ticker");
                Label tickerText = _document.rootVisualElement.Q<Label>("ticker-text");
                Assert.IsNotNull(ticker, "HUD has #ticker element.");
                Assert.IsNotNull(tickerText, "HUD has #ticker-text label.");

                yield return Until(
                    () => ticker.resolvedStyle.opacity >= 0.98f &&
                          !string.IsNullOrEmpty(tickerText.text) &&
                          tickerText.text.IndexOf("Solar Array", StringComparison.OrdinalIgnoreCase) >= 0,
                    8f,
                    "blueprint learned toast reaches full opacity on #ticker");

                yield return CaptureComposite(_view, "03-learned-toast");
            }

            // -------------------------------------------------------------------------
            // 4b. BUILD STRIP AFTER (SOLAR ARRAY ICON UNLOCKED)
            // -------------------------------------------------------------------------
            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State, "Pressing B re-opens the build strip.");
            Assert.AreEqual(2, build.Controller.RefreshAvailable(), "ChargingPad and SolarArray are now available.");
            yield return Wait(0.65f);
            yield return CaptureComposite(_view, "04b-build-strip-after");

            build.Controller.Close();
            yield return null;

            Assert.IsEmpty(_problems, "No errors in log:\n" + string.Join("\n", _problems));
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("RuinsBasinCaptureSession loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
                _document = _document != null ? _document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(bootstrap, "Scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Scene has GameplaySystem.");
            Assert.IsNotNull(_rover, "Scene has RoverController.");
            Assert.IsNotNull(_cameraRig, "Scene has RoverCameraRig.");
            Assert.IsNotNull(_document, "Scene has UIDocument.");

            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;

            _originalPanelSettings = _document.panelSettings;
            _uiTarget = new RenderTexture(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            _uiTarget.Create();
            _capturePanelSettings = Object.Instantiate(_originalPanelSettings);
            _capturePanelSettings.targetTexture = _uiTarget;
            _capturePanelSettings.clearColor = true;
            _capturePanelSettings.colorClearValue = Color.clear;
            _document.panelSettings = _capturePanelSettings;

            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            yield return Until(() => _awake, WakeTimeout, "07 wakes up");

            if (_cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
                yield return Wait(StickProbeSeconds);
                Set(_gamepad.rightStick, Vector2.zero);
                yield return Until(() => !_cameraRig.Opening.IsActive, WakeEaseTimeout, "the wake opening hands over");
            }

            // Restore real-time delta so UISystem (which ticks on Time.unscaledDeltaTime) advances in lockstep with Wait/Until.
            Time.captureDeltaTime = 0f;

            // Retire the opening's look/drive first-prompt so it never steals the guidance slot from the toast ticker.
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            Press(_keyboard.wKey);
            yield return null;
            yield return null;
            Release(_keyboard.wKey);
            Set(_gamepad.rightStick, Vector2.zero);
            _context.Events.Publish(new RoverWideShotChanged(false));
            _context.Events.Publish(new NextStepChanged(string.Empty));

            yield return Wait(SettleSeconds);
        }

        private IEnumerator PlaceRoverForCleanScanShot(ScanFragment fragment)
        {
            var terrain = _context.Get<ITerrainQuery>();
            var placement = _context.Get<IRoverPlacement>();
            Transform host = fragment.Root.parent != null ? fragment.Root.parent : fragment.Root;
            Vector3 away = Flat(fragment.Point - host.position);
            float baseBearing = away.sqrMagnitude > 1e-4f ? SurfaceRules.Bearing(away.normalized) : 195f;

            for (int strict = 1; strict >= 0; strict--)
            {
                foreach (float distance in CandidateDistances)
                {
                    for (int step = 0; step < 24; step++)
                    {
                        float sign = (step & 1) == 0 ? 1f : -1f;
                        float offset = (step / 2) * 15f * sign;
                        float bearing = baseBearing + offset;
                        Vector3 dir = SurfaceRules.BearingDirection(bearing);
                        Vector3 spot = Flat(fragment.Point + dir * distance);
                        float ground = terrain.SampleHeight(spot.x, spot.z);
                        var roverCheck = new Vector3(spot.x, ground + ParkClearance + 0.2f, spot.z);
                        bool openGround = !Physics.CheckSphere(
                            roverCheck,
                            ParkClearance,
                            Layers.PropMask,
                            QueryTriggerInteraction.Ignore);
                        if (!openGround)
                        {
                            continue;
                        }

                        Vector3 lookDir = Flat(fragment.Point - spot).normalized;
                        float baseYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
                        foreach (float yawOffset in CandidateYawOffsets)
                        {
                            Quaternion chassisRot = Quaternion.Euler(0f, baseYaw + yawOffset, 0f);
                            Vector3 camPos = new Vector3(spot.x, ground, spot.z)
                                - (chassisRot * Vector3.forward) * CameraBackCheck + Vector3.up * CameraHeightCheck;
                            if (Physics.CheckSphere(camPos, 0.5f, Layers.PropMask, QueryTriggerInteraction.Ignore))
                            {
                                continue;
                            }

                            placement.PlaceAt(new Vector3(spot.x, ground, spot.z), chassisRot);
                            yield return null;
                            yield return null;
                            if (_gameplay.Scans.Candidate != fragment)
                            {
                                continue;
                            }

                            if (strict == 1)
                            {
                                Vector3 lens = _rover.ScanOrigin.position;
                                float screenSep = Mathf.Abs(
                                    _view.WorldToScreenPoint(lens).x - _view.WorldToScreenPoint(fragment.Point).x);
                                if (screenSep < 60f)
                                {
                                    continue;
                                }
                            }

                            yield break;
                        }
                    }
                }
            }

            Assert.Fail($"Could not find a clean spot where {fragment.FragmentId} is the scan candidate.");
        }

        private void SetSkyClock(float timeSeconds, bool holdingEclipse)
        {
            if (!_context.TryGet(out ISunlight sunlight) || sunlight == null)
            {
                return;
            }

            MethodInfo method = sunlight.GetType().GetMethod(
                "SetClockTime",
                BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(sunlight, new object[] { timeSeconds, holdingEclipse, true });
        }

        private void DisableCameraFollowBehaviours(Camera viewCamera)
        {
            if (_cameraRig != null)
            {
                _cameraRig.enabled = false;
            }

            Behaviour[] cameraBehaviours = viewCamera.GetComponents<Behaviour>();
            for (int i = 0; i < cameraBehaviours.Length; i++)
            {
                Behaviour b = cameraBehaviours[i];
                if (b != null && !(b is Camera) &&
                    b.GetType().Name.IndexOf("Cinemachine", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.enabled = false;
                }
            }
        }

        private void EnableCameraFollowBehaviours(Camera viewCamera)
        {
            Behaviour[] cameraBehaviours = viewCamera.GetComponents<Behaviour>();
            for (int i = 0; i < cameraBehaviours.Length; i++)
            {
                Behaviour b = cameraBehaviours[i];
                if (b != null && !(b is Camera) &&
                    b.GetType().Name.IndexOf("Cinemachine", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.enabled = true;
                }
            }

            if (_cameraRig != null)
            {
                _cameraRig.enabled = true;
            }
        }

        private IEnumerator CaptureComposite(Camera camera, string fileName)
        {
            yield return null;
            Texture2D world = FrameCapture.Render(camera, CaptureWidth, CaptureHeight);
            var ui = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _uiTarget;
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
                    background[i] = new Color32(
                        (byte)Mathf.Min(255, over.r + under.r * keep / 255),
                        (byte)Mathf.Min(255, over.g + under.g * keep / 255),
                        (byte)Mathf.Min(255, over.b + under.b * keep / 255),
                        255);
                }

                world.SetPixels32(background);
                world.Apply(false);
                string path = Path.Combine(CaptureFolder, fileName + ".png");
                FrameCapture.WritePng(world, path);
                Debug.Log($"[ruins-capture] {path}");
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        private IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float deadline = Time.time + timeout;
            while (!condition())
            {
                Assert.Less(Time.time, deadline, $"Timed out waiting for: {what}");
                yield return null;
            }
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }
    }
}
