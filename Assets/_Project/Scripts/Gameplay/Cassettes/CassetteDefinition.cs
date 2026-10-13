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

        [Tooltip("Where it waits: at a world anchor, or on the derelict standing on one.")]
        [SerializeField] private CassetteSiteRule _site;

        [Tooltip("The anchor and offset it waits at (the offset is the Anchor rule's only; the Derelict rule's " +
                 "anchor is the derelict's micro-site, e.g. micro.06).")]
        [SerializeField] private AnchorSpot _anchor;

        [Tooltip("The rover ability needed to reach it (Bell never points behind a gate 07 cannot pass yet).")]
        [SerializeField] private AbilityGate _gate;

        [Tooltip("Art's sealed container it rests in (TapeCase, SupplyPod; 0.5.5 §2), its seal cut to find it.")]
        [SerializeField] private string _container;

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public CassetteSiteRule Site => _site;
        public AnchorSpot Anchor => _anchor;
        public AbilityGate Gate => _gate;

        /// <summary>Art's container prefab the tape rests in.</summary>
        public string Container => _container ?? string.Empty;

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

            if (string.IsNullOrWhiteSpace(_container))
            {
                return $"'{_id}' rests in no container (no tape lies on open ground, 0.5.5 §2)";
            }

            switch (_site)
            {
                case CassetteSiteRule.Anchor:
                case CassetteSiteRule.Derelict:
                    return _anchor == null || string.IsNullOrWhiteSpace(_anchor.AnchorId)
                        ? $"'{_id}' waits at an anchor but names none"
                        : null;
                default:
                    return $"'{_id}' has an unknown site rule {_site}";
            }
        }

        internal void Populate(string id, GameObject prefab, CassetteSiteRule site, AnchorSpot anchor,
            AbilityGate gate, string container)
        {
            _container = container ?? string.Empty;
            _id = id;
            _prefab = prefab;
            _site = site;
            _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        }
    }
}
