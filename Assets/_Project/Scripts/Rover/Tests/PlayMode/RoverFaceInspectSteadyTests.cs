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

        /// <summary>The most the screen's centre may wander in the frame (viewport units, 1 = the frame's height).</summary>
        private const float MaxWander = 0.0015f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private GameContext _context;
        private RoverController _rover;
        private RoverBodyLanguage _body;
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
            yield return Wait(SettleSeconds);
            Assert.Less(_rover.Speed, 0.1f, "07 has come to a stop under the face view.");

            Transform screen = _body.ScreenTransform;
            Vector2 first = default;
            Vector2 min = default;
            Vector2 max = default;
            var trace = new StringBuilder();
            for (float t = 0f; t < SampleSeconds; t += FrameTime)
            {
                Vector3 viewport = _view.WorldToViewportPoint(screen.position);
                var point = new Vector2(viewport.x, viewport.y);
                if (t == 0f)
                {
                    first = point;
                    min = point;
                    max = point;
                }

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
                trace.AppendLine($"{t:0.000}\t{point.x:0.00000}\t{point.y:0.00000}");
                yield return null;
            }

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "face_inspect_steady.tsv"), trace.ToString());
            float wander = Mathf.Max(max.x - min.x, max.y - min.y);
            Debug.Log($"[face-steady] screen at ({first.x:0.000}, {first.y:0.000}), wanders {wander:0.00000} of the frame");
            Assert.LessOrEqual(wander, MaxWander, "The CRT screen stays put in the frame once the face view has settled.");
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
            }

            Assert.IsNotNull(bootstrap);
            Assert.IsNotNull(_rover);
            Assert.IsNotNull(_body);
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
