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
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// The Lumen ruins in the real game (opening v2): boots the built Main scene on a new game, lets 07 wake, then
    /// checks that every ruin piece is solid only through Art's collision proxies and that every supply crate
    /// Gameplay set by the ruins and the lander leaves 07's body a free spot within collecting reach.
    /// </summary>
    [Explicit("Real-scene ruin collision session; run on demand with --category RuinReachSession.")]
    [Category("RuinReachSession")]
    public sealed class RuinReachSession
    {
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 2f;
        private const string RuinsRootName = "Ruins";
        private const string CratePrefix = "supply.";
        private const string ProxyPrefix = "UCX_";

        // 07's body (the scale bible): 1.47 m wide, 2.01 m long, 1.59 m high, lifted off the ground a little.
        private static readonly Vector3 RoverHalfExtents = new Vector3(0.735f, 0.795f, 1.005f);
        private const float GroundLift = 0.05f;

        // Gameplay's collecting reach (OpeningTuning, 3 m): 07 stands somewhere on these rings around a crate.
        private static readonly float[] StandRings = { 1.6f, 2.1f, 2.6f };
        private const int RingSteps = 24;

        private IDisposable _awoke;
        private bool _awake;

        [TearDown]
        public void TearDown()
        {
            _awoke?.Dispose();
            _awoke = null;
            _awake = false;
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator Ruins_AreSolidThroughTheirProxies_AndEveryCrateIsWithinReach()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The ruin reach session loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            var crates = new List<Transform>();
            Transform ruins = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            GameContext context = bootstrap.Context;
            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    ruins = ruins != null || node.name != RuinsRootName ? ruins : node;
                    if (node.name.StartsWith(CratePrefix, StringComparison.Ordinal))
                    {
                        crates.Add(node);
                    }
                }
            }

            Assert.IsNotNull(ruins, "World stood the ruins");
            Assert.That(ruins.childCount, Is.GreaterThan(0), "the ruins have pieces");
            foreach (Transform piece in ruins)
            {
                Collider[] colliders = piece.GetComponentsInChildren<Collider>(true);
                Assert.That(colliders.Length, Is.GreaterThan(0), $"{piece.name} is solid");
                foreach (Collider collider in colliders)
                {
                    Assert.IsTrue(collider.name.StartsWith(ProxyPrefix, StringComparison.Ordinal),
                        $"{piece.name}'s collider {collider.name} is not one of Art's proxies");
                }
            }

            Assert.That(crates.Count, Is.GreaterThan(0), "Gameplay set its supply crates");
            ITerrainQuery terrain = context.Get<ITerrainQuery>();
            var blocked = new List<string>();
            foreach (Transform crate in crates)
            {
                if (!HasStandingRoom(crate, terrain))
                {
                    blocked.Add($"{crate.name} at {crate.position}");
                }
            }

            Assert.IsEmpty(blocked, "07 finds no free spot within reach of these crates");
        }

        /// <summary>True when 07's body, facing <paramref name="crate"/>, fits somewhere on its stand rings.</summary>
        private static bool HasStandingRoom(Transform crate, ITerrainQuery terrain)
        {
            Vector3 at = crate.position;
            foreach (float ring in StandRings)
            {
                for (int step = 0; step < RingSteps; step++)
                {
                    float angle = step * Mathf.PI * 2f / RingSteps;
                    float x = at.x + Mathf.Cos(angle) * ring;
                    float z = at.z + Mathf.Sin(angle) * ring;
                    var centre = new Vector3(x, terrain.SampleHeight(x, z) + RoverHalfExtents.y + GroundLift, z);
                    Vector3 toCrate = at - centre;
                    toCrate.y = 0f;
                    Quaternion facing = Quaternion.LookRotation(toCrate.normalized, Vector3.up);
                    if (IsFree(centre, facing, crate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsFree(Vector3 centre, Quaternion facing, Transform crate)
        {
            foreach (Collider hit in Physics.OverlapBox(centre, RoverHalfExtents, facing, Layers.PropMask))
            {
                if (!hit.transform.IsChildOf(crate))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
