using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The built Main scene on an isolated save slot (<see cref="BuildModeCaptureTests.BuildCaptureScene"/> makes the
    /// copy), booted until 07 is awake: the real Gameplay with its <see cref="BuildField"/> under the real UI. Shared
    /// by the build mode tests and captures.
    /// </summary>
    internal sealed class BuildScene
    {
        private const float WakeTimeout = 25f;
        private const float SettleSeconds = 0.6f;

        public GameContext Context { get; private set; }

        public GameplaySystem Gameplay { get; private set; }

        public UISystem Ui { get; private set; }

        public BuildField Build => Gameplay.Building;

        /// <summary>Loads the scene copy and waits for 07 to wake.</summary>
        public IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(BuildModeCaptureTests.BuildCaptureScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                BuildModeCaptureTests.BuildCaptureScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The build scene loads in the Editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(BuildModeCaptureTests.BuildCaptureScene.ScenePath)
                         .GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                Gameplay = Gameplay != null ? Gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                Ui = Ui != null ? Ui : root.GetComponentInChildren<UISystem>(true);
            }

            Assert.IsNotNull(bootstrap, "Scene has GameBootstrap.");
            Assert.IsNotNull(Gameplay, "Scene has GameplaySystem.");
            Assert.IsNotNull(Ui, "Scene has UISystem.");
            Context = bootstrap.Context;

            bool awake = false;
            using (Context.Events.Subscribe<RoverAwoke>(_ => awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!awake && Time.time < deadline)
                {
                    yield return null;
                }
            }

            Assert.IsTrue(awake, "07 wakes up.");
            yield return new WaitForSeconds(SettleSeconds);
        }

        /// <summary>Empties the playtest unlocks, destroys the scene's objects and the slot's save.</summary>
        public void TearDown()
        {
            if (Gameplay != null && Gameplay.Building != null && Gameplay.Building.Tuning != null)
            {
                Gameplay.Building.Tuning.SetPlaytestUnlockedIds();
            }

            Scene scene = SceneManager.GetSceneByPath(BuildModeCaptureTests.BuildCaptureScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(BuildModeCaptureTests.BuildCaptureScene.SaveSlot);
        }

        /// <summary>
        /// Teaches 07 <paramref name="structureIds"/> (through the tuning's playtest list, which
        /// <see cref="TearDown"/> empties again) and sets its stock, so the menu has cards and placing can weld.
        /// </summary>
        public void Unlock(string[] structureIds, int metal, int wiring, int optics)
        {
            Build.Tuning.SetPlaytestUnlockedIds(structureIds);
            Build.ApplyPlaytestUnlocks(restored: true);
            Build.Controller.RefreshAvailable();
            Recipe current = new Recipe(Gameplay.Materials.Metal, Gameplay.Materials.Wiring, Gameplay.Materials.Optics);
            if (current.Metal > 0 || current.Wiring > 0 || current.Optics > 0)
            {
                Gameplay.Materials.TrySpend(current);
            }

            Gameplay.Materials.Collect(SalvageMaterial.Metal, metal);
            Gameplay.Materials.Collect(SalvageMaterial.Wiring, wiring);
            Gameplay.Materials.Collect(SalvageMaterial.Optics, optics);
        }
    }
}
