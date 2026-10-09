using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// New game (M3-15 §1): mid-game, with an ability earned, materials in the stock and the progress saved,
    /// <see cref="NewGameRequested"/> puts the save away and reloads the built Main scene (the playthrough's copy on
    /// its own slot) into a clean fresh boot: one bootstrap, nothing doubled in DontDestroyOnLoad, no ability, an empty
    /// stock, nothing loaded, and the old journey kept as &lt;slot&gt;.old-&lt;stamp&gt;.json.
    /// </summary>
    [PrebuildSetup(typeof(NewGameScene))]
    [PostBuildCleanup(typeof(NewGameScene))]
    public sealed class NewGameSession
    {
        private const float ReloadTimeout = 30f;

        [UnityTest]
        public IEnumerator NewGameRequested_PutsTheSaveAway_AndBootsAFreshGame()
        {
            GameBootstrap first = null;
            yield return Boot(found => first = found);
            GameContext context = first.Context;
            int persistent = PersistentRoots();
            context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            ((MaterialStock)context.Get<IMaterialStock>()).Add(SalvageMaterial.Metal, 20);
            ISaveService save = context.Get<ISaveService>();
            Assert.IsTrue(save.SaveNow(), "mid-game progress is on disk");
            string savePath = save.FilePath;

            context.Events.Publish(new NewGameRequested());
            float deadline = Time.realtimeSinceStartup + ReloadTimeout;
            GameBootstrap fresh = null;
            while (fresh == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                fresh = first == null ? FindBootstrap() : null;
            }

            Assert.IsNotNull(fresh, "the scene reloads with a new bootstrap");
            Assert.AreEqual(1, CountBootstraps(), "one bootstrap, never two");
            Assert.AreEqual(1, SceneManager.sceneCount, "the scene alone, reloaded");
            Assert.AreEqual(persistent, PersistentRoots(), "nothing doubled in DontDestroyOnLoad");

            GameContext now = fresh.Context;
            Assert.AreNotSame(context, now, "a new context");
            Assert.AreEqual(SaveLoadResult.NoSave, now.Get<ISaveService>().LoadResult, "nothing was loaded");
            IRoverAbilities abilities = now.Get<IRoverAbilities>();
            foreach (RoverAbility ability in Enum.GetValues(typeof(RoverAbility)))
            {
                Assert.IsFalse(abilities.Has(ability), $"{ability} is not earned in a new game");
            }

            Assert.AreEqual(0, now.Get<IMaterialStock>().Total, "the stock starts empty");
            Assert.IsFalse(File.Exists(savePath), "the old save is no longer the current one");
            Assert.AreEqual(1, NewGameScene.PutAwayFiles().Length, "it was put away, not deleted");
        }

        private static IEnumerator Boot(Action<GameBootstrap> found)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync(PlaythroughScene.ScenePath, LoadSceneMode.Single);
            Assert.IsNotNull(loading, "the copy is listed in the build settings for the run");
            while (!loading.isDone)
            {
                yield return null;
            }

            GameBootstrap bootstrap = FindBootstrap();
            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(bootstrap.Context, "and it booted");
            found(bootstrap);
        }

        private static GameBootstrap FindBootstrap()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    var bootstrap = root.GetComponent<GameBootstrap>();
                    if (bootstrap != null && bootstrap.Context != null)
                    {
                        return bootstrap;
                    }
                }
            }

            return null;
        }

        /// <summary>Root objects in DontDestroyOnLoad (the test runner's own included), found through a probe.</summary>
        private static int PersistentRoots()
        {
            var probe = new GameObject("NewGameProbe");
            Object.DontDestroyOnLoad(probe);
            int count = probe.scene.rootCount - 1;
            Object.DestroyImmediate(probe);
            return count;
        }

        private static int CountBootstraps()
        {
            int count = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    count += root.GetComponentsInChildren<GameBootstrap>(true).Length;
                }
            }

            return count;
        }

        /// <summary>
        /// The playthrough's copy of Main on its own slot, listed in the build settings for the run so the bootstrap
        /// can reload it the way it reloads Main (the list is restored afterwards); the slot's files, put-away ones
        /// included, are removed before and after.
        /// </summary>
        public sealed class NewGameScene : IPrebuildSetup, IPostBuildCleanup
        {
            public static string[] PutAwayFiles()
            {
                string directory = SaveService.DefaultDirectory;
                return Directory.Exists(directory)
                    ? Directory.GetFiles(directory, PlaythroughScene.SaveSlot + SaveService.PutAwayMarker + "*")
                    : Array.Empty<string>();
            }

            public void Setup()
            {
#if UNITY_EDITOR
                DeletePutAway();
                new PlaythroughScene().Setup();
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                var listed = new EditorBuildSettingsScene[scenes.Length + 1];
                scenes.CopyTo(listed, 0);
                listed[scenes.Length] = new EditorBuildSettingsScene(PlaythroughScene.ScenePath, true);
                EditorBuildSettings.scenes = listed;
#endif
            }

            public void Cleanup()
            {
#if UNITY_EDITOR
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                int kept = 0;
                var restored = new EditorBuildSettingsScene[scenes.Length];
                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    if (scene.path != PlaythroughScene.ScenePath)
                    {
                        restored[kept++] = scene;
                    }
                }

                Array.Resize(ref restored, kept);
                EditorBuildSettings.scenes = restored;
                new PlaythroughScene().Cleanup();
                DeletePutAway();
#endif
            }

            private static void DeletePutAway()
            {
                foreach (string file in PutAwayFiles())
                {
                    File.Delete(file);
                }
            }
        }
    }
}
