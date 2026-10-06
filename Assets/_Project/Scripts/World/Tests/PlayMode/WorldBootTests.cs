using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Input;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Boots <see cref="WorldSystem"/> through its only play-mode path (<see cref="IGameSystem.Initialize"/>) and
    /// checks the services it registers, that the baked chunk colliders match the analytic surface, and the boot
    /// budget.
    /// </summary>
    public sealed class WorldBootTests
    {
        // Mirrors WorldPaths / ArtPaths (editor assemblies cannot be referenced from a PlayMode test assembly).
        private const string SettingsPath = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        private const string TerrainMaterialPath = "Assets/_Project/Generated/Art/Palette/M_LowPoly.mat";
        private const string EarthMaterialPath = "Assets/_Project/Generated/World/M_Earth.mat";
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";
        private const string RockFolder = "Assets/_Project/Generated/Art/Rocks/";
        private static readonly string[] PebbleRocks = { "Rock_00", "Rock_01", "Rock_03" };
        private static readonly string[] BoulderRocks = { "Rock_02", "Rock_04", "Rock_05" };

        private const double BootBudgetMs = 1500.0;
        private const float ColliderTolerance = 0.25f;
        private const float ScatterProbeRadius = 1000f;

        private GameObject _host;
        private GameObject _lightHost;
        private InputReader _input;

        [UnityTest]
        public IEnumerator Initialize_BuildsTheWorld_WithinTheBootBudget()
        {
#if UNITY_EDITOR
            var settings = Load<WorldSettings>(SettingsPath);
            _lightHost = new GameObject("Earthlight");
            var light = _lightHost.AddComponent<Light>();
            _host = new GameObject("World");
            _host.SetActive(false);
            var world = _host.AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_terrainMaterial").objectReferenceValue = Load<Material>(TerrainMaterialPath);
            serialized.FindProperty("_earthMaterial").objectReferenceValue = Load<Material>(EarthMaterialPath);
            serialized.FindProperty("_earthlight").objectReferenceValue = light;
            AssignRocks(serialized.FindProperty("_pebbleRocks"), PebbleRocks);
            AssignRocks(serialized.FindProperty("_boulderRocks"), BoulderRocks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _host.SetActive(true);
            Assert.IsFalse(world.HasGeneratedWorld, "play mode must not build anything before Initialize");

            _input = new InputReader(Load<InputActionAsset>(ControlsPath));
            var context = new GameContext(new EventBus(), _input);
            var watch = Stopwatch.StartNew();
            world.Initialize(context);
            watch.Stop();
            yield return null;

            Assert.AreSame(world.Surface, context.Get<ITerrainQuery>());
            var layout = context.Get<IWorldLayout>();
            Assert.AreEqual(Vector3.zero, layout.BasePosition);
            Assert.AreEqual(world.Surface.PeakSummit, layout.PeakPosition);
            Assert.Less(Vector3.Angle(settings.Sky.EarthDirection, layout.EarthDirection), 0.01f);

            Physics.SyncTransforms();
            var probes = new[]
            {
                Vector2.zero, new Vector2(120f, -40f), new Vector2(-200f, 150f), new Vector2(60f, 75f),
                new Vector2(250f, 30f), new Vector2(-90f, -260f),
            };
            foreach (Vector2 probe in probes)
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(probe.x, 400f, probe.y), Vector3.down, out RaycastHit hit,
                    800f, Layers.GroundMask), $"no ground collider at {probe}");
                Assert.AreEqual(Layers.Ground, hit.collider.gameObject.layer);
                Assert.AreEqual(world.Surface.SampleHeight(probe.x, probe.y), hit.point.y, ColliderTolerance,
                    $"collider and surface disagree at {probe}");
            }

            ScatterBuildReport scatter = world.LastScatter;
            Assert.Greater(scatter.Pebbles, 0);
            int boulderColliders = Physics.OverlapSphere(Vector3.zero, ScatterProbeRadius, Layers.PropMask).Length;
            Assert.AreEqual(scatter.Boulders, boulderColliders, "every boulder needs one collider on Layers.Prop");

            string summary = $"Initialize {watch.Elapsed.TotalMilliseconds:0} ms; terrain {world.LastBuild}; " +
                $"scatter {scatter}";
            TestContext.WriteLine("World boot: " + summary);
            UnityEngine.Debug.Log("[moon] world boot: " + summary);
            Assert.Less(watch.Elapsed.TotalMilliseconds, BootBudgetMs, "world boot exceeds its budget");
#else
            Assert.Ignore("Needs editor asset access to wire the WorldSystem.");
            yield break;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.Destroy(_host);
            }

            if (_lightHost != null)
            {
                Object.Destroy(_lightHost);
            }

            _input?.Dispose();
        }

#if UNITY_EDITOR
        private static void AssignRocks(SerializedProperty property, string[] names)
        {
            property.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = Load<Mesh>(RockFolder + names[i] + ".asset");
            }
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"{path} missing: run the Art and World builders first.");
            return asset;
        }
#endif
    }
}
