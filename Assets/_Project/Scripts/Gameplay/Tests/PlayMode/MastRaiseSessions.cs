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

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheMastsSegments_StartLoweredInTheCabinet_LiftHomeInOrder_AndEndExactlyAsAuthored()
        {
            yield return Boot();
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 foot = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _context.Get<IRoverPlacement>().PlaceAt(foot + new Vector3(0f, 0f, -9f), Quaternion.identity);
            int id = _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.AntennaMast),
                new StructurePose(foot, 0f), 1f);
            Assert.IsTrue(_gameplay.Building.TryGetView(id, out StructureView view));
            CallReplySettings settings = _gameplay.MastRaiseSettings;
            Assert.IsTrue(_gameplay.MastRaiseView.IsBound, "the raise has the segments");
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(settings.SegmentTravelMetres * (i + 1), _gameplay.MastRaiseView.Lowered(i), 0.01f,
                    $"segment {i + 1} starts telescoped down into the cabinet");
            }

            float started = Time.time;
            var lowered = new float[3];
            bool inOrder = true;
            float total = MastRaise.TotalSeconds(settings) + ExtraSeconds;
            int shot = 0;
            float[] shotAt =
            {
                0.2f, MastRaise.LockTime(settings, 0) + 0.2f, MastRaise.LockTime(settings, 1) - 0.8f,
                MastRaise.LockTime(settings, 2) + 0.15f, total,
            };
            while (Time.time - started < total)
            {
                yield return null;
                float t = Time.time - started;
                for (int i = 0; i < 3; i++)
                {
                    float now = _gameplay.MastRaiseView.Lowered(i);
                    inOrder &= now <= lowered[i] + settings.SettleMetres + 0.02f || t < 0.5f || lowered[i] == 0f;
                    lowered[i] = now;
                }

                if (shot < shotAt.Length && t >= shotAt[shot])
                {
                    Capture($"mast-raise-{shot}", foot);
                    shot++;
                }
            }

            Capture("mast-raise-done", foot);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(0f, _gameplay.MastRaiseView.Lowered(i), 0.002f, $"segment {i + 1} ends where Art authored it");
            }

            Assert.IsTrue(_gameplay.MastRaise.Done);
        }

        private void Capture(string name, Vector3 foot)
        {
            var host = new GameObject("MastLens");
            Camera camera = host.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 40f;
            host.transform.position = foot + new Vector3(2.2f, 1.9f, -5.2f);
            host.transform.rotation = Quaternion.LookRotation(foot + Vector3.up * 1.5f - host.transform.position);
            string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Logs",
                "gameplay-captures"));
            System.IO.Directory.CreateDirectory(folder);
            FrameCapture.SavePng(camera, 960, 720, System.IO.Path.Combine(folder, name + ".png"));
            Object.Destroy(host);
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
