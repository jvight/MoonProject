using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;

namespace MoonProject.Showcase.Editor
{
    /// <summary>
    /// Builds and updates <c>Assets/_Project/Data/Tuning/ShowcaseTuning.asset</c> with the 6 sky phase presets and
    /// the 6 look-dev exhibits' node animation rules and Vietnamese help hints.
    /// </summary>
    public static class ShowcaseTuningBuilder
    {
        public const string BuilderPath = "Showcase/Tuning";
        public const int BuilderOrder = 550;

        public static void BuildShowcaseTuning()
        {
            BatchRunner.Run(nameof(BuildShowcaseTuning), _ =>
            {
                Build();
                AssetDatabase.SaveAssets();
                return true;
            });
        }

        [MoonBuilder(BuilderPath, BuilderOrder)]
        public static void Build()
        {
            GeneratedAssets.EnsureFolder("Assets/_Project/Data/Tuning");
            if (AssetDatabase.LoadAssetAtPath<ShowcaseTuning>(ShowcasePaths.Tuning) == null &&
                AssetDatabase.LoadMainAssetAtPath(ShowcasePaths.Tuning) != null)
            {
                AssetDatabase.DeleteAsset(ShowcasePaths.Tuning);
            }

            var draft = ScriptableObject.CreateInstance<ShowcaseTuning>();
            PopulateDefaultTuning(draft);
            ShowcaseTuning tuning = GeneratedAssets.CreateOrReplace(draft, ShowcasePaths.Tuning);
            string problem = tuning.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{nameof(ShowcaseTuningBuilder)}: {problem}");
            }
        }

