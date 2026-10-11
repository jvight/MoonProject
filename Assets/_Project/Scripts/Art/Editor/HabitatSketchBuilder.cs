using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The M3-19 sketch (draft, not adopted): <c>Generated/Art/Sketch/HabitatLander_Sketch.prefab</c>, a palette
    /// blockout of the habitat lander for the owner to see. The real Lander is untouched. Root on the ground under the
    /// lander's centre, +Z the front where 07 drives in under the belly. Nodes: <c>LiftPlatform</c> (down on the
    /// ground; its top at local y 0) with <c>RimLamps</c> and <c>RoverSpot</c>, <c>FloorHatch</c>, <c>Portholes</c>,
    /// <c>CornerLamps</c>, <c>LightTableGlow</c> and <c>DockGlow</c> (dark glow renderers) and <c>DockAnchor</c>.
    /// </summary>
    public static class HabitatSketchBuilder
    {
        public const string SketchName = "HabitatLander_Sketch";
        public const string PlatformName = "LiftPlatform";
        public const string FloorHatchName = "FloorHatch";
        public const string RoverSpotName = "RoverSpot";

        /// <summary>The lift platform's top when it is up in the cabin (sketch space y).</summary>
        public static float PlatformUpY => HabitatSketchMeshes.PlatformUpY;

        /// <summary>The camera ring rail inside the cabin (radius, height; sketch space).</summary>
        public static float RingRadius => HabitatSketchMeshes.RingRadius;

        public static float RingHeight => HabitatSketchMeshes.RingHeight;

        public static float FloorY => HabitatSketchMeshes.FloorY;

        [MoonBuilder("Art/Sketch", 199)]
        public static void Build()
        {
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateSketch()), ArtPaths.SketchFolder,
                PaletteAssetBuilder.LoadMaterial(), PaletteAssetBuilder.LoadGlowOffMaterial());
            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateSketch()
        {
            var root = new ModelNode(SketchName, Vector3.zero, new ModelMesh(SketchName, HabitatSketchMeshes.Hull()));
            Glow(root, "Portholes", HabitatSketchMeshes.Portholes());
            Glow(root, "CornerLamps", HabitatSketchMeshes.CornerLamps());
            Glow(root, "LightTableGlow", HabitatSketchMeshes.LightTableGlow());
            Glow(root, "DockGlow", HabitatSketchMeshes.DockGlow());
            root.Add(new ModelNode(FloorHatchName, Vector3.zero,
                new ModelMesh(SketchName + "_" + FloorHatchName, HabitatSketchMeshes.FloorHatch())));
            ModelNode platform = root.Add(new ModelNode(PlatformName,
                new Vector3(0f, HabitatSketchMeshes.PlatformDownY, 0f),
                new ModelMesh(SketchName + "_" + PlatformName, HabitatSketchMeshes.Platform())));
            platform.Add(new ModelNode("RimLamps", Vector3.zero, Quaternion.identity,
                new ModelMesh(SketchName + "_RimLamps", HabitatSketchMeshes.RimLamps()), ModelMaterial.PaletteGlowOff));
            platform.Add(new ModelNode(RoverSpotName, Vector3.zero));
            root.Add(new ModelNode("DockAnchor", new Vector3(0f, 0.14f, HabitatSketchMeshes.DockZ)));
            return root;
        }

        private static void Glow(ModelNode root, string name, LowPolyMeshBuilder mesh)
        {
            root.Add(new ModelNode(name, Vector3.zero, Quaternion.identity,
                new ModelMesh(SketchName + "_" + name, mesh), ModelMaterial.PaletteGlowOff));
        }
    }
}
