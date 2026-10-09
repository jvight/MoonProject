using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Art.Editor;
using MoonProject.Core;
using MoonProject.World.Editor;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The edit-mode preview of <see cref="WorldSystem"/> must never leak: it is DontSave, it is destroyed in
    /// OnDisable, play mode does not keep (or duplicate) it, and a play-mode round trip (with its domain reload)
    /// leaves exactly one preview behind. Locals do not survive the domain reload of EnterPlayMode, so every check
    /// re-derives what it needs from the scene.
    /// </summary>
    public sealed class WorldPreviewLifecycleTests
    {
        private const string HostName = "[WorldPreviewLifecycleTest]";
        private const string LightName = "[WorldPreviewLifecycleTestLight]";
        private const string BeaconName = "[WorldPreviewLifecycleTestBeacon]";
        private const string ChunkPrefix = "TerrainChunk_";

        [UnityTest]
        public IEnumerator Preview_SurvivesAPlayModeRoundTrip_WithoutLeaksOrDuplicates()
        {
            Assert.AreEqual(0, CountChunkObjects(), "leftover chunks before the test");
            Assert.AreEqual(0, CountGeneratedMeshes(), "leftover generated meshes before the test");

            CreateHost();
            Assert.AreEqual(ExpectedChunks(), CountChunkObjects(), "edit-mode preview not built");
            AssertPreviewIsNeverSaved();
            var world = FindHost().GetComponent<WorldSystem>();
            IWorldPreview preview = world;
            Assert.IsTrue(preview.HasGeneratedWorld, "other domains' edit-mode previews see no world");
            Assert.AreSame(world.Anchors, preview.Anchors, "other domains' edit-mode previews read other anchors");

            yield return new EnterPlayMode();
            Assert.AreEqual(0, CountChunkObjects(), "play mode kept or rebuilt the edit-mode preview");

            yield return new ExitPlayMode();
            Assert.AreEqual(ExpectedChunks(), CountChunkObjects(), "preview missing or duplicated after play mode");
            AssertPreviewIsNeverSaved();

            Object.DestroyImmediate(FindHost());
            Assert.AreEqual(0, CountChunkObjects(), "chunk objects leaked after the WorldSystem was destroyed");
            Assert.AreEqual(0, CountGeneratedMeshes(), "meshes leaked after the WorldSystem was destroyed");
        }

        [TearDown]
        public void RemoveHost()
        {
            GameObject host = FindHost();
            if (host != null)
            {
                Object.DestroyImmediate(host);
            }

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (light.gameObject.name == LightName)
                {
                    Object.DestroyImmediate(light.gameObject);
                }
            }

            foreach (PeakBeacon beacon in Object.FindObjectsByType<PeakBeacon>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (beacon.gameObject.name == BeaconName)
                {
                    Object.DestroyImmediate(beacon.gameObject);
                }
            }
        }

        private static void CreateHost()
        {
            var light = new GameObject(LightName).AddComponent<Light>();
            var host = new GameObject(HostName);
            host.SetActive(false);
            var world = host.AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            serialized.FindProperty("_settings").objectReferenceValue = LoadSettings();
            serialized.FindProperty("_groundMaterial").objectReferenceValue = Load<Material>(WorldPaths.GroundMaterial);
            serialized.FindProperty("_paletteMaterial").objectReferenceValue = Load<Material>(ArtPaths.LowPolyMaterial);
            serialized.FindProperty("_earthMaterial").objectReferenceValue = Load<Material>(WorldPaths.EarthMaterial);
            serialized.FindProperty("_earthlight").objectReferenceValue = light;
            serialized.FindProperty("_peakBeacon").objectReferenceValue = CreateBeaconHost(BeaconName);
            AssignRocks(serialized.FindProperty("_pebbleRocks"), WorldPaths.PebbleRocks);
            AssignRocks(serialized.FindProperty("_boulderRocks"), WorldPaths.BoulderRocks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
        }

        private static PeakBeacon CreateBeaconHost(string name)
        {
            var host = new GameObject(name);
            var lamp = new GameObject("Lamp").AddComponent<MeshRenderer>();
            lamp.transform.SetParent(host.transform, false);
            var halo = new GameObject("Halo").AddComponent<MeshRenderer>();
            halo.transform.SetParent(host.transform, false);
            var beacon = host.AddComponent<PeakBeacon>();
            var serialized = new SerializedObject(beacon);
            serialized.FindProperty("_lamp").objectReferenceValue = lamp;
            serialized.FindProperty("_halo").objectReferenceValue = halo;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return beacon;
        }

        private static void AssignRocks(SerializedProperty property, string[] names)
        {
            property.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue =
                    Load<Mesh>($"{ArtPaths.RockFolder}/{names[i]}.asset");
            }
        }

        private static GameObject FindHost()
        {
            foreach (WorldSystem world in Object.FindObjectsByType<WorldSystem>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (world.gameObject.name == HostName)
                {
                    return world.gameObject;
                }
            }

            return null;
        }

        private static void AssertPreviewIsNeverSaved()
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name.StartsWith(ChunkPrefix))
                {
                    Assert.AreEqual(HideFlags.DontSave, candidate.hideFlags & HideFlags.DontSave, candidate.name);
                }
            }
        }

        private static int CountChunkObjects()
        {
            int count = 0;
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name.StartsWith(ChunkPrefix) && !EditorUtility.IsPersistent(candidate))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountGeneratedMeshes()
        {
            int count = 0;
            foreach (Mesh mesh in Resources.FindObjectsOfTypeAll<Mesh>())
            {
                if (mesh.name.StartsWith(ChunkPrefix) || mesh.name.StartsWith("TerrainBackdrop") || mesh.name == "Earth"
                    || mesh.name.StartsWith("Pebbles_") || mesh.name.StartsWith("Boulders_")
                    || mesh.name.StartsWith("CanyonGate_"))
                {
                    count++;
                }
            }

            return count;
        }

        private static int ExpectedChunks()
        {
            WorldSettings settings = LoadSettings();
            return TerrainChunkPlanner.Plan(settings.Mesh, settings.CreateSurface().Canyon.Touches).Length;
        }

        private static WorldSettings LoadSettings()
        {
            return Load<WorldSettings>(WorldPaths.Settings);
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"{path} missing: run the Art and World builders first.");
            return asset;
        }
    }
}
