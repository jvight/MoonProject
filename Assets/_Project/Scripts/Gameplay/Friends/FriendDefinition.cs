using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of the outpost's lost machines (docs/features/M3-02-friends-tilly.md, M3-05): its id, its broken and
    /// repaired Art prefabs (same rig node names), its missing parts and the items its repair also needs (Bell's
    /// cassette), where it lies, its home socket at the base, its ability and how its repair runs. Name and repair log
    /// live in the localization tables (friend.&lt;id&gt;.name, friend.&lt;id&gt;.repair_log); a friend that announces
    /// its first homecoming has a ticker line ticker.&lt;id&gt;.home. Written by the Gameplay/Content builder;
    /// runtime code only reads it.
    /// </summary>
    public sealed class FriendDefinition : ScriptableObject
    {
        /// <summary>Tilly's gift: she spots undiscovered things near 07 (an <see cref="AbilityId"/>).</summary>
        public const string SpotterAbility = "spotter";

        /// <summary>Bell's gift: the radio dial and her signals (an <see cref="AbilityId"/>).</summary>
        public const string RadioDialAbility = "radio_dial";

        [Tooltip("Stable id (saves, localization keys friend.<id>.*, the lander's FriendSocket_<id>), e.g. tilly.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Art prefab of the friend as found, lying broken (Generated/Art/Friends/<Name>_Broken.prefab).")]
        [SerializeField] private GameObject _brokenPrefab;

        [Tooltip("Art prefab of the friend repaired (Generated/Art/Friends/<Name>.prefab), same rig nodes.")]
        [SerializeField] private GameObject _repairedPrefab;

        [Tooltip("Its body: a hover-drone (Tilly's rig) or a radio cabinet on legs (Bell's rig).")]
        [SerializeField] private FriendBodyKind _body;

        [Tooltip("Its missing parts (3 to 5), in order; each lights one PartLamp_<index> on its body.")]
        [SerializeField] private FriendPart[] _parts = Array.Empty<FriendPart>();

        [Tooltip("Items its repair also needs (e.g. a cassette id); each lights a PartLamp after the parts' lamps.")]
        [SerializeField] private string[] _items = Array.Empty<string>();

        [Tooltip("Which base prefab carries its home socket.")]
        [SerializeField] private FriendHome _home;

        [Tooltip("Name of its home socket at the base (FriendSocket_tilly on the lander, BellCorner on the tower).")]
        [SerializeField] private string _homeSocket = string.Empty;

        [Tooltip("Its first homecoming greeting puts a line on the radio ticker (ticker.<id>.home).")]
        [SerializeField] private bool _announcesHomecoming;

        [Tooltip("The gift it brings once awake (spotter, radio_dial).")]
        [SerializeField] private string _abilityId = SpotterAbility;

        [Tooltip("Seconds 07's beam stitches it before it boots up.")]
        [Range(1f, 15f)] [SerializeField] private float _repairDuration = 3.5f;

        [Tooltip("Chirp vocabulary id the Audio domain voices it with.")]
        [SerializeField] private string _chirpSet = string.Empty;

        [Tooltip("Whether its site is planned on the basin floor or taken from the World's anchors.")]
        [SerializeField] private FriendSiteRule _siteRule;

        [Tooltip("Where it lies and where its parts are scattered (Planner rule).")]
        [SerializeField] private FriendPlacement _placement;

        [Tooltip("Its anchors: site, parts, driving line and way home (Anchors rule).")]
        [SerializeField] private FriendAnchorPlacement _anchors;

        public string Id => _id;
        public GameObject BrokenPrefab => _brokenPrefab;
        public GameObject RepairedPrefab => _repairedPrefab;
        public FriendBodyKind Body => _body;
        public IReadOnlyList<FriendPart> Parts => _parts;
        public IReadOnlyList<string> Items => _items;
        public FriendHome Home => _home;
        public string HomeSocket => _homeSocket;
        public bool AnnouncesHomecoming => _announcesHomecoming;
        public string AbilityId => _abilityId;
        public float RepairDuration => _repairDuration;
        public string ChirpSet => _chirpSet;
        public FriendSiteRule SiteRule => _siteRule;
        public FriendPlacement Placement => _placement;
        public FriendAnchorPlacement Anchors => _anchors;

        /// <summary>Null when the definition is complete, else what is wrong with it.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return "has no id";
            }

            if (_brokenPrefab == null || _repairedPrefab == null)
            {
                return $"'{_id}' needs its broken and repaired prefabs (Generated/Art/Friends)";
            }

            if (_parts.Length < 1 || _parts.Length > FriendProgress.MaxParts)
            {
                return $"'{_id}' needs 1 to {FriendProgress.MaxParts} parts";
            }

            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null || _parts[i].Prefab == null || string.IsNullOrWhiteSpace(_parts[i].Id))
                {
                    return $"'{_id}' part {i} needs an id and a prefab";
                }
            }

            if (_items.Length > FriendProgress.MaxItems)
            {
                return $"'{_id}' needs at most {FriendProgress.MaxItems} items";
            }

            for (int i = 0; i < _items.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_items[i]))
                {
                    return $"'{_id}' item {i} has no id";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_items[i], _items[j], StringComparison.Ordinal))
                    {
                        return $"'{_id}' needs item '{_items[i]}' twice";
                    }
                }
            }

            if (_home != FriendHome.Lander && _home != FriendHome.RadioTower)
            {
                return $"'{_id}' has an unknown home {_home}";
            }

            if (string.IsNullOrWhiteSpace(_homeSocket))
            {
                return $"'{_id}' has no home socket";
            }

            if (_abilityId != SpotterAbility && _abilityId != RadioDialAbility)
            {
                return $"'{_id}' has an unknown ability '{_abilityId}'";
            }

            if (_body != FriendBodyKind.Drone && _body != FriendBodyKind.RadioCabinet)
            {
                return $"'{_id}' has an unknown body {_body}";
            }

            if (_body == FriendBodyKind.RadioCabinet && _items.Length == 0)
            {
                return $"'{_id}' is a radio cabinet with no tape to slide in (its first item)";
            }

            switch (_siteRule)
            {
                case FriendSiteRule.Planner:
                    return _placement == null ? $"'{_id}' has no placement" : null;
                case FriendSiteRule.Anchors:
                    string anchors = _anchors == null ? "has no anchors" : _anchors.Validate();
                    return anchors == null ? null : $"'{_id}' {anchors}";
                default:
                    return $"'{_id}' has an unknown site rule {_siteRule}";
            }
        }

        /// <summary>A friend placed by the basin planner.</summary>
        internal void Populate(string id, GameObject brokenPrefab, GameObject repairedPrefab, FriendBodyKind body,
            FriendPart[] parts, string[] items, FriendHome home, string homeSocket, bool announcesHomecoming,
            string abilityId, float repairDuration, string chirpSet, FriendPlacement placement)
        {
            Fill(id, brokenPrefab, repairedPrefab, body, parts, items, home, homeSocket, announcesHomecoming,
                abilityId, repairDuration, chirpSet);
            _siteRule = FriendSiteRule.Planner;
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
            _anchors = null;
        }

        /// <summary>A friend placed at the World's anchors.</summary>
        internal void Populate(string id, GameObject brokenPrefab, GameObject repairedPrefab, FriendBodyKind body,
            FriendPart[] parts, string[] items, FriendHome home, string homeSocket, bool announcesHomecoming,
            string abilityId, float repairDuration, string chirpSet, FriendAnchorPlacement anchors)
        {
            Fill(id, brokenPrefab, repairedPrefab, body, parts, items, home, homeSocket, announcesHomecoming,
                abilityId, repairDuration, chirpSet);
            _siteRule = FriendSiteRule.Anchors;
            _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
            _placement = null;
        }

        private void Fill(string id, GameObject brokenPrefab, GameObject repairedPrefab, FriendBodyKind body,
            FriendPart[] parts, string[] items, FriendHome home, string homeSocket, bool announcesHomecoming,
            string abilityId, float repairDuration, string chirpSet)
        {
            _id = id;
            _brokenPrefab = brokenPrefab;
            _repairedPrefab = repairedPrefab;
            _body = body;
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _home = home;
            _homeSocket = homeSocket;
            _announcesHomecoming = announcesHomecoming;
            _abilityId = abilityId;
            _repairDuration = repairDuration;
            _chirpSet = chirpSet;
        }
    }
}
