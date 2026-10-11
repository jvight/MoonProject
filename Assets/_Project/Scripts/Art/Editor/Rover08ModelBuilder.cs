using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds rover 08 (docs/design/gdd-v3.md §13.3) through Blender: runs <see cref="ScriptPath"/> headless with the
    /// palette written from <see cref="Palette"/>, copies the FBX it exports into Generated/Art/Rover08 only when its
    /// bytes changed (<see cref="Rover08ImportRules"/> owns the import settings) and writes <c>Rover08Model.prefab</c>
    /// with every part painted by M_LowPoly. Same contract as 07's prefab: metres, +Z forward, +Y up, origin at the
    /// ground contact patch, one node per moving part. The trial behind it: docs/design/trial-blender-svg.md.
    /// </summary>
    public static class Rover08ModelBuilder
    {
        public const string ModelName = "Rover08Model";
        public const string ScriptPath = "tools/blender/rover08.py";
        public const string FbxPath = ArtPaths.Rover08Folder + "/Rover08.fbx";
        public const string PrefabPath = ArtPaths.Rover08Folder + "/" + ModelName + ".prefab";

        /// <summary>The project-relative scratch folder Blender reads the palette from and exports into.</summary>
        private const string WorkFolder = "Temp/Blender";

        /// <summary>Parts the rig contract and the finale address by name (the antenna tip blinks the call sign).</summary>
        private static readonly string[] RequiredParts =
        {
            "Body", "Bogie_L", "Bogie_R", "Wheel_FL", "Wheel_FR", "Wheel_ML", "Wheel_MR", "Wheel_RL", "Wheel_RR",
            "Neck", "Head", "Eye", "SolarWing_L", "SolarWing_R", "Antenna", "AntennaTip",
        };

        /// <summary>Runs Blender, imports its FBX and writes the prefab (the palette material must exist).</summary>
        [MoonBuilder("Art/Rover08", 125)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            string work = BatchArgs.ProjectPath(WorkFolder);
            Directory.CreateDirectory(work);
            string palettePath = Path.Combine(work, "palette.json");
            string exportPath = Path.Combine(work, "Rover08.fbx");
            File.WriteAllText(palettePath, PaletteJson());
            File.Delete(exportPath);
            BlenderProcess.RunScript(BatchArgs.ProjectPath(ScriptPath),
                $"--palette \"{palettePath}\" --fbx \"{exportPath}\"");
            GeneratedAssets.WriteFileIfChanged(FbxPath, File.ReadAllBytes(exportPath));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null)
            {
                throw new InvalidOperationException($"{FbxPath} did not import as a model.");
            }

            WritePrefab(model, material);
        }

        // The palette as the bpy script reads it: grid size, then each swatch's name, index and sRGB hex.
        private static string PaletteJson()
        {
            var json = new StringBuilder();
            json.Append("{\"columns\":").Append(Palette.Columns).Append(",\"rows\":").Append(Palette.Rows)
                .Append(",\"swatches\":[");
            Array swatches = Enum.GetValues(typeof(PaletteSwatch));
            for (int i = 0; i < swatches.Length; i++)
            {
                var swatch = (PaletteSwatch)swatches.GetValue(i);
                Color32 colour = Palette.Get(swatch);
                json.Append(i == 0 ? "" : ",").Append("{\"name\":\"").Append(swatch).Append("\",\"index\":")
                    .Append((int)swatch).Append(",\"srgb\":\"")
                    .Append($"{colour.r:X2}{colour.g:X2}{colour.b:X2}").Append("\"}");
            }

            return json.Append("]}").ToString();
        }

        private static void WritePrefab(GameObject model, Material material)
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject root = scratch.Instantiate(model);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (string part in RequiredParts)
                {
                    if (Part(root.transform, part) == null)
                    {
                        throw new InvalidOperationException($"{FbxPath} has no part '{part}' ({ScriptPath}).");
                    }
                }

                if (Part(root.transform, "Eye").position.z <= 0f)
                {
                    throw new InvalidOperationException($"{FbxPath}: 08's lens must face +Z; check the export axes.");
                }

                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.sharedMaterial = material;
                }

                GeneratedAssets.SavePrefab(root, PrefabPath);
            }
        }

        private static Transform Part(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = child.name == name ? child : Part(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
