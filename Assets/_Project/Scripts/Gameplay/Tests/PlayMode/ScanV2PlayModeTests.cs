using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    /// (and whole body when outside head reach), the scan line of sight never crosses 07, and every scan completes;
    /// and writes 1920x1080 chase-camera captures (ruin-behind 4 frames + strip, surface close-up, daylight scan, and
    /// night scan) to <c>Logs/scan/</c>.
    /// </summary>
    public sealed class ScanV2PlayModeTests : InputTestFixture
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const int StripPanelWidth = 960;
        private const int StripPanelHeight = 540;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float ScanTimeout = 14f;
        private const float SunlitTimeSeconds = 210f;
        private const float EarthlitTimeSeconds = 4816f;

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

            // 1. In front in daylight (chase camera 1920x1080) + close-up of the line on the surface (1920x1080)
            yield return VerifyFrontDaylightAndCaptureCloseup();

            // 2. Beside 07 at night (within head yaw reach, chase camera 1920x1080 night capture + let-go return check)
            yield return VerifyBesideNightAndCapture();

            // 3. Behind 07 (ruin piece placed behind 07: body turn -> head aim -> line sweeping -> completion ping)
            yield return VerifyBehindRuinAndCaptureStrip();

            // 4. Up a slope + player-wins steering check
            yield return VerifyUpSlopeTargetAndPlayerSteering();

            Assert.IsEmpty(_problems, "No unexpected errors:\n" + string.Join("\n", _problems));
        }

        private IEnumerator VerifyFrontDaylightAndCaptureCloseup()
        {
            SetSkyClock(SunlitTimeSeconds);
            ScanField scans = _gameplay.Scans;
            ScanFragment target = scans.Find("rover.cargo_cradle.0");
            Assert.IsNotNull(target);
            ActivateOnly(target);
            Vector3 spot = OpenSpot();
            ClearNearbyProps(spot, 14f, target.Root);

            Vector3 targetXZ = spot + new Vector3(-1.10f, 0f, -1.65f);
            float fragGroundY = SampleGround(targetXZ.x, targetXZ.z);
            PlaceRoverAndFragment(spot, -145f, target, new Vector3(targetXZ.x, fragGroundY + 0.22f, targetXZ.z));
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(-10f);
            yield return WaitSeconds(0.55f);

            Assert.AreSame(target, scans.Candidate, "Front target must be selected.");
            Press(_keyboard.eKey);
            bool capturedDaylight = false;
            bool capturedCloseup = false;
            float deadline = Time.time + ScanTimeout;
            while (!target.Scanned)
            {
                Assert.Less(Time.time, deadline, "Front target scan must complete.");
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "front-daylight");
                    if (!capturedDaylight && target.Progress >= 0.48f)
                    {
                        FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight,
                            Path.Combine(ScanFolder, "scan_daylight.png"));
                        capturedDaylight = true;
                    }

                    if (!capturedCloseup && target.Progress >= 0.58f)
                    {
                        CaptureRasterCloseup(target);
                        capturedCloseup = true;
                    }
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            // Wait for ScanLook's 0.9s post-scan dissolve gaze hold to release before the next placement
            yield return WaitSeconds(1.05f);
            Assert.IsTrue(target.Scanned, "Front target scanned.");
            Assert.IsTrue(capturedDaylight, "Daylight scan captured.");
            Assert.IsTrue(capturedCloseup, "Raster close-up captured.");
        }

        private IEnumerator VerifyBesideNightAndCapture()
        {
            SetSkyClock(EarthlitTimeSeconds);
            ScanField scans = _gameplay.Scans;
            ScanFragment target = scans.Find("rover.cargo_cradle.1");
            Assert.IsNotNull(target);
            ActivateOnly(target);
            Vector3 spot = OpenSpot();
            ClearNearbyProps(spot, 14f, target.Root);

            // Place target ~2.3m to 07's left-front (~64 deg yaw from -85 deg heading, inside head pitch & yaw reach)
            Vector3 targetXZ = spot + new Vector3(-1.15f, 0f, -1.95f);
            float groundY = SampleGround(targetXZ.x, targetXZ.z);
            PlaceRoverAndFragment(spot, -85f, target, new Vector3(targetXZ.x, groundY + 0.54f, targetXZ.z));
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(-8f);
            yield return WaitSeconds(0.25f);

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

            // Hold E again to complete the beside scan and capture night scan from chase camera
            Press(_keyboard.eKey);
            bool capturedNight = false;
            deadline = Time.time + ScanTimeout;
            while (!target.Scanned)
            {
                Assert.Less(Time.time, deadline, "Beside target scan must complete.");
                maxHeadYaw = Mathf.Max(maxHeadYaw, Mathf.Abs(_bodyLanguage.CurrentHeadAngles.x));
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "beside-night");
                    if (!capturedNight && target.Progress >= 0.54f)
                    {
                        FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight,
                            Path.Combine(ScanFolder, "scan_night.png"));
                        capturedNight = true;
                    }
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);
            Assert.Greater(maxHeadYaw, 32f, "07's head must turn toward the target beside it.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(startBodyYaw, _rover.Rotation.eulerAngles.y)), 15f,
                "Target within head reach must not require a full body turn.");
            Assert.IsTrue(capturedNight, "Night scan captured.");
        }

        private IEnumerator VerifyBehindRuinAndCaptureStrip()
        {
            SetSkyClock(SunlitTimeSeconds);
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

            // Hide the solar wings and severed cable so only the compact ToppledGearbox ruin piece stands in open view
            // with zero solar panels or props covering the frame.
            Transform wingLeft = ruinRoot.Find("SolarWingLeft");
            Transform wingRight = ruinRoot.Find("SolarWingRight");
            Transform cableStub = ruinRoot.Find("CableStub");
            if (wingLeft != null)
            {
                wingLeft.gameObject.SetActive(false);
            }

            if (wingRight != null)
            {
                wingRight.gameObject.SetActive(false);
            }

            if (cableStub != null)
            {
                cableStub.gameObject.SetActive(false);
            }

            ClearNearbyProps(spot, 16f, ruinRoot);

            // Place the ruin piece BEHIND 07 (~161 deg behind 07's initial +12 deg heading), resting upright on the
            // lunar surface in the foreground-left of the chase camera.
            Vector3 ruinXZ = spot + new Vector3(-1.05f, 0f, -1.75f);
            float ruinGroundY = SampleGround(ruinXZ.x, ruinXZ.z);
            Quaternion uprightRuinRot = Quaternion.Euler(0f, -25f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            ruinRoot.SetPositionAndRotation(new Vector3(ruinXZ.x, ruinGroundY, ruinXZ.z), uprightRuinRot);
            Transform gearbox = ruinRoot.Find("ToppledGearbox");
            if (gearbox != null && gearbox.TryGetComponent(out Renderer gearboxRenderer))
            {
                Bounds gb = gearboxRenderer.bounds;
                float liftY = ruinGroundY - gb.min.y;
                ruinRoot.position += new Vector3(0f, liftY, 0f);
                ruinTarget.Root.position = gearboxRenderer.bounds.center;
            }

            _cameraRig.enabled = true;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, 12f, 0f));
            _cameraRig.Snap();
            // Chase camera looking along -10 deg so ToppledGearbox is on the left and 07 turns toward the camera
            _cameraRig.Orbit.AdoptBearing(-10f);
            yield return WaitSeconds(0.65f);

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

                if (panels[0] == null && turnDelta >= 55f)
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

                    if (panels[2] == null && ruinTarget.Progress >= 0.54f)
                    {
                        panels[2] = CapturePanel("ruin_behind_3_line_sweeping.png");
                        File.Copy(
                            Path.Combine(ScanFolder, "ruin_behind_3_line_sweeping.png"),
                            Path.Combine(ScanFolder, "ruin_behind_3_raster_sweep.png"),
                            overwrite: true);
                    }
                }

                yield return null;
            }

            // Wait ~65 ms into the completion dissolve so the thin warm-amber completion ping edge is at peak
            yield return WaitSeconds(0.065f);
            panels[3] = CapturePanel("ruin_behind_4_completion_ping.png");
            File.Copy(
                Path.Combine(ScanFolder, "ruin_behind_4_completion_ping.png"),
                Path.Combine(ScanFolder, "ruin_behind_4_completion.png"),
                overwrite: true);

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);

            Assert.Greater(maxTurnDelta, 90f, "07 must turn its whole body toward a target behind it.");
            Assert.Greater(maxWheelScrub, 0.25f, "Whole-body scan turn must apply wheel scrub.");
            StitchStrip(panels, Path.Combine(ScanFolder, "ruin_behind_strip.png"));
        }

        private IEnumerator VerifyUpSlopeTargetAndPlayerSteering()
        {
            _cameraRig.enabled = true;
            ScanField scans = _gameplay.Scans;
            ScanFragment slopeTarget = scans.Find("ruin.FallenSolarArray.1");
            Assert.IsNotNull(slopeTarget);
            ActivateOnly(slopeTarget);
            Vector3 spot = OpenSpot();
            Transform targetNode = slopeTarget.Root;
            targetNode.position = spot + new Vector3(-0.25f, 2.35f, 3.1f); // Elevated up a slope (~28 deg pitch)
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(-8f, 0f, 0f));
            _cameraRig.Snap();
            yield return WaitSeconds(0.45f);

            // Player-wins check: holding D while holding E lets player steer immediately
            Press(_keyboard.eKey);
            yield return null;
            Press(_keyboard.dKey);
            float yawBeforeSteer = _rover.Rotation.eulerAngles.y;
            yield return WaitSeconds(0.25f);
            float steerDelta = Mathf.DeltaAngle(yawBeforeSteer, _rover.Rotation.eulerAngles.y);
            Assert.Greater(steerDelta, 2f, "While the player steers, the player wins.");
            Release(_keyboard.dKey);
            yield return null;

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
            Transform brackets = target.Root != null ? target.Root.Find("Brackets") : null;
            bool bracketsWereActive = brackets != null && brackets.gameObject.activeSelf;
            if (brackets != null)
            {
                brackets.gameObject.SetActive(false);
            }

            _cameraRig.enabled = false;
            try
            {
                Vector3 toRover = (_rover.ScanOrigin.position - target.Point).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, toRover).normalized;
                Vector3 focus = target.Point - Vector3.up * 0.03f;
                camTr.position = focus + toRover * 0.36f + side * 1.12f + Vector3.up * 0.32f;
                camTr.rotation = Quaternion.LookRotation((focus - camTr.position).normalized, Vector3.up);
                _view.fieldOfView = 27f;
                FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, Path.Combine(ScanFolder, "raster_closeup.png"));
            }
            finally
            {
                if (brackets != null)
                {
                    brackets.gameObject.SetActive(bracketsWereActive);
                }

                camTr.SetPositionAndRotation(savedPos, savedRot);
                _view.fieldOfView = savedFov;
                _cameraRig.enabled = wasEnabled;
            }
        }

        private Texture2D CapturePanel(string fileName)
        {
            string path = Path.Combine(ScanFolder, fileName);
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, path);

            string tempStripPanel = Path.Combine(ScanFolder, "_tmp_" + fileName);
            FrameCapture.SavePng(_view, StripPanelWidth, StripPanelHeight, tempStripPanel);
            byte[] bytes = File.ReadAllBytes(tempStripPanel);
            File.Delete(tempStripPanel);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }

        private static void StitchStrip(Texture2D[] panels, string outputPath)
        {
            var strip = new Texture2D(StripPanelWidth * panels.Length, StripPanelHeight, TextureFormat.RGBA32, false);
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                {
                    strip.SetPixels(i * StripPanelWidth, 0, StripPanelWidth, StripPanelHeight, panels[i].GetPixels());
                    Object.DestroyImmediate(panels[i]);
                }
            }

            strip.Apply();
            File.WriteAllBytes(outputPath, strip.EncodeToPNG());
            Object.DestroyImmediate(strip);
        }

        private void SetSkyClock(float seconds)
        {
            if (_context.TryGet(out ISunlight sunlight) && sunlight != null)
            {
                MethodInfo method = sunlight.GetType().GetMethod("SetClockTime",
                    new[] { typeof(float), typeof(bool), typeof(bool) });
                method?.Invoke(sunlight, new object[] { seconds, false, true });
            }
        }

        private void ClearNearbyProps(Vector3 center, float radius, Transform keepRoot)
        {
            float radiusSq = radius * radius;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || !r.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    Transform tr = r.transform;
                    if (tr.IsChildOf(_rover.transform) || (keepRoot != null && tr.IsChildOf(keepRoot)))
                    {
                        continue;
                    }

                    Bounds b = r.bounds;
                    if (b.size.x > 18f || b.size.z > 18f)
                    {
                        continue; // Keep terrain chunks
                    }

                    Vector2 delta = new Vector2(b.center.x - center.x, b.center.z - center.z);
                    if (delta.sqrMagnitude <= radiusSq)
                    {
                        r.enabled = false;
                    }
                }
            }
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
            activeTarget.Scanned = false;
            activeTarget.Gone = false;
            activeTarget.Draw(0f);
            activeTarget.Dissolve(0f);
        }

        private void CacheRoverParts()
        {
            _headTransform = _rover.transform.Find("Visual/Chassis/RoverModel/Neck/NeckLift/Head");
            _roverColliders = _rover.GetComponentsInChildren<Collider>(true);
            _roverMeshes = _rover.GetComponentsInChildren<MeshFilter>(true);
        }

        private float SampleGround(float x, float z)
        {
            return _context.Get<ITerrainQuery>().SampleHeight(x, z);
        }

        private Vector3 OpenSpot()
        {
            var spot = new Vector3(24f, 0f, 22f);
            spot.y = SampleGround(spot.x, spot.z);
            return spot;
        }

        private void PlaceRoverAndFragment(Vector3 roverSpot, float roverYaw, ScanFragment fragment, Vector3 targetPos)
        {
            _cameraRig.enabled = true;
            fragment.Root.gameObject.SetActive(true);
            foreach (Renderer r in fragment.Root.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }

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

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                _problems.Add($"{type}: {condition}");
            }
        }
    }
}
