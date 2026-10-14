using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonProject.Art.Editor;
using MoonProject.Editor.Automation;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Batch harness for the soft low-poly look experiment (V0 baseline, V1 soft normals, V2 soft + depth/AA,
    /// V3 soft + near-camera detail &amp; dust grain). Rebuilds the procedural Art and Terrain meshes per variant,
    /// captures every pose in <c>Logs/look_poses.json</c> at 1920x1080, records vertex/triangle counts and GPU-synced
    /// frame times to <c>Logs/look/metrics.json</c>, and restores V0 assets on exit.
    /// </summary>
    public static class LookVariantCapture
    {
        private const string OcclusionFeature = "ScreenSpaceAmbientOcclusion";
        private const int TimingPasses = 6;

        [Serializable]
        private sealed class VariantMetric
        {
            public string variant;
            public double gpuFrameMs;
            public double cpuFrameMs;
            public int totalVertices;
            public int totalTriangles;
            public int terrainVertices;
            public int terrainTriangles;
            public int scatterVertices;
            public int scatterTriangles;
            public int modelVertices;
            public int modelTriangles;
        }

        [Serializable]
        private sealed class MetricsReport
        {
            public VariantMetric[] variants;
        }

        public static void CaptureAll()
        {
            BatchRunner.Run(nameof(CaptureAll), args =>
            {
                string posesRel = args.GetString("poses", "Logs/look_poses.json");
                string posesPath = BatchArgs.ProjectPath(posesRel);
                string outputRel = args.GetString("output", "Logs/look");
                string outputDir = BatchArgs.ProjectPath(outputRel);

                if (!File.Exists(posesPath))
                {
                    Debug.LogError($"LookVariantCapture: no pose file at {posesPath}");
                    return false;
                }

                CameraPoseSet poses = JsonUtility.FromJson<CameraPoseSet>(File.ReadAllText(posesPath));
                var worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
                if (worldSettings == null)
                {
                    Debug.LogError($"LookVariantCapture: no WorldSettings at {WorldPaths.Settings}");
                    return false;
                }

                RefineAddedPoses(poses, worldSettings, out Vector2 boulderFocusXZ);
                WriteFormattedPoseSet(poses, posesPath);

                var metrics = new VariantMetric[4];
                try
                {
                    for (int variant = 0; variant <= 3; variant++)
                    {
                        string tag = "V" + variant.ToString(CultureInfo.InvariantCulture);
                        BatchRunner.Log($"building look variant {tag}");
                        ApplyVariantAssetsAndGlobals(variant, worldSettings);

                        Scene scene = EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
                        worldSettings.Mesh.ConfigureForVariant(variant);
                        worldSettings.Mesh.SecondaryFineFocus = variant >= 3 ? boulderFocusXZ : Vector2.zero;
                        WorldSystem world = FindInScene<WorldSystem>(scene);
                        if (world == null)
                        {
                            Debug.LogError("LookVariantCapture: no WorldSystem in Main.unity");
                            return false;
                        }

                        world.RebuildPreview();

                        VariantMetric metric = CountSceneGeometry(scene, world, tag);
                        bool enableAa = variant >= 2;
                        metric.gpuFrameMs = CaptureAutomation.CapturePoseVariants(
                            poses, outputDir, tag, enableAa, TimingPasses, out double cpuMs);
                        metric.cpuFrameMs = cpuMs;
                        metrics[variant] = metric;

                        BatchRunner.Log(
                            $"variant {tag}: gpu={metric.gpuFrameMs:F2} ms, cpu={metric.cpuFrameMs:F2} ms, " +
                            $"tris={metric.totalTriangles} (terrain={metric.terrainTriangles}, " +
                            $"scatter={metric.scatterTriangles}, models={metric.modelTriangles}), " +
                            $"verts={metric.totalVertices}");
                    }
                }
                finally
                {
                    BatchRunner.Log("restoring V0 baseline assets and settings");
                    ApplyVariantAssetsAndGlobals(0, worldSettings);
                    worldSettings.Mesh.ConfigureForVariant(0);
                    worldSettings.Mesh.SecondaryFineFocus = Vector2.zero;
                    EditorUtility.SetDirty(worldSettings);
                    AssetDatabase.SaveAssets();
                }

                Directory.CreateDirectory(outputDir);
                string metricsPath = Path.Combine(outputDir, "metrics.json");
                File.WriteAllText(metricsPath, JsonUtility.ToJson(new MetricsReport { variants = metrics }, true));
                BatchRunner.Log($"wrote {metricsPath}");
                return true;
            });
        }

        [MenuItem("MoonProject/World/Look Variant: V0 (Current Baseline)")]
        private static void MenuVariant0() => SwitchEditorVariant(0);

        [MenuItem("MoonProject/World/Look Variant: V1 (Soft Normals)")]
        private static void MenuVariant1() => SwitchEditorVariant(1);

        [MenuItem("MoonProject/World/Look Variant: V2 (Soft + Depth & AA)")]
        private static void MenuVariant2() => SwitchEditorVariant(2);

        [MenuItem("MoonProject/World/Look Variant: V3 (Soft + Near Detail & Grain)")]
        private static void MenuVariant3() => SwitchEditorVariant(3);

        public static void SwitchEditorVariant(int variant)
        {
            var worldSettings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (worldSettings == null)
            {
                return;
            }

            ApplyVariantAssetsAndGlobals(variant, worldSettings);
            Scene scene = EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            worldSettings.Mesh.ConfigureForVariant(variant);
            WorldSystem world = FindInScene<WorldSystem>(scene);
            if (world != null)
            {
                world.RebuildPreview();
            }
        }

        private static void ApplyVariantAssetsAndGlobals(int variant, WorldSettings worldSettings)
        {
            float smoothAngle = variant >= 1 ? 52f : 0f;
            bool depth = variant >= 2;
            bool detail = variant >= 3;

            RockModelBuilder.Build(smoothAngle, depth ? 0.62f : 0f);
            RoverModelBuilder.Build(smoothAngle, depth);
            RoverKitBuilder.Build(smoothAngle);
            BaseModelBuilder.Build(smoothAngle);
            SiteModelBuilder.Build(smoothAngle);
            PropModelBuilder.Build(smoothAngle);
            LootContainerBuilder.Build(smoothAngle);
            DerelictBuilder.Build(smoothAngle);
            RelayModelBuilder.Build(smoothAngle);
            LandmarkModelBuilder.Build(smoothAngle);

            Shader.SetGlobalFloat("_MoonLookDepth", depth ? 1f : 0f);
            Shader.SetGlobalFloat("_MoonTerrainGrain", detail ? 1f : 0f);

            PostProcessSettings post = worldSettings.PostProcessing;
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(WorldPaths.PcRenderer);
            if (renderer != null)
            {
                ScriptableRendererFeature occlusion = null;
                foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
                {
                    if (feature != null && feature.GetType().Name == OcclusionFeature)
                    {
                        occlusion = feature;
                    }
                }

                if (occlusion != null)
                {
                    var serialized = new SerializedObject(occlusion);
                    SetSerializedFloat(serialized, "m_Settings.Intensity",
                        depth ? 0.52f : post.OcclusionIntensity);
                    SetSerializedFloat(serialized, "m_Settings.Radius",
                        depth ? 0.95f : post.OcclusionRadius);
                    SetSerializedFloat(serialized, "m_Settings.DirectLightingStrength",
                        depth ? 0.38f : post.OcclusionDirectLighting);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    occlusion.SetActive(true);
                    EditorUtility.SetDirty(occlusion);
                    EditorUtility.SetDirty(renderer);
                }
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WorldPaths.PcPipeline);
            if (pipeline != null)
            {
                pipeline.msaaSampleCount = depth ? 4 : 1;
                EditorUtility.SetDirty(pipeline);
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetSerializedFloat(SerializedObject serialized, string path, float value)
        {
            SerializedProperty prop = serialized.FindProperty(path);
            if (prop != null)
            {
                prop.floatValue = value;
            }
        }

        private static void RefineAddedPoses(CameraPoseSet poses, WorldSettings settings, out Vector2 boulderFocusXZ)
        {
            var surface = new MoonSurface(settings.Surface, WorldSettings.DefaultSeed);
            var anchors = new WorldAnchors(surface, settings.Surface.Canyon, settings.Relays);
            List<ScatterInstance> rocks = new ScatterPlanner(surface, settings.Scatter, anchors).Plan();

            ScatterInstance bestBoulder = default;
            Vector3 bestCamPos = Vector3.zero;
            Vector3 bestTarget = Vector3.zero;
            float bestScore = -1f;
            for (int i = 0; i < rocks.Count; i++)
            {
                ScatterInstance candidate = rocks[i];
                if (candidate.Kind != ScatterKind.Boulder || candidate.Size < 2.2f)
                {
                    continue;
                }

                float distFromBase = new Vector2(candidate.Position.x, candidate.Position.z).magnitude;
                if (distFromBase < 48f || distFromBase > 250f)
                {
                    continue;
                }

                Vector3 target = candidate.Position + new Vector3(0f, candidate.Size * 0.28f, 0f);
                Vector2 offsetDir = new Vector2(-candidate.Position.x, -candidate.Position.z).normalized;
                Vector2 side = new Vector2(-offsetDir.y, offsetDir.x);
                Vector2 camXZ = new Vector2(candidate.Position.x, candidate.Position.z)
                    + (offsetDir * 0.80f + side * 0.60f).normalized * (candidate.Size * 2.15f + 2.2f);
                float camY = surface.SampleHeight(camXZ.x, camXZ.y) + 1.55f;
                var camPos = new Vector3(camXZ.x, camY, camXZ.y);
                Vector3 forward = (target - camPos).normalized;
                float camToCandidate = Vector3.Distance(camPos, candidate.Position);

                bool blocked = false;
                float score = candidate.Size * 2.5f;
                int companionBoulders = 0;
                for (int j = 0; j < rocks.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Vector3 toRock = rocks[j].Position - camPos;
                    float dist = toRock.magnitude;
                    if (dist < 1e-3f)
                    {
                        continue;
                    }

                    float align = Vector3.Dot(toRock / dist, forward);
                    if (rocks[j].Kind == ScatterKind.Boulder && dist < camToCandidate - 0.3f && align > 0.82f)
                    {
                        blocked = true;
                        break;
                    }

                    if (rocks[j].Kind == ScatterKind.Boulder && dist < 34f && align > 0.68f)
                    {
                        score += (34f - dist) * 0.65f * rocks[j].Size * align;
                        companionBoulders++;
                    }
                    else if (rocks[j].Kind == ScatterKind.Pebble && dist < 16f && align > 0.68f)
                    {
                        score += (16f - dist) * 0.25f * align;
                    }
                }

                score += companionBoulders * 25f;
                if (!blocked && score > bestScore)
                {
                    bestScore = score;
                    bestBoulder = candidate;
                    bestCamPos = camPos;
                    bestTarget = target;
                }
            }

            boulderFocusXZ = bestScore > 0f
                ? new Vector2(bestBoulder.Position.x, bestBoulder.Position.z)
                : Vector2.zero;

            for (int i = 0; i < poses.poses.Length; i++)
            {
                CameraPose pose = poses.poses[i];
                if (pose.name == "4_boulder_field_close" && bestScore > 0f)
                {
                    pose.position = new[] { Round2(bestCamPos.x), Round2(bestCamPos.y), Round2(bestCamPos.z) };
                    pose.lookAt = new[] { Round2(bestTarget.x), Round2(bestTarget.y), Round2(bestTarget.z) };
                    pose.fov = 46f;
                }
                else if (pose.name == "6_kestrel_wreck_12m")
                {
                    Vector2 center = surface.Kestrel.Center;
                    Vector2 fall = surface.Kestrel.Fall;
                    Vector2 side = new Vector2(-fall.y, fall.x);
                    Vector2 camXZ = center - fall * 10.6f - side * 5.6f;
                    float camY = surface.SampleHeight(camXZ.x, camXZ.y) + 2.35f;
                    float lookY = surface.SampleHeight(center.x, center.y) + 0.95f;
                    pose.position = new[] { Round2(camXZ.x), Round2(camY), Round2(camXZ.y) };
                    pose.lookAt = new[] { Round2(center.x), Round2(lookY), Round2(center.y) };
                    pose.fov = 48f;
                }
            }
        }

        private static float Round2(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }

        private static void WriteFormattedPoseSet(CameraPoseSet set, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"width\": {0},", set.width));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"height\": {0},", set.height));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"postProcessing\": {0},",
                set.postProcessing ? "true" : "false"));
            sb.AppendLine("  \"poses\": [");
            for (int i = 0; i < set.poses.Length; i++)
            {
                CameraPose p = set.poses[i];
                string comma = i + 1 < set.poses.Length ? "," : "";
                if (p.lookAt != null && p.lookAt.Length == 3)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "    {{\"name\": \"{0}\", \"position\": [{1}, {2}, {3}], \"lookAt\": [{4}, {5}, {6}], \"fov\": {7}}}{8}",
                        p.name, p.position[0], p.position[1], p.position[2],
                        p.lookAt[0], p.lookAt[1], p.lookAt[2], p.fov, comma));
                }
                else if (p.euler != null && p.euler.Length == 3)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "    {{\"name\": \"{0}\", \"position\": [{1}, {2}, {3}], \"euler\": [{4}, {5}, {6}], \"fov\": {7}}}{8}",
                        p.name, p.position[0], p.position[1], p.position[2],
                        p.euler[0], p.euler[1], p.euler[2], p.fov, comma));
                }
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
        }

        private static VariantMetric CountSceneGeometry(Scene scene, WorldSystem world, string variantTag)
        {
            var metric = new VariantMetric
            {
                variant = variantTag,
                terrainTriangles = world.LastBuild.Triangles,
                terrainVertices = world.LastBuild.Triangles * 3,
                scatterTriangles = world.LastScatter.Triangles,
                scatterVertices = world.LastScatter.Triangles * 3,
            };
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(false);
                foreach (MeshFilter filter in filters)
                {
                    Mesh mesh = filter.sharedMesh;
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (mesh == null || renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    int verts = mesh.vertexCount;
                    int tris = (int)mesh.GetIndexCount(0) / 3;
                    metric.totalVertices += verts;
                    metric.totalTriangles += tris;
                }
            }

            metric.modelTriangles = Mathf.Max(0, metric.totalTriangles - metric.terrainTriangles - metric.scatterTriangles);
            metric.modelVertices = Mathf.Max(0, metric.totalVertices - metric.terrainVertices - metric.scatterVertices);
            return metric;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
