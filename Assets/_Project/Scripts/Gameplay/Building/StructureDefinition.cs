using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Definition of one buildable robot-scale base structure (0.5.7): identity, localization key, footprint and
    /// terrain slope/clearance rules, power ratings (production, consumption, storage, connection reach), material
    /// build cost and weld duration, local cable/snap ports, and prefab reference with explicit art-pending flag.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Structure Definition", fileName = "Structure")]
    public sealed class StructureDefinition : ScriptableObject
    {
        [Tooltip("Stable structure id (e.g. solar_array). Used by saves, events, and catalog lookups.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Localization key for the structure's player-facing name (e.g. structure.solar_array.name).")]
        [SerializeField] private string _nameKey = string.Empty;

        [Tooltip("Footprint width along local X in metres.")]
        [Range(0.2f, 20f)] [SerializeField] private float _width = 1.5f;

        [Tooltip("Footprint depth along local Z in metres.")]
        [Range(0.2f, 20f)] [SerializeField] private float _depth = 1.5f;

        [Tooltip("Structure height along local Y in metres.")]
        [Range(0.1f, 20f)] [SerializeField] private float _height = 1.2f;

        [Tooltip("Steepest ground slope in degrees on which this structure may be placed.")]
        [Range(1f, 45f)] [SerializeField] private float _maxGroundSlope = 15f;

        [Tooltip("Minimum clear margin around the footprint in metres before another structure may stand.")]
        [Range(0f, 5f)] [SerializeField] private float _clearance = 0.25f;

        [Tooltip("Electrical power produced in Watts at full sunlight (sunlight = 1).")]
        [Range(0f, 5000f)] [SerializeField] private float _produceWatts;

        [Tooltip("Electrical power demanded in Watts when operating at full speed.")]
        [Range(0f, 5000f)] [SerializeField] private float _consumeWatts;

        [Tooltip("Electrical energy capacity stored in Watt-hours (battery banks).")]
        [Range(0f, 20000f)] [SerializeField] private float _storeWattHours;

        [Tooltip("Power grid connection reach in metres: the array, the pylon, and the pad and bank (so a bank set " +
            "down beside its pad always connects); 0 for a consumer that hangs on a nearby hub.")]
        [Range(0f, 100f)] [SerializeField] private float _connectionRadius;

        [Tooltip("Salvage materials paid to place and weld this structure.")]
        [SerializeField] private Recipe _buildCost;

        [Tooltip("Seconds of active welding by 07 required to complete the structure.")]
        [Range(0.5f, 60f)] [SerializeField] private float _buildSeconds = 3f;

        [Tooltip("Cable and snap ports in local space (position + outward horizontal direction).")]
        [SerializeField] private StructurePort[] _ports = Array.Empty<StructurePort>();

        [Tooltip("Art prefab for this structure; may be null only while ArtPending is explicitly true.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Explicitly marks that the art prefab is still pending; validator reports this flag.")]
        [SerializeField] private bool _artPending;

        public string Id => _id;

        public string NameKey => _nameKey;

        public float Width => _width;

        public float Depth => _depth;

        public float Height => _height;

        /// <summary>Footprint dimensions (Width, Height, Depth) in metres.</summary>
        public Vector3 Footprint => new Vector3(_width, _height, _depth);

        public float MaxGroundSlope => _maxGroundSlope;

        public float Clearance => _clearance;

        public float ProduceWatts => _produceWatts;

        public float ConsumeWatts => _consumeWatts;

        public float StoreWattHours => _storeWattHours;

        public float ConnectionRadius => _connectionRadius;

        public Recipe BuildCost => _buildCost;

        public float BuildSeconds => _buildSeconds;

        public IReadOnlyList<StructurePort> Ports => _ports;

        public int PortCount => _ports != null ? _ports.Length : 0;

        public GameObject Prefab => _prefab;

        public bool ArtPending => _artPending;

        /// <summary>True when this structure generates power in sunlight.</summary>
        public bool IsPowerSource => _produceWatts > 0f;

        /// <summary>True when this structure draws power from the grid.</summary>
        public bool IsPowerConsumer => _consumeWatts > 0f;

        /// <summary>True when this structure stores energy in Watt-hours.</summary>
        public bool IsPowerStorage => _storeWattHours > 0f;

        /// <summary>True when this structure extends power grid connectivity to neighbours within its radius.</summary>
        public bool ExtendsPowerReach => _connectionRadius > 0f;

        public StructurePort PortAt(int index)
        {
            return _ports[index];
        }

        /// <summary>Populates all fields of the definition (used by content builders and EditMode tests).</summary>
        public void Populate(
            string id,
            string nameKey,
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
            GameObject prefab,
            bool artPending)
        {
            _id = id ?? string.Empty;
            _nameKey = nameKey ?? string.Empty;
            _width = footprint.x;
            _height = footprint.y;
            _depth = footprint.z;
            _maxGroundSlope = maxGroundSlope;
            _clearance = clearance;
            _produceWatts = produceWatts;
            _consumeWatts = consumeWatts;
            _storeWattHours = storeWattHours;
            _connectionRadius = connectionRadius;
            _buildCost = buildCost;
            _buildSeconds = buildSeconds;
            _ports = ports ?? Array.Empty<StructurePort>();
            _prefab = prefab;
            _artPending = artPending;
        }

        /// <summary>Null when the definition is valid, else the first problem found.</summary>
        public string Validate()
        {
            return Validate(out _);
        }

        /// <summary>
        /// Null when the definition is valid, else the first problem found. Reports via <paramref name="artPending"/>
        /// whether the definition is valid only because its prefab is explicitly flagged as pending art.
        /// </summary>
        public string Validate(out bool artPending)
        {
            artPending = _artPending && _prefab == null;

            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (string.IsNullOrWhiteSpace(_nameKey))
            {
                return $"'{_id}' has no localized name key";
            }

            if (_width <= 0f || _depth <= 0f || _height <= 0f)
            {
                return $"'{_id}' has non-positive footprint ({_width} x {_depth} x {_height})";
            }

            if (_maxGroundSlope <= 0f || _maxGroundSlope > 89f)
            {
                return $"'{_id}' has invalid maxGroundSlope ({_maxGroundSlope})";
            }

            if (_clearance < 0f)
            {
                return $"'{_id}' has negative clearance ({_clearance})";
            }

            if (_produceWatts < 0f || _consumeWatts < 0f || _storeWattHours < 0f)
            {
                return $"'{_id}' has negative power rating";
            }

            if (_connectionRadius < 0f)
            {
                return $"'{_id}' has negative connectionRadius ({_connectionRadius})";
            }

            if (_buildCost.Metal < 0 || _buildCost.Wiring < 0 || _buildCost.Optics < 0 || _buildCost.Total <= 0)
            {
                return $"'{_id}' must have a positive, non-negative buildCost";
            }

            if (_buildSeconds <= 0f)
            {
                return $"'{_id}' has non-positive buildSeconds ({_buildSeconds})";
            }

            if (_ports == null)
            {
                return $"'{_id}' has a null ports array";
            }

            for (int i = 0; i < _ports.Length; i++)
            {
                if (!_ports[i].HasValidDirection)
                {
                    return $"'{_id}' port {i} has a zero horizontal direction";
                }
            }

            if (_prefab == null && !_artPending)
            {
                return $"'{_id}' has no prefab and ArtPending is false";
            }

            if (_prefab != null && _artPending)
            {
                return $"'{_id}' has a prefab assigned while ArtPending is true";
            }

            return null;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isUpdating)
            {
                return;
            }
#endif
            if (string.IsNullOrEmpty(_id))
            {
                return;
            }

            string problem = Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(StructureDefinition)} '{name}': {problem}", this);
            }
        }
    }
}
