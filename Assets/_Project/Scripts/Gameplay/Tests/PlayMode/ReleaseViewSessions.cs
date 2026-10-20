using System;
using System.Collections;
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
    /// The release view (opening v3 R9, plan 4.4) in the real Main scene: from the rise by the mast, the three lures,
    /// relay.1's lamp, the Peak's dish and Kestrel-3's crater, lie in plain sight of each other's sky: no hill between
    /// the eye and any of them.
    /// </summary>
    [Category("ReleaseView")]
    public sealed class ReleaseViewSessions
    {
        private const float WakeTimeout = 20f;
        private const float EyeHeight = 1.4f;

        /// <summary>Metres at the summit where the ground itself may graze the line to the dish.</summary>
        private const float SummitGround = 15f;

        private GameContext _context;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            _awake = false;
        }

        [TearDown]
        public void TearDown()
        {
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
        public IEnumerator FromTheMast_TheThreeLures_AreInSight_AndReadAsSmallAnglesAcrossTheWest()
        {
            yield return Boot();
            var anchors = _context.Get<IWorldAnchors>();
            var layout = _context.Get<IWorldLayout>();
            Assert.IsTrue(anchors.TryGet("place.apron", out WorldAnchor apron));
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.RelayPrefix + "1", out WorldAnchor relay));
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.SitePrefix + "kestrel", out WorldAnchor kestrel));
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 eye = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z) + Vector3.up * EyeHeight;
            (string name, Vector3 point)[] lures =
            {
                ("relay.1's lamp", relay.Position + Vector3.up * 3.5f),
                ("the Peak's dish", layout.PeakPosition + Vector3.up * 6f),
                ("Kestrel-3's crater", kestrel.Position + Vector3.up * 1f),
            };
            foreach ((string name, Vector3 point) lure in lures)
            {
                Vector3 to = lure.point - eye;
                float distance = to.magnitude;
                bool blocked = Physics.Linecast(eye, lure.point, out RaycastHit hit);
                bool onlyItsOwnGround = blocked && hit.distance >= distance - SummitGround;
                Assert.IsTrue(!blocked || onlyItsOwnGround,
                    $"{lure.name} is hidden from the mast by {(blocked ? hit.collider.name : string.Empty)} at " +
                    $"{(blocked ? hit.distance : 0f):F0} m of {distance:F0} m");
                float flat = new Vector2(to.x, to.z).magnitude;
                float elevation = Mathf.Atan2(to.y, flat) * Mathf.Rad2Deg;
                Assert.Greater(elevation, -1f, $"{lure.name} does not sink below the horizon");
            }

            var host = new GameObject("RiseLens");
            Camera lens = host.AddComponent<Camera>();
            lens.enabled = false;
            lens.fieldOfView = 55f;
            lens.farClipPlane = 3000f;
            string[] shots = { "release-relay", "release-peak-dish", "release-kestrel" };
            string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Logs",
                "gameplay-captures"));
            System.IO.Directory.CreateDirectory(folder);
            for (int i = 0; i < lures.Length; i++)
            {
                host.transform.position = eye;
                host.transform.rotation = Quaternion.LookRotation(lures[i].point - eye, Vector3.up);
                FrameCapture.SavePng(lens, 1280, 720, System.IO.Path.Combine(folder, shots[i] + ".png"));
            }

            Object.Destroy(host);

            Vector3 relayTo = lures[0].point - eye;
            Vector3 peakTo = lures[1].point - eye;
            Vector3 kestrelTo = lures[2].point - eye;
            Assert.Greater(Mathf.Atan2(peakTo.y, new Vector2(peakTo.x, peakTo.z).magnitude) * Mathf.Rad2Deg, 5f,
                "the Peak rises well above the skyline");
            Assert.Less(new Vector2(kestrelTo.x, kestrelTo.z).magnitude, new Vector2(relayTo.x, relayTo.z).magnitude,
                "Kestrel-3 is the nearest of the three: the first place to drive to");
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
            throw new NotSupportedException("The release view sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            _context = bootstrap.Context;
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
        }
    }
}
