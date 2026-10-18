using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MoonProject.Editor.Automation;
using MoonProject.Rover;
using MoonProject.Testing;

namespace MoonProject.Showcase.Editor
{
    /// <summary>
    /// Batch capture session for <c>Showcase.unity</c> (Showcase 2). Builds the Showcase scene, initializes
    /// <see cref="ShowcaseController"/>, and captures all required look-dev verification PNGs into
    /// <c>Logs/captures/</c>:
    /// <list type="bullet">
    /// <item><description>Each of the 7 new R1 structures at 3 build stages (<c>Stage_1</c>, <c>Stage_3</c>, <c>Stage_4</c>) and with moving parts in their active pose</description></item>
    /// <item><description>The Ruins Corner at night (Earthlight + workshop/airlock/tower glow) and at dawn</description></item>
    /// <item><description>07 fully upgraded (front 3/4 and back 3/4) showing every socket-mounted kit and gift visual</description></item>
    /// <item><description>A wide shot of the whole 10-exhibit Showcase at night</description></item>
    /// </list>
    /// </summary>
    public static class ShowcaseCaptureSession
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        private static readonly (ShowcaseExhibitId id, string slug)[] NewStructureExhibits =
        {
            (ShowcaseExhibitId.BatteryBank, "battery_bank"),
            (ShowcaseExhibitId.AntennaMast, "antenna_mast"),
        };

        public static void CaptureAll()
        {
            BatchRunner.Run(nameof(CaptureAll), args =>
            {
                string outDir = BatchArgs.ProjectPath(args.GetString("out", "Logs/captures"));
                Directory.CreateDirectory(outDir);
                foreach (string existingPng in Directory.GetFiles(outDir, "*.png"))
                {
                    File.Delete(existingPng);
                }

                ShowcaseTuningBuilder.Build();
                ShowcaseSceneBuilder.Build();

                ShowcaseController controller = FindControllerInOpenScene();
                if (controller == null)
                {
                    Debug.LogError($"{nameof(ShowcaseCaptureSession)}: ShowcaseController not found in Showcase.unity.");
                    return false;
                }

                controller.Initialize();
                Camera cam = controller.CameraRig.Camera;

                bool previousAsyncShaders = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    Object.DestroyImmediate(FrameCapture.Render(cam, 64, 64));

                    int shotNumber = 1;

                    // 1..28: Each of the 7 new structures at Stage_1, Stage_3, Stage_4, and Active Pose
                    for (int i = 0; i < NewStructureExhibits.Length; i++)
                    {
                        (ShowcaseExhibitId exhibitId, string slug) = NewStructureExhibits[i];
                        ShowcaseExhibitRig rig = controller.FindExhibitRig(exhibitId);
                        controller.SelectExhibitById(exhibitId, immediate: true);
                        controller.SelectSkyPreset(ShowcaseSkyPresetId.FullSunlit, snapLighting: true);

                        // Stage 1 (foundation)
                        rig.SetAllTriggerableRulesActive(false);
                        rig.SetLampsLit(false);
                        controller.StepSimulation(1.5f);
                        rig.SetStageCountImmediate(1, 1f);
                        SaveShot(cam, outDir, $"{shotNumber++:D2}_{slug}_stage1.png");

                        // Stage 3 (structural shell complete)
                        rig.SetStageCountImmediate(3, 1f);
                        SaveShot(cam, outDir, $"{shotNumber++:D2}_{slug}_stage3.png");

                        // Stage 4 (rest pose)
                        rig.SetStageCountImmediate(4, 1f);
                        rig.SetLampsLit(true);
                        controller.StepSimulation(0.8f);
                        SaveShot(cam, outDir, $"{shotNumber++:D2}_{slug}_stage4.png");

                        // Stage 4 (active pose: moving parts engaged + lamps lit)
                        rig.SetStageCountImmediate(4, 1f);
                        rig.SetAllTriggerableRulesActive(true);
                        rig.SetLampsLit(true);
                        controller.StepSimulation(1.85f);
                        SaveShot(cam, outDir, $"{shotNumber++:D2}_{slug}_active.png");

                        // Reset back to settled Stage 4 with lamps lit
                        rig.SetAllTriggerableRulesActive(false);
                        rig.SetStageCountImmediate(4, 1f);
                        rig.SetLampsLit(true);
                    }

                    // 29: Ruins Corner at Earthlit Night
                    ShowcaseExhibitRig ruinsRig = controller.FindExhibitRig(ShowcaseExhibitId.RuinsCorner);
                    ruinsRig.SetLampsLit(true);
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.EarthlitNight, snapLighting: true);
                    controller.SelectExhibitById(ShowcaseExhibitId.RuinsCorner, immediate: true);
                    controller.StepSimulation(2.2f);
                    SaveShot(cam, outDir, $"{shotNumber++:D2}_ruins_corner_night.png");

                    // 30: Ruins Corner at Dawn (Sunrise)
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.Sunrise, snapLighting: true);
                    controller.SelectExhibitById(ShowcaseExhibitId.RuinsCorner, immediate: true);
                    controller.StepSimulation(2.2f);
                    SaveShot(cam, outDir, $"{shotNumber++:D2}_ruins_corner_dawn.png");

                    // 31: 07 Fully Upgraded — Front 3/4 view
                    ShowcaseExhibitRig upgradedRoverRig = controller.FindExhibitRig(ShowcaseExhibitId.Rover07Upgraded);
                    upgradedRoverRig.SetLampsLit(true);
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.FullSunlit, snapLighting: true);
                    controller.SelectExhibitById(
                        ShowcaseExhibitId.Rover07Upgraded,
                        immediate: true,
                        overrideYaw: 142f,
                        overridePitch: 13f,
                        overrideDistance: 3.15f,
                        overridePivotOffset: new Vector3(0f, 0.72f, 0.05f));
                    upgradedRoverRig.SetFaceFeeling(FaceFeeling.Happy);
                    upgradedRoverRig.TriggerKey(ShowcaseTriggerKey.Action2_X);
                    controller.StepSimulation(1.6f);
                    upgradedRoverRig.SnapFaceFeeling(FaceFeeling.Happy);
                    SaveShot(cam, outDir, $"{shotNumber++:D2}_rover07_upgraded_front34.png");
                    upgradedRoverRig.TriggerKey(ShowcaseTriggerKey.Action2_X);

