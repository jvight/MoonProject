using System;
using System.Collections;
using System.Globalization;
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

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07 gazing at the stars in the real Main scene (M3-15 §5): at an open spot in the basin and beside the lander,
    /// the player tips the view up with the gamepad until the camera sits under 07 looking at the sky, then rests.
    /// Frames before, while 07 lifts its head, and once the stargazing beat began go to Logs/rover-captures/*.png, with
    /// stargaze.md recording the orbit elevation, the camera's height above the dust, 07's share of the gaze and the
    /// beat. Drive input then ends the beat. The dock's evening (M3-16 P6) is caught the same way: 07 parked on its
    /// dock, the wide shot of home's lamps, its ease back, and drive input ending it early (evening-*.png).
    /// Slow and needs a GPU: run on demand with --category RoverStargazeCaptures.
    /// </summary>
    [Explicit("Slow real-game capture session; run on demand with --category RoverStargazeCaptures.")]
    [Category("RoverStargazeCaptures")]
    public sealed class RoverStargazeCaptures : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 2f;

        /// <summary>The open basin: this far from the base, on the far side from Earth.</summary>
        private const float BasinDistance = 160f;

        /// <summary>Beside the lander: this far back from its charging dock, turned this far off it.</summary>
        private const float LanderBack = 6f;
        private const float LanderSide = 3f;

        /// <summary>Orbit elevation (deg) the view is tipped to: the tuning's full stargaze elevation.</summary>
        private const float LookUpElevation = -20f;

        private const float LookStick = 0.8f;

        /// <summary>How long the stick is tried one way to learn which way tips the view up.</summary>
        private const float StickProbeSeconds = 0.3f;

        /// <summary>How far (deg) the view must move within the probe time to show which way the stick tips it.</summary>
        private const float ProbeTurn = 1f;
        private const float ProbeSeconds = 1f;
        private const float LookTimeout = 6f;
        private const float BeatTimeout = 8f;

        /// <summary>Once the beat is on, 07 lifts its head this far into the gaze within the timeout.</summary>
        private const float JoinedGaze = 0.9f;
        private const float GazeTimeout = 6f;

        private const float LiftFrameSeconds = 1.5f;
        private const float DriveStick = 0.6f;
        private const float EndTimeout = 1f;
        private const float MinCameraClearance = 0.2f;
        private const string DockAnchorName = "DockAnchor";

        /// <summary>The longest the wake may still hold the camera before the evening is tried.</summary>
        private const float WakeEaseTimeout = 8f;

        /// <summary>The dock's evening: seconds to the settled frame, the frame mid-ease back, and timeouts.</summary>
        private const float EveningSettleSeconds = 5f;
        private const float EveningSettled = 0.9f;
        private const float EveningBackFrameSeconds = 1f;
        private const float EveningEndTimeout = 20f;
        private const float EveningEarlySeconds = 2f;

        /// <summary>The scan capture: the fragment's distance ahead, the wait before each frame, and the flare's.
        /// </summary>
        private const float ScanReach = 2.5f;
        private const float ScanFrameSeconds = 1.2f;
        private const float ScanFlareFrameSeconds = 0.05f;

        /// <summary>The elder capture: 07's distance from the micro.06 site (inside the notice radius), how far off
        /// its nose 06 sits, and the frames' waits.</summary>
        private const float ElderApproach = 9f;
        private const float ElderOffAxis = 55f;
        private const float ElderGlanceFrame = 0.9f;
        private const float ElderLetGoFrame = 2.5f;

        /// <summary>The cut point ahead of 07, the Cells capture's frame waits, and the share of stage 2 at its frame.
        /// </summary>
        private const float CutReach = 1.4f;
        private const float CutRise = 0.6f;
        private const float MidStageShare = 0.5f;
        private const float LockFrameSeconds = 0.1f;
        private const float WarmFrameSeconds = 0.8f;

        /// <summary>The brace capture: a hull plate's stages, the tier-0 stage time, the brace and the held breath.</summary>
        private const int HullStages = 3;
        private const float StageSeconds = 0.45f;
        private const float BraceSeconds = 0.5f;
        private const float HeldBreathSeconds = 0.12f;

        /// <summary>The hatch capture: five of eight slots used, a bundle's flight, and the wait before each frame.</summary>
        private const int HatchCrates = 5;
        private const int HoldSlots = 8;
        private const float BundleFlight = 0.7f;
        private const float HatchFrameSeconds = 0.8f;

        /// <summary>How far (m) along Earth's direction a point is projected to find Earth on screen.</summary>
        private const float EarthProbeDistance = 1000f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-captures"));

        private readonly StringBuilder _report = new StringBuilder();
        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private ITerrainQuery _terrain;
        private Gamepad _gamepad;
        private bool _awake;
        private bool _stargazing;
        private IDisposable[] _subscriptions;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_subscriptions != null)
            {
                foreach (IDisposable subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }

            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator LookingUp_07GazesAtTheStars_InTheBasinAndBesideTheLander()
        {
            yield return Boot();
            _report.AppendLine("# Stargazing (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");

            IWorldLayout layout = _context.Get<IWorldLayout>();
            float earth = WideShotComposer.Bearing(layout.EarthDirection);
            Vector3 basin = layout.BasePosition - WideShotComposer.Direction(earth) * BasinDistance;
            Assert.IsTrue(_terrain.IsDrivable(basin.x, basin.z), "The basin spot is open ground.");
            yield return Stargaze("stargaze-basin", basin, earth);

            Transform dock = FindNode(DockAnchorName);
            Assert.IsNotNull(dock, "The lander carries the charging dock's DockAnchor.");
            Vector3 lander = dock.position - dock.forward * LanderBack + dock.right * LanderSide;
            yield return Stargaze("stargaze-lander", lander, dock.eulerAngles.y);

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "stargaze.md"), _report.ToString());
            Debug.Log("[rover-stargaze] " + _report);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator DockEvening_EasesOutToHomesLamps_HoldsThenEasesBack()
        {
            yield return Boot();
            _report.AppendLine("# The dock's evening (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first (the evening comes after a trip).");
                yield return null;
            }

            Transform dock = FindNode(DockAnchorName);
            Assert.IsNotNull(dock, "The lander carries the charging dock's DockAnchor.");
            _context.Get<IRoverPlacement>().PlaceAt(dock.position, dock.rotation);
            yield return Wait(SettleSeconds);
            Capture("evening-0-parked");
            HeldShot held = _cameraRig.Held;
            _context.Events.Publish(new DockEveningOpened(dock.position));
            Assert.IsTrue(held.IsOpen, "The evening shot opens.");
            yield return Wait(EveningSettleSeconds);
            Assert.Greater(held.Weight, EveningSettled, "Settled on the wide shot of home.");
            Capture("evening-1-wide");
            WideShotFrame frame = held.Frame;
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "\nevening-1: frame yaw {0:0} deg, elevation {1:0.0} deg, distance {2:0.0} m; camera {3:0.0} m off",
                frame.Yaw, frame.Elevation, frame.Distance,
                Vector3.Distance(_view.transform.position, _rover.Position)));
            float timeout = Time.time + EveningEndTimeout;
            while (held.IsOpen)
            {
                Assert.Less(Time.time, timeout, "The hold runs out and the view eases back.");
                yield return null;
            }

            yield return Wait(EveningBackFrameSeconds);
            Assert.That(held.Weight, Is.InRange(0.05f, 0.95f), "Easing back, never a cut.");
            Capture("evening-2-easing-back");
            while (held.Weight > 0f)
            {
                Assert.Less(Time.time, timeout + EveningEndTimeout, "Back on the chase frame.");
                yield return null;
            }

            _context.Events.Publish(new DockEveningOpened(dock.position));
            yield return Wait(EveningEarlySeconds);
            Set(_gamepad.leftStick, new Vector2(0f, DriveStick));
            float early = Time.time + EndTimeout;
            while (held.IsOpen)
            {
                Assert.Less(Time.time, early, "Drive input ends the evening early.");
                yield return null;
            }

            Set(_gamepad.leftStick, Vector2.zero);
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "evening.md"), _report.ToString());
        }

        /// <summary>
        /// The Cells charge-up (0.5.5 §5.4) from the chase camera: a warm three-stage charge (no brace, no push-in, so
        /// the frame holds) with frames mid stage 2, at the full lock, and in the warm trace after the cut.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ChargeCells_AreCapturedMidChargeLockedAndWarm_FromTheChaseCamera()
        {
            yield return Boot();
            _report.AppendLine("# The Cells charge-up (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
            Vector3 cutPoint = _rover.Position + _rover.Rotation * Vector3.forward * CutReach + Vector3.up * CutRise;
            _context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, cutPoint, Vector3.up, 0f,
                HullStages * StageSeconds));
            _context.Events.Publish(new CutChargeStaged(0, HullStages, true, StageSeconds));
            yield return Wait(StageSeconds);
            _context.Events.Publish(new CutChargeStaged(1, HullStages, true, StageSeconds));
            yield return Wait(StageSeconds * MidStageShare);
            Capture("cells-1-stage2");
            yield return Wait(StageSeconds * (1f - MidStageShare));
            _context.Events.Publish(new CutChargeStaged(2, HullStages, true, StageSeconds));
            yield return Wait(StageSeconds);
            _context.Events.Publish(new CutChargeStaged(3, HullStages, true, StageSeconds));
            yield return Wait(LockFrameSeconds);
            Capture("cells-2-lock");
            _context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Act, cutPoint, Vector3.up, 0f, 0f));
            _context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Impact, cutPoint, Vector3.up, 1f, 0f));
            yield return Wait(WarmFrameSeconds);
            Capture("cells-3-warm");
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "cells.md"), _report.ToString());
        }

        /// <summary>
        /// 07 scanning a fragment (0.5.5 §1.2) from the chase camera: the lid fully open and the fan sweeping toward a
        /// point ahead mid-scan, then the lens's flare as the scan completes.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scan_IsCapturedSweepingAndLearning_FromTheChaseCamera()
        {
            yield return Boot();
            _report.AppendLine("# The scan (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
            Vector3 target = _rover.Position + _rover.Rotation * Vector3.forward * ScanReach + Vector3.up * CutRise;
            _context.Events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Anticipate, target, Vector3.up, 0f, 0f));
            yield return Wait(ScanFrameSeconds);
            _context.Events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Act, target, Vector3.up, 0.5f, 0f));
            Capture("scan-1-sweep");
            yield return Wait(ScanFrameSeconds);
            _context.Events.Publish(new FeelBeat(FeelKind.Scan, FeelPhase.Reward, target, Vector3.up, 1f, 0f));
            yield return Wait(ScanFlareFrameSeconds);
            Capture("scan-2-learned");
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "scan.md"), _report.ToString());
        }

        /// <summary>
        /// 07 meets its elder 06 (0.5.5 §1.7) in the real scene, from the chase camera: placed on the home side of the
        /// micro.06 site with 06 ahead and off to one side, it notices 06 and holds its look; a frame mid-glance and
        /// one as it lets go, with where it looked in the report.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Elder_IsCapturedMidGlance_FromTheChaseCamera()
        {
            yield return Boot();
            _report.AppendLine("# 07 meets its elder 06 (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first.");
                yield return null;
            }

            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.Micro06, out WorldAnchor site),
                "The world has the micro.06 site.");
            Vector3 approach = site.Position + site.Forward * ElderApproach;
            approach.y = _terrain.SampleHeight(approach.x, approach.z);
            Vector3 toSite = site.Position - approach;
            float yaw = Mathf.Atan2(toSite.x, toSite.z) * Mathf.Rad2Deg + ElderOffAxis;
            _context.Get<IRoverPlacement>().PlaceAt(approach, Quaternion.Euler(0f, yaw, 0f));
            yield return Wait(ElderGlanceFrame);
            Capture("elder-1-glance");
            bool looking = _rover.Gaze.TryGetTop(out Vector3 gaze);
            _report.AppendLine();
            _report.AppendLine($"Site {site.Position}, 07 at {approach}, gaze {(looking ? gaze.ToString() : "none")}.");
            yield return Wait(ElderLetGoFrame);
            Capture("elder-2-after");
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "elder.md"), _report.ToString());
            Assert.IsTrue(looking, "07 looks at 06.");
        }

        /// <summary>
        /// The Tab "07" page's live portrait (0.5.5 §3.5) in the real scene: the texture written out as a PNG with the
        /// socket positions art has marked, to judge the pose and how 07 reads lit only by the scene and its lamps.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Portrait_IsCapturedFromItsOwnCamera()
        {
            yield return Boot();
            var portrait = _context.Get<IRoverPortrait>();
            portrait.SetActive(true);
            yield return Wait(SettleSeconds);
            RenderTexture texture = portrait.Texture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            portrait.SetActive(false);
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllBytes(Path.Combine(CaptureFolder, "portrait.png"), image.EncodeToPNG());
            var report = new StringBuilder("# The Tab 07 portrait" + Environment.NewLine);
            foreach (RoverSocket socket in Enum.GetValues(typeof(RoverSocket)))
            {
                report.AppendLine(portrait.TryGetSocketViewport(socket, out Vector2 uv)
                    ? $"- {socket}: {uv.x.ToString("F3", CultureInfo.InvariantCulture)}, "
                    + uv.y.ToString("F3", CultureInfo.InvariantCulture)
                    : $"- {socket}: no node yet");
            }

            int shown = 0;
            foreach (Color32 pixel in image.GetPixels32())
            {
                shown += pixel.a > 0 ? 1 : 0;
            }

            report.AppendLine($"- pixels with 07 in them: {shown} of {texture.width * texture.height}");
            File.WriteAllText(Path.Combine(CaptureFolder, "portrait.md"), report.ToString());
            Object.Destroy(image);
            Assert.Greater(shown, 0, "07 shows in its portrait.");
        }

        /// <summary>
        /// The charge-up's weight (0.5.5 §5.3, §5.6) from the chase camera: a fresh charge on a hull plate (3 stages)
        /// braces 07 and pushes the camera in; frames at rest, braced, and at the full lock, with the camera's
        /// distance and height from 07 in the report.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ChargeBrace_IsCapturedBracedAndPushedIn_ForAFreshHullCharge()
        {
            yield return Boot();
            _report.AppendLine("# The charge-up's brace and push-in (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
            Capture("brace-0-rest");
            Vector3 rest = _view.transform.position - _rover.Position;
            Vector3 cutPoint = _rover.Position + _rover.Rotation * Vector3.forward * CutReach + Vector3.up * CutRise;
            _context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Anticipate, cutPoint, Vector3.up, 0f,
                BraceSeconds + HullStages * StageSeconds));
            _context.Events.Publish(new CutChargeStaged(0, HullStages, false, StageSeconds));
            yield return Wait(BraceSeconds);
            Capture("brace-1-braced");
            for (int stage = 1; stage <= HullStages; stage++)
            {
                yield return Wait(StageSeconds);
                _context.Events.Publish(new CutChargeStaged(stage, HullStages, false, StageSeconds));
            }

            yield return Wait(HeldBreathSeconds);
            Capture("brace-2-lock");
            Vector3 locked = _view.transform.position - _rover.Position;
            _report.AppendLine();
            _report.AppendLine($"Camera from 07: rest {rest.magnitude:F2} m (up {rest.y:F2} m), full lock "
                + $"{locked.magnitude:F2} m (up {locked.y:F2} m).");
            _context.Events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Cancel, cutPoint, Vector3.up, 0f, 0f));
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "brace.md"), _report.ToString());
            Assert.Less(locked.magnitude, rest.magnitude, "A fresh hull charge pushes the camera in.");
        }

        /// <summary>
        /// 07's cargo hatch (M3-25 T1) from the chase camera: the hold five slots full (five crates in the window),
        /// then a bundle flying in opens the hatch just before it lands; frames of the shut window and the open hatch.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator CargoHatch_IsCapturedOpen_WithCratesInTheWindow()
        {
            yield return Boot();
            _report.AppendLine("# The cargo hatch (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Orbit elevation (deg) | Camera above dust (m) | 07's gaze | Beat |");
            _report.AppendLine("|---|---:|---:|---:|---|");
            Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
            yield return Wait(StickProbeSeconds);
            Set(_gamepad.rightStick, Vector2.zero);
            float opening = Time.time + WakeEaseTimeout;
            while (_cameraRig.Opening.IsActive)
            {
                Assert.Less(Time.time, opening, "The wake hands over first.");
                yield return null;
            }

            yield return Wait(SettleSeconds);
            _context.Events.Publish(new CargoChanged(HatchCrates, HoldSlots));
            yield return Wait(HatchFrameSeconds);
            Capture("hatch-1-crates");
            Vector3 socket = _rover.Position + Vector3.up;
            _context.Events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Anticipate, socket, Vector3.up, 1f,
                BundleFlight));
            yield return Wait(BundleFlight + HatchFrameSeconds);
            Capture("hatch-2-open");
            _context.Events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, socket, Vector3.up, 1f, 0f));
            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "hatch.md"), _report.ToString());
        }

        private IEnumerator Stargaze(string name, Vector3 ground, float yaw)
        {
            _context.Get<IRoverPlacement>().PlaceAt(ground, Quaternion.Euler(0f, yaw, 0f));
            yield return Wait(SettleSeconds);
            Capture(name + "-0-level");

            float target = LookUpElevation;
            float before = _cameraRig.Orbit.Elevation;
            Set(_gamepad.rightStick, new Vector2(0f, LookStick));
            float arrive = Time.time + LookTimeout;
            while (_context.Input.LookRate.sqrMagnitude <= 0f)
            {
                Assert.Less(Time.time, arrive, "The stick reaches 07's look input. " + InputState());
                yield return null;
            }

            float probe = Time.time + ProbeSeconds;
            while (Mathf.Abs(_cameraRig.Orbit.Elevation - before) < ProbeTurn && Time.time < probe)
            {
                yield return null;
            }

            // Unmoved, the view already sat at its limit that way: the stick tips it up the other way.
            float sign = _cameraRig.Orbit.Elevation < before - ProbeTurn ? 1f : -1f;
            Set(_gamepad.rightStick, new Vector2(0f, LookStick * sign));
            float timeout = Time.time + LookTimeout;
            while (_cameraRig.Orbit.Elevation > target)
            {
                Assert.Less(Time.time, timeout, "The view tips up until the camera looks up from under 07.");
                yield return null;
            }

            Set(_gamepad.rightStick, Vector2.zero);
            yield return Wait(LiftFrameSeconds);
            Capture(name + "-1-lifting");

            timeout = Time.time + BeatTimeout;
            while (!_stargazing)
            {
                Assert.Less(Time.time, timeout, "The stargazing beat begins after a rest looking up.");
                yield return null;
            }

            timeout = Time.time + GazeTimeout;
            while (_cameraRig.SkyLift < JoinedGaze)
            {
                Assert.Less(Time.time, timeout, $"07 joins the gaze (still {_cameraRig.SkyLift:0.00}).");
                yield return null;
            }
            Capture(name + "-2-beat");

            Set(_gamepad.leftStick, new Vector2(0f, DriveStick));
            timeout = Time.time + EndTimeout;
            while (_stargazing)
            {
                Assert.Less(Time.time, timeout, "Drive input ends the beat.");
                yield return null;
            }

            Set(_gamepad.leftStick, Vector2.zero);
            yield return Wait(SettleSeconds);
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            _gamepad = InputSystem.AddDevice<Gamepad>();
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The stargaze session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_rover, "the scene has 07");
            Assert.IsNotNull(_cameraRig, "the scene has the camera rig");
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _terrain = _context.Get<ITerrainQuery>();
            _subscriptions = new[]
            {
                _context.Events.Subscribe<RoverAwoke>(_ => _awake = true),
                _context.Events.Subscribe<StargazingChanged>(changed => _stargazing = changed.IsStargazing),
            };
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return Wait(SettleSeconds);
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
            Vector3 camera = _view.transform.position;
            float clearance = camera.y - _terrain.SampleHeight(camera.x, camera.z);
            Assert.Greater(clearance, MinCameraClearance, $"{name}: the camera stays out of the dust.");
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| {0} | {1:0.0} | {2:0.00} | {3:0.00} | {4} |", name, _cameraRig.Orbit.Elevation, clearance,
                _cameraRig.SkyLift, _stargazing ? "on" : "off"));
        }

        /// <summary>In front of the camera, inside the frame across, and between the heights given.</summary>
        private static bool InFrame(Vector3 viewport, float minY, float maxY)
        {
            return viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y >= minY && viewport.y < maxY;
        }

        private static Transform FindNode(string name)
        {
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        /// <summary>The input system as the look step sees it, for a failure message.</summary>
        private string InputState()
        {
            var state = new StringBuilder();
            state.Append("stick ").Append(_gamepad.rightStick.ReadValue()).Append(" gamepad added ")
                .Append(_gamepad.added).Append(" enabled ").Append(_gamepad.enabled).Append(" devices");
            foreach (InputDevice device in InputSystem.devices)
            {
                state.Append(' ').Append(device.name).Append(device.enabled ? "+" : "-");
            }

            state.Append("; reader enabled ").Append(_context.Input.Enabled).Append(", enabled LookRate controls:");
            foreach (InputAction action in InputSystem.ListEnabledActions())
            {
                if (action.name != "LookRate")
                {
                    continue;
                }

                foreach (InputControl control in action.controls)
                {
                    state.Append(' ').Append(control.path);
                }
            }

            state.Append("; update ").Append(InputSystem.settings.updateMode).Append(", timeScale ")
                .Append(Time.timeScale).Append(", focused ").Append(Application.isFocused);
            return state.ToString();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}
