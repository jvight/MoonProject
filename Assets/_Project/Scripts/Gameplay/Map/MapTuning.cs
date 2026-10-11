using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07): when a place counts as seen or visited, the deck lookout's "seen" sweep
    /// (level review L5, cutscenes §6), the breath before the hatch view opens on the first ride, and the chart pen's
    /// pace.
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

        [Header("Lookout sweep (L5)")]
        [Tooltip("Height (m) above the deck of the eye the sweep's sightlines start from: the lookout camera's rise.")]
        [Range(1f, 30f)]
        [SerializeField] private float _lookoutEyeHeight = 14f;

        [Tooltip("Height (m) above a place's ground of the light that must be seen for it to blink.")]
        [Range(0f, 10f)]
        [SerializeField] private float _lightHeight = 3f;

        [Tooltip("Metres between two ground samples along a sightline.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _sightStep = 2f;

        [Tooltip("Metres the ground may rise above a sightline before it hides the place.")]
        [Range(0f, 5f)]
        [SerializeField] private float _sightClearance = 0.5f;

        [Tooltip("Seconds after 07 settles on the deck before the first place blinks.")]
        [Range(0f, 10f)]
        [SerializeField] private float _sweepDelay = 2.5f;

        [Tooltip("Seconds between two places' blinks, in turn round the horizon.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _sweepInterval = 1.1f;

        [Tooltip("Radius (m) of a blink as it swells from its first size to its last.")]
        [SerializeField] private Vector2 _blinkRadius = new Vector2(2f, 6f);

        [Tooltip("Seconds a blink takes to swell and fade.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _blinkSeconds = 1.6f;

        [Tooltip("Brightness of a blink at its peak: faint, a pilot light far away.")]
        [Range(0f, 4f)]
        [SerializeField] private float _blinkIntensity = 0.8f;

        [Tooltip("Blinks that can glow at once.")]
        [Range(1, 12)]
        [SerializeField] private int _blinkPool = 4;

        [Header("The hatch and the look")]
        [Tooltip("First ride: seconds without look input after the sweep before the view eases into the map by itself.")]
        [Range(1f, 20f)]
        [SerializeField] private float _breatheSeconds = 6f;

        [Tooltip("First ride: degrees between the view's centre and the hatch window that count as looking at it.")]
        [Range(2f, 60f)]
        [SerializeField] private float _hatchLookAngle = 18f;

        [Tooltip("Metres from the lift's top stop in toward the lander's middle to the hatch window (a stand-in " +
            "until the hatch model carries its own window node).")]
        [Range(0f, 10f)]
        [SerializeField] private float _hatchInset = 2.4f;

        [Tooltip("Height (m) of the hatch window above the lift's top stop: 07's lens height.")]
        [Range(0f, 4f)]
        [SerializeField] private float _hatchHeight = 0.9f;

        [Tooltip("Seconds from the view opening on the map to the chart pen's first stroke (the view eases in).")]
        [Range(0f, 5f)]
        [SerializeField] private float _inkLead = 1.4f;

        [Tooltip("Seconds of one place's pen stroke; the next place starts as it ends.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _inkSeconds = 0.6f;

        [Tooltip("Drive input (stick or keys) that leaves the map and rides back down.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _leaveDeadzone = 0.35f;

        public float VisitMargin => _visitMargin;

        public float HomeRadius => _homeRadius;

        public float FriendRadius => _friendRadius;

        public float FriendMergeDistance => _friendMergeDistance;

        public float PollSeconds => _pollSeconds;

        public float LookoutEyeHeight => _lookoutEyeHeight;

        public float LightHeight => _lightHeight;

        public float SightStep => _sightStep;

        public float SightClearance => _sightClearance;

        public float SweepDelay => _sweepDelay;

        public float SweepInterval => _sweepInterval;

        public Vector2 BlinkRadius => _blinkRadius;

        public float BlinkSeconds => _blinkSeconds;

        public float BlinkIntensity => _blinkIntensity;

        public int BlinkPool => _blinkPool;

        public float BreatheSeconds => _breatheSeconds;

        public float HatchLookAngle => _hatchLookAngle;

        public float HatchInset => _hatchInset;

        public float HatchHeight => _hatchHeight;

        public float InkLead => _inkLead;

        public float InkSeconds => _inkSeconds;

        public float LeaveDeadzone => _leaveDeadzone;
    }
}
