using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Art;
using MoonProject.Core;
using MoonProject.Editor.Builders;
using MoonProject.Gameplay;
using Object = UnityEngine.Object;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Renders 256x256 three-quarter studio icons with a transparent background for every non-pending structure in
    /// <see cref="StructureCatalog"/> into <c>Assets/_Project/Generated/UI/StructureIcons/&lt;id&gt;.png</c>, framed at
    /// ~78% of the icon canvas.
    /// </summary>
    internal static class StructureIconBuilder
    {
        public const string BuilderPath = "UI/StructureIcons";
        public const int BuilderOrder = 635;

        private const int IconSize = 256;
        private const float TargetFillRatio = 0.85f;
        private const float FieldOfView = 24f;
        private const float CameraElevation = 28f;
        private const float CameraAzimuth = -145f;
        private const int StudioLayer = Layers.Rover;

        private static readonly Vector3 StageOrigin = Vector3.zero;
        private static readonly Color KeyColor = new Color(1.0f, 0.95f, 0.86f, 1f);
        private static readonly Color RimColor = new Color(0.55f, 0.78f, 1.0f, 1f);

        [MoonBuilder(BuilderPath, BuilderOrder)]
        internal static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StructureCatalog>(UiAssetPaths.StructureCatalog);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: {UiAssetPaths.StructureCatalog} is missing (run Gameplay/Content).");
            }

            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: {UiAssetPaths.StructureCatalog} is invalid: {problem}.");
            }

            GeneratedAssets.EnsureFolder(UiAssetPaths.StructureIconFolder);

            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            int renderedCount = 0;
            try
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    StructureDefinition definition = catalog.StructureAt(i);
                    if (definition == null || definition.ArtPending)
                    {
                        continue;
                    }

                    Texture2D icon = RenderStructureIcon(definition.Prefab);
                    string iconPath = UiAssetPaths.StructureIcon(definition.Id);
                    WriteIcon(iconPath, icon);
                    renderedCount++;
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previousAsync;
            }

            Debug.Log($"{BuilderPath}: wrote {renderedCount} structure icon(s) under {UiAssetPaths.StructureIconFolder}");
        }

        private static Texture2D RenderStructureIcon(GameObject prefab)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            var stageRoot = new GameObject("[StructureIconStage]") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(stageRoot, previewScene);
            stageRoot.transform.position = StageOrigin;

            var rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                GameObject model = Object.Instantiate(prefab, stageRoot.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                SetLayerRecursively(model.transform, StudioLayer);

                if (!MeshBounds.TryLocal(model.transform, model.transform, out Bounds localBounds))
                {
                    localBounds = new Bounds(new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 1.6f, 1.6f));
                }

                Vector3 worldCenter = model.transform.TransformPoint(localBounds.center);
                Quaternion camRot = Quaternion.Euler(CameraElevation, CameraAzimuth, 0f);
                Quaternion invCamRot = Quaternion.Inverse(camRot);

                float tanHalfFov = Mathf.Tan(0.5f * FieldOfView * Mathf.Deg2Rad);
                float requiredDistance = 1.5f;
                Vector3 min = localBounds.min;
                Vector3 max = localBounds.max;

                for (int cx = 0; cx < 2; cx++)
                {
                    for (int cy = 0; cy < 2; cy++)
                    {
                        for (int cz = 0; cz < 2; cz++)
                        {
                            var cornerLocal = new Vector3(
                                cx == 0 ? min.x : max.x,
                                cy == 0 ? min.y : max.y,
                                cz == 0 ? min.z : max.z);
                            Vector3 cornerWorld = model.transform.TransformPoint(cornerLocal);
                            Vector3 camRel = invCamRot * (cornerWorld - worldCenter);
                            float span = Mathf.Max(Mathf.Abs(camRel.x), Mathf.Abs(camRel.y));
                            float distForCorner = span / (TargetFillRatio * tanHalfFov) - camRel.z;
                            if (distForCorner > requiredDistance)
                            {
                                requiredDistance = distForCorner;
                            }
                        }
                    }
                }

                var keyObj = new GameObject("KeyLight") { hideFlags = HideFlags.HideAndDontSave };
                keyObj.transform.SetParent(stageRoot.transform, false);
                keyObj.transform.rotation = Quaternion.Euler(42f, -125f, 0f);
                var keyLight = keyObj.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = KeyColor;
                keyLight.intensity = 1.45f;
                keyLight.shadows = LightShadows.None;
                keyLight.cullingMask = 1 << StudioLayer;

                var rimObj = new GameObject("RimLight") { hideFlags = HideFlags.HideAndDontSave };
                rimObj.transform.SetParent(stageRoot.transform, false);
                rimObj.transform.rotation = Quaternion.Euler(24f, 45f, 0f);
                var rimLight = rimObj.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = RimColor;
                rimLight.intensity = 0.75f;
                rimLight.shadows = LightShadows.None;
                rimLight.cullingMask = 1 << StudioLayer;

                var camObj = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave };
                camObj.transform.SetParent(stageRoot.transform, false);
                camObj.transform.SetPositionAndRotation(
                    worldCenter - camRot * Vector3.forward * requiredDistance,
                    camRot);

                var cam = camObj.AddComponent<Camera>();
                cam.scene = previewScene;
                cam.fieldOfView = FieldOfView;
                cam.aspect = 1f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 50f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.cullingMask = 1 << StudioLayer;
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();
                cam.targetTexture = null;

                RenderTexture.active = rt;
                var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(stageRoot);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private static void WriteIcon(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
