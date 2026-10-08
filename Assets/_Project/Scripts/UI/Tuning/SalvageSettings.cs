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

        public RevealSettings Ring => _ring;

        public RevealSettings SiteName => _siteName;

        public float SiteNameHoldSeconds => _siteNameHoldSeconds;

        public float SiteNameLiftMetres => _siteNameLiftMetres;
    }
}
