using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The relay masts of the station-reach network (docs/features/M3-06-relay-network.md) in Generated/Art/Relay:
    /// <c>RelayMast</c> (restored, standing straight, dish aimed home), <c>RelayMast_Broken</c> (the same nodes, the
    /// mast leaning back and to its left away from the pad, the dish hanging, guy wires slack, the junction box open)
    /// and the relay part pickup <c>Part_RelayModule</c>. The root stands on the ground at the pad centre, +Z = toward
    /// home; the mast rises from the back edge of the pad. Lamp is its own renderer on the glow-off material in both:
    /// dark until gameplay lights it through the linear MaterialPropertyBlock contract, when it glows HDR WarmLamp.
    /// </summary>
    public static class RelayModelBuilder
    {
        public const string MastName = "RelayMast";
        public const string BrokenMastName = "RelayMast_Broken";
        public const string PartName = "Part_RelayModule";

        /// <summary>
        /// The service cabinet (0.5.5 §2): <c>Cabinet/Door</c> pivoted on its left hinge (turn it about local Y,
        /// negative, to swing it open; the restored mast's stands open) with <c>CutPoint</c> and <c>Seal</c>
        /// (M_LowPolyGlowOff) on its free edge, and <c>Cabinet/ItemSocket</c> where the spare part waits inside.
        /// </summary>
        public const string CabinetName = "Cabinet";

        /// <summary>
        /// relay.2's service hut (0.5.5 §1.4, §1.7), <c>RelayHut</c>: an open-fronted lean-to placed at the mast's foot,
        /// its root on the ground, +Z its open side; 06 slumps against it.
        /// </summary>
        public const string HutName = "RelayHut";

        public const string CabinetDoorName = "Door";
        public const string CabinetSealName = "Seal";

        /// <summary>The broken mast's lean: back (away from home) and to its left, about 13.5 degrees.</summary>
        public static readonly Vector3 BrokenLeanEuler = new Vector3(-9f, 0f, 10f);

        /// <summary>The broken dish, hanging face-down from its mount and twisted.</summary>
        public static readonly Vector3 BrokenDishEuler = new Vector3(72f, 25f, 18f);

        [MoonBuilder("Art/Relay", 180)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateMast(false)), ArtPaths.RelayFolder, material, glowOff);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateMast(true)), ArtPaths.RelayFolder, material, glowOff);
            ModelPrefabWriter.Write(CreatePart(), ArtPaths.RelayFolder, material);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateHut()), ArtPaths.RelayFolder, material);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The mast restored (<paramref name="broken"/> false) or as 07 finds it.</summary>
        public static ModelNode CreateMast(bool broken)
        {
            string variant = broken ? BrokenMastName : MastName;
            var root = new ModelNode(variant, Vector3.zero);
            root.Add(new ModelNode("Base", Vector3.zero, new ModelMesh(variant + "_Base", RelayMeshes.Base(broken))));
            Quaternion lean = broken ? Place.Rotation(BrokenLeanEuler) : Quaternion.identity;
            ModelNode mast = root.Add(new ModelNode("Mast", RelayMeshes.MastFoot, lean,
                new ModelMesh(variant + "_Mast", RelayMeshes.Mast(broken))));
            Quaternion dish = broken ? Place.Rotation(BrokenDishEuler) : Quaternion.identity;
            mast.Add(new ModelNode("Dish", RelayMeshes.DishMount, dish,
                new ModelMesh(MastName + "_Dish", RelayMeshes.Dish())));
            mast.Add(new ModelNode("Lamp", RelayMeshes.LampCentre, Quaternion.identity,
                new ModelMesh(MastName + "_Lamp", RelayMeshes.Lamp()), ModelMaterial.PaletteGlowOff));
            ModelNode cabinet = root.Add(new ModelNode(CabinetName, Vector3.zero));
            ModelNode door = cabinet.Add(new ModelNode(CabinetDoorName, RelayMeshes.CabinetHinge,
                Place.Rotation(new Vector3(0f, broken ? 0f : RelayMeshes.CabinetDoorOpenDegrees, 0f)),
                new ModelMesh(MastName + "_" + CabinetDoorName, RelayMeshes.CabinetDoor())));
            door.Add(new ModelNode(SiteModelBuilder.CutPointName, RelayMeshes.CabinetSealPoint, Quaternion.identity));
            door.Add(new ModelNode(CabinetSealName, Vector3.zero, Quaternion.identity,
                new ModelMesh(MastName + "_" + CabinetSealName, RelayMeshes.CabinetSeal()),
                ModelMaterial.PaletteGlowOff));
            cabinet.Add(new ModelNode(LootContainerBuilder.ItemSocketName, RelayMeshes.CabinetItem));
            root.Add(new ModelNode("PartSocket", RelayMeshes.PartSocket));
            root.Add(new ModelNode("BeamPoint", RelayMeshes.BeamPoint));
            root.Add(new ModelNode("ChargeRing", Vector3.zero,
                new ModelMesh(variant + "_ChargeRing", RelayMeshes.ChargeRing(broken))));
            return root;
        }

        public static ModelNode CreateHut()
        {
            return new ModelNode(HutName, Vector3.zero, new ModelMesh(HutName, ScanSiteMeshes.Hut()));
        }

        public static ModelNode CreatePart()
        {
            return new ModelNode(PartName, Vector3.zero,
                new ModelMesh(PartName, RecipeKit.CentredOnMass(RelayMeshes.Module())));
        }
    }
}
