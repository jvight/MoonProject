using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's visible kit (docs/ARCHITECTURE.md, rover rig contract, "Visible kit (M3-11)") in Generated/Art/Rover, each
    /// prefab parented to its RoverModel socket with identity: <c>Kit_LampBar</c> (HeadlampSocket) with its three
    /// <c>Lamp_0..2</c> glasses, <c>Kit_CapacitorDrum</c> (DrumSocket_L and DrumSocket_R, the same prefab twice) with
    /// its <c>Glow</c> band, and <c>Kit_CargoRack</c> (CargoSocket) with the <c>RelicSeat</c> a carried relic rests on.
    /// The glasses and the band are their own renderers on the glow-off material: dark until rover lights them
    /// through the linear MaterialPropertyBlock contract (lamps WarmLamp, band cyan like the coils).
    /// </summary>
    public static class RoverKitBuilder
    {
        public const string LampBarName = "Kit_LampBar";
        public const string CapacitorDrumName = "Kit_CapacitorDrum";
        public const string CargoRackName = "Kit_CargoRack";
        public const string BatterySpineName = "Kit_BatterySpine";
        public const string TapeDeckName = "Kit_TapeDeck";
        public const string TetherRingName = "Kit_TetherRing";
        public const string SideBinName = "Kit_SideBin";
        public const string RingGlowName = "Glow";
        public const string LampPrefix = "Lamp_";
        public const string DrumGlowName = "Glow";
        public const string RelicSeatName = "RelicSeat";

        [MoonBuilder("Art/RoverKit", 121)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            ModelPrefabWriter.Write(CreateLampBar(), ArtPaths.RoverFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateCapacitorDrum(), ArtPaths.RoverFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateCargoRack(), ArtPaths.RoverFolder, material);
            ModelPrefabWriter.Write(CreateBatterySpine(), ArtPaths.RoverFolder, material);
            ModelPrefabWriter.Write(CreateTapeDeck(), ArtPaths.RoverFolder, material);
            ModelPrefabWriter.Write(CreateTetherRing(), ArtPaths.RoverFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateSideBin(), ArtPaths.RoverFolder, material);
            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLampBar()
        {
            var root = new ModelNode(LampBarName, Vector3.zero,
                new ModelMesh(LampBarName, RoverKitMeshes.LampBar()));
            var glass = new ModelMesh(LampBarName + "_Glass", RoverKitMeshes.LampGlass());
            for (int i = 0; i < RoverKitMeshes.LampCount; i++)
            {
                root.Add(new ModelNode(LampPrefix + i, RoverKitMeshes.LampGlassCentre(i), Quaternion.identity, glass,
                    ModelMaterial.PaletteGlowOff));
            }

            return root;
        }

        public static ModelNode CreateCapacitorDrum()
        {
            var root = new ModelNode(CapacitorDrumName, Vector3.zero,
                new ModelMesh(CapacitorDrumName, RoverKitMeshes.CapacitorDrum()));
            root.Add(new ModelNode(DrumGlowName, RoverKitMeshes.DrumCentre, Quaternion.identity,
                new ModelMesh(CapacitorDrumName + "_Glow", RoverKitMeshes.DrumGlow()), ModelMaterial.PaletteGlowOff));
            return root;
        }

        /// <summary>Cell Pack II's battery spine on SpineSocket (M3-20): two cream cells on a rail, brass bands.</summary>
        public static ModelNode CreateBatterySpine()
        {
            return new ModelNode(BatterySpineName, Vector3.zero,
                new ModelMesh(BatterySpineName, RoverSpineMeshes.Spine()));
        }

        /// <summary>A side bin (RoverAbility.SideBins) on BinSocket_L and BinSocket_R, the same prefab twice.</summary>
        public static ModelNode CreateSideBin()
        {
            return new ModelNode(SideBinName, Vector3.zero, new ModelMesh(SideBinName, RoverBinMeshes.Bin()));
        }

        /// <summary>The old tape deck on DeckSocket (M3-24 C), also the find Tilly spots on the ground.</summary>
        public static ModelNode CreateTapeDeck()
        {
            return new ModelNode(TapeDeckName, Vector3.zero, new ModelMesh(TapeDeckName, RoverDeckMeshes.Deck()));
        }

        /// <summary>
        /// Kenji's spare tether ring on the Eye (first-hour v2 §3): the brass bezel and its <c>Glow</c> band on the
        /// glow-off material, which rover lights faintly at rest and brightly while the tether holds.
        /// </summary>
        public static ModelNode CreateTetherRing()
        {
            var root = new ModelNode(TetherRingName, Vector3.zero,
                new ModelMesh(TetherRingName, RoverTetherRingMeshes.Ring()));
            root.Add(new ModelNode(RingGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(TetherRingName + "_Glow", RoverTetherRingMeshes.Band()), ModelMaterial.PaletteGlowOff));
            return root;
        }

        public static ModelNode CreateCargoRack()
        {
            var root = new ModelNode(CargoRackName, Vector3.zero,
                new ModelMesh(CargoRackName, RoverKitMeshes.CargoRack()));
            root.Add(new ModelNode(RelicSeatName, RoverKitMeshes.RelicSeat));
            return root;
        }
    }
}
