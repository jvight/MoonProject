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
    /// budget (minimum across <see cref="BootRuns"/> boots to filter out machine load while catching regressions).
    /// </summary>
    public sealed class WorldBootTests
    {
        // Mirrors WorldPaths / ArtPaths (editor assemblies cannot be referenced from a PlayMode test assembly).
        private const string SettingsPath = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        private const string GroundMaterialPath = "Assets/_Project/Generated/World/M_Ground.mat";
        private const string GroundMarkMaterialPath = "Assets/_Project/Generated/World/M_GroundMarks.mat";
        private const string VentBeadMaterialPath = "Assets/_Project/Generated/World/M_VentBeads.mat";
        private const string LandmarkFolder = "Assets/_Project/Generated/Art/Landmarks/";
        private static readonly string[] DishFrames = { "DishFrame_0", "DishFrame_1", "DishFrame_2" };
        private const string PropFolder = "Assets/_Project/Generated/Art/Props/";
        private static readonly string[] HullFragments =
        {
            "HullFragment_0", "HullFragment_1", "HullFragment_2", "HullFragment_3",
        };
        private const string PaletteMaterialPath = "Assets/_Project/Generated/Art/R1/Materials/R1_Basalt.mat";
        private const string EarthMaterialPath = "Assets/_Project/Generated/World/M_Earth.mat";
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";
        private const string RockFolder = "Assets/_Project/Art/R1/Models/Rocks/";
        private static readonly string[] PebbleRocks = { "Rock_00", "Rock_01", "Rock_02", "Rock_03", "Rock_04" };
        private static readonly string[] BoulderRocks = { "Rock_05", "Rock_06", "Rock_07", "Rock_08", "Rock_09" };

        private const int BootRuns = 3;
        private const double BootBudgetMs = 1500.0;
        private const float ColliderTolerance = 0.25f;
        private const float ScatterProbeRadius = 1000f;

        // Gate probes start this far (m) in front of a gate slab, across these shares of the corridor's half width;
        // a sheer face has a near-horizontal normal and sits where the slab's front should be.
        private const float GateProbeRun = 6f;
        private const float GateProbeTolerance = 0.5f;
        private const float SheerNormalY = 0.2f;
        private static readonly float[] GateProbeShares = { -0.6f, 0f, 0.6f };
        private static readonly Vector2[] GroundProbes =
        {
            Vector2.zero, new Vector2(120f, -40f), new Vector2(-200f, 150f), new Vector2(60f, 75f),
            new Vector2(250f, 30f), new Vector2(-90f, -260f),
        };

        private GameObject _host;
        private GameObject _lightHost;
        private GameObject _fillHost;
        private PeakBeacon _beacon;
        private InputReader _input;

        [UnityTest]
        public IEnumerator Initialize_BuildsTheWorld_WithinTheBootBudget()
        {
#if UNITY_EDITOR
            var settings = Load<WorldSettings>(SettingsPath);
            _lightHost = new GameObject("Earthlight");
            var light = _lightHost.AddComponent<Light>();
            _fillHost = new GameObject("Fill Light");
            var fill = _fillHost.AddComponent<Light>();
            _host = new GameObject("World");
            _host.SetActive(false);
            var world = _host.AddComponent<WorldSystem>();
            var serialized = new SerializedObject(world);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_groundMaterial").objectReferenceValue = Load<Material>(GroundMaterialPath);
            serialized.FindProperty("_paletteMaterial").objectReferenceValue = Load<Material>(PaletteMaterialPath);
            serialized.FindProperty("_earthMaterial").objectReferenceValue = Load<Material>(EarthMaterialPath);
            serialized.FindProperty("_groundMarkMaterial").objectReferenceValue =
                Load<Material>(GroundMarkMaterialPath);
            serialized.FindProperty("_ventBeadMaterial").objectReferenceValue = Load<Material>(VentBeadMaterialPath);
            AssignLandmark(serialized, "_routePost", "RoutePost");
            AssignLandmark(serialized, "_bridgeNear", "ChasmBridge_Near");
            AssignLandmark(serialized, "_bridgeFar", "ChasmBridge_Far");
            AssignLandmark(serialized, "_bridgeCable", "ChasmBridge_Cable");
            SerializedProperty frames = serialized.FindProperty("_landmarkPrefabs._dishFrames");
            frames.arraySize = DishFrames.Length;
            for (int i = 0; i < DishFrames.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue =
                    Load<GameObject>(LandmarkFolder + DishFrames[i] + ".prefab");
            }

            SerializedProperty fragments = serialized.FindProperty("_landmarkPrefabs._hullFragments");
            fragments.arraySize = HullFragments.Length;
            for (int i = 0; i < HullFragments.Length; i++)
            {
                fragments.GetArrayElementAtIndex(i).objectReferenceValue =
                    Load<GameObject>(PropFolder + HullFragments[i] + ".prefab");
            }
            serialized.FindProperty("_earthlight").objectReferenceValue = light;
            serialized.FindProperty("_fillLight").objectReferenceValue = fill;
            _beacon = CreateBeaconHost("PeakBeacon");
            serialized.FindProperty("_peakBeacon").objectReferenceValue = _beacon;
            AssignRocks(serialized.FindProperty("_pebbleRocks"), PebbleRocks);
            AssignRocks(serialized.FindProperty("_boulderRocks"), BoulderRocks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _host.SetActive(true);
            Assert.IsFalse(world.HasGeneratedWorld, "play mode must not build anything before Initialize");

            _input = new InputReader(Load<InputActionAsset>(ControlsPath));
            var runsMs = new double[BootRuns];
            double minMs = double.MaxValue;
            TerrainBuildReport bestBuild = default;
            ScatterBuildReport bestScatter = default;

            for (int run = 0; run < BootRuns; run++)
            {
                if (run > 0)
                {
                    _host.SetActive(false);
                    _beacon.transform.position = Vector3.zero;
                    yield return null;
                    Assert.IsFalse(world.HasGeneratedWorld,
                        "deactivating the world must tear down what Initialize created");
                    Assert.AreEqual(0, _host.transform.childCount, "teardown must destroy the generated root");
                    Physics.SyncTransforms();
                    Assert.IsFalse(Physics.Raycast(new Vector3(0f, 400f, 0f), Vector3.down, 800f, Layers.GroundMask),
                        "teardown must remove ground colliders between boots");
                    Assert.AreEqual(0, Physics.OverlapSphere(Vector3.zero, ScatterProbeRadius, Layers.PropMask).Length,
                        "teardown must remove boulder colliders between boots");
                    _host.SetActive(true);
                    Assert.IsFalse(world.HasGeneratedWorld, "play mode must not build anything before Initialize");
                }

                var context = new GameContext(new EventBus(), _input);
                var watch = Stopwatch.StartNew();
                world.Initialize(context);
                watch.Stop();
                double elapsedMs = watch.Elapsed.TotalMilliseconds;
                runsMs[run] = elapsedMs;
                if (elapsedMs < minMs)
                {
                    minMs = elapsedMs;
                    bestBuild = world.LastBuild;
                    bestScatter = world.LastScatter;
                }

                yield return null;
                AssertBootedWorld(world, settings, context);
            }

            string runsLine = $"[moon] world boot runs: {runsMs[0]:0} / {runsMs[1]:0} / {runsMs[2]:0} ms, min {minMs:0}";
            string summary = $"Initialize {minMs:0} ms; terrain {bestBuild}; scatter {bestScatter}";
            TestContext.WriteLine(runsLine);
            TestContext.WriteLine("World boot: " + summary);
            UnityEngine.Debug.Log(runsLine);
            UnityEngine.Debug.Log("[moon] world boot: " + summary);
            Assert.Less(minMs, BootBudgetMs, "world boot exceeds its budget");
#else
            Assert.Ignore("Needs editor asset access to wire the WorldSystem.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private void AssertBootedWorld(WorldSystem world, WorldSettings settings, GameContext context)
        {
            Assert.IsTrue(world.HasGeneratedWorld, "Initialize must generate the world");
            Assert.AreEqual(1, _host.transform.childCount, "only one generated root may exist under the world");
            Assert.AreSame(world.Surface, context.Get<ITerrainQuery>());
            var layout = context.Get<IWorldLayout>();
            Assert.AreEqual(Vector3.zero, layout.BasePosition);
            Assert.AreEqual(world.Surface.PeakSummit, layout.PeakPosition);
            Assert.Less(Vector3.Angle(settings.Sky.EarthDirection, layout.EarthDirection), 0.01f);

            Physics.SyncTransforms();
            foreach (Vector2 probe in GroundProbes)
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(probe.x, 400f, probe.y), Vector3.down, out RaycastHit hit,
                    800f, Layers.GroundMask), $"no ground collider at {probe}");
                Assert.AreEqual(Layers.Ground, hit.collider.gameObject.layer);
                Assert.AreEqual(world.Surface.SampleHeight(probe.x, probe.y), hit.point.y, ColliderTolerance,
                    $"collider and surface disagree at {probe}");
            }

            Assert.AreSame(world.Anchors, context.Get<IWorldAnchors>());
            Canyon canyon = world.Surface.Canyon;
            float exitHalfWidth = settings.Surface.Canyon.ExitHalfWidth;
            AssertSheerFace(world.Surface, canyon.MainPath, canyon.FarFaceArc, settings.Surface.Canyon.ChasmHalfWidth,
                "the chasm's far face");
            AssertSheerFace(world.Surface, canyon.ExitPath, canyon.ExitStepArc, exitHalfWidth, "the exit step");

            ScatterBuildReport scatter = world.LastScatter;
            Assert.Greater(scatter.Pebbles, 0);
            Assert.Greater(scatter.SolidCobbles, 0, "medium cobbles >= CobbleColliderMinSize need SphereColliders");
            Assert.AreEqual(layout.PeakPosition, _beacon.transform.position, "the beacon stands on the summit");

            int propColliders = Physics.OverlapSphere(Vector3.zero, ScatterProbeRadius, Layers.PropMask).Length;
            Assert.Greater(propColliders, scatter.Boulders + scatter.SolidCobbles,
                "every boulder, solid medium cobble and placed landmark needs colliders on Layers.Prop");
        }
#endif

        /// <summary>
        /// A rover-height probe run straight at a gate face (across its width) must meet a vertical collider on the
        /// ground layer where the face is, not a ramp of terrain triangles in front of it.
        /// </summary>
        private static void AssertSheerFace(MoonSurface surface, CanyonPath path, float faceArc, float halfWidth,
            string what)
        {
            float before = faceArc - Canyon.SlabHalfDepth - GateProbeRun;
            float after = faceArc + Canyon.SlabHalfDepth + GateProbeRun;
            Vector2 along = path.TangentAt(faceArc);
            foreach (float share in GateProbeShares)
            {
                Vector2 start = path.PointAt(before) + path.RightAt(before) * (share * halfWidth);
                Vector2 end = path.PointAt(after) + path.RightAt(after) * (share * halfWidth);
                float low = surface.SampleHeight(start.x, start.y);
                float high = surface.SampleHeight(end.x, end.y);
                var origin = new Vector3(start.x, (low + high) * 0.5f, start.y);
                string where = $"{what} at {share * halfWidth:F1} m across";
                Assert.IsTrue(Physics.Raycast(origin, new Vector3(along.x, 0f, along.y), out RaycastHit hit,
                    2f * GateProbeRun, Layers.GroundMask), $"{where}: nothing to hit");
                Assert.Less(Mathf.Abs(hit.normal.y), SheerNormalY, $"{where}: the face is not sheer");
                Assert.AreEqual(GateProbeRun, hit.distance, GateProbeTolerance, $"{where}: the face is not in place");
            }
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

            if (_fillHost != null)
            {
                Object.Destroy(_fillHost);
            }

            if (_beacon != null)
            {
                Object.Destroy(_beacon.gameObject);
            }

            _input?.Dispose();
        }

#if UNITY_EDITOR
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
                property.GetArrayElementAtIndex(i).objectReferenceValue = Load<Mesh>(RockFolder + names[i] + ".fbx");
            }
        }

        private static void AssignLandmark(SerializedObject world, string field, string prefab)
        {
            world.FindProperty("_landmarkPrefabs." + field).objectReferenceValue =
                Load<GameObject>(LandmarkFolder + prefab + ".prefab");
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
