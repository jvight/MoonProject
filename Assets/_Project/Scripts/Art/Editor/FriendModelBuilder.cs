using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Friends (docs/features/M3-02-friends-tilly.md) in Generated/Art/Friends: <c>Tilly</c> (repaired rig),
    /// <c>Tilly_Broken</c> (the same nodes lying on her side in the dust, a bent rotor and antenna, eye dark) and her
    /// part pickups <c>Part_TillyRotor|Lens|Cell</c>. Tilly's root stands on the ground between her feet (+Z = gaze),
    /// so she sits on the lander's FriendSocket_tilly as is; parts are pivoted at the centre of mass like scrap.
    /// Eye and PartLamp_0..2 are their own renderers: PartLamps (and the broken Eye) use the glow-off material and
    /// are lit at runtime through a MaterialPropertyBlock _EmissionColor.
    /// </summary>
    public static class FriendModelBuilder
    {
        public const string TillyName = "Tilly";
        public const string TillyBrokenName = "Tilly_Broken";

        /// <summary>
        /// How the broken Tilly lies: rolled onto her left side (the INES sticker faces the sky), turned and tipped so
        /// her dark eye looks down into the dust.
        /// </summary>
        public static readonly Vector3 BrokenPoseEuler = new Vector3(14f, 25f, 78f);

        /// <summary>How far the broken pose sinks into the dust.</summary>
        public const float BrokenSink = 0.015f;

        /// <summary>Part pickup prefab names fixed for gameplay.</summary>
        public static IReadOnlyList<string> PartNames =>
            new[] { "Part_TillyRotor", "Part_TillyLens", "Part_TillyCell" };

        [MoonBuilder("Art/Friends", 160)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            ModelPrefabWriter.Write(CreateTilly(false), ArtPaths.FriendFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateTilly(true), ArtPaths.FriendFolder, material, glowOff);
            foreach (string part in PartNames)
            {
                ModelPrefabWriter.Write(CreatePart(part), ArtPaths.FriendFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Tilly as repaired (<paramref name="broken"/> false) or as found on the crater floor.</summary>
        public static ModelNode CreateTilly(bool broken)
        {
            string name = broken ? TillyBrokenName : TillyName;
            Quaternion pose = broken ? Place.Rotation(BrokenPoseEuler) : Quaternion.identity;
            LowPolyMeshBuilder body = TillyMeshes.Body();
            if (broken)
            {
                TillyMeshes.Dust(body, pose);
            }

            var rotor = new ModelMesh(TillyName + "_Rotor", TillyMeshes.Rotor());
            var parts = new List<NodeSpec>
            {
                new NodeSpec("Body", Vector3.zero, new ModelMesh(name + "_Body", body), ModelMaterial.Palette),
                new NodeSpec("Eye", TillyMeshes.EyeCentre, new ModelMesh(TillyName + "_Eye", TillyMeshes.Eye()),
                    broken ? ModelMaterial.PaletteGlowOff : ModelMaterial.Palette),
            };
            string[] rotorNames = { "Rotor_FL", "Rotor_FR", "Rotor_RL", "Rotor_RR" };
            for (int i = 0; i < TillyMeshes.RotorCount; i++)
            {
                ModelMesh mesh = broken && i == 1
                    ? new ModelMesh(TillyBrokenName + "_RotorBent", TillyMeshes.BentRotor())
                    : rotor;
                parts.Add(new NodeSpec(rotorNames[i], TillyMeshes.RotorCentre(i), mesh, ModelMaterial.Palette));
            }

            parts.Add(new NodeSpec("Antenna", TillyMeshes.AntennaBase, broken
                ? new ModelMesh(TillyBrokenName + "_AntennaBent", TillyMeshes.BentAntenna())
                : new ModelMesh(TillyName + "_Antenna", TillyMeshes.Antenna()), ModelMaterial.Palette));
            var lamp = new ModelMesh(TillyName + "_PartLamp", TillyMeshes.PartLamp());
            for (int i = 0; i < TillyMeshes.PartLampCount; i++)
            {
                parts.Add(new NodeSpec("PartLamp_" + i.ToString(CultureInfo.InvariantCulture),
                    TillyMeshes.PartLampPosition(i), lamp, ModelMaterial.PaletteGlowOff));
            }

            parts.Add(new NodeSpec("TetherPoint", TillyMeshes.TetherPoint, null, ModelMaterial.Palette));

            Matrix4x4 placement = Matrix4x4.Rotate(pose);
            if (broken)
            {
                placement = Matrix4x4.Translate(Vector3.down * (LowestPoint(parts, placement) + BrokenSink)) *
                    placement;
            }

            var root = new ModelNode(name, Vector3.zero);
            foreach (NodeSpec part in parts)
            {
                root.Add(new ModelNode(part.Name, placement.MultiplyPoint3x4(part.Position), pose, part.Mesh,
                    part.Material));
            }

            return root;
        }

        public static ModelNode CreatePart(string name)
        {
            LowPolyMeshBuilder geometry;
            switch (name)
            {
                case "Part_TillyRotor":
                    geometry = FriendPartMeshes.TillyRotor();
                    break;
                case "Part_TillyLens":
                    geometry = FriendPartMeshes.TillyLens();
                    break;
                case "Part_TillyCell":
                    geometry = FriendPartMeshes.TillyCell();
                    break;
                default:
                    throw new ArgumentException($"Unknown friend part '{name}'.", nameof(name));
            }

            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        private static float LowestPoint(List<NodeSpec> parts, Matrix4x4 placement)
        {
            float lowest = float.MaxValue;
            foreach (NodeSpec part in parts)
            {
                if (part.Mesh == null)
                {
                    continue;
                }

                Matrix4x4 node = placement * Matrix4x4.Translate(part.Position);
                IReadOnlyList<Vector3> positions = part.Mesh.Geometry.Positions;
                for (int v = 0; v < positions.Count; v++)
                {
                    lowest = Mathf.Min(lowest, node.MultiplyPoint3x4(positions[v]).y);
                }
            }

            return lowest;
        }

        private readonly struct NodeSpec
        {
            public NodeSpec(string name, Vector3 position, ModelMesh mesh, ModelMaterial material)
            {
                Name = name;
                Position = position;
                Mesh = mesh;
                Material = material;
            }

            public string Name { get; }

            public Vector3 Position { get; }

            public ModelMesh Mesh { get; }

            public ModelMaterial Material { get; }
        }
    }
}
