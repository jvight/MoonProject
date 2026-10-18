using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The owner saw the scan and the weld beams land in the air (2026-10-18). In the real Main scene, through whole
    /// scans of the lander's pad fragment, a fallen-array fragment seen at an angle and the habitat's modules, and a
    /// whole weld of the charging pad, every frame the beam is fully out its end must lie on the target: within
    /// <see cref="OnSurface"/> of the scan target's colliders (the box of its mesh where it has none) or of the pad's
    /// own part colliders, the weld's riding the printed layer line. A miss fails with the frame, the distance and
    /// the target. Each is captured from the beam's side in the eclipse and at golden hour
    /// (Logs/gameplay-captures/beam-*-eclipse.png, -golden.png), and each scan from the game's own chase camera at a
    /// quarter, half and three quarters through, so its beam and wireframe can be judged as the player sees them
    /// (scan-chase-*-25/50/75-eclipse.png, -golden.png).
    /// </summary>
    [Category("BeamAccuracy")]
    public sealed class BeamAccuracySessions : InputTestFixture
    {
        /// <summary>Metres a beam's end may lie off its target's surface.</summary>
        private const float OnSurface = 0.03f;

        /// <summary>A scan beam this far up (0..1) counts as fully out: its end is where it meets the target.</summary>
        private const float FullBeam = 0.99f;

        /// <summary>Metres the weld beam's end may lie off the printed layer line it rides.</summary>
        private const float LayerTolerance = 0.06f;

        /// <summary>Metres thick the slab is that asks whether a part stands at the layer line.</summary>
        private const float SlabMetres = 0.02f;

        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 0.6f;
        private const float CandidateTimeout = 4f;
        private const float ScanTimeout = 30f;
        private const float WeldTimeout = 40f;

        /// <summary>Metres from a scan target 07 stands to scan it.</summary>
        private const float ScanStandOff = 3f;

        /// <summary>Degrees round a target a stand spot is tried at until the ground there is clear.</summary>
        private const float SpotStep = 15f;

        private const float ClearHeight = 0.9f;
        private const float ClearRadius = 0.9f;
        private const float PlaceAhead = 4.6f;
        private const float StructureSpacing = 14f;

        /// <summary>Metres in front of 07 a structure is welded in the framing test: right at its nose.</summary>
        private const float WeldAhead = 3f;
        private const int MaxMenuSteps = 8;
        private const float FramingSettleSeconds = 2f;
        private const float ShotDelaySeconds = 0.2f;
        private const float CentralShare = 0.7f;
        private const int MissReportLength = 1500;

        /// <summary>Metres to the beam's side, above it and back toward 07 a capture looks across it from.</summary>
        private const float CaptureSideMetres = 3.2f;

        private const float CaptureRiseMetres = 1.4f;
        private const float CaptureBackMetres = 1.2f;

        /// <summary>The lunar clock's seconds searched for golden hour, and the step.</summary>
        private const float GoldenSearchFrom = 60f;

        private const float GoldenSearchTo = 2800f;
        private const float GoldenSearchStep = 10f;

        /// <summary>Golden hour: the target fully sunlit with the sun this low (its direction's height) or higher.
        /// </summary>
        private const float FullSun = 0.95f;

        private const float LowSun = 0.12f;

        /// <summary>Scan progress (0..1) at which the chase camera's view is captured.</summary>
        private static readonly float[] ChaseMarks = { 0.25f, 0.5f, 0.75f };

        private readonly Collider[] _near = new Collider[64];
        private Keyboard _keyboard;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private bool _awake;
        private string _atLine = string.Empty;
        private float _goldenSeconds;

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
        [Timeout(400000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheScanAndWeldBeams_EndOnTheirTargetsSurface_EveryFrame()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();
            _gameplay.StartKit.Restore(new StartKitSaveData { igniterFitted = true });
            _goldenSeconds = GoldenHour(_context.Get<IWorldLayout>().BasePosition);

            yield return ScanWhole("ruin." + ScanTuning.LanderCargoId, 0, 0f, "pad-fragment");
            yield return ScanWhole("ruin.FallenSolarArray", 1, 50f, "array-at-an-angle");
            yield return ScanWhole("ruin.HabitatModules", 0, -30f, "habitat");
            yield return WeldThePad();
        }

        [UnityTest]
        [Timeout(400000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator WeldingEachStructure_TheChaseCameraSeesTheLayerLine_NotJust07sBack()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();
            _gameplay.StartKit.Restore(new StartKitSaveData { igniterFitted = true });
            string[] structures =
            {
                StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank, StructureIds.AntennaMast,
            };
            for (int i = 0; i < structures.Length; i++)
            {
                yield return WeldInView(structures[i], i);
            }
        }

        /// <summary>
        /// 07 stands <see cref="ScanStandOff"/> from fragment <paramref name="index"/> of <paramref name="setId"/>,
        /// <paramref name="angle"/> degrees round from square, looks at it and holds E until the scan target on the
        /// same host its view rests on is scanned.
        /// </summary>
        private IEnumerator ScanWhole(string setId, int index, float angle, string capture)
        {
            ScanFragment fragment = _gameplay.Scans.Find(ScanField.FragmentId(setId, index));
            Assert.IsNotNull(fragment, $"{setId}.{index} is in the world");
            Renderer solid = fragment.Solid;
            Assert.IsNotNull(solid, $"{fragment.FragmentId} shows on a renderer");
            Vector3 outward = Flat(fragment.Point - solid.bounds.center);
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.back;
            yield return StandFacing(fragment.Point, Quaternion.Euler(0f, angle, 0f) * outward, ScanStandOff);
            ScanField scans = _gameplay.Scans;
            string host = fragment.HostId;
            yield return Until(() => scans.Candidate != null && scans.Candidate.HostId == host, CandidateTimeout,
                () => $"07's view rests on a scan target on {host} (candidate " +
                      $"{scans.Candidate?.FragmentId ?? "none"}, 07 at {_context.Get<IRoverState>().Position}, the " +
                      $"fragment at {fragment.Point})");

            // Another target on the same host (a neighbour in the set, a story panel) may lie squarer in view: the
            // beam is measured on the one 07 scans.
            fragment = scans.Candidate;

            ScanBeam beam = _gameplay.Salvage.Feel.ScanBeam;
            float worst = 0f;
            bool captured = false;
            int chaseShot = 0;
            int frame = 0;
            Press(_keyboard.eKey);
            float deadline = Time.time + ScanTimeout;
            while (!fragment.Scanned && Time.time < deadline)
            {
                yield return null;
                frame++;
                if (beam.Level < FullBeam || beam.IsPinging)
                {
                    continue;
                }

                Vector3 end = beam.WedgeTargetCenter;
                float off = DistanceToSurface(fragment, end);
                worst = Mathf.Max(worst, off);
                Assert.LessOrEqual(off, OnSurface,
                    $"frame {frame} ({fragment.Progress:P0}): the scan beam ends {off * 100f:F1} cm off " +
                    $"{fragment.FragmentId} at {end} ({fragment.Surface.Count} collider(s))");
                if (!captured && fragment.Progress >= 0.5f)
                {
                    captured = true;
                    CaptureAcross("beam-scan-" + capture, _gameplay.Salvage.ScanEmitter, end);
                }

                if (chaseShot < ChaseMarks.Length && fragment.Progress >= ChaseMarks[chaseShot])
                {
                    float mark = ChaseMarks[chaseShot++];
                    CaptureBothSkies(_context.Get<IViewCamera>().Camera,
                        $"scan-chase-{capture}-{Mathf.RoundToInt(mark * 100f)}");
                }
            }

            Release(_keyboard.eKey);
            Assert.IsTrue(fragment.Scanned, $"held E scans {fragment.FragmentId}");
            Debug.Log($"[beam] {fragment.FragmentId}: worst {worst * 100f:F2} cm off its surface over {frame} frames");
            yield return new WaitForSeconds(SettleSeconds);
        }

        /// <summary>The charging pad placed in front of 07 and welded whole with a held E.</summary>
        private IEnumerator WeldThePad()
        {
            BuildField build = _gameplay.Building;
            _gameplay.KnownStructures.Learn(StructureIds.ChargingPad);
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition pad));
            _gameplay.Materials.Stow(pad.BuildCost);

            var terrain = _context.Get<ITerrainQuery>();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            yield return StandFacing(home + Vector3.forward * (PlaceAhead + 2f), Vector3.back, PlaceAhead);
            var rover = _context.Get<IRoverState>();
            Vector3 spot = rover.Position + Flat(rover.Rotation * Vector3.forward).normalized * PlaceAhead;
            build.SetAimPointOverride(SurfaceRules.OnSurface(terrain, spot.x, spot.z));

            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State, "B opens the build menu");
            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Placing, build.State);
            yield return null;
            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Welding, build.State, "E places the pad");
            int id = build.Controller.ActiveWeldInstanceId;
            Assert.IsTrue(build.TryGetView(id, out StructureView view));

            bool captured = false;
            int frame = 0;
            int gapFrames = 0;
            Press(_keyboard.eKey);
            float deadline = Time.time + WeldTimeout;
            while (Time.time < deadline &&
                   !(_gameplay.PlacedBase.TryGetStructure(id, out PlacedStructure placed) && placed.IsBuilt))
            {
                yield return null;
                frame++;
                if (!build.IsWelding || build.WeldBeamLevel < 1f || view.SolidStageCount >= view.StageCount)
                {
                    continue;
                }

                Vector3 end = build.WeldBeamEnd;
                float layer = view.GetActiveWeldPoint().y;
                bool standsAtLine = PartsAtHeight(view, layer);
                if (standsAtLine)
                {
                    Assert.LessOrEqual(Mathf.Abs(end.y - layer), LayerTolerance,
                        $"frame {frame}: the weld beam ends {Mathf.Abs(end.y - layer) * 100f:F1} cm off the print's " +
                        $"layer line ({end.y:F3} vs {layer:F3}); at the line stand:{_atLine}");
                }
                else
                {
                    gapFrames++;
                    Assert.LessOrEqual(end.y, layer + LayerTolerance,
                        $"frame {frame}: through a gap in the pad the weld beam ends above its layer line " +
                        $"({end.y:F3} vs {layer:F3})");
                }
                float off = DistanceToParts(view, end);
                if (off > OnSurface)
                {
                    bool met = view.TryWeldContact(build.WeldBeamStart, out Vector3 contact, out _);
                    Assert.Fail($"frame {frame} (stage {view.SolidStageCount + 1}/{view.StageCount}): the weld beam " +
                                $"ends off the pad's parts at {end} (its contact {contact}, met a part: {met}; seam " +
                                $"{view.GetActiveWeldPoint()}; {StageColliders(view)})");
                }
                if (!captured && view.SolidStageCount >= view.StageCount / 2)
                {
                    captured = true;
                    CaptureAcross("beam-weld-pad", build.WeldBeamStart, end);
                }
            }

            Release(_keyboard.eKey);
            build.SetAimPointOverride(null);
            Assert.IsTrue(_gameplay.PlacedBase.TryGetStructure(id, out PlacedStructure built) && built.IsBuilt,
                "a held E welds the pad whole");
            Debug.Log($"[beam] pad weld: every full-beam frame within {OnSurface * 100f:F0} cm of its parts and on its " +
                      $"layer line over {frame} frames ({gapFrames} through gaps where no part stands at the line, the " +
                      "beam on the top of what is printed)");
        }

        /// <summary>
        /// A structure placed in front of 07 and welded whole: once the camera has had <see cref="FramingSettleSeconds"/>
        /// the layer line being welded is never hidden by 07's body and stays in the middle
        /// <see cref="CentralShare"/> of the screen, every frame of the weld.
        /// </summary>
        private IEnumerator WeldInView(string structureId, int index)
        {
            BuildField build = _gameplay.Building;
            _gameplay.KnownStructures.Learn(structureId);
            Assert.IsTrue(build.Catalog.TryGet(structureId, out StructureDefinition definition));
            _gameplay.Materials.Stow(definition.BuildCost);

            var terrain = _context.Get<ITerrainQuery>();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition + Vector3.right * (index * StructureSpacing);
            yield return StandFacing(home + Vector3.forward * (WeldAhead + 2f), Vector3.back, WeldAhead);
            var rover = _context.Get<IRoverState>();
            Vector3 spot = rover.Position + Flat(rover.Rotation * Vector3.forward).normalized * WeldAhead;
            build.SetAimPointOverride(SurfaceRules.OnSurface(terrain, spot.x, spot.z));

            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State, "B opens the build menu");
            for (int step = 0; step < MaxMenuSteps && build.Controller.SelectedStructure?.Id != structureId; step++)
            {
                yield return Tap(_keyboard.rightArrowKey);
            }

            Assert.AreEqual(structureId, build.Controller.SelectedStructure?.Id, $"the arrows pick {structureId}");
            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Placing, build.State);
            yield return null;
            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Welding, build.State, $"E places {structureId}");
            int id = build.Controller.ActiveWeldInstanceId;
            Assert.IsTrue(build.TryGetView(id, out StructureView view));

            Camera camera = _context.Get<IViewCamera>().Camera;
            Transform roverBody = _context.Get<IRoverState>() is Component body ? body.transform : null;
            Assert.IsNotNull(roverBody, "07's body is a component");
            RoverCameraRig cameraRig = Object.FindFirstObjectByType<RoverCameraRig>(FindObjectsInactive.Include);
            int checkedFrames = 0;
            bool shot = false;
            var misses = new System.Text.StringBuilder();
            float start = Time.time;
            Press(_keyboard.eKey);
            float deadline = Time.time + WeldTimeout;
            while (Time.time < deadline &&
                   !(_gameplay.PlacedBase.TryGetStructure(id, out PlacedStructure placed) && placed.IsBuilt))
            {
                yield return null;
                if (Time.time - start < FramingSettleSeconds || !build.IsWelding || view.SolidStageCount >= view.StageCount)
                {
                    continue;
                }

                checkedFrames++;
                if (!shot && Time.time - start >= FramingSettleSeconds + ShotDelaySeconds)
                {
                    shot = true;
                    CaptureBothSkies(camera, "weld-framing-" + structureId);
                }

                Vector3 line = view.GetActiveWeldPoint();
                string hider = RoverSight.HiddenBy(roverBody, camera.transform.position, line);
                bool central = RoverSight.IsCentral(camera, line, CentralShare, out Vector3 viewport);
                if ((hider != null || !central) && misses.Length < MissReportLength)
                {
                    misses.AppendLine($"{structureId} t {Time.time - start:F1} s: layer line {line} hidden by " +
                                      $"{hider ?? "nothing"}, central {central} (viewport {viewport}, body {roverBody.name} {roverBody.GetType().Name}, rover state {rover.Position}, held {cameraRig.Held.Weight:F2} opening {cameraRig.Opening.IsActive} framing {cameraRig.Framing.Weight:F2} goal {cameraRig.Framing.Goal}, camera " +
                                      $"{camera.transform.position}, 07 {roverBody.position})");
                }
            }

            Release(_keyboard.eKey);
            build.SetAimPointOverride(null);
            Assert.IsTrue(_gameplay.PlacedBase.TryGetStructure(id, out PlacedStructure built) && built.IsBuilt,
                $"a held E welds {structureId} whole");
            Assert.Greater(checkedFrames, 0, $"{structureId}: the weld ran past the camera's settle time (the weld took " +
                                        $"{Time.time - start:F1} s)");
            Assert.AreEqual(string.Empty, misses.ToString(), $"The chase camera hides the weld of {structureId}:{System.Environment.NewLine}" + misses);
        }

        /// <summary>Metres from <paramref name="point"/> to the scan target's surface.</summary>
        private static float DistanceToSurface(ScanFragment fragment, Vector3 point)
        {
            if (fragment.Surface.Count == 0)
            {
                Renderer solid = fragment.Solid;
                Mesh mesh = solid.GetComponent<MeshFilter>().sharedMesh;
                SurfaceAim.ClosestOnBox(solid.localToWorldMatrix, mesh.bounds, point, out Vector3 face, out _);
                return Vector3.Distance(face, point);
            }

            float nearest = float.MaxValue;
            for (int i = 0; i < fragment.Surface.Count; i++)
            {
                Collider collider = fragment.Surface[i];
                if (collider is BoxCollider box)
                {
                    SurfaceAim.ClosestOnBox(box.transform.localToWorldMatrix, new Bounds(box.center, box.size), point,
                        out Vector3 face, out _);
                    nearest = Mathf.Min(nearest, Vector3.Distance(face, point));
                }
                else
                {
                    nearest = Mathf.Min(nearest, Vector3.Distance(collider.ClosestPoint(point), point));
                }
            }

            return nearest;
        }

        /// <summary>
        /// True when one of the pad's parts stands at world height <paramref name="height"/> (a slab one
        /// <see cref="SlabMetres"/> thick across its footprint meets it): the print's layer line is drawn on it.
        /// </summary>
        private bool PartsAtHeight(StructureView view, float height)
        {
            Bounds footprint = view.Colliders[0].bounds;
            for (int c = 1; c < view.Colliders.Count; c++)
            {
                footprint.Encapsulate(view.Colliders[c].bounds);
            }

            var centre = new Vector3(footprint.center.x, height, footprint.center.z);
            var half = new Vector3(footprint.extents.x, SlabMetres * 0.5f, footprint.extents.z);
            int count = Physics.OverlapBoxNonAlloc(centre, half, _near, Quaternion.identity, ~0,
                QueryTriggerInteraction.Ignore);
            _atLine = string.Empty;
            bool stands = false;
            for (int i = 0; i < count; i++)
            {
                for (int c = 0; c < view.Colliders.Count; c++)
                {
                    if (view.Colliders[c] == _near[i])
                    {
                        stands = true;
                        _atLine += $" {_near[i].name} {_near[i].bounds.min:F2}..{_near[i].bounds.max:F2}";
                    }
                }
            }

            return stands;
        }

        /// <summary>What colliders the stage being welded has, for a miss's message.</summary>
        private static string StageColliders(StructureView view)
        {
            Transform stage = view.StageRoots[Mathf.Clamp(view.SolidStageCount, 0, view.StageCount - 1)];
            var parts = new System.Text.StringBuilder();
            int count = 0;
            for (int c = 0; c < view.Colliders.Count; c++)
            {
                Collider collider = view.Colliders[c];
                if (collider != null && collider.transform.IsChildOf(stage))
                {
                    count++;
                    parts.Append($" {collider.name}({collider.GetType().Name}, enabled {collider.enabled}, active " +
                                 $"{collider.gameObject.activeInHierarchy}, {collider.bounds.center} " +
                                 $"{collider.bounds.size})");
                }
            }

            return $"stage '{stage.name}' has {count} of the pad's {view.Colliders.Count} colliders:{parts}";
        }

        /// <summary>
        /// 0 when one of the pad's own part colliders lies within <see cref="OnSurface"/> of
        /// <paramref name="point"/>, else infinity (its parts are meshes: no nearest point to measure to).
        /// </summary>
        private float DistanceToParts(StructureView view, Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, OnSurface, _near, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                for (int c = 0; c < view.Colliders.Count; c++)
                {
                    if (view.Colliders[c] == _near[i])
                    {
                        return 0f;
                    }
                }
            }

            return float.PositiveInfinity;
        }

        /// <summary>
        /// 07 set down <paramref name="standOff"/> metres from <paramref name="target"/> along
        /// <paramref name="from"/> (turned round it until the ground there is clear), facing it, its view on it.
        /// </summary>
        private IEnumerator StandFacing(Vector3 target, Vector3 from, float standOff)
        {
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 spot = target;
            for (float turn = 0f; turn < 360f; turn += SpotStep)
            {
                Vector3 side = Quaternion.Euler(0f, turn * (((int)(turn / SpotStep) & 1) == 0 ? 1f : -1f), 0f) * from;
                spot = SurfaceRules.OnSurface(terrain, target.x + side.x * standOff, target.z + side.z * standOff);
                if (!Physics.CheckSphere(spot + Vector3.up * ClearHeight, ClearRadius, Layers.PropMask,
                        QueryTriggerInteraction.Ignore))
                {
                    break;
                }
            }

            Vector3 facing = Flat(target - spot);
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.LookRotation(facing.normalized, Vector3.up));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(facing, Vector3.up).eulerAngles.y);
            yield return new WaitForSeconds(SettleSeconds);
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
            throw new NotSupportedException("The beam sessions load the scene through the editor.");
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
                yield return Until(() => _awake, WakeTimeout, "07 wakes");
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            yield return Until(condition, timeout, () => what);
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, Func<string> what)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), "timed out waiting until " + (condition() ? string.Empty : what()));
        }

        private IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        /// <summary>
        /// The lunar clock's time (s) when the sun stands lowest while still lighting <paramref name="focus"/> fully:
        /// golden hour.
        /// </summary>
        private float GoldenHour(Vector3 focus)
        {
            var sunlight = _context.Get<ISunlight>();
            float best = GoldenSearchFrom;
            float lowest = float.MaxValue;
            for (float time = GoldenSearchFrom; time <= GoldenSearchTo; time += GoldenSearchStep)
            {
                SetSkyClock(time, false);
                float height = sunlight.SunDirection.y;
                if (sunlight.SunlightAt(focus) >= FullSun && height > LowSun && height < lowest)
                {
                    lowest = height;
                    best = time;
                }
            }

            SetSkyClock(0f, true);
            return best;
        }

        /// <summary>
        /// Sets World's sky clock (its SkyCycleSystem, which the Gameplay tests do not reference) to
        /// <paramref name="seconds"/>, holding the opening eclipse or not.
        /// </summary>
        private void SetSkyClock(float seconds, bool holdingEclipse)
        {
            var sunlight = _context.Get<ISunlight>();
            MethodInfo set = sunlight.GetType().GetMethod("SetClockTime", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(set, "World's sky clock can be set for the captures");
            set.Invoke(sunlight, new object[] { seconds, holdingEclipse, true });
        }

        /// <summary>
        /// The game's camera, for one frame, looking across the beam from <paramref name="start"/> to
        /// <paramref name="end"/> from its side, so both its ends and the target show.
        /// </summary>
        private void CaptureAcross(string name, Vector3 start, Vector3 end)
        {
            Transform camera = _context.Get<IViewCamera>().Camera.transform;
            Vector3 position = camera.position;
            Quaternion rotation = camera.rotation;
            Vector3 middle = (start + end) * 0.5f;
            Vector3 along = Flat(end - start).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            Vector3 eye = middle + side * CaptureSideMetres + Vector3.up * CaptureRiseMetres - along * CaptureBackMetres;
            camera.SetPositionAndRotation(eye, Quaternion.LookRotation(middle - eye, Vector3.up));
            CaptureBothSkies(camera.GetComponent<Camera>(), name);
            camera.SetPositionAndRotation(position, rotation);
        }

        /// <summary><paramref name="lens"/>'s view now, in the eclipse and at golden hour.</summary>
        private void CaptureBothSkies(Camera lens, string name)
        {
            FrameCapture.SavePng(lens, 1280, 720, Path.Combine(GameplayFixture.CaptureFolder, name + "-eclipse.png"));
            SetSkyClock(_goldenSeconds, false);
            FrameCapture.SavePng(lens, 1280, 720, Path.Combine(GameplayFixture.CaptureFolder, name + "-golden.png"));
            SetSkyClock(0f, true);
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }
    }
}
