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
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The owner's frame: 07 stopped at a scan (the lander's open crate, the fallen array, the airlock panel), seen
    /// through the real chase camera in the real Main scene. Within <see cref="SettleSeconds"/> of 07 standing still
    /// the camera must see every part being scanned, with 07's body not in the way, and the part must sit inside the
    /// central <see cref="CentralShare"/> of the screen. Writes the camera's frame to Logs/captures/chase-framing-*.
    /// </summary>
    public sealed class ChaseFramingSession : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 25f;
        private const float WakeEaseTimeout = 8f;
        private const float StickProbeSeconds = 0.3f;
        private const float LookStick = 0.8f;
        private const float SettleSeconds = 2f;
        private const float ReleaseSeconds = 2f;
        private const float TraceEvery = 0.25f;

        /// <summary>Set to 1 to capture the same stands with the framing switched off (its range 1 m).</summary>
        private const string BeforeVariable = "CHASE_FRAMING_BEFORE";
        private const string RangeField = "_framingRange";
        private const float OffRange = 1f;
        private const float CentralShare = 0.7f;
        private const float ArrayStandOff = 3.5f;
        private const float PanelStandOff = 2.5f;
        private const string HabitatId = "HabitatModules";
        private const string ArraySetId = "ruin.FallenSolarArray";

        /// <summary>Where the autoplay bot stood at B3 on main 4d10f6cc, and its heading toward the crate.</summary>
        private static readonly Vector3 BotPose = new Vector3(33f, 2.9f, 153.8f);

        private const float BotHeading = 72f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private Gamepad _gamepad;
        private IDisposable _awoke;
        private bool _awake;
        private Vector3 _awayFromTargets;
        private bool _before;
        private RoverCameraTuning _tuning;
        private float _range;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        public override void TearDown()
        {
            if (_tuning != null)
            {
                SetRange(_range);
            }

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
        public IEnumerator At07sScans_TheChaseCameraSeesTheParts_NotJust07sBack()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return Boot();
            _before = Environment.GetEnvironmentVariable(BeforeVariable) == "1";
            if (_before)
            {
                var tunings = Resources.FindObjectsOfTypeAll<RoverCameraTuning>();
                Assert.AreEqual(1, tunings.Length, "one camera tuning");
                _tuning = tunings[0];
#if UNITY_EDITOR
                _range = new UnityEditor.SerializedObject(_tuning).FindProperty(RangeField).floatValue;
#endif
                SetRange(OffRange);
            }

            var report = new StringBuilder("# Chase camera framing at the scans\n");
            var failures = new StringBuilder();
            ScanField scans = _gameplay.Scans;
            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.PlaceWake, out WorldAnchor wakeAnchor));
            Vector3 wake = wakeAnchor.Position;
            _awayFromTargets = Ground(terrain, wake);

            ScanFragment[] cargo = { Fragment(scans, "ruin.LanderCargo.0"), Fragment(scans, "ruin.LanderCargo.1") };
            yield return Stand("b3-cargo", BotPose, Quaternion.Euler(0f, BotHeading, 0f), cargo, placement, report,
                failures);

            for (int i = 0; scans.Find(ScanField.FragmentId(ArraySetId, i)) != null; i++)
            {
                ScanFragment part = Fragment(scans, ScanField.FragmentId(ArraySetId, i));
                Vector3 away = Flat(wake - part.Point).normalized;
                Vector3 at = Ground(terrain, part.Point + away * ArrayStandOff);
                yield return Stand("array-" + i.ToString(CultureInfo.InvariantCulture), at,
                    Quaternion.LookRotation(-away, Vector3.up), null, placement, report, failures);
            }

            ScanFragment panel = Fragment(scans, StoryScan.IdPrefix + "airlock");
            Vector3 outward = Flat(panel.Point - HabitatCentre()).normalized;
            yield return Stand("airlock-panel", Ground(terrain, panel.Point + outward * PanelStandOff),
                Quaternion.LookRotation(-outward, Vector3.up), null, placement, report, failures);

            File.WriteAllText(Path.Combine(CaptureFolder, "chase-framing" + Suffix() + ".md"), report.ToString());
            Debug.Log("[chase-framing]\n" + report);
            if (!_before)
            {
                Assert.AreEqual(string.Empty, failures.ToString(), "The chase camera hides a scan target:\n" + failures);
            }
        }

        /// <param name="parts">What must show; null for what the scan picks at the stand (the part E would scan).</param>
        private IEnumerator Stand(string name, Vector3 at, Quaternion facing, ScanFragment[] parts,
            IRoverPlacement placement, StringBuilder report, StringBuilder failures)
        {
            placement.PlaceAt(_awayFromTargets, Quaternion.identity);
            yield return Wait(ReleaseSeconds);
            placement.PlaceAt(at, facing);
            var trace = new StringBuilder();
            float settled = Time.time + SettleSeconds;
            float nextTrace = Time.time;
            while (Time.time < settled)
            {
                if (Time.time >= nextTrace)
                {
                    trace.Append($" [w {F(_cameraRig.Framing.Weight)} yaw {F(_cameraRig.Framing.YawOffset)} " +
                                 $"goal {F(_cameraRig.Framing.Goal.x)} orbit {F(_cameraRig.Orbit.YawOffset)}/{F(_cameraRig.Orbit.Elevation)}]");
                    nextTrace = Time.time + TraceEvery;
                }

                yield return null;
            }
            ScanField scans = _gameplay.Scans;
            Vector3 eye = _view.transform.position;
            report.AppendLine().AppendLine($"## {name}: 07 {V(_rover.Position)} speed {F(_rover.Speed)}, camera {V(eye)}, " +
                                           $"framing {F(_cameraRig.Framing.Weight)} (yaw {F(_cameraRig.Framing.YawOffset)}, " +
                                           $"lift {F(_cameraRig.Framing.Elevation)}, pull {F(_cameraRig.Framing.RadiusScale)}), focus " +
                                           $"{Focus()}, candidate {(scans.Candidate != null ? scans.Candidate.FragmentId : "none")}");
            report.AppendLine("- trace:" + trace);
            if (parts == null)
            {
                Assert.IsNotNull(scans.Candidate, $"{name}: the scan has a candidate at the stand");
                parts = new[] { scans.Candidate };
            }

            Vector3 toEye = eye - (_rover.Position + Vector3.up * 1.1f);
            report.AppendLine($"- orbit elevation {F(_cameraRig.Orbit.Elevation)} yaw {F(_cameraRig.Orbit.YawOffset)}, " +
                              $"real elevation {F(Mathf.Atan2(toEye.y, Flat(toEye).magnitude) * Mathf.Rad2Deg)} " +
                              $"radius {F(toEye.magnitude)}, goal {V(_cameraRig.Framing.Goal)}");
            foreach (ScanFragment part in parts)
            {
                Vector3 screen = _view.WorldToViewportPoint(part.Point);
                string first = Physics.Linecast(eye, part.Point, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    ? $"{hit.collider.name} at {F(hit.distance)} m"
                    : "clear";
                string hider = HiddenBy07(eye, part.Point);
                bool hidden = hider != null;

                float margin = (1f - CentralShare) * 0.5f;
                bool central = screen.z > 0f && screen.x >= margin && screen.x <= 1f - margin && screen.y >= margin
                    && screen.y <= 1f - margin;
                report.AppendLine($"- {part.FragmentId} at {V(part.Point)}: viewport {V(screen)}, first hit {first}, " +
                                  $"hidden by 07 {hider ?? "no"}, central {central}, model says clear " +
                                  $"{_cameraRig.Framing.BodyClear(_rover.Position, Heading(), eye, part.Point)} " +
                                  $"(07 yaw {F(Heading())})");
                if (hidden || !central)
                {
                    failures.AppendLine($"{name} / {part.FragmentId}: hidden by 07 {hidden}, central {central} " +
                                        $"(viewport {V(screen)}, first hit {first}).");
                }
            }

            yield return Capture("chase-framing-" + name + Suffix());
        }

        /// <summary>
        /// The name of the piece of 07's drawn body (each mesh's own box, not its physics) standing between the camera and
        /// the point, or null:
        /// 07's colliders are a low chassis, so a ray against them says clear while its head hides the part.
        /// </summary>
        private string HiddenBy07(Vector3 eye, Vector3 point)
        {
            foreach (Renderer renderer in _rover.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Bounds local;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    local = skinned.localBounds;
                }
                else if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    local = filter.sharedMesh.bounds;
                }
                else
                {
                    continue;
                }

                Transform node = renderer.transform;
                Vector3 from = node.InverseTransformPoint(eye);
                Vector3 along = node.InverseTransformPoint(point) - from;
                if (local.IntersectRay(new Ray(from, along), out float distance) && distance <= along.magnitude)
                {
                    return $"{renderer.name} local size {V(local.size)} centre {V(local.center)} scale {V(node.lossyScale)} world {V(renderer.bounds.size)}";
                }
            }

            return null;
        }

        private string Suffix()
        {
            return _before ? "-before" : string.Empty;
        }

        private void SetRange(float range)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_tuning);
            serialized.FindProperty(RangeField).floatValue = range;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
        }

        private float Heading()
        {
            Vector3 forward = _rover.Rotation * Vector3.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private string Focus()
        {
            var points = new Vector3[8];
            int count = _context.TryGet(out IInteractionFocus focus) ? focus.CopyFocus(points) : 0;
            var text = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                text.Append(V(points[i])).Append(' ');
            }

            return count > 0 ? text.ToString() : "none";
        }

        private IEnumerator Boot()
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
            throw new NotSupportedException("ChaseFramingSession loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            Assert.IsNotNull(bootstrap, "Scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Scene has GameplaySystem.");
            Assert.IsNotNull(_rover, "Scene has RoverController.");
            Assert.IsNotNull(_cameraRig, "Scene has RoverCameraRig.");
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            yield return Until(() => _awake, WakeTimeout, "07 wakes up");
            if (_cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(LookStick, 0f));
                yield return Wait(StickProbeSeconds);
                Set(_gamepad.rightStick, Vector2.zero);
                yield return Until(() => !_cameraRig.Opening.IsActive, WakeEaseTimeout, "the wake opening hands over");
            }
        }

        private IEnumerator Capture(string fileName)
        {
            yield return null;
            Texture2D frame = FrameCapture.Render(_view, CaptureWidth, CaptureHeight);
            try
            {
                FrameCapture.WritePng(frame, Path.Combine(CaptureFolder, fileName + ".png"));
            }
            finally
            {
                Object.Destroy(frame);
            }
        }

        private Vector3 HabitatCentre()
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                Transform ruins = root.transform.Find("Ruins");
                Transform habitat = ruins != null ? ruins.Find(HabitatId) : null;
                if (habitat != null)
                {
                    return habitat.position;
                }
            }

            Assert.Fail("The scene has no " + HabitatId + " ruin.");
            return Vector3.zero;
        }

        private static ScanFragment Fragment(ScanField scans, string id)
        {
            ScanFragment fragment = scans.Find(id);
            Assert.IsNotNull(fragment, $"scan fragment {id} exists");
            return fragment;
        }

        private static Vector3 Ground(ITerrainQuery terrain, Vector3 at)
        {
            return new Vector3(at.x, terrain.SampleHeight(at.x, at.z), at.z);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static string V(Vector3 v)
        {
            return $"({F(v.x)}, {F(v.y)}, {F(v.z)})";
        }

        private static string F(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
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
    }
}
