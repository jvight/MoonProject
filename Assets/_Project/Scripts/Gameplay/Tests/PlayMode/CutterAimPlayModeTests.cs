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
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// PlayMode verification and capture suite for CORE-FEEL FIX 1 round 2 (items 5–7):
    /// boots the real <c>Main</c> scene, places a cuttable salvage piece behind 07, holds <c>E</c> via
    /// <see cref="InputTestFixture"/>, and verifies that:
    /// <list type="bullet">
    /// <item>07 holds still and turns its whole body in place (with wheel scrub and head gaze) before charging or firing;</item>
    /// <item>the chest nozzle (<see cref="RoverController.NozzleDevice"/>) opens its iris, lights its hot core, spills
    /// warm light onto 07's chest/bumper, and squats 07's suspension (<see cref="RoverFeel.Brace"/>);</item>
    /// <item>the cut progresses to completion only after <see cref="IRoverRig.CutterOrigin"/> has line of sight, and the
    /// beam segment never intersects any of 07's colliders;</item>
    /// <item>writes the 4-frame strip (<c>behind_cut_strip.png</c>: turn → charge → beam → done) and nozzle firing
    /// close-up (<c>nozzle_firing_closeup.png</c>) to <c>Logs/Captures/cutter_aim/</c>.</item>
    /// </list>
    /// </summary>
    public sealed class CutterAimPlayModeTests : InputTestFixture
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const int StripPanelWidth = 960;
        private const int StripPanelHeight = 540;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float CutTimeout = 14f;
        private const float SunlitTimeSeconds = 210f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "cut"));

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
        [Timeout(240000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator HoldingE_TargetBehind07_TurnsBodyThenChargesAndCuts_BeamNeverIntersects07Colliders()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return BootRealScene();
            SetSkyClock(SunlitTimeSeconds);
            DisableScanCandidates();

            _roverColliders = _rover.GetComponentsInChildren<Collider>(true);
            _roverMeshes = _rover.GetComponentsInChildren<MeshFilter>(true);

            SalvageField salvage = _gameplay.Salvage;
            SalvagePiece target = PickStandaloneScrapPiece(salvage);
            Assert.IsNotNull(target, "Expected a cuttable hull scrap piece in SalvageField.");

            Vector3 spot = OpenSpot();
            ClearNearbyProps(spot, 16f, target.Body);
            if (target.WreckCollider != null)
            {
                target.WreckCollider.enabled = false;
            }

            // Place the salvage piece ~2.1m behind-left of 07's initial +15 deg heading, aligning its CutPoint so the
            // cut seam sits at pieceXZ and faces 07 cleanly from both the chase camera and the chest nozzle.
            Vector3 pieceXZ = spot + new Vector3(-0.95f, 0f, -1.85f);
            float groundY = SampleGround(pieceXZ.x, pieceXZ.z);
            Vector3 toRoverFlat = new Vector3(spot.x - pieceXZ.x, 0f, spot.z - pieceXZ.z).normalized;
            Vector3 cutFwdFlat = new Vector3(target.CutPoint.forward.x, 0f, target.CutPoint.forward.z);
            if (cutFwdFlat.sqrMagnitude > 1e-4f)
            {
                float yawFix = Vector3.SignedAngle(cutFwdFlat.normalized, toRoverFlat, Vector3.up);
                target.Body.rotation = Quaternion.Euler(0f, yawFix, 0f) * target.Body.rotation;
            }
            else
            {
                target.Body.rotation = Quaternion.LookRotation(toRoverFlat, Vector3.up);
            }

            Vector3 desiredCutPos = new Vector3(pieceXZ.x, groundY + 0.32f, pieceXZ.z);
            target.Body.position += desiredCutPos - target.CutPosition;

            _cameraRig.enabled = true;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, 15f, 0f));
            _cameraRig.Snap();
            // Aim chase camera along -12 deg so both 07 and the salvage piece behind-left of 07 sit inside the frame.
            _cameraRig.Orbit.AdoptBearing(-12f);
            yield return WaitSeconds(0.55f);

            Assert.AreSame(target, salvage.Candidate, "Target behind 07 (in camera view) must be the salvage Candidate.");
            float startYaw = _rover.Rotation.eulerAngles.y;
            float initialYawDelta = Mathf.Abs(ScanAim.HorizontalYawDelta(
                _rover.CutterOrigin.forward,
                target.HotPoint - _rover.CutterOrigin.position));
            Assert.Greater(initialYawDelta, 130f, "Target must start behind 07.");
            Assert.IsFalse(_rover.HasCutterLineOfSight(target.HotPoint),
                "Target behind 07 must not have cutter line of sight before 07 turns.");

            var panels = new Texture2D[4];

            Press(_keyboard.eKey);
            yield return null;
            Assert.IsNull(salvage.Cutting, "Cut must not start on frame 1 while target is behind 07.");
            Assert.AreEqual(0f, salvage.BeamLevel, 1e-4f, "Beam must not fire while target is behind 07.");

            float maxTurnDelta = 0f;
            float maxWheelScrub = 0f;
            float maxIrisOpen = 0f;
            float maxCoreGlow = 0f;
            float maxSpillLight = 0f;
            float minBraceOffsetY = 0f;
            bool sawTurnBeforeCharge = false;
            bool sawChargingPhase = false;
            bool sawCuttingPhase = false;
            bool capturedCloseup = false;

            float deadline = Time.time + CutTimeout;
            while (target.State == SalvagePieceState.Attached)
            {
                float turnDelta = Mathf.Abs(Mathf.DeltaAngle(startYaw, _rover.Rotation.eulerAngles.y));
                float yawToTarget = ScanAim.HorizontalYawDelta(
                    _rover.CutterOrigin.forward,
                    target.HotPoint - _rover.CutterOrigin.position);
                Assert.Less(
                    Time.time,
                    deadline,
                    $"Behind-07 salvage cut timed out: turnDelta={turnDelta:F1}, maxTurn={maxTurnDelta:F1}, yawToTarget={yawToTarget:F1}, " +
                    $"candidate={(salvage.Candidate != null ? salvage.Candidate.Body.name : "null")}, " +
                    $"cutting={(salvage.Cutting != null ? salvage.Cutting.Body.name : "null")}, " +
                    $"charging={salvage.IsBeamCharging}, isCutting={salvage.IsCutting}, " +
                    $"hasLoS={_rover.HasCutterLineOfSight(target.HotPoint)}, speed={_rover.Speed:F2}, progress={target.Progress:F2}");

                maxTurnDelta = Mathf.Max(maxTurnDelta, turnDelta);
                maxWheelScrub = Mathf.Max(maxWheelScrub, Mathf.Abs(_rover.ScanTurnScrub));

                if (_rover.NozzleDevice != null)
                {
                    maxIrisOpen = Mathf.Max(maxIrisOpen, _rover.NozzleDevice.IrisOpen01);
                    maxCoreGlow = Mathf.Max(maxCoreGlow, _rover.NozzleDevice.CoreGlow01);
                    maxSpillLight = Mathf.Max(maxSpillLight, _rover.NozzleDevice.SpillLightIntensity);
                }

                if (_rover.Feel != null)
                {
                    minBraceOffsetY = Mathf.Min(minBraceOffsetY, _rover.Feel.Brace.Heave);
                }

                if (salvage.Cutting == null && turnDelta >= 55f)
                {
                    sawTurnBeforeCharge = true;
                    if (panels[0] == null)
                    {
                        panels[0] = CapturePanel("behind_cut_1_turn.png");
                    }
                }

                if (salvage.IsBeamCharging)
                {
                    sawChargingPhase = true;
                    Assert.Greater(turnDelta, 110f, "Charge-up must only begin after 07 has turned toward the target.");
                    if (panels[1] == null && salvage.BeamChargeProgress >= 0.45f)
                    {
                        panels[1] = CapturePanel("behind_cut_2_charge.png");
                    }
                }

                if (salvage.IsCutting || salvage.BeamLevel > 0.01f)
                {
                    sawCuttingPhase = true;
                    AssertBeamNeverIntersectsRoverColliders(_rover.CutterOrigin.position, salvage.HotPoint);

                    if (panels[2] == null && target.Progress >= 0.42f && salvage.BeamLevel > 0.8f)
                    {
                        panels[2] = CapturePanel("behind_cut_3_beam.png");
                    }

                    if (!capturedCloseup && target.Progress >= 0.52f && salvage.BeamLevel > 0.85f)
                    {
                        CaptureNozzleCloseup(salvage.HotPoint);
                        capturedCloseup = true;
                    }
                }

                yield return null;
            }

            // Capture panel 4 ("done") as the severed piece pops free and folds toward 07's hold.
            yield return WaitSeconds(0.18f);
            panels[3] = CapturePanel("behind_cut_4_done.png");
            Release(_keyboard.eKey);

            StitchStrip(panels, Path.Combine(CaptureFolder, "behind_cut_strip.png"));

            Assert.IsTrue(sawTurnBeforeCharge, "07 must turn its body toward the behind target before charging.");
            Assert.IsTrue(sawChargingPhase, "Nozzle must go through its charge-up phase once aimed.");
            Assert.IsTrue(sawCuttingPhase, "Cutter beam must ignite and cut the target after charging.");
            Assert.IsTrue(capturedCloseup, "Nozzle firing close-up must be captured.");
            Assert.Greater(maxTurnDelta, 125f, "07 must turn its body to face the target behind it.");
            Assert.Greater(maxWheelScrub, 0.2f, "Cutter body turn must use the same wheel-scrub feel as scan turns.");
            Assert.Greater(maxIrisOpen, 0.85f, "Nozzle iris must open during charge/cut.");
            Assert.Greater(maxCoreGlow, 0.85f, "Nozzle hot core must glow during charge/cut.");
            Assert.Greater(maxSpillLight, 0.5f, "Warm light must spill onto 07's chest and bumper while cutting.");
            Assert.Less(minBraceOffsetY, -0.005f, "07's suspension must squat/brace while charging and cutting.");
            Assert.AreEqual(1f, target.Progress, 1e-3f, "Cut must complete to 100%.");
            Assert.IsEmpty(_problems, "No unexpected errors:\n" + string.Join("\n", _problems));
        }

        private void AssertBeamNeverIntersectsRoverColliders(Vector3 beamStart, Vector3 beamEnd)
        {
            Vector3 toEnd = beamEnd - beamStart;
            float distance = toEnd.magnitude;
            Assert.Greater(distance, 0.05f, "Beam segment must have non-zero length.");
            Vector3 dir = toEnd / distance;
            var ray = new Ray(beamStart, dir);

            for (int i = 0; i < _roverColliders.Length; i++)
            {
                Collider col = _roverColliders[i];
                if (col == null || !col.enabled || col.isTrigger)
                {
                    continue;
                }

                bool hit = col.Raycast(ray, out RaycastHit hitInfo, distance)
                           && hitInfo.distance > 0.02f
                           && hitInfo.distance < distance - 0.02f;
                Assert.IsFalse(hit,
                    $"Cutter beam segment intersected 07's collider '{col.name}' at distance {hitInfo.distance:F3}m / {distance:F3}m.");
            }

            Assert.IsTrue(
                ScanAim.HasCutterLineOfSight(
                    _rover.CutterOrigin,
                    beamEnd,
                    ScanAim.DefaultCutterFiringConeDeg,
                    _roverColliders,
                    _roverMeshes),
                "CutterOrigin must maintain clear line of sight to the active cut point while cutting.");
        }

        private void CaptureNozzleCloseup(Vector3 hotPoint)
        {
            Transform camTr = _view.transform;
            Vector3 savedPos = camTr.position;
            Quaternion savedRot = camTr.rotation;
            float savedFov = _view.fieldOfView;
            bool wasEnabled = _cameraRig.enabled;
            _cameraRig.enabled = false;
            try
            {
                Transform origin = _rover.CutterOrigin;
                Vector3 nozzlePos = origin.position;
                Vector3 beamDir = (hotPoint - nozzlePos).sqrMagnitude > 1e-4f
                    ? (hotPoint - nozzlePos).normalized
                    : origin.forward;
                Vector3 side = Vector3.Cross(Vector3.up, beamDir).normalized;
                Vector3 focus = nozzlePos + beamDir * 0.08f + Vector3.up * 0.02f;
                camTr.position = nozzlePos + beamDir * 0.62f + side * 0.52f + Vector3.up * 0.18f;
                camTr.rotation = Quaternion.LookRotation((focus - camTr.position).normalized, Vector3.up);
                _view.fieldOfView = 30f;
                FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight,
                    Path.Combine(CaptureFolder, "nozzle_firing_closeup.png"));
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
            string path = Path.Combine(CaptureFolder, fileName);
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, path);

            string tempStripPanel = Path.Combine(CaptureFolder, "_tmp_" + fileName);
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

        private static SalvagePiece PickStandaloneScrapPiece(SalvageField salvage)
        {
            IReadOnlyList<SalvageSite> derelicts = salvage.Derelicts;
            for (int d = 0; d < derelicts.Count; d++)
            {
                SalvageSite site = derelicts[d];
                if (!string.Equals(site.Id, SalvageField.ScrapFieldId, StringComparison.Ordinal))
                {
                    continue;
                }

                for (int p = 0; p < site.Pieces.Count; p++)
                {
                    SalvagePiece piece = site.Pieces[p];
                    if (piece.IsCuttable && !piece.IsDrag)
                    {
                        return piece;
                    }
                }
            }

            for (int i = 0; i < salvage.Pieces.Count; i++)
            {
                SalvagePiece piece = salvage.Pieces[i];
                if (piece.IsCuttable && !piece.IsDrag)
                {
                    return piece;
                }
            }

            return null;
        }

        private void DisableScanCandidates()
        {
            foreach (ScanFragment frag in _gameplay.Scans.Fragments)
            {
                frag.Scanned = true;
            }
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
                        tr.IsChildOf(_gameplay.Salvage.transform) && tr.name.Contains("Feel") ||
                        (keepRoot != null && tr.IsChildOf(keepRoot)))
                    {
                        continue;
                    }

                    // Keep FeelVfx sprites/beams/seams active.
                    if (tr.parent != null && tr.parent.name == "FeelVfx")
                    {
                        continue;
                    }

                    Bounds b = r.bounds;
                    if (b.size.x > 18f || b.size.z > 18f)
                    {
                        continue;
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
            throw new NotSupportedException("CutterAimPlayModeTests requires EditorSceneManager.");
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
