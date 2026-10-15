using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The ten robot-scale base structures of 0.5.7 in deterministic catalog order: charging pad, solar array, battery
    /// bank, power pylon, smelter, fabricator, storage rack, upgrade bay, antenna mast, and vehicle pad. Each entry
    /// holds the authored footprint, terrain slope and clearance limits, power ratings, material build cost, weld
    /// duration, cardinal cable/snap ports, and <c>ArtPending = true</c> while awaiting final Art prefabs.
    /// <see cref="GameplayContentBuilder"/> writes one <see cref="StructureDefinition"/> asset per entry and the
    /// <see cref="StructureCatalog"/> asset.
    /// </summary>
    internal static class BaseStructureRecipes
    {
        public static readonly Entry[] All =
        {
            new Entry(
                StructureIds.ChargingPad,
                new Vector3(2.2f, 0.4f, 2.2f),
                maxGroundSlope: 12f,
                clearance: 0.25f,
                produceWatts: 0f,
                consumeWatts: 120f,
                storeWattHours: 0f,
                connectionRadius: 0f,
                buildCost: new Recipe(4, 2, 0),
                buildSeconds: 3f,
                ports: CardinalPorts(new Vector3(2.2f, 0.4f, 2.2f), 0.25f),
                artPending: true),
            new Entry(
                StructureIds.SolarArray,
                new Vector3(2.0f, 1.8f, 2.0f),
                maxGroundSlope: 18f,
                clearance: 0.25f,
                produceWatts: 180f,
                consumeWatts: 0f,
                storeWattHours: 0f,
                connectionRadius: 14f,
                buildCost: new Recipe(3, 2, 1),
                buildSeconds: 3f,
                ports: CardinalPorts(new Vector3(2.0f, 1.8f, 2.0f), 0.25f),
                artPending: true),
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
                new Vector3(0.8f, 2.4f, 0.8f),
                maxGroundSlope: 22f,
                clearance: 0.2f,
                produceWatts: 0f,
                consumeWatts: 15f,
                storeWattHours: 0f,
                connectionRadius: 16f,
                buildCost: new Recipe(2, 1, 0),
                buildSeconds: 1.5f,
                ports: CardinalPorts(new Vector3(0.8f, 2.4f, 0.8f), 0.2f),
                artPending: true),
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
        /// Creates the in-memory <see cref="StructureDefinition"/> instances for every entry in <see cref="All"/>
        /// (used by <see cref="GameplayContentBuilder"/> and EditMode tests).
        /// </summary>
        public static StructureDefinition[] CreateDefinitions()
        {
            var definitions = new StructureDefinition[All.Length];
            for (int i = 0; i < All.Length; i++)
            {
                Entry entry = All[i];
                var definition = ScriptableObject.CreateInstance<StructureDefinition>();
                definition.name = entry.Id;
                definition.Populate(
                    entry.Id,
                    StructureIds.NameKey(entry.Id),
                    entry.Footprint,
                    entry.MaxGroundSlope,
                    entry.Clearance,
                    entry.ProduceWatts,
                    entry.ConsumeWatts,
                    entry.StoreWattHours,
                    entry.ConnectionRadius,
                    entry.BuildCost,
                    entry.BuildSeconds,
                    entry.Ports,
                    null,
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
                bool artPending)
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
        }
    }
}
