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

        private const float TeleportLift = 0.1f;
        private const float StopSpeed = 0.4f;
        private const float WaypointSpacing = 10f;
        private const float WaypointArrive = 4f;
        private const float WaypointTimeout = 20f;
        private const float RunTimeout = 30f;
        private const float InsideThrottle = 0.7f;
        private const float EyeHeight = 3f;
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
        [Timeout(900000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator Canyon_IsAGateForTheHoverJump_WithAOneWayExit()
        {
            yield return Boot();
            CaptureFromTheBase();

            yield return RunAtTheLip(CruiseThrottle, ThreeQuarterCharge, "without the Hover-Jump", false);
            AssertInTheTrough("without the Hover-Jump");
            Capture("02-trough-without-the-ability");
            yield return PushAtGate(_canyon.MainPath, _canyon.FarFaceArc, _canyon.ApronHeight, "the chasm's far face");
            yield return DriveTo(Ground(_canyon.MainPath.PointAt(0f)), 1f, "back out of the trough to the mouth");
            Note("Trough", "drove back out to the mouth on its own (no help)");

            _context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            yield return RunAtTheLip(1f, 0f, "a tap at top speed", true);
            AssertInTheTrough("a tap at top speed");

            yield return RunAtTheLip(CruiseThrottle, ThreeQuarterCharge, "a 3/4 charge at cruising speed", true);
            AssertAcross("a 3/4 charge at cruising speed");
            yield return RunAtTheLip(1f, FullChargeMargin, "a full charge at top speed", true);
            AssertAcross("a full charge at top speed");
            yield return Settle();
            Capture("05-landed-on-the-apron");

            yield return DriveInside();
            yield return LeaveByTheExit();
            yield return PushAtGate(_canyon.ExitPath, _canyon.ExitStepArc, ShelfHeight(), "the exit step");

            WriteReport();
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

        /// <summary>
        /// One run at the chasm: from the basin in line with the canyon at <paramref name="throttle"/>, holding Jump
        /// for <paramref name="charge"/> of a full charge so that it is let go just before the lip, then following
        /// the flight to touchdown (or, without a leap, the roll down into the trough).
        /// </summary>
        private IEnumerator RunAtTheLip(float throttle, float charge, string label, bool expectLeap)
        {
            yield return Teleport(_canyon.MainPath.PointAt(-RunUp));
            _pilot.Throttle = throttle;
            _pilot.Target = Ground(_canyon.MainPath.PointAt(_canyon.ApronEndArc));
            float fixedStep = Time.fixedDeltaTime;
            int holdSteps = Mathf.RoundToInt(charge * _rover.Tuning.HoverJump.ChargeTime / fixedStep) + 1;
            float started = Time.time;
            while (Arc() < _canyon.LipArc - ReleaseLead - _rover.Speed * holdSteps * fixedStep)
            {
                Assert.Less(Time.time - started, RunTimeout, $"{label}: 07 never reached the lip");
                yield return new WaitForFixedUpdate();
            }

            float speed = _rover.Speed;
            if (expectLeap && charge > 0.5f && charge < 1f)
            {
                Capture("03-lip-charging");
            }

            int leapsBefore = _leaps.Count;
            int landingsBefore = _landings.Count;
            _pilot.JumpHeld = true;
            for (int i = 0; i < holdSteps; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            _pilot.JumpHeld = false;
            Vector3 takeOff = _rover.Position;
            float apex = takeOff.y;
            bool captured = false;
            started = Time.time;
            while (Time.time - started < RunTimeout)
            {
                apex = Mathf.Max(apex, _rover.Position.y);
                bool leapt = _leaps.Count > leapsBefore;
                if (leapt && !captured && !_rover.IsGrounded && _rover.Velocity.y < 0f && charge > 0.5f)
                {
                    captured = true;
                    Capture("04-mid-leap-" + (throttle < 1f ? "cruise" : "top-speed"));
                    Review(SideView(), _rover.Position, WideFov, "04-mid-leap-side-" +
                                                                   (throttle < 1f ? "cruise" : "top-speed"));
                }

                bool touchedDown = leapt && _landings.Count > landingsBefore && _rover.IsGrounded;
                bool rolledIn = !expectLeap && Arc() > _canyon.LipArc + 6f && _rover.IsGrounded;
                if (touchedDown || rolledIn)
                {
                    break;
                }

                yield return null;
            }

            Assert.AreEqual(expectLeap, _leaps.Count > leapsBefore, $"{label}: leap or not as expected");
            _pilot.Release();
            Vector3 landing = _landings.Count > landingsBefore ? _landings[_landings.Count - 1] : _rover.Position;
            float run = Horizontal(landing - takeOff);
            string strength = expectLeap ? _leaps[_leaps.Count - 1].ToString("F2", CultureInfo.InvariantCulture)
                : "no leap";
            Note(label, string.Format(CultureInfo.InvariantCulture,
                "take-off {0:F1} m/s at {1:F1} m before the lip, strength {2}, apex {3:F1} m, touchdown {4:F1} m " +
                "from take-off at arc {5:F1} (far face slab from {6:F1})", speed, _canyon.LipArc - ArcOf(takeOff),
                strength, apex - takeOff.y, run, ArcOf(landing), _canyon.FarFaceArc - Canyon.SlabHalfDepth));
            yield return Settle();
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
            Vector3 glow = Ground(_canyon.GlowPoint) + Vector3.up * _settings.GlowHeight;
            Review(ledge.Position - ledge.Forward * 7f + Vector3.up * 2f, glow, WideFov, "06-ledge-glow");
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
                yield return Until(() => _pilot.Distance < WaypointArrive, WaypointTimeout,
                    $"07 drives {what} (waypoint at arc {arc:F0})");
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

        private IEnumerator Teleport(Vector2 xz)
        {
            _pilot.Release();
            Rigidbody body = _rover.PhysicsBody;
            var position = new Vector3(xz.x, _terrain.SampleHeight(xz.x, xz.y) + _rover.SphereRadius + TeleportLift,
                xz.y);
            body.linearVelocity = Vector3.zero;
            body.position = position;
            body.transform.position = position;
            Physics.SyncTransforms();
            yield return Settle();
        }

        private IEnumerator Settle()
        {
            yield return Until(() => _rover.IsGrounded && _rover.Speed < StopSpeed, RunTimeout, "07 settles");
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
            Vector3 glow = Ground(_canyon.GlowPoint) + Vector3.up * _settings.GlowHeight;
            Vector3 eye = Ground(Vector2.zero) + Vector3.up * EyeHeight;
            Review(eye, glow, WideFov, "01-canyon-from-the-base");
            Review(eye, glow, ZoomFov, "01-ledge-glow-from-the-base");
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
