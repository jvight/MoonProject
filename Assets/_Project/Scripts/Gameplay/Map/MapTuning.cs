using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07): when a place counts as seen or visited, and the chart pen's pace.
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

        [Header("The chart pen")]
        [Tooltip("Seconds from the view opening on the map to the chart pen's first stroke (the view eases in).")]
        [Range(0f, 5f)]
        [SerializeField] private float _inkLead = 1.4f;

        [Tooltip("Seconds of one place's pen stroke; the next place starts as it ends.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _inkSeconds = 0.6f;

        public float VisitMargin => _visitMargin;

        public float HomeRadius => _homeRadius;

        public float FriendRadius => _friendRadius;

        public float FriendMergeDistance => _friendMergeDistance;

        public float PollSeconds => _pollSeconds;

        public float InkLead => _inkLead;

        public float InkSeconds => _inkSeconds;
    }
}
