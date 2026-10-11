using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The recovery panel's lamps that no built thing counts yet (docs/features/M3-21 §1): the Link's last lamp for
    /// the Peak's foot, the great dish's four needs and the answer. Later versions light them.
    /// </summary>
    [Serializable]
    public struct RecoveryLamps
    {
        [Tooltip("The Link row's larger last lamp: the mast at the Peak's foot.")]
        [Range(0, 4)] [SerializeField] private int _peakFoot;

        [Tooltip("The Dish row: Atlas lifts it, the Superconductor, the Power Cell, the Star Tracker aims it.")]
        [Range(0, 8)] [SerializeField] private int _dish;

        [Tooltip("The Answer row: the broadcast.")]
        [Range(0, 4)] [SerializeField] private int _answer;

        public RecoveryLamps(int peakFoot, int dish, int answer)
        {
            _peakFoot = peakFoot;
            _dish = dish;
            _answer = answer;
        }

        public int PeakFoot => _peakFoot;

        public int Dish => _dish;

        public int Answer => _answer;
    }
}
