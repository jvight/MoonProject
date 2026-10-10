using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's lift (docs/features/M3-16 step 8): what the winch costs, how restoring and riding it are timed,
    /// where 07 rests to feed and to ride, and 07's look up the crew's ladder.
    /// </summary>
    [CreateAssetMenu(fileName = "LiftTuning", menuName = "MoonProject/Gameplay/Lift Tuning")]
    public sealed class LiftTuning : ScriptableObject
    {
        [Header("Restore")]
        [Tooltip("Metal the winch hopper takes.")]
        [Range(0, 20)]
        [SerializeField] private int _feedMetal = 2;

        [Tooltip("Wiring the winch hopper takes.")]
        [Range(0, 20)]
        [SerializeField] private int _feedWiring = 2;

        [Tooltip("Seconds 07 rests on the feed pad before its beam reaches for the hopper.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _feedDelay = 1f;

        [Tooltip("How far (m) in front of the winch's hopper mouth the feed pad lies.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _feedPadDistance = 2.4f;

        [Tooltip("Seconds of 07 stitching the winch while the platform eases level and down.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _stitchSeconds = 3.5f;

        [Tooltip("Winch lamp glow once restored (emission multiplier).")]
        [Range(0f, 8f)]
        [SerializeField] private float _lampGlow = 2.5f;

        [Header("Ride")]
        [Tooltip("How close (m) to the platform's rover spot 07 must be to count as on the platform.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _platformRadius = 1f;

        [Tooltip("Seconds 07 rests on the platform before it rises.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _boardDelay = 1.5f;

        [Tooltip("Seconds the platform takes between the stops, eased at both ends.")]
        [Range(1f, 20f)]
        [SerializeField] private float _rideSeconds = 6f;

        [Tooltip("Seconds the platform settles at a stop before the lookout opens or 07 is let go.")]
        [Range(0f, 3f)]
        [SerializeField] private float _settleSeconds = 0.8f;

        [Header("The ladder look")]
        [Tooltip("Seconds 07 looks up the crew's ladder the first time it comes near.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _ladderLookSeconds = 2.5f;

        [Tooltip("Seconds 07 then looks at the lift.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _liftLookSeconds = 2f;

        [Tooltip("Gaze priority of the ladder look (above 07's idle glances).")]
        [Range(0, 20)]
        [SerializeField] private int _gazePriority = 6;

        public Recipe FeedRecipe => new Recipe(_feedMetal, _feedWiring, 0);

        public float FeedDelay => _feedDelay;

        public float FeedPadDistance => _feedPadDistance;

        public float StitchSeconds => _stitchSeconds;

        public float LampGlow => _lampGlow;

        public float PlatformRadius => _platformRadius;

        public float BoardDelay => _boardDelay;

        public float RideSeconds => _rideSeconds;

        public float SettleSeconds => _settleSeconds;

        public float LadderLookSeconds => _ladderLookSeconds;

        public float LiftLookSeconds => _liftLookSeconds;

        public int GazePriority => _gazePriority;
    }
}
