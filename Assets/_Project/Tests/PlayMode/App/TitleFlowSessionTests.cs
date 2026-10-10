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
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Tests.PlayMode.App
{
    /// <summary>
    /// The built Main scene boots into the title (M3-09) for the game, and straight into play during a test run, so
    /// fixtures that drive 07 from the first frame keep working (MoonProject.Editor.Automation.TestRunTitleSkip).
    /// </summary>
    public sealed class TitleFlowSessionTests
    {
        // A save with no sections: enough for the title to offer Continue.
        private const string EmptySave =
            "{\"formatVersion\":1,\"contentVersion\":\"" + SaveService.OldestCompatibleContent + "\"," +
            "\"savedAtUtc\":\"2026-10-11T00:00:00Z\",\"sections\":[]}";

        private readonly TitleFlowScene _scene = new TitleFlowScene();
        private IDisposable _started;
        private int _sessions;
        private bool _freshGame;

        [TearDown]
        public void TearDown()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            _started?.Dispose();
            _started = null;
            _sessions = 0;
            _freshGame = false;
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, true);
#endif
            Scene scene = SceneManager.GetSceneByPath(_scene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(_scene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(TitleFlowScene))]
        [PostBuildCleanup(typeof(TitleFlowScene))]
        public IEnumerator MainScene_BootsIntoTheTitle_AndContinueStartsPlay()
        {
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, false);
#endif
            SeedSave();
            GameBootstrap bootstrap = null;
            yield return Load(found => bootstrap = found);
            IGameFlow flow = bootstrap.Context.Get<IGameFlow>();
            Assert.IsTrue(flow.ShowsTitle, "the scene builder turns the title on");
            Assert.AreEqual(GameFlowState.Title, flow.State, "the game boots into the title");
            Assert.IsTrue(flow.HasSave, "the title offers Continue over the save");
            yield return null;
            Assert.AreEqual(0, _sessions, "no session starts while the title is up");

            bootstrap.Context.Events.Publish(new ContinueRequested());
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, flow.State, "Continue starts play");
            Assert.AreEqual(1, _sessions, "Continue starts one session");
            Assert.IsFalse(_freshGame, "Continue resumes the save");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(TitleFlowScene))]
        [PostBuildCleanup(typeof(TitleFlowScene))]
        public IEnumerator MainScene_InATestRun_BootsStraightIntoPlay()
        {
#if UNITY_EDITOR
            Assert.IsTrue(SessionState.GetBool(GameBootstrap.TestRunSkipsTitleKey, false),
                "the test-run hook marks the run");
#endif
            GameBootstrap bootstrap = null;
            yield return Load(found => bootstrap = found);
            IGameFlow flow = bootstrap.Context.Get<IGameFlow>();
            Assert.AreEqual(GameFlowState.Playing, flow.State, "a test run boots straight into play");
            Assert.IsFalse(flow.ShowsTitle, "with no title to return to");
            yield return null;
            Assert.AreEqual(1, _sessions, "the boot starts its session, as App does without the title");
            Assert.IsTrue(_freshGame, "on a fresh save");
        }

        private void SeedSave()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory, _scene.SaveSlot + SaveService.Extension),
                EmptySave);
        }

        /// <summary>
        /// Loads the copy and hands over its bootstrap. Counts SessionStarted from sceneLoaded, which runs after the
        /// bootstrap's Awake and before its Start, where the boot publishes it.
        /// </summary>
        private IEnumerator Load(Action<GameBootstrap> found)
        {
            SceneManager.sceneLoaded += OnLoaded;
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(_scene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }

            SceneManager.sceneLoaded -= OnLoaded;
#else
            throw new NotSupportedException("The title flow sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(_scene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_started, "the session counter subscribed before the bootstrap's Start");
            found(bootstrap);
        }

        private void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var bootstrap = root.GetComponent<GameBootstrap>();
                if (bootstrap != null && _started == null)
                {
                    _started = bootstrap.Context.Events.Subscribe<SessionStarted>(started =>
                    {
                        _sessions++;
                        _freshGame = started.FreshGame;
                    });
                }
            }
        }
    }
}
