using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The real Main scene (<see cref="RoverSessionScene"/>'s copy) booted for a PlayMode test: loaded, 07 awake and
    /// handed to the player after its power-on self-test, with the systems a face-screen test drives. Unload it in the
    /// test's tear-down.
    /// </summary>
    internal sealed class RealRoverScene
    {
        private const float WakeTimeout = 20f;
        private const float PowerOnTimeout = 60f;

        private IDisposable _awokeSub;
        private bool _awake;

        public GameContext Context { get; private set; }

        public RoverController Rover { get; private set; }

        public RoverCameraRig CameraRig { get; private set; }

        public RoverBodyLanguage Body { get; private set; }

        public UISystem Ui { get; private set; }

        public Camera View { get; private set; }

        public IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("RealRoverScene loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                Rover = Rover != null ? Rover : root.GetComponentInChildren<RoverController>(true);
                CameraRig = CameraRig != null ? CameraRig : root.GetComponentInChildren<RoverCameraRig>(true);
                Body = Body != null ? Body : root.GetComponentInChildren<RoverBodyLanguage>(true);
                Ui = Ui != null ? Ui : root.GetComponentInChildren<UISystem>(true);
            }

            Assert.IsNotNull(bootstrap, "Main scene must contain GameBootstrap.");
            Assert.IsNotNull(Ui, "Main scene must contain UISystem.");
            Context = bootstrap.Context;
            View = Context.Get<IViewCamera>().Camera;
            _awokeSub = Context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float start = Time.time;
            while (!_awake || Body.Mood.Wake.Sleep > 0.05f)
            {
                Assert.Less(Time.time - start, WakeTimeout, "07 must finish waking in the real scene.");
                yield return null;
            }

            float released = Time.time;
            while (Rover.IsHeldStill)
            {
                Assert.Less(Time.time - released, PowerOnTimeout,
                    "07 must be handed to the player after its self-test.");
                yield return null;
            }
        }

        public void Unload()
        {
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

        /// <summary>Waits a few seconds of game time.</summary>
        public static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}
