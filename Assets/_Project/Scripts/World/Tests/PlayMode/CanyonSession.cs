using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
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

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Whispering Canyon driven by the real rover in the built Main scene (on its own save slot). Without the
    /// Hover-Jump a run at the chasm ends softly in the trough, the far face is never mounted and 07 drives back
    /// out. With it, a tap still drops into the trough while a 3/4 charge at cruising speed and a full charge at top
    /// speed both land on the far side. Then the canyon is driven to its terminus and left through the one-way
    /// exit, whose step is never climbed from the basin. Captures the base view, the lip, mid-leap, inside and the
    /// terminus (Logs/world-captures) and writes the leap numbers to canyon-session.md; any error in the log fails
    /// it. Slow and needs a GPU: run on demand with --category CanyonSession.
    /// </summary>
    [Explicit("Slow rover session in the real Main scene; run on demand with --category CanyonSession.")]
    [Category("CanyonSession")]
    public sealed class CanyonSession
    {
        private const float CruiseThrottle = 0.75f;
        private const float ThreeQuarterCharge = 0.75f;
        private const float FullChargeMargin = 1.2f;

        /// <summary>Each run at the chasm starts this far (m) out in the basin, in line with the canyon.</summary>
        private const float RunUp = 50f;

        /// <summary>The Jump button is let go this far (m) before the lip's crest.</summary>
        private const float ReleaseLead = 0.5f;

        /// <summary>Pushes at a gate face start this far (m) in front of its slab, at these angles (degrees).</summary>
        private const float PushRunUp = 12f;
        private const float PushTime = 6f;
        private static readonly float[] PushAngles = { -30f, 0f, 30f };

        /// <summary>A leap has made it across if it touches down no lower than this (m) below the apron.</summary>
        private const float LandingMargin = 1f;

        private const float StopSpeed = 0.4f;
        private const float WaypointSpacing = 10f;
        private const float WaypointArrive = 4f;
        private const float WaypointTimeout = 20f;

        // Getting this much closer to the waypoint counts as progress (restarts the stuck clock).
        private const float WaypointProgress = 0.5f;
        private const float RunTimeout = 30f;
        private const float InsideThrottle = 0.7f;
        private const float EyeHeight = 3f;

        // Review cameras look this far above the ledge's floor: the height of something standing on it.
        private const float LookHeight = 2f;
        private const float ExitViewDistance = 24f;
        private const float ExitViewHeight = 5f;
        private const float WideFov = 60f;
        private const float ZoomFov = 12f;
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "world-captures"));

        private readonly List<string> _problems = new List<string>();
        private readonly List<string> _report = new List<string>();
        private readonly List<Vector3> _landings = new List<Vector3>();
        private readonly List<float> _leaps = new List<float>();

        private GameContext _context;
        private RoverController _rover;
        private CanyonPilot _pilot;
        private ITerrainQuery _terrain;
        private Canyon _canyon;
        private CanyonSettings _settings;
        private IDisposable _jumped;
        private IDisposable _landed;
        private IDisposable _awoke;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
            _jumped?.Dispose();
            _landed?.Dispose();
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator RuinWall_StopsRover_InRealScene()
        {
            yield return Boot();
            Scene scene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            SkyCycleSystem skyCycle = null;
            Transform habitat = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (skyCycle == null)
                {
                    skyCycle = root.GetComponentInChildren<SkyCycleSystem>(true);
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (habitat == null && t.name == "HabitatModules")
                    {
                        habitat = t;
                    }
                }
            }

            Assert.IsNotNull(skyCycle, "Main scene must have SkyCycleSystem.");
            Assert.IsNotNull(habitat, "Main scene must have HabitatModules.");
            skyCycle.SetClockTime(210f, false);
            yield return null;

            string afterDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "after"));
            Directory.CreateDirectory(afterDir);
            Camera view = _context.Get<IViewCamera>().Camera;

            // Drive straight into the unobstructed north hull wall of HabitatModules and verify 07 stops against the compound BoxColliders.
            _pilot.Release();
            Vector3 habPos = habitat.position;
            Vector3 toHab = Vector3.back;
            Vector3 startHab = new Vector3(habPos.x, 0f, habPos.z + 6.5f);
            startHab.y = _terrain.SampleHeight(startHab.x, startHab.z);
            _rover.PlaceAt(startHab, Quaternion.LookRotation(toHab, Vector3.up));
            Physics.SyncTransforms();
            yield return Settle();
            for (int i = 0; i < 15; i++)
            {
                yield return null;
            }

            FrameCapture.SavePng(view, CaptureWidth, CaptureHeight, Path.Combine(afterDir, "ruin_wall_approach.png"));

            _pilot.Throttle = 0.80f;
            _pilot.Target = new Vector3(habPos.x, habPos.y, habPos.z - 6.0f);
            yield return new WaitForSeconds(2.2f);
            FrameCapture.SavePng(view, CaptureWidth, CaptureHeight, Path.Combine(afterDir, "ruin_wall_impact.png"));
            Assert.Greater(_rover.Position.z, habPos.z + 1.5f, "07 must be blocked outside the solid HabitatModules ruin wall.");
            Assert.Less(_rover.Speed, 0.8f, "Solid ruin wall must arrest 07.");
            _pilot.Release();

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The canyon session loads the scene through the editor.");
#endif
            Scene scene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            GameBootstrap bootstrap = null;
            WorldSystem world = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                world = world != null ? world : root.GetComponentInChildren<WorldSystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(world, "the scene has the world");
            _context = bootstrap.Context;
            _terrain = _context.Get<ITerrainQuery>();
            _canyon = world.Surface.Canyon;
            _settings = _canyon.Settings;
            _rover = (RoverController)_context.Get<IRoverRig>();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            _jumped = _context.Events.Subscribe<RoverJumped>(OnJumped);
            _landed = _context.Events.Subscribe<RoverLanded>(e => _landings.Add(e.Position));
            _pilot = new CanyonPilot(_rover);
            _rover.SetDriveSource(_pilot);
            var anchors = _context.Get<IWorldAnchors>();
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                Note($"Anchor {anchor.Id}", string.Format(CultureInfo.InvariantCulture,
                    "position ({0:F2}, {1:F2}, {2:F2}), forward ({3:F3}, {4:F3}, {5:F3}), radius {6:F2} m",
                    anchor.Position.x, anchor.Position.y, anchor.Position.z, anchor.Forward.x, anchor.Forward.y,
                    anchor.Forward.z, anchor.Radius));
            }

            yield return Until(() => _awake, 20f, "07 wakes up");
            yield return new WaitForSeconds(2f);
        }

        private void AssertInTheTrough(string label)
        {
            float arc = Arc();
            Assert.Less(arc, _canyon.FarFaceArc - Canyon.SlabHalfDepth, $"{label}: 07 should not cross the chasm");
            Assert.Greater(arc, _canyon.LipArc, $"{label}: 07 should be down in the trough");
            Assert.Less(_rover.Position.y, _canyon.LipHeight, $"{label}: 07 should be down in the trough");
        }

        private void AssertAcross(string label)
        {
            Vector3 landing = _landings[_landings.Count - 1];
            float arc = ArcOf(landing);
            Assert.Greater(arc, _canyon.FarFaceArc - Canyon.SlabHalfDepth, $"{label}: touchdown short of the far face");
            Assert.Less(arc, _canyon.ApronEndArc, $"{label}: touchdown past the landing apron");
            Assert.Greater(landing.y, _canyon.ApronHeight - LandingMargin, $"{label}: touchdown below the apron");
        }

        /// <summary>
        /// Drives at a gate face at full throttle from in front of it, straight on and at an angle either way:
        /// 07 must never get up it.
        /// </summary>
        private IEnumerator PushAtGate(CanyonPath path, float faceArc, float top, string what)
        {
            Vector2 start = path.PointAt(faceArc - Canyon.SlabHalfDepth - PushRunUp);
            Vector2 along = path.TangentAt(faceArc);
            float highest = float.MinValue;
            foreach (float angle in PushAngles)
            {
                yield return Teleport(start);
                Vector2 heading = Rotate(along, angle);
                _pilot.Throttle = 1f;
                _pilot.Target = Ground(start + heading * (PushRunUp * 4f));
                float until = Time.time + PushTime;
                while (Time.time < until)
                {
                    // On top of the gate means past the slab's front and at least as high as its top (the
                    // rover's position is its body, which rides about a wheel radius above the ground).
                    Vector3 p = _rover.Position;
                    highest = Mathf.Max(highest, p.y);
                    if (ArcOn(path, p) > faceArc - Canyon.SlabHalfDepth && p.y >= top)
                    {
                        path.TryProject(p.x, p.z, out float arc, out float lateral);
                        Review(p + Vector3.up * 12f - new Vector3(along.x, 0f, along.y) * 10f, p, WideFov,
                            $"mounted-{angle:F0}");
                        Assert.Fail($"07 got up {what} driving at it {angle:F0} degrees off: at {p} " +
                                    $"(arc {arc:F1}, {lateral:F1} m across; face at arc {faceArc:F1})");
                    }

                    yield return null;
                }

                _pilot.Release();
            }

            Note($"Push at {what}", string.Format(CultureInfo.InvariantCulture,
                "full throttle at {0} degrees: highest {1:F2} m, face top {2:F2} m", string.Join("/", PushAngles),
                highest, top));
            yield return Settle();
        }

        private IEnumerator DriveInside()
        {
            CanyonPath main = _canyon.MainPath;
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonLedge, out WorldAnchor ledge));
            yield return Follow(main, Arc(), _canyon.LedgeArc, "to the ledge");
            Review(ledge.Position - ledge.Forward * 7f + Vector3.up * 2f, ledge.Position + Vector3.up * LookHeight,
                WideFov, "06-ledge");
            yield return Follow(main, _canyon.LedgeArc, main.EndArc - _settings.TerminusRadius, "up the canyon");
            Capture("07-inside");
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonTerminus,
                out WorldAnchor terminus));
            yield return DriveTo(terminus.Position, InsideThrottle, "the terminus");
            yield return Settle();
            Capture("08-terminus");
            Review(terminus.Position - terminus.Forward * 4f + Vector3.up * 3f,
                terminus.Position - terminus.Forward * 30f, WideFov, "08-terminus-looking-back");
            Note("Inside", "drove from the landing past the ledge to the terminus");
        }

        private IEnumerator LeaveByTheExit()
        {
            CanyonPath main = _canyon.MainPath;
            CanyonPath exit = _canyon.ExitPath;
            float join = _canyon.FarFaceArc + _settings.ExitBranch;
            yield return Follow(main, main.EndArc - _settings.TerminusRadius, join, "back down the canyon");
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.CanyonExit, out WorldAnchor top));
            yield return Follow(exit, exit.EndArc, ArcOn(exit, top.Position), "along the exit shelf");
            yield return Settle();
            Capture("09-exit-top");
            float footArc = _canyon.ExitStepArc - Canyon.SlabHalfDepth - PushRunUp;
            yield return Follow(exit, ArcOn(exit, top.Position), footArc, "down the step");
            yield return Settle();
            Vector3 foot = Ground(_canyon.ExitFoot);
            Assert.Less(Mathf.Abs(_rover.Position.y - foot.y), 1f, "07 is back down on the basin floor");
            Review(foot + (foot - top.Position).normalized * ExitViewDistance + Vector3.up * ExitViewHeight,
                top.Position, WideFov, "10-exit-step-from-the-basin");
            Note("Exit", string.Format(CultureInfo.InvariantCulture,
                "drove off the step ({0:F2} m) and on into the basin", top.Position.y - foot.y));
        }

        private IEnumerator Follow(CanyonPath path, float from, float to, string what)
        {
            float direction = Mathf.Sign(to - from);
            float arc = from;
            while (direction * (to - arc) > 0f)
            {
                arc = direction > 0f ? Mathf.Min(arc + WaypointSpacing, to) : Mathf.Max(arc - WaypointSpacing, to);
                _pilot.Throttle = InsideThrottle;
                _pilot.Target = Ground(path.PointAt(arc));
                yield return UntilArrived($"07 drives {what} (waypoint at arc {arc:F0})");
            }

            _pilot.Release();
        }

        private IEnumerator DriveTo(Vector3 target, float throttle, string what)
        {
            _pilot.Throttle = throttle;
            _pilot.Target = target;
            yield return Until(() => _pilot.Distance < WaypointArrive, RunTimeout * 2f, $"07 drives to {what}");
            _pilot.Release();
        }

        /// <summary>
        /// Sets 07 down at <paramref name="xz"/> keeping its heading, through the rover's own placement so a rest on
        /// the dock (07 wakes on it) ends too, then waits for it to settle.
        /// </summary>
        private IEnumerator Teleport(Vector2 xz)
        {
            _pilot.Release();
            var position = new Vector3(xz.x, _terrain.SampleHeight(xz.x, xz.y), xz.y);
            _rover.PlaceAt(position, Quaternion.Euler(0f, _rover.Heading, 0f));
            Physics.SyncTransforms();
            yield return Settle();
        }

        private IEnumerator Settle()
        {
            yield return Until(() => _rover.IsGrounded && _rover.Speed < StopSpeed, RunTimeout, "07 settles");
        }

        /// <summary>
        /// Waits until 07 reaches the pilot's waypoint. It fails only when 07 stops getting closer for
        /// <see cref="WaypointTimeout"/> seconds (stuck), never on a slow but steady drive: a fixed total time made the
        /// check depend on how the turn at the terminus happened to go.
        /// </summary>
        private IEnumerator UntilArrived(string what)
        {
            float closest = _pilot.Distance;
            float progressedAt = Time.time;
            while (_pilot.Distance >= WaypointArrive)
            {
                if (_pilot.Distance < closest - WaypointProgress)
                {
                    closest = _pilot.Distance;
                    progressedAt = Time.time;
                }

                Assert.Less(Time.time - progressedAt, WaypointTimeout, string.Format(CultureInfo.InvariantCulture,
                    "stuck: {0}, {1:F1} m short, 07 at {2} moving {3:F2} m/s", what, _pilot.Distance,
                    _rover.Position, _rover.Speed));
                yield return null;
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float started = Time.time;
            while (!condition())
            {
                Assert.Less(Time.time - started, timeout, $"timed out: {what}");
                yield return null;
            }
        }

        private void CaptureFromTheBase()
        {
            Vector3 ledge = Ground(_canyon.LedgeCenter) + Vector3.up * LookHeight;
            Vector3 eye = Ground(Vector2.zero) + Vector3.up * EyeHeight;
            Review(eye, ledge, WideFov, "01-canyon-from-the-base");
            Review(eye, ledge, ZoomFov, "01-ledge-from-the-base");
        }

        /// <summary>A review camera beside the chasm, level with the lip, looking across the leap's path.</summary>
        private Vector3 SideView()
        {
            CanyonPath main = _canyon.MainPath;
            float middle = (_canyon.LipArc + _canyon.FarFaceArc) * 0.5f;
            Vector2 side = main.PointAt(_canyon.LipArc) - main.RightAt(middle) * (_settings.ChasmHalfWidth - 2f);
            return new Vector3(side.x, _canyon.LipHeight + EyeHeight, side.y);
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight, PathFor(name));
        }

        private void Review(Vector3 eye, Vector3 target, float fov, string name)
        {
            Camera view = _context.Get<IViewCamera>().Camera;
            var host = new GameObject("CanyonReviewCamera");
            try
            {
                var review = host.AddComponent<Camera>();
                review.CopyFrom(view);
                review.enabled = false;
                review.fieldOfView = fov;
                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                FrameCapture.SavePng(review, CaptureWidth, CaptureHeight, PathFor(name));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private static string PathFor(string name)
        {
            return Path.Combine(CaptureFolder, "canyon-session-" + name + ".png");
        }

        private float ShelfHeight()
        {
            Vector2 shelf = _canyon.ExitPath.PointAt(_canyon.ExitStepArc + Canyon.SlabHalfDepth + 1f);
            return _terrain.SampleHeight(shelf.x, shelf.y);
        }

        private float Arc()
        {
            return ArcOf(_rover.Position);
        }

        private float ArcOf(Vector3 position)
        {
            return ArcOn(_canyon.MainPath, position);
        }

        private static float ArcOn(CanyonPath path, Vector3 position)
        {
            return path.TryProject(position.x, position.z, out float arc, out float _) ? arc : float.MinValue;
        }

        private Vector3 Ground(Vector2 xz)
        {
            return new Vector3(xz.x, _terrain.SampleHeight(xz.x, xz.y), xz.y);
        }

        private static float Horizontal(Vector3 v)
        {
            return new Vector2(v.x, v.z).magnitude;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float radians = -degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void OnJumped(RoverJumped jumped)
        {
            _leaps.Add(jumped.Strength);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }

        private void Note(string step, string notes)
        {
            _report.Add($"| {step} | {notes} |");
            Debug.Log($"[canyon] {step}: {notes}");
        }

        private void WriteReport()
        {
            var report = new StringBuilder();
            report.AppendLine("# Whispering Canyon session (real Main scene)");
            report.AppendLine();
            report.AppendLine("| Step | Notes |");
            report.AppendLine("|---|---|");
            foreach (string line in _report)
            {
                report.AppendLine(line);
            }

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "canyon-session.md"), report.ToString());
        }
    }
}
