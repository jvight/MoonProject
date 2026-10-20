using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The antenna mast raised segment by segment, in the real Main scene (opening v3 3.3): a mast built live lifts
    /// base, middle, top, each locking with <see cref="MastSegmentRaised"/>, in order, once each, then the raise is
    /// done; and the call's reply has not been tried at a new game's start.
    /// </summary>
    [Category("MastRaise")]
    public sealed class MastRaiseSessions
    {
        private const float WakeTimeout = 20f;
        private const float ExtraSeconds = 1f;

        private readonly List<MastSegmentRaised> _raised = new List<MastSegmentRaised>();
        private readonly List<float> _raisedAt = new List<float>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private IDisposable _subscription;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            _raised.Clear();
            _raisedAt.Clear();
            _awake = false;
            _gameplay = null;
        }

        [TearDown]
        public void TearDown()
        {
            _subscription?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ABuiltMast_RaisesThreeSegmentsInOrder_ThenIsDone()
        {
            yield return Boot();
            Assert.IsFalse(_gameplay.Reply.Tried, "a new game has not tried its reply");
            Assert.IsFalse(_gameplay.MastRaise.Done, "and has no mast raised");

            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 at = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.AntennaMast),
                new StructurePose(at, 0f), 1f);
            float started = Time.time;
            float until = started + MastRaise.TotalSeconds(_gameplay.MastRaiseSettings) + ExtraSeconds;
            while (Time.time < until)
            {
                yield return null;
                while (_raisedAt.Count < _raised.Count)
                {
                    _raisedAt.Add(Time.time - started);
                }
            }

            Assert.AreEqual(3, _raised.Count, "one segment event each for base, middle and top");
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, _raised[i].Index, "in order");
                Assert.IsFalse(_raised[i].Restored);
            }

            Assert.Less(_raisedAt[0], _raisedAt[1]);
            Assert.Less(_raisedAt[1], _raisedAt[2]);
            Assert.IsTrue(_raised[2].IsLast);
            Assert.IsTrue(_gameplay.MastRaise.Done, "the raise is done after the settle");
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The mast raise sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            _subscription = _context.Events.Subscribe<MastSegmentRaised>(_raised.Add);
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
            _raised.Clear();
        }
    }
}
