using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The friend UI: the friend's name drifting up the first time it wakes (a count of its parts on the HUD is the
    /// always-on kind of note the owner rejects: her parts are found in her wrecked sled).
    /// </summary>
    [Serializable]
    public sealed class FriendUiSettings
    {
        [Tooltip("The friend's name easing in and out when it first wakes.")]
        [SerializeField] private RevealSettings _name = new RevealSettings(0.9f, 1.4f, 12f, 0.94f, 1f, 0.7f);

        [Tooltip("Seconds the name rests fully visible.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _nameHoldSeconds = 3.5f;

        [Tooltip("Metres above the friend where its name floats.")]
        [Range(0f, 5f)]
        [SerializeField] private float _nameLiftMetres = 1.1f;

        [Tooltip("Friends whose face sits above the shared lift float their name at their own height (Bell's dial).")]
        [SerializeField] private FriendNameLift[] _nameLifts = { new FriendNameLift("bell", 2f) };

        public RevealSettings Name => _name;

        public float NameHoldSeconds => _nameHoldSeconds;

        /// <summary>Metres above friend <paramref name="friendId"/> where its name floats.</summary>
        public float NameLiftFor(string friendId)
        {
            for (int i = 0; i < _nameLifts.Length; i++)
            {
                if (string.Equals(_nameLifts[i].FriendId, friendId, StringComparison.Ordinal))
                {
                    return _nameLifts[i].LiftMetres;
                }
            }

            return _nameLiftMetres;
        }
    }
}
