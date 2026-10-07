using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A crew member's tin box left out in the world with one of their logs inside (docs/STORY.md "Logs"). Reaching
    /// it opens the log card (log.&lt;logId&gt; in the localization tables). Written by the Gameplay/Content builder;
    /// runtime code only reads it.
    /// </summary>
    public sealed class LogCacheDefinition : ScriptableObject
    {
        [Tooltip("Stable id of the log inside (saves, Core CrewLogFound, localization log.<id>), e.g. ro_1.")]
        [SerializeField] private string _logId = string.Empty;

        [Tooltip("Art prefab of the cache (Generated/Art/Props/LogCache.prefab): root on the ground, +Z = lid front.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("The world anchor and offset it stands at.")]
        [SerializeField] private AnchorSpot _anchor;

        [Tooltip("The rover ability needed to reach it (Bell never points behind a gate 07 cannot pass yet).")]
        [SerializeField] private AbilityGate _gate;

        public string LogId => _logId;
        public GameObject Prefab => _prefab;
        public AnchorSpot Anchor => _anchor;
        public AbilityGate Gate => _gate;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_logId))
            {
                return "has no log id";
            }

            if (_prefab == null)
            {
                return $"'{_logId}' needs its cache prefab (Generated/Art/Props)";
            }

            if (_anchor == null || string.IsNullOrWhiteSpace(_anchor.AnchorId))
            {
                return $"'{_logId}' names no world anchor";
            }

            return _gate == null ? $"'{_logId}' has no ability gate" : null;
        }

        internal void Populate(string logId, GameObject prefab, AnchorSpot anchor, AbilityGate gate)
        {
            _logId = logId;
            _prefab = prefab;
            _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }
    }
}
