using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Every friend in the game, in a fixed order (written by the Gameplay/Content builder).</summary>
    public sealed class FriendCatalog : ScriptableObject
    {
        [Tooltip("The friend definitions; order is stable.")]
        [SerializeField] private FriendDefinition[] _friends = Array.Empty<FriendDefinition>();

        public IReadOnlyList<FriendDefinition> Friends => _friends;

        /// <summary>Null when every definition is complete and ids are unique, else the first problem.</summary>
        public string Validate()
        {
            for (int i = 0; i < _friends.Length; i++)
            {
                if (_friends[i] == null)
                {
                    return $"entry {i} is empty";
                }

                string problem = _friends[i].Validate();
                if (problem != null)
                {
                    return $"friend {i} {problem}";
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_friends[j].Id, _friends[i].Id, StringComparison.Ordinal))
                    {
                        return $"friend id '{_friends[i].Id}' appears twice";
                    }
                }
            }

            return null;
        }

        internal void Populate(FriendDefinition[] friends)
        {
            _friends = friends ?? throw new ArgumentNullException(nameof(friends));
        }
    }
}
