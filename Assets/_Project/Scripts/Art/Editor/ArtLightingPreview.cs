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
    ///                  builtfor07]
    /// </code>
    /// (default rover; a ';'-separated list captures several scenes in one run)
    /// rover: 07 at rest (half-lidded, head lowered, wing ajar) with its headlamp. base: the lander with the shelf
    /// and the L3 tower on their anchors, its lamp sockets lit, 07 coming home. towers: L1-L3 side by side seen
    /// from 15 m and 55 m. relics: the six relics on the lit museum shelf, and seen from 15 m. shadow: 07's cast
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
    /// material bundles and the Kestrel trail's loose bits on the dust beside 07. kit: 07 bare, with each crafted
    /// kit piece (lamp bar lit, drums glowing as if boosting, the cargo cradle carrying a relic), fully kitted with
    /// the friends' gifts, and "minute one versus hour five" side by side from the chase camera and at 30 m. kitmain:
    /// the same comparison in Main.unity under its own grade: minute one beside fully kitted at the spawn, travelling,
    /// with play's road lights and glow levels, from the chase camera and at 30 m, each 07 from its own chase camera,
    /// and the lamp bar close. home:
    /// the base as Main.unity has it (its own grading), from the spawn view, the lander close and three-quarter, the
    /// tower, the shelf area, a 07 parked in front from the chase camera and close, and Kenji's Rover Bay (sign lit)
    /// from the front and the quarter; homeclean: the same with every Weather_* layer hidden, as restoration will
    /// leave it. builtfor07: Main.unity with the Rover Bay on the WorkshopAnchor and 07 parked on its turntable (lamps
    /// and sign lit, one arm lowered as if fitting), the true-size relics in a row beside 07 and a 1.75 m person (a
    /// capture-only reference), 07's spare wheel close, the tower port, the dock, the lift and the human-scale
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
        private const float ChasePitch = 16f;
        private const float ChaseTargetHeight = 1.1f;
        private const float ChaseFov = 52f;
        private const float ChaseQuarterYaw = 35f;
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
                    case "home":
                        poses = HomeScene(temporary, true);
                        break;
                    case "homeclean":
                        poses = HomeScene(temporary, false);
                        break;
                    case "builtfor07":
                        poses = BuiltFor07Scene(material, temporary);
                        break;
                    default:
                        Debug.LogError($"ArtLightingPreview: unknown scene '{scene}' " +
                            "(rover|base|towers|relics|shadow|grit|friends|workshop|bell|bellbroken|pickups|" +
                            "bellcorner|warmpoints|relaysbroken|relayslit|sites|sitesclean|salvagebits|kit|kitmain|" +
                            "home|homeclean|builtfor07).");
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
        private static void NightSetting(Material material, TemporaryObjects temporary, float floorRadius,
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
            Instantiate(BaseModelBuilder.ShelfName, temporary).transform.position = BaseModelBuilder.ShelfAnchor;
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

            temporary.Add(Rover(temporary, new Vector3(2.4f, 0f, 9f), 195f).gameObject);
            Transform perch = Descendant(lander.transform, "FriendSocket_tilly");
            GameObject tilly = Instantiate(FriendModelBuilder.TillyName, temporary, ArtPaths.FriendFolder);
            tilly.transform.SetPositionAndRotation(perch.position, perch.rotation * Quaternion.Euler(0f, -30f, 0f));
            return Poses(
                Pose("perch", new[] { -3.6f, 3.6f, 3.6f }, new[] { -1.6f, 3.2f, 0.9f }, 40f),
                Pose("approach", new[] { 4f, 5f, 26f }, new[] { 0f, 2.5f, 0f }, 45f),
                Pose("homecoming", new[] { 4.5f, 3.6f, 14.5f }, new[] { 1.5f, 1.8f, 4f }, 50f),
                Pose("porch", new[] { -2.5f, 2.2f, 6.5f }, new[] { 0.2f, 2.6f, 1.5f }, 45f),
                Pose("shelf", new[] { 6f, 2.4f, 6.5f }, new[] { 6f, 1.5f, 1.2f }, 45f),
                Pose("far55m", new[] { -18f, 9f, 52f }, new[] { -2f, 4f, 0f }, 40f));
        }

        private static CameraPoseSet TowersScene(TemporaryObjects temporary)
        {
            for (int level = 1; level <= 3; level++)
            {
                Instantiate(BaseModelBuilder.TowerPrefix + level, temporary).transform.position =
                    new Vector3((level - 2) * 6f, 0f, 0f);
                Light lamp = NewLight("TowerLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 2f);
                lamp.transform.position = new Vector3((level - 2) * 6f + 1.2f, 1.6f, 1.6f);
                lamp.range = 6f;
                temporary.Add(lamp.gameObject);
            }

            return Poses(
                Pose("near15m", new[] { 0f, 4f, 15f }, new[] { 0f, 4.5f, 0f }, 50f),
                Pose("far55m", new[] { 8f, 8f, 55f }, new[] { 0f, 4f, 0f }, 30f));
        }

        private static CameraPoseSet RelicsScene(TemporaryObjects temporary)
        {
            GameObject shelf = Instantiate(BaseModelBuilder.ShelfName, temporary);
            IReadOnlyList<string> ids = RelicModelBuilder.Ids;
            for (int i = 0; i < ids.Count; i++)
            {
                Transform slot = Descendant(shelf.transform, "Slot_" + i);
                GameObject relic = Instantiate(RelicModelBuilder.PrefabName(ids[i]), temporary, ArtPaths.RelicFolder);
                float lift = relic.transform.position.y - RendererBounds(relic).min.y;
                relic.transform.position = slot.position + Vector3.up * lift;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                GameObject relic = Instantiate(RelicModelBuilder.PrefabName(ids[i]), temporary, ArtPaths.RelicFolder);
                float lift = relic.transform.position.y - RendererBounds(relic).min.y;
                relic.transform.SetPositionAndRotation(new Vector3((i - 2.5f) * 1.4f, lift, 4f),
                    Quaternion.Euler(0f, 20f, 0f));
            }

            Light lamp = NewLight("ShelfLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 3f);
            lamp.transform.position = new Vector3(0f, 2.6f, 2.6f);
            lamp.range = BaseLampRange;
            lamp.shadows = LightShadows.Soft;
            temporary.Add(lamp.gameObject);
            return Poses(
                Pose("shelf", new[] { 0.6f, 1.9f, 6.2f }, new[] { 0f, 1.45f, 0f }, 45f),
                Pose("row", new[] { 0.5f, 1.6f, 7.5f }, new[] { 0f, 0.4f, 4f }, 50f),
                Pose("far15m", new[] { 3f, 4f, 19f }, new[] { 0f, 0.6f, 4f }, 40f));
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
            Instantiate(BaseModelBuilder.ShelfName, temporary).transform.position = BaseModelBuilder.ShelfAnchor;
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
            Instantiate(BaseModelBuilder.ShelfName, temporary).transform.position = BaseModelBuilder.ShelfAnchor;
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
            Ground(Instantiate(SiteModelBuilder.BundleName(SalvageMaterial.Wiring), temporary, ArtPaths.PickupFolder),
                new Vector3(0.95f, 0f, 2.6f));
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

            Transform bar = Attach(rover, RoverKitBuilder.LampBarName, "HeadlampSocket", temporary);
            SetGlow(bar, RoverKitBuilder.LampPrefix, RoverKitMeshes.LampCount, LampBarGlow);
            road.transform.position = Descendant(bar, RoverKitBuilder.LampPrefix + "1").position;
            road.color = WarmLightColor;
            road.intensity = WarmLightIntensity;
            road.range = WarmLightRange;
            road.spotAngle = WarmLightAngle;
            road.innerSpotAngle = WarmLightInnerAngle;
            foreach (string drumSocket in new[] { "DrumSocket_L", "DrumSocket_R" })
            {
                Transform drum = Attach(rover, RoverKitBuilder.CapacitorDrumName, drumSocket, temporary);
                SetGlow(drum, RoverKitBuilder.DrumGlowName, new[] { string.Empty }, DrumIdleGlow);
            }

            Attach(rover, RoverKitBuilder.CargoRackName, "CargoSocket", temporary);
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
            Vector3 shelf = lander.TransformPoint(BaseModelBuilder.ShelfAnchor);
            Vector3 roverBack = rover.transform.TransformDirection(new Vector3(0.6f, 0f, -0.8f));
            Transform bay = SceneObject(BaseModelBuilder.RoverBayName);
            SetGlow(bay, "BaySign", new[] { string.Empty }, 1f);
            Vector3 bayCentre = bay.TransformPoint(RoverBayMeshes.TurntableCentre + Vector3.up * 1.6f);
            var poses = new List<CameraPose>
            {
                new CameraPose { name = "spawn_first_frame", position = new[] { 0.78f, 4f, -8.97f },
                    euler = new[] { 6f, -5f, 0f }, fov = 60f },
                Pose("lander_close", Point(lander.TransformPoint(new Vector3(0.6f, 2.3f, 8.4f))),
                    Point(lander.TransformPoint(new Vector3(0f, 2.6f, 0f))), 55f),
                Pose("lander_quarter", Point(lander.TransformPoint(new Vector3(6.2f, 3.2f, 6.4f))),
                    Point(lander.TransformPoint(new Vector3(0f, 2.2f, 0f))), 50f),
                Pose("tower", Point(tower + lander.TransformDirection(new Vector3(4.5f, 3f, 8f))),
                    Point(tower + Vector3.up * 3.5f), 55f),
                Pose("shelf_area", Point(shelf + lander.TransformDirection(new Vector3(1.8f, 2f, 5.5f))),
                    Point(shelf + Vector3.up * 1.1f), 50f),
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
        /// optionally with a relic riding in the cradle, and shows the friends' gifts.
        /// </summary>
        private static void FitKit(Transform rover, TemporaryObjects temporary, bool lampBar, bool drums, bool cradle,
            bool relic, bool gifts)
        {
            if (lampBar)
            {
                Transform bar = Attach(rover, RoverKitBuilder.LampBarName, "HeadlampSocket", temporary);
                SetGlow(bar, RoverKitBuilder.LampPrefix, RoverKitMeshes.LampCount, 1f);
            }

            if (drums)
            {
                foreach (string socket in new[] { "DrumSocket_L", "DrumSocket_R" })
                {
                    Transform drum = Attach(rover, RoverKitBuilder.CapacitorDrumName, socket, temporary);
                    SetGlow(drum, RoverKitBuilder.DrumGlowName, new[] { "" }, 1f);
                }
            }

            if (cradle)
            {
                Transform rack = Attach(rover, RoverKitBuilder.CargoRackName, "CargoSocket", temporary);
                if (relic)
                {
                    Transform seat = Descendant(rack, RoverKitBuilder.RelicSeatName);
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
            }
        }

        private static Transform Attach(Transform rover, string kit, string socket, TemporaryObjects temporary)
        {
            GameObject piece = Instantiate(kit, temporary, ArtPaths.RoverFolder);
            piece.transform.SetParent(Descendant(rover, socket), false);
            return piece.transform;
        }

        /// <summary>The three material bundles in a row by 07, the trail's loose bits strewn behind them.</summary>
        private static CameraPoseSet SalvageBitsScene(TemporaryObjects temporary)
        {
            Array materials = Enum.GetValues(typeof(SalvageMaterial));
            for (int i = 0; i < materials.Length; i++)
            {
                GameObject bundle = Instantiate(SiteModelBuilder.BundleName((SalvageMaterial)materials.GetValue(i)),
                    temporary, ArtPaths.PickupFolder);
                float lift = bundle.transform.position.y - RendererBounds(bundle).min.y;
                bundle.transform.SetPositionAndRotation(new Vector3((i - 1) * 0.6f, lift, 1.6f),
                    Quaternion.Euler(0f, 20f, 0f));
            }

            IReadOnlyList<string> debris = SiteModelBuilder.DebrisNames;
            for (int i = 0; i < debris.Count; i++)
            {
                GameObject bit = Instantiate(debris[i], temporary, ArtPaths.SitesFolder);
                float lift = bit.transform.position.y - RendererBounds(bit).min.y;
                bit.transform.SetPositionAndRotation(new Vector3((i - 2) * 1.2f, lift, 3.6f + (i % 2) * 0.8f),
                    Quaternion.Euler(0f, -30f + i * 35f, 0f));
            }

            temporary.Add(Rover(temporary, new Vector3(1.9f, 0f, 0.2f), -20f).gameObject);
            return Poses(
                Pose("bundles", new[] { 0f, 0.9f, 0.2f }, new[] { 0f, 0.12f, 1.6f }, 40f),
                Pose("debris", new[] { 0f, 2.2f, 0.4f }, new[] { 0f, 0.2f, 4f }, 45f),
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
            poses.Add(Pose("human_bench", Point(bay.TransformPoint(new Vector3(-1.2f, 1.6f, 6.4f))),
                Point(bay.TransformPoint(new Vector3(-3.8f, 1f, 1f))), 45f));
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
        /// 07 resting on its dock at the ladder's foot, nose to the contacts, the charging glow lit; the lift as it has
        /// hung for decades.
        /// </summary>
        private static void StageDockAndLift(Transform lander, TemporaryObjects temporary, List<CameraPose> poses)
        {
            Transform dock = Descendant(lander, "DockAnchor");
            GameObject rover = Instantiate(RoverModelBuilder.ModelName, temporary, ArtPaths.RoverFolder);
            rover.transform.SetPositionAndRotation(dock.position, dock.rotation);
            SetGlow(lander, "DockGlow", new[] { string.Empty }, 1f);
            Vector3 lift = Descendant(lander, "LiftPlatform").position;
            poses.Add(Pose("dock_rest", Point(lander.TransformPoint(new Vector3(1.8f, 2.2f, 10.5f))),
                Point(dock.position + Vector3.up * 0.9f), 50f));
            poses.Add(Pose("dock_close", Point(lander.TransformPoint(new Vector3(-1.3f, 1.3f, 2.35f))),
                Point(lander.TransformPoint(new Vector3(0f, 0.6f, 3.35f))), 50f));
            poses.Add(Pose("lift", Point(lander.TransformPoint(new Vector3(6.5f, 3.2f, 10f))),
                Point(lift + Vector3.up * 0.8f), 50f));
            poses.Add(Pose("lift_close", Point(lander.TransformPoint(new Vector3(4.2f, 2.4f, 7.2f))),
                Point(lift + lander.TransformDirection(new Vector3(0f, 0.6f, -1f))), 45f));
        }

        /// <summary>The reference person on the lander's porch beside the door and beside the museum shelf.</summary>
        private static void StageHumanScale(Transform lander, Material material, TemporaryObjects temporary,
            List<CameraPose> poses)
        {
            float deck = BaseModelBuilder.LanderDeckTop;
            Person(material, temporary, lander.TransformPoint(new Vector3(0.62f, deck, 1.62f)), lander.rotation);
            poses.Add(Pose("human_door", Point(lander.TransformPoint(new Vector3(-2.3f, deck + 1.1f, 6f))),
                Point(lander.TransformPoint(new Vector3(0.2f, deck + 0.9f, 1.5f))), 45f));
            Transform shelf = SceneObject(BaseModelBuilder.ShelfName);
            Person(material, temporary, shelf.TransformPoint(new Vector3(-1.75f, 0f, 0.5f)), shelf.rotation);
            poses.Add(Pose("human_shelf", Point(shelf.TransformPoint(new Vector3(0.6f, 1.3f, 4.8f))),
                Point(shelf.TransformPoint(new Vector3(-0.4f, 0.9f, 0.3f))), 45f));
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

        private static float[] Point(Vector3 p)
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
        private static Transform Rover(TemporaryObjects temporary, Vector3 position, float yaw)
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

        private static GameObject Instantiate(string prefabName, TemporaryObjects temporary,
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

        private static CameraPoseSet Poses(params CameraPose[] poses)
        {
            return new CameraPoseSet { width = 1280, height = 720, postProcessing = true, poses = poses };
        }

        private static CameraPose Pose(string name, float[] position, float[] lookAt, float fov)
        {
            return new CameraPose { name = name, position = position, lookAt = lookAt, fov = fov };
        }

        private static GameObject MeshObject(string name, Mesh mesh, Material material, TemporaryObjects temporary)
        {
            temporary.Add(mesh);
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private static Light NewLight(string name, LightType type, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            return light;
        }

        private static GameObject NewVolume(TemporaryObjects temporary)
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

        private static Transform Descendant(Transform root, string name)
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
        private sealed class TemporaryObjects : IDisposable
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
