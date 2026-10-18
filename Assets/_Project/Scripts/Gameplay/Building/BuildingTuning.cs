using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Designer-facing tuning for 07's freeform base building (0.5.7): placement distance bounds from 07, soft-snap
    /// distance and angle tolerances, welding range and the weld's beam and sparks, deconstruction material refund
    /// share, and the optional playtest unlock list applied at initialization.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Building Tuning", fileName = "BuildingTuning")]
    public sealed class BuildingTuning : ScriptableObject
    {
        [Tooltip("Closest horizontal distance (m) from 07's pivot at which a structure centre may be placed.")]
        [Range(0.5f, 10f)] [SerializeField] private float _minPlacementDistance = 2.0f;

        [Tooltip("Furthest horizontal distance (m) from 07's pivot at which a structure centre may be placed.")]
        [Range(3f, 40f)] [SerializeField] private float _maxPlacementDistance = 12.0f;

        [Tooltip("Maximum horizontal distance (m) within which a candidate softly snaps to a neighbour's port or edge.")]
        [Range(0.05f, 3f)] [SerializeField] private float _snapDistance = 0.6f;

        [Tooltip("Maximum angular difference (degrees) within which a candidate softly snaps to a neighbour's port or edge.")]
        [Range(1f, 45f)] [SerializeField] private float _snapAngleDegrees = 20f;

        [Tooltip("Maximum horizontal distance (m) from a structure's pivot within which 07's welding beam can build it.")]
        [Range(1f, 20f)] [SerializeField] private float _weldRange = 6.0f;

        [Tooltip("Share (0..1) of a structure's Recipe build cost returned when 07 deconstructs it.")]
        [Range(0f, 1f)] [SerializeField] private float _deconstructRefundShare = 0.75f;

        [Tooltip("Seconds the apron's bay outline takes to warm to its glow as build mode opens, and to cool as it " +
                 "closes: a slow breath, never a switch.")]
        [Range(0.1f, 3f)] [SerializeField] private float _apronGlowFadeSeconds = 0.8f;

        [Tooltip("Metres in front of 07 the placing ghost rides while 07 drives it to its spot (kept inside the " +
                 "placement distances).")]
        [Range(2f, 12f)] [SerializeField] private float _ghostAheadMetres = 4.5f;

        [Tooltip("Metres from the apron's centre within which the charging pad's riding ghost settles onto the " +
                 "apron's marked bay.")]
        [Range(0.5f, 8f)] [SerializeField] private float _apronSnapRadius = 3f;

        [Tooltip("Downward physical settle thump (m) when a structure finishes welding and locks into the regolith.")]
        [Range(0.01f, 0.15f)] [SerializeField] private float _settleThumpMeters = 0.042f;

        [Tooltip("Number of ballistic vacuum dust puffs thrown outward in a ring when a structure finishes settling.")]
        [Range(4, 32)] [SerializeField] private int _settleDustRingCount = 18;

        [Header("The ghost's grid link preview")]
        [Tooltip("Metres above the ground the preview cable leaves the ghost and meets the hub it would join.")]
        [Range(0.1f, 2f)] [SerializeField] private float _linkHeight = 0.6f;

        [Tooltip("Metres the in-reach cable droops at its middle: a cable lying slack, not a laser line.")]
        [Range(0f, 2f)] [SerializeField] private float _linkSag = 0.35f;

        [Tooltip("Width of the preview cable, metres.")]
        [Range(0.01f, 0.2f)] [SerializeField] private float _linkWidth = 0.05f;

        [Tooltip("Colour of the cable when the ghost would join the grid: warm amber, like the old cable's lamps.")]
        [SerializeField] private Color _linkColor = new Color(1f, 0.62f, 0.25f, 0.9f);

        [Tooltip("Metres of the loose cable end that hangs from the ghost when nothing is in reach.")]
        [Range(0.3f, 3f)] [SerializeField] private float _looseLength = 1.2f;

        [Tooltip("Colour of the loose cable end (no link): dull, so it reads as unplugged rather than warning red.")]
        [SerializeField] private Color _looseColor = new Color(0.55f, 0.5f, 0.46f, 0.75f);

        [Tooltip("Width of the loose cable's plug end, metres: a small blunt end that reads as unplugged.")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _loosePlugWidth = 0.14f;

        [Tooltip("Structure IDs that a new game knows by design (e.g. charging_pad), applied on a new game and saved like learned blueprints.")]
        [SerializeField] private string[] _startingKnownIds = Array.Empty<string>();

        [Tooltip("Structure IDs unlocked at game start for playtesting (kept empty in production).")]
        [SerializeField] private string[] _playtestUnlockedIds = Array.Empty<string>();

        public float MinPlacementDistance => _minPlacementDistance;

        public float MaxPlacementDistance => _maxPlacementDistance;

        public float SnapDistance => _snapDistance;

        public float SnapAngleDegrees => _snapAngleDegrees;

        public float WeldRange => _weldRange;

        public float DeconstructRefundShare => _deconstructRefundShare;

        public float ApronGlowFadeSeconds => _apronGlowFadeSeconds;

        public float GhostAheadMetres => Mathf.Clamp(_ghostAheadMetres, _minPlacementDistance, _maxPlacementDistance);

        public float ApronSnapRadius => _apronSnapRadius;

        public float SettleThumpMeters => _settleThumpMeters;

        public int SettleDustRingCount => _settleDustRingCount;

        public float LinkHeight => _linkHeight;

        public float LinkSag => _linkSag;

        public float LinkWidth => _linkWidth;

        public Color LinkColor => _linkColor;

        public float LooseLength => _looseLength;

        public Color LooseColor => _looseColor;

        public float LoosePlugWidth => _loosePlugWidth;


        /// <summary>
        /// Structure IDs that a new game knows by design (e.g. <see cref="StructureIds.ChargingPad"/>), applied on a
        /// new game and saved like learned blueprints.
        /// </summary>
        public IReadOnlyList<string> StartingKnownIds => _startingKnownIds ?? Array.Empty<string>();

        /// <summary>
        /// Structure IDs configured in data to start unlocked for playtests or capture sessions (empty by default in a
        /// normal game).
        /// </summary>
        public IReadOnlyList<string> PlaytestUnlockedIds => _playtestUnlockedIds ?? Array.Empty<string>();

        /// <summary>Populates tuning parameters (used by tests and tuning builders).</summary>
        public void Populate(
            float minPlacementDistance,
            float maxPlacementDistance,
            float snapDistance,
            float snapAngleDegrees,
            float weldRange,
            float deconstructRefundShare,
            string[] playtestUnlockedIds = null,
            string[] startingKnownIds = null)
        {
            _minPlacementDistance = minPlacementDistance;
            _maxPlacementDistance = maxPlacementDistance;
            _snapDistance = snapDistance;
            _snapAngleDegrees = snapAngleDegrees;
            _weldRange = weldRange;
            _deconstructRefundShare = Mathf.Clamp01(deconstructRefundShare);
            _startingKnownIds = SanitizeStructureIdArray(startingKnownIds);
            _playtestUnlockedIds = SanitizeStructureIdArray(playtestUnlockedIds);
        }

        /// <summary>Sets the data-driven playtest unlock structure IDs (empty by default).</summary>
        public void SetPlaytestUnlockedIds(params string[] structureIds)
        {
            _playtestUnlockedIds = SanitizeStructureIdArray(structureIds);
        }

        private void OnValidate()
        {
            if (_maxPlacementDistance < _minPlacementDistance)
            {
                _maxPlacementDistance = _minPlacementDistance;
            }

            _deconstructRefundShare = Mathf.Clamp01(_deconstructRefundShare);
            _settleThumpMeters = Mathf.Clamp(_settleThumpMeters, 0.01f, 0.15f);
            _settleDustRingCount = Mathf.Clamp(_settleDustRingCount, 4, 32);
            _startingKnownIds = SanitizeStructureIdArray(_startingKnownIds);
            _playtestUnlockedIds = SanitizeStructureIdArray(_playtestUnlockedIds);
        }

        private static string[] SanitizeStructureIdArray(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Array.Empty<string>();
            }

            int validCount = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i]))
                {
                    validCount++;
                }
            }

            if (validCount == ids.Length)
            {
                return ids;
            }

            if (validCount == 0)
            {
                return Array.Empty<string>();
            }

            var cleaned = new string[validCount];
            int dst = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ids[i]))
                {
                    cleaned[dst++] = ids[i];
                }
            }

            return cleaned;
        }
    }
}
