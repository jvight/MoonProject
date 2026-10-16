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
    /// Batch capture session for <c>Showcase.unity</c>. Builds the Showcase scene, initializes
    /// <see cref="ShowcaseController"/>, and captures all required look-dev verification PNGs into
    /// <c>Logs/captures/</c>:
    /// <list type="bullet">
    /// <item><description>Each of the 6 exhibits at night (EarthlitNight) and in sunlight (FullSunlit)</description></item>
    /// <item><description>Eclipse hold and Dawn bead</description></item>
    /// <item><description>07 in three different feelings (Happy, Curious, Proud) with animated parts</description></item>
    /// <item><description>Cargo Lander with ramp down and bay lit</description></item>
    /// <item><description>Solar Array tracking the sun</description></item>
    /// </list>
    /// </summary>
    public static class ShowcaseCaptureSession
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;

        public static void CaptureAll()
        {
            BatchRunner.Run(nameof(CaptureAll), args =>
            {
                string outDir = BatchArgs.ProjectPath(args.GetString("out", "Logs/captures"));
                Directory.CreateDirectory(outDir);

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

                    // 1-12: Each of the 6 exhibits at Earthlit Night and in Full Sunlit
                    string[] slugs =
                    {
                        "rover07",
                        "cargo_lander",
                        "charging_pad",
                        "solar_array",
                        "power_pylon",
                        "rock_cluster",
                    };

                    ShowcaseExhibitRig roverRig = controller.Exhibits[0];

                    for (int i = 0; i < 6; i++)
                    {
                        controller.SelectExhibit(i, immediate: true);
                        controller.SelectSkyPreset(ShowcaseSkyPresetId.EarthlitNight, snapLighting: true);
                        controller.StepSimulation(2.2f);
                        if (i == 0)
                        {
                            roverRig.SnapFaceFeeling(FaceFeeling.Calm);
                        }

                        SaveShot(cam, outDir, $"{(i * 2 + 1):D2}_{slugs[i]}_night.png");

                        controller.SelectExhibit(i, immediate: true);
                        controller.SelectSkyPreset(ShowcaseSkyPresetId.FullSunlit, snapLighting: true);
                        controller.StepSimulation(2.2f);
                        if (i == 0)
                        {
                            roverRig.SnapFaceFeeling(FaceFeeling.Calm);
                        }

                        SaveShot(cam, outDir, $"{(i * 2 + 2):D2}_{slugs[i]}_sunlit.png");
                    }

                    // 13: Eclipse hold (looking North-Northwest toward Earth at bearing 338, elevation 16, with 07 on the left and Earth unobstructed)
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.EclipseHold, snapLighting: true);
                    controller.SelectExhibit(
                        0,
                        immediate: true,
                        overrideYaw: 342f,
                        overridePitch: -8f,
                        overrideDistance: 3.8f,
                        overridePivotOffset: new Vector3(0.95f, 0.78f, 0f));
                    controller.StepSimulation(2.0f);
                    SaveShot(cam, outDir, "13_eclipse_hold.png");

                    // 14: Dawn bead (Sun limb breaking out on Earth's right edge at t = 10.5s, unobstructed to the right of 07)
                    controller.SetClockTime(10.5f, holdEclipse: false, snapLighting: true);
                    controller.SelectExhibit(
                        0,
                        immediate: true,
                        overrideYaw: 342f,
                        overridePitch: -8f,
                        overrideDistance: 3.8f,
                        overridePivotOffset: new Vector3(0.95f, 0.78f, 0f));
                    controller.StepSimulation(2.0f);
                    SaveShot(cam, outDir, "14_dawn_bead.png");

                    // 15-17: 07 in three different feelings (Happy, Curious, Proud) with animated parts
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.FullSunlit, snapLighting: true);

                    // Feeling 1: Happy + scoop lowered
                    controller.SelectExhibit(
                        0,
                        immediate: true,
                        overrideYaw: 164f,
                        overridePitch: 8f,
                        overrideDistance: 2.35f,
                        overridePivotOffset: new Vector3(0f, 0.90f, 0.10f));
                    roverRig.SetFaceFeeling(FaceFeeling.Happy);
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action2_X);
                    controller.StepSimulation(1.6f);
                    roverRig.SnapFaceFeeling(FaceFeeling.Happy);
                    SaveShot(cam, outDir, "15_rover07_feeling_happy.png");
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action2_X);

                    // Feeling 2: Curious + cutter turret aimed
                    controller.SelectExhibit(
                        0,
                        immediate: true,
                        overrideYaw: 152f,
                        overridePitch: 9f,
                        overrideDistance: 2.35f,
                        overridePivotOffset: new Vector3(0f, 0.92f, 0.08f));
                    roverRig.SetFaceFeeling(FaceFeeling.Curious);
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action5_G);
                    controller.StepSimulation(1.6f);
                    roverRig.SnapFaceFeeling(FaceFeeling.Curious);
                    SaveShot(cam, outDir, "16_rover07_feeling_curious.png");
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action5_G);

                    // Feeling 3: Proud + rear canopy hatch open
                    controller.SelectExhibit(
                        0,
                        immediate: true,
                        overrideYaw: 132f,
                        overridePitch: 16f,
                        overrideDistance: 2.45f,
                        overridePivotOffset: new Vector3(0f, 0.90f, -0.05f));
                    roverRig.SetFaceFeeling(FaceFeeling.Proud);
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action3_C);
                    controller.StepSimulation(1.6f);
                    roverRig.SnapFaceFeeling(FaceFeeling.Proud);
                    SaveShot(cam, outDir, "17_rover07_feeling_proud.png");

                    // Reset 07 toggles back to rest
                    roverRig.TriggerKey(ShowcaseTriggerKey.Action3_C);
                    roverRig.SnapFaceFeeling(FaceFeeling.Calm);

                    // 18: Cargo Lander with ramp down and bay lit (at dusk/night so warm amber bay lights + lowered ramp shine)
                    ShowcaseExhibitRig landerRig = controller.Exhibits[1];
                    controller.SelectSkyPreset(ShowcaseSkyPresetId.EarthlitNight, snapLighting: true);
                    controller.SelectExhibit(
                        1,
                        immediate: true,
                        overrideYaw: 158f,
                        overridePitch: 12f,
                        overrideDistance: 6.8f,
                        overridePivotOffset: new Vector3(0f, 1.65f, 0.45f));
                    landerRig.SetLampsLit(true);
                    landerRig.TriggerKey(ShowcaseTriggerKey.Action1_Z); // Lower RampDoor
                    landerRig.TriggerKey(ShowcaseTriggerKey.Action3_C); // Open CargoHatch & CrateLid
                    controller.StepSimulation(2.2f);
                    SaveShot(cam, outDir, "18_lander_ramp_down_bay_lit.png");

                    // 19: Solar Array tracking the Sun (at Sunrise / low golden sun, showing unfolded panels tilted toward the Sun)
                    controller.SetClockTime(1400f, holdEclipse: false, snapLighting: true);
                    controller.SelectExhibit(
                        3,
                        immediate: true,
                        overrideYaw: 132f,
                        overridePitch: 13f,
                        overrideDistance: 4.1f,
                        overridePivotOffset: new Vector3(0f, 1.35f, 0f));
                    controller.StepSimulation(2.5f);
                    SaveShot(cam, outDir, "19_solar_array_tracking_sun.png");
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
