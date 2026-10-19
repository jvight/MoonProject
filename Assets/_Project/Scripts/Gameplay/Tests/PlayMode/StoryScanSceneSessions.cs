using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The story scans in the real Main scene: every story scan in ScanTuning whose host stands in the scene is
    /// registered with the scan field, and the seismic station, the retroreflector and the flag are among them. A host
    /// or node name that drifts from World's props can no longer fail quietly (a host that is not in the scene is
    /// skipped by design; a host that is must show its scan).
    /// </summary>
    public sealed class StoryScanSceneSessions : InputTestFixture
    {
        private static readonly string[] Required =
        {
            "airlock", "scout3", "seismic", "retroreflector", "flag", "kestrel3", "elder06", "elder01",
        };

        private GameplaySystem _gameplay;
        private Scene _scene;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                foreach (GameObject root in _scene.GetRootGameObjects())
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
        public IEnumerator EveryStoryScanWhoseHostStands_IsRegistered_AndTheNewThreeAreThere()
        {
            yield return Boot();
            ScanField scans = _gameplay.Scans;
            int registered = 0;
            foreach (StoryScan story in scans.Tuning.Stories)
            {
                Transform host = FindHost(story.HostId);
                if (host == null)
                {
                    continue;
                }

                Assert.IsNotNull(scans.Find(story.FragmentId),
                    $"{story.FragmentId}: its host '{story.HostId}' stands in the scene, so its node '{story.Node}' " +
                    "must too (World's prop names and Art's node names)");
                registered++;
            }

            Assert.Greater(registered, 0, "the scene has story props");
            foreach (string id in Required)
            {
                Assert.IsNotNull(scans.Find(StoryScan.IdPrefix + id),
                    $"the story scan '{id}' is registered in the built Main scene");
            }
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The story scan sessions load the scene through the editor.");
#endif
            _scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            yield return null;
            yield return null;
            Assert.IsNotNull(_gameplay.Scans, "the gameplay system stood its scans up");
        }

        /// <summary>The scene transform named <paramref name="name"/> (a ruin piece or a basin prop), or null.</summary>
        private Transform FindHost(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == name)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }
    }
}
