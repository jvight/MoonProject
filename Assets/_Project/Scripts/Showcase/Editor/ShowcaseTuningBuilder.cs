using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;
using MoonProject.Rover;

namespace MoonProject.Showcase.Editor
{
    /// <summary>
    /// Builds and updates <c>Assets/_Project/Data/Tuning/ShowcaseTuning.asset</c> with the 6 sky phase presets,
    /// the 5 ruin piece placements, and the 10 look-dev exhibits' node animation rules and Vietnamese help hints.
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

            ShowcaseRuinPiecePlacement[] ruinPieces =
            {
                new ShowcaseRuinPiecePlacement(
                    ShowcaseRuinPieceId.HabitatModules,
                    new Vector3(-4.80f, 0f, 1.50f),
                    -10f),
                new ShowcaseRuinPiecePlacement(
                    ShowcaseRuinPieceId.RadioTower,
                    new Vector3(1.80f, 0f, 9.20f),
                    -8f),
                new ShowcaseRuinPiecePlacement(
                    ShowcaseRuinPieceId.KenjiWorkshop,
                    new Vector3(6.35f, 0f, 1.20f),
                    -16f),
                new ShowcaseRuinPiecePlacement(
                    ShowcaseRuinPieceId.FallenSolarArray,
                    new Vector3(2.15f, 0f, -6.20f),
                    -20f),
                new ShowcaseRuinPiecePlacement(
                    ShowcaseRuinPieceId.ScatterSet,
                    new Vector3(-6.80f, 0f, 6.80f),
                    18f),
            };

            ShowcaseExhibitDefinition[] exhibits =
            {
                BuildRoverExhibit(),
                BuildCargoLanderExhibit(),
                BuildChargingPadExhibit(),
                BuildSolarArrayExhibit(),
                BuildPowerPylonExhibit(),
                BuildRockClusterExhibit(),
                BuildRoverUpgradedExhibit(),
                BuildBatteryBankExhibit(),
                BuildAntennaMastExhibit(),
                BuildRuinsCornerExhibit(),
            };

