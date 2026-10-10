using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A blueprint tube in a salvage site's heart: found with the heart's relic, it pins a kit.</summary>
    [Serializable]
    public struct BlueprintTube
    {
        [Tooltip("The kit the blueprint pins (an upgrade id the bay sells, e.g. rover.cargo_cradle).")]
        [SerializeField] private string _kitId;

        [Tooltip("The salvage site whose heart holds the tube (e.g. site.garage).")]
        [SerializeField] private string _siteId;

        public BlueprintTube(string kitId, string siteId)
        {
            _kitId = kitId;
            _siteId = siteId;
        }

        public string KitId => _kitId;

        public string SiteId => _siteId;
    }
}
