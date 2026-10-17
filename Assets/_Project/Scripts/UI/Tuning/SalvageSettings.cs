using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The salvage UI (docs/features/M3-13): the hold ring at a piece's cut point.
    /// </summary>
    [Serializable]
    public sealed class SalvageSettings
    {
        [Tooltip("The hold ring easing in and out at the cut point.")]
        [SerializeField] private RevealSettings _ring = new RevealSettings(0.3f, 0.5f, 0f, 0.85f, 1.8f, 0.6f);

        [Tooltip("Seconds the ring's charge segment takes to empty when the player lets go before ignition.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _chargeEmptySeconds = 0.2f;

        [Tooltip("Seconds the full charge segment takes to ease away at ignition, as the progress fill takes over.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _chargeHandoffSeconds = 0.35f;

        public RevealSettings Ring => _ring;

        public float ChargeEmptySeconds => _chargeEmptySeconds;

        public float ChargeHandoffSeconds => _chargeHandoffSeconds;
    }
}
