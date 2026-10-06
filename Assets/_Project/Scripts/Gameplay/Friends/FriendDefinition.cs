using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of the outpost's lost machines (docs/features/M3-02-friends-tilly.md): its id, its broken and repaired Art
    /// prefabs (same rig node names), its missing parts, where it lies, its perch at the base, its ability and how its
    /// repair runs. Name and repair log live in the localization tables (friend.&lt;id&gt;.name,
    /// friend.&lt;id&gt;.repair_log). Written by the Gameplay/Content builder; runtime code only reads it.
    /// </summary>
    public sealed class FriendDefinition : ScriptableObject
    {
        /// <summary>The abilities the runtime knows (<see cref="AbilityId"/> must be one of them).</summary>
        public const string SpotterAbility = "spotter";

        [Tooltip("Stable id (saves, localization keys friend.<id>.*, the lander's FriendSocket_<id>), e.g. tilly.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Art prefab of the friend as found, lying broken (Generated/Art/Friends/<Name>_Broken.prefab).")]
        [SerializeField] private GameObject _brokenPrefab;

        [Tooltip("Art prefab of the friend repaired (Generated/Art/Friends/<Name>.prefab), same rig nodes.")]
        [SerializeField] private GameObject _repairedPrefab;

        [Tooltip("Its missing parts (3 to 5), in order; each lights one PartLamp_<index> on its body.")]
        [SerializeField] private FriendPart[] _parts = Array.Empty<FriendPart>();

        [Tooltip("Name of the empty on the lander where it perches at home (e.g. FriendSocket_tilly).")]
        [SerializeField] private string _homeSocket = string.Empty;

        [Tooltip("The gift it brings once awake (spotter).")]
        [SerializeField] private string _abilityId = SpotterAbility;

        [Tooltip("Seconds 07's beam stitches it before it boots up.")]
        [Range(1f, 15f)] [SerializeField] private float _repairDuration = 3.5f;

        [Tooltip("Chirp vocabulary id the Audio domain voices it with.")]
        [SerializeField] private string _chirpSet = string.Empty;

        [Tooltip("Where it lies and where its parts are scattered.")]
        [SerializeField] private FriendPlacement _placement;

        public string Id => _id;
        public GameObject BrokenPrefab => _brokenPrefab;
        public GameObject RepairedPrefab => _repairedPrefab;
        public IReadOnlyList<FriendPart> Parts => _parts;
        public string HomeSocket => _homeSocket;
        public string AbilityId => _abilityId;
        public float RepairDuration => _repairDuration;
        public string ChirpSet => _chirpSet;
        public FriendPlacement Placement => _placement;

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

            if (string.IsNullOrWhiteSpace(_homeSocket))
            {
                return $"'{_id}' has no home socket";
            }

            if (_abilityId != SpotterAbility)
            {
                return $"'{_id}' has an unknown ability '{_abilityId}'";
            }

            return _placement == null ? $"'{_id}' has no placement" : null;
        }

        internal void Populate(string id, GameObject brokenPrefab, GameObject repairedPrefab, FriendPart[] parts,
            string homeSocket, string abilityId, float repairDuration, string chirpSet, FriendPlacement placement)
        {
            _id = id;
            _brokenPrefab = brokenPrefab;
            _repairedPrefab = repairedPrefab;
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            _homeSocket = homeSocket;
            _abilityId = abilityId;
            _repairDuration = repairDuration;
            _chirpSet = chirpSet;
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        }
    }
}
