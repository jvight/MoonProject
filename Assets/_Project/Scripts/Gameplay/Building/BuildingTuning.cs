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

        [Tooltip("Heading (degrees, 0 faces +Z) the charging pad takes when its ghost settles onto the apron's bay: " +
                 "its open front toward the south, where 07 rolls in from the lander's cable, the cooling tower " +
                 "at the back.")]
        [Range(0f, 360f)] [SerializeField] private float _apronPadYaw = 180f;

        [Tooltip("Metres from the apron's centre within which the charging pad's riding ghost settles onto the " +
                 "apron's marked bay.")]
        [Range(0.5f, 8f)] [SerializeField] private float _apronSnapRadius = 3f;

        [Tooltip("Metres kept inside the weld range when the riding ghost settles onto the apron: the bay only " +
                 "catches the ghost while 07 stands this much closer than the weld range, so a placed frame can " +
                 "always be welded where 07 stands.")]
        [Range(0f, 2f)] [SerializeField] private float _apronSnapReachMargin = 0.5f;

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

        [Header("Printed in place (build C)")]
        [Tooltip("Layers a print rises in: its layer line steps up one layer at a time, so the rise reads as made.")]
        [Range(8, 120)] [SerializeField] private int _printLayers = 40;

        [Tooltip("Share of the print the weld point stands at no lower than, so the beam starts on the first layer.")]
        [Range(0f, 0.2f)] [SerializeField] private float _printWeldFloor = 0.025f;

        [Tooltip("Lowest and highest share of the print the rising layer shows while welding (x: start, y: end).")]
        [SerializeField] private Vector2 _printShownRange = new Vector2(0.03f, 0.995f);

        [Tooltip("Metres the layer line keeps inside the structure's foot and top.")]
        [Range(0f, 0.2f)] [SerializeField] private float _printLayerInset = 0.03f;

        [Tooltip("Full side-to-side sweeps the weld point and the guide's bead make across the layer over a print.")]
        [Range(1f, 20f)] [SerializeField] private float _printSweepCycles = 6f;

        [Tooltip("Share of the layer's half width the weld point sweeps across.")]
        [Range(0.1f, 1f)] [SerializeField] private float _printWeldSweepShare = 0.85f;

        [Tooltip("Metres (at the guide's reference width) the bead travels either side of the layer line's middle.")]
        [Range(0.05f, 1f)] [SerializeField] private float _printBeadSweep = 0.56f;

        [Tooltip("Metres the bead rides in front of the layer line, so it never sinks into the filament.")]
        [Range(0f, 0.05f)] [SerializeField] private float _printBeadDepth = 0.01f;

        [Tooltip("Share of the print's height where the layer moves from the deck's cross-section to the upper " +
                 "stage's (x: start, y: end).")]
        [SerializeField] private Vector2 _printUpperBlend = new Vector2(0.18f, 0.32f);

        [Tooltip("Share of the print's height over which the layer crosses the deck from its back to its front.")]
        [Range(0.05f, 1f)] [SerializeField] private float _printDeckTravel = 0.24f;

        [Tooltip("Metres the layer keeps inside the deck's front and back edges as it crosses.")]
        [Range(0f, 1f)] [SerializeField] private float _printDeckInset = 0.28f;

        [Tooltip("Metres in from the upper stage's front the layer stands.")]
        [Range(0f, 0.5f)] [SerializeField] private float _printUpperInset = 0.06f;

        [Tooltip("Share of the deck's half width the layer spans.")]
        [Range(0.1f, 1f)] [SerializeField] private float _printDeckWidthShare = 0.68f;

        [Tooltip("Narrowest and widest half width (m) of the layer across the deck.")]
        [Min(0f)] [SerializeField] private Vector2 _printDeckHalfWidth = new Vector2(0.24f, 0.68f);

        [Tooltip("Share of the upper stage's half width the layer spans.")]
        [Range(0.1f, 1f)] [SerializeField] private float _printUpperWidthShare = 0.82f;

        [Tooltip("Narrowest and widest half width (m) of the layer across the upper stage.")]
        [Min(0f)] [SerializeField] private Vector2 _printUpperHalfWidth = new Vector2(0.12f, 0.28f);

        [Tooltip("Half width (m) of the layer while the structure has no measured stages yet.")]
        [Range(0.05f, 1f)] [SerializeField] private float _printFallbackHalfWidth = 0.45f;

        [Tooltip("Heat left on the top stage after a print, so the last layer cools visibly.")]
        [Range(0f, 1f)] [SerializeField] private float _printResidualHeat = 0.45f;

        [Tooltip("Metres round the weld point the print's metal glows hot.")]
        [Range(0.05f, 1.5f)] [SerializeField] private float _printSeamRadius = 0.38f;

        [Tooltip("How fast the settle thump's wobble dies away (higher: a shorter ring).")]
        [Range(0.5f, 12f)] [SerializeField] private float _settleDamping = 4.8f;

        [Tooltip("Half-waves the settle thump rings through before it dies away.")]
        [Range(0.5f, 6f)] [SerializeField] private float _settleHalfWaves = 2.2f;

        [Tooltip("Share the structure squashes down by at the settle thump's deepest point.")]
        [Range(0f, 0.15f)] [SerializeField] private float _settleSquash = 0.032f;

        [Tooltip("Share the structure bulges sideways by at the settle thump's deepest point.")]
        [Range(0f, 0.15f)] [SerializeField] private float _settleStretch = 0.018f;

        [Header("The print guide (build C)")]
        [Tooltip("Size (m) of the guide's bright filament along the layer line at its reference width.")]
        [Min(0.001f)] [SerializeField] private Vector3 _guideFilamentSize = new Vector3(1.52f, 0.015f, 0.018f);

        [Tooltip("Metres either side of the filament's middle its end ticks stand.")]
        [Range(0.1f, 2f)] [SerializeField] private float _guideTickOffset = 0.77f;

        [Tooltip("Size (m) of each end tick of the print guide.")]
        [Min(0.001f)] [SerializeField] private Vector3 _guideTickSize = new Vector3(0.024f, 0.048f, 0.14f);

        [Tooltip("Size (m) of the molten bead that travels along the layer line.")]
        [Min(0.001f)] [SerializeField] private Vector3 _guideBeadSize = new Vector3(0.18f, 0.026f, 0.032f);

        [Tooltip("Layer width (m) at which the guide is drawn at its authored size; it scales to the layer from here.")]
        [Range(0.2f, 3f)] [SerializeField] private float _guideReferenceWidth = 1.48f;

        [Tooltip("Smallest and largest scale of the guide against its reference width.")]
        [Min(0.01f)] [SerializeField] private Vector2 _guideScaleRange = new Vector2(0.28f, 1.08f);

        [Tooltip("Hologram brightness of the guide's parts (1: as bright as a ghost).")]
        [Range(0.2f, 3f)] [SerializeField] private float _guideBrightness = 1.35f;

        [Header("Known blueprints")]
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

        public float ApronPadYaw => _apronPadYaw;

        public float ApronSnapRadius => _apronSnapRadius;

        public float ApronSnapReachMargin => _apronSnapReachMargin;

        public float SettleThumpMeters => _settleThumpMeters;

        public int SettleDustRingCount => _settleDustRingCount;

        public float LinkHeight => _linkHeight;

        public float LinkSag => _linkSag;

        public float LinkWidth => _linkWidth;

        public Color LinkColor => _linkColor;

        public float LooseLength => _looseLength;

        public Color LooseColor => _looseColor;

        public float LoosePlugWidth => _loosePlugWidth;

        public int PrintLayers => _printLayers;

        public float PrintWeldFloor => _printWeldFloor;

        public Vector2 PrintShownRange => _printShownRange;

        public float PrintLayerInset => _printLayerInset;

        public float PrintSweepCycles => _printSweepCycles;

        public float PrintWeldSweepShare => _printWeldSweepShare;

        public float PrintBeadSweep => _printBeadSweep;

        public float PrintBeadDepth => _printBeadDepth;

        public Vector2 PrintUpperBlend => _printUpperBlend;

        public float PrintDeckTravel => _printDeckTravel;

        public float PrintDeckInset => _printDeckInset;

        public float PrintUpperInset => _printUpperInset;

        public float PrintDeckWidthShare => _printDeckWidthShare;

        public Vector2 PrintDeckHalfWidth => _printDeckHalfWidth;

        public float PrintUpperWidthShare => _printUpperWidthShare;

        public Vector2 PrintUpperHalfWidth => _printUpperHalfWidth;

        public float PrintFallbackHalfWidth => _printFallbackHalfWidth;

        public float PrintResidualHeat => _printResidualHeat;

        public float PrintSeamRadius => _printSeamRadius;

        public float SettleDamping => _settleDamping;

        public float SettleHalfWaves => _settleHalfWaves;

        public float SettleSquash => _settleSquash;

        public float SettleStretch => _settleStretch;

        public Vector3 GuideFilamentSize => _guideFilamentSize;

        public float GuideTickOffset => _guideTickOffset;

        public Vector3 GuideTickSize => _guideTickSize;

        public Vector3 GuideBeadSize => _guideBeadSize;

        public float GuideReferenceWidth => _guideReferenceWidth;

        public Vector2 GuideScaleRange => _guideScaleRange;

        public float GuideBrightness => _guideBrightness;


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
