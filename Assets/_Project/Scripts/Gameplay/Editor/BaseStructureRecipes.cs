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
    /// The robot-scale base structures of the opening (opening-v2 §5) in deterministic catalog order: charging pad,
    /// solar array, battery bank, power pylon and antenna mast. All wrap
    /// Art's R1 prefabs with their upright mesh bounds, <c>Socket_Power_&lt;n&gt;</c> ports, <see cref="Layers.Prop"/>
    /// collider, and <see cref="StructureView"/> (<c>ArtPending = false</c>).
    /// </summary>
    internal static class BaseStructureRecipes
    {
        // The pad's and the bank's own grid reach, metres: a bank set down beside its pad (the base's structures stand
        // a few metres apart) always connects without a pylon.
        private const float PadAndBankReach = 10f;

        private const string StagePrefix = "Stage_";
        private const string SocketPowerPrefix = "Socket_Power_";
        private const string LampAmberMaterialName = "R1_LampAmber";
        private const float StatusLightIntensity = 1.6f;
        private const float StatusLightRange = 5.0f;
        private static readonly Color StatusLightColor = new Color(1.0f, 0.74f, 0.36f);

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
                connectionRadius: PadAndBankReach,
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
                Vector3.zero,
                maxGroundSlope: 15f,
                clearance: 0.25f,
                produceWatts: 0f,
                consumeWatts: 0f,
                storeWattHours: 360f,
                connectionRadius: PadAndBankReach,
                buildCost: new Recipe(3, 3, 0),
                buildSeconds: 2.5f,
                ports: Array.Empty<StructurePort>(),
                artPending: false,
                r1ModelName: "BatteryBank"),
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
                StructureIds.AntennaMast,
                Vector3.zero,
                maxGroundSlope: 15f,
                clearance: 0.3f,
                produceWatts: 0f,
                consumeWatts: 90f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(4, 3, 2),
                buildSeconds: 4.5f,
                ports: Array.Empty<StructurePort>(),
                artPending: false,
                r1ModelName: "AntennaMast"),
        };

        /// <summary>
        /// Builds and saves the runtime structure prefabs under <c>Assets/_Project/Generated/Gameplay/Structures/</c>
        /// for every entry that has an R1 model prefab.
        /// </summary>
        public static void BuildRuntimePrefabs()
        {
            using (var scratch = new BuilderScratchScene())
            {
                for (int i = 0; i < All.Length; i++)
                {
                    Entry entry = All[i];
                    if (!entry.ArtPending && !string.IsNullOrEmpty(entry.R1ModelName))
                    {
                        BuildRuntimePrefab(scratch, entry);
                    }
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
                    else if (requireRuntimePrefabs &&
                             (!prefab.TryGetComponent(out StructureView view) || view.StageCount == 0))
                    {
                        throw new InvalidOperationException(
                            $"BaseStructureRecipes: runtime structure prefab at '{runtimePath}' is missing a wired {nameof(StructureView)} (run {GameplayContentBuilder.BuilderPath}).");
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

        private static GameObject BuildRuntimePrefab(BuilderScratchScene scratch, Entry entry)
        {
            string sourcePath = GameplayAssetPaths.R1StructurePrefab(entry.R1ModelName);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: missing R1 structure prefab at '{sourcePath}'.");
            }

            GameObject root = AssembleRuntimeHierarchy(scratch, entry.Id, entry.R1ModelName, sourcePrefab);
            string runtimePath = GameplayAssetPaths.RuntimeStructurePrefab(entry.Id);
            return GeneratedAssets.SavePrefab(root, runtimePath);
        }

        /// <summary>
        /// Assembles the runtime structure GameObject hierarchy in <paramref name="scratch"/> and wires
        /// <see cref="StructureView"/>'s <c>_stageRoots</c>, <c>_lampRenderers</c>, <c>_colliders</c>, and
        /// <c>_statusLight</c> from <paramref name="sourcePrefab"/>'s <c>Stage_1..N</c> children.
        /// </summary>
        /// <summary>
        /// One mesh collider per mesh, exact rather than convex (a structure never moves, so its colliders are static):
        /// a structure is solid where its parts stand and open where they don't, so 07 drives up onto the charging
        /// pad's low deck while its totem stays solid. A convex hull, or one box round the whole model, made the pad a
        /// block as tall as the totem.
        /// </summary>
        private static Collider[] AddPartColliders(Transform model)
        {
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
            var colliders = new Collider[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                var collider = filters[i].gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filters[i].sharedMesh;
                colliders[i] = collider;
            }

            return colliders;
        }

        internal static GameObject AssembleRuntimeHierarchy(
            BuilderScratchScene scratch,
            string structureId,
            string r1ModelName,
            GameObject sourcePrefab)
        {
            GameObject root = scratch.Create(structureId);
            GameObject model = scratch.Instantiate(sourcePrefab, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = sourcePrefab.transform.localRotation;
            model.transform.localScale = sourcePrefab.transform.localScale;
            SetLayerRecursively(root.transform, Layers.Prop);

            if (!MeshBounds.TryLocal(root.transform, root.transform, out Bounds bounds))
            {
                Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: R1 structure '{r1ModelName}' has no meshes.");
            }

            Collider[] colliders = AddPartColliders(model.transform);

            var stages = new List<Transform>(4);
            for (int stageIndex = 1; stageIndex <= 8; stageIndex++)
            {
                Transform stage = model.transform.Find(StagePrefix + stageIndex);
                if (stage == null)
                {
                    break;
                }

                stages.Add(stage);
            }

            if (stages.Count == 0)
            {
                Object.DestroyImmediate(root);
                throw new InvalidOperationException(
                    $"BaseStructureRecipes: R1 structure '{r1ModelName}' has no Stage_1..N children.");
            }

            var lampRenderers = new List<Renderer>();
            Renderer[] allRenderers = model.GetComponentsInChildren<Renderer>(true);
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

            GameObject lightGo = scratch.Create("StatusGlow", root.transform);
            lightGo.transform.localPosition = new Vector3(
                bounds.center.x,
                Mathf.Max(0.85f, bounds.max.y * 0.78f),
                bounds.center.z);
            var statusLight = lightGo.AddComponent<Light>();
            statusLight.type = LightType.Point;
            statusLight.color = StatusLightColor;
            statusLight.intensity = StatusLightIntensity;
            statusLight.range = StatusLightRange;
            statusLight.shadows = LightShadows.None;
            statusLight.enabled = false;

            Transform[] stageArray = stages.ToArray();
            StructureView.StageWeldSeam[] stageSeams = StructureView.BuildStageSeams(stageArray, root.transform);
            for (int s = 0; s < stageSeams.Length; s++)
            {
                if (!stageSeams[s].IsValid)
                {
                    Object.DestroyImmediate(root);
                    throw new InvalidOperationException(
                        $"BaseStructureRecipes: R1 structure '{r1ModelName}' stage '{stageArray[s].name}' has no renderers or valid weld seam.");
                }
            }

            var view = root.AddComponent<StructureView>();
            view.Populate(
                structureId,
                stageArray,
                lampRenderers.ToArray(),
                colliders,
                statusLight,
                StatusLightIntensity,
                stageSeams);
            return root;
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
            Transform rootTransform = root.transform;
            Quaternion rootRot = rootTransform.localRotation;

            if (TryUprightBounds(rootTransform, rootRot, out Bounds bounds))
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
                if (child == rootTransform || !child.name.StartsWith(SocketPowerPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 offset = rootRot * rootTransform.InverseTransformPoint(child.position);
                var horizontal = new Vector3(offset.x, 0f, offset.z);
                Vector3 direction;
                if (horizontal.sqrMagnitude > 1e-6f)
                {
                    direction = horizontal.normalized;
                }
                else
                {
                    Vector3 fwd = rootRot * rootTransform.InverseTransformDirection(child.forward);
                    fwd.y = 0f;
                    direction = fwd.sqrMagnitude > 1e-6f ? fwd.normalized : Vector3.forward;
                }

                portList.Add(new StructurePort(offset, direction));
            }

            ports = portList.ToArray();
        }

        private static bool TryUprightBounds(Transform rootTransform, Quaternion rootRot, out Bounds bounds)
        {
            bool found = false;
            bounds = new Bounds();
            foreach (MeshFilter filter in rootTransform.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3(
                        (corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                        (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = rootRot * rootTransform.InverseTransformPoint(filter.transform.TransformPoint(local));
                    if (found)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }

            return found;
        }

        private static void ExtractBoundsAndPorts(
            Transform root,
            float clearance,
            ref Vector3 footprint,
            out StructurePort[] ports)
        {
            ExtractDimensionsAndPorts(root.gameObject, out Vector3 dimensions, out _, out StructurePort[] socketPorts);
            if (TryUprightBounds(root, root.localRotation, out _))
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
