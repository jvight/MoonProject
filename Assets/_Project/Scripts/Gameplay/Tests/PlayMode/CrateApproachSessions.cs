using System;
using System.Collections;
using System.Text;
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
    /// The checkpoint bot could not reach supply.workshop.a at B7 (2026-10-19): a sealed crate a player must cut has to
    /// stand where 07 can drive up to its seal. In the real Main scene, for every sealed supply crate: 07's body (a
    /// capsule of its width and height) sweeps clear of props from a line-up in front of the seal to where it stands
    /// to cut, the ground on the way is drivable and under the slope limit, the cutter's line from there to the seal is
    /// clear and the seal stands in the open (the bot could not cut supply.workshop.b inside the workshop's shell), and
    /// the seal lies far enough from every scan target that the cut prompt and the scan never claim the same stance.
    /// </summary>
    [Category("CrateApproach")]
    public sealed class CrateApproachSessions
    {
        // Rover 07's body (docs/design/scale.md: 1.47 m wide, 1.59 m tall) and the bot's approach to a seal
        // (OpeningBase: 1.6 m in front of it, lined up 3 m further out).
        private const float RoverRadius = 0.74f;
        private const float RoverHeight = 1.59f;
        private const float GroundClearance = 0.15f;
        private const float SealStandOff = 1.6f;
        private const float SealLineUp = 3f;

        // 07's cutter turret (docs/design/scale.md: CutterOrigin 0.73 m up, 0.72 m ahead of 07's middle): from where
        // 07 stands to cut, its beam must reach the seal with no other prop in the way, and the seal must not lie
        // inside another prop (a ruin's shell drawn over the crate).
        private const float CutterHeight = 0.73f;
        private const float CutterAhead = 0.72f;
        private const float SealClearRadius = 0.05f;

        // The line-up must open onto open ground: 07 comes to it from this many metres further out without a prop in
        // its way.
        private const float OpenGround = 5f;

        // The drivable floor's slope limit (MoonSurfaceTests), sampled this often along the approach.
        private const float MaxSlope = 20f;
        private const float SlopeStep = 0.5f;

        // The cut prompt claims within 3.5 m of 07 (CargoTuning) and the scan picks within the camera cone: a seal
        // this far from every scan target never shares a stance with it (basin dressing, 2026-10-19).
        private const float ScanClearance = 5f;

        private const float WakeTimeout = 30f;

        private GameplaySystem _gameplay;
        private GameContext _context;
        private bool _awake;

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
        [Timeout(120000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator EverySealedCrate_HasAClearDrivableApproach_AwayFromTheScans()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();
            var terrain = _context.Get<ITerrainQuery>();
            var problems = new StringBuilder();
            SupplyCaches caches = _gameplay.Opening.Caches;
            int checkedCrates = 0;
            for (int i = 0; i < caches.Count; i++)
            {
                SupplyCache cache = caches.Get(i);
                if (cache.StartsOpen)
                {
                    continue;
                }

                SealedContainer crate = _gameplay.Containers.Find(cache.Id);
                Vector3 seal = crate.CutPoint.position;
                Vector3 side = crate.SealSide(crate.Root.position);
                Vector3 stand = Flat(seal) + side * SealStandOff;
                Vector3 lineUp = stand + side * SealLineUp;
                Check(problems, cache.Id, seal, stand, lineUp, lineUp + side * OpenGround, crate.Root, terrain);
                checkedCrates++;
            }

            Assert.Greater(checkedCrates, 0, "the opening has sealed crates");
            Assert.IsEmpty(problems.ToString(), "sealed crates 07 cannot cut as a player would:\n" + problems);
        }

        private void Check(StringBuilder problems, string id, Vector3 seal, Vector3 stand, Vector3 lineUp,
            Vector3 open, Transform crate, ITerrainQuery terrain)
        {
            foreach (ScanFragment fragment in _gameplay.Scans.Fragments)
            {
                float apart = Flat(fragment.Point - seal).magnitude;
                if (apart < ScanClearance)
                {
                    problems.AppendLine($"{id}: its seal lies {apart:F1} m from scan target {fragment.FragmentId}");
                }
            }

            Vector3 from = OnGround(terrain, open);
            Vector3 to = OnGround(terrain, stand);
            Vector3 bottom = Vector3.up * (GroundClearance + RoverRadius);
            Vector3 top = Vector3.up * (RoverHeight - RoverRadius);
            Vector3 path = to - from;
            if (Physics.CheckCapsule(from + bottom, from + top, RoverRadius, Layers.PropMask,
                    QueryTriggerInteraction.Ignore))
            {
                problems.AppendLine($"{id}: 07 does not fit on the open ground {from} it comes in from");
            }
            else if (Physics.CapsuleCast(from + bottom, from + top, RoverRadius, path.normalized, out RaycastHit hit,
                         path.magnitude, Layers.PropMask, QueryTriggerInteraction.Ignore)
                     && !hit.collider.transform.IsChildOf(crate))
            {
                problems.AppendLine($"{id}: {hit.collider.name} stands between the open ground and the seal, " +
                                    $"{hit.distance:F1} m along");
            }

            Vector3 cutter = to + Flat(seal - to).normalized * CutterAhead + Vector3.up * CutterHeight;
            foreach (RaycastHit hit2 in Physics.RaycastAll(cutter, seal - cutter, Vector3.Distance(cutter, seal),
                         Layers.PropMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit2.collider.transform.IsChildOf(crate))
                {
                    problems.AppendLine($"{id}: {hit2.collider.name} blocks the cutter's line to the seal");
                }
            }

            foreach (Collider inside in Physics.OverlapSphere(seal, SealClearRadius, Layers.PropMask,
                         QueryTriggerInteraction.Ignore))
            {
                if (!inside.transform.IsChildOf(crate))
                {
                    problems.AppendLine($"{id}: its seal lies inside {inside.name}");
                }
            }

            for (float d = 0f; d <= path.magnitude + SealStandOff; d += SlopeStep)
            {
                Vector3 at = from + Flat(seal - from).normalized * d;
                float slope = Vector3.Angle(Vector3.up, terrain.SampleNormal(at.x, at.z));
                if (!terrain.IsDrivable(at.x, at.z) || slope > MaxSlope)
                {
                    problems.AppendLine($"{id}: the ground at ({at.x:F1}, {at.z:F1}) is not drivable " +
                                        $"({slope:F1} deg)");
                    break;
                }
            }

            Debug.Log($"[crate-approach] {id}: seal {seal}, stand {to}, line-up {lineUp}, open ground {from}");
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
            throw new NotSupportedException("The crate approach sessions load the scene through the editor.");
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
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }
            }

            Assert.IsTrue(_awake, "07 wakes");
        }

        private static Vector3 OnGround(ITerrainQuery terrain, Vector3 at)
        {
            return new Vector3(at.x, terrain.SampleHeight(at.x, at.z), at.z);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
