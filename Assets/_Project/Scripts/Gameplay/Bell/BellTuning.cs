using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How Bell moves (docs/features/M3-05): her repair beat, her solid body, her two-step and waddle, her life at the
    /// base (swaying, foot taps, watching 07 park, dozing and waking), her dial and its prompt, her signal pillar and
    /// the cassette shelf at her corner. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class BellTuning : ScriptableObject
    {
        [Header("Repair beat")]
        [Tooltip("Seconds the tape glides along 07's beam into her tape slot.")]
        [Range(0.2f, 5f)] [SerializeField] private float _tapeSlide = 1.4f;

        [Tooltip("Height (m) of the tape's arc on its way in.")]
        [Range(0f, 2f)] [SerializeField] private float _tapeArc = 0.35f;

        [Tooltip("Scale of the tape as it slips into her slot.")]
        [Range(0.05f, 1f)] [SerializeField] private float _tapeArrivalScale = 0.7f;

        [Tooltip("Seconds her dial lamp flickers before it holds its glow.")]
        [Range(0.1f, 4f)] [SerializeField] private float _dialWarm = 1.1f;

        [Tooltip("Seconds the needle sweeps across the band and back to Lumen After Dark.")]
        [Range(0.2f, 5f)] [SerializeField] private float _needleSweep = 1.6f;

        [Tooltip("Seconds she takes to stand up on her four legs.")]
        [Range(0.2f, 6f)] [SerializeField] private float _standUp = 2.2f;

        [Tooltip("Wobble (degrees) while she stands up, settling as she finds her feet.")]
        [Range(0f, 20f)] [SerializeField] private float _standWobble = 5f;

        [Header("Lying broken against the wall")]
        [Tooltip("How far (m) her broken pose reaches back behind her root (art: 1.03, the open lid).")]
        [Range(0f, 3f)] [SerializeField] private float _brokenBackReach = 1.03f;

        [Tooltip("Height (m) of that farthest-back point, where the wall behind her is felt for.")]
        [Range(0f, 3f)] [SerializeField] private float _wallProbeHeight = 1f;

        [Tooltip("The wall is felt for from this many metres in front of her placed spot, out to twice as far behind.")]
        [Range(0.5f, 10f)] [SerializeField] private float _wallProbe = 3f;

        [Tooltip("Gap (m) left between her back and the wall's surface.")]
        [Range(0f, 0.5f)] [SerializeField] private float _wallGap = 0.05f;

        [Header("Body")]
        [Tooltip("Room (m) her collider keeps around her cabinet on every side, so 07's nose (which reaches past its " +
                 "physics sphere) stops softly just short of her, broken or home.")]
        [Range(0f, 0.6f)] [SerializeField] private float _bodyPadding = 0.25f;

        [Header("Two-step (after her repair)")]
        [Tooltip("Seconds of her little dance before she sets off home.")]
        [Range(0.5f, 10f)] [SerializeField] private float _danceDuration = 3.2f;

        [Tooltip("Steps per second of the two-step.")]
        [Range(0.2f, 4f)] [SerializeField] private float _danceRate = 1.5f;

        [Tooltip("Body sway (degrees) of the two-step.")]
        [Range(0f, 15f)] [SerializeField] private float _danceRoll = 6f;

        [Tooltip("Leg lift (degrees) of each step.")]
        [Range(0f, 40f)] [SerializeField] private float _danceStep = 16f;

        [Tooltip("Hop (m) on each step.")]
        [Range(0f, 0.3f)] [SerializeField] private float _danceHop = 0.05f;

        [Tooltip("Motor effort (Core RotorSpeed scale) while dancing, for her voice.")]
        [Range(0f, 1f)] [SerializeField] private float _danceMotor = 0.4f;

        [Header("Waddle")]
        [Tooltip("Waddle cycles per metre walked (one cycle: each diagonal pair steps once).")]
        [Range(0.1f, 3f)] [SerializeField] private float _waddlePerMetre = 0.7f;

        [Tooltip("Hip swing (degrees) of a stepping leg pair.")]
        [Range(0f, 40f)] [SerializeField] private float _waddleSwing = 15f;

        [Tooltip("Knee bend (degrees) of the leg pair swinging forward.")]
        [Range(0f, 60f)] [SerializeField] private float _waddleKnee = 18f;

        [Tooltip("Body roll (degrees) of the waddle.")]
        [Range(0f, 15f)] [SerializeField] private float _waddleRoll = 4f;

        [Tooltip("Body bob (m) of the waddle.")]
        [Range(0f, 0.2f)] [SerializeField] private float _waddleBob = 0.025f;

        [Tooltip("Seconds (time constant) to turn toward where she walks or looks.")]
        [Range(0.05f, 3f)] [SerializeField] private float _turnEase = 0.45f;

        [Tooltip("Knee tuck (degrees) at the top of a hop down a step.")]
        [Range(0f, 60f)] [SerializeField] private float _hopTuck = 28f;

        [Tooltip("Motor effort (Core RotorSpeed scale) while walking, for her leg taps.")]
        [Range(0f, 1f)] [SerializeField] private float _walkMotor = 0.6f;

        [Tooltip("Metres from the canyon exit's anchor, on past its step, to the basin floor she heads for first.")]
        [Range(0f, 40f)] [SerializeField] private float _belowStep = 14f;

        [Header("At home")]
        [Tooltip("Sway cycles per second while music plays.")]
        [Range(0.05f, 2f)] [SerializeField] private float _swayRate = 0.55f;

        [Tooltip("Body roll (degrees) of her sway.")]
        [Range(0f, 10f)] [SerializeField] private float _swayRoll = 3.5f;

        [Tooltip("Body bob (m) of her sway, twice per cycle.")]
        [Range(0f, 0.1f)] [SerializeField] private float _swayBob = 0.012f;

        [Tooltip("Speaker cone pulse (m) while music plays (art: about 6 mm).")]
        [Range(0f, 0.02f)] [SerializeField] private float _speakerPulse = 0.006f;

        [Tooltip("Speaker pulses per second.")]
        [Range(0.1f, 4f)] [SerializeField] private float _speakerRate = 1.2f;

        [Tooltip("Seconds between two foot taps (min, max).")]
        [SerializeField] private Vector2 _footTapInterval = new Vector2(5f, 10f);

        [Tooltip("Seconds of one foot tap.")]
        [Range(0.1f, 2f)] [SerializeField] private float _footTapDuration = 0.5f;

        [Tooltip("Front-right hip swing (degrees) of a foot tap (art: about -20).")]
        [Range(-45f, 0f)] [SerializeField] private float _footTapLeg = -20f;

        [Tooltip("Front-right knee bend (degrees) of a foot tap (art: about 30).")]
        [Range(0f, 60f)] [SerializeField] private float _footTapKnee = 30f;

        [Tooltip("Breathing bob (m) while no music plays or while she dozes.")]
        [Range(0f, 0.05f)] [SerializeField] private float _breathBob = 0.006f;

        [Tooltip("Seconds per breath.")]
        [Range(1f, 12f)] [SerializeField] private float _breathPeriod = 4.5f;

        [Tooltip("She turns her dial to watch 07 park within this many metres.")]
        [Range(2f, 40f)] [SerializeField] private float _watchRadius = 14f;

        [Tooltip("Farthest she turns (degrees) from her corner's facing to watch 07.")]
        [Range(0f, 120f)] [SerializeField] private float _watchMaxTurn = 55f;

        [Tooltip("Dial lamp glow while awake (linear emission multiplier; art: 1 reads amber, above 1.2 bleaches).")]
        [Range(0f, 1.2f)] [SerializeField] private float _dialGlow = 1f;

        [Tooltip("Seconds (time constant) for her dial lamp to change glow.")]
        [Range(0.05f, 3f)] [SerializeField] private float _dialEase = 0.4f;

        [Tooltip("Brightest flicker of her dial lamp while she lies broken (the warm light deep in the canyon).")]
        [Range(0f, 1.2f)] [SerializeField] private float _brokenFlicker = 0.27f;

        [Tooltip("Flickers per second of her broken dial lamp.")]
        [Range(0.1f, 10f)] [SerializeField] private float _brokenFlickerRate = 2.3f;

        [Header("Dozing and waking")]
        [Tooltip("Seconds 07 must sit still near home before she dozes off.")]
        [Range(5f, 600f)] [SerializeField] private float _dozeAfter = 90f;

        [Tooltip("07 counts as sitting still below this speed (m/s).")]
        [Range(0.01f, 2f)] [SerializeField] private float _stillSpeed = 0.3f;

        [Tooltip("Dial glow while she dozes: a low ember, never dark.")]
        [Range(0.02f, 0.6f)] [SerializeField] private float _emberGlow = 0.05f;

        [Tooltip("Seconds (time constant) for her dial to dim to an ember as she dozes off.")]
        [Range(0.1f, 6f)] [SerializeField] private float _dozeEase = 2.5f;

        [Tooltip("How much lower (m) she settles while dozing.")]
        [Range(0f, 0.15f)] [SerializeField] private float _dozeSink = 0.035f;

        [Tooltip("Knee bend (degrees) while dozing.")]
        [Range(0f, 30f)] [SerializeField] private float _dozeKnee = 7f;

        [Tooltip("Waking: seconds for the dial to warm from ember to glow (first).")]
        [Range(0.1f, 4f)] [SerializeField] private float _wakeDial = 0.9f;

        [Tooltip("Waking: seconds for the needle to lift to its station (second).")]
        [Range(0.1f, 4f)] [SerializeField] private float _wakeNeedle = 0.9f;

        [Tooltip("Waking: seconds of the little leg stretch (last).")]
        [Range(0.1f, 4f)] [SerializeField] private float _wakeStretch = 1.2f;

        [Tooltip("Hip swing (degrees) of the stretch.")]
        [Range(0f, 40f)] [SerializeField] private float _stretchLeg = 12f;

        [Tooltip("Rise (m) of the stretch.")]
        [Range(0f, 0.2f)] [SerializeField] private float _stretchLift = 0.04f;

        [Header("Greeting and reactions")]
        [Tooltip("Seconds of her greeting (the jingle, a lid lift and a little bounce).")]
        [Range(0.5f, 6f)] [SerializeField] private float _greetDuration = 2.6f;

        [Tooltip("Bounces of the greeting.")]
        [Range(1, 6)] [SerializeField] private int _greetBounces = 2;

        [Tooltip("Bounce (m) of the greeting.")]
        [Range(0f, 0.2f)] [SerializeField] private float _greetHop = 0.06f;

        [Tooltip("Lid lift (degrees, negative opens) of the greeting's brow lift (art: -20 to -30).")]
        [Range(-45f, 0f)] [SerializeField] private float _greetLid = -25f;

        [Tooltip("Seconds of the happy station-switch crackle when a new relic reaches the shelf.")]
        [Range(0.2f, 4f)] [SerializeField] private float _crackleDuration = 1.1f;

        [Tooltip("Needle jiggle (degrees) of that crackle.")]
        [Range(0f, 40f)] [SerializeField] private float _crackleNeedle = 12f;

        [Tooltip("Needle jiggles per second of that crackle.")]
        [Range(1f, 20f)] [SerializeField] private float _crackleRate = 9f;

        [Tooltip("Extra dial glow at the crackle's peak (stays under the 1.2 bloom limit with the base glow).")]
        [Range(0f, 0.3f)] [SerializeField] private float _crackleFlash = 0.15f;

        [Header("Dial")]
        [Tooltip("Needle angle (degrees) of the Lumen After Dark detent (art tick at 0).")]
        [Range(0f, 140f)] [SerializeField] private float _lumenDetent;

        [Tooltip("Needle angle (degrees) of the Tape Deck detent (art tick at 70).")]
        [Range(0f, 140f)] [SerializeField] private float _tapeDeckDetent = 70f;

        [Tooltip("Needle angle (degrees) of the Quiet Hours detent (art tick at 140).")]
        [Range(0f, 140f)] [SerializeField] private float _quietDetent = 140f;

        [Tooltip("Seconds (time constant) of the needle easing to its detent.")]
        [Range(0.05f, 2f)] [SerializeField] private float _needleEase = 0.3f;

        [Tooltip("Needle kick (degrees) of a dial click (it settles back into the detent).")]
        [Range(0f, 20f)] [SerializeField] private float _clickKick = 7f;

        [Tooltip("Seconds of that kick.")]
        [Range(0.05f, 2f)] [SerializeField] private float _clickDuration = 0.35f;

        [Tooltip("07 parks this many metres in front of her dial to turn it (art: 2 m keeps off the tower pad).")]
        [Range(0.5f, 6f)] [SerializeField] private float _tuneFront = 2f;

        [Tooltip("Radius (m) around that spot where the dial is in reach.")]
        [Range(0.3f, 5f)] [SerializeField] private float _tuneRadius = 1.6f;

        [Tooltip("07 counts as parked below this speed (m/s).")]
        [Range(0.05f, 3f)] [SerializeField] private float _tuneMaxSpeed = 0.8f;

        [Header("Signal pillar")]
        [Tooltip("Brightness of her amber signal pillar (warm, persistent).")]
        [Range(0f, 4f)] [SerializeField] private float _pillarGlow = 0.9f;

        [Tooltip("Brightness of the breathing ring at the pillar's foot.")]
        [Range(0f, 4f)] [SerializeField] private float _pillarRingGlow = 0.5f;

        [Tooltip("Seconds the pillar takes to rise.")]
        [Range(0.1f, 10f)] [SerializeField] private float _pillarRise = 2.5f;

        [Tooltip("Seconds the pillar takes to fade once its target is found.")]
        [Range(0.1f, 10f)] [SerializeField] private float _pillarFade = 2.2f;

        [Tooltip("How deep (0..1) the pillar breathes.")]
        [Range(0f, 1f)] [SerializeField] private float _pillarBreathDepth = 0.25f;

        [Tooltip("Seconds per pillar breath.")]
        [Range(0.5f, 20f)] [SerializeField] private float _pillarBreathPeriod = 5f;

        [Header("Cassette shelf")]
        [Tooltip("Height (m) of a tape's pivot above its shelf slot (art: about 0.11).")]
        [Range(0f, 0.5f)] [SerializeField] private float _shelfLift = 0.11f;

        [Tooltip("Seconds a new tape takes to settle into its slot.")]
        [Range(0.1f, 4f)] [SerializeField] private float _shelfSettle = 0.9f;

        [Tooltip("Overshoot of that settle.")]
        [Range(0f, 4f)] [SerializeField] private float _shelfOvershoot = 1.6f;

        public float TapeSlide => _tapeSlide;
        public float TapeArc => _tapeArc;
        public float TapeArrivalScale => _tapeArrivalScale;
        public float DialWarm => _dialWarm;
        public float NeedleSweep => _needleSweep;
        public float StandUp => _standUp;
        public float StandWobble => _standWobble;
        public float BrokenBackReach => _brokenBackReach;
        public float WallProbeHeight => _wallProbeHeight;
        public float WallProbe => _wallProbe;
        public float WallGap => _wallGap;
        public float BodyPadding => _bodyPadding;
        public float DanceDuration => _danceDuration;
        public float DanceRate => _danceRate;
        public float DanceRoll => _danceRoll;
        public float DanceStep => _danceStep;
        public float DanceHop => _danceHop;
        public float DanceMotor => _danceMotor;
        public float WaddlePerMetre => _waddlePerMetre;
        public float WaddleSwing => _waddleSwing;
        public float WaddleKnee => _waddleKnee;
        public float WaddleRoll => _waddleRoll;
        public float WaddleBob => _waddleBob;
        public float TurnEase => _turnEase;
        public float HopTuck => _hopTuck;
        public float WalkMotor => _walkMotor;
        public float BelowStep => _belowStep;
        public float SwayRate => _swayRate;
        public float SwayRoll => _swayRoll;
        public float SwayBob => _swayBob;
        public float SpeakerPulse => _speakerPulse;
        public float SpeakerRate => _speakerRate;
        public Vector2 FootTapInterval => Ordered(_footTapInterval);
        public float FootTapDuration => _footTapDuration;
        public float FootTapLeg => _footTapLeg;
        public float FootTapKnee => _footTapKnee;
        public float BreathBob => _breathBob;
        public float BreathPeriod => _breathPeriod;
        public float WatchRadius => _watchRadius;
        public float WatchMaxTurn => _watchMaxTurn;
        public float DialGlow => _dialGlow;
        public float DialEase => _dialEase;
        public float BrokenFlicker => _brokenFlicker;
        public float BrokenFlickerRate => _brokenFlickerRate;
        public float DozeAfter => _dozeAfter;
        public float StillSpeed => _stillSpeed;
        public float EmberGlow => _emberGlow;
        public float DozeEase => _dozeEase;
        public float DozeSink => _dozeSink;
        public float DozeKnee => _dozeKnee;
        public float WakeDial => _wakeDial;
        public float WakeNeedle => _wakeNeedle;
        public float WakeStretch => _wakeStretch;
        public float StretchLeg => _stretchLeg;
        public float StretchLift => _stretchLift;
        public float GreetDuration => _greetDuration;
        public int GreetBounces => _greetBounces;
        public float GreetHop => _greetHop;
        public float GreetLid => _greetLid;
        public float CrackleDuration => _crackleDuration;
        public float CrackleNeedle => _crackleNeedle;
        public float CrackleRate => _crackleRate;
        public float CrackleFlash => _crackleFlash;
        public float NeedleEase => _needleEase;
        public float ClickKick => _clickKick;
        public float ClickDuration => _clickDuration;
        public float TuneFront => _tuneFront;
        public float TuneRadius => _tuneRadius;
        public float TuneMaxSpeed => _tuneMaxSpeed;
        public float PillarGlow => _pillarGlow;
        public float PillarRingGlow => _pillarRingGlow;
        public float PillarRise => _pillarRise;
        public float PillarFade => _pillarFade;
        public float PillarBreathDepth => _pillarBreathDepth;
        public float PillarBreathPeriod => _pillarBreathPeriod;
        public float ShelfLift => _shelfLift;
        public float ShelfSettle => _shelfSettle;
        public float ShelfOvershoot => _shelfOvershoot;

        /// <summary>Seconds of the whole waking: dial, then needle, then stretch.</summary>
        public float WakeDuration => _wakeDial + _wakeNeedle + _wakeStretch;

        /// <summary>The needle angle (degrees) of <paramref name="channel"/>'s detent.</summary>
        public float Detent(RadioChannel channel)
        {
            switch (channel)
            {
                case RadioChannel.TapeDeck:
                    return _tapeDeckDetent;
                case RadioChannel.QuietHours:
                    return _quietDetent;
                default:
                    return _lumenDetent;
            }
        }

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
