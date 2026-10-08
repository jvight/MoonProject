using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One salvage site in the catalog: its World anchor and Art's site prefab (docs/features/M3-13: a Skeleton, the
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" pieces each with a CutPoint, and the Heart where a relic rests).
    /// </summary>
    [Serializable]
    public sealed class SalvageSiteEntry
    {
        [Tooltip("World anchor the site stands on (Core WorldAnchorIds.SitePrefix + name, e.g. site.depot).")]
        [SerializeField] private string _anchorId = string.Empty;

        [Tooltip("Art's site prefab (Generated/Art/Sites/Site_<name>.prefab), root at the anchor, +Z its forward.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("The rover ability reaching it needs (the canyon lander stands past the Hover-Jump gate).")]
        [SerializeField] private AbilityGate _gate = AbilityGate.Open;

        public SalvageSiteEntry(string anchorId, GameObject prefab, AbilityGate gate)
        {
            _anchorId = anchorId;
            _prefab = prefab;
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        /// <summary>The site's id everywhere (saves, events, relic definitions): its anchor id.</summary>
        public string AnchorId => _anchorId;

        public GameObject Prefab => _prefab;

        public AbilityGate Gate => _gate;

        /// <summary>Null when the entry is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_anchorId) ||
                !_anchorId.StartsWith(WorldAnchorIds.SitePrefix, StringComparison.Ordinal))
            {
                return $"has no '{WorldAnchorIds.SitePrefix}' anchor id ('{_anchorId}')";
            }

            if (_prefab == null)
            {
                return $"'{_anchorId}' has no prefab (Generated/Art/Sites)";
            }

            return _gate == null ? $"'{_anchorId}' has no ability gate" : null;
        }
    }
}
