using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Core;
using MoonProject.Editor.Builders;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The ten robot-scale base structures of 0.5.7 in deterministic catalog order: charging pad, solar array, battery
    /// bank, power pylon, smelter, fabricator, storage rack, upgrade bay, antenna mast, and vehicle pad. The three
    /// slice-6 structures (<c>charging_pad</c>, <c>solar_array</c>, <c>power_pylon</c>) wrap Art's R1 prefabs with
    /// their real mesh bounds, <c>Socket_Power_&lt;n&gt;</c> ports, <see cref="Layers.Prop"/> collider, and
    /// <see cref="StructureView"/> (<c>ArtPending = false</c>); the remaining seven stay <c>ArtPending = true</c>.
    /// </summary>
    internal static class BaseStructureRecipes
    {
        private const string StagePrefix = "Stage_";
        private const string SocketPowerPrefix = "Socket_Power_";
        private const string LampAmberMaterialName = "R1_LampAmber";

        public static readonly Entry[] All =
        {
            new Entry(
                StructureIds.ChargingPad,
                Vector3.zero,
                maxGroundSlope: 12f,
                clearance: 0.25f,
                produceWatts: 0f,
                consumeWatts: 120f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(4, 2, 0),
                buildSeconds: 3f,
                ports: Array.Empty<StructurePort>(),
                artPending: false,
                r1ModelName: "ChargingPad"),
            new Entry(
                StructureIds.SolarArray,
                Vector3.zero,
                maxGroundSlope: 18f,
                clearance: 0.25f,
                produceWatts: 180f,
                consumeWatts: 0f,
                storeWattHours: 0f,
                connectionRadius: 14f,
                buildCost: new Recipe(3, 2, 1),
                buildSeconds: 3f,
                ports: Array.Empty<StructurePort>(),
                artPending: false,
                r1ModelName: "SolarArray"),
            new Entry(
                StructureIds.BatteryBank,
                new Vector3(1.4f, 1.2f, 1.4f),
                maxGroundSlope: 15f,
                clearance: 0.25f,
                produceWatts: 0f,
                consumeWatts: 0f,
                storeWattHours: 360f,
                connectionRadius: 0f,
                buildCost: new Recipe(3, 3, 0),
                buildSeconds: 2.5f,
                ports: CardinalPorts(new Vector3(1.4f, 1.2f, 1.4f), 0.25f),
                artPending: true),
            new Entry(
                StructureIds.PowerPylon,
                Vector3.zero,
                maxGroundSlope: 22f,
                clearance: 0.2f,
                produceWatts: 0f,
                consumeWatts: 15f,
                storeWattHours: 0f,
                connectionRadius: 16f,
                buildCost: new Recipe(2, 1, 0),
                buildSeconds: 1.5f,
                ports: Array.Empty<StructurePort>(),
                artPending: false,
                r1ModelName: "PowerPylon"),
            new Entry(
                StructureIds.Smelter,
                new Vector3(2.0f, 1.8f, 2.0f),
                maxGroundSlope: 12f,
                clearance: 0.3f,
                produceWatts: 0f,
                consumeWatts: 150f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(6, 2, 0),
                buildSeconds: 4f,
                ports: CardinalPorts(new Vector3(2.0f, 1.8f, 2.0f), 0.3f),
                artPending: true),
            new Entry(
                StructureIds.Fabricator,
                new Vector3(2.2f, 1.6f, 1.8f),
                maxGroundSlope: 12f,
                clearance: 0.3f,
                produceWatts: 0f,
                consumeWatts: 140f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(5, 3, 1),
                buildSeconds: 4f,
                ports: CardinalPorts(new Vector3(2.2f, 1.6f, 1.8f), 0.3f),
                artPending: true),
            new Entry(
                StructureIds.StorageRack,
                new Vector3(1.8f, 1.5f, 1.0f),
                maxGroundSlope: 16f,
                clearance: 0.2f,
                produceWatts: 0f,
                consumeWatts: 10f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(4, 0, 0),
                buildSeconds: 2f,
                ports: CardinalPorts(new Vector3(1.8f, 1.5f, 1.0f), 0.2f),
                artPending: true),
            new Entry(
                StructureIds.UpgradeBay,
                new Vector3(2.6f, 2.0f, 2.6f),
                maxGroundSlope: 10f,
                clearance: 0.35f,
                produceWatts: 0f,
                consumeWatts: 160f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(6, 4, 2),
                buildSeconds: 5f,
                ports: CardinalPorts(new Vector3(2.6f, 2.0f, 2.6f), 0.35f),
                artPending: true),
            new Entry(
                StructureIds.AntennaMast,
                new Vector3(1.6f, 3.5f, 1.6f),
                maxGroundSlope: 15f,
                clearance: 0.3f,
                produceWatts: 0f,
                consumeWatts: 90f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(4, 3, 2),
                buildSeconds: 4.5f,
                ports: CardinalPorts(new Vector3(1.6f, 3.5f, 1.6f), 0.3f),
                artPending: true),
            new Entry(
                StructureIds.VehiclePad,
                new Vector3(3.2f, 0.5f, 3.2f),
                maxGroundSlope: 10f,
                clearance: 0.4f,
                produceWatts: 0f,
                consumeWatts: 100f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(8, 3, 1),
                buildSeconds: 5f,
                ports: CardinalPorts(new Vector3(3.2f, 0.5f, 3.2f), 0.4f),
                artPending: true),
        };

        /// <summary>
        /// Builds and saves the runtime structure prefabs under <c>Assets/_Project/Generated/Gameplay/Structures/</c>
        /// for every entry that has an R1 model prefab.
        /// </summary>
        public static void BuildRuntimePrefabs()
        {
            for (int i = 0; i < All.Length; i++)
            {
                Entry entry = All[i];
                if (!entry.ArtPending && !string.IsNullOrEmpty(entry.R1ModelName))
                {
                    BuildRuntimePrefab(entry);
                }
            }
        }

        /// <summary>
        /// Creates the in-memory <see cref="StructureDefinition"/> instances for every entry in <see cref="All"/>
        /// (used by <see cref="GameplayContentBuilder"/> and EditMode tests).
        /// </summary>
        public static StructureDefinition[] CreateDefinitions(bool requireRuntimePrefabs = false)
        {
            var definitions = new StructureDefinition[All.Length];
            for (int i = 0; i < All.Length; i++)
            {
                Entry entry = All[i];
                Vector3 footprint = entry.Footprint;
                StructurePort[] ports = entry.Ports;
                GameObject prefab = null;

                if (!entry.ArtPending && !string.IsNullOrEmpty(entry.R1ModelName))
                {
                    string runtimePath = GameplayAssetPaths.RuntimeStructurePrefab(entry.Id);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(runtimePath);
                    if (prefab == null)
                    {
                        if (requireRuntimePrefabs)
                        {
                            throw new InvalidOperationException(
                                $"BaseStructureRecipes: missing runtime structure prefab at '{runtimePath}' (run {GameplayContentBuilder.BuilderPath}).");
                        }

                        string r1Path = GameplayAssetPaths.R1StructurePrefab(entry.R1ModelName);
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(r1Path);
                        if (prefab == null)
                        {
                            throw new InvalidOperationException(
                                $"BaseStructureRecipes: missing R1 structure prefab at '{r1Path}'.");
                        }
                    }

                    ExtractBoundsAndPorts(prefab.transform, entry.Clearance, ref footprint, out ports);
                }

                var definition = ScriptableObject.CreateInstance<StructureDefinition>();
                definition.name = entry.Id;
                definition.Populate(
                    entry.Id,
                    StructureIds.NameKey(entry.Id),
                    footprint,
                    entry.MaxGroundSlope,
                    entry.Clearance,
                    entry.ProduceWatts,
                    entry.ConsumeWatts,
                    entry.StoreWattHours,
                    entry.ConnectionRadius,
                    entry.BuildCost,
                    entry.BuildSeconds,
                    ports,
                    prefab,
                    entry.ArtPending);
                definitions[i] = definition;
            }

            return definitions;
        }

        /// <summary>Creates an in-memory <see cref="StructureCatalog"/> populated from <see cref="CreateDefinitions"/>.</summary>
        public static StructureCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<StructureCatalog>();
            catalog.Populate(CreateDefinitions());
            return catalog;
        }

        private static GameObject BuildRuntimePrefab(Entry entry)
        {
            string sourcePath = GameplayAssetPaths.R1StructurePrefab(entry.R1ModelName);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: missing R1 structure prefab at '{sourcePath}'.");
            }

            GameObject instance = Object.Instantiate(sourcePrefab);
            instance.name = entry.Id;
            NormalizeBlenderEmptyTransforms(instance.transform);
            SetLayerRecursively(instance.transform, Layers.Prop);

            if (!MeshBounds.TryLocal(instance.transform, instance.transform, out Bounds bounds))
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: R1 structure '{entry.R1ModelName}' has no meshes.");
            }

            var collider = instance.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            var stages = new List<Transform>(4);
            for (int stageIndex = 1; stageIndex <= 8; stageIndex++)
            {
                Transform stage = instance.transform.Find(StagePrefix + stageIndex);
                if (stage == null)
                {
                    break;
                }

                stages.Add(stage);
            }

            if (stages.Count == 0)
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: R1 structure '{entry.R1ModelName}' has no Stage_1..N children.");
            }

            var lampRenderers = new List<Renderer>();
            Renderer[] allRenderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < allRenderers.Length; r++)
            {
                Renderer renderer = allRenderers[r];
                Material[] mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null && mats[m].name == LampAmberMaterialName)
                    {
                        lampRenderers.Add(renderer);
                        break;
                    }
                }
            }

            var view = instance.AddComponent<StructureView>();
            view.Populate(stages.ToArray(), lampRenderers.ToArray(), new Collider[] { collider });

            string runtimePath = GameplayAssetPaths.RuntimeStructurePrefab(entry.Id);
            return GeneratedAssets.SavePrefab(instance, runtimePath);
        }

        private static void NormalizeBlenderEmptyTransforms(Transform root)
        {
            var qFix = new Quaternion(0f, 0.70710678f, 0.70710678f, 0f);
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child == root)
                {
                    continue;
                }

                if (child.name.StartsWith(SocketPowerPrefix, StringComparison.Ordinal) &&
                    Mathf.Abs(child.localRotation.w) < 0.99f)
                {
                    Vector3 p = child.localPosition;
                    child.localPosition = new Vector3(-p.x, p.z, p.y);
                    child.localRotation = Quaternion.identity;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == "Stage_4" &&
                    !child.TryGetComponent<MeshFilter>(out _) &&
                    child.childCount > 0 &&
                    Mathf.Abs(child.GetChild(0).localRotation.w) < 0.99f)
                {
                    Vector3 sp = child.localPosition;
                    child.localPosition = new Vector3(-sp.x, sp.z, sp.y);
                    child.localRotation = Quaternion.identity;
                    for (int j = 0; j < child.childCount; j++)
                    {
                        Transform c = child.GetChild(j);
                        Vector3 cp = c.localPosition;
                        c.localPosition = new Vector3(-cp.x, cp.z, cp.y);
                        c.localRotation = qFix * c.localRotation;

                        if (c.name == "Azimuth")
                        {
                            Transform elev = c.Find("Elevation");
                            if (elev != null)
                            {
                                elev.localPosition = new Vector3(0f, 0.31f, 0f);
                                elev.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                                Transform wingL = elev.Find("Wing_L");
                                if (wingL != null)
                                {
                                    wingL.localPosition = new Vector3(-0.075f, 0f, -0.18f);
                                    wingL.localRotation = Quaternion.Euler(0f, 0f, 5f);
                                }

                                Transform wingR = elev.Find("Wing_R");
                                if (wingR != null)
                                {
                                    wingR.localPosition = new Vector3(0.075f, 0f, -0.18f);
                                    wingR.localRotation = Quaternion.Euler(0f, 0f, -5f);
                                }
                            }
                        }
                    }
                }
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

        internal static void ExtractDimensionsAndPorts(
            GameObject root,
            out Vector3 dimensions,
            out Vector3 localCenter,
            out StructurePort[] ports)
        {
            if (MeshBounds.TryLocal(root.transform, root.transform, out Bounds bounds))
            {
                dimensions = new Vector3(
                    Mathf.Max(0.1f, bounds.size.x),
                    Mathf.Max(0.1f, bounds.size.y),
                    Mathf.Max(0.1f, bounds.size.z));
                localCenter = bounds.center;
            }
            else
            {
                dimensions = Vector3.one;
                localCenter = Vector3.zero;
            }

            var portList = new List<StructurePort>();
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform child = descendants[i];
                if (child == root.transform || !child.name.StartsWith(SocketPowerPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 offset = root.transform.InverseTransformPoint(child.position);
                var horizontal = new Vector3(offset.x, 0f, offset.z);
                Vector3 direction = horizontal.sqrMagnitude > 1e-6f ? horizontal.normalized : Vector3.forward;
                portList.Add(new StructurePort(offset, direction));
            }

            ports = portList.ToArray();
        }

        private static void ExtractBoundsAndPorts(
            Transform root,
            float clearance,
            ref Vector3 footprint,
            out StructurePort[] ports)
        {
            ExtractDimensionsAndPorts(root.gameObject, out Vector3 dimensions, out _, out StructurePort[] socketPorts);
            if (MeshBounds.TryLocal(root, root, out _))
            {
                footprint = dimensions;
            }

            ports = socketPorts.Length > 0 ? socketPorts : CardinalPorts(footprint, clearance);
        }

        private static StructurePort[] CardinalPorts(Vector3 footprint, float clearance)
        {
            float halfX = footprint.x * 0.5f + clearance;
            float halfZ = footprint.z * 0.5f + clearance;
            return new[]
            {
                new StructurePort(new Vector3(0f, 0f, halfZ), Vector3.forward),
                new StructurePort(new Vector3(0f, 0f, -halfZ), Vector3.back),
                new StructurePort(new Vector3(halfX, 0f, 0f), Vector3.right),
                new StructurePort(new Vector3(-halfX, 0f, 0f), Vector3.left),
            };
        }

        internal readonly struct Entry
        {
            public Entry(
                string id,
                Vector3 footprint,
                float maxGroundSlope,
                float clearance,
                float produceWatts,
                float consumeWatts,
                float storeWattHours,
                float connectionRadius,
                Recipe buildCost,
                float buildSeconds,
                StructurePort[] ports,
                bool artPending,
                string r1ModelName = null)
            {
                Id = id;
                Footprint = footprint;
                MaxGroundSlope = maxGroundSlope;
                Clearance = clearance;
                ProduceWatts = produceWatts;
                ConsumeWatts = consumeWatts;
                StoreWattHours = storeWattHours;
                ConnectionRadius = connectionRadius;
                BuildCost = buildCost;
                BuildSeconds = buildSeconds;
                Ports = ports;
                ArtPending = artPending;
                R1ModelName = r1ModelName;
            }

            public string Id { get; }

            public Vector3 Footprint { get; }

            public float MaxGroundSlope { get; }

            public float Clearance { get; }

            public float ProduceWatts { get; }

            public float ConsumeWatts { get; }

            public float StoreWattHours { get; }

            public float ConnectionRadius { get; }

            public Recipe BuildCost { get; }

            public float BuildSeconds { get; }

            public StructurePort[] Ports { get; }

            public bool ArtPending { get; }

            public string R1ModelName { get; }
        }
    }
}
