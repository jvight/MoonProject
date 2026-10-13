using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's belly lift and the lab's turntable (docs/features/M3-19 §4, §5): how the jammed platform hangs, how
    /// it is restored and ridden, how the turntable turns and answers the stick, and 07's look up the crew's ladder.
    /// </summary>
    [CreateAssetMenu(fileName = "LiftTuning", menuName = "MoonProject/Gameplay/Lift Tuning")]
    public sealed class LiftTuning : ScriptableObject
    {
        [Header("Jammed and restored")]
        [Tooltip("07 is home carrying something for the lab within this many metres (horizontal) of the platform: " +
                 "the lift asks for the stitch.")]
        [Range(3f, 40f)]
        [SerializeField] private float _callRadius = 20f;

        [Tooltip("How far (m) out from the platform's centre, toward the dock, 07 rests to stitch the jammed cable " +
                 "(just off the platform's edge, facing it).")]
        [Range(1f, 8f)]
        [SerializeField] private float _stitchStandOff = 2.8f;

        [Tooltip("How close (m) to that spot 07 must rest to stitch.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _stitchRadius = 1f;

        [Tooltip("Seconds 07 rests by the jammed platform before its beam reaches for the cable.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _stitchDelay = 1f;

        [Tooltip("Seconds of 07 stitching the cable while the platform eases level.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _stitchSeconds = 3.5f;

        [Tooltip("Glow of the platform's pad ring while the lift asks for the stitch or waits for a ride (emission " +
                 "multiplier).")]
        [Range(0f, 8f)]
        [SerializeField] private float _padGlow = 2f;

        [Tooltip("Seconds (time constant) for the pad ring to light or dim.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _padEase = 0.5f;

        [Header("Ride")]
        [Tooltip("How close (m) to the platform's centre 07 must be to count as on the platform.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _platformRadius = 1f;

        [Tooltip("Seconds 07 rests on the restored platform (and at least this long after the stitch) before it " +
                 "rises by itself.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _boardDelay = 1f;

        [Tooltip("Seconds the platform takes between the stops, eased at both ends (Rover's crane path matches it).")]
        [Range(1f, 20f)]
        [SerializeField] private float _rideSeconds = 4f;

        [Tooltip("Seconds the platform settles at a stop before the lab opens or 07 is let go.")]
        [Range(0f, 3f)]
        [SerializeField] private float _settleSeconds = 0.6f;

        [Tooltip("Seconds the belly hatch's leaves take to slide open or shut.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _hatchSeconds = 1.2f;

        [Tooltip("Where a relic towed onto the platform rides beside 07, in the platform's space (m from the middle " +
                 "of its top: X to the side, Y up, Z ahead).")]
        [SerializeField] private Vector3 _riderOffset = new Vector3(1.15f, 0.7f, 0f);

        [Tooltip("Seconds a towed relic takes to float to its place beside 07 as the ride starts.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _riderBoardSeconds = 1f;

        [Header("Inside: the turntable")]
        [Tooltip("Seconds the turntable takes for a quarter turn, eased at both ends.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _turnSecondsPerQuarter = 1.1f;

        [Tooltip("Seconds even the smallest turn takes.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _turnMinSeconds = 0.6f;

        [Tooltip("Stick push (0..1) that counts as a command: turn, look or down.")]
        [Range(0.2f, 0.95f)]
        [SerializeField] private float _commandPress = 0.6f;

        [Tooltip("The stick must come back under this push before the next command.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _commandRelease = 0.3f;

        [Tooltip("Seconds after the turntable comes to rest before it turns by itself to a station that wants what " +
                 "07 carries.")]
        [Range(0f, 3f)]
        [SerializeField] private float _autoTurnDelay = 0.6f;

        [Tooltip("Seconds 07 rests facing the survey chart before the map opens by itself, when it has news.")]
        [Range(0f, 6f)]
        [SerializeField] private float _chartBreath = 1.5f;

        [Header("The ladder look")]
        [Tooltip("Seconds 07 looks up the crew's ladder the first time it comes near.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _ladderLookSeconds = 2.5f;

        [Tooltip("How high (m above the dock, at its foot) 07 looks up the ladder, toward the roof hatch it leads to.")]
        [Range(1f, 12f)]
        [SerializeField] private float _ladderLookHeight = 6f;

        [Tooltip("Seconds 07 then looks down at the belly lift.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _liftLookSeconds = 2f;

        [Tooltip("Gaze priority of the ladder look and of 07's gaze in the lab (above 07's idle glances).")]
        [Range(0, 20)]
        [SerializeField] private int _gazePriority = 6;

        public float CallRadius => _callRadius;

        public float StitchStandOff => _stitchStandOff;

        public float StitchRadius => _stitchRadius;

        public float StitchDelay => _stitchDelay;

        public float StitchSeconds => _stitchSeconds;

        public float PadGlow => _padGlow;

        public float PadEase => _padEase;

        public float PlatformRadius => _platformRadius;

        public float BoardDelay => _boardDelay;

        public float RideSeconds => _rideSeconds;

        public float SettleSeconds => _settleSeconds;

        public float HatchSeconds => _hatchSeconds;

        public Vector3 RiderOffset => _riderOffset;

        public float RiderBoardSeconds => _riderBoardSeconds;

        public float TurnSecondsPerQuarter => _turnSecondsPerQuarter;

        public float TurnMinSeconds => _turnMinSeconds;

        public float CommandPress => _commandPress;

        public float CommandRelease => _commandRelease;

        public float AutoTurnDelay => _autoTurnDelay;

        public float ChartBreath => _chartBreath;

        public float LadderLookSeconds => _ladderLookSeconds;

        public float LadderLookHeight => _ladderLookHeight;

        public float LiftLookSeconds => _liftLookSeconds;

        public int GazePriority => _gazePriority;
    }
}
