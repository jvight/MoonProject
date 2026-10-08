using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The radio tower: its pad (the diegetic shop), 07 feeding its service port's hopper, the hatch and the stitching
    /// beam, the beacon, how a new stage grows in, and the warm bloom that rolls out to the new clear-signal radius.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RadioTowerTuning : ScriptableObject
    {
        [Header("Pad")]
        [Tooltip("Pad centre relative to the lander's tower anchor, in lander space (m): in front of the tower's " +
                 "service port, its ring clear of the port's plate (which reaches 1.4 m out from the tower).")]
        [SerializeField] private Vector3 _padOffset = new Vector3(0f, 0f, 4.1f);

        [Tooltip("07 counts as parked on the pad within this many metres of its centre.")]
        [Range(0.5f, 6f)] [SerializeField] private float _padRadius = 2.4f;

        [Tooltip("Width (m) of the pad's ring of light.")]
        [Range(0.05f, 2f)] [SerializeField] private float _padRingWidth = 0.35f;

        [Tooltip("Pad brightness with nothing to buy yet (it breathes softly).")]
        [Range(0f, 3f)] [SerializeField] private float _padIdle = 0.22f;

        [Tooltip("Pad brightness when the next level is affordable: an invitation.")]
        [Range(0f, 3f)] [SerializeField] private float _padInviting = 0.65f;

        [Tooltip("Pad brightness while 07 is parked on it.")]
        [Range(0f, 3f)] [SerializeField] private float _padOccupied = 1f;

        [Tooltip("Pad brightness once the tower is fully upgraded.")]
        [Range(0f, 3f)] [SerializeField] private float _padDone = 0.12f;

        [Tooltip("Seconds per breath of the pad and the beacon.")]
        [Range(0.5f, 10f)] [SerializeField] private float _breathPeriod = 3.2f;

        [Tooltip("How deeply the waiting pad breathes (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _padBreathDepth = 0.35f;

        [Tooltip("Seconds (time constant) for the pad to brighten or dim.")]
        [Range(0f, 3f)] [SerializeField] private float _padEase = 0.4f;

        [Tooltip("Segments of the pad ring.")]
        [Range(8, 128)] [SerializeField] private int _padSegments = 48;

        [Header("Service port")]
        [Tooltip("Seconds 07's beam reaches the port's hopper before the first bundle of materials leaves 07.")]
        [Range(0f, 1f)] [SerializeField] private float _feedBeamLead = 0.15f;

        [Tooltip("Seconds each bundle takes from 07's cargo socket into the hopper's mouth.")]
        [Range(0.1f, 2f)] [SerializeField] private float _feedFlight = 0.55f;

        [Tooltip("Seconds between one bundle leaving 07 and the next (one bundle per material the recipe spends).")]
        [Range(0f, 1f)] [SerializeField] private float _feedStagger = 0.15f;

        [Tooltip("Metres a bundle's flight bows up above the straight line.")]
        [Range(0f, 2f)] [SerializeField] private float _feedLift = 0.45f;

        [Tooltip("Share of a flight a bundle spends popping out of 07, and again shrinking into the mouth.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float _feedGrowShare = 0.25f;

        [Tooltip("Degrees the service hatch swings open (on its left hinge) once the hopper is fed.")]
        [Range(10f, 160f)] [SerializeField] private float _hatchAngle = 70f;

        [Tooltip("Seconds the hatch takes to swing open, and again to swing shut after the stitching.")]
        [Range(0.05f, 2f)] [SerializeField] private float _hatchTime = 0.45f;

        [Tooltip("Stitches per second of 07's beam while it stitches up the tower.")]
        [Range(0.2f, 10f)] [SerializeField] private float _stitchRate = 2.5f;

        [Tooltip("Half-width (m) of the stitching across the tower.")]
        [Range(0.02f, 2f)] [SerializeField] private float _stitchSpread = 0.2f;

        [Header("Beacon and lights")]
        [Tooltip("Colour of the beacon's light.")]
        [SerializeField] private Color _beaconColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Radius (m) of the beacon's glow ball.")]
        [Range(0.05f, 2f)] [SerializeField] private float _beaconRadius = 0.35f;

        [Tooltip("Beacon brightness at level 1; each further level adds the step below.")]
        [Range(0f, 4f)] [SerializeField] private float _beaconGlow = 0.8f;

        [Tooltip("Extra beacon brightness per level above 1.")]
        [Range(0f, 2f)] [SerializeField] private float _beaconGlowStep = 0.25f;

        [Tooltip("How deeply the beacon breathes (0 = steady).")]
        [Range(0f, 1f)] [SerializeField] private float _beaconBreathDepth = 0.15f;

        [Tooltip("Range (m) of the beacon's point light.")]
        [Range(1f, 40f)] [SerializeField] private float _beaconLightRange = 9f;

        [Tooltip("Intensity of the beacon's point light at level 1 (it grows with the glow).")]
        [Range(0f, 10f)] [SerializeField] private float _beaconLightIntensity = 1.5f;

        [Tooltip("Tower light strips while the mast is unpowered (level 0; linear emission multiplier).")]
        [Range(0f, 1f)] [SerializeField] private float _unpoweredGlow = 0.02f;

        [Tooltip("Tower light strips once powered (level 1 and up; linear emission multiplier, 1 = as authored). " +
                 "The upgrade flare adds up to 1 on top.")]
        [Range(0f, 4f)] [SerializeField] private float _poweredGlow = 1f;

        [Header("Upgrade moment")]
        [Tooltip("Seconds the beacon flares before the new stage appears (07's beam is stitching by then).")]
        [Range(0.1f, 3f)] [SerializeField] private float _flareDuration = 0.7f;

        [Tooltip("Peak extra beacon brightness of the flare.")]
        [Range(0f, 10f)] [SerializeField] private float _flareGlow = 2.2f;

        [Tooltip("Seconds the new stage takes to grow in.")]
        [Range(0.1f, 4f)] [SerializeField] private float _growDuration = 1.3f;

        [Tooltip("Scale the new stage grows from.")]
        [Range(0.3f, 1f)] [SerializeField] private float _growFrom = 0.85f;

        [Tooltip("Overshoot of the growth (soft settle).")]
        [Range(0f, 3f)] [SerializeField] private float _growOvershoot = 1.2f;

        [Tooltip("The old stage sinks to this scale while the beacon flares.")]
        [Range(0.5f, 1f)] [SerializeField] private float _sinkTo = 0.92f;

        [Header("Signal bloom")]
        [Tooltip("Seconds the warm ring takes to roll out to the new clear-signal radius.")]
        [Range(0.5f, 15f)] [SerializeField] private float _bloomDuration = 4f;

        [Tooltip("Width (m) of the bloom ring.")]
        [Range(0.2f, 10f)] [SerializeField] private float _bloomWidth = 2.5f;

        [Tooltip("Brightness of the bloom ring as it leaves the tower.")]
        [Range(0f, 4f)] [SerializeField] private float _bloomGlow = 0.7f;

        [Tooltip("How the bloom fades as it spreads: brightness = (1 - progress) ^ this.")]
        [Range(0.5f, 4f)] [SerializeField] private float _bloomFadeCurve = 1.5f;

        [Tooltip("Segments of the bloom ring.")]
        [Range(16, 512)] [SerializeField] private int _bloomSegments = 160;

        public Vector3 PadOffset => _padOffset;
        public float PadRadius => _padRadius;
        public float PadOccupied => _padOccupied;
        public float BreathPeriod => _breathPeriod;
        public float BeaconBreathDepth => _beaconBreathDepth;
        public float BloomFadeCurve => _bloomFadeCurve;
        public Color BeaconColor => _beaconColor;
        public float BeaconRadius => _beaconRadius;
        public float BeaconGlow => _beaconGlow;
        public float BeaconGlowStep => _beaconGlowStep;
        public float BeaconLightRange => _beaconLightRange;
        public float BeaconLightIntensity => _beaconLightIntensity;
        public float UnpoweredGlow => _unpoweredGlow;
        public float PoweredGlow => _poweredGlow;
        public float FlareDuration => _flareDuration;
        public float FlareGlow => _flareGlow;
        public float GrowDuration => _growDuration;
        public float GrowFrom => _growFrom;
        public float GrowOvershoot => _growOvershoot;
        public float SinkTo => _sinkTo;
        public float HatchAngle => _hatchAngle;
        public float HatchTime => _hatchTime;
        public float StitchRate => _stitchRate;
        public float StitchSpread => _stitchSpread;
        public float BloomDuration => _bloomDuration;
        public float BloomWidth => _bloomWidth;
        public float BloomGlow => _bloomGlow;
        public int BloomSegments => _bloomSegments;

        public FeedLook FeedLook => new FeedLook(_feedBeamLead, _feedFlight, _feedStagger, _feedLift, _feedGrowShare);

        public PadLook PadLook => new PadLook(_padRadius, _padRingWidth, _padSegments, _padIdle, _padInviting,
            _padOccupied, _padDone, _breathPeriod, _padBreathDepth, _padEase);

        /// <summary>Beacon brightness at <paramref name="level"/> (dark before the first purchase).</summary>
        public float BeaconGlowAt(int level)
        {
            return level <= 0 ? 0f : _beaconGlow + (level - 1) * _beaconGlowStep;
        }
    }
}
