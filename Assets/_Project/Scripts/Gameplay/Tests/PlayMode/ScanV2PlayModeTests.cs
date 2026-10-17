using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// PlayMode verification and capture suite for Task SCAN v2: holds E with <see cref="InputTestFixture"/> on scan
    /// targets in front, beside, behind (a ruin piece placed behind 07), and up a slope; verifies 07 turns its head
    /// (and whole body when outside head reach), the beam never crosses 07, and every scan completes; and writes the
    /// ruin-behind frame strip and raster close-up captures to <c>Logs/scan/</c>.
    /// </summary>
    public sealed class ScanV2PlayModeTests : InputTestFixture
    {
        private const int PanelWidth = 960;
        private const int PanelHeight = 540;
        private const int CloseupWidth = 1600;
        private const int CloseupHeight = 900;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float ScanTimeout = 12f;

        private static readonly string ScanFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "scan"));

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private RoverBodyLanguage _bodyLanguage;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private IDisposable _awoke;
        private bool _awake;
        private Transform _headTransform;
        private Collider[] _roverColliders;
        private MeshFilter[] _roverMeshes;

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
        public IEnumerator HoldingE_ScansTargetsInFrontBesideBehindAndUpSlope_BeamNeverCrosses07_AndCapturesStrip()
        {
            Directory.CreateDirectory(ScanFolder);
            yield return BootRealScene();
            ClearBlockingVerbsAndOtherFragments();
            CacheRoverParts();

            // 1. In front + close-up raster capture
            yield return VerifyFrontTargetAndCaptureCloseup();

            // 2. Beside 07 (within head yaw reach, head turns without body turn, returns to rest on release)
            yield return VerifyBesideTarget();

            // 3. Behind 07 (ruin piece placed behind 07: body turns -> head aims -> raster sweeps -> completes)
            yield return VerifyBehindRuinAndCaptureStrip();

            // 4. Up a slope + player-wins steering check
            yield return VerifyUpSlopeTargetAndPlayerSteering();

            Assert.IsEmpty(_problems, "No unexpected errors:\n" + string.Join("\n", _problems));
        }

        private IEnumerator VerifyFrontTargetAndCaptureCloseup()
        {
            ScanField scans = _gameplay.Scans;
            ScanFragment target = scans.Find("rover.cargo_cradle.0");
            Assert.IsNotNull(target);
            ActivateOnly(target);
            Vector3 spot = OpenSpot();
            PlaceRoverAndFragment(spot, 0f, target, spot + new Vector3(0.15f, 0.95f, 3.4f));
            yield return WaitSeconds(0.4f);

            Assert.AreSame(target, scans.Candidate, "Front target must be selected.");
            Press(_keyboard.eKey);
            bool capturedCloseup = false;
            float deadline = Time.time + ScanTimeout;
            while (!target.Scanned)
            {
                Assert.Less(Time.time, deadline, "Front target scan must complete.");
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "front");
                    if (!capturedCloseup && target.Progress >= 0.62f)
                    {
                        CaptureRasterCloseup(target);
                        capturedCloseup = true;
                    }
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(0.2f);
            Assert.IsTrue(target.Scanned, "Front target scanned.");
            Assert.IsTrue(capturedCloseup, "Raster close-up captured.");
        }

        private IEnumerator VerifyBesideTarget()
        {
            ScanField scans = _gameplay.Scans;
            ScanFragment target = scans.Find("rover.cargo_cradle.1");
            Assert.IsNotNull(target);
            ActivateOnly(target);
            Vector3 spot = OpenSpot();
            // Place target 3.2m to 07's left-front (~68 deg yaw, inside head reach)
            PlaceRoverAndFragment(spot, 0f, target, spot + new Vector3(-2.9f, 0.95f, 1.2f));
            yield return WaitSeconds(0.4f);

            float startBodyYaw = _rover.Rotation.eulerAngles.y;
            Press(_keyboard.eKey);
            float maxHeadYaw = 0f;
            float deadline = Time.time + ScanTimeout;
            while (scans.Scanning != target || target.Progress < 0.18f)
            {
                Assert.Less(Time.time, deadline, "Head must turn toward beside target and begin scan.");
                maxHeadYaw = Mathf.Max(maxHeadYaw, Mathf.Abs(_bodyLanguage.CurrentHeadAngles.x));
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "beside-aim");
                }

                yield return null;
            }

            // Letting go of E returns the head to rest
            Release(_keyboard.eKey);
            yield return Until(() => Mathf.Abs(_bodyLanguage.CurrentHeadAngles.x) < 10f, 2.8f,
                "Letting go of E returns 07's head to rest.");

            // Hold E again to complete the beside scan
            Press(_keyboard.eKey);
            deadline = Time.time + ScanTimeout;
            while (!target.Scanned)
            {
                Assert.Less(Time.time, deadline, "Beside target scan must complete.");
                maxHeadYaw = Mathf.Max(maxHeadYaw, Mathf.Abs(_bodyLanguage.CurrentHeadAngles.x));
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "beside");
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.Greater(maxHeadYaw, 35f, "07's head must turn toward the target beside it.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(startBodyYaw, _rover.Rotation.eulerAngles.y)), 15f,
                "Target within head reach must not require a full body turn.");
        }

        private IEnumerator VerifyBehindRuinAndCaptureStrip()
        {
            ScanField scans = _gameplay.Scans;
            ScanFragment ruinTarget = scans.Find("ruin.FallenSolarArray.0");
            Assert.IsNotNull(ruinTarget, "FallenSolarArray ruin scan target must exist.");
            ActivateOnly(ruinTarget);
            Vector3 spot = OpenSpot();
            Transform ruinRoot = ruinTarget.Root.parent != null ? ruinTarget.Root.parent : ruinTarget.Root;
            foreach (Collider col in ruinRoot.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            Vector3 targetOffset = ruinTarget.Point - ruinRoot.position;
            Vector3 desiredPoint = spot + new Vector3(0.9f, 0.95f, -3.6f); // ~166 deg behind 07
            ruinRoot.position = desiredPoint - targetOffset;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, 0f, 0f));
            yield return WaitSeconds(0.45f);

            PositionStripCamera(spot, desiredPoint);
            float startYaw = _rover.Rotation.eulerAngles.y;
            var panels = new Texture2D[4];

            Press(_keyboard.eKey);
            yield return null;
            Assert.IsNull(scans.Scanning, "Scan must wait while target is behind 07 without line of sight.");

            float maxTurnDelta = 0f;
            float maxWheelScrub = 0f;
            float deadline = Time.time + ScanTimeout;
            while (!ruinTarget.Scanned)
            {
                Assert.Less(Time.time, deadline, "Behind ruin target scan must complete.");
                float turnDelta = Mathf.Abs(Mathf.DeltaAngle(startYaw, _rover.Rotation.eulerAngles.y));
                maxTurnDelta = Mathf.Max(maxTurnDelta, turnDelta);
                maxWheelScrub = Mathf.Max(maxWheelScrub, Mathf.Abs(_rover.ScanTurnScrub));

                if (panels[0] == null && turnDelta >= 45f)
                {
                    panels[0] = CapturePanel("ruin_behind_1_body_turn.png");
                }

                if (scans.Scanning == ruinTarget)
                {
                    AssertBeamClearOfRover(ruinTarget.Point, "behind-ruin");
                    if (panels[1] == null && ruinTarget.Progress >= 0.08f)
                    {
                        panels[1] = CapturePanel("ruin_behind_2_head_aim.png");
                    }

                    if (panels[2] == null && ruinTarget.Progress >= 0.58f)
                    {
                        panels[2] = CapturePanel("ruin_behind_3_raster_sweep.png");
                    }
                }

                yield return null;
            }

            yield return null;
            panels[3] = CapturePanel("ruin_behind_4_completion.png");
            Release(_keyboard.eKey);

            Assert.Greater(maxTurnDelta, 90f, "07 must turn its whole body toward a target behind it.");
            Assert.Greater(maxWheelScrub, 0.25f, "Whole-body scan turn must apply wheel scrub.");
            StitchStrip(panels, Path.Combine(ScanFolder, "ruin_behind_strip.png"));
        }

        private IEnumerator VerifyUpSlopeTargetAndPlayerSteering()
        {
            ScanField scans = _gameplay.Scans;
            ScanFragment slopeTarget = scans.Find("ruin.FallenSolarArray.1");
            Assert.IsNotNull(slopeTarget);
            ActivateOnly(slopeTarget);
            Vector3 spot = OpenSpot();
            Transform targetNode = slopeTarget.Root;
            targetNode.position = spot + new Vector3(-0.25f, 2.35f, 3.1f); // Elevated up a slope (~28 deg pitch)
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(-8f, 0f, 0f));
            yield return WaitSeconds(0.45f);

            // Player-wins check: holding D while holding E lets player steer immediately
            Press(_keyboard.eKey);
            Press(_keyboard.dKey);
            float yawBeforeSteer = _rover.Rotation.eulerAngles.y;
            yield return WaitSeconds(0.25f);
            float steerDelta = Mathf.DeltaAngle(yawBeforeSteer, _rover.Rotation.eulerAngles.y);
            Assert.Greater(steerDelta, 2f, "While the player steers, the player wins.");
            Release(_keyboard.dKey);

            float maxHeadPitch = 0f;
            float deadline = Time.time + ScanTimeout;
            while (!slopeTarget.Scanned)
            {
                Assert.Less(Time.time, deadline, "Up-slope target scan must complete.");
                maxHeadPitch = Mathf.Max(maxHeadPitch, _bodyLanguage.CurrentHeadAngles.y);
                if (scans.Scanning == slopeTarget)
                {
                    AssertBeamClearOfRover(slopeTarget.Point, "up-slope");
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.Greater(maxHeadPitch, 12f, "07's head must pitch up toward the elevated slope target.");
            Assert.IsTrue(slopeTarget.Scanned);
        }

        private void AssertBeamClearOfRover(Vector3 targetPoint, string label)
        {
            Transform lens = _rover.ScanOrigin;
            bool clear = ScanAim.HasLineOfSight(lens, targetPoint, 14f, _headTransform, _roverColliders, _roverMeshes);
            Assert.IsTrue(clear, $"Scan beam ({label}) must have line of sight and never cross 07.");
        }

        private void CaptureRasterCloseup(ScanFragment target)
        {
            Transform camTr = _view.transform;
            Vector3 savedPos = camTr.position;
            Quaternion savedRot = camTr.rotation;
            float savedFov = _view.fieldOfView;
            bool wasEnabled = _cameraRig.enabled;
            _cameraRig.enabled = false;
            try
            {
                Vector3 toRover = (_rover.ScanOrigin.position - target.Point).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, toRover).normalized;
                camTr.position = target.Point + toRover * 0.85f + side * 0.95f + Vector3.up * 0.28f;
                camTr.rotation = Quaternion.LookRotation((target.Point - camTr.position).normalized, Vector3.up);
                _view.fieldOfView = 32f;
                FrameCapture.SavePng(_view, CloseupWidth, CloseupHeight, Path.Combine(ScanFolder, "raster_closeup.png"));
            }
            finally
            {
                camTr.SetPositionAndRotation(savedPos, savedRot);
                _view.fieldOfView = savedFov;
                _cameraRig.enabled = wasEnabled;
            }
        }

        private void PositionStripCamera(Vector3 roverSpot, Vector3 targetPoint)
        {
            _cameraRig.enabled = false;
            Vector3 mid = (roverSpot + targetPoint) * 0.5f + Vector3.up * 0.75f;
            Vector3 camPos = mid + new Vector3(3.8f, 2.3f, 0.35f);
            _view.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation((mid - camPos).normalized, Vector3.up));
            _view.fieldOfView = 46f;
        }

        private Texture2D CapturePanel(string fileName)
        {
            string path = Path.Combine(ScanFolder, fileName);
            FrameCapture.SavePng(_view, PanelWidth, PanelHeight, path);
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }

        private static void StitchStrip(Texture2D[] panels, string outputPath)
        {
            var strip = new Texture2D(PanelWidth * panels.Length, PanelHeight, TextureFormat.RGBA32, false);
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    strip.SetPixels(i * PanelWidth, 0, PanelWidth, PanelHeight, panels[i].GetPixels());
                    Object.DestroyImmediate(panels[i]);
                }
            }

            strip.Apply();
            File.WriteAllBytes(outputPath, strip.EncodeToPNG());
            Object.DestroyImmediate(strip);
        }

        private IEnumerator BootRealScene()
        {
#if UNITY_EDITOR
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("ScanV2PlayModeTests requires EditorSceneManager.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _bodyLanguage = _bodyLanguage != null ? _bodyLanguage : root.GetComponentInChildren<RoverBodyLanguage>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            yield return Until(() => _awake, WakeTimeout, "07 wakes up");
            if (_cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(0.8f, 0f));
                yield return WaitSeconds(0.3f);
                Set(_gamepad.rightStick, Vector2.zero);
                yield return Until(() => !_cameraRig.Opening.IsActive, 8f, "wake opening handover");
            }
        }

        private void ClearBlockingVerbsAndOtherFragments()
        {
            foreach (ScanFragment frag in _gameplay.Scans.Fragments)
            {
                frag.Scanned = true;
            }
        }

        private void ActivateOnly(ScanFragment activeTarget)
        {
            foreach (ScanFragment frag in _gameplay.Scans.Fragments)
            {
                frag.Scanned = frag != activeTarget;
            }

            activeTarget.Progress = 0f;
        }

        private void CacheRoverParts()
        {
            _headTransform = _rover.transform.Find("Visual/Chassis/RoverModel/Neck/NeckLift/Head");
            _roverColliders = _rover.GetComponentsInChildren<Collider>(true);
            _roverMeshes = _rover.GetComponentsInChildren<MeshFilter>(true);
        }

        private Vector3 OpenSpot()
        {
            var terrain = _context.Get<ITerrainQuery>();
            var spot = new Vector3(18f, 0f, 18f);
            spot.y = terrain.SampleHeight(spot.x, spot.z);
            return spot;
        }

        private void PlaceRoverAndFragment(Vector3 roverSpot, float roverYaw, ScanFragment fragment, Vector3 targetPos)
        {
            _cameraRig.enabled = true;
            Vector3 offset = fragment.Point - fragment.Root.position;
            fragment.Root.position = targetPos - offset;
            _context.Get<IRoverPlacement>().PlaceAt(roverSpot, Quaternion.Euler(0f, roverYaw, 0f));
        }

        private static IEnumerator WaitSeconds(float seconds)
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
