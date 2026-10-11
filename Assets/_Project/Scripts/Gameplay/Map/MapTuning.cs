using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07, M3-19 §7): when a place counts as seen or visited, the dock's evening
    /// sweep that marks what home can see, and the chart pen's pace.
    /// </summary>
    [CreateAssetMenu(fileName = "MapTuning", menuName = "MoonProject/Gameplay/Map Tuning")]
    public sealed class MapTuning : ScriptableObject
    {
        [Header("Places")]
        [Tooltip("Metres beyond a place's own radius that still count as having been there.")]
        [Range(0f, 20f)]
        [SerializeField] private float _visitMargin = 3f;

        [Tooltip("Radius (m) of home on the map: the lander and its stations.")]
        [Range(5f, 60f)]
        [SerializeField] private float _homeRadius = 22f;

        [Tooltip("Radius (m) of a friend's site, which has no anchor radius of its own.")]
        [Range(2f, 30f)]
        [SerializeField] private float _friendRadius = 8f;

        [Tooltip("A friend whose site lies this close (m) to another place belongs to that place (Bell at the " +
            "canyon's terminus) instead of getting a place of its own.")]
        [Range(0f, 40f)]
        [SerializeField] private float _friendMergeDistance = 15f;

        [Tooltip("Seconds between two reads of the saved state that also mark places seen or visited (a site's " +
            "answer, a lit mast, a stitched friend).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _pollSeconds = 0.5f;

        [Header("The dock's evening sweep (P6)")]
        [Tooltip("Seconds after the evening opens (the wide shot settling) before the first place blinks seen.")]
        [Range(0f, 6f)]
        [SerializeField] private float _sweepDelay = 1.5f;

        [Tooltip("Seconds between two places blinking in turn.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _sweepGap = 0.35f;

        [Tooltip("Places further than this (m, horizontal) from the dock are beyond the evening's view.")]
        [Range(50f, 1000f)]
        [SerializeField] private float _sweepRange = 450f;

        [Tooltip("Height (m) above the dock of the eye the sweep sees from (the lab's big porthole).")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _sweepEyeHeight = 5f;

        [Tooltip("Height (m) above a place of the light that must be seen.")]
        [Range(0f, 20f)]
        [SerializeField] private float _sweepLightHeight = 3f;

        [Tooltip("Metres between the ground samples along a sightline.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _sightStep = 4f;

        [Tooltip("Metres the ground may rise above a sightline before it hides the light.")]
        [Range(0f, 5f)]
        [SerializeField] private float _sightClearance = 0.5f;

        [Header("The chart pen")]
        [Tooltip("Seconds from the view opening on the map to the chart pen's first stroke (the view eases in).")]
        [Range(0f, 5f)]
        [SerializeField] private float _inkLead = 1.4f;

        [Tooltip("Seconds of one place's pen stroke; the next place starts as it ends.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _inkSeconds = 0.6f;

        [Header("Chargers' circles (M3-21)")]
        [Tooltip("Share of a full battery's flat-ground drive drawn as a charger's circle on the chart: out and back " +
                 "with a margin and a site's work (GDD v3 §3.7: about 140 m on four cells).")]
        [Range(0.1f, 0.5f)]
        [SerializeField] private float _chargerRangeShare = 0.3f;

        public float VisitMargin => _visitMargin;

        public float HomeRadius => _homeRadius;

        public float FriendRadius => _friendRadius;

        public float FriendMergeDistance => _friendMergeDistance;

        public float PollSeconds => _pollSeconds;

        public float SweepDelay => _sweepDelay;

        public float SweepGap => _sweepGap;

        public float SweepRange => _sweepRange;

        public float SweepEyeHeight => _sweepEyeHeight;

        public float SweepLightHeight => _sweepLightHeight;

        public float SightStep => _sightStep;

        public float SightClearance => _sightClearance;

        public float InkLead => _inkLead;

        public float InkSeconds => _inkSeconds;

        public float ChargerRangeShare => _chargerRangeShare;
    }
}
