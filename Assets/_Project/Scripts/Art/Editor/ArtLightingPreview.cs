using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Night-time look check for the palette material and the rover "07": a moonlit dust floor with rocks, the
    /// rover posed at rest (half-lidded eye, head lowered, wing ajar), its headlamp as a warm spot light and a warm
    /// point light standing in for a base lamp, rendered through <see cref="CaptureAutomation.CapturePoses"/> with
    /// bloom. Complements the neutral turntables: this is where emission, spot and point light response are judged.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Art.Editor.ArtLightingPreview.Capture
    /// </code>
    /// </summary>
    public static class ArtLightingPreview
    {
        private const float RestingEyelidDegrees = 48f;
        private const float RestingHeadPitchDegrees = 9f;
        private const float RestingNeckYawDegrees = -12f;
        private const float RestingWingDegrees = 10f;

        public static void Capture()
        {
            BatchRunner.Run(nameof(ArtLightingPreview), args =>
            {
                string roverPath = $"{ArtPaths.RoverFolder}/{RoverModelBuilder.ModelName}.prefab";
                var rover = AssetDatabase.LoadAssetAtPath<GameObject>(roverPath);
                if (rover == null)
                {
                    Debug.LogError("ArtLightingPreview: build Art/Rover first.");
                    return false;
                }

                Material material = PaletteAssetBuilder.LoadMaterial();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temporary = new TemporaryObjects();
                try
                {
                    BuildScene(rover, material, temporary);
                    string output = Path.Combine(args.OutputDirectory, "art_preview");
                    return CaptureAutomation.CapturePoses(Poses(), output, "night") > 0;
                }
                finally
                {
                    temporary.Dispose();
                }
            });
        }

        private static void BuildScene(GameObject roverPrefab, Material material, TemporaryObjects temporary)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = (Color)Palette.Get(PaletteSwatch.SkyHorizon) * 0.9f;
            RenderSettings.ambientEquatorColor = (Color)Palette.Get(PaletteSwatch.DustShadow) * 0.55f;
            RenderSettings.ambientGroundColor = Palette.Get(PaletteSwatch.SkyTop);

            var ground = new LowPolyMeshBuilder(64);
            ground.Prism(Place.At(0f, -0.1f, 0f), 40f, 0.2f, 24, PaletteSwatch.DustMid);
            for (int i = 0; i < 9; i++)
            {
                float angle = i * 2.4f;
                float distance = 3.5f + i * 0.9f;
                var at = new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                RockGenerator.Build(ground, 100 + i, 0.4f + (i % 4) * 0.45f, (RockStyle)(i % 5), Place.At(at));
            }

            temporary.Add(MeshObject("Ground", ground.ToMesh("PreviewGround"), material, temporary));

            Light moon = NewLight("Moonlight", LightType.Directional, new Color(0.72f, 0.74f, 1f), 0.55f);
            moon.transform.rotation = Quaternion.Euler(32f, 140f, 0f);
            moon.shadows = LightShadows.Soft;
            RenderSettings.sun = moon;
            temporary.Add(moon.gameObject);

            var rover = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
            temporary.Add(rover);
            PoseAtRest(rover.transform);

            Transform socket = Descendant(rover.transform, "HeadlampSocket");
            Light headlamp = NewLight("Headlamp", LightType.Spot, Palette.Get(PaletteSwatch.WarmLamp), 9f);
            headlamp.transform.SetPositionAndRotation(socket.position, socket.rotation);
            headlamp.range = 16f;
            headlamp.spotAngle = 80f;
            headlamp.innerSpotAngle = 35f;
            headlamp.shadows = LightShadows.Soft;
            temporary.Add(headlamp.gameObject);

            Light baseLamp = NewLight("BaseLamp", LightType.Point, Palette.Get(PaletteSwatch.WarmLamp), 4f);
            baseLamp.transform.position = new Vector3(-2.4f, 1.7f, -1.2f);
            baseLamp.range = 7f;
            baseLamp.shadows = LightShadows.Soft;
            temporary.Add(baseLamp.gameObject);

            temporary.Add(NewVolume(temporary));
        }

        private static void PoseAtRest(Transform rover)
        {
            Descendant(rover, "Neck").localRotation = Quaternion.Euler(0f, RestingNeckYawDegrees, 0f);
            Descendant(rover, "Head").localRotation = Quaternion.Euler(RestingHeadPitchDegrees, 0f, 0f);
            Descendant(rover, "Eyelid").localRotation = Quaternion.Euler(RestingEyelidDegrees, 0f, 0f);
            Descendant(rover, "SolarWing").localRotation = Quaternion.Euler(RestingWingDegrees, 0f, 0f);
        }

        private static CameraPoseSet Poses()
        {
            return new CameraPoseSet
            {
                width = 1280,
                height = 720,
                postProcessing = true,
                poses = new[]
                {
                    Pose("hero", new[] { 2.6f, 1.25f, 3.4f }, new[] { 0f, 0.85f, 0.2f }, 40f),
                    Pose("face", new[] { 0.55f, 1.3f, 1.75f }, new[] { 0f, 1.25f, 0.6f }, 35f),
                    Pose("chase", new[] { 1.2f, 3.2f, -6.2f }, new[] { 0f, 0.8f, 1.5f }, 50f),
                    Pose("side", new[] { -5f, 1.1f, 0.4f }, new[] { 0f, 0.7f, 0.2f }, 35f),
                    Pose("far30m", new[] { 6f, 9f, -28f }, new[] { 0f, 0.7f, 0f }, 50f),
                },
            };
        }

        private static CameraPose Pose(string name, float[] position, float[] lookAt, float fov)
        {
            return new CameraPose { name = name, position = position, lookAt = lookAt, fov = fov };
        }

        private static GameObject MeshObject(string name, Mesh mesh, Material material, TemporaryObjects temporary)
        {
            temporary.Add(mesh);
            var gameObject = new GameObject(name);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        private static Light NewLight(string name, LightType type, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            return light;
        }

        private static GameObject NewVolume(TemporaryObjects temporary)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            var volumeObject = new GameObject("PreviewVolume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volumeObject;
        }

        private static Transform Descendant(Transform root, string name)
        {
            Transform hit = Search(root, name);
            if (hit == null)
            {
                throw new System.InvalidOperationException($"RoverModel has no node named {name}.");
            }

            return hit;
        }

        private static Transform Search(Transform node, string name)
        {
            if (node.name == name)
            {
                return node;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform hit = Search(node.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Everything the preview creates, destroyed afterwards so no state outlives the capture.</summary>
        private sealed class TemporaryObjects : System.IDisposable
        {
            private readonly System.Collections.Generic.List<Object> _objects =
                new System.Collections.Generic.List<Object>();

            public void Add(Object item)
            {
                _objects.Add(item);
            }

            public void Dispose()
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }

                _objects.Clear();
            }
        }
    }
}
