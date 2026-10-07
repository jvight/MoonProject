using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    ///     [--arg scene=rover|base|towers|relics|shadow|grit|friends|workshop|bell|bellbroken|pickups|bellcorner]
    /// </code>
    /// (default rover; a ';'-separated list captures several scenes in one run)
    /// rover: 07 at rest (half-lidded, head lowered, wing ajar) with its headlamp. base: the lander with the shelf
    /// and the L3 tower on their anchors, its lamp sockets lit, 07 coming home. towers: L1-L3 side by side seen
    /// from 15 m and 55 m. relics: the six relics on the lit museum shelf, and seen from 15 m. shadow: 07's cast
    /// shadow under a low (30 degree) earthlight from its side, seen from the gameplay camera's height. grit: a
    /// dense field of <see cref="RockStyle.Grit"/> pebbles batched into one mesh around 07. friends: Tilly
    /// hovering beside 07 (3/3 part lamps lit), and broken on the dust among her amber parts (1/3 lit), near and
    /// from 30 m. The base scene also seats Tilly on her lander perch. workshop: Kenji's bench on the lander's
    /// WorkshopAnchor with its lamp lit, 07 on the bench pad wearing the Hover-Jump coils glowing as if charging.
    /// bell: Bell standing with her dial and 4/4 lamps lit beside 07 for scale, and a second Bell caught mid-dance
    /// (lid lifted, needle swept, a foot tapping) to prove the pivots, near and from 15 m and 30 m. bellbroken: Bell
    /// tipped back against a canyon wall, dark, with Ro's log cache, the Vol. 1 tape and her three parts in the dust,
    /// 07 arriving. pickups: the three parts, the three tapes and the cache on the dust, and the tape rack with the
    /// tapes in slots 0-2. bellcorner: the base with Bell on the L3 tower's BellCorner, her rack on its anchor, 07
    /// parked at her dial, the tower's upgrade pad ring and her 2.5 m clear circle drawn on the dust.
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

        /// <summary>The ground Bell keeps clear for her dance and for 07 parking at her dial.</summary>
        private const float BellClearRadius = 2.5f;

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
                    default:
                        Debug.LogError($"ArtLightingPreview: unknown scene '{scene}' " +
                            "(rover|base|towers|relics|shadow|grit|friends|workshop|bell|bellbroken|pickups|" +
                            "bellcorner).");
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
            Instantiate(BaseModelBuilder.TowerPrefix + "2", temporary).transform.position = BaseModelBuilder.TowerAnchor;
            Transform anchor = Descendant(lander.transform, "WorkshopAnchor");
            GameObject bench = Instantiate(BaseModelBuilder.WorkbenchName, temporary);
            bench.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            Transform bulb = Descendant(bench.transform, "Lights");
            Light lamp = NewLight("WorkLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 3f);
            lamp.transform.position = bulb.position - Vector3.up * 0.1f;
            lamp.range = 6f;
            lamp.shadows = LightShadows.Soft;
            temporary.Add(lamp.gameObject);
            for (int i = 0; i < 4; i++)
            {
                Light baseLamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp),
                    BaseLampIntensity);
                baseLamp.transform.position = Descendant(lander.transform, "LampSocket_" + i).position;
                baseLamp.range = BaseLampRange;
                temporary.Add(baseLamp.gameObject);
            }

            Vector3 pad = anchor.position + anchor.rotation * new Vector3(0f, 0f, 3.2f);
            Vector3 parked = pad + new Vector3(1.9f, 0f, 0.4f);
            Transform rover = Rover(temporary, parked, 200f);
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
                Pose("bench", new[] { 12.1f, 1.9f, 2.8f }, new[] { 12.5f, 1.3f, -1.9f }, 50f),
                Pose("coils", new[] { parked.x + 0.75f, 0.24f, parked.z + 2.1f },
                    new[] { parked.x, 0.2f, parked.z }, 45f),
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

        /// <summary>Lights the glow renderers named prefix + each suffix (MaterialPropertyBlock _EmissionColor).</summary>
        private static void SetGlow(Transform root, string prefix, string[] suffixes, float intensity)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_EmissionColor", Color.white * intensity);
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
