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
        // Largest change of any channel's share of the body's total colour between scan frames: plain shading moves it
        // by about 0.01, a cyan tint by about 0.1.
        private const float BodyChromaticityLimit = 0.04f;

        private IDisposable _awoke;
        private bool _awake;
        private Transform _headTransform;
        private Transform _bodyTransform;
        private Collider[] _roverColliders;
        private MeshFilter[] _roverMeshes;

        public override void Setup()
        {
            base.Setup();
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
            Time.captureDeltaTime = FrameTime;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = true;
#endif
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

        private static readonly int RetiredGroundStartShaderId = Shader.PropertyToID("_MoonScanGroundStart");

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

            // 1. Daylight scan of a target 6-8 m away (~6.5 m) on open ground + close-up of 07's lens while scanning
            yield return VerifyFrontDaylightAndCaptureCloseup();

            // 2. Beside 07 head-turn & let-go verification + night scan of the 6-8 m (~6.5 m) target
            yield return VerifyBesideNightAndCapture();

            // 3. Behind 07 (ruin piece 6.5 m behind 07: body turn -> head aim -> line sweeping -> completion ping)
            yield return VerifyBehindRuinAndCaptureStrip();

            // 4. Up a slope + player-wins steering check
            yield return VerifyUpSlopeTargetAndPlayerSteering();

            Assert.IsEmpty(_problems, "No unexpected errors:\n" + string.Join("\n", _problems));
        }

        private IEnumerator VerifyFrontDaylightAndCaptureCloseup()
        {
            SetSkyClock(SunlitTimeSeconds);
            ScanField scans = _gameplay.Scans;

            // First verify a ruin target scan in front (ruin.HabitatModules.0)
            ScanFragment frontTarget = scans.Find("ruin.HabitatModules.0");
            Assert.IsNotNull(frontTarget);
            ActivateOnly(frontTarget);
            Vector3 spot = OpenSpot();
            ClearNearbyProps(spot, 16f, frontTarget.Root);

            Vector3 frontXZ = spot + new Vector3(-1.10f, 0f, -1.65f);
            float frontGroundY = SampleGround(frontXZ.x, frontXZ.z);
            PlaceRoverAndFragment(spot, -145f, frontTarget, new Vector3(frontXZ.x, frontGroundY + 0.22f, frontXZ.z));
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(-10f);
            yield return WaitSeconds(0.35f);

            Assert.AreSame(frontTarget, scans.Candidate, "Front target must be selected.");
            Press(_keyboard.eKey);
            float deadline = Time.time + ScanTimeout;
            while (!frontTarget.Scanned)
            {
                Assert.Less(Time.time, deadline, "Front target scan must complete.");
                if (scans.Scanning == frontTarget)
                {
                    AssertBeamClearOfRover(frontTarget.Point, "front");
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);
            Assert.IsTrue(frontTarget.Scanned, "Front target scanned.");

            // Now scan a target 6.5 m away (6-8 m) on open ground in daylight for scan_daylight.png + lens_closeup.png
            ScanFragment distantTarget = PrepareOpenGroundRuinTarget(spot, new Vector3(-2.95f, 0f, -5.79f), out float distanceMeters);
            Assert.GreaterOrEqual(distanceMeters, 6.0f, "Target must be at least 6 m away.");
            Assert.LessOrEqual(distanceMeters, 8.0f, "Target must be within 8 m scan reach.");

            _cameraRig.enabled = true;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, -125f, 0f));
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(-112f);
            yield return WaitSeconds(0.55f);

            Assert.AreSame(distantTarget, scans.Candidate, "6.5 m daylight target must be selected.");
            Color preScanDaylightBodyColor = SampleRoverBodyAverageColorFromCamera(_view, CaptureWidth, CaptureHeight);

            Press(_keyboard.eKey);
            bool capturedDaylight = false;
            bool capturedLensCloseup = false;
            Vector3? earlyWedgeTarget = null;
            deadline = Time.time + ScanTimeout;
            while (!distantTarget.Scanned)
            {
                Assert.Less(Time.time, deadline, "6.5 m daylight target scan must complete.");
                if (scans.Scanning == distantTarget)
                {
                    AssertBeamClearOfRover(distantTarget.Point, "front-daylight-6.5m");
                    if (!earlyWedgeTarget.HasValue && distantTarget.Progress >= 0.18f)
                    {
                        earlyWedgeTarget = AssertScanBeamWedgeActiveAndTapered();
                    }

                    if (!capturedDaylight && distantTarget.Progress >= 0.48f)
                    {
                        Vector4 retiredGround = Shader.GetGlobalVector(RetiredGroundStartShaderId);
                        Assert.AreEqual(0f, retiredGround.w, 1e-4f, "Retired ground scan line shader global must remain inactive.");
                        Vector3 midWedgeTarget = AssertScanBeamWedgeActiveAndTapered();
                        if (earlyWedgeTarget.HasValue)
                        {
                            // Up a tall target, across a flat one's top: the far edge rides the tilted sweep line.
                            Assert.Greater(Vector3.Distance(midWedgeTarget, earlyWedgeTarget.Value), 0.04f,
                                "Scan beam wedge far edge must move over the target with the surface scan sweep.");
                        }

                        string daylightPath = Path.Combine(ScanFolder, "scan_daylight.png");
                        FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, daylightPath);
                        AssertNoBrightRegionOutsideLensAndTarget(daylightPath, distantTarget, 0.90f, "scan_daylight.png");
                        Color duringScanDaylightBodyColor = SampleRoverBodyAverageColorFromPng(daylightPath, _view);
                        AssertRoverBodyColorUnchanged(preScanDaylightBodyColor, duringScanDaylightBodyColor, "scan_daylight.png", 0.04f);
                        capturedDaylight = true;
                    }

                    if (!capturedLensCloseup && distantTarget.Progress >= 0.56f)
                    {
                        CaptureLensCloseup(distantTarget);
                        capturedLensCloseup = true;
                    }
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);
            Assert.IsTrue(distantTarget.Scanned, "6.5 m daylight target scanned.");
            Assert.IsTrue(capturedDaylight, "Daylight 6.5 m scan captured.");
            Assert.IsTrue(capturedLensCloseup, "Close-up of 07's lens while scanning captured.");
        }

        private IEnumerator VerifyBesideNightAndCapture()
        {
            SetSkyClock(EarthlitTimeSeconds);
            ScanField scans = _gameplay.Scans;
            ScanFragment target = scans.Find("ruin.HabitatModules.1");
            Assert.IsNotNull(target);
            ActivateOnly(target);
            Vector3 spot = OpenSpot();
            ClearNearbyProps(spot, 16f, target.Root);

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

            // Hold E again to complete the beside scan
            Press(_keyboard.eKey);
            deadline = Time.time + ScanTimeout;
            while (!target.Scanned)
            {
                Assert.Less(Time.time, deadline, "Beside target scan must complete.");
                maxHeadYaw = Mathf.Max(maxHeadYaw, Mathf.Abs(_bodyLanguage.CurrentHeadAngles.x));
                if (scans.Scanning == target)
                {
                    AssertBeamClearOfRover(target.Point, "beside-night");
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);
            Assert.Greater(maxHeadYaw, 32f, "07's head must turn toward the target beside it.");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(startBodyYaw, _rover.Rotation.eulerAngles.y)), 15f,
                "Target within head reach must not require a full body turn.");

            // Capture the 6.5 m (6-8 m) scan at night (scan_night.png) matching scan_daylight.png
            ScanFragment nightDistantTarget = PrepareOpenGroundRuinTarget(spot, new Vector3(-2.95f, 0f, -5.79f), out float nightDist);
            Assert.GreaterOrEqual(nightDist, 6.0f);
            Assert.LessOrEqual(nightDist, 8.0f);

            _cameraRig.enabled = true;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, -125f, 0f));
            _cameraRig.Snap();
            _cameraRig.Orbit.AdoptBearing(-112f);
            yield return WaitSeconds(0.45f);

            Press(_keyboard.eKey);
            bool capturedNight = false;
            deadline = Time.time + ScanTimeout;
            while (!nightDistantTarget.Scanned)
            {
                Assert.Less(Time.time, deadline, "6.5 m night target scan must complete.");
                if (scans.Scanning == nightDistantTarget)
                {
                    AssertBeamClearOfRover(nightDistantTarget.Point, "night-6.5m");
                    if (!capturedNight && nightDistantTarget.Progress >= 0.50f)
                    {
                        AssertScanBeamWedgeActiveAndTapered();
                        string nightPath = Path.Combine(ScanFolder, "scan_night.png");
                        FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, nightPath);
                        AssertNoBrightRegionOutsideLensAndTarget(nightPath, nightDistantTarget, 0.58f, "scan_night.png");
                        capturedNight = true;
                    }
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);
            Assert.IsTrue(capturedNight, "Night 6.5 m scan captured.");
        }

        private IEnumerator VerifyBehindRuinAndCaptureStrip()
        {
            SetSkyClock(SunlitTimeSeconds);
            ScanField scans = _gameplay.Scans;
            Vector3 spot = OpenSpot();

            // Place the ruin piece 6.5 m BEHIND 07 (~168 deg behind 07's initial +15 deg heading) on open regolith
            ScanFragment ruinTarget = PrepareOpenGroundRuinTarget(spot, new Vector3(-2.95f, 0f, -5.79f), out float ruinDist);
            Assert.GreaterOrEqual(ruinDist, 6.0f);
            Assert.LessOrEqual(ruinDist, 8.0f);

            _cameraRig.enabled = true;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, 15f, 0f));
            _cameraRig.Snap();
            // Chase camera looking along -112 deg so 07 is in the lower-right foreground and the 6.5 m lens beam wedge +
            // ballistic glints + ToppledGearbox ruin piece stretch diagonally across open ground to the upper-left.
            _cameraRig.Orbit.AdoptBearing(-112f);
            yield return WaitSeconds(0.65f);

            float startYaw = _rover.Rotation.eulerAngles.y;
            var panels = new Texture2D[4];

            Press(_keyboard.eKey);
            yield return null;
            Assert.IsNull(scans.Scanning, "Scan must wait while target is behind 07 without line of sight.");

            float maxTurnDelta = 0f;
            float maxWheelScrub = 0f;
            Color stripFrame1BodyColor = Color.black;
            Color stripFrame2BodyColor = Color.black;
            Color stripFrame3BodyColor = Color.black;
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
                    stripFrame1BodyColor = SampleRoverBodyAverageColorFromPng(
                        Path.Combine(ScanFolder, "ruin_behind_1_body_turn.png"), _view);
                }

                if (scans.Scanning == ruinTarget)
                {
                    AssertBeamClearOfRover(ruinTarget.Point, "behind-ruin");
                    if (panels[1] == null && ruinTarget.Progress >= 0.10f)
                    {
                        panels[1] = CapturePanel("ruin_behind_2_head_aim.png");
                        stripFrame2BodyColor = SampleRoverBodyAverageColorFromPng(
                            Path.Combine(ScanFolder, "ruin_behind_2_head_aim.png"), _view);
                        AssertRoverBodyChromaticityUnchanged(stripFrame1BodyColor, stripFrame2BodyColor, "scan_strip frame 2 vs frame 1");
                    }

                    if (panels[2] == null && ruinTarget.Progress >= 0.52f)
                    {
                        string sweepPath = Path.Combine(ScanFolder, "ruin_behind_3_line_sweeping.png");
                        panels[2] = CapturePanel("ruin_behind_3_line_sweeping.png");
                        AssertNoBrightRegionOutsideLensAndTarget(sweepPath, ruinTarget, 0.90f, "ruin_behind_3_line_sweeping.png");
                        stripFrame3BodyColor = SampleRoverBodyAverageColorFromPng(sweepPath, _view);
                        AssertRoverBodyChromaticityUnchanged(stripFrame1BodyColor, stripFrame3BodyColor, "scan_strip frame 3 vs frame 1");
                        AssertRoverBodyChromaticityUnchanged(stripFrame2BodyColor, stripFrame3BodyColor, "scan_strip frame 3 vs frame 2");
                        File.Copy(
                            sweepPath,
                            Path.Combine(ScanFolder, "ruin_behind_3_raster_sweep.png"),
                            overwrite: true);
                    }
                }

                yield return null;
            }

            // Wait ~110 ms into the completion dissolve so the warm-amber completion ping edge is at peak while the lens beam ramps off
            yield return WaitSeconds(0.11f);
            panels[3] = CapturePanel("ruin_behind_4_completion_ping.png");
            string frame4Path = Path.Combine(ScanFolder, "ruin_behind_4_completion_ping.png");
            Color stripFrame4BodyColor = SampleRoverBodyAverageColorFromPng(frame4Path, _view);
            AssertRoverBodyChromaticityUnchanged(stripFrame1BodyColor, stripFrame4BodyColor, "scan_strip frame 4 vs frame 1");
            File.Copy(
                frame4Path,
                Path.Combine(ScanFolder, "ruin_behind_4_completion.png"),
                overwrite: true);

            Release(_keyboard.eKey);
            yield return WaitSeconds(1.05f);

            Assert.Greater(maxTurnDelta, 90f, "07 must turn its whole body toward a target behind it.");
            Assert.Greater(maxWheelScrub, 0.25f, "Whole-body scan turn must apply wheel scrub.");
            string stripPath = Path.Combine(ScanFolder, "ruin_behind_strip.png");
            StitchStrip(panels, stripPath);
            File.Copy(stripPath, Path.Combine(ScanFolder, "scan_strip.png"), overwrite: true);
        }

        private ScanFragment PrepareOpenGroundRuinTarget(Vector3 spot, Vector3 offsetXZ, out float horizontalDistance)
        {
            ScanFragment ruinTarget = _gameplay.Scans.Find("ruin.FallenSolarArray.0");
            Assert.IsNotNull(ruinTarget, "FallenSolarArray ruin scan target must exist.");
            ActivateOnly(ruinTarget);
            Transform ruinRoot = ruinTarget.Root.parent != null ? ruinTarget.Root.parent : ruinTarget.Root;
            ruinRoot.gameObject.SetActive(true);
            foreach (Collider col in ruinRoot.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

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

            ClearNearbyProps(spot, 18f, ruinRoot);

            Vector3 ruinXZ = spot + offsetXZ;
            float ruinGroundY = SampleGround(ruinXZ.x, ruinXZ.z);
            Quaternion uprightRuinRot = Quaternion.Euler(0f, 22f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            ruinRoot.SetPositionAndRotation(new Vector3(ruinXZ.x, ruinGroundY, ruinXZ.z), uprightRuinRot);
            Transform gearbox = ruinRoot.Find("ToppledGearbox");
            if (gearbox != null && gearbox.TryGetComponent(out Renderer gearboxRenderer))
            {
                gearboxRenderer.enabled = true;
                Bounds gb = gearboxRenderer.bounds;
                float liftY = ruinGroundY - gb.min.y;
                ruinRoot.position += new Vector3(0f, liftY, 0f);
                ruinTarget.Root.position = gearboxRenderer.bounds.center;
            }

            horizontalDistance = new Vector2(ruinTarget.Point.x - spot.x, ruinTarget.Point.z - spot.z).magnitude;
            return ruinTarget;
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

        private Vector3 AssertScanBeamWedgeActiveAndTapered()
        {
            Transform wedgeTr = null;
            foreach (Transform child in _gameplay.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "ScanBeamWedge")
                {
                    wedgeTr = child;
                    break;
                }
            }

            Assert.IsNotNull(wedgeTr, "ScanBeamWedge GameObject must exist under GameplaySystem.");
            var mr = wedgeTr.GetComponent<MeshRenderer>();
            var mf = wedgeTr.GetComponent<MeshFilter>();
            Assert.IsNotNull(mr, "ScanBeamWedge must have a MeshRenderer.");
            Assert.IsNotNull(mf, "ScanBeamWedge must have a MeshFilter.");
            Assert.IsTrue(mr.enabled, "ScanBeamWedge MeshRenderer must be enabled while scanning.");

            Mesh mesh = mf.sharedMesh;
            Assert.IsNotNull(mesh, "ScanBeamWedge must have a dynamic mesh.");
            Vector3[] verts = mesh.vertices;
            Color[] colors = mesh.colors;
            const int rings = 16;
            const int slices = 9;
            int centerSlice = slices / 2;

            Vector3 lensCenter = verts[centerSlice];
            Vector3 targetCenter = verts[(rings - 1) * slices + centerSlice];
            Assert.Less(Vector3.Distance(lensCenter, _rover.ScanOrigin.position), 0.12f,
                "ScanBeamWedge must originate at 07's cheek lens (ScanOrigin).");

            float lensWidth = Vector3.Distance(verts[0], verts[slices - 1]);
            float targetWidth = Vector3.Distance(verts[(rings - 1) * slices], verts[(rings - 1) * slices + slices - 1]);
            Assert.Greater(targetWidth, lensWidth * 3.5f,
                "ScanBeamWedge must taper from a narrow lens aperture to a wider fan spanning the target scan line.");

            float lensAlpha = colors[centerSlice].a;
            float midAlpha = colors[(rings / 2) * slices + centerSlice].a;
            float targetAlpha = colors[(rings - 1) * slices + centerSlice].a;
            Assert.Greater(lensAlpha, midAlpha * 3f, "ScanBeamWedge must be brightest at the lens.");
            Assert.Greater(targetAlpha, midAlpha * 2.5f, "ScanBeamWedge must brighten where it meets the target.");
            Assert.LessOrEqual(midAlpha, 0.08f, "ScanBeamWedge must have very low alpha in the middle.");

            return targetCenter;
        }

        private void AssertNoBrightRegionOutsideLensAndTarget(
            string pngPath,
            ScanFragment target,
            float maxAllowedBrightness,
            string label)
        {
            byte[] bytes = File.ReadAllBytes(pngPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            try
            {
                Color32[] pixels = tex.GetPixels32();
                int w = tex.width;
                int h = tex.height;

                Rect lensRect = ProjectSphereToScreenRect(_view, _rover.ScanOrigin.position, 0.42f, w, h, 22f);
                Rect targetRect = ProjectTargetToScreenRect(_view, target, w, h, 26f);
                Rect roverRect = ProjectRoverToScreenRect(_view, w, h);

                const int blockSize = 8;
                float maxBrightness = 0f;
                Vector2Int maxBrightPos = Vector2Int.zero;
                Color maxBrightColor = Color.black;

                float maxCyanBrightness = 0f;
                Vector2Int maxCyanPos = Vector2Int.zero;
                Color maxCyanColor = Color.black;

                float maxBlueExcess = 0f;
                Vector2Int maxBluePos = Vector2Int.zero;
                Color maxBlueColor = Color.black;

                for (int by = 0; by <= h - blockSize; by += blockSize)
                {
                    for (int bx = 0; bx <= w - blockSize; bx += blockSize)
                    {
                        var blockRect = new Rect(bx, by, blockSize, blockSize);
                        if (blockRect.Overlaps(lensRect) || blockRect.Overlaps(targetRect))
                        {
                            continue;
                        }

                        float sumR = 0f;
                        float sumG = 0f;
                        float sumB = 0f;
                        for (int py = 0; py < blockSize; py++)
                        {
                            int rowOffset = (by + py) * w + bx;
                            for (int px = 0; px < blockSize; px++)
                            {
                                Color32 c = pixels[rowOffset + px];
                                sumR += c.r;
                                sumG += c.g;
                                sumB += c.b;
                            }
                        }

                        const float invScale = 1f / (blockSize * blockSize * 255f);
                        float avgR = sumR * invScale;
                        float avgG = sumG * invScale;
                        float avgB = sumB * invScale;

                        float brightness = Mathf.Max(avgR, Mathf.Max(avgG, avgB));

                        // 07's own paint in direct sun is legitimately near white: only the cyan and blue checks
                        // below cover it, and the body colour check compares it with the pre-scan frame.
                        if (brightness > maxBrightness && !blockRect.Overlaps(roverRect))
                        {
                            maxBrightness = brightness;
                            maxBrightPos = new Vector2Int(bx, by);
                            maxBrightColor = new Color(avgR, avgG, avgB, 1f);
                        }

                        float blueExcess = avgB - avgR;
                        if (blueExcess > 0.04f && brightness > maxCyanBrightness)
                        {
                            maxCyanBrightness = brightness;
                            maxCyanPos = new Vector2Int(bx, by);
                            maxCyanColor = new Color(avgR, avgG, avgB, 1f);
                        }

                        if (blueExcess > maxBlueExcess)
                        {
                            maxBlueExcess = blueExcess;
                            maxBluePos = new Vector2Int(bx, by);
                            maxBlueColor = new Color(avgR, avgG, avgB, 1f);
                        }
                    }
                }

                Debug.Log($"[scan-check] {label}: maxBrightnessOutside={maxBrightness:F3} (limit {maxAllowedBrightness:F2}) at {maxBrightPos} rgb=({maxBrightColor.r:F2},{maxBrightColor.g:F2},{maxBrightColor.b:F2}), " +
                          $"maxCyanBrightnessOutside={maxCyanBrightness:F3} at {maxCyanPos} rgb=({maxCyanColor.r:F2},{maxCyanColor.g:F2},{maxCyanColor.b:F2}), " +
                          $"maxBlueCyanExcessOutside={maxBlueExcess:F3} at {maxBluePos} rgb=({maxBlueColor.r:F2},{maxBlueColor.g:F2},{maxBlueColor.b:F2}), " +
                          $"lensRect={lensRect}, targetRect={targetRect}, roverRect={roverRect}");

                Assert.LessOrEqual(maxBrightness, maxAllowedBrightness,
                    $"{label}: screen region at {maxBrightPos} outside 07's lens and target surface exceeded brightness threshold " +
                    $"({maxBrightness:F3} > {maxAllowedBrightness:F2}, rgb=({maxBrightColor.r:F2},{maxBrightColor.g:F2},{maxBrightColor.b:F2})).");
                Assert.LessOrEqual(maxCyanBrightness, 0.65f,
                    $"{label}: cyan/blue screen region at {maxCyanPos} outside 07's lens and target surface exceeded brightness threshold " +
                    $"({maxCyanBrightness:F3} > 0.65, rgb=({maxCyanColor.r:F2},{maxCyanColor.g:F2},{maxCyanColor.b:F2})).");
                Assert.LessOrEqual(maxBlueExcess, 0.25f,
                    $"{label}: blue/cyan bloom blob detected at {maxBluePos} outside 07's lens and target surface " +
                    $"(blueCyanExcess={maxBlueExcess:F3} > 0.25, rgb=({maxBlueColor.r:F2},{maxBlueColor.g:F2},{maxBlueColor.b:F2})).");
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        private static Rect ProjectSphereToScreenRect(
            Camera cam,
            Vector3 center,
            float radius,
            int screenW,
            int screenH,
            float padPx)
        {
            float minX = screenW;
            float minY = screenH;
            float maxX = 0f;
            float maxY = 0f;

            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    for (int dz = -1; dz <= 1; dz += 2)
                    {
                        Vector3 wp = center + new Vector3(dx * radius, dy * radius, dz * radius);
                        Vector3 sp = cam.WorldToViewportPoint(wp);
                        if (sp.z > 0f)
                        {
                            float px = sp.x * screenW;
                            float py = sp.y * screenH;
                            minX = Mathf.Min(minX, px);
                            minY = Mathf.Min(minY, py);
                            maxX = Mathf.Max(maxX, px);
                            maxY = Mathf.Max(maxY, py);
                        }
                    }
                }
            }

            return Rect.MinMaxRect(minX - padPx, minY - padPx, maxX + padPx, maxY + padPx);
        }

        private static Rect ProjectTargetToScreenRect(
            Camera cam,
            ScanFragment target,
            int screenW,
            int screenH,
            float padPx)
        {
            Transform root = target.Root.parent != null ? target.Root.parent : target.Root;
            bool hasBounds = false;
            Bounds combined = default;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                {
                    if (!hasBounds)
                    {
                        combined = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(r.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                return ProjectSphereToScreenRect(cam, target.Point, 0.45f, screenW, screenH, padPx);
            }

            Vector3 min = combined.min;
            Vector3 max = combined.max;
            float minX = screenW;
            float minY = screenH;
            float maxX = 0f;
            float maxY = 0f;

            for (int ix = 0; ix <= 1; ix++)
            {
                for (int iy = 0; iy <= 1; iy++)
                {
                    for (int iz = 0; iz <= 1; iz++)
                    {
                        Vector3 corner = new Vector3(
                            ix == 0 ? min.x : max.x,
                            iy == 0 ? min.y : max.y,
                            iz == 0 ? min.z : max.z);
                        Vector3 sp = cam.WorldToViewportPoint(corner);
                        if (sp.z > 0f)
                        {
                            float px = sp.x * screenW;
                            float py = sp.y * screenH;
                            minX = Mathf.Min(minX, px);
                            minY = Mathf.Min(minY, py);
                            maxX = Mathf.Max(maxX, px);
                            maxY = Mathf.Max(maxY, py);
                        }
                    }
                }
            }

            return Rect.MinMaxRect(minX - padPx, minY - padPx, maxX + padPx, maxY + padPx);
        }

        private Rect ProjectRoverToScreenRect(Camera cam, int screenW, int screenH)
        {
            Rect union = Rect.zero;
            bool any = false;
            foreach (MeshRenderer meshRenderer in _rover.GetComponentsInChildren<MeshRenderer>())
            {
                Bounds bounds = meshRenderer.bounds;
                Rect rect = ProjectSphereToScreenRect(cam, bounds.center, bounds.extents.magnitude, screenW, screenH, 0f);
                union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, rect.xMin), Mathf.Min(union.yMin, rect.yMin),
                    Mathf.Max(union.xMax, rect.xMax), Mathf.Max(union.yMax, rect.yMax)) : rect;
                any = true;
            }

            return union;
        }

        private Color SampleRoverBodyAverageColorFromCamera(Camera cam, int width, int height)
        {
            Texture2D tex = FrameCapture.Render(cam, width, height);
            try
            {
                return SampleRoverBodyAverageColor(tex, cam);
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        private Color SampleRoverBodyAverageColorFromPng(string pngPath, Camera cam)
        {
            byte[] bytes = File.ReadAllBytes(pngPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            try
            {
                return SampleRoverBodyAverageColor(tex, cam);
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        private Color SampleRoverBodyAverageColor(Texture2D tex, Camera cam)
        {
            int w = tex.width;
            int h = tex.height;
            Vector3 bodyCenter = _rover.Position + Vector3.up * 0.36f;
            if (_bodyTransform != null && _bodyTransform.TryGetComponent(out Renderer bodyRenderer))
            {
                bodyCenter = bodyRenderer.bounds.center;
            }

            Rect bodyRect = ProjectSphereToScreenRect(cam, bodyCenter, 0.16f, w, h, 0f);
            Rect lensRect = ProjectSphereToScreenRect(cam, _rover.ScanOrigin.position, 0.10f, w, h, 4f);

            int minX = Mathf.Clamp(Mathf.RoundToInt(bodyRect.xMin), 0, w - 1);
            int maxX = Mathf.Clamp(Mathf.RoundToInt(bodyRect.xMax), 0, w - 1);
            int minY = Mathf.Clamp(Mathf.RoundToInt(bodyRect.yMin), 0, h - 1);
            int maxY = Mathf.Clamp(Mathf.RoundToInt(bodyRect.yMax), 0, h - 1);

            Color32[] pixels = tex.GetPixels32();
            double sumR = 0.0;
            double sumG = 0.0;
            double sumB = 0.0;
            int count = 0;

            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * w;
                for (int x = minX; x <= maxX; x++)
                {
                    if (lensRect.Contains(new Vector2(x, y)))
                    {
                        continue;
                    }

                    Color32 c = pixels[rowOffset + x];
                    sumR += c.r;
                    sumG += c.g;
                    sumB += c.b;
                    count++;
                }
            }

            Assert.Greater(count, 64, "Projected 07 body sample region must contain enough pixels.");
            double inv = 1.0 / (count * 255.0);
            return new Color((float)(sumR * inv), (float)(sumG * inv), (float)(sumB * inv), 1f);
        }

        /// <summary>
        /// The strip frames catch 07 mid-turn, so the sun lights its body differently in each and its brightness
        /// legitimately changes. A scan tint changes the colour balance instead, so the frames are compared by each
        /// channel's share of the total.
        /// </summary>
        private static void AssertRoverBodyChromaticityUnchanged(Color before, Color during, string label)
        {
            float beforeSum = before.r + before.g + before.b;
            float duringSum = during.r + during.g + during.b;
            float maxShareDelta = Mathf.Max(
                Mathf.Abs(during.r / duringSum - before.r / beforeSum),
                Mathf.Max(Mathf.Abs(during.g / duringSum - before.g / beforeSum),
                    Mathf.Abs(during.b / duringSum - before.b / beforeSum)));

            Debug.Log($"[scan-body-color] {label}: before=({before.r:F3},{before.g:F3},{before.b:F3}), " +
                      $"during=({during.r:F3},{during.g:F3},{during.b:F3}), maxChannelShareDelta={maxShareDelta:F4} " +
                      $"(limit {BodyChromaticityLimit:F2})");
            Assert.LessOrEqual(maxShareDelta, BodyChromaticityLimit,
                $"{label}: 07's body colour balance shifted by {maxShareDelta:F4} > {BodyChromaticityLimit:F2}; " +
                "the scan tinted it.");
        }

        private static void AssertRoverBodyColorUnchanged(Color beforeScan, Color duringScan, string label, float maxChannelDelta)
        {
            float dr = Mathf.Abs(duringScan.r - beforeScan.r);
            float dg = Mathf.Abs(duringScan.g - beforeScan.g);
            float db = Mathf.Abs(duringScan.b - beforeScan.b);
            float maxDelta = Mathf.Max(dr, Mathf.Max(dg, db));
            float beforeCyanExcess = beforeScan.b - beforeScan.r;
            float duringCyanExcess = duringScan.b - duringScan.r;
            float cyanShift = duringCyanExcess - beforeCyanExcess;

            Debug.Log($"[scan-body-color] {label}: before=({beforeScan.r:F3},{beforeScan.g:F3},{beforeScan.b:F3}), " +
                      $"during=({duringScan.r:F3},{duringScan.g:F3},{duringScan.b:F3}), " +
                      $"maxChannelDelta={maxDelta:F4} (limit {maxChannelDelta:F3}), cyanShift={cyanShift:F4}");

            Assert.LessOrEqual(maxDelta, maxChannelDelta,
                $"{label}: 07's body average colour during scan ({duringScan.r:F3},{duringScan.g:F3},{duringScan.b:F3}) " +
                $"diverged from before scan ({beforeScan.r:F3},{beforeScan.g:F3},{beforeScan.b:F3}) by {maxDelta:F4} > {maxChannelDelta:F3}.");
            Assert.LessOrEqual(cyanShift, 0.03f,
                $"{label}: 07's body turned cyan during scan (cyanShift={cyanShift:F4} > 0.03).");
        }

        private void CaptureLensCloseup(ScanFragment target)
        {
            Transform camTr = _view.transform;
            Vector3 savedPos = camTr.position;
            Quaternion savedRot = camTr.rotation;
            float savedFov = _view.fieldOfView;
            bool wasEnabled = _cameraRig.enabled;
            _cameraRig.enabled = false;
            try
            {
                Vector3 lensPos = _rover.ScanOrigin.position;
                Vector3 toTarget = (target.Point - lensPos).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, toTarget).normalized;

                // Close-up of 07's brass cheek lens while scanning: shows the open cyan iris, emitter core,
                // cross-flare glint, and narrow beam wedge leaving the lens while 07's body stays untinted.
                Vector3 focus = lensPos + toTarget * 0.48f + right * 0.02f - Vector3.up * 0.14f;
                camTr.position = lensPos + toTarget * 1.05f - right * 0.92f + Vector3.up * 0.22f;
                camTr.rotation = Quaternion.LookRotation((focus - camTr.position).normalized, Vector3.up);
                _view.fieldOfView = 40f;

                string lensPath = Path.Combine(ScanFolder, "lens_closeup.png");
                FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, lensPath);
                File.Copy(lensPath, Path.Combine(ScanFolder, "raster_closeup.png"), overwrite: true);
            }
            finally
            {
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
                    if (tr.IsChildOf(_rover.transform) ||
                        (keepRoot != null && tr.IsChildOf(keepRoot)) ||
                        r is ParticleSystemRenderer ||
                        r is LineRenderer ||
                        string.Equals(tr.name, "ScanBeamWedge", StringComparison.Ordinal))
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
            _bodyTransform = _rover.transform.Find("Visual/Chassis/RoverModel/Body");
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
