using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The recovery protocol's rows after Power (docs/features/M3-21, opening-v2 §2): how many lamps each shows. They
    /// stay dark through the opening; Power counts the base's own power steps.
    /// </summary>
    [Serializable]
    public struct RecoveryLamps
    {
        [Tooltip("The Link row: the masts 07 builds toward the west signal.")]
        [Range(0, 8)] [SerializeField] private int _link;

        [Tooltip("The Dish row.")]
        [Range(0, 8)] [SerializeField] private int _dish;

        [Tooltip("The Answer row: the broadcast.")]
        [Range(0, 4)] [SerializeField] private int _answer;

        public RecoveryLamps(int link, int dish, int answer)
        {
            _link = link;
            _dish = dish;
            _answer = answer;
        }

        public int Link => _link;

        public int Dish => _dish;

        public int Answer => _answer;
    }
}
