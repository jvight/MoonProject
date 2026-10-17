using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Automation;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Night-time look checks for the art under the game's kind of light (moonlight, indigo ambient, warm spot and
    /// point lights, bloom), rendered through <see cref="CaptureAutomation.CapturePoses"/>. Complements the neutral
    /// turntables: this is where emission, spot and point light response and far readability are judged.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Art.Editor.ArtLightingPreview.Capture
    ///     [--arg scene=rover|base|towers|relics|shadow|grit|friends|workshop|bell|bellbroken|pickups|bellcorner|
    ///                  warmpoints|relaysbroken|relayslit|sites|sitesclean|salvagebits|kit|kitmain|home|homeclean|
    ///                  builtfor07|shelves|focuslens|orangebeads|containers|scan|fragments|scanplaces|derelicts|wake_dark|wake_home|wake_bay|wake_chart|wake_pads|lab_dark|lab_lit]
    /// </code>
    /// (default rover; a ';'-separated list captures several scenes in one run)
    /// rover: 07 at rest (half-lidded, head lowered, wing ajar) with its headlamp. base: the habitat lander and the
    /// L3 tower on its anchor, its lamp sockets lit, 07 coming home. towers: L1-L3 side by side seen from 15 m and
    /// 55 m. relics: the six relics in the lab's keepsake cubbies under their reading lamps, and in a row from 15 m. shadow: 07's cast
    /// shadow under a low (30 degree) earthlight from its side, seen from the gameplay camera's height. grit: a
    /// dense field of <see cref="RockStyle.Grit"/> pebbles batched into one mesh around 07. friends: Tilly
    /// hovering beside 07 (3/3 part lamps lit), and broken on the dust among her amber parts (1/3 lit), near and
    /// from 30 m. The base scene also seats Tilly on her lander perch. workshop: Kenji's Rover Bay on the lander's
    /// WorkshopAnchor with its lamps and sign lit, 07 on its turntable wearing the Hover-Jump coils glowing as if
    /// charging.
    /// bell: Bell standing with her dial and 4/4 lamps lit beside 07 for scale, and a second Bell caught mid-dance
    /// (lid lifted, needle swept, a foot tapping) to prove the pivots, near and from 15 m and 30 m. bellbroken: Bell
    /// tipped back against a canyon wall, dark, with Ro's log cache, the Vol. 1 tape and her three parts in the dust,
    /// 07 arriving. pickups: the three parts, the three tapes and the cache on the dust, and the tape rack with the
    /// tapes in slots 0-2. bellcorner: the base with Bell on the L3 tower's BellCorner, her rack on its anchor, 07
    /// parked at her dial, the tower's upgrade pad ring and her 2.5 m clear circle drawn on the dust. warmpoints: the
    /// glowing cast inside Main.unity's own earthlight, fog and grade, 07's eye at three linear glow levels (pillar 6,
    /// "warm points in a cold field"). relaysbroken / relayslit: a relay mast on each of World's relay anchors in
    /// Main.unity, dark and leaning or restored and lit, from the spawn first frame, from the base and at each pad.
    /// sites / sitesclean: the five salvage sites on World's site anchors in Main.unity, whole or picked clean to their
    /// skeletons, from the way in, the side and 30 m, Kestrel-3 from the base, the depot close. salvagebits: the three
    /// material bundles on the dust beside 07. kit: 07 bare, with each crafted
    /// kit piece (lamp bar lit, drums glowing as if boosting, the cargo cradle carrying a relic), fully kitted with
    /// the friends' gifts, and "minute one versus hour five" side by side from the chase camera and at 30 m. kitmain:
    /// the same comparison in Main.unity under its own grade: minute one beside fully kitted at the spawn, travelling,
    /// with play's road lights and glow levels, from the chase camera and at 30 m, each 07 from its own chase camera,
    /// and the lamp bar close. wake_dark .. wake_pads: Main.unity's base waking step by step (M3-16), each stage on
    /// top of the last: dark (only the dock, 07's eye and the tower's pilot warm), home (windows, porch bulbs, the big
    /// porthole, the belly lamp and hatch rim, the HOME sign straightened, its cable lit; the lift still jammed), bay
    /// (shutter rolled up, sign, lamps and plotter lamp lit, its cable), chart (the lift restored: level on its stops,
    /// both cables taut, pad and rim lit; the light table and its cable lit, the porthole's first lamps), pads
    /// (capture-placed station pads lit at the tower, the bay and the lift); from the spawn, the owner's view from the
    /// dock side, the chase camera behind a 07 facing the lift, and the bay. lab_dark / lab_lit: Main.unity's lab
    /// asleep (only the earthlight through the portholes) and awake (its lamps, the board's, the recovery panel's and
    /// the chart's lit, a few sheets pinned): the owner's two frames (behind 07 at the base, the lander from the
    /// ladder side), then 07 on the raised turntable facing each station in turn, seen from the camera's ring rail. home:
    /// the base as Main.unity has it (its own grading), from the spawn view, the lander close and three-quarter, the
    /// tower, the shelf area, a 07 parked in front from the chase camera and close, and Kenji's Rover Bay (sign lit)
    /// from the front and the quarter; homeclean: the same with every Weather_* layer hidden, as restoration will
    /// leave it. builtfor07: Main.unity with the Rover Bay on the WorkshopAnchor and 07 parked on its turntable (lamps
    /// and sign lit, one arm lowered as if fitting), the true-size relics in a row beside 07 and a 1.75 m person (a
    /// capture-only reference), 07's spare wheel close, the tower port, the dock, the belly lift and the human-scale
    /// checks.
    /// Glows are lit with the linear MaterialPropertyBlock contract.
    /// </summary>
    public static class ArtLightingPreview
    {
        private const float RestingEyelidDegrees = 48f;
        private const float RestingHeadPitchDegrees = 9f;
        private const float RestingNeckYawDegrees = -12f;
        private const float RestingWingDegrees = 10f;
        private const float BaseLampIntensity = 3f;
        private const float BaseLampRange = 7f;

        /// <summary>Gameplay's tower upgrade pad (RadioTowerTuning): centre ahead of the tower, radius.</summary>
        private const float TowerPadOffset = 3.4f;
        private const float TowerPadRadius = 2.4f;

        /// <summary>
        /// Where 07 stops to work the tower's service port (metres ahead of the tower), and how far the hatch swings.
        /// </summary>
        private const float TowerServiceStop = 2.9f;

        private const float OpenHatchYaw = -70f;

        /// <summary>How far the builtfor07 capture raises the bay's floor arm out of its pit.</summary>
        private const float FloorLiftShown = 0.18f;

        /// <summary>Spacing of the kit scene's 07s, wide enough that each chase shot frames one rover.</summary>
        private const float KitSpacing = 9f;

        /// <summary>How far apart kitmain parks minute one and the fully kitted 07, side by side.</summary>
        private const float KitPairGap = 4f;

        // The game's default chase camera (RoverCameraTuning asset): distance, pitch, look-at height and lens; and the
        // spawn heading (RoverTuning) both kitmain 07s face, so Earth and The Peak sit ahead of them.
        private const float ChaseDistance = 7.5f;

        // The charge-up scenes: full lock pushes the cyan toward white; the warm-emitter window keeps a faint trace.
        private const string ChargeScenePrefix = "charge_";
        private const float ChargeLockGlow = 2.4f;
        private const float ChargeWarmGlow = 0.3f;
        private static readonly string[] ChargeStates = { "rest", "s1", "s2", "lock", "warm" };
        private const int WarmState = 4;
        private const float ChasePitch = 16f;
        private const float ChaseTargetHeight = 1.1f;
        private const float ChaseFov = 52f;
        private const float ChaseQuarterYaw = 35f;

        // The lab's station views look over 07's shoulder: this far round the ring rail from straight behind it.
        private const float ShoulderYaw = 30f;
        private const float FarDistance = 30f;
        private const float SpawnYaw = 355f;

        // 07 travelling as play poses it (RoverCharacterTuning): the head a little up, the lid at its active droop.
        private const float TravelHeadPitchDegrees = -6f;
        private const float ActiveEyelidDegrees = 24f;

        // Play's road light (RoverRigTuning) and the Warm Headlamp's from the lamp bar's middle glass (KitSettings),
        // neither casting shadows; the eye's small glow light (RoverCharacterTuning).
        private const float RoadLightIntensity = 35f;
        private const float RoadLightRange = 20f;
        private const float RoadLightAngle = 62f;
        private const float RoadLightInnerAngle = 30f;
        private const float WarmLightIntensity = 42f;
        private const float WarmLightRange = 24f;
        private const float WarmLightAngle = 90f;
        private const float WarmLightInnerAngle = 48f;
        private const float EyeLightIntensity = 0.8f;
        private const float EyeLightRange = 1.6f;

        // Play's glow levels (KitSettings): the lamp bar's glasses, and the drum bands idling between boosts.
        private const float LampBarGlow = 0.7f;
        private const float DrumIdleGlow = 0.05f;

        private static readonly Color WarmLightColor = new Color(1f, 0.62f, 0.33f);

        /// <summary>The base's wake stages the wake_* scenes capture, each on top of the last (M3-16).</summary>
        private static readonly string[] WakeStages = { "dark", "home", "bay", "chart", "pads" };

        /// <summary>The ground Bell keeps clear for her dance and for 07 parking at her dial.</summary>
        private const float BellClearRadius = 2.5f;

        /// <summary>Where the warmpoints cast stands in Main.unity: open dust well away from the base.</summary>
        private static readonly Vector3 WarmStage = new Vector3(30f, 0f, -30f);

        /// <summary>World's relay.0..3 anchors for the default seed (M3-06 anchor table); each faces home.</summary>
        private static readonly Vector3[] RelayAnchors =
        {
            new Vector3(62.63f, 1.68f, 92.85f), new Vector3(-204.85f, 7.97f, -74.56f),
            new Vector3(268.1f, 5.74f, 74.49f), new Vector3(456.57f, 36.35f, -16.66f),
        };

        /// <summary>
        /// World's salvage site anchors for the default seed (M3-13 table), in SiteModelBuilder.Ids order.
        /// </summary>
        private static readonly Vector3[] SiteAnchors =
        {
            new Vector3(-38.89f, -0.88f, 38.89f), new Vector3(-12.9f, 3.77f, 184.55f),
            new Vector3(136.51f, 2.68f, -57.95f), new Vector3(10.89f, 2.07f, -124.52f),
            new Vector3(335.96f, 9.29f, 98.65f),
        };

        /// <summary>The site anchors' Forward: the way 07 approaches, away from home.</summary>
        private static readonly Vector3[] SiteForwards =
        {
            new Vector3(-0.71f, 0f, 0.71f), new Vector3(0.34f, 0f, 0.94f), new Vector3(0.92f, 0f, -0.39f),
            new Vector3(0.09f, 0f, -1f), new Vector3(0.31f, 0f, -0.95f),
        };

        public static void Capture()
        {
            BatchRunner.Run(nameof(ArtLightingPreview), args =>
            {
                string[] scenes = args.GetList("scene");
                Material material = PaletteAssetBuilder.LoadMaterial();
                string output = Path.Combine(args.OutputDirectory, "art_preview");
                bool captured = true;
                foreach (string scene in scenes.Length > 0 ? scenes : new[] { "rover" })
                {
                    captured &= CaptureScene(scene, material, output);
                }

                return captured;
            });
        }

        /// <summary>Stages <paramref name="scene"/> in a fresh scene and renders it; false if unknown.</summary>
        private static bool CaptureScene(string scene, Material material, string output)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temporary = new TemporaryObjects();
            try
            {
                CameraPoseSet poses;
                switch (scene)
                {
                    case "rover":
                        NightSetting(material, temporary, 40f, 3.5f);
                        poses = RoverScene(temporary);
                        break;
                    case "base":
                        NightSetting(material, temporary, 70f, 10f);
                        poses = BaseScene(temporary);
                        break;
                    case "towers":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = TowersScene(temporary);
                        break;
                    case "focuslens":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = FocusLensScene(temporary);
                        break;
                    case "orangebeads":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = OrangeBeadsScene(temporary);
                        break;
                    case "scanplaces":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = ScanPlacesScene(temporary);
                        break;
                    case "derelicts":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = DerelictsScene(temporary);
                        break;
                    case "fragments":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = FragmentsScene(temporary);
                        break;
                    case "scan":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = ScanScene(temporary);
                        break;
                    case "containers":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = ContainersScene(temporary);
                        break;
                    case "shelves":
                        NightSetting(material, temporary, 90f, 12f);
                        poses = ShelvesScene(temporary);
                        break;
                    case "relics":
                        NightSetting(material, temporary, 40f, 8f);
                        poses = RelicsScene(temporary);
                        break;
                    case "shadow":
                        NightSetting(material, temporary, 40f, 8f);
                        poses = ShadowScene(temporary);
                        break;
                    case "friends":
                        NightSetting(material, temporary, 50f, 12f);
                        poses = FriendsScene(temporary);
                        break;
                    case "workshop":
                        NightSetting(material, temporary, 70f, 16f);
                        poses = WorkshopScene(temporary);
                        break;
                    case "grit":
                        NightSetting(material, temporary, 40f, 9f);
                        poses = GritScene(material, temporary);
                        break;
                    case "bell":
                        NightSetting(material, temporary, 50f, 7f);
                        poses = BellScene(temporary);
                        break;
                    case "bellbroken":
                        NightSetting(material, temporary, 50f, 9f);
                        poses = BellBrokenScene(material, temporary);
                        break;
                    case "pickups":
                        NightSetting(material, temporary, 40f, 8f);
                        poses = PickupsScene(temporary);
                        break;
                    case "bellcorner":
                        NightSetting(material, temporary, 80f, 18f);
                        poses = BellCornerScene(material, temporary);
                        break;
                    case "warmpoints":
                        poses = WarmPointsScene(material, temporary);
                        break;
                    case "relaysbroken":
                        poses = RelaysScene(temporary, false);
                        break;
                    case "relayslit":
                        poses = RelaysScene(temporary, true);
                        break;
                    case "sites":
                        poses = SitesScene(temporary, false);
                        break;
                    case "sitesclean":
                        poses = SitesScene(temporary, true);
                        break;
                    case "salvagebits":
                        NightSetting(material, temporary, 40f, 8f);
                        poses = SalvageBitsScene(temporary);
                        break;
                    case "kit":
                        NightSetting(material, temporary, 90f, 40f);
                        poses = KitScene(temporary);
                        break;
                    case "kitmain":
                        poses = KitMainScene(temporary);
                        break;
                    case "wake_dark":
                    case "wake_home":
                    case "wake_bay":
                    case "wake_chart":
                    case "wake_pads":
                        poses = WakeScene(temporary, Array.IndexOf(WakeStages, scene.Substring(5)));
                        break;
                    case "lab_dark":
                    case "lab_lit":
                        poses = LabScene(temporary, scene == "lab_lit", output, "night_" + scene);
                        break;
                    case "home":
                        poses = HomeScene(temporary, true);
                        break;
                    case "homeclean":
                        poses = HomeScene(temporary, false);
                        break;
                    case "builtfor07":
                        poses = BuiltFor07Scene(material, temporary);
                        break;
                    case string charge when charge.StartsWith(ChargeScenePrefix, StringComparison.Ordinal):
                        NightSetting(material, temporary, 90f, 12f);
                        poses = ChargeUpScene(temporary, charge);
                        break;
                    default:
                        Debug.LogError($"ArtLightingPreview: unknown scene '{scene}' " +
                            "(rover|base|towers|relics|shadow|grit|friends|workshop|bell|bellbroken|pickups|" +
                            "bellcorner|warmpoints|relaysbroken|relayslit|sites|sitesclean|salvagebits|kit|kitmain|" +
                            "home|homeclean|builtfor07|wake_dark|wake_home|wake_bay|wake_chart|wake_pads|" +
                            "lab_dark|lab_lit).");
                        return false;
                }

                return CaptureAutomation.CapturePoses(poses, output, "night_" + scene) > 0;
            }
            finally
            {
                temporary.Dispose();
            }
        }

        /// <summary>Dust floor, rocks from <paramref name="clearRadius"/> outwards, moonlight, bloom.</summary>
        internal static void NightSetting(Material material, TemporaryObjects temporary, float floorRadius,
            float clearRadius)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = (Color)Palette.Get(PaletteSwatch.SkyHorizon) * 0.9f;
            RenderSettings.ambientEquatorColor = (Color)Palette.Get(PaletteSwatch.DustShadow) * 0.55f;
            RenderSettings.ambientGroundColor = Palette.Get(PaletteSwatch.SkyTop);

            var ground = new LowPolyMeshBuilder(64);
            ground.Prism(Place.At(0f, -0.1f, 0f), floorRadius, 0.2f, 24, PaletteSwatch.DustMid);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * 2.4f;
                float distance = clearRadius + i * 0.9f;
                var at = new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                RockGenerator.Build(ground, 100 + i, 0.4f + (i % 4) * 0.45f, (RockStyle)(i % 5), Place.At(at));
            }

            temporary.Add(MeshObject("Ground", ground.ToMesh("PreviewGround"), material, temporary));

            Light moon = NewLight("Moonlight", LightType.Directional, new Color(0.72f, 0.74f, 1f), 0.55f);
            moon.transform.rotation = Quaternion.Euler(32f, 140f, 0f);
            moon.shadows = LightShadows.Soft;
            RenderSettings.sun = moon;
            temporary.Add(moon.gameObject);
            temporary.Add(NewVolume(temporary));
        }

        private static CameraPoseSet RoverScene(TemporaryObjects temporary)
        {
            Transform rover = Rover(temporary, Vector3.zero, 0f);
            Light baseLamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 4f);
            baseLamp.transform.position = new Vector3(-2.4f, 1.7f, -1.2f);
            baseLamp.range = BaseLampRange;
            baseLamp.shadows = LightShadows.Soft;
            temporary.Add(baseLamp.gameObject);
            temporary.Add(rover.gameObject);
            return Poses(
                Pose("hero", new[] { 2.6f, 1.25f, 3.4f }, new[] { 0f, 0.85f, 0.2f }, 40f),
                Pose("face", new[] { 0.55f, 1.3f, 1.75f }, new[] { 0f, 1.25f, 0.6f }, 35f),
                Pose("chase", new[] { 1.2f, 3.2f, -6.2f }, new[] { 0f, 0.8f, 1.5f }, 50f),
                Pose("side", new[] { -5f, 1.1f, 0.4f }, new[] { 0f, 0.7f, 0.2f }, 35f),
                Pose("far30m", new[] { 6f, 9f, -28f }, new[] { 0f, 0.7f, 0f }, 50f));
        }

        private static CameraPoseSet BaseScene(TemporaryObjects temporary)
        {
            GameObject lander = Instantiate(BaseModelBuilder.LanderName, temporary);
            Instantiate(BaseModelBuilder.TowerPrefix + "3", temporary).transform.position =
                BaseModelBuilder.TowerAnchor;
            for (int i = 0; i < 4; i++)
            {
                Transform socket = Descendant(lander.transform, "LampSocket_" + i);
                Light lamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp),
                    BaseLampIntensity);
                lamp.transform.position = socket.position;
                lamp.range = BaseLampRange;
                lamp.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
                temporary.Add(lamp.gameObject);
            }

            temporary.Add(Rover(temporary, new Vector3(0.4f, 0f, 11f), 185f).gameObject);
            Transform perch = Descendant(lander.transform, "FriendSocket_tilly");
            GameObject tilly = Instantiate(FriendModelBuilder.TillyName, temporary, ArtPaths.FriendFolder);
            tilly.transform.SetPositionAndRotation(perch.position, perch.rotation * Quaternion.Euler(0f, -30f, 0f));
            return Poses(
                Pose("perch", new[] { 3.8f, 4.8f, 6f }, new[] { 1.6f, 4.1f, 2.8f }, 40f),
                Pose("approach", new[] { 4f, 6f, 28f }, new[] { 0f, 3.5f, 0f }, 45f),
                Pose("homecoming", new[] { 4.5f, 3.6f, 16f }, new[] { 0.5f, 2.4f, 4f }, 50f),
                Pose("belly", new[] { -1.5f, 1.4f, 8.5f }, new[] { 0f, 2f, 0f }, 50f),
                Pose("porthole", new[] { -8f, 5.5f, -7f }, new[] { -2f, 5.2f, -2f }, 45f),
                Pose("far55m", new[] { -18f, 9f, 52f }, new[] { -2f, 4f, 0f }, 40f));
        }

        private static CameraPoseSet TowersScene(TemporaryObjects temporary)
        {
            var towers = new Transform[3];
            for (int level = 1; level <= 3; level++)
            {
                towers[level - 1] = Instantiate(BaseModelBuilder.TowerPrefix + level, temporary).transform;
                towers[level - 1].position = new Vector3((level - 2) * 6f, 0f, 0f);
                Light lamp = NewLight("TowerLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 2f);
                lamp.transform.position = new Vector3((level - 2) * 6f + 1.2f, 1.6f, 1.6f);
                lamp.range = 6f;
                temporary.Add(lamp.gameObject);
            }

            // Power climbing (feel-pass §8.2): L1 offline with its plaque lit, L2 two rings up, L3 every ring.
            SetGlow(towers[0], BaseModelBuilder.OfflineLampName, new[] { string.Empty }, 1f);
            SetGlow(towers[1], BaseModelBuilder.TowerLightsName, new[] { string.Empty }, 1f);
            SetGlow(towers[1], BaseModelBuilder.TowerLightRingPrefix, new[] { "1" }, 1f);
            SetGlow(towers[2], BaseModelBuilder.TowerLightsName, new[] { string.Empty }, 1f);
            SetGlow(towers[2], BaseModelBuilder.TowerLightRingPrefix, new[] { "1", "2", "3" }, 1f);
            Vector3 plaque = Descendant(towers[0], BaseModelBuilder.OfflinePlaqueName).position;

            return Poses(
                Pose("near15m", new[] { 0f, 4f, 15f }, new[] { 0f, 4.5f, 0f }, 50f),
                Pose("far55m", new[] { 8f, 8f, 55f }, new[] { 0f, 4f, 0f }, 30f),
                Pose("plaque", new[] { plaque.x + 0.6f, plaque.y + 0.5f, plaque.z + 2.2f },
                    new[] { plaque.x, plaque.y, plaque.z }, 35f));
        }

        private static CameraPoseSet FocusLensScene(TemporaryObjects temporary)
        {
            Transform charged = Rover(temporary, Vector3.zero, 160f);
            temporary.Add(charged.gameObject);
            Mount(temporary, RoverModelBuilder.FocusLensName, Descendant(charged, RoverModelBuilder.LensSocketName));
            SetGlow(Descendant(charged, RoverModelBuilder.FocusLensName), RoverModelBuilder.FocusLensGlassName,
                new[] { string.Empty }, 1f);
            PoseCharge(charged, ChargeStates.Length - 2);
            for (int i = 5; i < RoverModelBuilder.HatchCrates; i++)
            {
                Descendant(charged, RoverModelBuilder.HatchCratePrefix + i).gameObject.SetActive(false);
            }
            PoseCharge(Rover(temporary, new Vector3(-2.4f, 0f, -0.6f), 120f), 1);
            Transform dark = Rover(temporary, new Vector3(2.4f, 0f, -0.6f), 200f);
            temporary.Add(dark.gameObject);
            Mount(temporary, RoverModelBuilder.FocusLensName, Descendant(dark, RoverModelBuilder.LensSocketName));
            PoseCharge(dark, 0);
            Transform eyeNode = Descendant(charged, "Eye");
            Vector3 eye = eyeNode.position;
            Vector3 front = eye + eyeNode.forward * 0.9f + eyeNode.right * 0.25f + Vector3.up * 0.15f;
            Vector3 above = eye + eyeNode.forward * 0.7f - eyeNode.right * 0.6f + Vector3.up * 0.75f;
            Vector3 behind = charged.position - charged.forward * 1.5f + charged.right * 0.7f + Vector3.up * 1.9f;
            return Poses(
                ChasePose("run_chase", charged.position, charged.rotation, ChaseDistance, 0f),
                ChasePose("run_chase_near", charged.position, charged.rotation, ChaseDistance * 0.4f, 0f),
                Pose("run_top", new[] { charged.position.x, 4.6f, charged.position.z - 0.01f },
                    new[] { charged.position.x, 0.9f, charged.position.z }, 30f),
                Pose("run_back", new[] { behind.x, behind.y, behind.z },
                    new[] { charged.position.x, 1.05f, charged.position.z }, 45f),
                Pose("lens_close", new[] { front.x, front.y, front.z }, new[] { eye.x, eye.y, eye.z }, 35f),
                Pose("brow_three_quarter", new[] { above.x, above.y, above.z },
                    new[] { eye.x, eye.y + 0.12f, eye.z }, 40f),
                Pose("pair_chase", new[] { 1.2f, 2.2f, -4.5f }, new[] { 1.2f, 0.9f, 0f }, 45f));
        }

        /// <summary>
        /// Poses 07's charge-up in <paramref name="state"/> (an index into <see cref="ChargeStates"/>): stages filled up
        /// to it with their sleeves slid forward and lit, the conduits flowing, full lock pushed toward white with the
        /// ring lit, or the warm trace on the conduits and the ring alone.
        /// </summary>
        private static void PoseCharge(Transform rover, int state)
        {
            bool warm = state == WarmState;
            int filled = warm ? 0 : state;
            float hot = filled == RoverModelBuilder.ChargeStages ? ChargeLockGlow : 1f;
            foreach (Transform node in rover.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == RoverModelBuilder.ConduitGlowName)
                {
                    Lit(node, warm ? ChargeWarmGlow : filled > 0 ? hot : 0f);
                }
                else if (node.name == RoverModelBuilder.LockGlowName)
                {
                    Lit(node, warm ? ChargeWarmGlow : filled == RoverModelBuilder.ChargeStages ? ChargeLockGlow : 0f);
                }
                else if (node.name.StartsWith(RoverModelBuilder.StageGlowPrefix, StringComparison.Ordinal))
                {
                    bool full = node.name[RoverModelBuilder.StageGlowPrefix.Length] - '0' <= filled;
                    Lit(node, full ? hot : 0f);
                    node.localPosition = RoverModelBuilder.SleeveRest +
                                         Vector3.forward * (full ? RoverModelBuilder.ChargeSleeveTravel : 0f);
                }
            }
        }

        private static void Mount(TemporaryObjects temporary, string prefab, Transform socket)
        {
            Transform part = Instantiate(prefab, temporary, ArtPaths.RoverFolder).transform;
            part.SetParent(socket, false);
            part.localPosition = Vector3.zero;
            part.localRotation = Quaternion.identity;
        }

        private static CameraPoseSet OrangeBeadsScene(TemporaryObjects temporary)
        {
            var field = new[]
            {
                new Vector3(0f, 0f, 6f), new Vector3(3.2f, 0f, 8.5f), new Vector3(-2.8f, 0f, 9f),
                new Vector3(1.2f, 0f, 12f), new Vector3(-0.6f, 0f, 15f),
            };
            for (int i = 0; i < field.Length; i++)
            {
                GameObject patch = Instantiate(
                    PropModelBuilder.OrangeBeadPatchPrefix + (i % PropModelBuilder.OrangeBeadPatchVariants), temporary,
                    ArtPaths.PropFolder);
                patch.transform.SetPositionAndRotation(field[i], Quaternion.Euler(0f, i * 47f, 0f));
            }

            string[] bundles =
            {
                SiteModelBuilder.BundleName(SalvagePieceMaterial.Metal),
                SiteModelBuilder.BundleName(SalvagePieceMaterial.Wiring),
                SiteModelBuilder.BundleName(SalvagePieceMaterial.Optics), SiteModelBuilder.OrangeBeadsBundleName,
            };
            for (int i = 0; i < bundles.Length; i++)
            {
                GameObject bundle = Instantiate(bundles[i], temporary, ArtPaths.PickupFolder);
                float lift = bundle.transform.position.y - RendererBounds(bundle).min.y;
                bundle.transform.SetPositionAndRotation(new Vector3((i - 1.5f) * 0.55f, lift, 2.2f),
                    Quaternion.Euler(0f, 20f, 0f));
            }

            temporary.Add(Rover(temporary, new Vector3(2.6f, 0f, 3.4f), -20f).gameObject);
            return Poses(
                Pose("bundles", new[] { 0.1f, 1.2f, 0.6f }, new[] { 0f, 0.15f, 2.2f }, 45f),
                Pose("field_near", new[] { 1.5f, 2.2f, -1.5f }, new[] { 0f, 0f, 8f }, 50f),
                Pose("field_far30m", new[] { 6f, 5f, -20f }, new[] { 0f, 0f, 10f }, 40f));
        }

        /// <summary>Stacks crates on a supply shelf the way gameplay does: one material per slot, 1-3 high.</summary>
        private static void StockShelf(Transform shelf, TemporaryObjects temporary)
        {
            IReadOnlyList<string> materials = BaseModelBuilder.ShelfMaterials;
            for (int slot = 0; slot < BaseModelBuilder.ShelfSlots; slot++)
            {
                string name = BaseModelBuilder.ShelfSlotPrefix +
                              slot.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                Transform anchor = Descendant(shelf, name);
                int stacked = 1 + slot * 7 % BaseModelBuilder.ShelfStackHeight;
                for (int level = 0; level < stacked; level++)
                {
                    Transform crate = Instantiate(BaseModelBuilder.ShelfCratePrefix + materials[slot % materials.Count],
                        temporary).transform;
                    crate.SetParent(anchor.GetChild(level), false);
                }
            }
        }

        /// <summary>
        /// 07's charge-up (0.5.5 owner feedback §5.4, "Cells"): <c>charge_&lt;state&gt;</c>, 07 with the Focus Lens
        /// fitted, posed <c>rest</c>; <c>s1</c>, <c>s2</c> (stages filled, sleeves slid forward, the conduits flowing);
        /// <c>lock</c> (all three, everything pushed toward white, the ring lit); or <c>warm</c> (the trace after
        /// firing). Seen from the chase camera at 7.5 m and 30 m, from above and from a close three-quarter.
        /// </summary>
        private static CameraPoseSet ChargeUpScene(TemporaryObjects temporary, string scene)
        {
            int state = Array.IndexOf(ChargeStates, scene.Substring(ChargeScenePrefix.Length));
            if (state < 0)
            {
                throw new ArgumentException($"ArtLightingPreview: '{scene}' is not charge_<rest|s1|s2|lock|warm>.");
            }

            Transform rover = Rover(temporary, Vector3.zero, 160f);
            temporary.Add(rover.gameObject);
            Mount(temporary, RoverModelBuilder.FocusLensName, Descendant(rover, RoverModelBuilder.LensSocketName));
            PoseCharge(rover, state);
            Transform eye = Descendant(rover, "Eye");
            Vector3 quarter = rover.position - rover.forward * 1.25f + rover.right * 1.05f + Vector3.up * 1.45f;
            return Poses(
                ChasePose("chase7m", rover.position, rover.rotation, ChaseDistance, 0f),
                ChasePose("chase30m", rover.position, rover.rotation, FarDistance, 0f),
                Pose("top", new[] { rover.position.x, 4.6f, rover.position.z - 0.01f },
                    new[] { rover.position.x, 0.9f, rover.position.z }, 30f),
                Pose("quarter", new[] { quarter.x, quarter.y, quarter.z },
                    new[] { eye.position.x * 0.3f + rover.position.x * 0.7f, 0.95f, rover.position.z }, 45f));
        }

        private static void Lit(Transform node, float intensity)
        {
            var block = new MaterialPropertyBlock();
            block.SetVector("_EmissionColor", new Vector4(intensity, intensity, intensity, 1f));
            node.GetComponent<Renderer>().SetPropertyBlock(block);
        }

        /// <summary>
        /// 0.5.5 slice C's places for finds: the coil crate (opened), Ro's tape case, the junction box and the supply pod
        /// (sealed, their seams lit, the pod's beacon warm) in a row; Tilly's sled with its clamp and pod seals lit; the
        /// garage's three relic pockets against its wreck; and the relay masts' cabinets, dormant (sealed) and restored.
        /// </summary>
        private static CameraPoseSet ContainersScene(TemporaryObjects temporary)
        {
            string[] row =
            {
                LootContainerBuilder.CoilCrateName, LootContainerBuilder.TapeCaseName,
                LootContainerBuilder.JunctionBoxName, LootContainerBuilder.SupplyPodName,
            };
            for (int i = 0; i < row.Length; i++)
            {
                Transform container = Instantiate(row[i], temporary, ArtPaths.PropFolder).transform;
                container.SetPositionAndRotation(new Vector3(-3f + i * 1.6f, 0f, 0f), Quaternion.Euler(0f, 200f, 0f));
                foreach (Transform node in container.GetComponentsInChildren<Transform>())
                {
                    if (node.name == LootContainerBuilder.SealName || node.name == LootContainerBuilder.GlowName)
                    {
                        Lit(node, i == 0 ? 0f : 1f);
                    }
                }

                if (i == 0)
                {
                    Descendant(container, LootContainerBuilder.LidName).localRotation =
                        Quaternion.Euler(LootContainerBuilder.LidOpenDegrees, 0f, 0f);
                }
            }

            Transform sled = Instantiate(LootContainerBuilder.TillySledName, temporary, ArtPaths.PropFolder).transform;
            sled.SetPositionAndRotation(new Vector3(0f, 0f, 4f), Quaternion.Euler(0f, 160f, 0f));
            foreach (Transform node in sled.GetComponentsInChildren<Transform>())
            {
                if (node.name == LootContainerBuilder.SealName)
                {
                    Lit(node, 1f);
                }
            }

            Transform garage = Instantiate(SiteModelBuilder.SiteName("garage"), temporary, ArtPaths.SitesFolder)
                .transform;
            garage.position = new Vector3(14f, 0f, 0f);
            Transform pocket = Descendant(garage, SiteModelBuilder.RelicPocketPrefix + "rubber_duck");
            Transform spare = Instantiate(LootContainerBuilder.SpareCrateName, temporary, ArtPaths.PropFolder)
                .transform;
            spare.SetParent(Descendant(garage, SiteModelBuilder.SpareCrateSocketName), false);
            Lit(Descendant(spare, LootContainerBuilder.SealName), 1f);
            for (int i = 0; i < 2; i++)
            {
                SeatFragment(temporary, Descendant(garage, SiteModelBuilder.FragmentSocketPrefix + "CargoCradle_" + i),
                    "CargoCradle", i, 0f, 0f);
            }

            foreach (bool broken in new[] { true, false })
            {
                Transform mast = Instantiate(broken ? RelayModelBuilder.BrokenMastName : RelayModelBuilder.MastName,
                    temporary, ArtPaths.RelayFolder).transform;
                mast.position = new Vector3(broken ? -14f : -24f, 0f, 0f);
                Transform cabinet = Instantiate(LootContainerBuilder.CabinetName, temporary, ArtPaths.PropFolder)
                    .transform;
                cabinet.SetParent(Descendant(mast, RelayModelBuilder.CabinetSocketName), false);
                Lit(Descendant(cabinet, LootContainerBuilder.SealName), broken ? 1f : 0f);
                if (!broken)
                {
                    Descendant(cabinet, LootContainerBuilder.LidName).localRotation =
                        Quaternion.Euler(0f, LootContainerBuilder.LidOpenDegrees, 0f);
                }
            }

            Vector3 duck = pocket.position;
            return Poses(
                Pose("row", new[] { 0f, 1.6f, -3.4f }, new[] { -0.6f, 0.3f, 0f }, 50f),
                Pose("pod_mast", new[] { 4.5f, 2.2f, -6.5f }, new[] { 1.8f, 1.8f, 0f }, 45f),
                Pose("pod_far60m", new[] { 30f, 6f, -55f }, new[] { 1.8f, 2f, 0f }, 25f),
                Pose("sled", new[] { 1.6f, 1.5f, 1.6f }, new[] { 0f, 0.25f, 4f }, 45f),
                Pose("pocket_close", new[] { duck.x + pocket.forward.x * 1.4f, 0.9f, duck.z + pocket.forward.z * 1.4f },
                    new[] { duck.x, 0.1f, duck.z }, 45f),
                Pose("garage_pockets", new[] { 14f, 7f, -9f }, new[] { 14f, 0.5f, 0f }, 50f),
                Pose("spare_crate", new[] { 13.4f, 1.6f, 3.6f }, new[] { 12.3f, 1f, 5.5f }, 50f),
                Pose("cabinet_sealed", new[] { -10.9f, 1.5f, -0.5f }, new[] { -12.25f, 0.6f, -2.8f }, 45f),
                Pose("cabinet_open", new[] { -20.9f, 1.5f, -0.5f }, new[] { -22.25f, 0.6f, -2.8f }, 45f));
        }

        /// <summary>
        /// The fragments where they lie (0.5.5 §1.3): set on their sockets at the garage, the drill rig, Kestrel-3 and the
        /// crashed lander, on relay.2's hut shelf, and round the chasm lip's crashed jumper, brackets lit.
        /// </summary>
        private static CameraPoseSet ScanPlacesScene(TemporaryObjects temporary)
        {
            var poses = new List<CameraPose>();
            string[] sites = { "garage", "kestrel", "lander" };
            for (int s = 0; s < sites.Length; s++)
            {
                Transform site = Instantiate(SiteModelBuilder.SiteName(sites[s]), temporary, ArtPaths.SitesFolder).transform;
                site.position = new Vector3(s * 30f, 0f, 0f);
                (string blueprint, int count) = SiteModelBuilder.FragmentsAt(sites[s]);
                for (int i = 0; i < count; i++)
                {
                    (float progress, float dissolve) = InSituStages[(s + i) % InSituStages.Length];
                    SeatFragment(temporary, Descendant(site, SiteModelBuilder.FragmentSocketPrefix + blueprint + "_" + i),
                        blueprint, i, progress, dissolve);
                }

                Vector3 middle = Descendant(site, SiteModelBuilder.FragmentSocketPrefix + blueprint + "_0").position;
                Vector3 eye = middle + InSituViews[s];
                poses.Add(Pose("site_" + sites[s], new[] { eye.x, eye.y, eye.z },
                    new[] { middle.x, middle.y + 0.1f, middle.z }, 50f));
            }

            Transform jumper = Instantiate(PropModelBuilder.JumperWreckName, temporary, ArtPaths.PropFolder).transform;
            jumper.position = new Vector3(30f, 0f, 40f);
            for (int i = 0; i < 3; i++)
            {
                SeatFragment(temporary, Descendant(jumper, SiteModelBuilder.FragmentSocketPrefix + "HoverJump_" + i),
                    "HoverJump", i, InSituStages[i + 2].Progress, InSituStages[i + 2].Dissolve);
            }

            poses.Add(Pose("jumper", new[] { 32.5f, 2f, 44f }, new[] { 30f, 0.3f, 40f }, 50f));
            return Poses(poses.ToArray());
        }

        // Where each site's in-situ view looks from, relative to its first fragment: through the garage's open front at
        // Kenji's bench, into Kestrel-3's split back, into the lander's torn cargo module.
        private static readonly Vector3[] InSituViews =
        {
            new Vector3(1.9f, 0.8f, -1.6f), new Vector3(-0.4f, 1.1f, -2.3f), new Vector3(0.6f, 0.9f, -2.6f),
        };

        // The scan's stages, shown across the fragments in place: noticed, half swept, drawn, fading, unthreading.
        private static readonly (float Progress, float Dissolve)[] InSituStages =
        {
            (0f, 0f), (0.5f, 0f), (1f, 0f), (1f, 0.18f), (1f, 0.6f),
        };

        /// <summary>
        /// The robots left behind (0.5.5 §1.7), each where World anchors it with its fragments seated (noticed, brackets
        /// lit): 06 slumped against relay.2's hut with Ro's tape case by its bay, Mule face-down by its last lamp post,
        /// the haul sled bot stalled with its load, and the drone caught in its wire; each from a close three-quarter
        /// and from 20 m.
        /// </summary>
        private static CameraPoseSet DerelictsScene(TemporaryObjects temporary)
        {
            var poses = new List<CameraPose>();
            (string name, string blueprint, int count)[] hosts =
            {
                (DerelictBuilder.ElderName, "CellPack", 3), (DerelictBuilder.MuleName, "WarmHeadlamp", 2),
                (DerelictBuilder.HaulBotName, "BoostCoils", 3), (DerelictBuilder.DroneName, string.Empty, 0),
            };
            for (int h = 0; h < hosts.Length; h++)
            {
                Transform host = Instantiate(hosts[h].name, temporary, ArtPaths.PropFolder).transform;
                var at = new Vector3(h * 18f, 0f, 0f);
                host.SetPositionAndRotation(at, Quaternion.identity);
                for (int i = 0; i < hosts[h].count; i++)
                {
                    SeatFragment(temporary,
                        Descendant(host, SiteModelBuilder.FragmentSocketPrefix + hosts[h].blueprint + "_" + i),
                        hosts[h].blueprint, i, 0f, 0f);
                }

                if (hosts[h].name == DerelictBuilder.ElderName)
                {
                    Transform hut = Instantiate(RelayModelBuilder.HutName, temporary, ArtPaths.RelayFolder).transform;
                    hut.SetParent(Descendant(host, DerelictBuilder.HutSocketName), false);
                    Transform tape = Instantiate(LootContainerBuilder.TapeCaseName, temporary, ArtPaths.PropFolder)
                        .transform;
                    tape.SetParent(Descendant(host, DerelictBuilder.TapeSocketName), false);
                    Lit(Descendant(tape, LootContainerBuilder.SealName), 1f);
                }

                string tag = hosts[h].name.Substring(DerelictBuilder.Prefix.Length);
                float lift = h == 3 ? 1.2f : 0.4f;
                poses.Add(Pose("near_" + tag, new[] { at.x + 2.4f, 1.6f + lift, at.z + 2.8f },
                    new[] { at.x + 0.2f, lift, at.z }, 50f));
                poses.Add(Pose("far20m_" + tag, new[] { at.x + 9f, 5f, at.z + 18f }, new[] { at.x, lift, at.z }, 35f));
            }

            return Poses(poses.ToArray());
        }

        private static void SeatFragment(TemporaryObjects temporary, Transform socket, string blueprint, int index,
            float progress, float dissolve)
        {
            Transform fragment = Instantiate(FragmentBuilder.PrefabName(blueprint, index), temporary,
                ArtPaths.FragmentFolder).transform;
            fragment.SetPositionAndRotation(socket.position, socket.rotation);
            Transform brackets = Descendant(fragment, FragmentBuilder.BracketsName);
            brackets.gameObject.SetActive(progress <= 0f);
            Lit(brackets, 1f);
            var block = new MaterialPropertyBlock();
            block.SetFloat("_ScanProgress", progress);
            block.SetFloat("_Dissolve", dissolve);
            Descendant(fragment, FragmentBuilder.SolidName).GetComponent<Renderer>().SetPropertyBlock(block);
            Descendant(fragment, FragmentBuilder.LinesName).GetComponent<Renderer>().SetPropertyBlock(block);
        }

        /// <summary>All 18 fragments in rows by blueprint, as found (solid, brackets lit), for their shapes.</summary>
        private static CameraPoseSet FragmentsScene(TemporaryObjects temporary)
        {
            IReadOnlyList<(string Blueprint, int Count)> sets = FragmentBuilder.Sets;
            for (int row = 0; row < sets.Count; row++)
            {
                for (int i = 0; i < sets[row].Count; i++)
                {
                    Transform fragment = Instantiate(FragmentBuilder.PrefabName(sets[row].Blueprint, i), temporary,
                        ArtPaths.FragmentFolder).transform;
                    fragment.SetPositionAndRotation(new Vector3((i - 1f) * 1.8f, 0f, row * 1.7f),
                        Quaternion.Euler(0f, 200f, 0f));
                    Transform brackets = Descendant(fragment, FragmentBuilder.BracketsName);
                    brackets.gameObject.SetActive(true);
                    Lit(brackets, 1f);
                }
            }

            return Poses(
                Pose("rows_near", new[] { 0f, 2.6f, -3.2f }, new[] { 0f, 0.2f, 2.6f }, 55f),
                Pose("rows_far", new[] { 0f, 3.4f, 3.5f }, new[] { 0f, 0.2f, 9.5f }, 55f));
        }

        /// <summary>
        /// A scan's stages side by side (0.5.5 §1, D2), on a large and a medium fragment: noticed (brackets lit), half
        /// swept, fully drawn, the solid half faded, the lines half unthreaded, and the dust outline left behind; with
        /// 07 beside them for scale.
        /// </summary>
        private static CameraPoseSet ScanScene(TemporaryObjects temporary)
        {
            (float progress, float dissolve, bool brackets, bool dust)[] stages =
            {
                (0f, 0f, true, false), (0.5f, 0f, false, false), (1f, 0f, false, false), (1f, 0.18f, false, false),
                (1f, 0.6f, false, false), (1f, 1f, false, true),
            };
            string[] fragments = { FragmentBuilder.PrefabName("BoostCoils", 2), FragmentBuilder.PrefabName("CargoCradle", 0) };
            for (int row = 0; row < fragments.Length; row++)
            {
                for (int i = 0; i < stages.Length; i++)
                {
                    Transform fragment = Instantiate(fragments[row], temporary, ArtPaths.FragmentFolder).transform;
                    fragment.SetPositionAndRotation(new Vector3((i - 2.5f) * 1.9f, 0f, row * 2.4f),
                        Quaternion.Euler(0f, 200f, 0f));
                    var block = new MaterialPropertyBlock();
                    block.SetFloat("_ScanProgress", stages[i].progress);
                    block.SetFloat("_Dissolve", stages[i].dissolve);
                    Descendant(fragment, FragmentBuilder.SolidName).GetComponent<Renderer>().SetPropertyBlock(block);
                    Descendant(fragment, FragmentBuilder.LinesName).GetComponent<Renderer>().SetPropertyBlock(block);
                    Transform brackets = Descendant(fragment, FragmentBuilder.BracketsName);
                    brackets.gameObject.SetActive(stages[i].brackets);
                    Lit(brackets, 1f);
                    Transform dust = Descendant(fragment, FragmentBuilder.DustOutlineName);
                    dust.gameObject.SetActive(stages[i].dust);
                    var fade = new MaterialPropertyBlock();
                    fade.SetFloat("_Fade", 0.4f);
                    dust.GetComponent<Renderer>().SetPropertyBlock(fade);
                }
            }

            Transform scanning = Rover(temporary, new Vector3(0f, 0f, -2.6f), 0f);
            temporary.Add(scanning.gameObject);
            Mount(temporary, RoverModelBuilder.ScanFanName, Descendant(scanning, RoverModelBuilder.ScanFanSocketName));
            Lit(Descendant(scanning, RoverModelBuilder.ScanFanName), 0.6f);
            return Poses(
                Pose("stages", new[] { 0f, 3.2f, -6.5f }, new[] { 0f, 0.4f, 1.2f }, 55f),
                Pose("close", new[] { -3.2f, 1.5f, -2f }, new[] { -2.8f, 0.4f, 0.6f }, 50f));
        }

        private static CameraPoseSet ShelvesScene(TemporaryObjects temporary)
        {
            Transform lander = Instantiate(BaseModelBuilder.LanderName, temporary).transform;
            Transform shelves = Descendant(lander, BaseModelBuilder.ShelvesName);
            foreach (Transform shelf in shelves)
            {
                SetGlow(shelf, BaseModelBuilder.ShelfLampName, new[] { string.Empty }, 1f);
                Light lamp = NewLight("ShelfLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 1.2f);
                lamp.transform.position = Descendant(shelf, BaseModelBuilder.ShelfLampName).position;
                lamp.range = 3f;
                temporary.Add(lamp.gameObject);
                StockShelf(shelf, temporary);
            }

            GameObject site = Instantiate(SiteModelBuilder.SiteName("depot"), temporary, ArtPaths.SitesFolder);
            site.transform.position = new Vector3(-20f, 0f, 0f);
            Vector3 crate = Descendant(site.transform, SiteModelBuilder.CrewCrateName).position;
            SetGlow(site.transform, SiteModelBuilder.CrewCrateSealName, new[] { string.Empty }, 1f);
            Vector3 dock = lander.TransformPoint(ChargingDockMeshes.Anchor);
            return Poses(
                Pose("from_dock", new[] { dock.x, dock.y + 1.6f, dock.z }, new[] { 0f, 1.2f, 0f }, 45f),
                Pose("east_shelves", new[] { 7.5f, 1.6f, 1.5f }, new[] { 3f, 0.8f, 0f }, 50f),
                Pose("west_shelves", new[] { -7.5f, 1.6f, -1.5f }, new[] { -3f, 0.8f, 0f }, 50f),
                Pose("crew_crate", new[] { crate.x + 1.6f, crate.y + 1.4f, crate.z + 2.4f },
                    new[] { crate.x, crate.y + 0.3f, crate.z }, 45f),
                Pose("crate_site", new[] { -20f + 6f, 4f, 10f }, new[] { crate.x, 0.5f, crate.z }, 50f));
        }

        private static CameraPoseSet RelicsScene(TemporaryObjects temporary)
        {
            Transform lander = Instantiate(BaseModelBuilder.LanderName, temporary).transform;
            Transform shelf = Descendant(lander, BaseModelBuilder.StationPrefix + "Keepsakes");
            IReadOnlyList<string> ids = RelicModelBuilder.Ids;
            for (int i = 0; i < ids.Count; i++)
            {
                Transform slot = Descendant(shelf, BaseModelBuilder.SlotPrefix + i);
                GameObject relic = Instantiate(RelicModelBuilder.PrefabName(ids[i]), temporary, ArtPaths.RelicFolder);
                float lift = relic.transform.position.y - RendererBounds(relic).min.y;
                relic.transform.SetPositionAndRotation(slot.position + Vector3.up * lift, slot.rotation);
            }

            SetGlow(shelf, BaseModelBuilder.ReadingLampPrefix, 4, 1f);
            for (int i = 0; i < ids.Count; i++)
            {
                GameObject relic = Instantiate(RelicModelBuilder.PrefabName(ids[i]), temporary, ArtPaths.RelicFolder);
                float lift = relic.transform.position.y - RendererBounds(relic).min.y;
                relic.transform.SetPositionAndRotation(new Vector3((i - 2.5f) * 1.4f, lift, 9f),
                    Quaternion.Euler(0f, 20f, 0f));
            }

            WarmLamp(temporary, shelf.TransformPoint(new Vector3(0f, 0.7f, 0.6f)), 2f, 3.5f);
            Light lamp = NewLight("RowLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 3f);
            lamp.transform.position = new Vector3(0f, 2.6f, 7.6f);
            lamp.range = BaseLampRange;
            temporary.Add(lamp.gameObject);
            return Poses(
                Pose("shelf", Point(shelf.TransformPoint(new Vector3(0.15f, 0.55f, 2.1f))),
                    Point(shelf.TransformPoint(new Vector3(0f, -0.15f, -0.3f))), 55f),
                Pose("row", new[] { 0.5f, 1.6f, 12.5f }, new[] { 0f, 0.4f, 9f }, 50f),
                Pose("far15m", new[] { 3f, 4f, 24f }, new[] { 0f, 0.6f, 9f }, 40f));
        }

        private static CameraPoseSet ShadowScene(TemporaryObjects temporary)
        {
            Light moon = RenderSettings.sun;
            moon.transform.rotation = Quaternion.Euler(30f, -90f, 0f);
            moon.intensity = 0.9f;
            Transform rover = Rover(temporary, Vector3.zero, 0f);
            temporary.Add(rover.gameObject);
            return Poses(
                Pose("side", new[] { -2.2f, 6.5f, -5.5f }, new[] { -2.2f, 0f, 0f }, 50f),
                Pose("above", new[] { -1.4f, 5.5f, -0.6f }, new[] { -1.4f, 0f, 0.2f }, 45f),
                Pose("chase", new[] { 0.5f, 4f, -7f }, new[] { -1.5f, 0.3f, 0.5f }, 50f));
        }

        private static CameraPoseSet GritScene(Material material, TemporaryObjects temporary)
        {
            const int count = 400;
            var field = new LowPolyMeshBuilder(count * 20);
            var random = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2d);
                float distance = 1.8f + (float)random.NextDouble() * 7f;
                float size = 0.1f + (float)random.NextDouble() * 0.25f;
                var at = new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                RockGenerator.Build(field, i, size, RockStyle.Grit, Place.At(at));
            }

            temporary.Add(MeshObject("Grit", field.ToMesh("PreviewGrit"), material, temporary));
            temporary.Add(Rover(temporary, Vector3.zero, 0f).gameObject);
            return Poses(
                Pose("chase", new[] { 1.2f, 3.2f, -6.2f }, new[] { 0f, 0.8f, 1.5f }, 50f),
                Pose("close", new[] { 2.4f, 0.9f, 2.6f }, new[] { 1.6f, 0.1f, 4.2f }, 45f));
        }

        private static CameraPoseSet WorkshopScene(TemporaryObjects temporary)
        {
            GameObject lander = Instantiate(BaseModelBuilder.LanderName, temporary);
            GameObject tower = Instantiate(BaseModelBuilder.TowerPrefix + "2", temporary);
            tower.transform.position = BaseModelBuilder.TowerAnchor;
            Transform anchor = Descendant(lander.transform, "WorkshopAnchor");
            GameObject bay = Instantiate(BaseModelBuilder.RoverBayName, temporary);
            bay.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            LightBay(bay.transform, temporary);
            for (int i = 0; i < 4; i++)
            {
                Light baseLamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp),
                    BaseLampIntensity);
                baseLamp.transform.position = Descendant(lander.transform, "LampSocket_" + i).position;
                baseLamp.range = BaseLampRange;
                temporary.Add(baseLamp.gameObject);
            }

            Transform turntable = Descendant(bay.transform, "Turntable");
            Vector3 parked = turntable.position;
            Transform rover = Rover(temporary, parked, turntable.eulerAngles.y);
            temporary.Add(rover.gameObject);
            GameObject coils = Instantiate(RoverModelBuilder.HoverCoilsName, temporary, ArtPaths.RoverFolder);
            Transform socket = Descendant(rover, "CoilSocket");
            coils.transform.SetParent(socket, false);
            SetGlow(coils.transform, "Glow_", new[] { "FL", "FR", "RL", "RR" }, 1.5f);
            for (int i = 0; i < 4; i++)
            {
                coils.transform.GetChild(i).localScale = new Vector3(1f, 0.75f, 1f);
            }

            return Poses(
                Pose("bay", new[] { 13.5f, 2.4f, 6.5f }, new[] { 12.5f, 1.2f, -2.3f }, 50f),
                Pose("coils", new[] { parked.x + 0.3f, 0.42f, parked.z + 2.3f },
                    new[] { parked.x, 0.33f, parked.z }, 40f),
                Pose("homecoming", new[] { 4.5f, 3.6f, 14.5f }, new[] { 4f, 1.8f, 0f }, 55f),
                Pose("far40m", new[] { 32f, 12f, 30f }, new[] { 10f, 1.2f, -1f }, 35f));
        }

        private static CameraPoseSet FriendsScene(TemporaryObjects temporary)
        {
            temporary.Add(Rover(temporary, Vector3.zero, 20f).gameObject);
            GameObject tilly = Instantiate(FriendModelBuilder.TillyName, temporary, ArtPaths.FriendFolder);
            tilly.transform.SetPositionAndRotation(new Vector3(1.4f, 1.15f, 1.5f), Quaternion.Euler(0f, 20f, 0f));
            SetGlow(tilly.transform, "PartLamp_", 3, 1f);

            var site = new Vector3(-5f, 0f, 7f);
            GameObject broken = Instantiate(FriendModelBuilder.TillyBrokenName, temporary, ArtPaths.FriendFolder);
            broken.transform.SetPositionAndRotation(site, Quaternion.Euler(0f, -20f, 0f));
            SetGlow(broken.transform, "PartLamp_", 1, 1f);
            IReadOnlyList<string> parts = FriendModelBuilder.PartNames;
            for (int i = 0; i < parts.Count; i++)
            {
                GameObject part = Instantiate(parts[i], temporary, ArtPaths.FriendFolder);
                float angle = i * 2.1f + 0.4f;
                var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (2.2f + i * 0.9f);
                float lift = part.transform.position.y - RendererBounds(part).min.y;
                part.transform.SetPositionAndRotation(site + offset + Vector3.up * lift,
                    Quaternion.Euler(0f, i * 70f, 0f));
            }

            return Poses(
                Pose("pair", new[] { 2.4f, 1.6f, 4.2f }, new[] { 0.7f, 1.1f, 0.8f }, 40f),
                Pose("broken", new[] { -3.2f, 1.2f, 9.6f }, new[] { -5f, 0.2f, 7f }, 40f),
                Pose("broken30m", new[] { -24f, 9f, 27f }, new[] { -5f, 0f, 7f }, 35f));
        }

        private static CameraPoseSet BellScene(TemporaryObjects temporary)
        {
            GameObject bell = Instantiate(BellModelBuilder.BellName, temporary, ArtPaths.FriendFolder);
            bell.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            SetGlow(bell.transform, "PartLamp_", 4, 1f);
            SetGlow(bell.transform, "DialLamp", new[] { "" }, 1f);
            temporary.Add(Rover(temporary, new Vector3(1.9f, 0f, 0.7f), -25f).gameObject);
            Light lamp = NewLight("PorchLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 2.5f);
            lamp.transform.position = new Vector3(-1.2f, 2.2f, 2.2f);
            lamp.range = BaseLampRange;
            lamp.shadows = LightShadows.Soft;
            temporary.Add(lamp.gameObject);

            GameObject dancer = Instantiate(BellModelBuilder.BellName, temporary, ArtPaths.FriendFolder);
            dancer.transform.SetPositionAndRotation(new Vector3(-2.4f, 0f, 0.4f), Quaternion.Euler(0f, 30f, 0f));
            SetGlow(dancer.transform, "PartLamp_", 4, 1f);
            SetGlow(dancer.transform, "DialLamp", new[] { "" }, 1f);
            Transform body = Descendant(dancer.transform, "Body");
            body.localPosition += new Vector3(0.03f, -0.04f, 0f);
            body.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Descendant(dancer.transform, "Lid").localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Descendant(dancer.transform, "Needle").localRotation = Quaternion.Euler(0f, 0f, 90f);
            Descendant(dancer.transform, "Leg_FR").localRotation = Quaternion.Euler(-24f, 0f, 6f);
            Descendant(dancer.transform, "Shin_FR").localRotation = Quaternion.Euler(30f, 0f, 0f);
            foreach (string corner in new[] { "FL", "RL", "RR" })
            {
                Descendant(dancer.transform, "Leg_" + corner).localPosition += new Vector3(0f, -0.04f, 0f);
                Descendant(dancer.transform, "Shin_" + corner).localRotation = Quaternion.Euler(8f, 0f, 0f);
            }

            return Poses(
                Pose("hero", new[] { 2.4f, 1.5f, 3.9f }, new[] { 0.6f, 0.95f, 0f }, 42f),
                Pose("face", new[] { 0.35f, 1.35f, 1.5f }, new[] { 0f, 1.2f, 0.2f }, 40f),
                Pose("dance", new[] { -1.2f, 1.3f, 3.2f }, new[] { -2.3f, 0.9f, 0.4f }, 40f),
                Pose("side", new[] { -3.2f, 1.2f, -1.6f }, new[] { 0f, 0.95f, 0f }, 40f),
                Pose("far15m", new[] { 4f, 4f, 14f }, new[] { 0f, 0.9f, 0f }, 40f),
                Pose("far30m", new[] { 8f, 8f, 28f }, new[] { 0f, 0.9f, 0f }, 40f));
        }

        private static CameraPoseSet BellBrokenScene(Material material, TemporaryObjects temporary)
        {
            GameObject bell = Instantiate(BellModelBuilder.BellBrokenName, temporary, ArtPaths.FriendFolder);
            float back = RendererBounds(bell).min.z;
            BatchRunner.Log($"Bell_Broken reaches {back:F3} m behind her root: stand her that far from the wall");
            var wall = new LowPolyMeshBuilder(64);
            wall.Box(Place.At(0f, 2f, back - 0.6f), new Vector3(9f, 4f, 1.2f), PaletteSwatch.RockDark, 0.2f,
                new Displacement(5, 0.12f, 1.4f, 2));
            temporary.Add(MeshObject("CanyonWall", wall.ToMesh("PreviewWall"), material, temporary));

            GameObject cache = Instantiate(PropModelBuilder.LogCacheName, temporary, ArtPaths.PropFolder);
            cache.transform.SetPositionAndRotation(new Vector3(1.15f, 0f, 0.35f), Quaternion.Euler(0f, -28f, 0f));
            GameObject tape = Instantiate(CassetteModelBuilder.PrefabName("after_dark_1"), temporary,
                ArtPaths.PickupFolder);
            float tapeLift = tape.transform.position.y - RendererBounds(tape).min.y;
            tape.transform.SetPositionAndRotation(new Vector3(1.6f, tapeLift, 0.95f), Quaternion.Euler(0f, -40f, 0f));
            IReadOnlyList<string> parts = BellModelBuilder.PartNames;
            for (int i = 0; i < parts.Count; i++)
            {
                GameObject part = Instantiate(parts[i], temporary, ArtPaths.FriendFolder);
                var at = new Vector3(-2.6f + i * 2.4f, 0f, 3.2f + (i % 2) * 1.6f);
                float lift = part.transform.position.y - RendererBounds(part).min.y;
                part.transform.SetPositionAndRotation(at + Vector3.up * lift, Quaternion.Euler(0f, i * 50f, 0f));
            }

            temporary.Add(Rover(temporary, new Vector3(2.2f, 0f, 6.5f), 200f).gameObject);
            return Poses(
                Pose("found", new[] { 2.4f, 1.5f, 4.4f }, new[] { 0.2f, 0.45f, 0f }, 42f),
                Pose("close", new[] { 0.7f, 1f, 1.9f }, new[] { 0f, 0.5f, -0.1f }, 42f),
                Pose("cache", new[] { 1.7f, 0.75f, 1.9f }, new[] { 1.2f, 0.2f, 0.5f }, 40f),
                Pose("parts", new[] { 0.5f, 2.2f, 8.5f }, new[] { -0.2f, 0.2f, 3.6f }, 50f),
                Pose("far30m", new[] { -6f, 8f, 28f }, new[] { 0f, 0.5f, 0f }, 35f));
        }

        private static CameraPoseSet PickupsScene(TemporaryObjects temporary)
        {
            IReadOnlyList<string> parts = BellModelBuilder.PartNames;
            for (int i = 0; i < parts.Count; i++)
            {
                GameObject part = Instantiate(parts[i], temporary, ArtPaths.FriendFolder);
                float lift = part.transform.position.y - RendererBounds(part).min.y;
                part.transform.SetPositionAndRotation(new Vector3((i - 1) * 0.6f, lift, 2.4f),
                    Quaternion.Euler(0f, 15f, 0f));
            }

            IReadOnlyList<CassetteStyle> styles = CassetteModelBuilder.Styles;
            for (int i = 0; i < styles.Count; i++)
            {
                GameObject tape = Instantiate(CassetteModelBuilder.PrefabName(styles[i].Id), temporary,
                    ArtPaths.PickupFolder);
                float lift = tape.transform.position.y - RendererBounds(tape).min.y;
                tape.transform.SetPositionAndRotation(new Vector3((i - 1) * 0.55f, lift, 1.4f),
                    Quaternion.Euler(0f, -12f + i * 12f, 0f));
            }

            GameObject cache = Instantiate(PropModelBuilder.LogCacheName, temporary, ArtPaths.PropFolder);
            cache.transform.SetPositionAndRotation(new Vector3(1.6f, 0f, 1.9f), Quaternion.Euler(0f, -30f, 0f));
            GameObject shelf = Instantiate(BaseModelBuilder.CassetteShelfName, temporary);
            shelf.transform.SetPositionAndRotation(new Vector3(-1.7f, 0f, 0.4f), Quaternion.Euler(0f, 25f, 0f));
            FillShelf(shelf.transform, temporary);
            temporary.Add(Rover(temporary, new Vector3(0.3f, 0f, -1.6f), 10f).gameObject);
            Light lamp = NewLight("PorchLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 2.5f);
            lamp.transform.position = new Vector3(-0.4f, 2.2f, 3.4f);
            lamp.range = BaseLampRange;
            temporary.Add(lamp.gameObject);
            return Poses(
                Pose("parts", new[] { 0f, 0.75f, 3.6f }, new[] { 0f, 0.12f, 2.4f }, 40f),
                Pose("tapes", new[] { 0f, 0.55f, 2.5f }, new[] { 0f, 0.1f, 1.4f }, 40f),
                Pose("cache", new[] { 1.9f, 0.7f, 2.9f }, new[] { 1.6f, 0.15f, 1.9f }, 40f),
                Pose("shelf", new[] { -0.9f, 1.3f, 2.6f }, new[] { -1.7f, 0.75f, 0.4f }, 42f),
                Pose("far15m", new[] { 3f, 5f, 14f }, new[] { 0f, 0.3f, 1.5f }, 40f));
        }

        private static CameraPoseSet BellCornerScene(Material material, TemporaryObjects temporary)
        {
            GameObject lander = Instantiate(BaseModelBuilder.LanderName, temporary);
            GameObject tower = Instantiate(BaseModelBuilder.TowerPrefix + "3", temporary);
            tower.transform.position = BaseModelBuilder.TowerAnchor;
            for (int i = 0; i < 4; i++)
            {
                Light baseLamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp),
                    BaseLampIntensity);
                baseLamp.transform.position = Descendant(lander.transform, "LampSocket_" + i).position;
                baseLamp.range = BaseLampRange;
                baseLamp.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
                temporary.Add(baseLamp.gameObject);
            }

            Transform corner = Descendant(tower.transform, "BellCorner");
            GameObject bell = Instantiate(BellModelBuilder.BellName, temporary, ArtPaths.FriendFolder);
            bell.transform.SetPositionAndRotation(corner.position, corner.rotation);
            SetGlow(bell.transform, "PartLamp_", 4, 1f);
            SetGlow(bell.transform, "DialLamp", new[] { "" }, 1f);

            Transform anchor = Descendant(tower.transform, "CassetteShelfAnchor");
            GameObject shelf = Instantiate(BaseModelBuilder.CassetteShelfName, temporary);
            shelf.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            FillShelf(shelf.transform, temporary);

            Vector3 parked = corner.position + corner.forward * 2f;
            temporary.Add(Rover(temporary, parked, corner.eulerAngles.y + 180f).gameObject);

            var rings = new LowPolyMeshBuilder(400);
            Vector3 pad = tower.transform.TransformPoint(new Vector3(0f, 0.03f, TowerPadOffset));
            rings.Torus(Place.At(pad), TowerPadRadius, 0.06f, 32, 3, PaletteSwatch.PilotLight);
            rings.Torus(Place.At(corner.position + Vector3.up * 0.03f), BellClearRadius, 0.03f, 32, 3,
                PaletteSwatch.Metal);
            temporary.Add(MeshObject("LayoutRings", rings.ToMesh("PreviewRings"), material, temporary));

            Vector3 bellAt = corner.position;
            return Poses(
                Pose("homecoming", new[] { 2f, 4f, 16f }, new[] { -7f, 1.2f, 0f }, 50f),
                Pose("parked", new[] { parked.x + 2.2f, 2.4f, parked.z + 1.6f },
                    new[] { bellAt.x, 0.9f, bellAt.z }, 50f),
                Pose("overhead", new[] { -6.5f, 22f, 4f }, new[] { -6.5f, 0f, 0.2f }, 50f),
                Pose("far40m", new[] { 14f, 12f, 38f }, new[] { -6f, 2f, 0f }, 35f));
        }

        /// <summary>
        /// Opens Main.unity (its earthlight, ambient, fog and graded volume; never saved) and stages the glowing cast
        /// on a dust disc away from the base, every glow at a linear multiplier: three 07s whose eyes glow at 0.4, 0.7
        /// and 1 (authored), Bell with her dial and part lamps at 1, Tilly at 1, and the pickups.
        /// </summary>
        private static CameraPoseSet WarmPointsScene(Material material, TemporaryObjects temporary)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            var ground = new LowPolyMeshBuilder(64);
            ground.Prism(Place.At(WarmStage + Vector3.down * 0.1f), 14f, 0.2f, 24, PaletteSwatch.DustMid);
            temporary.Add(MeshObject("PreviewGround", ground.ToMesh("PreviewGround"), material, temporary));

            float[] eyes = { 0.4f, 0.7f, 1f };
            for (int i = 0; i < eyes.Length; i++)
            {
                Transform rover = Rover(temporary, WarmStage + new Vector3((i - 1) * 2.3f, 0f, -3f), 0f);
                temporary.Add(rover.gameObject);
                SetGlow(rover, "Eye", new[] { "" }, eyes[i]);
            }

            GameObject tilly = Instantiate(FriendModelBuilder.TillyName, temporary, ArtPaths.FriendFolder);
            tilly.transform.SetPositionAndRotation(WarmStage + new Vector3(1.6f, 1.1f, 0.6f),
                Quaternion.Euler(0f, -15f, 0f));
            SetGlow(tilly.transform, "PartLamp_", 3, 1f);
            GameObject bell = Instantiate(BellModelBuilder.BellName, temporary, ArtPaths.FriendFolder);
            bell.transform.SetPositionAndRotation(WarmStage + new Vector3(-1.6f, 0f, 0.4f),
                Quaternion.Euler(0f, 15f, 0f));
            SetGlow(bell.transform, "PartLamp_", 4, 1f);
            SetGlow(bell.transform, "DialLamp", new[] { "" }, 1f);

            string[] pickups = { "Part_BellValve", "Part_BellCone", "Part_TillyLens" };
            for (int i = 0; i < pickups.Length; i++)
            {
                Ground(Instantiate(pickups[i], temporary, ArtPaths.FriendFolder),
                    new Vector3(i * 0.55f - 1.4f, 0f, 2.6f));
            }

            Ground(Instantiate(CassetteModelBuilder.PrefabName("after_dark_1"), temporary, ArtPaths.PickupFolder),
                new Vector3(0.35f, 0f, 2.7f));
            Ground(Instantiate(SiteModelBuilder.BundleName(SalvagePieceMaterial.Wiring), temporary,
                ArtPaths.PickupFolder), new Vector3(0.95f, 0f, 2.6f));
            return Poses(
                Pose("eyes", Stage(0f, 1.5f, 1.2f), Stage(0f, 1.15f, -3f), 50f),
                Pose("friends", Stage(0f, 1.4f, 4.4f), Stage(0f, 1f, 0.5f), 45f),
                Pose("pickups", Stage(-0.2f, 0.9f, 4.6f), Stage(-0.2f, 0.15f, 2.6f), 40f),
                Pose("stage_from_40m", Stage(8f, 8f, 40f), Stage(0f, 0.8f, -1f), 40f));
        }

        /// <summary>Stands a pickup on the dust at <paramref name="offset"/> from the stage.</summary>
        private static void Ground(GameObject pickup, Vector3 offset)
        {
            float lift = pickup.transform.position.y - RendererBounds(pickup).min.y;
            pickup.transform.SetPositionAndRotation(WarmStage + offset + Vector3.up * lift,
                Quaternion.Euler(0f, 20f, 0f));
        }

        private static float[] Stage(float x, float y, float z)
        {
            Vector3 p = WarmStage + new Vector3(x, y, z);
            return new[] { p.x, p.y, p.z };
        }

        /// <summary>
        /// Opens Main.unity (its terrain preview, earthlight, fog and grade; never saved) and stands a relay mast on
        /// each of World's relay anchors, facing home: broken with its part glinting on relay.0's pad, or restored with
        /// its lamp lit at the authored 1. Poses: the spawn first frame, relay.0 from the base, and each mast near
        /// and from afar.
        /// </summary>
        private static CameraPoseSet RelaysScene(TemporaryObjects temporary, bool restored)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            string prefab = restored ? RelayModelBuilder.MastName : RelayModelBuilder.BrokenMastName;
            var poses = new List<CameraPose>
            {
                new CameraPose { name = "spawn_first_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("relay0_from_base", new[] { 0f, 3f, 0f }, Point(RelayAnchors[0] + Vector3.up * 4f), 50f),
            };
            for (int i = 0; i < RelayAnchors.Length; i++)
            {
                Vector3 anchor = RelayAnchors[i];
                Vector3 home = new Vector3(-anchor.x, 0f, -anchor.z).normalized;
                Quaternion facing = Quaternion.LookRotation(home);
                GameObject mast = Instantiate(prefab, temporary, ArtPaths.RelayFolder);
                mast.transform.SetPositionAndRotation(anchor, facing);
                if (restored)
                {
                    SetGlow(mast.transform, "Lamp", new[] { "" }, 1f);
                }

                Vector3 side = facing * Vector3.right;
                Vector3 look = anchor + Vector3.up * 4.5f;
                poses.Add(Pose($"relay{i}_near", Point(anchor + home * 13f + side * 5f + Vector3.up * 3f),
                    Point(look), 50f));
                float far = i == RelayAnchors.Length - 1 ? 35f : 90f;
                poses.Add(Pose($"relay{i}_far", Point(anchor + home * far + side * 8f + Vector3.up * 9f), Point(look),
                    40f));
            }

            if (!restored)
            {
                GameObject part = Instantiate(RelayModelBuilder.PartName, temporary, ArtPaths.RelayFolder);
                Vector3 spot = RelayAnchors[0] + Quaternion.LookRotation(new Vector3(-RelayAnchors[0].x, 0f,
                    -RelayAnchors[0].z)) * new Vector3(1.8f, 0f, 1.2f);
                float lift = part.transform.position.y - RendererBounds(part).min.y;
                part.transform.SetPositionAndRotation(spot + Vector3.up * lift, Quaternion.Euler(0f, 30f, 0f));
            }

            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// Opens Main.unity (never saved) and stands each salvage site on its World anchor, +Z along the anchor's
        /// Forward: whole, or <paramref name="pickedClean"/> with every salvage piece taken. Poses: the spawn first
        /// frame, Kestrel-3 from the base, each site from the way in, its side and 30 m, the depot close.
        /// </summary>
        private static CameraPoseSet SitesScene(TemporaryObjects temporary, bool pickedClean)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            Vector3 kestrel = SiteAnchors[1] + Vector3.up * 3f;
            var poses = new List<CameraPose>
            {
                new CameraPose { name = "spawn_first_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("kestrel_from_base", new[] { 0f, 3f, 0f }, Point(kestrel), 60f),
                Pose("kestrel_from_base_zoom", new[] { 0f, 3f, 0f }, Point(kestrel), 15f),
            };
            IReadOnlyList<string> ids = SiteModelBuilder.Ids;
            for (int i = 0; i < ids.Count; i++)
            {
                Vector3 anchor = SiteAnchors[i];
                Vector3 forward = SiteForwards[i].normalized;
                Quaternion facing = Quaternion.LookRotation(forward);
                GameObject site = Instantiate(SiteModelBuilder.SiteName(ids[i]), temporary, ArtPaths.SitesFolder);
                site.transform.SetPositionAndRotation(anchor, facing);
                if (ids[i] == "drill")
                {
                    Transform bot = Instantiate(DerelictBuilder.HaulBotName, temporary, ArtPaths.PropFolder).transform;
                    bot.SetParent(Descendant(site.transform, SiteModelBuilder.HaulBotSocketName), false);
                }

                if (pickedClean)
                {
                    foreach (Transform child in site.transform)
                    {
                        child.gameObject.SetActive(!child.name.StartsWith("Salvage_", StringComparison.Ordinal));
                    }
                }

                Vector3 side = facing * Vector3.right;
                poses.Add(Pose($"{ids[i]}_approach", Point(anchor - forward * 14f + side * 4f + Vector3.up * 4.5f),
                    Point(anchor + forward * 1.5f + Vector3.up * 1.2f), 50f));
                poses.Add(Pose($"{ids[i]}_side", Point(anchor + side * 13f + forward * 3f + Vector3.up * 5f),
                    Point(anchor + forward * 2f + Vector3.up * 1f), 50f));
                poses.Add(Pose($"{ids[i]}_30m", Point(anchor - forward * 28f - side * 10f + Vector3.up * 6f),
                    Point(anchor + Vector3.up * 1.5f), 40f));
            }

            int garage = 0;
            while (ids[garage] != "garage")
            {
                garage++;
            }

            Vector3 garageForward = SiteForwards[garage].normalized;
            poses.Add(Pose("garage_top", Point(SiteAnchors[garage] + garageForward * 2.5f + Vector3.up * 16f),
                Point(SiteAnchors[garage] + garageForward * 2.6f), 50f));
            Vector3 garageSide = Quaternion.LookRotation(garageForward) * Vector3.right;
            Vector3 behind = SiteAnchors[garage] + garageForward * 13f + garageSide * 3f + Vector3.up * 3.5f;
            Vector3 backLane = SiteAnchors[garage] + garageForward * 6f + garageSide * 1.2f + Vector3.up;
            poses.Add(Pose("garage_back", Point(behind), Point(backLane), 50f));

            Vector3 depot = SiteAnchors[0];
            Vector3 depotForward = SiteForwards[0].normalized;
            Vector3 depotSide = Quaternion.LookRotation(depotForward) * Vector3.right;
            poses.Add(Pose("depot_close", Point(depot - depotForward * 5.5f + depotSide * 2.5f + Vector3.up * 2.4f),
                Point(depot + depotForward * 2.5f + depotSide * 1.2f + Vector3.up * 1f), 55f));
            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// 07 in five stages in a row (bare, lamp bar, drums, cradle, everything plus the gifts), each from the
        /// default chase camera's rear three-quarter; then a bare 07 beside a fully kitted one, from the chase camera
        /// and from 30 m.
        /// </summary>
        private static CameraPoseSet KitScene(TemporaryObjects temporary)
        {
            string[] stages = { "bare", "lampbar", "drums", "cradle", "full" };
            var chase = new Vector3(4.3f, 2.1f, -6.1f);
            var poses = new List<CameraPose>();
            for (int i = 0; i < stages.Length; i++)
            {
                var at = new Vector3((i - 2) * KitSpacing, 0f, 0f);
                Transform rover = Rover(temporary, at, 0f);
                temporary.Add(rover.gameObject);
                FitKit(rover, temporary, i == 1 || i == 4, i == 2 || i == 4, i == 3 || i == 4, i == 4, i == 4);
                poses.Add(Pose(stages[i] + "_chase", Point(at + chase), Point(at + Vector3.up * 0.9f), 45f));
            }

            var pair = new Vector3(0f, 0f, KitSpacing * 4f);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform rover = Rover(temporary, pair + Vector3.right * side * 1.9f, 0f);
                temporary.Add(rover.gameObject);
                bool kitted = side > 0;
                FitKit(rover, temporary, kitted, kitted, kitted, kitted, kitted);
            }

            Vector3 look = pair + Vector3.up * 0.8f;
            poses.Add(Pose("minute_one_vs_hour_five_chase", Point(pair + new Vector3(2.6f, 2.6f, -7.2f)), Point(look),
                45f));
            poses.Add(Pose("minute_one_vs_hour_five_30m", Point(pair + new Vector3(15f, 8.3f, -24.5f)), Point(look),
                40f));
            poses.Add(Pose("full_front", Point(new Vector3(2f * KitSpacing - 2.2f, 1.4f, 3.6f)),
                Point(new Vector3(2f * KitSpacing, 0.9f, 0f)), 45f));
            poses.Add(Pose("full_top", Point(new Vector3(2f * KitSpacing, 4.2f, -0.4f)),
                Point(new Vector3(2f * KitSpacing, 0.6f, -0.1f)), 40f));
            poses.Add(Pose("full_back", Point(new Vector3(2f * KitSpacing, 1.5f, -3.2f)),
                Point(new Vector3(2f * KitSpacing, 0.75f, -0.6f)), 40f));
            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// Opens Main.unity (its terrain preview, earthlight, fog and grade; never saved), hides the scene's own 07 and
        /// parks two on the base pad at the spawn, facing the spawn heading: minute one (bare) on the left and fully
        /// kitted (lamp bar, drums idling, empty cradle, Hover-Jump coils, the friends' gifts) on the right, both
        /// travelling with play's road light (the kitted one's warmer and wider, from the lamp bar's middle glass) and
        /// eye light. Poses at the game's lens: the pair from the chase camera and from 30 m (straight behind, the rear
        /// three-quarter and broadside), each 07 from its own chase camera, and the lamp bar close from the front
        /// three-quarter and from behind.
        /// </summary>
        private static CameraPoseSet KitMainScene(TemporaryObjects temporary)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            SceneObject("[Rover]").gameObject.SetActive(false);
            Quaternion heading = Quaternion.Euler(0f, SpawnYaw, 0f);
            Vector3 side = heading * Vector3.right * (KitPairGap * 0.5f);
            Transform bare = TravellingRover(temporary, -side, heading, false);
            Transform kitted = TravellingRover(temporary, side, heading, true);
            Vector3 pair = Vector3.zero;
            var poses = new List<CameraPose>
            {
                ChasePose("pair_chase", pair, heading, ChaseDistance, 0f),
                ChasePose("pair_chase_quarter", pair, heading, ChaseDistance, ChaseQuarterYaw),
                ChasePose("pair_30m", pair, heading, FarDistance, 0f),
                ChasePose("pair_30m_quarter", pair, heading, FarDistance, ChaseQuarterYaw),
                ChasePose("pair_30m_broadside", pair, heading, FarDistance, 90f),
                ChasePose("minute_one_chase", bare.position, heading, ChaseDistance, 0f),
                ChasePose("full_kit_chase", kitted.position, heading, ChaseDistance, 0f),
                ChasePose("minute_one_chase_quarter", bare.position, heading, ChaseDistance, -ChaseQuarterYaw),
                ChasePose("full_kit_chase_quarter", kitted.position, heading, ChaseDistance, ChaseQuarterYaw),
                Pose("lampbar_close", Point(kitted.TransformPoint(new Vector3(1.25f, 1.2f, 2.3f))),
                    Point(kitted.TransformPoint(new Vector3(0f, 1.1f, 0.7f))), 45f),
                Pose("lampbar_close_behind", Point(kitted.TransformPoint(new Vector3(1.1f, 2.3f, -2.6f))),
                    Point(kitted.TransformPoint(new Vector3(0f, 1.1f, 0.7f))), 45f),
            };
            return new CameraPoseSet { width = 1920, height = 1080, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// Opens Main.unity (never saved), hides the scene's own 07, parks a resting 07 on its dock and another in
        /// front of the lift facing it, and wakes the base up to <paramref name="stage"/> (see the class summary).
        /// </summary>
        private static CameraPoseSet WakeScene(TemporaryObjects temporary, int stage)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            SceneObject("[Rover]").gameObject.SetActive(false);
            Transform lander = SceneObject(BaseModelBuilder.LanderName);
            Transform bay = SceneObject(BaseModelBuilder.RoverBayName);
            Transform dock = Descendant(lander, "DockAnchor");
            TravellingRover(temporary, dock.position, dock.rotation, false);
            SetGlow(lander, "DockGlow", new[] { string.Empty }, 1f);
            Transform tower = SceneObjectStartingWith(BaseModelBuilder.TowerPrefix);
            SetGlow(tower, "Lights", new[] { string.Empty }, 1f);
            SetGlow(tower, BaseModelBuilder.TowerLightRingPrefix, new[] { "1", "2", "3" }, 1f);
            Quaternion toLift = lander.rotation * Quaternion.Euler(0f, 180f, 0f);
            Vector3 front = lander.TransformPoint(new Vector3(-1.6f, 0f, 9.2f));
            TravellingRover(temporary, front, toLift, false);
            if (stage >= 1)
            {
                SetGlow(lander, "Windows", new[] { string.Empty }, 1f);
                SetGlow(lander, BaseModelBuilder.PanePrefix, new[] { "1", "2" }, 1f);
                SetGlow(lander, BaseModelBuilder.BigPortholeName, new[] { string.Empty }, 1f);
                Transform belly = Descendant(lander, BaseModelBuilder.BellyLiftName);
                SetGlow(belly, BaseModelBuilder.BellyLampName, new[] { string.Empty }, 1f);
                SetGlow(belly, BaseModelBuilder.HatchRimName, new[] { string.Empty }, 1f);
                foreach (Transform socket in ParentsOf(lander, BaseModelBuilder.BulbName))
                {
                    SetGlow(socket, BaseModelBuilder.BulbName, new[] { string.Empty }, 1f);
                }

                Descendant(lander, BaseModelBuilder.HomeSignName).localRotation = BaseModelBuilder.HomeSignUpright;
                LightCable(lander, "Home");
            }

            if (stage >= 2)
            {
                Transform shutter = Descendant(bay, BaseModelBuilder.ShutterName);
                shutter.localScale = new Vector3(1f, BaseModelBuilder.ShutterRolledScale, 1f);
                LightBay(bay, temporary);
                SetGlow(bay, BaseModelBuilder.PlotterLampName, new[] { string.Empty }, 1f);
                LightCable(lander, "Bay");
            }

            if (stage >= 3)
            {
                RestoreLift(lander);
                SetGlow(lander, BaseModelBuilder.LightTableGlowName, new[] { string.Empty }, 1f);
                SetGlow(Descendant(lander, BaseModelBuilder.PortholeLampsName), BaseModelBuilder.LampPrefix, 4, 1f);
                LightCable(lander, "Chart");
            }

            if (stage >= 4)
            {
                PlacePad(temporary, 1, tower.position + tower.forward * TowerPadOffset, tower.rotation);
                PlacePad(temporary, 2, bay.TransformPoint(new Vector3(0f, 0f, 4.2f)), bay.rotation);
                PlacePad(temporary, 0, lander.TransformPoint(new Vector3(2f, 0f, 7.4f)), lander.rotation);
            }

            var poses = new List<CameraPose>
            {
                new CameraPose { name = "spawn_first_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("dock_side_lander_lift_ladder", Point(lander.TransformPoint(new Vector3(-2.2f, 1.7f, 9.5f))),
                    Point(lander.TransformPoint(new Vector3(1.4f, 2.4f, 3.2f))), 55f),
                ChasePose("lift_from_chase", front, toLift, ChaseDistance, 0f),
                ChasePose("lift_from_chase_quarter", front, toLift, ChaseDistance, ChaseQuarterYaw),
                Pose("bay_front", Point(bay.TransformPoint(new Vector3(1.5f, 2.8f, 9f))),
                    Point(bay.TransformPoint(new Vector3(0f, 1.6f, 0f))), 55f),
                Pose("base_wide", Point(lander.TransformPoint(new Vector3(-14f, 9f, 22f))),
                    Point(lander.TransformPoint(new Vector3(2f, 1.5f, 0f))), 50f),
            };
            return new CameraPoseSet { width = 1920, height = 1080, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// Main.unity (never saved) with home awake and the lift restored; the lab <paramref name="lit"/> or asleep.
        /// The owner's two frames first (behind 07 at the base, the lander from the ladder side, a 07 parked in front
        /// of the belly), returned for the usual capture; and a 07 up on the raised turntable facing each station in
        /// turn, seen over its shoulder from the camera's ring rail just under the rail, captured here into
        /// <paramref name="output"/> one station at a time (the one 07 turns between them).
        /// </summary>
        private static CameraPoseSet LabScene(TemporaryObjects temporary, bool lit, string output, string prefix)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            Transform spawn = SceneObject("[Rover]");
            spawn.gameObject.SetActive(false);
            TravellingRover(temporary, spawn.position, spawn.rotation, false);
            Transform lander = SceneObject(BaseModelBuilder.LanderName);
            SetGlow(lander, "Windows", new[] { string.Empty }, 1f);
            SetGlow(lander, BaseModelBuilder.PanePrefix, new[] { "1", "2" }, 1f);
            SetGlow(lander, "DockGlow", new[] { string.Empty }, 1f);
            foreach (Transform socket in ParentsOf(lander, BaseModelBuilder.BulbName))
            {
                SetGlow(socket, BaseModelBuilder.BulbName, new[] { string.Empty }, 1f);
            }

            Transform belly = Descendant(lander, BaseModelBuilder.BellyLiftName);
            SetGlow(belly, BaseModelBuilder.BellyLampName, new[] { string.Empty }, 1f);
            SetGlow(belly, BaseModelBuilder.HatchRimName, new[] { string.Empty }, 1f);
            RestoreLift(lander);
            Transform homeTower = SceneObjectStartingWith(BaseModelBuilder.TowerPrefix);
            SetGlow(homeTower, "Lights", new[] { string.Empty }, 1f);
            SetGlow(homeTower, BaseModelBuilder.TowerLightRingPrefix, new[] { "1", "2", "3" }, 1f);
            Transform lab = Descendant(lander, BaseModelBuilder.LabName);
            if (lit)
            {
                LightLab(lander, lab, temporary);
            }

            var poses = new List<CameraPose>
            {
                new CameraPose { name = "owner_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("ladder_side", Point(lander.TransformPoint(new Vector3(-11f, 3.2f, 13f))),
                    Point(lander.TransformPoint(new Vector3(0f, 3.4f, 0f))), 50f),
            };
            Quaternion inward = lander.rotation * Quaternion.Euler(0f, 180f, 0f);
            Transform front = TravellingRover(temporary, lander.TransformPoint(new Vector3(0f, 0f, 5.2f)), inward,
                false);
            poses.Add(ChasePose("belly_from_chase", front.position, inward, ChaseDistance, 0f));
            front.gameObject.SetActive(false);
            RaisePlatform(belly, Descendant(belly, BaseModelBuilder.PlatformUpName).localPosition.y);
            Descendant(belly, BaseModelBuilder.HatchLeafLeftName).localPosition =
                Vector3.left * BaseModelBuilder.HatchLeafTravel;
            Descendant(belly, BaseModelBuilder.HatchLeafRightName).localPosition =
                Vector3.right * BaseModelBuilder.HatchLeafTravel;
            Transform spot = Descendant(belly, BaseModelBuilder.RoverSpotName);
            Transform rover = TravellingRover(temporary, spot.position, inward, false);
            for (int i = 0; i < LabMeshes.StationNames.Length; i++)
            {
                Transform station = Descendant(lab, BaseModelBuilder.StationPrefix + LabMeshes.StationNames[i]);
                Vector3 toStation = Vector3.ProjectOnPlane(station.position - spot.position, Vector3.up).normalized;
                rover.rotation = Quaternion.LookRotation(toStation);
                Vector3 behind = Quaternion.Euler(0f, ShoulderYaw, 0f) * -toStation;
                Vector3 rail = lab.position + behind * (BaseModelBuilder.LabRingRadius - 0.05f)
                    + Vector3.up * (LabMeshes.RingHeight - 0.15f);
                CameraPose view = Pose("inside_" + LabMeshes.StationNames[i].ToLowerInvariant(), Point(rail),
                    Point(station.position), 55f);
                CaptureAutomation.CapturePoses(new CameraPoseSet { width = 1920, height = 1080, postProcessing = true,
                    poses = new[] { view } }, output, prefix);
            }

            return new CameraPoseSet { width = 1920, height = 1080, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// The lab awake as a mid-game save would show it: wall lamps, reading lamps, the light table and the big
        /// porthole lit, warm point lights at the lamps (World drives them in play), six of the porthole's lamps for
        /// six known nodes, the tower's three lamps on the board with their sheets, and the first recovery lamps.
        /// </summary>
        private static void LightLab(Transform lander, Transform lab, TemporaryObjects temporary)
        {
            SetGlow(lab, BaseModelBuilder.CabinLampsName, new[] { string.Empty }, 1f);
            SetGlow(lander, BaseModelBuilder.BigPortholeName, new[] { string.Empty }, 1f);
            SetGlow(lander, BaseModelBuilder.LightTableGlowName, new[] { string.Empty }, 1f);
            SetGlow(Descendant(lander, BaseModelBuilder.PortholeLampsName), BaseModelBuilder.LampPrefix, 6, 1f);
            Transform keepsakes = Descendant(lab, BaseModelBuilder.StationPrefix + "Keepsakes");
            SetGlow(keepsakes, BaseModelBuilder.ReadingLampPrefix, 4, 0.7f);
            Transform board = Descendant(lab, BaseModelBuilder.StationPrefix + "Board");
            SetGlow(board, BaseModelBuilder.LampPrefix, 3, 1f);
            for (int n = 0; n < 6; n++)
            {
                Descendant(board, BaseModelBuilder.SheetPrefix + n).gameObject.SetActive(true);
            }

            SetGlow(Descendant(board, BaseModelBuilder.RowPrefix + "Power"), BaseModelBuilder.LampPrefix, 3, 1f);
            SetGlow(Descendant(board, BaseModelBuilder.RowPrefix + "Link"), BaseModelBuilder.LampPrefix, 2, 1f);
            Renderer lamps = Descendant(lab, BaseModelBuilder.CabinLampsName).GetComponent<Renderer>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 toWall = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
                WarmLamp(temporary, lab.TransformPoint(toWall * 2.45f + Vector3.up * 1.95f), 1.4f, 3.6f);
            }

            WarmLamp(temporary, lamps.bounds.center + Vector3.up * 0.4f, 0.8f, 4.5f);
            for (int i = 0; i < 4; i++)
            {
                Transform reading = Descendant(keepsakes, BaseModelBuilder.ReadingLampPrefix + i);
                WarmLamp(temporary, reading.position + Vector3.down * 0.12f, 0.5f, 1.4f);
            }
        }

        /// <summary>A warm practical lamp (a point light) where World lights the lab's lamps in play.</summary>
        private static void WarmLamp(TemporaryObjects temporary, Vector3 position, float intensity, float range)
        {
            Light lamp = NewLight("LabLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), intensity);
            temporary.Add(lamp.gameObject);
            lamp.range = range;
            lamp.shadows = LightShadows.None;
            lamp.transform.position = position;
        }

        /// <summary>
        /// The belly lift as restored: the platform level on its ground stops, both hoist cables taut from their
        /// sheaves to its eyes, the jam and the dust drift gone, its pad and rim lamps lit.
        /// </summary>
        private static void RestoreLift(Transform lander)
        {
            Transform belly = Descendant(lander, BaseModelBuilder.BellyLiftName);
            RaisePlatform(belly, Descendant(belly, BaseModelBuilder.PlatformDownName).localPosition.y);
            Transform platform = Descendant(belly, BaseModelBuilder.PlatformName);
            Descendant(belly, BaseModelBuilder.CableJamName).gameObject.SetActive(false);
            Descendant(platform, Weathering.DustName).gameObject.SetActive(false);
            SetGlow(platform, BaseModelBuilder.PadGlowName, new[] { string.Empty }, 1f);
            SetGlow(platform, BaseModelBuilder.RimLampsName, new[] { string.Empty }, 1f);
        }

        /// <summary>
        /// Sets the belly platform level with its top at <paramref name="height"/> (lift space) and hangs both hoist
        /// cables to its eyes (their built length reaches the jammed platform's left eye).
        /// </summary>
        private static void RaisePlatform(Transform belly, float height)
        {
            Transform platform = Descendant(belly, BaseModelBuilder.PlatformName);
            float built = platform.localPosition.y + (platform.localRotation * Vector3.left).y * 1.77f;
            platform.SetLocalPositionAndRotation(new Vector3(0f, height, 0f), BaseModelBuilder.BellyPlatformLevel);
            foreach (string name in new[] { BaseModelBuilder.CableLeftName, BaseModelBuilder.CableRightName })
            {
                Transform hoist = Descendant(belly, name);
                hoist.gameObject.SetActive(true);
                float length = BaseModelBuilder.BellyCableBuiltLength - (height - built);
                hoist.localScale = new Vector3(1f, Mathf.Max(0.01f, length) / BaseModelBuilder.BellyCableBuiltLength,
                    1f);
            }
        }

        /// <summary>Lights every glass segment of the ground cable to <paramref name="target"/>.</summary>
        private static void LightCable(Transform lander, string target)
        {
            Transform cable = Descendant(lander, BaseModelBuilder.PowerCablePrefix + target);
            SetGlow(cable, BaseModelBuilder.CableGlowPrefix, cable.childCount, 1f);
        }

        /// <summary>Stands a station pad of size <paramref name="size"/> on the ground with its lamps lit.</summary>
        private static void PlacePad(TemporaryObjects temporary, int size, Vector3 at, Quaternion turn)
        {
            GameObject pad = Instantiate(BaseModelBuilder.PadName(size), temporary);
            pad.transform.SetPositionAndRotation(new Vector3(at.x, 0f, at.z), turn);
            SetGlow(pad.transform, BaseModelBuilder.PadLampPrefix, 6, 1f);
        }

        /// <summary>The parent of every descendant of <paramref name="root"/> called <paramref name="name"/>.</summary>
        private static List<Transform> ParentsOf(Transform root, string name)
        {
            var found = new List<Transform>();
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    found.Add(node.parent);
                }
            }

            return found;
        }

        /// <summary>The first object in the open scene whose name starts with <paramref name="prefix"/>.</summary>
        private static Transform SceneObjectStartingWith(string prefix)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name.StartsWith(prefix, StringComparison.Ordinal) && node.gameObject.activeInHierarchy)
                    {
                        return node;
                    }
                }
            }

            throw new InvalidOperationException($"The open scene has no active object named {prefix}*.");
        }

        /// <summary>
        /// The game's chase camera on a 07 at <paramref name="at"/> facing <paramref name="heading"/>,
        /// <paramref name="distance"/> out and swung <paramref name="yaw"/> degrees from straight behind (+ = to its
        /// right).
        /// </summary>
        private static CameraPose ChasePose(string name, Vector3 at, Quaternion heading, float distance, float yaw)
        {
            Vector3 target = at + Vector3.up * ChaseTargetHeight;
            Vector3 offset = heading * Quaternion.Euler(ChasePitch, -yaw, 0f) * Vector3.back * distance;
            return Pose(name, Point(target + offset), Point(target), ChaseFov);
        }

        /// <summary>
        /// A 07 travelling as play poses it, with play's road light and eye light; <paramref name="kitted"/> fits the
        /// whole kit lit at play's levels (the drums idling), the Hover-Jump coils and the friends' gifts, and moves
        /// the road light to the lamp bar's middle glass, warmer and wider.
        /// </summary>
        private static Transform TravellingRover(TemporaryObjects temporary, Vector3 position, Quaternion heading,
            bool kitted)
        {
            GameObject instance = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            Transform rover = instance.transform;
            rover.SetPositionAndRotation(position, heading);
            Descendant(rover, "Head").localRotation = Quaternion.Euler(TravelHeadPitchDegrees, 0f, 0f);
            Descendant(rover, "Eyelid").localRotation = Quaternion.Euler(ActiveEyelidDegrees, 0f, 0f);

            Transform socket = Descendant(rover, "HeadlampSocket");
            Light road = NewLight("RoadLight", LightType.Spot, Palette.Get(PaletteSwatch.WarmLamp), RoadLightIntensity);
            road.transform.SetPositionAndRotation(socket.position, socket.rotation);
            road.range = RoadLightRange;
            road.spotAngle = RoadLightAngle;
            road.innerSpotAngle = RoadLightInnerAngle;
            road.shadows = LightShadows.None;
            temporary.Add(road.gameObject);

            Light eye = NewLight("EyeGlow", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), EyeLightIntensity);
            eye.transform.position = Descendant(rover, "Eye").position;
            eye.range = EyeLightRange;
            eye.shadows = LightShadows.None;
            temporary.Add(eye.gameObject);
            if (!kitted)
            {
                return rover;
            }

            Transform bar = Attach(rover, RoverModelBuilder.LampBarName, "HeadlampSocket", temporary);
            SetGlow(bar, RoverModelBuilder.LampPrefix, RoverModelBuilder.LampCount, LampBarGlow);
            road.transform.position = Descendant(bar, RoverModelBuilder.LampPrefix + "1").position;
            road.color = WarmLightColor;
            road.intensity = WarmLightIntensity;
            road.range = WarmLightRange;
            road.spotAngle = WarmLightAngle;
            road.innerSpotAngle = WarmLightInnerAngle;
            foreach (string drumSocket in new[] { "DrumSocket_L", "DrumSocket_R" })
            {
                Transform drum = Attach(rover, RoverModelBuilder.CapacitorDrumName, drumSocket, temporary);
                SetGlow(drum, RoverModelBuilder.DrumGlowName, new[] { string.Empty }, DrumIdleGlow);
            }

            Attach(rover, RoverModelBuilder.CargoRackName, "CargoSocket", temporary);
            Attach(rover, RoverModelBuilder.SideBinName, "BinSocket_L", temporary);
            Attach(rover, RoverModelBuilder.SideBinName, "BinSocket_R", temporary);
            Attach(rover, RoverModelBuilder.HoverCoilsName, "CoilSocket", temporary);
            foreach (string gift in new[]
            {
                RoverModelBuilder.Decal07FreshName, RoverModelBuilder.CellFilledName, RoverModelBuilder.PennantName,
            })
            {
                Descendant(rover, gift).gameObject.SetActive(true);
            }

            return rover;
        }

        /// <summary>
        /// Opens Main.unity (never saved) as it stands, with a 07 parked in front of the lander, and frames the base
        /// the way the owner sees it: the spawn view, the lander close and three-quarter, the tower, the shelf area and
        /// 07 from the chase camera.
        /// </summary>
        private static CameraPoseSet HomeScene(TemporaryObjects temporary, bool weathered)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            Transform lander = SceneObject(BaseModelBuilder.LanderName);
            Vector3 parked = lander.TransformPoint(new Vector3(5.5f, 0f, 9f));
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(parked, lander.rotation * Quaternion.Euler(0f, 250f, 0f));
            Vector3 tower = lander.TransformPoint(BaseModelBuilder.TowerAnchor);
            Vector3 porthole = Descendant(lander, BaseModelBuilder.BigPortholeName).GetComponent<Renderer>().bounds
                .center;
            Vector3 roverBack = rover.transform.TransformDirection(new Vector3(0.6f, 0f, -0.8f));
            Transform bay = SceneObject(BaseModelBuilder.RoverBayName);
            SetGlow(bay, "BaySign", new[] { string.Empty }, 1f);
            Vector3 bayCentre = bay.TransformPoint(RoverBayMeshes.TurntableCentre + Vector3.up * 1.6f);
            var poses = new List<CameraPose>
            {
                new CameraPose { name = "spawn_first_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("lander_close", Point(lander.TransformPoint(new Vector3(0.6f, 2.6f, 11f))),
                    Point(lander.TransformPoint(new Vector3(0f, 3.4f, 0f))), 55f),
                Pose("lander_quarter", Point(lander.TransformPoint(new Vector3(8.5f, 4f, 9f))),
                    Point(lander.TransformPoint(new Vector3(0f, 3.2f, 0f))), 50f),
                Pose("tower", Point(tower + lander.TransformDirection(new Vector3(4.5f, 3f, 8f))),
                    Point(tower + Vector3.up * 3.5f), 55f),
                Pose("big_porthole", Point(porthole + lander.TransformDirection(new Vector3(-5.5f, -1.2f, -5.5f))),
                    Point(porthole), 45f),
                Pose("rover_chase", Point(parked + roverBack * 7.4f + Vector3.up * 2.2f), Point(parked + Vector3.up),
                    50f),
                Pose("rover_close", Point(parked + roverBack * 3.2f + Vector3.up * 1.6f),
                    Point(parked + Vector3.up * 0.7f), 50f),
                Pose("bay_front", Point(bay.TransformPoint(new Vector3(1.5f, 2.8f, 8.5f))), Point(bayCentre), 55f),
                Pose("bay_quarter", Point(bay.TransformPoint(new Vector3(-5.5f, 3.4f, 7f))), Point(bayCentre), 50f),
            };
            if (!weathered)
            {
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (node.name.StartsWith("Weather_", StringComparison.Ordinal))
                        {
                            node.gameObject.SetActive(false);
                        }
                    }
                }
            }

            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>
        /// Fits the chosen kit to a rover instance on its sockets, lit as in play (the lamps on, the drums glowing),
        /// optionally with a relic riding in the cradle, and shows the friends' gifts with Ro's tape deck on the back.
        /// </summary>
        private static void FitKit(Transform rover, TemporaryObjects temporary, bool lampBar, bool drums, bool cradle,
            bool relic, bool gifts)
        {
            if (lampBar)
            {
                Transform bar = Attach(rover, RoverModelBuilder.LampBarName, "HeadlampSocket", temporary);
                SetGlow(bar, RoverModelBuilder.LampPrefix, RoverModelBuilder.LampCount, 1f);
            }

            if (drums)
            {
                foreach (string socket in new[] { "DrumSocket_L", "DrumSocket_R" })
                {
                    Transform drum = Attach(rover, RoverModelBuilder.CapacitorDrumName, socket, temporary);
                    SetGlow(drum, RoverModelBuilder.DrumGlowName, new[] { "" }, 1f);
                }
            }

            if (cradle)
            {
                Transform rack = Attach(rover, RoverModelBuilder.CargoRackName, "CargoSocket", temporary);
                if (relic)
                {
                    Transform seat = Descendant(rack, RoverModelBuilder.RelicSeatName);
                    GameObject carried = Instantiate(RelicModelBuilder.PrefabName("teapot"), temporary,
                        ArtPaths.RelicFolder);
                    float lift = carried.transform.position.y - RendererBounds(carried).min.y;
                    carried.transform.SetPositionAndRotation(seat.position + seat.up * lift,
                        seat.rotation * Quaternion.Euler(0f, 30f, 0f));
                }
            }

            if (gifts)
            {
                foreach (string gift in new[]
                {
                    RoverModelBuilder.Decal07FreshName, RoverModelBuilder.CellFilledName, RoverModelBuilder.PennantName,
                })
                {
                    Descendant(rover, gift).gameObject.SetActive(true);
                }

                Attach(rover, RoverModelBuilder.TapeDeckName, RoverModelBuilder.DeckSocketName, temporary);
            }
        }

        private static Transform Attach(Transform rover, string kit, string socket, TemporaryObjects temporary)
        {
            GameObject piece = Instantiate(kit, temporary, ArtPaths.RoverFolder);
            piece.transform.SetParent(Descendant(rover, socket), false);
            return piece.transform;
        }

        /// <summary>The three material bundles in a row by 07.</summary>
        private static CameraPoseSet SalvageBitsScene(TemporaryObjects temporary)
        {
            Array materials = Enum.GetValues(typeof(SalvagePieceMaterial));
            for (int i = 0; i < materials.Length; i++)
            {
                var material = (SalvagePieceMaterial)materials.GetValue(i);
                GameObject bundle = Instantiate(SiteModelBuilder.BundleName(material), temporary,
                    ArtPaths.PickupFolder);
                float lift = bundle.transform.position.y - RendererBounds(bundle).min.y;
                bundle.transform.SetPositionAndRotation(new Vector3((i - 1) * 0.6f, lift, 1.6f),
                    Quaternion.Euler(0f, 20f, 0f));
            }

            temporary.Add(Rover(temporary, new Vector3(1.9f, 0f, 0.2f), -20f).gameObject);
            return Poses(
                Pose("bundles", new[] { 0f, 0.9f, 0.2f }, new[] { 0f, 0.12f, 1.6f }, 40f),
                Pose("bits_from_10m", new[] { 3f, 4f, -7f }, new[] { 0f, 0.3f, 2.5f }, 40f));
        }

        /// <summary>
        /// Opens Main.unity (never saved) and stages what M3-14 built for 07: Main's own Rover Bay on the lander's
        /// WorkshopAnchor with 07 parked on its turntable and an arm fitting, the true-size relics beside a
        /// second 07 and a 1.75 m reference person on open ground left of the lander, a third 07 working the radio
        /// tower's service port with the hatch swung open, a fourth resting on its charging dock with the glow lit, and
        /// the crew's cable lift jammed halfway beside the ladder. The 1.75 m reference person also stands on the
        /// porch beside the door, by the museum shelf and at Kenji's old bench for the human-scale audit.
        /// </summary>
        private static CameraPoseSet BuiltFor07Scene(Material material, TemporaryObjects temporary)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            Transform lander = SceneObject(BaseModelBuilder.LanderName);
            var poses = new List<CameraPose>();
            StageRoverBay(material, temporary, poses);
            StageRelics(lander, material, temporary, poses);
            StageTowerPort(temporary, poses);
            StageDockAndLift(lander, temporary, poses);
            StageHumanScale(lander, material, temporary, poses);
            return new CameraPoseSet { width = 1600, height = 900, postProcessing = true, poses = poses.ToArray() };
        }

        /// <summary>The bay powered: its lamps and sign glowing, a warm point light under each lamp.</summary>
        private static void LightBay(Transform bay, TemporaryObjects temporary)
        {
            SetGlow(bay, "Lamp_", 2, 1f);
            SetGlow(bay, "BaySign", new[] { string.Empty }, 1f);
            for (int i = 0; i < 2; i++)
            {
                Light lamp = NewLight("WorkLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 3f);
                lamp.transform.position = Descendant(bay, "Lamp_" + i).position - Vector3.up * 0.2f;
                lamp.range = 6f;
                lamp.shadows = LightShadows.Soft;
                temporary.Add(lamp.gameObject);
            }
        }

        /// <summary>
        /// Main.unity's Rover Bay: lamps and sign lit, 07 on the turntable, the right arm swung out and lowered over
        /// 07's back as if fitting its rack, the floor arm raised half way through the turntable's hole.
        /// </summary>
        private static void StageRoverBay(Material material, TemporaryObjects temporary, List<CameraPose> poses)
        {
            Transform bay = SceneObject(BaseModelBuilder.RoverBayName);
            Transform turntable = Descendant(bay, "Turntable");
            GameObject parked = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            parked.transform.SetPositionAndRotation(turntable.position, turntable.rotation);
            LightBay(bay, temporary);
            Transform fitting = Descendant(bay, "Arm_2");
            Descendant(fitting, "Yaw").localRotation = Quaternion.Euler(0f, 26f, 0f);
            Descendant(fitting, "Upper").localRotation = Quaternion.Euler(-77f, 0f, 0f);
            Descendant(fitting, "Lower").localRotation = Quaternion.Euler(69f, 0f, 0f);
            Descendant(fitting, "Tip").localRotation = Quaternion.Euler(8f, 0f, 0f);
            Descendant(bay, "FloorLift").localPosition += Vector3.up * FloorLiftShown;
            Vector3 centre = turntable.position + Vector3.up * 1.1f;
            poses.Add(Pose("bay_front", Point(bay.TransformPoint(new Vector3(1.5f, 2.6f, 7.5f))), Point(centre), 55f));
            poses.Add(Pose("bay_quarter", Point(bay.TransformPoint(new Vector3(6.5f, 3.4f, 6f))), Point(centre), 50f));
            poses.Add(Pose("bay_hopper", Point(bay.TransformPoint(new Vector3(-0.35f, 2.35f, 0.1f))),
                Point(bay.TransformPoint(RoverBayMeshes.HopperMouth)), 50f));
            poses.Add(Pose("bay_arms", Point(bay.TransformPoint(new Vector3(0.5f, 1.6f, 3.4f))),
                Point(bay.TransformPoint(new Vector3(0f, 2.8f, -0.6f))), 60f));
            poses.Add(Pose("bay_pit", Point(bay.TransformPoint(new Vector3(0.35f, 0.45f, 2.4f))),
                Point(bay.TransformPoint(RoverBayMeshes.FloorArmBase + Vector3.up * 0.28f)), 40f));
            Person(material, temporary, bay.TransformPoint(new Vector3(-3.2f, 0f, 2.2f)), bay.rotation);
            poses.Add(Pose("human_bay", Point(bay.TransformPoint(new Vector3(-1.2f, 1.6f, 6.4f))),
                Point(bay.TransformPoint(new Vector3(-3.4f, 1f, 1.6f))), 45f));
            poses.Add(Pose("bay_back_wall", Point(bay.TransformPoint(new Vector3(0.6f, 2.2f, 1.6f))),
                Point(bay.TransformPoint(RoverBayMeshes.PlotterOnWall + Vector3.up * 1.1f)), 55f));
        }

        /// <summary>The true-size relics in a row before 07 and a 1.75 m reference person; 07's spare wheel.</summary>
        private static void StageRelics(Transform lander, Material material, TemporaryObjects temporary,
            List<CameraPose> poses)
        {
            Vector3 row = lander.TransformPoint(new Vector3(-9f, 0f, 6.5f));
            Quaternion facing = lander.rotation * Quaternion.Euler(0f, 90f, 0f);
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(row, facing);
            Vector3 side = facing * Vector3.right;
            Vector3 ahead = facing * Vector3.forward;
            Person(material, temporary, row - side * 1.7f, facing);
            IReadOnlyList<string> relics = RelicModelBuilder.Ids;
            for (int i = 0; i < relics.Count; i++)
            {
                GameObject relic = Instantiate(RelicModelBuilder.PrefabName(relics[i]), temporary,
                    ArtPaths.RelicFolder);
                float lift = relic.transform.position.y - RendererBounds(relic).min.y;
                Vector3 spot = row + ahead * 2.2f + side * ((i - 2.5f) * 0.6f);
                relic.transform.SetPositionAndRotation(spot + Vector3.up * lift,
                    facing * Quaternion.Euler(0f, 180f, 0f));
            }

            Transform wheel = Descendant(rover.transform, "Wheel_RL");
            poses.Add(Pose("relics_and_reference", Point(row + ahead * 5.5f + side * 1.2f + Vector3.up * 1.6f),
                Point(row + ahead * 1f + Vector3.up * 0.6f), 50f));
            poses.Add(Pose("spare_wheel_close", Point(wheel.position - side * 1.5f - ahead * 1.1f + Vector3.up * 0.5f),
                Point(wheel.position), 45f));
        }

        /// <summary>
        /// The tower stage Main.unity shows, its service hatch swung open and 07 stopped before the port, nose to the
        /// hopper.
        /// </summary>
        private static void StageTowerPort(TemporaryObjects temporary, List<CameraPose> poses)
        {
            Transform stage = ActiveTowerStage();
            Descendant(stage, "ServiceHatch").localRotation = Quaternion.Euler(0f, OpenHatchYaw, 0f);
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(stage.TransformPoint(new Vector3(0f, 0f, TowerServiceStop)),
                stage.rotation * Quaternion.Euler(0f, 180f, 0f));
            Vector3 port = stage.TransformPoint(new Vector3(-0.1f, 0.85f, 1.1f));
            poses.Add(Pose("tower_port", Point(stage.TransformPoint(new Vector3(3.4f, 2.2f, 5.2f))), Point(port), 50f));
            poses.Add(Pose("tower_port_close", Point(stage.TransformPoint(new Vector3(2.7f, 1.8f, 4.1f))),
                Point(port), 40f));
        }

        /// <summary>
        /// 07 resting on its dock on the apron, nose to the contacts, the charging glow lit; the belly lift as it has
        /// sat jammed for decades.
        /// </summary>
        private static void StageDockAndLift(Transform lander, TemporaryObjects temporary, List<CameraPose> poses)
        {
            Transform dock = Descendant(lander, "DockAnchor");
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(dock.position, dock.rotation);
            SetGlow(lander, "DockGlow", new[] { string.Empty }, 1f);
            Vector3 lift = Descendant(lander, BaseModelBuilder.PlatformName).position;
            poses.Add(Pose("dock_rest", Point(dock.TransformPoint(new Vector3(-1.2f, 2.2f, -6f))),
                Point(dock.position + Vector3.up * 0.9f), 50f));
            poses.Add(Pose("dock_close", Point(dock.TransformPoint(new Vector3(1.3f, 1.3f, 2.2f))),
                Point(dock.TransformPoint(new Vector3(0f, 0.6f, 1.2f))), 50f));
            poses.Add(Pose("lift", Point(lander.TransformPoint(new Vector3(6.5f, 3f, 11f))),
                Point(lift + Vector3.up * 1.2f), 50f));
            poses.Add(Pose("lift_close", Point(lander.TransformPoint(new Vector3(3.2f, 1.6f, 5.5f))),
                Point(lift + Vector3.up * 0.4f), 45f));
        }

        /// <summary>
        /// The reference person at the foot of the crew's ladder, and inside the lab beside the keepsake shelf (the
        /// crew's cabin was theirs before it was 07's).
        /// </summary>
        private static void StageHumanScale(Transform lander, Material material, TemporaryObjects temporary,
            List<CameraPose> poses)
        {
            Person(material, temporary, lander.TransformPoint(new Vector3(-3.3f, 0f, 4.9f)), lander.rotation);
            poses.Add(Pose("human_ladder", Point(lander.TransformPoint(new Vector3(-1.2f, 1.8f, 11f))),
                Point(lander.TransformPoint(new Vector3(-3.4f, 1.6f, 4.2f))), 45f));
            Transform shelf = Descendant(lander, BaseModelBuilder.StationPrefix + "Keepsakes");
            Person(material, temporary, shelf.TransformPoint(new Vector3(-1.3f, -1.25f, 0.2f)), shelf.rotation);
            poses.Add(Pose("human_lab", Point(shelf.TransformPoint(new Vector3(0f, 1.05f, 4.4f))),
                Point(shelf.TransformPoint(new Vector3(-0.6f, -0.3f, 0f))), 55f));
        }

        /// <summary>Stands a capture-only 1.75 m reference person at <paramref name="position"/>.</summary>
        private static void Person(Material material, TemporaryObjects temporary, Vector3 position, Quaternion facing)
        {
            GameObject person = MeshObject("ReferencePerson", ReferencePerson().ToMesh("ReferencePerson"), material,
                temporary);
            person.transform.SetPositionAndRotation(position, facing);
        }

        /// <summary>The radio tower stage the open scene shows (fails loudly if none is active).</summary>
        private static Transform ActiveTowerStage()
        {
            for (int level = 1; level <= 3; level++)
            {
                string name = BaseModelBuilder.TowerPrefix + level.ToString(CultureInfo.InvariantCulture);
                Transform stage = SceneObject(name);
                if (stage.gameObject.activeInHierarchy)
                {
                    return stage;
                }
            }

            throw new InvalidOperationException("The open scene shows no radio tower stage.");
        }

        /// <summary>
        /// A plain 1.75 m person for scale checks in captures only (legs, body, arms, head in charcoal): never part
        /// of the game's content.
        /// </summary>
        private static LowPolyMeshBuilder ReferencePerson()
        {
            var b = new LowPolyMeshBuilder(200);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(Place.At(side * 0.1f, 0.42f, 0f), new Vector3(0.13f, 0.84f, 0.16f), PaletteSwatch.Charcoal,
                    0.03f);
                b.Box(Place.At(side * 0.27f, 1.12f, 0f), new Vector3(0.09f, 0.62f, 0.1f), PaletteSwatch.Charcoal,
                    0.03f);
            }

            b.Box(Place.At(0f, 1.15f, 0f), new Vector3(0.4f, 0.62f, 0.22f), PaletteSwatch.Charcoal, 0.05f);
            b.Icosphere(Place.At(0f, 1.62f, 0f), 0.13f, 1, PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>
        /// The first object called <paramref name="name"/> in the open scene (fails loudly if missing).
        /// </summary>
        private static Transform SceneObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform hit = Search(root.transform, name);
                if (hit != null)
                {
                    return hit;
                }
            }

            throw new InvalidOperationException($"The open scene has no object named {name}.");
        }

        internal static float[] Point(Vector3 p)
        {
            return new[] { p.x, p.y, p.z };
        }

        /// <summary>Stands one tape of each style on the first slots of a cassette shelf instance.</summary>
        private static void FillShelf(Transform shelf, TemporaryObjects temporary)
        {
            IReadOnlyList<CassetteStyle> styles = CassetteModelBuilder.Styles;
            for (int i = 0; i < styles.Count; i++)
            {
                Transform slot = Descendant(shelf, "Slot_" + i);
                GameObject tape = Instantiate(CassetteModelBuilder.PrefabName(styles[i].Id), temporary,
                    ArtPaths.PickupFolder);
                float lift = tape.transform.position.y - RendererBounds(tape).min.y;
                tape.transform.SetPositionAndRotation(slot.position + slot.up * lift, slot.rotation);
            }
        }

        /// <summary>Lights the first <paramref name="count"/> glow renderers named prefix0, prefix1, ...</summary>
        private static void SetGlow(Transform root, string prefix, int count, float intensity)
        {
            var suffixes = new string[count];
            for (int i = 0; i < count; i++)
            {
                suffixes[i] = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            SetGlow(root, prefix, suffixes, intensity);
        }

        /// <summary>
        /// Lights the glow renderers named prefix + each suffix with a linear multiplier (the MaterialPropertyBlock
        /// contract: SetVector, never the gamma-encoded SetColor).
        /// </summary>
        private static void SetGlow(Transform root, string prefix, string[] suffixes, float intensity)
        {
            var block = new MaterialPropertyBlock();
            block.SetVector("_EmissionColor", new Vector4(intensity, intensity, intensity, 1f));
            foreach (string suffix in suffixes)
            {
                Descendant(root, prefix + suffix).GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        /// <summary>07 at rest at a spot with its headlamp on as a warm spot light.</summary>
        internal static Transform Rover(TemporaryObjects temporary, Vector3 position, float yaw)
        {
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            Descendant(rover.transform, "Neck").localRotation = Quaternion.Euler(0f, RestingNeckYawDegrees, 0f);
            Descendant(rover.transform, "Head").localRotation = Quaternion.Euler(RestingHeadPitchDegrees, 0f, 0f);
            Descendant(rover.transform, "Eyelid").localRotation = Quaternion.Euler(RestingEyelidDegrees, 0f, 0f);
            Descendant(rover.transform, "SolarWing").localRotation = Quaternion.Euler(RestingWingDegrees, 0f, 0f);

            Transform socket = Descendant(rover.transform, "HeadlampSocket");
            Light headlamp = NewLight("Headlamp", LightType.Spot, Palette.Get(PaletteSwatch.WarmLamp), 9f);
            headlamp.transform.SetPositionAndRotation(socket.position, socket.rotation);
            headlamp.range = 16f;
            headlamp.spotAngle = 80f;
            headlamp.innerSpotAngle = 35f;
            headlamp.shadows = LightShadows.Soft;
            temporary.Add(headlamp.gameObject);
            return rover.transform;
        }

        internal static GameObject Instantiate(string prefabName, TemporaryObjects temporary,
            string folder = ArtPaths.BaseFolder)
        {
            string path = $"{folder}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"ArtLightingPreview: {path} is missing; run the Art builders.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            temporary.Add(instance);
            return instance;
        }

        private static Bounds RendererBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        internal static CameraPoseSet Poses(params CameraPose[] poses)
        {
            return new CameraPoseSet { width = 1280, height = 720, postProcessing = true, poses = poses };
        }

        internal static CameraPose Pose(string name, float[] position, float[] lookAt, float fov)
        {
            return new CameraPose { name = name, position = position, lookAt = lookAt, fov = fov };
        }

        internal static GameObject MeshObject(string name, Mesh mesh, Material material, TemporaryObjects temporary)
        {
            temporary.Add(mesh);
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        internal static Light NewLight(string name, LightType type, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            return light;
        }

        internal static GameObject NewVolume(TemporaryObjects temporary)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            var volumeObject = new GameObject("PreviewVolume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volumeObject;
        }

        internal static Transform Descendant(Transform root, string name)
        {
            Transform hit = Search(root, name);
            if (hit == null)
            {
                throw new InvalidOperationException($"{root.name} has no node named {name}.");
            }

            return hit;
        }

        private static Transform Search(Transform node, string name)
        {
            if (node.name == name)
            {
                return node;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform hit = Search(node.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Everything the preview creates, destroyed afterwards so no state outlives the capture.</summary>
        internal sealed class TemporaryObjects : IDisposable
        {
            private readonly List<Object> _objects = new List<Object>();

            public void Add(Object item)
            {
                _objects.Add(item);
            }

            public void Dispose()
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }

                _objects.Clear();
            }
        }
    }
}
