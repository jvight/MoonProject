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
    /// The lander's open crate (opening-v2 B3/B4) in the real Main scene, in the wake's eclipse light: 07 on the cable
    /// 20 m out, at the port, arrived and turned toward the crate (where both cargo fragments must be scan-acquirable),
    /// and where the autoplay bot stood, each with the game camera's view captured and what the scan picks written
    /// down (each fragment's distance, aim score, the first thing between the camera or the lens and it), plus the
    /// layout from above and the crate up close. Captures and the report land in Logs/captures/lander-crate-*.
    /// </summary>
    [Explicit("Slow capture session in the real Main scene; run on demand with --category LanderCrateSession.")]
    [Category("LanderCrateSession")]
    public sealed class LanderCrateSession : InputTestFixture
    {
        private const string CrateId = "supply.lander.open";
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 25f;
        private const float WakeEaseTimeout = 8f;
        private const float StickProbeSeconds = 0.3f;
        private const float LookStick = 0.8f;
        private const float SettleSeconds = 1.5f;
        private const string PortName = "ServicePort";
        private const float ApproachBack = 20f;
        private const float PortBack = 3.5f;
        private const float OverheadHeight = 14f;
        private const float CloseBack = 3f;
        private const float CloseSide = 1.5f;
        private const float CloseHeight = 1.7f;
        private const float CloseLookHeight = 0.5f;
        private const float CloseFov = 40f;
        private const float OverheadFov = 50f;

        /// <summary>Where the autoplay bot stood when it timed out at B3 on main 4d10f6cc.</summary>
        private static readonly Vector3 BotPose = new Vector3(31.6f, 2.9f, 154.6f);

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

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        public override void TearDown()
        {
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
        public IEnumerator TheLanderCrate_IsCapturedFromWhere07DrivesUp_InTheEclipse()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return Boot();

            Transform crate = _gameplay.Opening.Caches.RootOf(CrateId);
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(WorldAnchorIds.PlaceLander, out WorldAnchor lander));
            var report = new StringBuilder();
            report.AppendLine("# The lander's open crate (B3/B4)").AppendLine();
            report.AppendLine($"- lander anchor {V(lander.Position)} forward {V(lander.Forward)}");
            report.AppendLine($"- crate {V(crate.position)} shown {Shown(crate)}");
            foreach (BoxCollider box in crate.GetComponentsInChildren<BoxCollider>(true))
            {
                report.AppendLine($"- solid {box.name} centre {V(box.center)} size {V(box.size)}, world " +
                                  $"{V(box.bounds.min)}..{V(box.bounds.max)}, crate scale {V(crate.lossyScale)}");
            }

            IWorldAnchors anchors = _context.Get<IWorldAnchors>();
            Quaternion level = _gameplay.Opening.Caches.LevelOf(CrateId);
            foreach (MeshFilter mesh in crate.GetComponentsInChildren<MeshFilter>(true))
            {
                report.AppendLine($"- level frame: {mesh.name} {InLevel(mesh.transform, crate.position, level)}");
            }

            for (int i = 1; i <= 2; i++)
            {
                Transform piece = Node(crate, "CargoFragment_" + i.ToString(CultureInfo.InvariantCulture));
                report.AppendLine($"- {piece.name} {V(piece.position)} shown {Shown(piece)}");
            }

            foreach (string id in new[] { WorldAnchorIds.PlaceWake, WorldAnchorIds.PlaceApron })
            {
                Assert.IsTrue(anchors.TryGet(id, out WorldAnchor anchor), id);
                report.AppendLine($"- {id} {V(anchor.Position)}");
            }

            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 port = PortPosition();
            Vector3 outward = Flat(lander.Forward).normalized;
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.PlaceWake, out WorldAnchor wake));
            Vector3 fromWake = Flat(port - wake.Position).normalized;
            yield return Pose("approach", Ground(terrain, port - fromWake * ApproachBack),
                Quaternion.LookRotation(fromWake, Vector3.up), crate, report, placement);
            yield return Pose("port", Ground(terrain, port + outward * PortBack),
                Quaternion.LookRotation(-outward, Vector3.up), crate, report, placement);
            Vector3 toFront = Flat(port + outward * PortBack - crate.position).normalized;
            yield return Pose("arrival", Ground(terrain, port + outward * PortBack),
                Quaternion.LookRotation(-toFront, Vector3.up), crate, report, placement, true);
            yield return Pose("bot", BotPose, Quaternion.Euler(0f, BotHeading, 0f), crate, report, placement);
            yield return Still("lander-crate-overhead", crate.position + Vector3.up * OverheadHeight,
                Quaternion.LookRotation(Vector3.down, Vector3.forward), OverheadFov);
            Vector3 side = Vector3.Cross(Vector3.up, toFront);
            Vector3 closeEye = crate.position + (toFront * CloseBack + side * CloseSide) + Vector3.up * CloseHeight;
            yield return Still("lander-crate-close", closeEye,
                Quaternion.LookRotation(crate.position + Vector3.up * CloseLookHeight - closeEye, Vector3.up),
                CloseFov);
            File.WriteAllText(Path.Combine(CaptureFolder, "lander-crate.md"), report.ToString());
            Debug.Log("[lander-crate]\n" + report);
        }

        /// <param name="acquirable">07 stands where it arrives at B3, turned toward the crate: both fragments must lie
        /// in reach, inside the camera cone and in plain view of its lens.</param>
        private IEnumerator Pose(string name, Vector3 at, Quaternion facing, Transform crate, StringBuilder report,
            IRoverPlacement placement, bool acquirable = false)
        {
            placement.PlaceAt(at, facing);
            yield return Wait(SettleSeconds);
            Transform rim = Node(crate, "RimGlow");
            report.AppendLine().AppendLine($"## {name}: 07 {V(_rover.Position)}, camera " +
                                           $"{V(_view.transform.position)} looking {V(_view.transform.forward)}");
            ScanField scans = _gameplay.Scans;
            ScanFragment looked = scans.LookedAt(out float lookedScore);
            report.AppendLine($"- candidate {Id(scans.Candidate)}, looked at {Id(looked)} ({F(lookedScore)}), crate " +
                              $"{F(Vector3.Distance(_rover.Position, crate.position))} m, rim lit " +
                              $"{rim.gameObject.activeInHierarchy}");
            for (int i = 0; i < 2; i++)
            {
                ScanFragment fragment = scans.Find("ruin." + ScanTuning.LanderCargoId + "." +
                                                   i.ToString(CultureInfo.InvariantCulture));
                Assert.IsNotNull(fragment, $"cargo fragment {i}");
                bool inView = scans.InView(fragment.Point, out float score);
                Vector3 eye = _view.transform.position;
                string blocker = Physics.Linecast(eye, fragment.Point, out RaycastHit hit, ~0,
                    QueryTriggerInteraction.Ignore)
                    ? $"{NodePath(hit.collider.transform)} at {F(hit.distance)} m"
                    : "clear";
                Vector3 lens = _rover.ScanOrigin.position;
                string lensBlocker = Physics.Linecast(lens, fragment.Point, out RaycastHit lensHit, ~0,
                    QueryTriggerInteraction.Ignore)
                    ? $"{NodePath(lensHit.collider.transform)} at {F(lensHit.distance)} m"
                    : "clear";
                Vector3 screen = _view.WorldToViewportPoint(fragment.Point);
                float away = Vector3.Distance(_rover.Position, fragment.Point);
                report.AppendLine($"- {fragment.FragmentId} at {V(fragment.Point)}: {F(away)} m from 07, " +
                    $"in view {inView} ({F(score)}), viewport " +
                    $"{V(screen)}, camera ray {blocker}, lens ray {lensBlocker}");
                if (acquirable)
                {
                    Assert.LessOrEqual(away, _gameplay.Scans.Tuning.Reach, $"{fragment.FragmentId} in scan reach");
                    Assert.IsTrue(inView, $"{fragment.FragmentId} inside the camera cone");
                    Assert.AreEqual("clear", lensBlocker, $"{fragment.FragmentId} in plain view of 07's lens");
                }
            }

            yield return Capture("lander-crate-" + name);
        }

        /// <summary>A still from a free camera with the game camera's look: the layout, or the crate close.</summary>
        private IEnumerator Still(string fileName, Vector3 eye, Quaternion look, float fov)
        {
            var host = new GameObject("LanderCrateStill");
            try
            {
                Camera camera = host.AddComponent<Camera>();
                camera.CopyFrom(_view);
                camera.enabled = false;
                camera.fieldOfView = fov;
                host.transform.SetPositionAndRotation(eye, look);
                yield return null;
                Texture2D frame = FrameCapture.Render(camera, CaptureWidth, CaptureHeight);
                FrameCapture.WritePng(frame, Path.Combine(CaptureFolder, fileName + ".png"));
                Object.Destroy(frame);
            }
            finally
            {
                Object.Destroy(host);
            }
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
            throw new NotSupportedException("LanderCrateSession loads the scene through the editor.");
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

        private Vector3 PortPosition()
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == PortName)
                    {
                        return node.position;
                    }
                }
            }

            Assert.Fail("The scene has no lander service port.");
            return Vector3.zero;
        }

        private static Transform Node(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            Assert.Fail($"{root.name} has no {name}.");
            return null;
        }

        private static Vector3 Ground(ITerrainQuery terrain, Vector3 at)
        {
            return new Vector3(at.x, terrain.SampleHeight(at.x, at.z), at.z);
        }

        /// <summary>A part's own mesh's extent in the crate's level frame (x right, y up, z its front).</summary>
        private static string InLevel(Transform part, Vector3 origin, Quaternion level)
        {
            Quaternion toLevel = Quaternion.Inverse(level);
            var bounds = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in part.GetComponents<MeshFilter>())
            {
                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = toLevel * (filter.transform.TransformPoint(local) - origin);
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }

                    bounds.Encapsulate(point);
                }
            }

            return any ? $"min {V(bounds.min)} max {V(bounds.max)}" : "no mesh";
        }

        private static string Shown(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return "nothing";
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return $"bounds centre {V(bounds.center)} size {V(bounds.size)}";
        }

        private static string NodePath(Transform node)
        {
            string path = node.name;
            for (Transform parent = node.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        private static string Id(ScanFragment fragment)
        {
            return fragment != null ? fragment.FragmentId : "none";
        }

        private static string V(Vector3 v)
        {
            return $"({F(v.x)}, {F(v.y)}, {F(v.z)})";
        }

        private static string F(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
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
    }
}
