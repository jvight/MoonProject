using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>The rover touched down after being airborne. Drives landing audio, dust puffs and camera bump.</summary>
    public readonly struct RoverLanded
    {
        public RoverLanded(Vector3 position, float impactSpeed, float airTime)
        {
            Position = position;
            ImpactSpeed = impactSpeed;
            AirTime = airTime;
        }

        public Vector3 Position { get; }

        /// <summary>Downward speed at touchdown in m/s (always positive).</summary>
        public float ImpactSpeed { get; }

        /// <summary>Seconds spent airborne before this landing.</summary>
        public float AirTime { get; }
    }

    /// <summary>
    /// 07 began waking up at the start of the session (eye opening, design ruling 8): the moment the radio crackles on.
    /// </summary>
    public readonly struct RoverAwoke
    {
        public RoverAwoke(Vector3 position, bool wokenByPlayer)
        {
            Position = position;
            WokenByPlayer = wokenByPlayer;
        }

        public Vector3 Position { get; }

        /// <summary>True when the player drove before 07 woke on its own (the wake-up is quick, not slow).</summary>
        public bool WokenByPlayer { get; }
    }

    /// <summary>
    /// 07 got stuck and is being lifted gently to a nearby open spot (design ruling 7). The lift is a continuous arc
    /// from <see cref="From"/> to <see cref="To"/> lasting <see cref="Duration"/> seconds; audio or UI may soften it.
    /// </summary>
    public readonly struct RoverRecovering
    {
        public RoverRecovering(Vector3 from, Vector3 to, float duration)
        {
            From = from;
            To = to;
            Duration = duration;
        }

        public Vector3 From { get; }

        public Vector3 To { get; }

        public float Duration { get; }
    }

    /// <summary>
    /// The Hover-Jump charge grew: published as charging starts (strength 0) and at each equal step up to a full charge
    /// (1); with the default four steps that is five notes, ready for a pentatonic climb. Drives the charge hum.
    /// </summary>
    public readonly struct RoverJumpCharged
    {
        public RoverJumpCharged(float strength)
        {
            Strength = strength;
        }

        /// <summary>Charge so far, 0..1.</summary>
        public float Strength { get; }
    }

    /// <summary>07 leapt with the Hover-Jump. Its landing is announced by <see cref="RoverLanded"/> as usual.</summary>
    public readonly struct RoverJumped
    {
        public RoverJumped(float strength)
        {
            Strength = strength;
        }

        /// <summary>0 for a tap (small hop) .. 1 for a full charge (the big leap).</summary>
        public float Strength { get; }
    }

    /// <summary>
    /// A Hover-Jump charge ended without a leap: 07 left the ground, the ability went away or gameplay held it still.
    /// </summary>
    public readonly struct RoverJumpCancelled
    {
    }

    /// <summary>
    /// A leap was asked for but the battery can't pay for it (<see cref="IRoverBattery.CanLeap"/> false,
    /// docs/features/M3-20): the coils never charge, they fizzle softly at <see cref="Position"/> and 07 glances at its
    /// flank lamps. Published by Rover once per refused press.
    /// </summary>
    public readonly struct RoverLeapRefused
    {
        public RoverLeapRefused(Vector3 position)
        {
            Position = position;
        }

        /// <summary>Where 07 stood (the coils under its belly).</summary>
        public Vector3 Position { get; }
    }

    /// <summary>A piece of 07's visible kit or a friend's gift on 07 (docs/features/M3-11, VISION ruling 11).</summary>
    public enum RoverKitPiece
    {
        /// <summary>The Hover-Jump's coils under the belly.</summary>
        HoverCoils = 0,

        /// <summary>The Warm Headlamp's caged lamp bar across the front.</summary>
        LampBar = 1,

        /// <summary>The Boost Coils' twin capacitor drums on the flanks.</summary>
        CapacitorDrums = 2,

        /// <summary>The Cargo Cradle's strapped rear rack.</summary>
        CargoRack = 3,

        /// <summary>Tilly's gift: the solar wing's missing cell replaced.</summary>
        SolarCell = 4,

        /// <summary>Bell's gift: the "07" freshly stencilled and a radio pennant on the antenna.</summary>
        FreshPaint = 5,

        /// <summary>Cell Pack II's battery spine on the back (docs/features/M3-20): two more cells.</summary>
        BatterySpine = 6,

        /// <summary>
        /// The old tape deck Tilly spotted, on 07's left flank (docs/features/M3-24 C): it plays the first tape softly.
        /// </summary>
        TapeDeck = 7,

        /// <summary>
        /// Kenji's spare tether ring round the beam's emitter, fitted at his garage port (docs/design/first-hour-v2.md
        /// §3): faintly cyan at rest, bright while the tether holds.
        /// </summary>
        TetherRing = 8,

        /// <summary>The Focus Lens clipped into 07's eye (M3-26 tier 1; the Cutter socket, 0.5.5 §3.5).</summary>
        FocusLens = 9,

        /// <summary>The Side Bins, a mirrored pair low between the wheels (M3-25; the Panniers socket, 0.5.5 §3.5).
        /// </summary>
        SideBins = 10,
    }

    /// <summary>
    /// A kit piece is about to be fitted on 07 after a purchase (<see cref="Gift"/> false), or a friend's gift is about
    /// to appear as 07 comes home (true): the install moment begins and the camera eases round to look. A loaded game
    /// shows its kit silently, without this.
    /// </summary>
    public readonly struct RoverKitInstalling
    {
        public RoverKitInstalling(RoverKitPiece piece, bool gift)
        {
            Piece = piece;
            Gift = gift;
        }

        public RoverKitPiece Piece { get; }

        /// <summary>True for a friend's gift: the soft version of the moment.</summary>
        public bool Gift { get; }
    }

    /// <summary>
    /// The piece is on 07: a crafted piece set on its socket by the Rover Bay's arm (the "fitted" clunk; 07 strikes its
    /// proud pose as the turntable shows it), or a friend's gift fully there.
    /// </summary>
    public readonly struct RoverKitFitted
    {
        public RoverKitFitted(RoverKitPiece piece, bool gift, string upgradeId)
        {
            Piece = piece;
            Gift = gift;
            UpgradeId = upgradeId ?? string.Empty;
        }

        public RoverKitPiece Piece { get; }

        /// <summary>True for a friend's gift.</summary>
        public bool Gift { get; }

        /// <summary>
        /// The upgrade that brought a crafted piece (UI names it by this, "upgrade.&lt;id&gt;.name"); empty for a gift.
        /// </summary>
        public string UpgradeId { get; }
    }

    /// <summary>
    /// A loaded game's piece already on 07, shown silently without its moment: published once per such piece on the
    /// kit's first tick of play, so listeners (the tape deck's music) can start in the right state.
    /// </summary>
    public readonly struct RoverKitRestored
    {
        public RoverKitRestored(RoverKitPiece piece)
        {
            Piece = piece;
        }

        public RoverKitPiece Piece { get; }
    }

    /// <summary>
    /// The Boost Coils engaged (<see cref="Boosting"/> true: holding Drive at cruise on open, flat-ish ground) or let
    /// go. The boost itself eases in and out over about a second; listeners ease their own response (the hum).
    /// </summary>
    public readonly struct RoverBoostChanged
    {
        public RoverBoostChanged(bool boosting)
        {
            Boosting = boosting;
        }

        public bool Boosting { get; }
    }

    /// <summary>
    /// 07 was set down somewhere else in one step (<see cref="IRoverPlacement.PlaceAt"/>, e.g. a radio-hop while the
    /// view is dark): the camera and anything that follows 07 snap with it instead of easing across.
    /// </summary>
    public readonly struct RoverPlaced
    {
        public RoverPlaced(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>The ground point 07 now rests on.</summary>
        public Vector3 Position { get; }

        /// <summary>07's heading (yaw only).</summary>
        public Quaternion Rotation { get; }
    }

    /// <summary>
    /// The camera began its slow drift out to the lonely wide shot after 07 rested a while (<see cref="Wide"/> true;
    /// VISION pillar 6), or handed the view back because the player drove, looked around or something began
    /// (false). 07 sighs as the frame opens; anything else that wants to breathe out with it may listen.
    /// </summary>
    public readonly struct RoverWideShotChanged
    {
        public RoverWideShotChanged(bool wide)
        {
            Wide = wide;
        }

        /// <summary>True as the frame starts to open, false as it starts to come back to the chase view.</summary>
        public bool Wide { get; }
    }

    /// <summary>
    /// The stargazing beat began or ended: 07 rests while the player looks up at the sky. UI fades the HUD and audio
    /// thins the music while it is on; any drive input ends it.
    /// </summary>
    public readonly struct StargazingChanged
    {
        public StargazingChanged(bool isStargazing)
        {
            IsStargazing = isStargazing;
        }

        public bool IsStargazing { get; }
    }

    /// <summary>
    /// 07 wants to say something in its machine chirp (docs/design/feel-pass.md §7.2). Published by Rover, whose
    /// character decides when 07 speaks and keeps the cooldowns; Audio voices it (or drops it, e.g. over a voice line)
    /// and announces each syllable with <see cref="RoverChirpSyllable"/>.
    /// </summary>
    public readonly struct RoverChirpRequested
    {
        public RoverChirpRequested(ChirpMood mood, float intensity)
        {
            Mood = mood;
            Intensity = Mathf.Clamp01(intensity);
        }

        public ChirpMood Mood { get; }

        /// <summary>0..1: how strongly 07 feels it (may lift the phrase a step).</summary>
        public float Intensity { get; }
    }

    /// <summary>
    /// Asks 07's rear cargo hatch to open and shut once, as a show of the hold (0.5.5 §3: ui's first-Tab boot). It
    /// eases open, stays open for <see cref="OpenSeconds"/>, then shuts with its soft thunk. Ignored while the hatch is
    /// already open for a bundle or an unload.
    /// </summary>
    public readonly struct RoverHatchCycleRequested
    {
        public RoverHatchCycleRequested(float openSeconds)
        {
            OpenSeconds = Mathf.Max(0f, openSeconds);
        }

        /// <summary>Seconds from the request until the hatch starts to shut.</summary>
        public float OpenSeconds { get; }
    }

    /// <summary>
    /// A syllable of 07's chirp starts sounding now (docs/design/feel-pass.md §7.2). Published by Audio at each onset,
    /// so 07's eye, antenna and head move in time with its voice.
    /// </summary>
    public readonly struct RoverChirpSyllable
    {
        public RoverChirpSyllable(ChirpMood mood, int index, int count, float pitch, float seconds)
        {
            Mood = mood;
            Index = index;
            Count = count;
            Pitch = Mathf.Clamp01(pitch);
            Seconds = Mathf.Max(0f, seconds);
        }

        public ChirpMood Mood { get; }

        /// <summary>This syllable's place in the phrase, from 0.</summary>
        public int Index { get; }

        /// <summary>How many syllables the phrase has.</summary>
        public int Count { get; }

        /// <summary>Where the syllable sits in 07's range, 0 (lowest) .. 1 (highest).</summary>
        public float Pitch { get; }

        /// <summary>How long the syllable lasts.</summary>
        public float Seconds { get; }
    }

    /// <summary>
    /// 07's wake reached a beat (docs/design/cutscenes.md §1): the opening on a fresh game, the short wake on a loaded
    /// one. <see cref="Hurried"/> is true once the player's drive input has hurried the wake, so cues can be shorter.
    /// </summary>
    public readonly struct RoverWakeBeatReached
    {
        public RoverWakeBeatReached(WakeBeat beat, bool hurried)
        {
            Beat = beat;
            Hurried = hurried;
        }

        public WakeBeat Beat { get; }

        public bool Hurried { get; }
    }
}
