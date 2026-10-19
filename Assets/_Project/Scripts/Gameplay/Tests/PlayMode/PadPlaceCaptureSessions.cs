using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The first pad's home and its take-down in the real Main scene (owner playtest, B5): with the build menu open
    /// beside the lander the ghost rides onto the marked apron and settles there (captured), and a finished pad taken
    /// down comes apart on screen (three frames captured) and pays back all it cost, once.
    /// </summary>
    public sealed class PadPlaceCaptureSessions : InputTestFixture
    {
        private const float StandBack = 5f;

        /// <summary>Where 07 approaches the apron from: the chase camera's normal distance.</summary>
        private const float ApproachBack = 9f;
        private const float SettleSeconds = 1f;
        private const float WakeTimeout = 120f;
        private static readonly float[] TakeDownMarks = { 0.33f, 0.66f, 0.97f };

        private Keyboard _keyboard;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private Scene _scene;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
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
        public IEnumerator TheGhostSettlesOnTheApron_AndATakenDownPadComesApart_PayingBackAllOfItOnce()
        {
            yield return Boot();
            BuildField building = _gameplay.Building;
            Assert.IsTrue(building.ApronCentre.HasValue, "the apron is marked");
            Vector3 apron = building.ApronCentre.Value;
            _gameplay.KnownStructures.Learn(StructureIds.ChargingPad);
            StructureDefinition pad = building.Catalog.Find(StructureIds.ChargingPad);
            _gameplay.Materials.Stow(pad.BuildCost);

            // The approach: 9 m back with the build menu open, the guide frames the bay and marks its centre.
            yield return StandFacing(Flat(apron) + Vector3.back * ApproachBack, apron);
            Assert.IsTrue(building.Controller.TryOpenMenu());
            yield return new WaitForSeconds(_gameplay.Building.Tuning.ApronGuideFadeSeconds + 0.5f);
            Assert.IsTrue(_gameplay.ApronGuide.IsShown, "the guide shows while the first pad is placed");
            Capture("apron-guide-9m-a");
            yield return new WaitForSeconds(_gameplay.Building.Tuning.ApronGuidePeriodSeconds * 0.5f);
            Capture("apron-guide-9m-b");
            building.Controller.Close();

            // Placing: 07 stands south of the apron facing it; the ghost rides onto the bay and is good there.
            Vector3 stand = Flat(apron) + Vector3.back * StandBack;
            yield return StandFacing(stand, apron);
            Assert.IsTrue(building.Controller.TryOpenMenu());
            Assert.IsTrue(building.Controller.PickStructure(StructureIds.ChargingPad));
            yield return new WaitForSeconds(SettleSeconds);
            Assert.AreEqual(BuildModeState.Placing, building.State);
            Assert.Less(Vector3.Distance(Flat(building.Controller.CandidatePose.Position), Flat(apron)), 0.3f,
                "the ghost settles on the marked apron");
            Assert.AreEqual(PlacementReason.Valid, building.Controller.CandidateReason, "and is good there");
            Capture("pad-ghost-on-the-apron");
            building.Controller.Close();
            yield return new WaitForSeconds(_gameplay.Building.Tuning.ApronGuideFadeSeconds + 0.3f);
            _gameplay.PlacedBase.PlaceUnchecked(pad, new StructurePose(apron, 0f), 0.5f);
            Assert.IsTrue(building.Controller.TryOpenMenu());
            yield return new WaitForSeconds(_gameplay.Building.Tuning.ApronGuideFadeSeconds + 0.3f);
            Assert.IsFalse(_gameplay.ApronGuide.IsShown, "once a pad is placed the guide is gone");
            building.Controller.Close();
            Assert.IsTrue(_gameplay.PlacedBase.TryDeconstruct(_gameplay.PlacedBase.StructureAt(0).InstanceId, out _));
            _gameplay.Materials.Stow(pad.BuildCost);

            // Taking down: a finished pad on the apron, the menu open, X held until it comes apart.
            var removed = new System.Collections.Generic.List<StructureRemoved>();
            using (_context.Events.Subscribe<StructureRemoved>(removed.Add))
            {
                int id = _gameplay.PlacedBase.PlaceUnchecked(pad, new StructurePose(apron, 0f), 1f);
                int metalBefore = _gameplay.Materials.Metal;
                int wiringBefore = _gameplay.Materials.Wiring;
                yield return StandFacing(stand, apron);
                Assert.IsTrue(building.Controller.TryOpenMenu());
                yield return Until(() => building.HoveredInstanceId == id, 5f, "the pad is in view to take down");
                Capture("pad-take-down-000");
                Press(_keyboard.xKey);
                foreach (float mark in TakeDownMarks)
                {
                    yield return Until(() => building.Controller.DeconstructProgress01 >= mark || removed.Count > 0, 4f,
                        "the held X fills the take-down");
                    if (removed.Count == 0)
                    {
                        Assert.Greater(building.TakeDownLevel, 0f, "the beam is on the piece while it is held");
                        Capture("pad-take-down-" + ((int)(mark * 100f)).ToString("000"));
                    }
                }

                yield return Until(() => removed.Count > 0, 4f, "a held X takes the pad down");
                Release(_keyboard.xKey);
                yield return new WaitForSeconds(0.3f);
                Assert.AreEqual(0f, building.TakeDownLevel, 1e-4f, "the beam lets go once it is gone");
                Assert.AreEqual(1, removed.Count, "the pad is taken down once");
                Assert.AreEqual(metalBefore + pad.BuildCost.Metal, _gameplay.Materials.Metal, "all of its Metal back");
                Assert.AreEqual(wiringBefore + pad.BuildCost.Wiring, _gameplay.Materials.Wiring, "all of its Wiring back");
                Assert.AreEqual(0, _gameplay.PlacedBase.Count);
            }
        }

        private IEnumerator StandFacing(Vector3 spot, Vector3 target)
        {
            Vector3 facing = Flat(target - spot).normalized;
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.LookRotation(facing, Vector3.up));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(facing, Vector3.up).eulerAngles.y);
            yield return new WaitForSeconds(SettleSeconds);
        }

        private void Capture(string name)
        {
            Camera lens = _context.Get<IViewCamera>().Camera;
            FrameCapture.SavePng(lens, 1280, 720, Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
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
            throw new NotSupportedException("The pad sessions load the scene through the editor.");
#endif
            _scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            GameBootstrap bootstrap = null;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                yield return Until(() => _awake, WakeTimeout, "07 wakes");
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), "timed out waiting for: " + what);
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }
    }
}
