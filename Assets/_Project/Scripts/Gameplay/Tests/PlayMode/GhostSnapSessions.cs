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
    /// PlayMode verification and before/after capture session for build-mode ghost grounding, marked-apron snap, and
    /// chase-camera framing (GD feel audit #5, B4 pad and B7/B8 solar array):
    /// <list type="bullet">
    /// <item>While placing (<see cref="BuildModeState.Placing"/>), the ghost sits on the ground every frame and never
    /// floats above terrain or apron.</item>
    /// <item>When the marked apron bay is within reach, the charging pad ghost snaps onto it (~0.15 s ease) and reads
    /// clearly valid; looking or moving away un-snaps it.</item>
    /// <item>On entering <see cref="BuildModeState.Placing"/>, the chase camera eases calmly (&lt;= 20 deg/s, modest
    /// pitch) to frame 07 and the ghost/apron together, follows the ghost, and eases back when leaving build mode.</item>
    /// </list>
    /// </summary>
    public sealed class GhostSnapSessions : InputTestFixture
    {
        private const float WakeTimeout = 120f;
        private const float SettleBeforeSeconds = 0.6f;
        private const float SnapVerifySeconds = 0.35f;
        private const float CameraFrameSeconds = 3.5f;
        private const float OffAxisPadBearing = -65f;
        private const float OffAxisArrayBearing = -55f;
        private const float LookAwayBearing = 85f;
        private const float ArrayWestSpacing = 9f;
        private const float GroundHeightTolerance = 0.02f;
        private const float ApronSnapTolerance = 0.08f;
        private const float ViewportMinX = 0.22f;
        private const float ViewportMaxX = 0.78f;
        private const float ViewportMinY = 0.14f;
        private const float ViewportMaxY = 0.78f;
        private const int Width16x9 = 1280;
        private const int Height16x9 = 720;
        private const int Width21x9 = 1680;
        private const int Height21x9 = 720;

        private static readonly string CapturesDir =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private GameContext _context;
        private GameplaySystem _gameplay;
        private Scene _scene;
        private bool _awake;

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
        public IEnumerator GhostStaysOnGround_SnapsOnMarkedApron_AndCameraFramesBothForPadAndArray()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();

            BuildField build = _gameplay.Building;
            var terrain = _context.Get<ITerrainQuery>();
            var rover = _context.Get<IRoverState>();
            var placement = _context.Get<IRoverPlacement>();
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            Camera lens = rig.Camera;

            Assert.IsTrue(build.ApronCentre.HasValue, "Marked apron bay exists in Main scene.");
            Vector3 apron = build.ApronCentre.Value;

            _gameplay.KnownStructures.Learn(StructureIds.ChargingPad);
            _gameplay.KnownStructures.Learn(StructureIds.SolarArray);
            StructureDefinition padDef = build.Catalog.Find(StructureIds.ChargingPad);
            StructureDefinition arrayDef = build.Catalog.Find(StructureIds.SolarArray);
            _gameplay.Materials.Stow(padDef.BuildCost);
            _gameplay.Materials.Stow(arrayDef.BuildCost);

            // --- 1. B4 Charging Pad on the Marked Apron ---
            Vector3 padStand = Flat(apron) + Vector3.back * build.Tuning.GhostAheadMetres;
            padStand.y = terrain.SampleHeight(padStand.x, padStand.z);
            placement.PlaceAt(padStand, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            rig.Orbit.AdoptBearing(OffAxisPadBearing);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            // Capture BEFORE state (off-axis camera, un-snapped floating ghost along old off-axis ray).
            Vector3 legacyPadGhost = padStand + Quaternion.Euler(0f, OffAxisPadBearing, 0f) * Vector3.forward *
                build.Tuning.MaxPlacementDistance + Vector3.up * 0.9f;
            build.SetAimPointOverride(legacyPadGhost);
            Assert.IsTrue(build.Controller.TryOpenMenu());
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.ChargingPad));
            rig.BuildFraming.Reset();
            rig.Orbit.AdoptBearing(OffAxisPadBearing);
            yield return null;
            if (build.GhostView != null)
            {
                build.GhostView.transform.position = legacyPadGhost;
            }

            CapturePair(lens, "ghostsnap_pad", "before");
            build.Close();
            build.SetAimPointOverride(null);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            // Enter Placing with the real grounded apron-snap and build-mode camera framing.
            placement.PlaceAt(padStand, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            rig.Orbit.AdoptBearing(OffAxisPadBearing);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            Assert.IsTrue(build.TryOpen(), "Open build menu at B4 stand.");
            Assert.IsTrue(build.TryPick(0), "Pick charging pad to enter Placing.");
            Assert.AreEqual(BuildModeState.Placing, build.State);

            float startTime = Time.time;
            bool checkedSnap = false;
            while (Time.time - startTime < CameraFrameSeconds)
            {
                yield return null;
                AssertGhostGrounded(build, terrain);

                if (!checkedSnap && Time.time - startTime >= SnapVerifySeconds)
                {
                    checkedSnap = true;
                    Assert.IsTrue(build.Controller.CandidateSnapped, "Pad ghost snaps onto the marked apron in reach.");
                    Assert.AreEqual(PlacementReason.Valid, build.Controller.CandidateReason,
                        "Snapped pad ghost on the marked apron reads clearly valid.");
                    Assert.Less(
                        Vector3.Distance(Flat(build.Controller.CandidatePose.Position), Flat(apron)),
                        ApronSnapTolerance,
                        "Snapped pad ghost sits on the marked apron centre.");
                }
            }

            AssertBothFramed(lens, rover.Position + Vector3.up * 0.7f, apron + Vector3.up * 0.2f, "pad + apron");
            CapturePair(lens, "ghostsnap_pad", "after");

            // Turning the view away from the apron un-snaps the ghost while keeping it grounded.
            rig.Orbit.AdoptBearing(LookAwayBearing);
            float lookAwayStart = Time.time;
            while (Time.time - lookAwayStart < 0.6f)
            {
                yield return null;
                AssertGhostGrounded(build, terrain);
            }

            Assert.IsFalse(build.Controller.CandidateSnapped, "Looking away from the apron un-snaps the ghost.");
            Assert.Greater(
                Vector3.Distance(Flat(build.Controller.CandidatePose.Position), Flat(apron)),
                1f,
                "Un-snapped ghost leaves the apron bay.");

            // Leaving build mode ends the build framing.
            build.Close();
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(rig.BuildFraming.IsActive, "Leaving build mode ends the build framing.");

            // --- 2. B7/B8 Solar Array West of the Pad ---
            Vector3 arraySpot = Flat(apron) + Vector3.left * ArrayWestSpacing;
            arraySpot.y = terrain.SampleHeight(arraySpot.x, arraySpot.z);
            Vector3 arrayStand = Flat(arraySpot) + Vector3.back * build.Tuning.GhostAheadMetres;
            arrayStand.y = terrain.SampleHeight(arrayStand.x, arrayStand.z);

            placement.PlaceAt(arrayStand, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            rig.Orbit.AdoptBearing(OffAxisArrayBearing);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            // Capture BEFORE state for the solar array.
            Vector3 legacyArrayGhost = arrayStand + Quaternion.Euler(0f, OffAxisArrayBearing, 0f) * Vector3.forward *
                build.Tuning.MaxPlacementDistance + Vector3.up * 0.9f;
            build.SetAimPointOverride(legacyArrayGhost);
            Assert.IsTrue(build.Controller.TryOpenMenu());
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.SolarArray));
            rig.BuildFraming.Reset();
            rig.Orbit.AdoptBearing(OffAxisArrayBearing);
            yield return null;
            if (build.GhostView != null)
            {
                build.GhostView.transform.position = legacyArrayGhost;
            }

            CapturePair(lens, "ghostsnap_array", "before");
            build.Close();
            build.SetAimPointOverride(null);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            // Enter Placing for the solar array with real grounding and build framing.
            placement.PlaceAt(arrayStand, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            rig.Orbit.AdoptBearing(OffAxisArrayBearing);
            yield return new WaitForSeconds(SettleBeforeSeconds);

            Assert.IsTrue(build.Controller.TryOpenMenu());
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.SolarArray));
            Assert.AreEqual(BuildModeState.Placing, build.State);

            float arrayStart = Time.time;
            while (Time.time - arrayStart < CameraFrameSeconds)
            {
                yield return null;
                AssertGhostGrounded(build, terrain);
            }

            Vector3 arrayGhostPos = build.Controller.CandidatePose.Position;
            AssertBothFramed(lens, rover.Position + Vector3.up * 0.7f, arrayGhostPos + Vector3.up * 0.4f, "07 + solar array ghost");
            CapturePair(lens, "ghostsnap_array", "after");

            build.Close();
        }

        private static void AssertGhostGrounded(BuildField build, ITerrainQuery terrain)
        {
            Assert.IsNotNull(build.GhostView, "GhostView is active while Placing.");
            Vector3 ghostPos = build.GhostView.transform.position;
            float groundY = terrain.SampleHeight(ghostPos.x, ghostPos.z);
            Assert.AreEqual(groundY, ghostPos.y, GroundHeightTolerance,
                "Placing ghost sits on the ground and never floats above it.");

            Vector3 candidatePos = build.Controller.CandidatePose.Position;
            float candidateGroundY = terrain.SampleHeight(candidatePos.x, candidatePos.z);
            Assert.AreEqual(candidateGroundY, candidatePos.y, GroundHeightTolerance,
                "Candidate pose Y sits on the ground.");
        }

        private static void AssertBothFramed(Camera lens, Vector3 roverPoint, Vector3 ghostPoint, string label)
        {
            Vector3 roverVp = lens.WorldToViewportPoint(roverPoint);
            Vector3 ghostVp = lens.WorldToViewportPoint(ghostPoint);

            Assert.Greater(roverVp.z, 0f, $"07 is in front of the camera ({label}).");
            Assert.Greater(ghostVp.z, 0f, $"Ghost is in front of the camera ({label}).");
            Assert.That(roverVp.x, Is.InRange(ViewportMinX, ViewportMaxX), $"07 X is framed ({label}).");
            Assert.That(roverVp.y, Is.InRange(ViewportMinY, ViewportMaxY), $"07 Y is framed ({label}).");
            Assert.That(ghostVp.x, Is.InRange(ViewportMinX, ViewportMaxX), $"Ghost X is framed ({label}).");
            Assert.That(ghostVp.y, Is.InRange(ViewportMinY, ViewportMaxY), $"Ghost Y is framed ({label}).");
        }

        private static void CapturePair(Camera lens, string prefix, string phase)
        {
            Directory.CreateDirectory(CapturesDir);
            FrameCapture.SavePng(lens, Width16x9, Height16x9,
                Path.Combine(CapturesDir, $"{prefix}_16x9_{phase}.png"));
            FrameCapture.SavePng(lens, Width21x9, Height21x9,
                Path.Combine(CapturesDir, $"{prefix}_21x9_{phase}.png"));
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
            throw new NotSupportedException("GhostSnapSessions loads the scene through the editor.");
#endif
            _scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            GameBootstrap bootstrap = null;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "Main scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Main scene has GameplaySystem.");
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes.");
            }
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }
    }
}
