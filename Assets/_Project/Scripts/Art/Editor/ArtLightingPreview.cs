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
    ///     [--arg scene=rover|shadow|grit|relaysbroken|relayslit|sites|sitesclean|salvagebits|kit|kitmain|
    ///                  focuslens|orangebeads|containers|scan|fragments|scanplaces|derelicts]
    /// </code>
    /// (default rover; a ';'-separated list captures several scenes in one run)
    /// rover: 07 at rest (half-lidded, head lowered, wing ajar) with its headlamp. shadow: 07's cast
    /// shadow under a low (30 degree) earthlight from its side, seen from the gameplay camera's height. grit: a
    /// dense field of <see cref="RockStyle.Grit"/> pebbles batched into one mesh around 07.
    /// relaysbroken / relayslit: a relay mast on each of World's relay anchors in
    /// Main.unity, dark and leaning or restored and lit, from the spawn first frame and at each pad.
    /// sites / sitesclean: the five salvage sites on World's site anchors in Main.unity, whole or picked clean to their
    /// skeletons, from the way in, the side and 30 m, the depot close. salvagebits: the three
    /// material bundles on the dust beside 07. kit: 07 bare, with each crafted
    /// kit piece (lamp bar lit, drums glowing as if boosting, the cargo cradle carrying a relic), fully kitted,
    /// and "minute one versus hour five" side by side from the chase camera and at 30 m. kitmain:
    /// the same comparison in Main.unity under its own grade: minute one beside fully kitted at the spawn, travelling,
    /// with play's road lights and glow levels, from the chase camera and at 30 m, each 07 from its own chase camera,
    /// and the lamp bar close.
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
                    case "shadow":
                        NightSetting(material, temporary, 40f, 8f);
                        poses = ShadowScene(temporary);
                        break;
                    case "grit":
                        NightSetting(material, temporary, 40f, 9f);
                        poses = GritScene(material, temporary);
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

        /// <summary>A warm practical lamp (a point light) where World lights the lab's lamps in play.</summary>
        private static void WarmLamp(TemporaryObjects temporary, Vector3 position, float intensity, float range)
        {
            Light lamp = NewLight("LabLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), intensity);
            temporary.Add(lamp.gameObject);
            lamp.range = range;
            lamp.shadows = LightShadows.None;
            lamp.transform.position = position;
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

        /// <summary>Stands a capture-only 1.75 m reference person at <paramref name="position"/>.</summary>
        private static void Person(Material material, TemporaryObjects temporary, Vector3 position, Quaternion facing)
        {
            GameObject person = MeshObject("ReferencePerson", ReferencePerson().ToMesh("ReferencePerson"), material,
                temporary);
            person.transform.SetPositionAndRotation(position, facing);
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
            string folder)
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
