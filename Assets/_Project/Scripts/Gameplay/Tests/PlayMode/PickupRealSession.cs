using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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
    /// Picking parts up with E in the real Main scene, as the owner plays it: Tilly's parts rest in her crashed sled
    /// (0.5.5 §2). 07 cuts its clamp and its sensor pod open, comes to rest beside each part at the open mouth and a
    /// tap of E draws it in; the one under the hull is dug free.
    /// </summary>
    [Category("Pickups")]
    public sealed class PickupRealSession : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float PickupTimeout = 6f;
        private const string TillyId = "tilly";

        /// <summary>07 stops this far (m) from a part, inside its reach, as a player would.</summary>
        private const float Beside = 2.5f;

        /// <summary>07 stands this far (m) in front of a seal to cut it.</summary>
        private const float SealStandOff = 1.4f;

        /// <summary>Seconds to cut a seal and let the lid swing open.</summary>
        private const float SealTimeout = 12f;

        private GameContext _context;
        private GameplaySystem _gameplay;
        private Keyboard _keyboard;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
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
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TillysParts_EachComeInWithATapOfE()
        {
            yield return Boot();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            int[] order = { 0, 2, 1 };
            Assert.AreEqual(1, tilly.BuriedPart, "her sled: clamp, hull, pod");
            foreach (int p in order)
            {
                int before = tilly.Progress.Collected;
                if (tilly.PartContainers[p] != null)
                {
                    yield return CutOpen(tilly.PartContainers[p], placement, terrain);
                }

                Vector3 part = tilly.PartRest[p];
                Vector3 from = Flat(part) + Vector3.back * Beside;
                from.y = terrain.SampleHeight(from.x, from.z);
                placement.PlaceAt(from, Quaternion.LookRotation(Flat(part) - Flat(from)));
                yield return new WaitForSeconds(0.5f);
                if (p == tilly.BuriedPart)
                {
                    Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Excavate, out _), "a Dig prompt");
                    Press(_keyboard.eKey);
                    yield return new WaitForSeconds(_gameplay.Friends.Tuning.PartDigSeconds + 0.3f);
                    Release(_keyboard.eKey);
                    yield return WaitCollected(tilly, before, "the hold that dug it free draws it in too");
                    continue;
                }

                Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.PickUp, out InteractionHint hint),
                    $"Tilly's part {p} offers its pickup prompt from {Beside} m (07 at " +
                    $"{_context.Get<IRoverState>().Position}, part at {part})");
                Assert.Less(Vector3.Distance(hint.Position, part), 1f, $"the prompt is on part {p}");
                Press(_keyboard.eKey);
                yield return null;
                yield return null;
                Release(_keyboard.eKey);
                yield return WaitCollected(tilly, before, $"a tap of E drew part {p} in");
            }
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator HoldingE_As07ComesUpToAPart_DrawsItIn()
        {
            yield return Boot();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            int p = tilly.BuriedPart == 0 ? 1 : 0;
            yield return CutOpen(tilly.PartContainers[p], placement, terrain);
            Vector3 part = tilly.PartRest[p];
            Vector3 far = Flat(part) + Vector3.back * 20f;
            far.y = terrain.SampleHeight(far.x, far.z);
            placement.PlaceAt(far, Quaternion.LookRotation(Flat(part) - Flat(far)));
            yield return new WaitForSeconds(0.5f);
            Press(_keyboard.eKey);
            yield return new WaitForSeconds(0.3f);
            Vector3 from = Flat(part) + Vector3.back * Beside;
            from.y = terrain.SampleHeight(from.x, from.z);
            placement.PlaceAt(from, Quaternion.LookRotation(Flat(part) - Flat(from)));
            float deadline = Time.time + PickupTimeout;
            while (tilly.Progress.Collected == 0 && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.AreEqual(1, tilly.Progress.Collected, "E is held, as for every other verb: the part comes in");
        }

        /// <summary>07 comes up in front of a seal on Tilly's sled, facing it, holds E to cut it and lets go.</summary>
        private IEnumerator CutOpen(SealedContainer box, IRoverPlacement placement, ITerrainQuery terrain)
        {
            Assert.IsNotNull(box, "a sealed part");
            Vector3 cut = box.CutPoint.position;
            Vector3 outward = Flat(box.CutPoint.forward).normalized;
            Vector3 from = Flat(cut) + outward * SealStandOff;
            from.y = terrain.SampleHeight(from.x, from.z);
            placement.PlaceAt(from, Quaternion.LookRotation(-outward));
            yield return new WaitForSeconds(0.5f);
            Press(_keyboard.eKey);
            float deadline = Time.time + SealTimeout;
            while (!box.Opened && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.IsTrue(box.Opened, $"held E cuts the seal of {box.Id}");
            while (!box.StandsOpen && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(box.StandsOpen, $"{box.Id} stands open");
            yield return new WaitForSeconds(_gameplay.Friends.Tuning.PartRiseSeconds);
        }

        private static IEnumerator WaitCollected(Friend tilly, int before, string what)
        {
            float deadline = Time.time + PickupTimeout;
            while (tilly.Progress.Collected == before && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(before + 1, tilly.Progress.Collected, what);
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
            throw new NotSupportedException("The pickup session loads the scene through the editor.");
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

        private static Vector3 Flat(Vector3 at)
        {
            return new Vector3(at.x, 0f, at.z);
        }
    }
}
