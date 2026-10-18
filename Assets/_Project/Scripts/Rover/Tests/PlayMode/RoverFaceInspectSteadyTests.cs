using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
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
    /// Owner playtest: pressing Tab while driving made the inventory screen shake slightly and continuously. In the
    /// real scene 07 drives, the face view opens on it, and once the view has settled the CRT screen must stay put in
    /// the frame: its viewport position may not wander over two seconds.
    /// </summary>
    public sealed class RoverFaceInspectSteadyTests
    {
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 20f;
        private const float DriveSeconds = 4f;
        private const float SettleSeconds = 4f;
        private const float SampleSeconds = 2f;

        /// <summary>The most the screen's pose may turn in the frame from one frame to the next once the view eases in (deg).</summary>
        /// <summary>The shipped face-inspect settle speed (m/s).</summary>
        private const float SettleSpeed = 0.05f;

        private const float MaxFrameStep = 0.6f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private GameContext _context;
        private RoverController _rover;
        private RoverBodyLanguage _body;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private IDisposable _awokeSub;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
            _awake = false;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _awokeSub?.Dispose();
            _awokeSub = null;
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator OpeningTheFaceWhileDriving_LeavesTheScreenSteadyInTheFrame()
        {
            yield return BootRealScene();
            var drive = new ScriptedDrive { Drive = new Vector2(0f, 1f) };
            _rover.SetDriveSource(drive);
            yield return Wait(DriveSeconds);
            Assert.Greater(_rover.Speed, 2f, "07 is driving when Tab is pressed.");

            drive.Drive = Vector2.zero;
            _rover.SetScreenOpen(true, null);
            Transform screen = _body.ScreenTransform;
            Transform camera = _view.transform;
            var trace = new StringBuilder("t\tspeed\tdistance\tyaw\tpitch\troll\tviewX\tviewY\n");
            float settleSpeed = SettleSpeed;
            float previousPitch = 0f;
            float previousRoll = 0f;
            float maxStep = 0f;
            bool eased = false;
            for (float t = 0f; t < SettleSeconds + SampleSeconds; t += FrameTime)
            {
                if (_rover.Speed > settleSpeed)
                {
                    Assert.AreEqual(0f, _cameraRig.FaceInspectWeight, 1e-6f,
                        "While 07 still brakes the camera stays in the chase frame.");
                }

                Quaternion local = Quaternion.Inverse(camera.rotation) * screen.rotation;
                Vector3 euler = local.eulerAngles;
                Vector3 viewport = _view.WorldToViewportPoint(screen.position);
                trace.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:0.000}\t{1:0.000}\t{2:0.0000}\t{3:0.000}\t{4:0.000}\t{5:0.000}\t{6:0.00000}\t{7:0.00000}",
                    t, _rover.Speed, Vector3.Distance(camera.position, screen.position),
                    Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.x), Mathf.DeltaAngle(0f, euler.z),
                    viewport.x, viewport.y));
                float pitch = Mathf.DeltaAngle(0f, euler.x);
                float roll = Mathf.DeltaAngle(0f, euler.z);
                if (_cameraRig.FaceInspectWeight > 0.001f)
                {
                    if (eased)
                    {
                        maxStep = Mathf.Max(maxStep, Mathf.Abs(pitch - previousPitch), Mathf.Abs(roll - previousRoll));
                    }

                    eased = true;
                }

                previousPitch = pitch;
                previousRoll = roll;
                yield return null;
            }

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "face_inspect_steady.tsv"), trace.ToString());
            Assert.IsTrue(eased, "The face view eases in once 07 has stopped.");
            Assert.LessOrEqual(maxStep, MaxFrameStep, "The screen's pose never jolts from one frame to the next.");
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IEnumerator BootRealScene()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The face-inspect session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _body = _body != null ? _body : root.GetComponentInChildren<RoverBodyLanguage>(true);
                _cameraRig = _cameraRig != null ? _cameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
            }

            Assert.IsNotNull(bootstrap);
            Assert.IsNotNull(_rover);
            Assert.IsNotNull(_body);
            Assert.IsNotNull(_cameraRig);
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _awokeSub = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float start = Time.time;
            while (!_awake || _body.Mood.Wake.Sleep > 0.05f)
            {
                Assert.Less(Time.time - start, WakeTimeout, "07 must finish waking in the real scene.");
                yield return null;
            }
        }
    }
}
