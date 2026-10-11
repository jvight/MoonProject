using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where a kit's blueprint tube lies (docs/features/M3-16 "Blueprints", M3-21 §4): in a salvage site's heart, found
    /// with the heart's relic, or on the ground at a world anchor, found by driving through it.
    /// </summary>
    [Serializable]
    public struct BlueprintTube
    {
        [Tooltip("The kit the blueprint pins (an upgrade id the bay sells, e.g. rover.cargo_cradle).")]
        [SerializeField] private string _kitId;

        [Tooltip("The salvage site whose heart holds the tube (e.g. site.garage); empty when it lies at an anchor.")]
        [SerializeField] private string _siteId;

        [Tooltip("The world anchor the tube waits at instead of a site's heart (e.g. canyon.lip, the chasm's near " +
                 "lip); empty for a site's heart.")]
        [SerializeField] private string _anchorId;

        [Tooltip("Metres the tube lies back from its anchor, against the way 07 arrives (away from a drop).")]
        [Range(0f, 20f)] [SerializeField] private float _setback;

        /// <summary>A tube in <paramref name="siteId"/>'s heart.</summary>
        public BlueprintTube(string kitId, string siteId)
        {
            _kitId = kitId;
            _siteId = siteId;
            _anchorId = string.Empty;
            _setback = 0f;
        }

        private BlueprintTube(string kitId, string anchorId, float setback)
        {
            _kitId = kitId;
            _siteId = string.Empty;
            _anchorId = anchorId;
            _setback = setback;
        }

        public string KitId => _kitId;

        /// <summary>The site whose heart holds it, or empty.</summary>
        public string SiteId => _siteId;

        /// <summary>The world anchor it waits at, or empty.</summary>
        public string AnchorId => _anchorId;

        public float Setback => _setback;

        public bool IsAtAnchor => !string.IsNullOrEmpty(_anchorId);

        /// <summary>A tube waiting on the ground <paramref name="setback"/> metres back from an anchor.</summary>
        public static BlueprintTube AtAnchor(string kitId, string anchorId, float setback)
        {
            return new BlueprintTube(kitId, anchorId, setback);
        }
    }
}
