using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The salvage UI (docs/features/M3-13): the hold ring at a piece's cut point, and a site's name drifting up the
    /// first time it answers the sonar.
    /// </summary>
    [Serializable]
    public sealed class SalvageSettings
    {
        [Tooltip("The hold ring easing in and out at the cut point.")]
        [SerializeField] private RevealSettings _ring = new RevealSettings(0.3f, 0.5f, 0f, 0.85f, 1.8f, 0.6f);

        [Tooltip("A site's name easing in and out the first time it answers the sonar.")]
        [SerializeField] private RevealSettings _siteName = new RevealSettings(0.9f, 1.4f, 12f, 0.94f, 1f, 0.7f);

        [Tooltip("Seconds a site's name rests fully visible.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _siteNameHoldSeconds = 3f;

        [Tooltip("Metres above a site's anchor where its name floats.")]
        [Range(0f, 30f)]
        [SerializeField] private float _siteNameLiftMetres = 6f;

        [Tooltip("Seconds the ring's charge segment takes to empty when the player lets go before ignition.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _chargeEmptySeconds = 0.2f;

        [Tooltip("Seconds the full charge segment takes to ease away at ignition, as the progress fill takes over.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _chargeHandoffSeconds = 0.35f;

        public RevealSettings Ring => _ring;

        public float ChargeEmptySeconds => _chargeEmptySeconds;

        public float ChargeHandoffSeconds => _chargeHandoffSeconds;

        public RevealSettings SiteName => _siteName;

        public float SiteNameHoldSeconds => _siteNameHoldSeconds;

        public float SiteNameLiftMetres => _siteNameLiftMetres;
    }
}
