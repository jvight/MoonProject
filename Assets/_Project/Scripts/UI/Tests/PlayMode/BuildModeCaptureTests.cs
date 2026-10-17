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
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Visual capture session for 0.5.7 slice 6 (BUILD-MODE). Boots the built Main scene on an isolated save slot and
    /// renders the seven required screenshots into <c>Logs/captures/</c>:
    /// <list type="number">
    /// <item><description><c>01-build-menu.png</c> — the build strip menu open with learned structures.</description></item>
    /// <item><description><c>02-valid-ghost.png</c> — a valid cyan/amber hologram ghost in front of 07.</description></item>
    /// <item><description><c>03-invalid-ghost-reason.png</c> — an invalid ghost with its world-anchored one-line reason under the ghost.</description></item>
    /// <item><description><c>04-ghost-snapped-beside-pylon.png</c> — a ghost soft-snapped beside a placed power pylon.</description></item>
    /// <item><description><c>05-mid-weld-stage-2-of-4-night.png</c> — mid-weld (stage 2 of 4 solid, stage 3 hologram, beam + sparks) at night.</description></item>
    /// <item><description><c>06-three-structures-night.png</c> — the three finished structures at night with standby amber lamps lit.</description></item>
    /// <item><description><c>07-three-structures-sunlight.png</c> — the same three finished structures in full lunar sunlight.</description></item>
    /// </list>
    /// Run explicitly via:
    /// <c>python tools/unity_batch.py tests --platform playmode --filter MoonProject.UI.PlayModeTests.BuildModeCaptureTests</c>.
    /// </summary>
    [Category("BuildModeCapture")]
    [Explicit("Renders the 7 BUILD-MODE visual captures in the full Main scene; run explicitly.")]
    public sealed class BuildModeCaptureTests : InputTestFixture
    {
        private const int Width = 1600;
        private const int Height = 900;
        private const float WakeTimeout = 25f;
        private const float SettleSeconds = 0.6f;
        private const float EarthlitNightSeconds = 4816f;
        private const float SunlitDaySeconds = 210f;
        private const float ClearEastOffset = 18f;
        private const float ClearNorthOffset = 14f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private GameContext _context;
        private GameplaySystem _gameplay;
        private UISystem _ui;
        private UIDocument _document;
        private PanelSettings _originalPanelSettings;
        private PanelSettings _capturePanelSettings;
        private RenderTexture _uiTarget;
        private Keyboard _keyboard;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            if (_gameplay != null && _gameplay.Building != null && _gameplay.Building.Tuning != null)
            {
                _gameplay.Building.Tuning.SetPlaytestUnlockedIds();
            }

            if (_document != null && _originalPanelSettings != null)
            {
                _document.panelSettings = _originalPanelSettings;
            }

            if (_capturePanelSettings != null)
            {
                _capturePanelSettings.targetTexture = null;
                Object.DestroyImmediate(_capturePanelSettings);
                _capturePanelSettings = null;
            }

            if (_uiTarget != null)
            {
                _uiTarget.Release();
                Object.DestroyImmediate(_uiTarget);
                _uiTarget = null;
            }

            Scene scene = SceneManager.GetSceneByPath(BuildCaptureScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(BuildCaptureScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(BuildCaptureScene))]
        [PostBuildCleanup(typeof(BuildCaptureScene))]
        public IEnumerator CaptureAllSevenBuildModeStates()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return BootMainScene();

            BuildField build = _gameplay.Building;
            Assert.IsNotNull(build, "BuildField must be wired in Main.unity.");

            var placement = _context.Get<IRoverPlacement>();
            var terrain = _context.Get<ITerrainQuery>();
            Camera viewCamera = _context.Get<IViewCamera>().Camera;
            Assert.IsNotNull(viewCamera, "Main scene must provide IViewCamera.Camera.");

            // Disable CinemachineBrain and any camera rig MonoBehaviour so deterministic camera framing sticks.
            DisableCameraFollowBehaviours(viewCamera);

            Vector3 basePos = _context.Get<IWorldLayout>().BasePosition;
            Vector3 stageOrigin = new Vector3(basePos.x + ClearEastOffset, 0f, basePos.z + ClearNorthOffset);
            stageOrigin.y = terrain.SampleHeight(stageOrigin.x, stageOrigin.z);
            placement.PlaceAt(stageOrigin, Quaternion.LookRotation(Vector3.forward, Vector3.up));

            // Set sky to sunlit for the first 4 placement/menu shots so the terrain and hologram read clearly.
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            yield return new WaitForSecondsRealtime(2.0f);

            // Unlock all 10 R1 structures via the data-driven BuildingTuning playtest unlock list (no KnownStructures.Learn call).
            build.Tuning.SetPlaytestUnlockedIds(StructureIds.All);
            build.ApplyPlaytestUnlocks(restored: true);
            Assert.AreEqual(StructureIds.Count, build.Known.Count);
            Assert.AreEqual(StructureIds.Count, build.Controller.RefreshAvailable());

            // Give 07 5 Metal, 2 Wiring, 1 Optics so several structures are affordable and larger ones show coral shortage.
            DrainAllMaterials();
            _gameplay.Materials.Receive(SalvageMaterial.Metal, 5);
            _gameplay.Materials.Receive(SalvageMaterial.Wiring, 2);
            _gameplay.Materials.Receive(SalvageMaterial.Optics, 1);
            _ui.Pickups.Tick(10f);

            // Switch localization to Vietnamese ("vi") for the 10-structure build menu capture.
            var localization = _context.Get<ILocalization>();
            localization.SetLanguage("vi");
            yield return null;

            Vector3 roverPos = _context.Get<IRoverState>().Position;
            FrameBehindRover(viewCamera, roverPos, Vector3.forward, distance: 4.8f, height: 1.95f, pitchDeg: 13f);

            // Warm up FrameCapture once so URP post-processing history buffers are settled before shot 1.
            Texture2D warmup = FrameCapture.Render(viewCamera, Width, Height);
            Object.Destroy(warmup);
            yield return null;

            // -------------------------------------------------------------------------
            // 1. THE MENU WITH ALL 10 STRUCTURES IN VIETNAMESE (01-build-menu-vi.png)
            // -------------------------------------------------------------------------
            yield return Tap(_keyboard.bKey);
            Assert.AreEqual(BuildModeState.Menu, build.State);
            yield return SettleUi(0.65f);
            Assert.IsTrue(_ui.BuildStrip.IsVisible, "Build strip must be visible in Menu state.");
            Assert.AreEqual(StructureIds.Count, _ui.BuildStrip.CardCount, "All 10 structures must appear in the build strip.");
            for (int card = 0; card < _ui.BuildStrip.CardCount; card++)
            {
                StringAssert.Contains("%", _ui.BuildStrip.EnergyText(card),
                    "every card says what its weld takes from 07's battery");
            }

            yield return CaptureComposite(viewCamera, "01-build-menu-vi");

            // Deliver abundant materials onto home's shelves for placing and welding structures.
            _gameplay.Materials.Receive(SalvageMaterial.Metal, 40);
            _gameplay.Materials.Receive(SalvageMaterial.Wiring, 30);
            _gameplay.Materials.Receive(SalvageMaterial.Optics, 20);
            _ui.Pickups.Tick(10f);
            yield return null;

            // -------------------------------------------------------------------------
            // 2. A VALID GHOST (02-valid-ghost.png)
            // -------------------------------------------------------------------------
            // Select Solar Array (index 1) so the multi-panel solar array ghost is shown.
            build.Controller.MoveSelection(1);
            Assert.AreEqual(StructureIds.SolarArray, build.Controller.SelectedStructure.Id);
            Vector3 validSpot = roverPos + Vector3.forward * 4.5f + Vector3.right * 1.15f;
            validSpot.y = terrain.SampleHeight(validSpot.x, validSpot.z);
            build.SetAimPointOverride(validSpot);

            yield return Tap(_keyboard.eKey);
            Assert.AreEqual(BuildModeState.Placing, build.State);
            FrameBehindRover(viewCamera, roverPos, Vector3.forward, distance: 4.8f, height: 2.15f, pitchDeg: 15f);
            yield return SettleUi(0.65f);
            Assert.IsFalse(_ui.BuildStrip.IsVisible, "Build strip must hide when entering Placing state.");
            Assert.AreEqual(PlacementReason.Valid, build.Controller.CandidateReason);
            Assert.IsNotNull(build.GhostView);
            Assert.IsTrue(build.GhostView.IsGhostValid);
            yield return CaptureComposite(viewCamera, "02-valid-ghost");

            // -------------------------------------------------------------------------
            // 3. AN INVALID GHOST WITH ITS REASON (03-invalid-ghost-reason.png)
            // -------------------------------------------------------------------------
            // Aim too close to 07 on its front-right so CandidateReason == PlacementReason.TooCloseTo07
            // and both the crimson/amber invalid ghost and its world-anchored CRT reason chip sit over clear terrain.
            Vector3 invalidCloseSpot = roverPos + new Vector3(1.05f, 0f, 1.10f);
            invalidCloseSpot.y = terrain.SampleHeight(invalidCloseSpot.x, invalidCloseSpot.z);
            build.SetAimPointOverride(invalidCloseSpot);
            Vector3 invalidCamPos = roverPos + new Vector3(1.15f, 2.2f, -4.2f);
            Vector3 invalidLookAt = roverPos + new Vector3(0.55f, 0.45f, 1.25f);
            viewCamera.transform.SetPositionAndRotation(
                invalidCamPos,
                Quaternion.LookRotation(invalidLookAt - invalidCamPos, Vector3.up));
            yield return SettleUi(0.65f);
            Assert.AreEqual(PlacementReason.TooCloseTo07, build.Controller.CandidateReason);
            Assert.IsFalse(build.GhostView.IsGhostValid);
            Assert.IsTrue(_ui.BuildStrip.IsReasonVisible, "World-anchored reason chip must be visible under invalid ghost.");
            yield return CaptureComposite(viewCamera, "03-invalid-ghost-reason");

            // -------------------------------------------------------------------------
            // 4. A GHOST SNAPPED BESIDE A PLACED PYLON (04-ghost-snapped-beside-pylon.png)
            // -------------------------------------------------------------------------
            build.Controller.Close();
            yield return null;

            Vector3 pylonPos = roverPos + new Vector3(-2.4f, 0f, 4.6f);
            pylonPos.y = terrain.SampleHeight(pylonPos.x, pylonPos.z);
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.PowerPylon, out StructureDefinition pylonDef));
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition padDef));
            int pylonId = _gameplay.PlacedBase.PlaceUnchecked(
                pylonDef,
                new StructurePose(pylonPos, 0f),
                initialProgress01: 1f,
                restored: true);
            yield return null;

            // Open placing mode for Charging Pad and aim near the pylon's +X side so SnapSolver snaps it beside the pylon.
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.ChargingPad));
            float snapCenterOffset = (pylonDef.Width + padDef.Width) * 0.5f +
                                     Mathf.Max(pylonDef.Clearance, padDef.Clearance) + 0.15f;
            Vector3 nearPylonEast = pylonPos + new Vector3(snapCenterOffset, 0f, 0.08f);
            nearPylonEast.y = terrain.SampleHeight(nearPylonEast.x, nearPylonEast.z);
            build.SetAimPointOverride(nearPylonEast);

            Vector3 snapCamPos = roverPos + new Vector3(-1.35f, 2.35f, -3.6f);
            Vector3 snapLookAt = Vector3.Lerp(pylonPos, nearPylonEast, 0.55f) + Vector3.up * 0.55f;
            viewCamera.transform.SetPositionAndRotation(
                snapCamPos,
                Quaternion.LookRotation(snapLookAt - snapCamPos, Vector3.up));
            yield return SettleUi(0.65f);
            Assert.IsTrue(build.Controller.CandidateSnapped, "Ghost should be soft-snapped beside the placed pylon.");
            Assert.IsTrue(build.GhostView.IsGhostValid);
            yield return CaptureComposite(viewCamera, "04-ghost-snapped-beside-pylon");

            // -------------------------------------------------------------------------
            // 5. MID-WELD (STAGE 2 OF 4) AT NIGHT (05-mid-weld-stage-2-of-4-night.png)
            // -------------------------------------------------------------------------
            SetSkyClock(EarthlitNightSeconds, holdingEclipse: false);
            yield return null;

            Assert.IsTrue(build.Catalog.TryGet(StructureIds.SolarArray, out StructureDefinition solarDef));
            float solarSnapOffset = (pylonDef.Width + solarDef.Width) * 0.5f +
                                    Mathf.Max(pylonDef.Clearance, solarDef.Clearance) + 0.15f;
            Vector3 weldTargetPos = pylonPos + new Vector3(solarSnapOffset, 0f, 0f);
            weldTargetPos.y = terrain.SampleHeight(weldTargetPos.x, weldTargetPos.z);
            build.SetAimPointOverride(weldTargetPos);
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.SolarArray));
            yield return null;
            Press(_keyboard.eKey);
            yield return null;
            int midWeldId = build.Controller.ActiveWeldInstanceId;
            Assert.Greater(midWeldId, 0);
            Assert.AreEqual(BuildModeState.Welding, build.State);

            // Frame a 3/4 side angle that clearly shows 07, its active weld beam + sparks, and the 4-stage structure.
            Vector3 midCamPos = roverPos + new Vector3(3.15f, 1.85f, 1.15f);
            Vector3 midLookAt = Vector3.Lerp(roverPos + Vector3.up * 0.55f, weldTargetPos + Vector3.up * 0.85f, 0.58f);
            viewCamera.transform.SetPositionAndRotation(
                midCamPos,
                Quaternion.LookRotation(midLookAt - midCamPos, Vector3.up));

            // Keep E held until SolidStageCount == 2 (stage 2 of 4 solid, stage 3 hologram).
            float targetProgress = 0.56f;
            float weldDeadline = Time.time + solarDef.BuildSeconds + 2f;
            while (Time.time < weldDeadline)
            {
                if (_gameplay.PlacedBase.TryGetStructure(midWeldId, out PlacedStructure st) &&
                    st.BuildProgress01 >= targetProgress)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsTrue(build.TryGetView(midWeldId, out StructureView midView));
            Assert.AreEqual(2, midView.SolidStageCount, "Mid-weld shot must show stage 2 of 4 solid.");
            IRoverRig rig = _context.Get<IRoverRig>();
            Assert.IsNotNull(rig, "Main scene must provide IRoverRig.");
            Assert.IsNotNull(rig.CutterOrigin, "IRoverRig.CutterOrigin must be wired.");
            Assert.Less(
                Vector3.Distance(build.WeldBeamStart, rig.CutterOrigin.position),
                0.01f,
                "Weld beam must originate at IRoverRig.CutterOrigin (chest cutter turret).");
            yield return CaptureComposite(viewCamera, "05-mid-weld-stage-2-of-4-night");
            string r4Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "r4"));
            Directory.CreateDirectory(r4Folder);
            string r4WeldCapturePath = Path.Combine(r4Folder, "weld_from_cutter.png");
            File.Copy(
                Path.Combine(CaptureFolder, "05-mid-weld-stage-2-of-4-night.png"),
                r4WeldCapturePath,
                overwrite: true);
            Debug.Log($"[build-capture] {r4WeldCapturePath}");

            // Finish welding the Solar Array to 100%.
            while (Time.time < weldDeadline + 3f)
            {
                if (_gameplay.PlacedBase.TryGetStructure(midWeldId, out PlacedStructure st) && st.IsBuilt)
                {
                    break;
                }

                yield return null;
            }

            Release(_keyboard.eKey);
            yield return null;

            // -------------------------------------------------------------------------
            // 6. FINISHED BASE WITH 6 STRUCTURES AT NIGHT (06-base-night.png)
            // -------------------------------------------------------------------------
            // Place 4 more distinct R1 structures (ChargingPad, BatteryBank, AntennaMast, PowerPylon) around the
            // PowerPylon + SolarArray so a 6-structure base stands lit at night with warm amber standby lamps.
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.BatteryBank, out StructureDefinition batteryDef));
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.AntennaMast, out StructureDefinition smelterDef));
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.PowerPylon, out StructureDefinition fabricatorDef));

            Vector3 padPos = roverPos + new Vector3(2.8f, 0f, 4.4f);
            padPos.y = terrain.SampleHeight(padPos.x, padPos.z);
            int padId = _gameplay.PlacedBase.PlaceUnchecked(
                padDef,
                new StructurePose(padPos, -18f),
                initialProgress01: 1f,
                restored: true);

            Vector3 batteryPos = roverPos + new Vector3(-4.6f, 0f, 6.2f);
            batteryPos.y = terrain.SampleHeight(batteryPos.x, batteryPos.z);
            int batteryId = _gameplay.PlacedBase.PlaceUnchecked(
                batteryDef,
                new StructurePose(batteryPos, 22f),
                initialProgress01: 1f,
                restored: true);

            Vector3 smelterPos = roverPos + new Vector3(0.2f, 0f, 7.8f);
            smelterPos.y = terrain.SampleHeight(smelterPos.x, smelterPos.z);
            int smelterId = _gameplay.PlacedBase.PlaceUnchecked(
                smelterDef,
                new StructurePose(smelterPos, -8f),
                initialProgress01: 1f,
                restored: true);

            Vector3 fabricatorPos = roverPos + new Vector3(4.2f, 0f, 7.2f);
            fabricatorPos.y = terrain.SampleHeight(fabricatorPos.x, fabricatorPos.z);
            int fabricatorId = _gameplay.PlacedBase.PlaceUnchecked(
                fabricatorDef,
                new StructurePose(fabricatorPos, -28f),
                initialProgress01: 1f,
                restored: true);

            // Wait for welding completion sparks to dissipate and standby amber lamps to settle at steady glow.
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.GreaterOrEqual(_gameplay.PlacedBase.Count, 5, "Finished base capture must have 5+ structures.");
            Assert.IsTrue(build.TryGetView(pylonId, out StructureView pylonView) && pylonView.LampsLit);
            Assert.IsTrue(build.TryGetView(midWeldId, out StructureView solarView) && solarView.LampsLit);
            Assert.IsTrue(build.TryGetView(padId, out StructureView padView) && padView.LampsLit);
            Assert.IsTrue(build.TryGetView(batteryId, out StructureView batteryView) && batteryView.LampsLit);
            Assert.IsTrue(build.TryGetView(smelterId, out StructureView smelterView) && smelterView.LampsLit);
            Assert.IsTrue(build.TryGetView(fabricatorId, out StructureView fabricatorView) && fabricatorView.LampsLit);

            Vector3 groupCenter = (pylonPos + weldTargetPos + padPos + batteryPos + smelterPos + fabricatorPos) / 6f +
                                  Vector3.up * 0.75f;
            Vector3 groupCamPos = roverPos + new Vector3(-0.85f, 3.15f, -4.2f);
            viewCamera.transform.SetPositionAndRotation(
                groupCamPos,
                Quaternion.LookRotation(groupCenter - groupCamPos, Vector3.up));
            yield return SettleUi(0.25f);
            yield return CaptureComposite(viewCamera, "06-base-night");

            // -------------------------------------------------------------------------
            // 7. THE SAME 6-STRUCTURE BASE IN SUNLIGHT (07-base-sunlight.png)
            // -------------------------------------------------------------------------
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            yield return SettleUi(0.35f);
            yield return CaptureComposite(viewCamera, "07-base-sunlight");
        }

        private IEnumerator BootMainScene()
        {
            BootstrapHarness.DeleteSaveFiles(BuildCaptureScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                BuildCaptureScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("BuildModeCaptureTests runs in the Editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(BuildCaptureScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>(true);
                _ui = _ui != null ? _ui : root.GetComponentInChildren<UISystem>(true);
            }

            Assert.IsNotNull(bootstrap, "Scene has GameBootstrap.");
            Assert.IsNotNull(_gameplay, "Scene has GameplaySystem.");
            Assert.IsNotNull(_ui, "Scene has UISystem.");
            _context = bootstrap.Context;

            _document = _ui.GetComponent<UIDocument>();
            Assert.IsNotNull(_document, "UISystem has UIDocument.");
            _originalPanelSettings = _document.panelSettings;
            _uiTarget = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _uiTarget.Create();
            _capturePanelSettings = Object.Instantiate(_originalPanelSettings);
            _capturePanelSettings.targetTexture = _uiTarget;
            _capturePanelSettings.clearColor = true;
            _capturePanelSettings.colorClearValue = Color.clear;
            _document.panelSettings = _capturePanelSettings;

            _awake = false;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }
            }

            Assert.IsTrue(_awake, "07 wakes up.");
            yield return new WaitForSeconds(SettleSeconds);
        }

        private void SetSkyClock(float timeSeconds, bool holdingEclipse)
        {
            if (!_context.TryGet(out ISunlight sunlight) || sunlight == null)
            {
                return;
            }

            MethodInfo method = sunlight.GetType().GetMethod(
                "SetClockTime",
                BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(sunlight, new object[] { timeSeconds, holdingEclipse, true });
        }

        private void DrainAllMaterials()
        {
            Recipe current = new Recipe(
                _gameplay.Materials.Metal,
                _gameplay.Materials.Wiring,
                _gameplay.Materials.Optics);
            if (current.Metal > 0 || current.Wiring > 0 || current.Optics > 0)
            {
                _gameplay.Materials.TrySpend(current);
            }
        }

        private void DisableCameraFollowBehaviours(Camera viewCamera)
        {
            if (_context != null && _context.TryGet(out IViewCamera viewCamService) &&
                viewCamService is MonoBehaviour rigBehaviour)
            {
                rigBehaviour.enabled = false;
            }

            Behaviour[] cameraBehaviours = viewCamera.GetComponents<Behaviour>();
            for (int i = 0; i < cameraBehaviours.Length; i++)
            {
                Behaviour b = cameraBehaviours[i];
                if (b != null && !(b is Camera) &&
                    b.GetType().Name.IndexOf("Cinemachine", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    b.enabled = false;
                }
            }

            Transform cursor = viewCamera.transform;
            while (cursor != null)
            {
                MonoBehaviour[] behaviours = cursor.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour b = behaviours[i];
                    if (b != null && b.GetType().Namespace != null &&
                        b.GetType().Namespace.StartsWith("MoonProject.Rover", StringComparison.Ordinal))
                    {
                        b.enabled = false;
                    }
                }

                cursor = cursor.parent;
            }
        }

        private static void FrameBehindRover(
            Camera camera,
            Vector3 roverPos,
            Vector3 forwardFlat,
            float distance,
            float height,
            float pitchDeg)
        {
            Vector3 flat = Vector3.ProjectOnPlane(forwardFlat, Vector3.up).normalized;
            Vector3 eye = roverPos - flat * distance + Vector3.up * height;
            Vector3 right = Vector3.Cross(Vector3.up, flat).normalized;
            Quaternion rot = Quaternion.LookRotation(Quaternion.AngleAxis(pitchDeg, right) * flat, Vector3.up);
            camera.transform.SetPositionAndRotation(eye, rot);
            camera.fieldOfView = 56f;
        }

        private IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static IEnumerator SettleUi(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private IEnumerator CaptureComposite(Camera camera, string fileName)
        {
            yield return null;
            Texture2D world = FrameCapture.Render(camera, Width, Height);
            var ui = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _uiTarget;
            ui.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            try
            {
                Color32[] background = world.GetPixels32();
                Color32[] overlay = ui.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    background[i] = Over(overlay[i], background[i]);
                }

                world.SetPixels32(background);
                world.Apply(false);
                string path = Path.Combine(CaptureFolder, fileName + ".png");
                FrameCapture.WritePng(world, path);
                Debug.Log($"[build-capture] {path}");
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        private static Color32 Over(Color32 ui, Color32 world)
        {
            int keep = 255 - ui.a;
            return new Color32(
                (byte)Mathf.Min(255, ui.r + world.r * keep / 255),
                (byte)Mathf.Min(255, ui.g + world.g * keep / 255),
                (byte)Mathf.Min(255, ui.b + world.b * keep / 255),
                255);
        }

        public sealed class BuildCaptureScene : IPrebuildSetup, IPostBuildCleanup
        {
            public const string MainScene = "Assets/_Project/Scenes/Main.unity";
            public const string ScenePath = "Assets/_Project/Generated/UI/BuildCapture/BuildCaptureMain.unity";
            public static string SaveSlot => BootstrapHarness.ProcessSlot("build-capture");

            private const string ParentFolder = "Assets/_Project/Generated/UI";
            private const string Folder = "Assets/_Project/Generated/UI/BuildCapture";
            private const string SceneSlotLine = "_saveSlot: main";

            public void Setup()
            {
#if UNITY_EDITOR
                BootstrapHarness.DeleteSaveFiles(SaveSlot);
                DeleteCopy();
                if (!AssetDatabase.IsValidFolder(ParentFolder))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Generated", "UI");
                }

                if (!AssetDatabase.IsValidFolder(Folder))
                {
                    AssetDatabase.CreateFolder(ParentFolder, "BuildCapture");
                }

                string text = File.ReadAllText(MainScene);
                File.WriteAllText(ScenePath, text.Replace(SceneSlotLine, "_saveSlot: " + SaveSlot));
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
#endif
            }

            public void Cleanup()
            {
#if UNITY_EDITOR
                DeleteCopy();
                BootstrapHarness.DeleteSaveFiles(SaveSlot);
#endif
            }

#if UNITY_EDITOR
            private static void DeleteCopy()
            {
                if (AssetDatabase.IsValidFolder(Folder))
                {
                    AssetDatabase.DeleteAsset(Folder);
                }
            }
#endif
        }
    }
}
