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

        /// <summary>The broken mast's lean: back (away from home) and to its left, about 13.5 degrees.</summary>
        public static readonly Vector3 BrokenLeanEuler = new Vector3(-9f, 0f, 10f);

        /// <summary>The broken dish, hanging face-down from its mount and twisted.</summary>
        public static readonly Vector3 BrokenDishEuler = new Vector3(72f, 25f, 18f);

        [MoonBuilder("Art/Relay", 180)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            ModelPrefabWriter.Write(CreateMast(false), ArtPaths.RelayFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateMast(true), ArtPaths.RelayFolder, material, glowOff);
            ModelPrefabWriter.Write(CreatePart(), ArtPaths.RelayFolder, material);
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
            root.Add(new ModelNode("PartSocket", RelayMeshes.PartSocket));
            root.Add(new ModelNode("BeamPoint", RelayMeshes.BeamPoint));
            return root;
        }

        public static ModelNode CreatePart()
        {
            return new ModelNode(PartName, Vector3.zero,
                new ModelMesh(PartName, RecipeKit.CentredOnMass(RelayMeshes.Module())));
        }
    }
}
