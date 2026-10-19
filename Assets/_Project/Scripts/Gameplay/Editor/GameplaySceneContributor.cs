using System;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The Gameplay domain's part of Main.unity: under [Gameplay], the <see cref="GameplaySystem"/> (one system,
    /// initialised after World and Rover) and its parts (salvage, the battery, base building), wired to the tuning,
    /// content and material assets, and to the Lumen ruin pieces World stood under [World]/Ruins (the scan targets' and
    /// supply crates' hosts). There is no prebuilt home (opening-v2 §5): 07 builds its base. The salvage site, the
    /// opening's crates and the scan targets are placed from the World's surface and anchors at boot (the editor
    /// previews the wreck: <see cref="GameplayEditPreview"/>). Fails loudly when a required asset is missing.
    /// </summary>
    public sealed class GameplaySceneContributor : ISceneContributor
    {
        /// <summary>World's service port under [World] (World's opening layout contract).</summary>
        private const string LanderPortPath = "Lander/ServicePort";
        private const string ApronOutlinePath = "OpeningProps/ApronMarkings/BayGlowOutline";
        private const string ApronCableChannelPath = "OpeningProps/ApronMarkings/CableChannel_Anchor";

        public int Order => 500;

        public void Contribute(SceneBuildContext context)
        {
            var visuals = context.LoadAsset<GameplayVisuals>(GameplayAssetPaths.Visuals);
            var baseTuning = context.LoadAsset<BaseTuning>(GameplayAssetPaths.BaseTuning);
            var glintTuning = context.LoadAsset<GlintTuning>(GameplayAssetPaths.GlintTuning);
            var salvageTuning = context.LoadAsset<SalvageTuning>(GameplayAssetPaths.SalvageTuning);
            var batteryTuning = context.LoadAsset<BatteryTuning>(GameplayAssetPaths.BatteryTuning);
            var cargoTuning = context.LoadAsset<CargoTuning>(GameplayAssetPaths.CargoTuning);
            var cutterTuning = context.LoadAsset<CutterTuning>(GameplayAssetPaths.CutterTuning);
            var scanTuning = context.LoadAsset<ScanTuning>(GameplayAssetPaths.ScanTuning);
            var openingTuning = context.LoadAsset<OpeningTuning>(GameplayAssetPaths.OpeningTuning);
            var buildingTuning = context.LoadAsset<BuildingTuning>(GameplayAssetPaths.BuildingTuning);
            var structureCatalog = context.LoadAsset<StructureCatalog>(GameplayAssetPaths.StructureCatalog);
            var materialCatalog = context.LoadAsset<MaterialCatalog>(GameplayAssetPaths.MaterialCatalog);
            var salvageCatalog = context.LoadAsset<SalvageCatalog>(GameplayAssetPaths.SalvageCatalog);
            var databankCatalogue = context.LoadAsset<DatabankCatalogue>(GameplayAssetPaths.DatabankCatalogue);
            Require(visuals.Validate(), nameof(GameplayVisuals));
            Require(salvageCatalog.Validate(), nameof(SalvageCatalog));
            Require(materialCatalog.Validate(), nameof(MaterialCatalog));
            Require(structureCatalog.Validate(), nameof(StructureCatalog));
            Require(databankCatalogue.Validate(), nameof(DatabankCatalogue));

            GameObject host = context.CreateChild("GameplaySystem", context.GameplayRoot.transform);
            var gameplay = host.AddComponent<GameplaySystem>();
            var salvage = Part<SalvageField>(context, host, "Salvage");
            var battery = Part<BatteryField>(context, host, "Battery");
            var building = Part<BuildField>(context, host, "Building");
            salvage.Wire(salvageTuning, salvageCatalog);
            battery.Wire(batteryTuning);
            building.Wire(structureCatalog, buildingTuning);
            gameplay.Wire(visuals, glintTuning, salvage);
            gameplay.WireBattery(battery);
            gameplay.WireBuilding(building);
            gameplay.WireCargo(materialCatalog, cargoTuning);
            gameplay.WireCutter(cutterTuning);
            gameplay.WireScans(scanTuning);
            gameplay.WireDatabank(databankCatalogue);
            gameplay.WireRuins(Ruins(context.WorldRoot.transform));
            gameplay.WireStoryProps(StoryProps(context.WorldRoot.transform));
            gameplay.WireLanderPort(LanderPort(context.WorldRoot.transform));
            gameplay.WireApronOutline(ApronOutline(context.WorldRoot.transform));
            gameplay.WireApronCableChannel(ApronCableChannel(context.WorldRoot.transform));
            gameplay.WireOpening(openingTuning, baseTuning);
            context.AddSystem(gameplay);
        }

        /// <summary>
        /// Every Lumen ruin piece World stood under [World]/Ruins: the scan sets' hosts and the supply crates' frames.
        /// </summary>
        private static Transform[] Ruins(Transform worldRoot)
        {
            Transform ruinsRoot = worldRoot.Find("Ruins");
            if (ruinsRoot == null)
            {
                throw new InvalidOperationException("Gameplay scene build: [World] has no 'Ruins' node (the Lumen " +
                                                    "ruins contract, opening-v2 §4).");
            }

            var ruins = new Transform[ruinsRoot.childCount];
            for (int i = 0; i < ruins.Length; i++)
            {
                ruins[i] = ruinsRoot.GetChild(i);
            }

            return ruins;
        }

        /// <summary>World's story props in the basin under [World]/BasinProps (basin dressing §2): story scan hosts.</summary>
        private static Transform[] StoryProps(Transform worldRoot)
        {
            Transform propsRoot = worldRoot.Find("BasinProps");
            if (propsRoot == null)
            {
                throw new InvalidOperationException("Gameplay scene build: [World] has no 'BasinProps' node (the " +
                                                    "basin's story props, basin-dressing §2).");
            }

            var props = new Transform[propsRoot.childCount];
            for (int i = 0; i < props.Length; i++)
            {
                props[i] = propsRoot.GetChild(i);
            }

            return props;
        }

        /// <summary>World's service port on the lander, where the apron cable ends: the open crate's frame.</summary>
        private static Transform LanderPort(Transform worldRoot)
        {
            Transform port = worldRoot.Find(LanderPortPath);
            if (port == null)
            {
                throw new InvalidOperationException($"Gameplay scene build: [World] has no '{LanderPortPath}' (the " +
                                                    "lander's service port, opening-v2 §4).");
            }

            return port;
        }

        /// <summary>World's apron cable channel: a pad on the apron cables to it, into the lander's cable.</summary>
        private static Transform ApronCableChannel(Transform worldRoot)
        {
            Transform channel = worldRoot.Find(ApronCableChannelPath);
            if (channel == null)
            {
                throw new InvalidOperationException($"Gameplay scene build: [World] has no '{ApronCableChannelPath}' " +
                                                    "(the lander's cable across the apron).");
            }

            return channel;
        }

        /// <summary>The bay outline on World's apron markings: it glows while build mode is open.</summary>
        private static Renderer ApronOutline(Transform worldRoot)
        {
            Transform outline = worldRoot.Find(ApronOutlinePath);
            Renderer renderer = outline != null ? outline.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                throw new InvalidOperationException($"Gameplay scene build: [World] has no renderer at " +
                                                    $"'{ApronOutlinePath}' (the apron markings' bay outline).");
            }

            return renderer;
        }

        private static T Part<T>(SceneBuildContext context, GameObject host, string name) where T : Component
        {
            return context.CreateChild(name, host.transform).AddComponent<T>();
        }

        private static void Require(string problem, string asset)
        {
            if (problem != null)
            {
                throw new InvalidOperationException($"Gameplay scene build: {asset} {problem}. " +
                                                    "Run the Gameplay builders (MoonProject/Build/Build All).");
            }
        }
    }
}
