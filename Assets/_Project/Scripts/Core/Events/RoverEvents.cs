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
    /// 07 bumped or scraped into an obstacle (boulder, crate, wall, ruin). Drives contact thud audio and chassis jolt.
    /// </summary>
    public readonly struct RoverCollided
    {
        public RoverCollided(Vector3 position, Vector3 normal, float impactSpeed)
        {
            Position = position;
            Normal = normal;
            ImpactSpeed = impactSpeed;
        }

        public Vector3 Position { get; }

        /// <summary>Outward planar normal of the obstacle contact.</summary>
        public Vector3 Normal { get; }

        /// <summary>Speed into the obstacle normal at impact in m/s (always positive).</summary>
        public float ImpactSpeed { get; }
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

        /// <summary>
        /// The Hex Charge Adapter, a Tycho socket kit that lets 07 charge on a hex contact plate such as Kestrel-3's
        /// (docs/design/opening-v3-purpose.md §5).
        /// </summary>
        HexChargeAdapter = 11,
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
    /// The player switched 07's lamps on (<see cref="On"/>) or off with the lights key; they ease there over a quarter
    /// second. A soft mechanical switch click belongs here, at the lamp.
    /// </summary>
    public readonly struct RoverLightsSwitched
    {
        public RoverLightsSwitched(bool on, Vector3 position)
        {
            On = on;
            Position = position;
        }

        public bool On { get; }

        /// <summary>The headlamp, where the switch clicks.</summary>
        public Vector3 Position { get; }
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
    /// one. Neither can be hurried.
    /// </summary>
    public readonly struct RoverWakeBeatReached
    {
        public RoverWakeBeatReached(WakeBeat beat)
        {
            Beat = beat;
        }

        public WakeBeat Beat { get; }
    }

    /// <summary>
    /// A line of 07's power-on self-test (the New Game wake) begins: its motion starts on 07. UI prints the line in its
    /// "checking" form, Audio plays the step's start sound. <see cref="BootCheckCompleted"/> follows.
    /// </summary>
    public readonly struct BootCheckStarted
    {
        public BootCheckStarted(BootCheckStep step)
        {
            Step = step;
        }

        public BootCheckStep Step { get; }
    }

    /// <summary>
    /// A line of the self-test is done and its result known: UI settles the line (OK, FAULT, JAMMED...), Audio plays
    /// the result's tone.
    /// </summary>
    public readonly struct BootCheckCompleted
    {
        public BootCheckCompleted(BootCheckStep step, BootCheckResult result)
        {
            Step = step;
            Result = result;
        }

        public BootCheckStep Step { get; }

        public BootCheckResult Result { get; }
    }

    /// <summary>
    /// The self-test is over: control returns to the player, and the objective line may appear (only if the player
    /// idles).
    /// </summary>
    public readonly struct BootCheckFinished
    {
    }

    /// <summary>
    /// A key fast-forwarded the self-test: its remaining lines now run <see cref="Scale"/> times faster (each still
    /// started and completed in order), so the face's log types them that much faster too.
    /// </summary>
    public readonly struct BootCheckFastForwarded
    {
        public BootCheckFastForwarded(float scale)
        {
            Scale = scale;
        }

        public float Scale { get; }
    }

    /// <summary>
    /// 07's tube wakes on its face before the self-test (Rover): a dot, a line, then the full raster with a warm bloom,
    /// over <see cref="Seconds"/>. UI draws it; Audio voices it.
    /// </summary>
    public readonly struct CrtPowerOn
    {
        public CrtPowerOn(float seconds)
        {
            Seconds = seconds;
        }

        public float Seconds { get; }
    }

    /// <summary>
    /// The maker's mark begins to draw itself on 07's face (Rover): its strokes, its wordmark, a hold and a dim, over
    /// <see cref="Seconds"/>. UI draws it and reports each stroke (<see cref="LogoStrokeDrawn"/>).
    /// </summary>
    public readonly struct BootLogoBegan
    {
        public BootLogoBegan(float seconds)
        {
            Seconds = seconds;
        }

        public float Seconds { get; }
    }

    /// <summary>
    /// 07 loads its field profile on its face (Rover), over <see cref="Seconds"/>. UI draws it and reports each step
    /// (<see cref="LoadStep"/>).
    /// </summary>
    public readonly struct BootLoadBegan
    {
        public BootLoadBegan(float seconds)
        {
            Seconds = seconds;
        }

        public float Seconds { get; }
    }

    /// <summary>The beam begins stroke <see cref="Index"/> (from 0) of the maker's mark on 07's face (UI).</summary>
    public readonly struct LogoStrokeDrawn
    {
        public LogoStrokeDrawn(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    /// <summary>
    /// Text typed on 07's face during the boot (UI), every character or every few as UI tunes it:
    /// <see cref="CharIndex"/> is the last character on screen of line <see cref="LineIndex"/>. Lines count in the
    /// order they show: 0 the maker's wordmark, 1 the load line, then 2 onward the self-test's lines.
    /// </summary>
    public readonly struct BootTextTyped
    {
        public BootTextTyped(int lineIndex, int charIndex)
        {
            LineIndex = lineIndex;
            CharIndex = charIndex;
        }

        public int LineIndex { get; }

        public int CharIndex { get; }
    }

    /// <summary>
    /// Line <see cref="LineIndex"/> on 07's face (numbered as in <see cref="BootTextTyped"/>) has typed out and settled
    /// (UI), once per line.
    /// </summary>
    public readonly struct BootLineCompleted
    {
        public BootLineCompleted(int lineIndex)
        {
            LineIndex = lineIndex;
        }

        public int LineIndex { get; }
    }

    /// <summary>Step <see cref="Index"/> (from 0) of the field profile's load bar has filled on 07's face (UI).</summary>
    public readonly struct LoadStep
    {
        public LoadStep(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    /// <summary>
    /// One audible or visible tick inside a self-test check: a wheel's twitch (Wheels 0..3), a lamp flicker (Lamps 0..2,
    /// then the ramp as index 3), a spark (Cutter 0..2) with the beacon blinking in sync (Cutter 3..5), the head's sweep
    /// and nod (Head 0, 1), the motor's shudder and creep (Motor 0, 1), the wing's jerk, stall and fold (SolarWing 0..2),
    /// the antenna (Antenna 0 starts to rise, 1 the signal), the cells (Battery 0) and the start (Begin 0 the head
    /// turn, 1 the dust shiver, 2 the headlamps). <see cref="Progress01"/> runs 0 to 1 over the continuous ones (the
    /// lamp ramp, the cell fill, the antenna's rise), published about ten times a second; it is 0 for the rest.
    /// </summary>
    public readonly struct BootCheckPulse
    {
        public BootCheckPulse(BootCheckStep step, int index, float progress01)
        {
            Step = step;
            Index = index;
            Progress01 = progress01;
        }

        public BootCheckStep Step { get; }

        public int Index { get; }

        public float Progress01 { get; }
    }
}
