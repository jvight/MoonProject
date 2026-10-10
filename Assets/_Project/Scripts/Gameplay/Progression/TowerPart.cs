using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A tower story part: what one tower level needs besides materials, and where 07 finds it.</summary>
    [Serializable]
    public struct TowerPart
    {
        [Tooltip("Art's model of the part under Generated/Art/Pickups (Part_TowerCoil, ...); also its id in saves.")]
        [SerializeField] private string _partId;

        [Tooltip("The tower level (1-based) that cannot be built until this part is fed to the tower's port.")]
        [Min(1)] [SerializeField] private int _towerLevel;

        [Tooltip("Where the part waits for 07.")]
        [SerializeField] private TowerPartSource _source;

        [Tooltip("For a site's heart: the salvage site holding it (e.g. site.depot).")]
        [SerializeField] private string _siteId;

        [Tooltip("For a site's heart: offset (m) from the Heart in the site's frame (x to its right, y along its " +
                 "forward), clear of the heart's relics.")]
        [SerializeField] private Vector2 _heartOffset;

        [Tooltip("Tilly spots it only once 07 has looked across the chasm at Bell's light (M3-16 P2): the bay's " +
                 "power waits on that look.")]
        [SerializeField] private bool _afterTheChasm;

        public TowerPart(string partId, int towerLevel, TowerPartSource source, string siteId, Vector2 heartOffset,
            bool afterTheChasm = false)
        {
            _afterTheChasm = afterTheChasm;
            _partId = partId;
            _towerLevel = towerLevel;
            _source = source;
            _siteId = siteId;
            _heartOffset = heartOffset;
        }

        public string PartId => _partId;

        public int TowerLevel => _towerLevel;

        public TowerPartSource Source => _source;

        public string SiteId => _siteId;

        public Vector2 HeartOffset => _heartOffset;

        public bool AfterTheChasm => _afterTheChasm;
    }
}