                    // 32: 07 Fully Upgraded — Back 3/4 view (showing cargo rack, battery spine, tether ring, side bins, capacitor drums, pennant, solar cells)
                    controller.SelectExhibitById(
                        ShowcaseExhibitId.Rover07Upgraded,
                        immediate: true,
                        overrideYaw: 322f,
                        overridePitch: 16f,
                        overrideDistance: 3.10f,
                        overridePivotOffset: new Vector3(0f, 0.76f, -0.05f));
                    controller.StepSimulation(1.6f);
                    SaveShot(cam, outDir, $"{shotNumber++:D2}_rover07_upgraded_back34.png");

                    // 33: Wide shot of the whole 10-exhibit Showcase at night
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.EarthlitNight, snapLighting: true);
                    for (int i = 0; i < controller.Exhibits.Length; i++)
                    {
                        ShowcaseExhibitRig ex = controller.Exhibits[i];
                        if (ex != null)
                        {
                            if (ex.HasBuildStages)
                            {
                                ex.SetStageCountImmediate(4, 1f);
                            }
                            ex.SetLampsLit(true);
                        }
                    }

                    controller.CameraRig.SetWorldPoseImmediate(
                        new Vector3(-13.5f, 27.0f, 52.0f),
                        Quaternion.Euler(24.0f, 166.0f, 0f));
                    controller.StepSimulation(2.2f);
                    SaveShot(cam, outDir, $"{shotNumber++:D2}_showcase_wide_night.png");
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = previousAsyncShaders;
                }

                EditorSceneManager.OpenScene(ShowcasePaths.Scene, OpenSceneMode.Single);
                return true;
            });
        }

        private static ShowcaseController FindControllerInOpenScene()
        {
            GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var controller = roots[i].GetComponentInChildren<ShowcaseController>(true);
                if (controller != null)
                {
                    return controller;
                }
            }

            return null;
        }

        private static void SaveShot(Camera camera, string outDir, string fileName)
        {
            string path = Path.Combine(outDir, fileName);
            FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, path);
            BatchRunner.Log($"capture {path}");
        }
    }
}
