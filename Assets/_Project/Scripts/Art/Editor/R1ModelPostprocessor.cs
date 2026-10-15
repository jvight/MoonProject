using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Enforces R1 import rules on Blender FBX models under <c>Assets/_Project/Art/R1/Models/</c> and texture maps
    /// under <c>Assets/_Project/Art/R1/Textures/</c>. Maps the 11 fixed Blender material slot names to the R1
    /// material library and fails loudly via <see cref="Debug.LogError(object)"/> on any unknown slot name.
    /// </summary>
    public sealed class R1ModelPostprocessor : AssetPostprocessor
    {
        public const uint PostprocessorVersion = 1;
        public const string ModelsPrefix = "Assets/_Project/Art/R1/Models/";
        public const string TexturesPrefix = "Assets/_Project/Art/R1/Textures/";

        private static readonly byte[] BinaryMaterialSuffix =
            Encoding.ASCII.GetBytes("\x00\x01Material");

        public override uint GetVersion()
        {
            return PostprocessorVersion;
        }

        /// <summary>Returns true when <paramref name="assetPath"/> is an FBX model inside the R1 models folder.</summary>
        public static bool IsR1ModelPath(string assetPath)
        {
            string normalized = NormalizePath(assetPath);
            return normalized.StartsWith(ModelsPrefix, StringComparison.Ordinal)
                && normalized.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true when <paramref name="assetPath"/> is a PNG texture inside the R1 textures folder.</summary>
        public static bool IsR1TexturePath(string assetPath)
        {
            string normalized = NormalizePath(assetPath);
            return normalized.StartsWith(TexturesPrefix, StringComparison.Ordinal)
                && normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves a Blender material slot name to its library <see cref="Material"/>. Logs an error containing
        /// <paramref name="modelPath"/> and <paramref name="slotName"/> if the slot name is not in
        /// <see cref="R1MaterialCatalog"/>.
        /// </summary>
        public static bool TryResolveSlotMaterial(string slotName, string modelPath, out Material material)
        {
            string cleanSlot = StripUnityMaterialSuffix(slotName);
            if (!R1MaterialCatalog.TryGetBySlotName(cleanSlot, out R1MaterialSpec spec))
            {
                Debug.LogError(
                    $"R1ModelPostprocessor: Model '{modelPath}' uses unknown Blender material slot '{cleanSlot}'. " +
                    "All slots must match one of the 11 fixed R1_* names in R1MaterialCatalog.");
                material = null;
                return false;
            }

            material = R1MaterialLibrary.LoadMaterial(spec.Slot);
            return material != null;
        }

        /// <summary>
        /// Maps <paramref name="slotNames"/> to R1 library materials and assigns them to every
        /// <see cref="Renderer"/> under <paramref name="root"/>. Returns false and logs an error if any slot is unknown.
        /// </summary>
        public static bool AssignSlotsToHierarchy(GameObject root, IReadOnlyList<string> slotNames, string modelPath)
        {
            if (root == null || slotNames == null || slotNames.Count == 0)
            {
                return true;
            }

            var resolved = new Material[slotNames.Count];
            bool allValid = true;
            for (int i = 0; i < slotNames.Count; i++)
            {
                if (TryResolveSlotMaterial(slotNames[i], modelPath, out Material mat))
                {
                    resolved[i] = mat;
                }
                else
                {
                    allValid = false;
                }
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                int targetCount = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                    ? renderer.sharedMaterials.Length
                    : resolved.Length;
                var assigned = new Material[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    assigned[i] = resolved[i < resolved.Length ? i : resolved.Length - 1];
                }

                renderer.sharedMaterials = assigned;
            }

            return allValid;
        }

        /// <summary>
        /// Extracts Blender material slot names in order from raw FBX file bytes (supports both binary FBX 7100-7700
        /// and ASCII FBX 7.x files).
        /// </summary>
        public static List<string> ExtractFbxMaterialSlotNames(byte[] fbxBytes)
        {
            var slots = new List<string>();
            if (fbxBytes == null || fbxBytes.Length == 0)
            {
                return slots;
            }

            // 1. Check binary FBX Material records: 'S' + int32(nameLen + 10) + <slotName> + "\x00\x01Material"
            int suffixLen = BinaryMaterialSuffix.Length;
            for (int i = 5; i <= fbxBytes.Length - suffixLen; i++)
            {
                bool matchesSuffix = true;
                for (int k = 0; k < suffixLen; k++)
                {
                    if (fbxBytes[i + k] != BinaryMaterialSuffix[k])
                    {
                        matchesSuffix = false;
                        break;
                    }
                }

                if (!matchesSuffix)
                {
                    continue;
                }

                // Walk backward to find the 'S' property header
                for (int nameLen = 1; nameLen <= 128 && i - nameLen - 5 >= 0; nameLen++)
                {
                    int headerPos = i - nameLen - 5;
                    if (fbxBytes[headerPos] != (byte)'S')
                    {
                        continue;
                    }

                    int totalLen = fbxBytes[headerPos + 1]
                        | (fbxBytes[headerPos + 2] << 8)
                        | (fbxBytes[headerPos + 3] << 16)
                        | (fbxBytes[headerPos + 4] << 24);
                    if (totalLen == nameLen + suffixLen)
                    {
                        string slotName = Encoding.UTF8.GetString(fbxBytes, i - nameLen, nameLen);
                        if (!string.IsNullOrEmpty(slotName) && !slots.Contains(slotName))
                        {
                            slots.Add(slotName);
                        }

                        break;
                    }
                }
            }

            if (slots.Count > 0)
            {
                return slots;
            }

            // 2. Check ASCII FBX Material records: "Material::<SlotName>"
            string text = Encoding.UTF8.GetString(fbxBytes);
            const string Marker = "\"Material::";
            int searchIndex = 0;
            while (searchIndex < text.Length)
            {
                int found = text.IndexOf(Marker, searchIndex, StringComparison.Ordinal);
                if (found < 0)
                {
                    break;
                }

                int nameStart = found + Marker.Length;
                int nameEnd = text.IndexOf('"', nameStart);
                if (nameEnd < 0)
                {
                    break;
                }

                string slotName = text.Substring(nameStart, nameEnd - nameStart).Trim();
                if (!string.IsNullOrEmpty(slotName) && !slots.Contains(slotName))
                {
                    slots.Add(slotName);
                }

                searchIndex = nameEnd + 1;
            }

            return slots;
        }

        /// <summary>
        /// Generates a minimal valid ASCII FBX 7.3 mesh file with the specified Blender material slot names.
        /// Used by EditMode tests to exercise <see cref="R1ModelPostprocessor"/> end-to-end.
        /// </summary>
        public static byte[] CreateMinimalAsciiFbxBytes(params string[] slotNames)
        {
            string[] effectiveSlots = slotNames != null && slotNames.Length > 0
                ? slotNames
                : new[] { R1MaterialCatalog.CreamEnamelSlot };
            int matCount = effectiveSlots.Length;

            var sb = new StringBuilder(2048);
            sb.AppendLine("; FBX 7.3.0 project file");
            sb.AppendLine("FBXHeaderExtension:  {");
            sb.AppendLine("    FBXHeaderVersion: 1003");
            sb.AppendLine("    FBXVersion: 7300");
            sb.AppendLine("}");
            sb.AppendLine("GlobalSettings:  {");
            sb.AppendLine("    Version: 1000");
            sb.AppendLine("    Properties70:  {");
            sb.AppendLine("        P: \"UpAxis\", \"int\", \"Integer\", \"\",1");
            sb.AppendLine("        P: \"UpAxisSign\", \"int\", \"Integer\", \"\",1");
            sb.AppendLine("        P: \"FrontAxis\", \"int\", \"Integer\", \"\",2");
            sb.AppendLine("        P: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");
            sb.AppendLine("        P: \"CoordAxis\", \"int\", \"Integer\", \"\",0");
            sb.AppendLine("        P: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
            sb.AppendLine("        P: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("Definitions:  {");
            sb.AppendLine("    Version: 100");
            sb.AppendLine($"    Count: {2 + matCount}");
            sb.AppendLine("    ObjectType: \"Model\" {");
            sb.AppendLine("        Count: 1");
            sb.AppendLine("    }");
            sb.AppendLine("    ObjectType: \"Geometry\" {");
            sb.AppendLine("        Count: 1");
            sb.AppendLine("    }");
            sb.AppendLine("    ObjectType: \"Material\" {");
            sb.AppendLine($"        Count: {matCount}");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("Objects:  {");
            sb.AppendLine("    Geometry: 1001, \"Geometry::R1TestMesh\", \"Mesh\" {");
            sb.AppendLine("        Vertices: *12 {");
            sb.AppendLine("            a: 0,0,0,1,0,0,1,1,0,0,1,0");
            sb.AppendLine("        }");
            sb.AppendLine("        PolygonVertexIndex: *6 {");
            sb.AppendLine("            a: 0,1,-3,0,2,-4");
            sb.AppendLine("        }");
            sb.AppendLine("        GeometryVersion: 124");
            sb.AppendLine("        LayerElementNormal: 0 {");
            sb.AppendLine("            Version: 101");
            sb.AppendLine("            Name: \"\"");
            sb.AppendLine("            MappingInformationType: \"ByPolygonVertex\"");
            sb.AppendLine("            ReferenceInformationType: \"Direct\"");
            sb.AppendLine("            Normals: *18 {");
            sb.AppendLine("                a: 0,0,1,0,0,1,0,0,1,0,0,1,0,0,1,0,0,1");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        LayerElementUV: 0 {");
            sb.AppendLine("            Version: 101");
            sb.AppendLine("            Name: \"UVMap\"");
            sb.AppendLine("            MappingInformationType: \"ByPolygonVertex\"");
            sb.AppendLine("            ReferenceInformationType: \"IndexToDirect\"");
            sb.AppendLine("            UV: *8 {");
            sb.AppendLine("                a: 0,0,1,0,1,1,0,1");
            sb.AppendLine("            }");
            sb.AppendLine("            UVIndex: *6 {");
            sb.AppendLine("                a: 0,1,2,0,2,3");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        LayerElementMaterial: 0 {");
            sb.AppendLine("            Version: 101");
            sb.AppendLine("            Name: \"\"");
            sb.AppendLine("            MappingInformationType: \"ByPolygon\"");
            sb.AppendLine("            ReferenceInformationType: \"IndexToDirect\"");
            sb.AppendLine("            Materials: *2 {");
            int secondMatIdx = matCount > 1 ? 1 : 0;
            sb.AppendLine($"                a: 0,{secondMatIdx}");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        Layer: 0 {");
            sb.AppendLine("            Version: 100");
            sb.AppendLine("            LayerElement:  {");
            sb.AppendLine("                Type: \"LayerElementNormal\"");
            sb.AppendLine("                TypedIndex: 0");
            sb.AppendLine("            }");
            sb.AppendLine("            LayerElement:  {");
            sb.AppendLine("                Type: \"LayerElementMaterial\"");
            sb.AppendLine("                TypedIndex: 0");
            sb.AppendLine("            }");
            sb.AppendLine("            LayerElement:  {");
            sb.AppendLine("                Type: \"LayerElementUV\"");
            sb.AppendLine("                TypedIndex: 0");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    Model: 1002, \"Model::R1TestModel\", \"Mesh\" {");
            sb.AppendLine("        Version: 232");
            sb.AppendLine("        Properties70:  {");
            sb.AppendLine("            P: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",0,0,0");
            sb.AppendLine("            P: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",0,0,0");
            sb.AppendLine("            P: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");
            sb.AppendLine("        }");
            sb.AppendLine("        Shading: T");
            sb.AppendLine("        Culling: \"CullingOff\"");
            sb.AppendLine("    }");

            for (int i = 0; i < matCount; i++)
            {
                int matId = 2000 + i;
                sb.AppendLine($"    Material: {matId}, \"Material::{effectiveSlots[i]}\", \"\" {{");
                sb.AppendLine("        Version: 102");
                sb.AppendLine("        ShadingModel: \"phong\"");
                sb.AppendLine("        MultiLayer: 0");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
            sb.AppendLine("Connections:  {");
            sb.AppendLine("    C: \"OO\",1002,0");
            sb.AppendLine("    C: \"OO\",1001,1002");
            for (int i = 0; i < matCount; i++)
            {
                int matId = 2000 + i;
                sb.AppendLine($"    C: \"OO\",{matId},1002");
            }

            sb.AppendLine("}");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private void OnPreprocessTexture()
        {
            if (!IsR1TexturePath(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool isNormal = name.EndsWith("_Normal", StringComparison.OrdinalIgnoreCase);
            bool isMask = name.EndsWith("_Mask", StringComparison.OrdinalIgnoreCase);

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormal && !isMask;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
        }

        private void OnPreprocessModel()
        {
            if (!IsR1ModelPath(assetPath))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = true;
            importer.isReadable = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsR1ModelPath(assetPath))
            {
                return;
            }

            string normalizedPath = NormalizePath(assetPath);
            if (!File.Exists(normalizedPath))
            {
                return;
            }

            byte[] fbxBytes = File.ReadAllBytes(normalizedPath);
            List<string> slotNames = ExtractFbxMaterialSlotNames(fbxBytes);
            if (slotNames.Count > 0)
            {
                AssignSlotsToHierarchy(root, slotNames, normalizedPath);
            }
        }

        private static string StripUnityMaterialSuffix(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
            {
                return string.Empty;
            }

            const string InstanceSuffix = " (Instance)";
            string trimmed = rawName.Trim();
            if (trimmed.EndsWith(InstanceSuffix, StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - InstanceSuffix.Length).Trim();
            }

            return trimmed;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }
    }
}
