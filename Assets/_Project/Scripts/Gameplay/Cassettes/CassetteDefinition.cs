using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Ro's cassette tapes (docs/features/M3-05 "Cassettes"): its id, its Art pickup and where it waits. Its
    /// title and liner note live in the localization tables (cassette.&lt;id&gt;.title, cassette.&lt;id&gt;.note) and
    /// its track in the music playlist. Written by the Gameplay/Content builder; runtime code only reads it.
    /// </summary>
    public sealed class CassetteDefinition : ScriptableObject
    {
        [Tooltip("Stable id (saves, the radio program, localization keys cassette.<id>.*), e.g. after_dark_1.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Art pickup (Generated/Art/Pickups/Cassette_<id>.prefab): meshes only, label +Z.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Where it waits: at a world anchor, or on a basin spot the cassette planner picks.")]
        [SerializeField] private CassetteSiteRule _site;

        [Tooltip("The anchor and offset it waits at (Anchor rule only).")]
        [SerializeField] private AnchorSpot _anchor;

        [Tooltip("Seed of the basin planner (BasinPlanner rule only): the same seed and world give the same spot.")]
        [SerializeField] private int _plannerSeed;

        [Tooltip("The rover ability needed to reach it (Bell never points behind a gate 07 cannot pass yet).")]
        [SerializeField] private AbilityGate _gate;

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public CassetteSiteRule Site => _site;
        public AnchorSpot Anchor => _anchor;
        public int PlannerSeed => _plannerSeed;
        public AbilityGate Gate => _gate;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (_prefab == null)
            {
                return $"'{_id}' needs its pickup prefab (Generated/Art/Pickups)";
            }

            if (_gate == null)
            {
                return $"'{_id}' has no ability gate";
            }

            switch (_site)
            {
                case CassetteSiteRule.Anchor:
                    return _anchor == null || string.IsNullOrWhiteSpace(_anchor.AnchorId)
                        ? $"'{_id}' waits at an anchor but names none"
                        : null;
                case CassetteSiteRule.BasinPlanner:
                    return null;
                default:
                    return $"'{_id}' has an unknown site rule {_site}";
            }
        }

        internal void Populate(string id, GameObject prefab, CassetteSiteRule site, AnchorSpot anchor,
            int plannerSeed, AbilityGate gate)
        {
            _id = id;
            _prefab = prefab;
            _site = site;
            _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _plannerSeed = plannerSeed;
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }
    }
}
