using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Catalog of all buildable base structures (0.5.7). Provides lookup by structure id and whole-catalog validation
    /// (reporting how many definitions still have art pending).
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Structure Catalog", fileName = "StructureCatalog")]
    public sealed class StructureCatalog : ScriptableObject
    {
        [Tooltip("All buildable structure definitions in deterministic catalog order.")]
        [SerializeField] private StructureDefinition[] _structures = Array.Empty<StructureDefinition>();

        public IReadOnlyList<StructureDefinition> Structures => _structures;

        public int Count => _structures != null ? _structures.Length : 0;

        public StructureDefinition StructureAt(int index)
        {
            return _structures[index];
        }

        /// <summary>Finds a structure definition by <paramref name="id"/>, or returns null when unknown.</summary>
        public StructureDefinition Find(string id)
        {
            return TryGet(id, out StructureDefinition definition) ? definition : null;
        }

        /// <summary>True when a structure with <paramref name="id"/> exists in this catalog.</summary>
        public bool TryGet(string id, out StructureDefinition definition)
        {
            if (!string.IsNullOrEmpty(id) && _structures != null)
            {
                for (int i = 0; i < _structures.Length; i++)
                {
                    StructureDefinition candidate = _structures[i];
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        /// <summary>Writes the structure array into the catalog.</summary>
        public void Populate(StructureDefinition[] structures)
        {
            _structures = structures ?? throw new ArgumentNullException(nameof(structures));
        }

        /// <summary>Null when every definition in the catalog is valid and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            return Validate(out _);
        }

        /// <summary>
        /// Null when every definition in the catalog is valid and ids are unique, else the first problem. Reports via
        /// <paramref name="artPendingCount"/> how many valid definitions have <see cref="StructureDefinition.ArtPending"/>
        /// set while awaiting art prefabs.
        /// </summary>
        public string Validate(out int artPendingCount)
        {
            artPendingCount = 0;
            if (_structures == null || _structures.Length == 0)
            {
                return "has no structures";
            }

            for (int i = 0; i < _structures.Length; i++)
            {
                StructureDefinition current = _structures[i];
                if (current == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = current.Validate(out bool artPending);
                if (problem != null)
                {
                    return $"structure {i} {problem}";
                }

                if (artPending)
                {
                    artPendingCount++;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_structures[j].Id, current.Id, StringComparison.Ordinal))
                    {
                        return $"structure id '{current.Id}' appears twice";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the default 10-structure catalog (charging pad, solar array, battery bank, power pylon, smelter,
        /// fabricator, storage rack, upgrade bay, antenna mast, vehicle pad) with <c>artPending = true</c>.
        /// </summary>
        public static StructureCatalog CreateDefault()
        {
            var catalog = CreateInstance<StructureCatalog>();
            catalog.Populate( new[]
            {
                CreateSpec(StructureIds.ChargingPad, new Vector3(2.2f, 0.4f, 2.2f), 12f, 0.25f,
                    0f, 120f, 0f, 0f, new Recipe(4, 2, 0), 3f),
                CreateSpec(StructureIds.SolarArray, new Vector3(2.0f, 1.8f, 2.0f), 18f, 0.25f,
                    180f, 0f, 0f, 14f, new Recipe(3, 2, 1), 3f),
                CreateSpec(StructureIds.BatteryBank, new Vector3(1.4f, 1.2f, 1.4f), 15f, 0.25f,
                    0f, 0f, 360f, 0f, new Recipe(3, 3, 0), 2.5f),
                CreateSpec(StructureIds.PowerPylon, new Vector3(0.8f, 2.4f, 0.8f), 22f, 0.2f,
                    0f, 15f, 0f, 16f, new Recipe(2, 1, 0), 1.5f),
                CreateSpec(StructureIds.Smelter, new Vector3(2.0f, 1.8f, 2.0f), 12f, 0.3f,
                    0f, 150f, 0f, 0f, new Recipe(6, 2, 0), 4f),
                CreateSpec(StructureIds.Fabricator, new Vector3(2.2f, 1.6f, 1.8f), 12f, 0.3f,
                    0f, 140f, 0f, 0f, new Recipe(5, 3, 1), 4f),
                CreateSpec(StructureIds.StorageRack, new Vector3(1.8f, 1.5f, 1.0f), 16f, 0.2f,
                    0f, 10f, 0f, 0f, new Recipe(4, 0, 0), 2f),
                CreateSpec(StructureIds.UpgradeBay, new Vector3(2.6f, 2.0f, 2.6f), 10f, 0.35f,
                    0f, 160f, 0f, 0f, new Recipe(6, 4, 2), 5f),
                CreateSpec(StructureIds.AntennaMast, new Vector3(1.6f, 3.5f, 1.6f), 15f, 0.3f,
                    0f, 90f, 0f, 0f, new Recipe(4, 3, 2), 4.5f),
                CreateSpec(StructureIds.VehiclePad, new Vector3(3.2f, 0.5f, 3.2f), 10f, 0.4f,
                    0f, 100f, 0f, 0f, new Recipe(8, 3, 1), 5f),
            });
            return catalog;
        }

        private static StructureDefinition CreateSpec(
            string id,
            Vector3 footprint,
            float maxSlope,
            float clearance,
            float produceW,
            float consumeW,
            float storeWh,
            float radius,
            Recipe cost,
            float buildSeconds)
        {
            float halfX = footprint.x * 0.5f + clearance;
            float halfZ = footprint.z * 0.5f + clearance;
            var ports = new[]
            {
                new StructurePort(new Vector3(0f, 0f, halfZ), Vector3.forward),
                new StructurePort(new Vector3(0f, 0f, -halfZ), Vector3.back),
                new StructurePort(new Vector3(halfX, 0f, 0f), Vector3.right),
                new StructurePort(new Vector3(-halfX, 0f, 0f), Vector3.left),
            };

            var definition = CreateInstance<StructureDefinition>();
            definition.name = id;
            definition.Populate(
                id,
                StructureIds.NameKey(id),
                footprint,
                maxSlope,
                clearance,
                produceW,
                consumeW,
                storeWh,
                radius,
                cost,
                buildSeconds,
                ports,
                null,
                true);
            return definition;
        }

        private void OnValidate()
        {
            if (_structures == null || _structures.Length == 0)
            {
                return;
            }

            string problem = Validate(out _);
            if (problem != null)
            {
                Debug.LogError($"{nameof(StructureCatalog)}: {problem}.", this);
            }
        }
    }
}