            tuning.Configure(
                presets,
                exhibits,
                ruinPieces,
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
                lampEaseSeconds: 0.55f,
                stageRiseDepthMeters: 0.42f,
                stageSettleOvershootMeters: 0.035f,
                stageRiseDurationSeconds: 0.65f,
                stageStepIntervalSeconds: 0.55f,
                stageAutoLoopHoldSeconds: 2.4f,
                feelingDemoIntervalSeconds: 2.2f,
                tourExhibitHoldSeconds: 4.2f,
                tourTransitionSeconds: 1.6f,
                tourOrbitSweepDegrees: 22f);
        }

        private static ShowcaseNodeRule[] CreateBaseRoverNodes(string prefix = "rover")
        {
            return new[]
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
                    $"{prefix}.scoop.arm",
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
                    $"{prefix}.scoop.bucket",
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
                    $"{prefix}.hatch.canopy",
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
                    $"{prefix}.wheel.fl",
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
                    $"{prefix}.wheel.fr",
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
                    $"{prefix}.wheel.rl",
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
                    $"{prefix}.wheel.rr",
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
                    $"{prefix}.cutter.turret",
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
                    $"{prefix}.cutter.head",
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
        }

        private static ShowcaseExhibitDefinition BuildRoverExhibit()
        {
            ShowcaseNodeRule[] nodes = CreateBaseRoverNodes("rover");

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("F", "Đổi biểu cảm màn hình CRT (9 trạng thái cảm xúc)"),
                new ShowcaseKeyHint("B", "Chớp mắt trên màn hình CRT"),
                new ShowcaseKeyHint("U", "Chạy chuỗi khởi động màn hình (Boot)"),
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
                new ShowcaseKeyHint("J", "Chạy chuỗi dựng 4 giai đoạn (Stage_1..4 mọc lên êm)"),
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
                new ShowcaseKeyHint("J", "Chạy chuỗi dựng 4 giai đoạn (Stage_1..4 mọc lên êm)"),
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
                new ShowcaseKeyHint("J", "Chạy chuỗi dựng 4 giai đoạn (Stage_1..4 mọc lên êm)"),
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

        private static ShowcaseExhibitDefinition BuildRoverUpgradedExhibit()
        {
            ShowcaseNodeRule[] baseNodes = CreateBaseRoverNodes("rover_up");
            var nodes = new ShowcaseNodeRule[baseNodes.Length + 1];
            Array.Copy(baseNodes, nodes, baseNodes.Length);
            nodes[baseNodes.Length] = new ShowcaseNodeRule(
                "rover_up.scan_fan",
                "Quay quạt quét cảm biến (ScanFan)",
                RoverModelNodes.ScanFanSocket,
                ShowcaseMotionKind.LocalRotation,
                Vector3.up,
                restValue: 0f,
                activeValue: 28f,
                minLimit: -35f,
                maxLimit: 35f,
                easeDuration: 0.75f,
                ShowcaseTriggerKey.Action5_G);

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("F / B / U", "Đổi biểu cảm CRT / Chớp mắt / Khởi động màn hình (Boot)"),
                new ShowcaseKeyHint("K", "Chế độ Demo: Cả hai xe 07 tự động đổi biểu cảm trên màn hình CRT"),
                new ShowcaseKeyHint("Z", "Đầu & cổ: Tự nhìn theo camera / Góc nghiêng kiểm tra"),
                new ShowcaseKeyHint("X", "Hạ cần xúc & đào đất (DigScoop + ScoopBucket)"),
                new ShowcaseKeyHint("C", "Mở / Đóng nắp khoang chứa sau (UpperCanopy)"),
                new ShowcaseKeyHint("V", "Bật / Tắt quay 4 bánh xe (Wheel_FL..RR)"),
                new ShowcaseKeyHint("G", "Xoay tháp cắt laser & quay quạt quét (CutterTurret + ScanFan)"),
                new ShowcaseKeyHint("R", "Bật / Tắt đèn pha ấm, dàn đèn nóc & thanh trạng thái"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.Rover07Upgraded,
                numberKey: 7,
                titleVi: "07 — Xe tự hành nâng cấp toàn diện (Full Kit)",
                subtitleVi: "Xe tự hành 07 gắn đủ mọi bộ kit nâng cấp trên đúng khớp socket gốc, cùng quà kỷ niệm và màn hình CRT.",
                positionOffset: new Vector3(-24f, 0f, 6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 0.92f, 0.04f),
                defaultOrbitYaw: 158f,
                defaultOrbitPitch: 11f,
                defaultOrbitDistance: 3.15f,
                minOrbitDistance: 1.35f,
                maxOrbitDistance: 8.5f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildBatteryBankExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    "battery.safety_bar",
                    "Nâng / hạ thanh khóa an toàn (SafetyBar)",
                    "Stage_4/SafetyBar",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.right,
                    restValue: 0f,
                    activeValue: 82f,
                    minLimit: 0f,
                    maxLimit: 90f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "battery.plunger.1",
                    "Nhô / thu chốt pít-tông 1 (Plunger_1)",
                    "Stage_4/Plunger_1",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.06f,
                    minLimit: 0f,
                    maxLimit: 0.08f,
                    easeDuration: 0.65f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "battery.plunger.2",
                    "Nhô / thu chốt pít-tông 2 (Plunger_2)",
                    "Stage_4/Plunger_2",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.06f,
                    minLimit: 0f,
                    maxLimit: 0.08f,
                    easeDuration: 0.65f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "battery.plunger.3",
                    "Nhô / thu chốt pít-tông 3 (Plunger_3)",
                    "Stage_4/Plunger_3",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.06f,
                    minLimit: 0f,
                    maxLimit: 0.08f,
                    easeDuration: 0.65f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "battery.plunger.4",
                    "Nhô / thu chốt pít-tông 4 (Plunger_4)",
                    "Stage_4/Plunger_4",
                    ShowcaseMotionKind.LocalPosition,
                    Vector3.back,
                    restValue: 0f,
                    activeValue: 0.06f,
                    minLimit: 0f,
                    maxLimit: 0.08f,
                    easeDuration: 0.65f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "battery.dial",
                    "Xoay núm chỉnh điện áp (RotaryDial)",
                    "Stage_4/RotaryDial",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 90f,
                    minLimit: 0f,
                    maxLimit: 120f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action3_C),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("J", "Chạy chuỗi dựng 4 giai đoạn (Stage_1..4 mọc lên êm)"),
                new ShowcaseKeyHint("Z", "Nâng / Hạ thanh khóa an toàn (SafetyBar)"),
                new ShowcaseKeyHint("X", "Nhô / Thu 4 chốt pít-tông (Plunger_1..4)"),
                new ShowcaseKeyHint("C", "Xoay núm chỉnh điện áp (RotaryDial)"),
                new ShowcaseKeyHint("V", "Bật / Tắt đèn ống thủy & đèn nóc (SightGlass_1..4, RoofBeacon)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.BatteryBank,
                numberKey: 8,
                titleVi: "Trạm ắc-quy dự trữ (Battery Bank)",
                subtitleVi: "Khối ắc-quy R1 4 giai đoạn với thanh khóa an toàn, 4 chốt pít-tông, núm xoay cơ khí và đèn báo mức điện.",
                positionOffset: new Vector3(24f, 0f, 6f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 1.05f, 0f),
                defaultOrbitYaw: 154f,
                defaultOrbitPitch: 14f,
                defaultOrbitDistance: 4.2f,
                minOrbitDistance: 2.0f,
                maxOrbitDistance: 10.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildAntennaMastExhibit()
        {
            ShowcaseNodeRule[] nodes =
            {
                new ShowcaseNodeRule(
                    "antenna.azimuth",
                    "Xoay phương vị chảo thu phát (DishAzimuth)",
                    "Stage_4/Mast/DishAzimuth",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.forward,
                    restValue: 0f,
                    activeValue: 48f,
                    minLimit: -180f,
                    maxLimit: 180f,
                    easeDuration: 0.90f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "antenna.dish",
                    "Ngẩng / hạ lòng chảo ăng-ten (Dish)",
                    "Stage_4/Mast/DishAzimuth/Dish",
                    ShowcaseMotionKind.LocalRotation,
                    Vector3.left,
                    restValue: 0f,
                    activeValue: 36f,
                    minLimit: 0f,
                    maxLimit: 45f,
                    easeDuration: 0.85f,
                    ShowcaseTriggerKey.Action1_Z),
                new ShowcaseNodeRule(
                    "antenna.sonar",
                    "Quay vòng cảm biến định vị (SonarLoop)",
                    "Stage_4/Mast/SonarLoop",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.forward,
                    restValue: 0f,
                    activeValue: 95f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.80f,
                    ShowcaseTriggerKey.Action2_X),
                new ShowcaseNodeRule(
                    "antenna.reel.l",
                    "Quay cuộn băng từ trái (TapeReel_L)",
                    "Stage_4/TapeReel_L",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: 120f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action3_C),
                new ShowcaseNodeRule(
                    "antenna.reel.r",
                    "Quay cuộn băng từ phải (TapeReel_R)",
                    "Stage_4/TapeReel_R",
                    ShowcaseMotionKind.ContinuousSpin,
                    Vector3.up,
                    restValue: 0f,
                    activeValue: -120f,
                    minLimit: 0f,
                    maxLimit: 360f,
                    easeDuration: 0.75f,
                    ShowcaseTriggerKey.Action3_C),
            };

            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("J", "Chạy chuỗi dựng 4 giai đoạn (Stage_1..4 mọc lên êm)"),
                new ShowcaseKeyHint("Z", "Xoay phương vị & ngẩng chảo thu phát (DishAzimuth + Dish)"),
                new ShowcaseKeyHint("X", "Bật / Tắt quay vòng cảm biến (SonarLoop)"),
                new ShowcaseKeyHint("C", "Bật / Tắt quay đôi cuộn băng từ (TapeReel_L, TapeReel_R)"),
                new ShowcaseKeyHint("V", "Bật / Tắt đèn đỉnh tháp & mặt đồng hồ (BeaconLamp, DialLamp)"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.AntennaMast,
                numberKey: 13,
                titleVi: "Tháp ăng-ten liên lạc (Antenna Mast)",
                subtitleVi: "Tháp viễn thông R1 4 giai đoạn với chảo thu phát xoay 2 trục, vòng cảm biến quay và đôi cuộn băng từ.",
                positionOffset: new Vector3(6f, 0f, 18f),
                yawDegrees: 350f,
                orbitPivotOffset: new Vector3(0f, 1.75f, 0f),
                defaultOrbitYaw: 196f,
                defaultOrbitPitch: 13f,
                defaultOrbitDistance: 5.4f,
                minOrbitDistance: 2.4f,
                maxOrbitDistance: 13.0f,
                nodes: nodes,
                keyHints: hints);
        }

        private static ShowcaseExhibitDefinition BuildRuinsCornerExhibit()
        {
            ShowcaseKeyHint[] hints =
            {
                new ShowcaseKeyHint("Z", "Bật / Tắt đèn bàn xưởng Kenji & đèn hộp cáp tháp vô tuyến"),
                new ShowcaseKeyHint("T", "Đổi pha bầu trời để quan sát cụm phế tích dưới ánh đêm Địa Cầu & bình minh"),
                new ShowcaseKeyHint("Chuột phải", "Xoay quanh cụm 5 di tích: Cư trú, Tháp vô tuyến, Xưởng Kenji, Pin gãy, Mảnh vỡ"),
            };

            return new ShowcaseExhibitDefinition(
                ShowcaseExhibitId.RuinsCorner,
                numberKey: 10,
                titleVi: "Góc phế tích căn cứ cũ (Ruins Corner)",
                subtitleVi: "Cụm 5 di tích tiền trạm: Mô-đun cư trú (bảng khóa khí CRT), Tháp vô tuyến, Xưởng Kenji, Dàn pin gãy và Mảnh vỡ.",
                positionOffset: new Vector3(0f, 0f, -24f),
                yawDegrees: 170f,
                orbitPivotOffset: new Vector3(0f, 2.30f, -0.50f),
                defaultOrbitYaw: 162f,
                defaultOrbitPitch: 14f,
                defaultOrbitDistance: 17.5f,
                minOrbitDistance: 6.0f,
                maxOrbitDistance: 32.0f,
                nodes: Array.Empty<ShowcaseNodeRule>(),
                keyHints: hints);
        }
    }
}
