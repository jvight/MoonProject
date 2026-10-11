using System;
using UnityEditor;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Import settings of the Blender-built models under <see cref="ArtPaths.Rover08Folder"/>. They live in code, not
    /// in the FBX's .meta, because generated metas are recreated by whichever editor imports first: geometry, normals
    /// and both palette UV sets come in exactly as Blender wrote them (UV0 the swatch cell, UV1.x the baked
    /// occlusion LofiLit reads), with no materials, cameras, lights, animation or compression.
    /// </summary>
    public sealed class Rover08ImportRules : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtPaths.Rover08Folder + "/", StringComparison.Ordinal))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.weldVertices = true;
            importer.generateSecondaryUV = false;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
        }
    }
}
