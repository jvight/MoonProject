using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One ruin piece's scan targets (RUINS-WORLD): the R1 ruin prefab id, the structure blueprints it teaches once
    /// every <c>Scan_1..N</c> target on it is scanned, and each target's scan size class.
    /// </summary>
    [Serializable]
    public struct RuinScanSet
    {
        public const string BlueprintPrefix = "ruin.";

        [Tooltip("R1 ruin piece name in [World]/Ruins (e.g. FallenSolarArray).")]
        [SerializeField] private string _ruinId;

        [Tooltip("Structure blueprint ids learned when every Scan_1..N target on this ruin is scanned.")]
        [SerializeField] private string[] _structureIds;

        [Tooltip("Each Scan_1..N target's size class, in 1-based target order.")]
        [SerializeField] private ScanSize[] _sizes;

        public RuinScanSet(string ruinId, string[] structureIds, params ScanSize[] sizes)
        {
            _ruinId = ruinId;
            _structureIds = structureIds ?? Array.Empty<string>();
            _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
        }

        public string RuinId => _ruinId ?? string.Empty;

        public string BlueprintId => BlueprintPrefix + RuinId;

        public IReadOnlyList<string> StructureIds => _structureIds ?? Array.Empty<string>();

        /// <summary>How many Scan_1..N targets make the ruin's scan set.</summary>
        public int Count => _sizes?.Length ?? 0;

        public ScanSize Size(int index)
        {
            return _sizes[index];
        }
    }
}
