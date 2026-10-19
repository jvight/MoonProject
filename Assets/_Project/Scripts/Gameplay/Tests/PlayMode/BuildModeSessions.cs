using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// End-to-end PlayMode session in the real Main scene for 07's freeform base building (0.5.7 slice 6, BUILD-MODE):
    /// 1. Starts a fresh game (knows only the charging pad).
    /// 2. Verifies the charging pad (<see cref="StructureIds.ChargingPad"/>) is unlocked.
    /// 3. Places it on flat ground in front of 07.
    /// 4. Welds it to completion (all 4 stages solid, standby lamps lit).
    /// 5. Saves, reloads the Main scene, and verifies both <see cref="KnownStructures"/> and the finished charging pad
    ///    are restored in the world.
    /// 6. Deconstructs the restored charging pad by holding <c>Deconstruct</c> (X) and verifies the tuned material
    ///    refund is returned to 07's stock.
    /// </summary>
    [Category("BuildMode")]
    public sealed class BuildModeSessions : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float StepWaitSeconds = 0.25f;
        private const float ClearSpotEastOffset = 18f;
        private const float ClearSpotNorthOffset = 14f;
        private const float PlaceAheadDistance = 4.6f;

        /// <summary>Seconds W is held while placing: 07 drives the ghost a little way.</summary>
        private const float RideSeconds = 0.8f;

        /// <summary>Metres the riding ghost may stand off the spot ahead of 07 (07 eases to a stop).</summary>
        private const float RideTolerance = 0.8f;

        /// <summary>Metres short of the apron's centre 07's ghost rides when it is set down facing the apron.</summary>
        private const float ApronShortfall = 1f;

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
            UnloadPlaythroughScene();
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator LearnChargingPad_PlaceInFrontOf07_WeldToDone_SaveReloadRestore_AndDeconstructForRefund()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return BootScene(waitWake: true);

            BuildField build = _gameplay.Building;
            Assert.IsNotNull(build, "Main scene GameplaySystem must have BuildField wired.");
            Assert.AreEqual(0, _gameplay.KnownStructures.Count, "A new game knows no structure (opening-v2 B3).");
            Assert.IsTrue(_gameplay.KnownStructures.Learn(StructureIds.ChargingPad),
                "the pad, as the lander's cargo scan teaches it");
            Assert.AreEqual(BuildModeState.Closed, build.State);

            // Place 07 on flat ground near home, clear of any lander props.
            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 basePos = _context.Get<IWorldLayout>().BasePosition;
            Vector3 roverSpot = new Vector3(
                basePos.x + ClearSpotEastOffset,
                0f,
                basePos.z + ClearSpotNorthOffset);
            roverSpot.y = terrain.SampleHeight(roverSpot.x, roverSpot.z);
            placement.PlaceAt(roverSpot, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return new WaitForSeconds(StepWaitSeconds);

            Vector3 roverPos = _context.Get<IRoverState>().Position;
            Vector3 aimSpot = roverPos + Vector3.forward * PlaceAheadDistance;
            aimSpot.y = terrain.SampleHeight(aimSpot.x, aimSpot.z);
            build.SetAimPointOverride(aimSpot);

            // 1. Verify the charging pad is known and stow materials to afford it.
            Assert.IsTrue(_gameplay.KnownStructures.IsKnown(StructureIds.ChargingPad));
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition padDef));

            Recipe padCost = padDef.BuildCost;
            _gameplay.Materials.Stow(padCost);
            int metalBeforePlace = _gameplay.Materials.Metal;
            int wiringBeforePlace = _gameplay.Materials.Wiring;

            // A screen that hands over to build mode (the face screen's Build) opens it as B would, and the apron's
            // bay outline warms while it is open and cools once it closes.
            InputReader input = _context.Input;
            float fade = build.Tuning.ApronGlowFadeSeconds + StepWaitSeconds;
            Assert.IsTrue(build.TryOpen(), "TryOpen opens the build menu like the Build key.");
            Assert.AreEqual(BuildModeState.Menu, build.State);
            Assert.IsTrue(input.HasModeOwner, "Build mode opened by TryOpen claims input like the Build key.");
            Assert.IsFalse(build.TryOpen(), "TryOpen does nothing while build mode is already open.");
            yield return new WaitForSeconds(fade);
            Assert.AreEqual(1f, _gameplay.ApronGlow.Glow, 1e-3f, "The apron's outline glows while build mode is open.");
            Assert.IsFalse(build.TryPick(build.Controller.AvailableStructures.Count), "no pick past the menu's end");
            Assert.IsTrue(build.TryPick(0), "TryPick picks a menu structure as E on it would");
            Assert.AreEqual(BuildModeState.Placing, build.State);
            Assert.IsFalse(build.TryPick(0), "TryPick only picks from the menu");
            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Closed, build.State, "B closes build mode TryOpen opened.");
            yield return new WaitForSeconds(fade);
            Assert.AreEqual(0f, _gameplay.ApronGlow.Glow, 1e-3f, "The apron's outline cools once build mode closes.");

            // 2. Open the build menu (B), verify input isolation (ENV-02), select charging pad (E), and tap E to place (ENV-01).
            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State, "Build key opens the build menu.");
            Assert.IsNotNull(build.Controller.SelectedStructure);
            Assert.AreEqual(StructureIds.ChargingPad, build.Controller.SelectedStructure.Id);
            Assert.IsTrue(input.HasModeOwner, "Build mode claims exclusive input ownership while open.");

            // ENV-02: Holding W (Drive) while Build Mode is open must not drive 07.
            Vector3 posBeforeDrive = _context.Get<IRoverState>().Position;
            Press(_keyboard.wKey);
            yield return null;
            yield return null;
            Assert.AreEqual(Vector2.zero, input.Drive, "InputReader.Drive is consumed while Build Mode is open.");
            Assert.Less(
                Vector3.Distance(_context.Get<IRoverState>().Position, posBeforeDrive),
                0.02f,
                "07 must not drive while Build Mode is open.");
            Release(_keyboard.wKey);
            yield return null;

            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Placing, build.State, "Excavate (E) enters Placing state.");
            yield return null;
            Assert.IsNotNull(build.GhostView, "Placing state activates the hologram ghost view.");
            Assert.AreEqual(PlacementReason.Valid, build.Controller.CandidateReason);
            Assert.IsTrue(build.Controller.CanPlaceNow);

            // Placing shares Drive: 07 drives the ghost, which rides ahead of it, and the charging pad's ghost settles
            // onto the apron's bay as it nears it.
            build.SetAimPointOverride(null);
            var rover = _context.Get<IRoverState>();
            Vector3 beforeRide = rover.Position;
            Press(_keyboard.wKey);
            yield return new WaitForSeconds(RideSeconds);
            Assert.AreNotEqual(Vector2.zero, input.Drive, "Drive reaches 07 while placing.");
            Assert.IsTrue(input.HasModeOwner, "Build mode still owns E while placing.");
            Release(_keyboard.wKey);
            yield return new WaitForSeconds(RideSeconds);
            Assert.Greater(Vector3.Distance(rover.Position, beforeRide), 0.1f, "07 drives while placing.");
            Vector3 heading = Flat(rover.Rotation * Vector3.forward).normalized;
            Vector3 ahead = rover.Position + heading * build.Tuning.GhostAheadMetres;
            Assert.Less(Vector3.Distance(Flat(build.Controller.CandidatePose.Position), Flat(ahead)), RideTolerance,
                "The placing ghost rides in front of 07.");

            Assert.IsTrue(build.ApronCentre.HasValue, "The apron's bay is marked for the pad's ghost.");
            Vector3 apron = build.ApronCentre.Value;
            Vector3 approach = Flat(apron - roverSpot).normalized;
            Vector3 shortOf = apron - approach * (build.Tuning.GhostAheadMetres + ApronShortfall);
            shortOf.y = terrain.SampleHeight(shortOf.x, shortOf.z);
            placement.PlaceAt(shortOf, Quaternion.LookRotation(approach, Vector3.up));
            yield return new WaitForSeconds(StepWaitSeconds);
            Assert.Less(Vector3.Distance(Flat(build.Controller.CandidatePose.Position), Flat(apron)), 0.05f,
                "Riding within reach of the apron, the pad's ghost settles on its bay.");

            placement.PlaceAt(roverSpot, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            build.SetAimPointOverride(aimSpot);
            yield return new WaitForSeconds(StepWaitSeconds);
            Assert.IsTrue(build.Controller.CanPlaceNow, "Back on clear ground, the ghost can be placed.");

            // ENV-01: Tapping E places the structure and stays in Welding state on the next frame (never cancels at 0%).
            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Welding, build.State, "Tapping E to place stays in Welding state awaiting Hold E.");
            Assert.AreEqual(1, _gameplay.PlacedBase.Count, "Charging pad is placed in PlacedBase.");
            Assert.AreEqual(metalBeforePlace - padCost.Metal, _gameplay.Materials.Metal, "Metal cost spent on place.");
            Assert.AreEqual(wiringBeforePlace - padCost.Wiring, _gameplay.Materials.Wiring, "Wiring cost spent on place.");

            int placedId = build.Controller.ActiveWeldInstanceId;
            Assert.Greater(placedId, 0);
            Assert.IsTrue(_gameplay.PlacedBase.TryGetStructure(placedId, out PlacedStructure placedFrame));
            Assert.IsFalse(placedFrame.IsBuilt);
            Assert.IsTrue(build.TryGetView(placedId, out StructureView placedView));
            Assert.AreEqual(4, placedView.StageCount, "Charging pad runtime prefab must have 4 wired stage roots.");
            Assert.AreEqual(0, placedView.SolidStageCount, "At 0% progress, 0 stages are solid and Stage_1 is hologram.");

            // Now hold E to weld the placed structure and verify VFX-05 (3-layer beam, warm light, sparks, progressive seam).
            Press(_keyboard.eKey);
            yield return null;
            yield return null;

            IRoverRig rig = _context.Get<IRoverRig>();
            Assert.IsNotNull(rig, "Main scene must provide IRoverRig.");
            Assert.IsNotNull(rig.CutterOrigin, "IRoverRig.CutterOrigin must be wired.");
            Assert.AreEqual(3, build.WeldBeamLayerCount, "Structure weld reuses the 3-layer CutBeam (core + sheath + halo).");
            Assert.Greater(build.WeldBeamLevel, 0f, "Weld beam extends during active welding.");
            Assert.Greater(build.WeldLightIntensity, 0f, "Warm flickering weld light illuminates the structure during welding.");
            Assert.Greater(build.WeldSeamHeat, 0f, "Progressive HeatSeam glows along the active weld stage.");
            Assert.Less(
                Vector3.Distance(build.WeldBeamStart, rig.CutterOrigin.position),
                0.01f,
                "Weld beam must originate at IRoverRig.CutterOrigin (chest cutter turret).");
            if (rig.TetherOrigin != null)
            {
                Assert.Greater(
                    Vector3.Distance(build.WeldBeamStart, rig.TetherOrigin.position),
                    0.05f,
                    "Weld beam start must not originate at IRoverRig.TetherOrigin (head).");
            }

            // 3. Keep holding E to weld the charging pad to completion.
            Vector3 placedPosition = placedFrame.Pose.Position;
            float weldDeadline = Time.time + padDef.BuildSeconds + 2.5f;
            while (Time.time < weldDeadline)
            {
                if (_gameplay.PlacedBase.TryGetStructure(placedId, out PlacedStructure current) && current.IsBuilt)
                {
                    break;
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return null;

            Assert.IsTrue(_gameplay.PlacedBase.TryGetStructure(placedId, out PlacedStructure finishedPad));
            Assert.IsTrue(finishedPad.IsBuilt, "Holding E welds the charging pad to 100% completion.");
            Assert.AreEqual(BuildModeState.Closed, build.State, "Completing weld returns BuildField to Closed state.");
            Assert.IsFalse(input.HasModeOwner, "Mode ownership releases once Build Mode closes.");
            Assert.AreEqual(placedView.StageCount, placedView.SolidStageCount, "All stages are solid after welding.");
            Assert.IsTrue(placedView.LampsLit, "Finished charging pad lights its standby amber lamps.");

            // 4. Save, reload the Main scene, and verify restoration.
            ISaveService save = _context.Get<ISaveService>();
            Assert.IsTrue(save.SaveNow(), "SaveNow writes the placed base to disk.");

            yield return BootScene(waitWake: false);
            yield return null;

            BuildField restoredBuild = _gameplay.Building;
            Assert.IsNotNull(restoredBuild);
            Assert.IsTrue(
                _gameplay.KnownStructures.IsKnown(StructureIds.ChargingPad),
                "Reloaded save restores charging_pad in KnownStructures.");
            Assert.AreEqual(1, _gameplay.PlacedBase.Count, "Reloaded save restores the placed charging pad.");

            PlacedStructure restoredPad = _gameplay.PlacedBase.StructureAt(0);
            Assert.AreEqual(StructureIds.ChargingPad, restoredPad.Definition.Id);
            Assert.IsTrue(restoredPad.IsBuilt, "Restored charging pad is built.");
            Assert.Less(Vector3.Distance(restoredPad.Pose.Position, placedPosition), 0.05f);
            Assert.IsTrue(restoredBuild.TryGetView(restoredPad.InstanceId, out StructureView restoredView));
            Assert.IsNotNull(restoredView);
            Assert.AreEqual(restoredView.StageCount, restoredView.SolidStageCount);
            Assert.IsTrue(restoredView.LampsLit);

            // 5. Aim 07 at the restored charging pad and hold X (Deconstruct) to deconstruct it and receive the refund.
            var restoredPlacement = _context.Get<IRoverPlacement>();
            var restoredTerrain = _context.Get<ITerrainQuery>();
            Vector3 standBack = restoredPad.Pose.Position - Vector3.forward * PlaceAheadDistance;
            standBack.y = restoredTerrain.SampleHeight(standBack.x, standBack.z);
            restoredPlacement.PlaceAt(standBack, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            Camera viewCam = _context.Get<IViewCamera>().Camera;
            if (viewCam != null)
            {
                Vector3 camEye = standBack - Vector3.forward * 2.5f + Vector3.up * 1.6f;
                Vector3 target = restoredPad.Pose.Position + Vector3.up * (padDef.Height * 0.45f);
                viewCam.transform.SetPositionAndRotation(camEye, Quaternion.LookRotation(target - camEye, Vector3.up));
            }

            yield return null;
            Assert.IsFalse(
                _gameplay.Hints.TryGet(InteractionKind.Deconstruct, out _),
                "No Deconstruct prompt while 07 merely stands by a built piece outside build mode.");
            Press(_keyboard.xKey);
            yield return new WaitForSeconds(BuildModeController.DefaultDeconstructHoldSeconds + 0.5f);
            Release(_keyboard.xKey);
            yield return null;
            Assert.AreEqual(1, _gameplay.PlacedBase.Count, "Holding X outside build mode deconstructs nothing.");
            Press(_keyboard.bKey);
            yield return null;
            Release(_keyboard.bKey);
            yield return null;
            Assert.AreEqual(BuildModeState.Menu, restoredBuild.State, "B opens the build menu: the builder is out.");
            Assert.AreEqual(
                restoredPad.InstanceId,
                restoredBuild.HoveredInstanceId,
                "07 hovers the restored charging pad within weld range.");
            Assert.IsTrue(
                _gameplay.Hints.TryGet(InteractionKind.Deconstruct, out _),
                "InteractionHints offers the Deconstruct prompt in build mode while looking at the placed structure.");

            int metalBeforeDeconstruct = _gameplay.Materials.Metal;
            int wiringBeforeDeconstruct = _gameplay.Materials.Wiring;
            Recipe expectedRefund = BuildJob.CalculateRefund(padCost, restoredBuild.Tuning.DeconstructRefundShare);

            Press(_keyboard.xKey);
            float deconstructDeadline = Time.time + BuildModeController.DefaultDeconstructHoldSeconds + 1.5f;
            while (_gameplay.PlacedBase.Count > 0 && Time.time < deconstructDeadline)
            {
                if (viewCam != null)
                {
                    Vector3 camEye = standBack - Vector3.forward * 2.5f + Vector3.up * 1.6f;
                    Vector3 target = restoredPad.Pose.Position + Vector3.up * (padDef.Height * 0.45f);
                    viewCam.transform.SetPositionAndRotation(camEye, Quaternion.LookRotation(target - camEye, Vector3.up));
                }

                yield return null;
            }

            Release(_keyboard.xKey);
            yield return null;

            Assert.AreEqual(0, _gameplay.PlacedBase.Count, "Holding Deconstruct removes the structure.");
            Assert.IsFalse(restoredBuild.TryGetView(restoredPad.InstanceId, out _), "Deconstructed view is destroyed.");
            Assert.AreEqual(
                metalBeforeDeconstruct + expectedRefund.Metal,
                _gameplay.Materials.Metal,
                "Deconstructing a finished structure refunds the tuned Metal share.");
            Assert.AreEqual(
                wiringBeforeDeconstruct + expectedRefund.Wiring,
                _gameplay.Materials.Wiring,
                "Deconstructing a finished structure refunds the tuned Wiring share.");
        }

        private IEnumerator BootScene(bool waitWake)
        {
            _context = null;
            _gameplay = null;
            _awake = false;
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("BuildModeSessions loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "Playthrough scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Playthrough scene has GameplaySystem.");
            _context = bootstrap.Context;

            if (waitWake)
            {
                using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
                {
                    float deadline = Time.time + WakeTimeout;
                    while (!_awake && Time.time < deadline)
                    {
                        yield return null;
                    }
                }

                Assert.IsTrue(_awake, "07 wakes up.");
            }
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        private IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static void UnloadPlaythroughScene()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }
        }
    }
}
