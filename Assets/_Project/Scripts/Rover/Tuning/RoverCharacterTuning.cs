using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's body language: where the head looks, how the lid droops and blinks, how the eye breathes, when the tired
    /// solar wing sighs open, and the small perk-up / "oof" reactions. Everything is slow and subtle on purpose:
    /// gently melancholic machinery, never cartoony (docs/VISION.md, "The rover - 07").
    /// </summary>
    [CreateAssetMenu(fileName = "RoverCharacterTuning", menuName = "MoonProject/Rover/Rover Character Tuning")]
    public sealed class RoverCharacterTuning : ScriptableObject
    {
        [Header("Gaze")]
        [Tooltip("How far (deg) the neck can turn either way.")]
        [Range(0f, 170f)]
        [SerializeField] private float _neckYawLimit = 110f;

        [Tooltip("How far (deg) the head can tilt up (Earth is high in the sky).")]
        [Range(0f, 89f)]
        [SerializeField] private float _headPitchUpLimit = 55f;

        [Tooltip("How far (deg) the head can tilt down.")]
        [Range(0f, 89f)]
        [SerializeField] private float _headPitchDownLimit = 25f;

        [Tooltip("While driving, the head turns into the steer by this much (deg at full steering).")]
        [Range(0f, 60f)]
        [SerializeField] private float _lookIntoTurn = 22f;

        [Tooltip("While driving, the head looks slightly down at the road (deg, negative = down).")]
        [Range(-30f, 30f)]
        [SerializeField] private float _travelHeadPitch = -6f;

        [Tooltip("Spring frequency (Hz) of the head following the travel direction: a gentle, lagging look.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _travelGazeFrequency = 0.7f;

        [Tooltip("Spring frequency (Hz) of the head turning toward something it interacts with: attentive.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _targetGazeFrequency = 1.1f;

        [Tooltip("Spring frequency (Hz) of the head slowly turning up toward Earth when left alone.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _idleGazeFrequency = 0.22f;

        [Tooltip("Damping ratio of every head movement (just under 1: arrives softly without wobbling).")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _gazeDamping = 0.92f;

        [Header("Idle (left alone)")]
        [Tooltip("Below this speed (m/s), with no drive input, 07 counts as standing still.")]
        [Range(0f, 2f)]
        [SerializeField] private float _stillSpeed = 0.25f;

        [Tooltip("Drive input (0..1) above this counts as the player doing something.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _activityInput = 0.1f;

        [Tooltip("Seconds standing still before 07 starts to daydream (looks up at Earth, lid droops, wing sighs).")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _idleDelay = 4f;

        [Tooltip("Half-life (s) of drifting into the daydream.")]
        [Range(0.05f, 10f)]
        [SerializeField] private float _idleRiseHalfLife = 1.4f;

        [Tooltip("Half-life (s) of snapping out of it when driving resumes.")]
        [Range(0.02f, 3f)]
        [SerializeField] private float _idleFallHalfLife = 0.25f;

        [Tooltip("Driving off while at least this deep in the daydream (0..1) makes 07 perk up.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wakeThreshold = 0.5f;

        [Tooltip("While daydreaming the head rises and sinks with each breath by this much (deg).")]
        [Range(0f, 10f)]
        [SerializeField] private float _idleHeadBreath = 1.2f;

        [Header("Eyelid")]
        [Tooltip("Eyelid rotation (deg, + local X) when fully closed.")]
        [Range(10f, 150f)]
        [SerializeField] private float _eyelidClosedAngle = 75f;

        [Tooltip("Lid closure (0 open .. 1 closed) while active. Half-lidded is 07's resting face.")]
        [Range(0f, 1f)]
        [SerializeField] private float _activeLid = 0.32f;

        [Tooltip("Lid closure while daydreaming.")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleLid = 0.62f;

        [Tooltip("How much a full perk-up opens the lid.")]
        [Range(0f, 1f)]
        [SerializeField] private float _perkWiden = 0.3f;

        [Tooltip("How much a full 'oof' squints the lid.")]
        [Range(0f, 1f)]
        [SerializeField] private float _oofSquint = 0.45f;

        [Tooltip("Shortest gap (s) between blinks.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _blinkMinInterval = 4f;

        [Tooltip("Longest gap (s) between blinks.")]
        [Range(0.5f, 60f)]
        [SerializeField] private float _blinkMaxInterval = 10f;

        [Tooltip("Duration (s) of one slow blink, closing and opening.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _blinkDuration = 0.45f;

        [Header("Eye glow")]
        [Tooltip("Seconds per breath of the eye glow.")]
        [Range(1f, 15f)]
        [SerializeField] private float _breathPeriod = 5.5f;

        [Tooltip("Glow swing (fraction of authored brightness) of each breath while active.")]
        [Range(0f, 1f)]
        [SerializeField] private float _activeBreathDepth = 0.06f;

        [Tooltip("Glow swing of each breath while daydreaming.")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleBreathDepth = 0.28f;

        [Tooltip("How much dimmer (fraction) the eye rests while daydreaming.")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleGlowDim = 0.15f;

        [Tooltip("Extra brightness (fraction) at the peak of a full perk-up.")]
        [Range(0f, 3f)]
        [SerializeField] private float _perkGlowBoost = 0.7f;

        [Tooltip("How much a closed lid dims the eye (fraction at fully closed).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lidGlowDim = 0.5f;

        [Tooltip("Intensity of the small warm light inside the eye at authored brightness.")]
        [Range(0f, 10f)]
        [SerializeField] private float _eyeLightIntensity = 0.8f;

        [Tooltip("Range (m) of the small warm light inside the eye. Small: it only warms the hood and nose.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _eyeLightRange = 1.6f;

        [Header("Solar wing")]
        [Tooltip("Wing rotation (deg, + local X) when fully open.")]
        [Range(10f, 180f)]
        [SerializeField] private float _wingOpenAngle = 110f;

        [Tooltip("How far (0..1) the wing settles open while daydreaming.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wingIdleOpen = 0.16f;

        [Tooltip("Spring frequency (Hz) of the wing. Slow: a sigh, not a flap.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _wingFrequency = 0.3f;

        [Tooltip("Damping ratio of the wing (under 1: it eases past its target a touch and settles).")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _wingDamping = 0.6f;

        [Tooltip("Extra wing opening (0..1) at the height of a full sigh.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wingSighAmount = 0.14f;

        [Header("Sigh (drifting into a daydream, a snapped tether)")]
        [Tooltip("Spring frequency (Hz) of a sigh swell: slow in, slower out.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _sighFrequency = 0.2f;

        [Tooltip("Head droop (deg) at the height of a full sigh.")]
        [Range(0f, 30f)]
        [SerializeField] private float _sighHeadDrop = 7f;

        [Tooltip("Extra lid closure (0..1) at the height of a full sigh.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sighLidDroop = 0.2f;

        [Tooltip("Sigh strength (0..1) when 07 drifts into a daydream.")]
        [Range(0f, 1f)]
        [SerializeField] private float _daydreamSigh = 1f;

        [Tooltip("Sigh strength (0..1) when the tether snaps (gentle disappointment, never frustration).")]
        [Range(0f, 1f)]
        [SerializeField] private float _snapSigh = 0.8f;

        [Header("Gameplay reactions")]
        [Tooltip("Perk-up strength for a single scrap pickup.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scrapPerk = 0.18f;

        [Tooltip("Extra perk-up strength per combo step, so chained pickups feel happier.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _scrapComboPerk = 0.07f;

        [Tooltip("Strongest perk-up a scrap chain can give.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scrapPerkMax = 0.6f;

        [Tooltip("Perk-up strength when a buried relic answers the sonar.")]
        [Range(0f, 1f)]
        [SerializeField] private float _relicAnsweredPerk = 0.5f;

        [Tooltip("Perk-up strength when a relic finishes surfacing: delight.")]
        [Range(0f, 1f)]
        [SerializeField] private float _relicSurfacedPerk = 1f;

        [Tooltip("Seconds 07 keeps glancing at an answering or surfacing relic on its own.")]
        [Range(0f, 10f)]
        [SerializeField] private float _relicGlanceSeconds = 2.5f;

        [Tooltip("Gaze priority of those glances; gameplay requests at this priority or above win (ties: most "
            + "recent).")]
        [SerializeField] private int _reactionGazePriority = -1;

        [Tooltip("Nod strength (0..1) when a relic is placed on a museum shelf.")]
        [Range(0f, 1f)]
        [SerializeField] private float _depositNod = 1f;

        [Tooltip("Spring frequency (Hz) of the contented nod.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _nodFrequency = 0.9f;

        [Tooltip("Head dip (deg) at the bottom of a full nod.")]
        [Range(0f, 30f)]
        [SerializeField] private float _nodDepth = 9f;

        [Header("Perk-up")]
        [Tooltip("Spring frequency (Hz) of the perk-up swell: peaks after ~1/(2*pi*f) s and fades softly.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _perkFrequency = 0.65f;

        [Tooltip("Head lift (deg) at the peak of a full perk-up.")]
        [Range(0f, 30f)]
        [SerializeField] private float _perkHeadLift = 8f;

        [Tooltip("Antenna wiggle (deg/s kick) of a full perk-up.")]
        [Range(0f, 600f)]
        [SerializeField] private float _perkAntennaKick = 140f;

        [Tooltip("Perk-up strength when driving off from a daydream.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wakePerk = 0.8f;

        [Tooltip("Perk-up strength after a soft, floaty landing (a little joy).")]
        [Range(0f, 1f)]
        [SerializeField] private float _softLandingPerk = 0.45f;

        [Header("Oof (hard landing)")]
        [Tooltip("Impact speed (m/s) from which a landing gets an 'oof' instead of a perk-up.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _oofImpactSpeed = 3.2f;

        [Tooltip("Impact speed (m/s) of the strongest 'oof'.")]
        [Range(1f, 15f)]
        [SerializeField] private float _oofFullImpact = 5f;

        [Tooltip("Strength (0..1) of the gentlest 'oof', right at the hard-landing threshold.")]
        [Range(0f, 1f)]
        [SerializeField] private float _oofMinStrength = 0.5f;

        [Tooltip("Spring frequency (Hz) of the 'oof' swell (quicker than a perk-up).")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _oofFrequency = 1.1f;

        [Tooltip("Head dip (deg) at the peak of a full 'oof'.")]
        [Range(0f, 30f)]
        [SerializeField] private float _oofHeadDip = 11f;

        [Tooltip("Extra chassis squash (downward m/s kick) of a full 'oof'.")]
        [Range(0f, 3f)]
        [SerializeField] private float _oofSquashKick = 0.6f;

        [Header("Antenna tip")]
        [Tooltip("Seconds between blinks of the antenna tip.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _tipBlinkPeriod = 2.8f;

        [Tooltip("Seconds the antenna tip glow takes to swell and fade in each blink.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _tipBlinkDuration = 0.6f;

        [Tooltip("Antenna tip brightness between blinks (fraction of authored).")]
        [Range(0f, 1f)]
        [SerializeField] private float _tipRestGlow = 0.15f;

        public float NeckYawLimit => _neckYawLimit;

        public float HeadPitchUpLimit => _headPitchUpLimit;

        public float HeadPitchDownLimit => _headPitchDownLimit;

        public float LookIntoTurn => _lookIntoTurn;

        public float TravelHeadPitch => _travelHeadPitch;

        public float TravelGazeFrequency => _travelGazeFrequency;

        public float TargetGazeFrequency => _targetGazeFrequency;

        public float IdleGazeFrequency => _idleGazeFrequency;

        public float GazeDamping => _gazeDamping;

        public float StillSpeed => _stillSpeed;

        public float ActivityInput => _activityInput;

        public float IdleDelay => _idleDelay;

        public float IdleRiseHalfLife => _idleRiseHalfLife;

        public float IdleFallHalfLife => _idleFallHalfLife;

        public float WakeThreshold => _wakeThreshold;

        public float IdleHeadBreath => _idleHeadBreath;

        public float EyelidClosedAngle => _eyelidClosedAngle;

        public float ActiveLid => _activeLid;

        public float IdleLid => _idleLid;

        public float PerkWiden => _perkWiden;

        public float OofSquint => _oofSquint;

        public float BlinkMinInterval => _blinkMinInterval;

        public float BlinkMaxInterval => Mathf.Max(_blinkMinInterval, _blinkMaxInterval);

        public float BlinkDuration => _blinkDuration;

        public float BreathPeriod => _breathPeriod;

        public float ActiveBreathDepth => _activeBreathDepth;

        public float IdleBreathDepth => _idleBreathDepth;

        public float IdleGlowDim => _idleGlowDim;

        public float PerkGlowBoost => _perkGlowBoost;

        public float LidGlowDim => _lidGlowDim;

        public float EyeLightIntensity => _eyeLightIntensity;

        public float EyeLightRange => _eyeLightRange;

        public float WingOpenAngle => _wingOpenAngle;

        public float WingIdleOpen => _wingIdleOpen;

        public float WingFrequency => _wingFrequency;

        public float WingDamping => _wingDamping;

        public float WingSighAmount => _wingSighAmount;

        public float SighFrequency => _sighFrequency;

        public float SighHeadDrop => _sighHeadDrop;

        public float SighLidDroop => _sighLidDroop;

        public float DaydreamSigh => _daydreamSigh;

        public float SnapSigh => _snapSigh;

        public float ScrapPerk => _scrapPerk;

        public float ScrapComboPerk => _scrapComboPerk;

        public float ScrapPerkMax => _scrapPerkMax;

        public float RelicAnsweredPerk => _relicAnsweredPerk;

        public float RelicSurfacedPerk => _relicSurfacedPerk;

        public float RelicGlanceSeconds => _relicGlanceSeconds;

        public int ReactionGazePriority => _reactionGazePriority;

        public float DepositNod => _depositNod;

        public float NodFrequency => _nodFrequency;

        public float NodDepth => _nodDepth;

        public float PerkFrequency => _perkFrequency;

        public float PerkHeadLift => _perkHeadLift;

        public float PerkAntennaKick => _perkAntennaKick;

        public float WakePerk => _wakePerk;

        public float SoftLandingPerk => _softLandingPerk;

        public float OofImpactSpeed => _oofImpactSpeed;

        public float OofFullImpact => Mathf.Max(_oofImpactSpeed + 0.01f, _oofFullImpact);

        public float OofMinStrength => _oofMinStrength;

        public float OofFrequency => _oofFrequency;

        public float OofHeadDip => _oofHeadDip;

        public float OofSquashKick => _oofSquashKick;

        public float TipBlinkPeriod => _tipBlinkPeriod;

        public float TipBlinkDuration => Mathf.Min(_tipBlinkDuration, _tipBlinkPeriod);

        public float TipRestGlow => _tipRestGlow;
    }
}