        public static void PopulateDefaultTuning(ShowcaseTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            ShowcaseSkyPreset[] presets =
            {
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.EclipseHold,
                    "Nhật thực toàn phần (Giữ)",
                    0f,
                    holdEclipse: true),
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.DawnBead,
                    "Thoát nhật thực / Hạt quang",
                    10f,
                    holdEclipse: false),
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.Sunrise,
                    "Bình minh Mặt Trăng",
                    40f,
                    holdEclipse: false),
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.FullSunlit,
                    "Ban ngày nắng gắt",
                    900f,
                    holdEclipse: false),
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.Sunset,
                    "Hoàng hôn Mặt Trăng",
                    2650f,
                    holdEclipse: false),
                new ShowcaseSkyPreset(
                    ShowcaseSkyPresetId.EarthlitNight,
                    "Đêm ánh Địa Cầu",
                    4800f,
                    holdEclipse: false),
            };

            ShowcaseExhibitDefinition[] exhibits =
            {
                BuildRoverExhibit(),
                BuildCargoLanderExhibit(),
                BuildChargingPadExhibit(),
                BuildSolarArrayExhibit(),
                BuildPowerPylonExhibit(),
                BuildRockClusterExhibit(),
            };

            tuning.Configure(
                presets,
                exhibits,
                freeFlySpeed: 8.5f,
                freeFlyFastMultiplier: 3.2f,
                freeFlyVerticalSpeed: 6.0f,
                lookSensitivity: 0.16f,
                gamepadLookRate: 120f,
                orbitZoomStep: 0.45f,
                orbitTransitionSeconds: 0.75f,
                scrubSecondsPerSecond: 600f,
                fastTimeScale: 60f,
                lampOffEmission: 0f,
                lampOnEmission: 2.2f,
                lampPulseMinEmission: 0.25f,
                lampPulseMaxEmission: 2.5f,
                lampPulseSpeed: 0.85f,
                lampEaseSeconds: 0.55f);
        }

        private static ShowcaseExhibitDefinition BuildRoverExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.RoverHeadLookRuleId,
                    "Quay cổ theo camera / góc quét",
                    "Neck",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 42f,
                    minLimit: -120f,
                    maxLimit: 120f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.RoverHeadPitchRuleId,
                    "Ngẩng / cúi đầu CRT",
                    "Neck/NeckLift/Head",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: -14f,
                    minLimit: -20f,
                    maxLimit: 20f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "rover.scoop.arm",
                    "Hạ / nâng cần xúc đất",
                    "DigScoop",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 18f,
                    minLimit: -12f,
                    maxLimit: 22f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "rover.scoop.bucket",
                    "Xúc / đổ gầu đào đất",
                    "DigScoop/ScoopArms/ScoopBucket",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 25f,
                    minLimit: -15f,
                    maxLimit: 30f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "rover.hatch.canopy",
                    "Mở / đóng nắp khoang chứa sau",
                    "Body/CargoHatch/UpperCanopy",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 52f,
                    minLimit: 0f,
                    maxLimit: 60f,
                    easeDuration: 0.90f,
                    ShowcaseTriggerKey.Action3_C),
                new ShowcaseNodeRule(
                    "rover.wheel.fl",
                    "Quay bánh trước trái",
                    "Wheel_FL",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 200f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.70f,
                    ShowcaseTriggerKey.Action4_V),
                new ShowcaseNodeRule(
                    "rover.wheel.fr",
                    "Quay bánh trước phải",
                    "Wheel_FR",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 200f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.70f,
                    ShowcaseTriggerKey.Action4_V),
                new ShowcaseNodeRule(
                    "rover.wheel.rl",
                    "Quay bánh sau trái",
                    "Wheel_RL",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 200f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.70f,
                    ShowcaseTriggerKey.Action4_V),
                new ShowcaseNodeRule(
                    "rover.wheel.rr",
                    "Quay bánh sau phải",
                    "Wheel_RR",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 200f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.70f,
                    ShowcaseTriggerKey.Action4_V),
                new ShowcaseNodeRule(
                    "rover.cutter.turret",
                    "Xoay tháp cắt laser",
                    "CutterTurret",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 32f,
                    minLimit: -35f,
                    maxLimit: 35f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action5_G),
                new ShowcaseNodeRule(
                    "rover.cutter.head",
                    "Ngắm đầu cắt laser",
                    "CutterTurret/CutterHead",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 16f,
                    minLimit: -15f,
                    maxLimit: 20f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action5_G),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("F", "Đổi biểu cảm màn hình CRT (9 trạng thái cảm xúc)"),
                new ShowcaseKeyHint("B", "Chớp mắt trên màn hình CRT"),
                new ShowcaseKeyHint("O", "Chạy chuỗi khởi động màn hình (Boot)"),
                new ShowcaseKeyHint("Z", "Đầu & cổ: Tự nhìn theo camera / Góc nghiêng kiểm tra"),
                new ShowcaseKeyHint("X", "Hạ cần xúc & đào đất (DigScoop + ScoopBucket)"),
                new ShowcaseKeyHint("C", "Mở / Đóng nắp khoang chứa sau (UpperCanopy)"),
                new ShowcaseKeyHint("V", "Bật / Tắt quay 4 bánh xe (Wheel_FL..RR)"),
                new ShowcaseKeyHint("G", "Xoay & ngắm tháp cắt laser (CutterTurret)"),
                new ShowcaseKeyHint("R", "Bật / Tắt đèn pha ấm & thanh trạng thái"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.Rover07,
                numberKey: 1,
                titleVi: "07 — Xe tự hành Mặt Trăng (v3)",
                subtitleVi: "Mẫu nhân vật chính R1 với màn hình CRT, gầu xúc, nắp khoang và tháp cắt.",
                positionOffset: new Vector3(-12f, 0f, 6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 0.88f, 0.04f),
                defaultOrbitYaw: 158f,
                defaultOrbitPitch: 10f,
                defaultOrbitDistance: 2.95f,
                minOrbitDistance: 1.35f,
                maxOrbitDistance: 8.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildCargoLanderExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    "lander.ramp",
                    "Hạ / nâng cầu dẫn khoang tàu",
                    "RampDoor",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 108f,
                    minLimit: 0f,
                    maxLimit: 110f,
                    easeDuration: 1.25f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.LanderWingLeftFoldRuleId,
                    "Gập / mở cánh pin trái",
                    "Wing_L",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.down,
                    restValue: 0f,
                    activeValue: 82f,
                    minLimit: 0f,
                    maxLimit: 88f,
                    easeDuration: 1.15f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "lander.wing.r.fold",
                    "Gập / mở cánh pin phải",
                    "Wing_R",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 82f,
                    minLimit: 0f,
                    maxLimit: 88f,
                    easeDuration: 1.15f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.LanderWingLeftPitchRuleId,
                    "Cánh trái xoay đón nắng",
                    "Wing_L",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 0f,
                    minLimit: -35f,
                    maxLimit: 35f,
                    easeDuration: 0.95f,
                    ShowcaseTriggerKey.None),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.LanderWingRightPitchRuleId,
                    "Cánh phải xoay đón nắng",
                    "Wing_R",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 0f,
                    minLimit: -35f,
                    maxLimit: 35f,
                    easeDuration: 0.95f,
                    ShowcaseTriggerKey.None),
                new ShowcaseNodeRule(
                    "lander.cargo.hatch",
                    "Mở / đóng nắp khoang hàng trên",
                    "CargoHatch",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 68f,
                    minLimit: 0f,
                    maxLimit: 75f,
                    easeDuration: 1.00f,
                    ShowcaseTriggerKey.Action3_C),
                new ShowcaseNodeRule(
                    "lander.crate.lid",
                    "Mở / đóng nắp thùng hàng",
                    "Crate/CrateLid",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 72f,
                    minLimit: 0f,
                    maxLimit: 80f,
                    easeDuration: 0.95f,
                    ShowcaseTriggerKey.Action3_C),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.LanderBayRepairRuleId,
                    "Ngàm sạc & cáp khoang tàu (Hỏng / Đã sửa)",
                    "ChargeContacts",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.08f,
                    minLimit: 0f,
                    maxLimit: 0.10f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action4_V,
                    defaultActive: true),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("Z", "Hạ / Nâng cầu dẫn khoang tàu (RampDoor)"),
                new ShowcaseKeyHint("X", "Gập / Mở hai cánh pin Mặt Trời (Wing_L, Wing_R)"),
                new ShowcaseKeyHint("C", "Mở / Đóng nắp khoang hàng & thùng hàng (CargoHatch, CrateLid)"),
                new ShowcaseKeyHint("V", "Chuyển trạng thái cáp khoang: Đã sửa / Đứt hỏng + Ngàm sạc"),
                new ShowcaseKeyHint("G", "Bật / Tắt đèn khoang ấm (BayLight_1..4)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.CargoLander,
                numberKey: 2,
                titleVi: "Tàu đổ bộ chở hàng (Cargo Lander)",
                subtitleVi: "Căn cứ đổ bộ R1 với cầu dẫn, hai cánh pin đón nắng, thùng hàng và khoang sạc.",
                positionOffset: new Vector3(0f, 0f, 6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 2.25f, 0.10f),
                defaultOrbitYaw: 156f,
                defaultOrbitPitch: 13f,
                defaultOrbitDistance: 9.2f,
                minOrbitDistance: 3.0f,
                maxOrbitDistance: 16.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildChargingPadExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    "pad.contacts",
                    "Nâng / hạ ngàm tiếp điện",
                    "Stage_4/ContactShoes",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.08f,
                    minLimit: 0f,
                    maxLimit: 0.10f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "pad.louver",
                    "Mở / đóng cửa chớp tản nhiệt",
                    "Stage_4/CoolingLouver",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 20f,
                    minLimit: 0f,
                    maxLimit: 25f,
                    easeDuration: 0.70f,
                    ShowcaseTriggerKey.Action2_X),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("Z", "Nâng / Hạ ngàm tiếp điện (ContactShoes)"),
                new ShowcaseKeyHint("X", "Mở / Đóng cửa chớp tản nhiệt (CoolingLouver)"),
                new ShowcaseKeyHint("C", "Bật / Tắt nhịp sáng cột đèn & dải dẫn hướng (TotemBar_1..4)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.ChargingPad,
                numberKey: 3,
                titleVi: "Bệ sạc năng lượng (Charging Pad)",
                subtitleVi: "Bệ sạc R1 hoàn chỉnh với ngàm tiếp điện, cửa chớp tản nhiệt và cột đèn báo mức sạc.",
                positionOffset: new Vector3(12f, 0f, 6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 0.78f, 0f),
                defaultOrbitYaw: 155f,
                defaultOrbitPitch: 15f,
                defaultOrbitDistance: 4.1f,
                minOrbitDistance: 2.0f,
                maxOrbitDistance: 10.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildSolarArrayExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.SolarAzimuthRuleId,
                    "Mâm xoay hướng Mặt Trời (Azimuth)",
                    "Stage_4/Azimuth",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.forward,
                    restValue: 0f,
                    activeValue: 0f,
                    minLimit: -180f,
                    maxLimit: 180f,
                    easeDuration: 1.10f,
                    ShowcaseTriggerKey.None),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.SolarElevationRuleId,
                    "Khớp ngẩng theo độ cao Mặt Trời (Elevation)",
                    "Stage_4/Azimuth/Elevation",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 0f,
                    minLimit: -25f,
                    maxLimit: 40f,
                    easeDuration: 1.10f,
                    ShowcaseTriggerKey.None),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.SolarWingLeftRuleId,
                    "Cánh pin trái (Tự gập ban đêm / nhật thực)",
                    "Stage_4/Azimuth/Elevation/Wing_L",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 55f,
                    minLimit: 0f,
                    maxLimit: 65f,
                    easeDuration: 1.05f,
                    ShowcaseTriggerKey.None),
                new ShowcaseNodeRule(
                    ShowcaseExhibitRig.SolarWingRightRuleId,
                    "Cánh pin phải (Tự gập ban đêm / nhật thực)",
                    "Stage_4/Azimuth/Elevation/Wing_R",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: -55f,
                    minLimit: -65f,
                    maxLimit: 0f,
                    easeDuration: 1.05f,
                    ShowcaseTriggerKey.None),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("Tự động", "Xoay hướng theo Mặt Trời thực tế; tự gập cánh vào ban đêm & nhật thực"),
                new ShowcaseKeyHint("Z", "Gập / Mở hai cánh pin thủ công (Wing_L, Wing_R)"),
                new ShowcaseKeyHint("X", "Chuyển giữa chế độ bám Mặt Trời tự động và góc nghiêng kiểm tra"),
                new ShowcaseKeyHint("C", "Bật / Tắt đèn báo trạng thái (StatusLamp)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.SolarArray,
                numberKey: 4,
                titleVi: "Dàn pin Mặt Trời (Solar Array)",
                subtitleVi: "Tháp pin R1 tự động xoay theo hướng Mặt Trời và gập cánh khi đêm xuống hoặc nhật thực.",
                positionOffset: new Vector3(-12f, 0f, -6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 1.35f, 0f),
                defaultOrbitYaw: 152f,
                defaultOrbitPitch: 14f,
                defaultOrbitDistance: 4.5f,
                minOrbitDistance: 2.2f,
                maxOrbitDistance: 11.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildPowerPylonExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    "pylon.drum.1",
                    "Quay tang trống cáp 1",
                    "Stage_4/CableDrum_1",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 110f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "pylon.drum.2",
                    "Quay tang trống cáp 2",
                    "Stage_4/CableDrum_2",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: -110f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "pylon.drum.3",
                    "Quay tang trống cáp 3",
                    "Stage_4/CableDrum_3",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 110f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "pylon.knife",
                    "Đóng / ngắt cầu dao điện",
                    "Stage_4/KnifeSwitch",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 56f,
                    minLimit: 0f,
                    maxLimit: 60f,
                    easeDuration: 0.65f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "pylon.yoke",
                    "Xoay cổ dẫn hướng cáp",
                    "Stage_4/Yoke",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.forward,
                    restValue: 0f,
                    activeValue: 45f,
                    minLimit: 0f,
                    maxLimit: 60f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action2_X),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("Z", "Bật / Tắt quay 3 tang trống cáp (CableDrum_1..3)"),
                new ShowcaseKeyHint("X", "Đóng / Ngắt cầu dao & xoay cổ dẫn cáp (KnifeSwitch, Yoke)"),
                new ShowcaseKeyHint("C", "Bật / Tắt đèn dẫn đường ấm (PathLamp)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.PowerPylon,
                numberKey: 5,
                titleVi: "Trụ truyền tải điện (Power Pylon)",
                subtitleVi: "Trụ nối mạng điện R1 với 3 tang trống cáp, cầu dao cơ khí và đèn dẫn đường màu hổ phách.",
                positionOffset: new Vector3(0f, 0f, -6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 1.15f, 0f),
                defaultOrbitYaw: 148f,
                defaultOrbitPitch: 13f,
                defaultOrbitDistance: 3.4f,
                minOrbitDistance: 1.8f,
                maxOrbitDistance: 9.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildRockClusterExhibit()
        {
            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("T", "Đổi pha bầu trời để kiểm tra bóng đổ đá bazan dưới nắng thấp & đêm Địa Cầu"),
                new ShowcaseKeyHint("[ / ]", "Tua đồng hồ Mặt Trăng để quan sát bóng dài quét qua cụm đá"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.RockCluster,
                numberKey: 6,
                titleVi: "Cụm đá bazan Mặt Trăng (Rock Cluster)",
                subtitleVi: "Tập hợp 10 mẫu đá bazan R1 (Rock_00..09) kiểm tra sắc độ bề mặt và bóng đổ.",
                positionOffset: new Vector3(12f, 0f, -6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 0.55f, 0f),
                defaultOrbitYaw: 155f,
                defaultOrbitPitch: 16f,
                defaultOrbitDistance: 4.8f,
                minOrbitDistance: 2.2f,
                maxOrbitDistance: 12.0f,
                nodes: Array.Empty<ShowcaseNodeRule>(),
                keyHints: hints);
        }
    }
}
