using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How <see cref="RelicSitePlanner"/> spreads the buried relics over the crater floor. Created by the
    /// Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RelicPlacementTuning : ScriptableObject
    {
        [Header("Determinism")]
        [Tooltip("Seed of the placement: the same seed and world always give the same sites.")]
        [SerializeField] private int _seed = 23;

        [Tooltip("Candidate spots tried per relic before relaxing the spacing rule.")]
        [Range(8, 512)] [SerializeField] private int _attemptsPerSite = 128;

        [Header("Every site")]
        [Tooltip("Steepest ground (degrees) a relic may be buried in: 07 must be able to park over it.")]
        [Range(2f, 30f)] [SerializeField] private float _maxSlopeDegrees = 11f;

        [Tooltip("Radius (m) around a site that must also be that flat (where 07 parks).")]
        [Range(0.5f, 10f)] [SerializeField] private float _flatnessProbeRadius = 3f;

        [Tooltip("Metres kept between sites and the edge of the drivable floor.")]
        [Range(0f, 60f)] [SerializeField] private float _edgeMargin = 12f;

        [Tooltip("No relic closer to the base centre than this (m): home stays tidy.")]
        [Range(0f, 100f)] [SerializeField] private float _baseClearance = 32f;

        [Tooltip("Minimum metres between two sites, so each answer points somewhere new.")]
        [Range(5f, 200f)] [SerializeField] private float _minSiteSpacing = 55f;

        [Header("Onboarding (close to home)")]
        [Tooltip("Distance range (m) from the base.")]
        [SerializeField] private Vector2 _onboardingDistance = new Vector2(38f, 68f);

        [Tooltip("Bearing (degrees from +Z toward +X) the onboarding sites gather around: ahead of 07 at spawn.")]
        [Range(-180f, 180f)] [SerializeField] private float _onboardingBearing;

        [Tooltip("Half-width (degrees) of the onboarding fan.")]
        [Range(5f, 180f)] [SerializeField] private float _onboardingSpread = 70f;

        [Header("Wanderers (around the basin)")]
        [Tooltip("Distance range (m) from the base.")]
        [SerializeField] private Vector2 _wandererDistance = new Vector2(95f, 190f);

        [Header("Rim view (toward The Peak)")]
        [Tooltip("Half-width (degrees) of the fan around the bearing of The Peak searched for the rim site.")]
        [Range(5f, 90f)] [SerializeField] private float _rimBearingSpread = 35f;

        [Tooltip("Fraction of the way from the base to the drivable edge the rim site may sit (min, max).")]
        [SerializeField] private Vector2 _rimReach = new Vector2(0.78f, 1f);

        [Tooltip("Height (m) of 07's eye above the rim site, for the line-of-sight check to The Peak.")]
        [Range(0.5f, 5f)] [SerializeField] private float _rimEyeHeight = 1.6f;

        [Tooltip("Fraction of the way to the summit checked for line of sight (the peak's own flank is skipped).")]
        [Range(0.5f, 0.99f)] [SerializeField] private float _rimSightReach = 0.9f;

        [Tooltip("Terrain samples along the line of sight.")]
        [Range(8, 256)] [SerializeField] private int _rimSightSamples = 64;

        public int Seed => _seed;
        public int AttemptsPerSite => _attemptsPerSite;
        public float MaxSlopeDegrees => _maxSlopeDegrees;
        public float FlatnessProbeRadius => _flatnessProbeRadius;
        public float EdgeMargin => _edgeMargin;
        public float BaseClearance => _baseClearance;
        public float MinSiteSpacing => _minSiteSpacing;
        public Vector2 OnboardingDistance => Ordered(_onboardingDistance);
        public float OnboardingBearing => _onboardingBearing;
        public float OnboardingSpread => _onboardingSpread;
        public Vector2 WandererDistance => Ordered(_wandererDistance);
        public float RimBearingSpread => _rimBearingSpread;
        public Vector2 RimReach => Ordered(new Vector2(Mathf.Clamp01(_rimReach.x), Mathf.Clamp01(_rimReach.y)));
        public float RimEyeHeight => _rimEyeHeight;
        public float RimSightReach => _rimSightReach;
        public int RimSightSamples => _rimSightSamples;

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
