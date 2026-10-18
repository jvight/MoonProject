using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The finer shape of the scan's light (scan B, "wireframe reveal"): where 07's cheek lens sits, how its iris,
    /// needles and motes move, how the beam wedge fans, fades and wraps the target, and how the data ticks, dust
    /// glints and closing ribbon travel. Held by <see cref="ScanTuning"/>; runtime code only reads it.
    /// </summary>
    [Serializable]
    public sealed class ScanBeamLook
    {
        [Header("07's cheek lens (where the beam leaves)")]
        [Tooltip("Metres from 07's ScanOrigin to the middle of its cheek porthole (x: to the side away from the " +
                 "beam's right, y: back along the beam).")]
        [SerializeField] private Vector2 _cheekOffset = new Vector2(0.078f, 0.072f);

        [Tooltip("Metres from the porthole's middle to the lens's hot front (x: further to the side, y: forward).")]
        [SerializeField] private Vector2 _lensFrontOffset = new Vector2(0.008f, 0.010f);

        [Tooltip("Metres from 07's ScanOrigin to where the beam wedge and the closing ribbon meet the lens (x: to " +
                 "the side away from the beam's right, y: back along the beam).")]
        [SerializeField] private Vector2 _apertureOffset = new Vector2(0.074f, 0.056f);

        [Tooltip("Smallest sweep height (m) the beam spans, so a flat target still gets a readable fan.")]
        [Range(0.05f, 1f)] [SerializeField] private float _minSpanHeight = 0.36f;

        [Header("The lens's glow")]
        [Tooltip("Share of white in the lens's glow core.")]
        [Range(0f, 1f)] [SerializeField] private float _coreWhite = 0.46f;

        [Tooltip("Share of white in the hot diode at the lens's front.")]
        [Range(0f, 1f)] [SerializeField] private float _diodeWhite = 0.78f;

        [Tooltip("The diode's opacity against the glow core's.")]
        [Range(0f, 1f)] [SerializeField] private float _diodeAlphaShare = 0.98f;

        [Tooltip("The diode's size against the glow core's.")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _diodeSizeShare = 0.56f;

        [Header("The lens's iris")]
        [Tooltip("Radius of the opened iris ring against the iris's size.")]
        [Range(0.1f, 1f)] [SerializeField] private float _irisRadiusShare = 0.38f;

        [Tooltip("Size of each iris segment against the iris's size.")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _irisSegmentShare = 0.105f;

        [Tooltip("Smallest size (m) of an iris segment, so the ring never breaks into dust.")]
        [Range(0.002f, 0.05f)] [SerializeField] private float _irisSegmentMin = 0.014f;

        [Tooltip("Share of white in the iris ring.")]
        [Range(0f, 1f)] [SerializeField] private float _irisWhite = 0.32f;

        [Tooltip("Radians per second the iris ring turns while it scans.")]
        [Range(0f, 3f)] [SerializeField] private float _irisSpinSpeed = 0.35f;

        [Tooltip("Radians the iris ring turns over a whole scan, so it reads as the scan's progress.")]
        [Range(0f, 6.3f)] [SerializeField] private float _irisProgressTurn = 0.8f;

        [Tooltip("Share of white in the iris's two caliper ticks.")]
        [Range(0f, 1f)] [SerializeField] private float _caliperWhite = 0.56f;

        [Tooltip("The caliper ticks' opacity against the iris ring's.")]
        [Range(0f, 2f)] [SerializeField] private float _caliperAlphaShare = 1.05f;

        [Tooltip("Radius of the caliper ticks against the iris's size.")]
        [Range(0.1f, 1f)] [SerializeField] private float _caliperRadiusShare = 0.42f;

        [Tooltip("The caliper ticks' size against an iris segment's.")]
        [Range(0.5f, 3f)] [SerializeField] private float _caliperSizeShare = 1.35f;

        [Tooltip("Up-and-down shifts the caliper ticks make over a whole scan, following the sweep.")]
        [Range(0f, 6f)] [SerializeField] private float _caliperSweepCycles = 1.75f;

        [Tooltip("Up-and-down shifts per second the caliper ticks make on their own.")]
        [Range(0f, 3f)] [SerializeField] private float _caliperIdleCycles = 0.5f;

        [Tooltip("Metres the caliper ticks shift up and down.")]
        [Range(0f, 0.03f)] [SerializeField] private float _caliperShift = 0.005f;

        [Header("The lens's flare needles")]
        [Tooltip("Share of white in the flare needles.")]
        [Range(0f, 1f)] [SerializeField] private float _needleWhite = 0.58f;

        [Tooltip("The upright needles' width against the glint width.")]
        [Range(0.2f, 4f)] [SerializeField] private float _needleWidthShare = 1.55f;

        [Tooltip("The level needles' width against the upright needles'.")]
        [Range(0.2f, 3f)] [SerializeField] private float _needleLevelWidthShare = 1.15f;

        [Tooltip("The level needles' length against the glint length and the bezel's radius (whichever is shorter).")]
        [Range(0.1f, 1f)] [SerializeField] private float _needleLevelLengthShare = 0.56f;

        [Tooltip("The upright needles' longest reach against the bezel's radius.")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _needleUprightBezelShare = 0.94f;

        [Tooltip("Direction of the level needles in the lens plane (x: forward along the beam, y: to the side away " +
                 "from the beam's right).")]
        [SerializeField] private Vector2 _needleLevelAxis = new Vector2(0.90f, 0.44f);

        [Header("The lens's motes")]
        [Tooltip("Life cycles per second of each mote round the lens.")]
        [Range(0.1f, 4f)] [SerializeField] private float _moteCycles = 0.95f;

        [Tooltip("Share of a life cycle between one mote and the next, so they never pulse together.")]
        [Range(0f, 1f)] [SerializeField] private float _motePhaseStep = 0.41f;

        [Tooltip("Radians per second the motes turn round the lens.")]
        [Range(0f, 3f)] [SerializeField] private float _moteSpin = 0.55f;

        [Tooltip("Share of the mote radius a mote is born at; it drifts out to the full radius.")]
        [Range(0f, 1f)] [SerializeField] private float _moteBirthRadiusShare = 0.35f;

        [Tooltip("Metres a mote drifts out to the side over its life.")]
        [Range(0f, 0.08f)] [SerializeField] private float _moteDrift = 0.015f;

        [Tooltip("Size of the even and the odd motes against the mote size.")]
        [Min(0f)] [SerializeField] private Vector2 _moteSizeShares = new Vector2(0.75f, 1.08f);

        [Header("The beam wedge's length")]
        [Tooltip("Shortest beam (m) that draws its wedge; nearer, only the lens glows.")]
        [Range(0.02f, 1f)] [SerializeField] private float _wedgeMinLength = 0.12f;

        [Tooltip("Share of the beam's length the bright nozzle at the lens may reach at most.")]
        [Range(0.02f, 0.5f)] [SerializeField] private float _nozzleLengthShare = 0.14f;

        [Tooltip("Share of the beam's length the contact glow at the target may reach at most.")]
        [Range(0.02f, 0.5f)] [SerializeField] private float _contactLengthShare = 0.12f;

        [Tooltip("Shortest fade (m) of the nozzle and the contact glow on a very short beam.")]
        [Range(0.005f, 0.1f)] [SerializeField] private float _minGlowDecay = 0.02f;

        [Tooltip("Metres from the lens the wedge stays at full brightness before it clears 07's own screen.")]
        [Range(0f, 0.3f)] [SerializeField] private float _screenClearStart = 0.095f;

        [Tooltip("Metres from the lens over which the wedge comes back after clearing 07's screen (x: start, y: end).")]
        [SerializeField] private Vector2 _screenClearRamp = new Vector2(0.115f, 0.48f);

        [Tooltip("The wedge's brightness against its own where it passes 07's screen.")]
        [Range(0f, 1f)] [SerializeField] private float _screenClearFloor = 0.18f;

        [Tooltip("How much of the wedge's widening eases in rather than running straight (0: a straight taper).")]
        [Range(0f, 1f)] [SerializeField] private float _widthEase = 0.18f;

        [Tooltip("Share of white at the lens end of the wedge.")]
        [Range(0f, 1f)] [SerializeField] private float _lensWhite = 0.62f;

        [Tooltip("Share of white at the target end of the wedge.")]
        [Range(0f, 1f)] [SerializeField] private float _contactWhite = 0.34f;

        [Tooltip("Share of the beam's length over which its cyan warms toward amber (x: start, y: end).")]
        [SerializeField] private Vector2 _amberRamp = new Vector2(0.25f, 0.95f);

        [Tooltip("Most amber the wedge warms to at its target end.")]
        [Range(0f, 1f)] [SerializeField] private float _amberShare = 0.82f;

        [Header("The beam wedge's far edge")]
        [Tooltip("The far edge's width at the start and end of a scan against its width mid-scan.")]
        [Range(0.1f, 1f)] [SerializeField] private float _targetEdgeShare = 0.75f;

        [Tooltip("Narrowest and widest the far edge spans on the target (m).")]
        [Min(0.01f)] [SerializeField] private Vector2 _targetWidthRange = new Vector2(0.32f, 0.92f);

        [Tooltip("Radius the far edge wraps round the target with, against the sweep height.")]
        [Range(0f, 1f)] [SerializeField] private float _wrapShare = 0.26f;

        [Tooltip("Smallest and largest wrap radius (m).")]
        [Min(0f)] [SerializeField] private Vector2 _wrapRadiusRange = new Vector2(0.06f, 0.24f);

        [Tooltip("Largest wrap radius against the beam's length, so a near target is never swallowed.")]
        [Range(0f, 1f)] [SerializeField] private float _wrapLengthShare = 0.25f;

        [Tooltip("Share of the beam's length where the far edge starts wrapping round the target.")]
        [Range(0f, 1f)] [SerializeField] private float _wrapStart = 0.68f;

        [Header("The beam wedge's fan")]
        [Tooltip("Tilt of the two crossed sheets out of level, at the lens (x) and at the target (y), so the fan " +
                 "reads from the low chase camera.")]
        [SerializeField] private Vector2 _sheetTilt = new Vector2(0.68f, 0.34f);

        [Tooltip("The second sheet's opacity against the first, at the lens (x) and at the target (y).")]
        [SerializeField] private Vector2 _secondSheetAlpha = new Vector2(0.78f, 0.52f);

        [Tooltip("The second sheet's arch against the first's.")]
        [Range(0f, 1f)] [SerializeField] private float _secondSheetArch = 0.5f;

        [Tooltip("How much the arch flattens toward the target (0: the same arch the whole way).")]
        [Range(0f, 1f)] [SerializeField] private float _archFlatten = 0.5f;

        [Tooltip("How rounded the fan's sides are (0: square edges, 1: a full ellipse).")]
        [Range(0f, 1f)] [SerializeField] private float _sideRoundness = 0.85f;

        [Tooltip("Fine striations across the fan.")]
        [Range(1f, 20f)] [SerializeField] private float _striations = 7f;

        [Tooltip("Striations the fan's lines drift across over a whole scan.")]
        [Range(0f, 6f)] [SerializeField] private float _striationDrift = 1.5f;

        [Tooltip("Metres from the lens over which the striations fade in.")]
        [Range(0.005f, 0.3f)] [SerializeField] private float _striationFadeIn = 0.06f;

        [Tooltip("Depth of the striations against the scanline contrast.")]
        [Range(0f, 1f)] [SerializeField] private float _striationDepth = 0.32f;

        [Tooltip("How tightly the bright filament hugs the beam's axis (higher: thinner).")]
        [Range(0.5f, 6f)] [SerializeField] private float _filamentSharpness = 2.2f;

        [Tooltip("Metres from the lens over which the axis filament fades.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _filamentLength = 0.085f;

        [Tooltip("Opacity of the axis filament at the lens.")]
        [Range(0f, 1f)] [SerializeField] private float _filamentAlpha = 0.26f;

        [Header("The data ticks (B: the target's data streaming into 07)")]
        [Tooltip("Shortest beam (m) that carries data ticks.")]
        [Range(0.05f, 2f)] [SerializeField] private float _tickMinBeamLength = 0.35f;

        [Tooltip("The ticks' stream speed against the beam pulse speed.")]
        [Range(0.05f, 2f)] [SerializeField] private float _tickSpeedShare = 0.42f;

        [Tooltip("Share of the beam's length a tick starts at (x, by the target) and ends at (y, by the lens).")]
        [SerializeField] private Vector2 _tickTravel = new Vector2(0.96f, 0.06f);

        [Tooltip("How evenly a tick moves along the beam (1: steady, lower: it gathers speed toward the lens).")]
        [Range(0f, 1f)] [SerializeField] private float _tickEvenness = 0.65f;

        [Tooltip("Half width of the ticks' lanes at the target against the far edge's width.")]
        [Range(0f, 1f)] [SerializeField] private float _tickTargetSpread = 0.36f;

        [Tooltip("Narrowest half width (m) of the ticks' lanes at the target.")]
        [Range(0f, 0.5f)] [SerializeField] private float _tickTargetSpreadMin = 0.08f;

        [Tooltip("Half width of the ticks' lanes at the lens against the beam's lens width.")]
        [Range(0f, 1f)] [SerializeField] private float _tickLensSpread = 0.35f;

        [Tooltip("Spacing between the three tick lanes, against the lane half width.")]
        [Range(0f, 1f)] [SerializeField] private float _tickLaneSpacing = 0.42f;

        [Tooltip("Offset of alternate ticks off their lane, against the lane half width.")]
        [Range(0f, 1f)] [SerializeField] private float _tickLaneOffset = 0.24f;

        [Tooltip("Metres a tick bows up or down mid-beam.")]
        [Range(0f, 0.05f)] [SerializeField] private float _tickBow = 0.012f;

        [Tooltip("Length (m) of a tick as it appears (x) and at its brightest (y).")]
        [Min(0f)] [SerializeField] private Vector2 _tickLength = new Vector2(0.032f, 0.068f);

        [Tooltip("Width (m) of a tick.")]
        [Range(0.002f, 0.05f)] [SerializeField] private float _tickWidth = 0.011f;

        [Tooltip("Share of white in the cyan a tick cools to near the lens.")]
        [Range(0f, 1f)] [SerializeField] private float _tickCyanWhite = 0.55f;

        [Tooltip("Most a tick cools from amber toward cyan as it reaches the lens.")]
        [Range(0f, 1f)] [SerializeField] private float _tickCyanShare = 0.35f;

        [Tooltip("Peak opacity of a tick.")]
        [Range(0f, 1f)] [SerializeField] private float _tickAlpha = 0.28f;

        [Tooltip("Most opacity a tick may reach, kept under bloom.")]
        [Range(0f, 1f)] [SerializeField] private float _tickAlphaCap = 0.32f;

        [Header("The dust glints along the beam")]
        [Tooltip("Shortest and longest twinkle (s) of a dust glint.")]
        [Min(0.01f)] [SerializeField] private Vector2 _glintLifetimeRange = new Vector2(0.10f, 0.25f);

        [Tooltip("Spread of each glint's twinkle against the glint lifetime (x: shortest, y: longest).")]
        [Min(0.01f)] [SerializeField] private Vector2 _glintTwinkleSpread = new Vector2(0.75f, 1.22f);

        [Tooltip("Spread of each glint's share of time shown (x: least, y: most), so only one or two show at once.")]
        [Min(0.01f)] [SerializeField] private Vector2 _glintDutySpread = new Vector2(0.85f, 1.15f);

        [Tooltip("Least share of time a glint is shown.")]
        [Range(0.05f, 1f)] [SerializeField] private float _glintMinDuty = 0.20f;

        [Tooltip("Shortest beam (m) that carries dust glints.")]
        [Range(0.1f, 3f)] [SerializeField] private float _glintMinBeamLength = 0.65f;

        [Tooltip("Metres from the lens the nearest glint may stand, so none sparkle on 07's own face.")]
        [Range(0f, 2f)] [SerializeField] private float _glintLensClearance = 0.75f;

        [Tooltip("Narrowest half width (m) of the band the glints stand in at the target.")]
        [Range(0f, 0.5f)] [SerializeField] private float _glintTargetSpreadMin = 0.12f;

        [Tooltip("A glint's size near the lens (x) and near the target (y), against the glint size.")]
        [Min(0f)] [SerializeField] private Vector2 _glintDistanceScale = new Vector2(0.72f, 1.08f);

        [Tooltip("Spread of each grain's size against the glint size (x: smallest, y: largest).")]
        [Min(0f)] [SerializeField] private Vector2 _glintGrainScale = new Vector2(0.86f, 1.12f);

        [Tooltip("Most opacity a glint may reach, kept under bloom.")]
        [Range(0f, 1f)] [SerializeField] private float _glintAlphaCap = 0.24f;

        [Tooltip("Share of white in a glint.")]
        [Range(0f, 1f)] [SerializeField] private float _glintWhite = 0.22f;

        [Header("The closing ribbon (the target's lines streaming into the lens)")]
        [Tooltip("Sweep height (m) the ribbon's shell takes when the scan reports none.")]
        [Range(0.05f, 2f)] [SerializeField] private float _ribbonFallbackSpan = 0.65f;

        [Tooltip("Radius of the shell the ribbon gathers from, against the sweep height.")]
        [Range(0.05f, 1f)] [SerializeField] private float _ribbonShellShare = 0.42f;

        [Tooltip("Smallest and largest radius (m) of the shell the ribbon gathers from.")]
        [Min(0f)] [SerializeField] private Vector2 _ribbonShellRange = new Vector2(0.16f, 0.46f);

        [Tooltip("Height rows of the ribbon's shell.")]
        [Range(1, 9)] [SerializeField] private int _ribbonShellRows = 5;

        [Tooltip("Height between the shell's rows, against its radius.")]
        [Range(0f, 1f)] [SerializeField] private float _ribbonRowSpacing = 0.22f;

        [Tooltip("Depth of the shell toward 07, against its radius.")]
        [Range(0f, 1f)] [SerializeField] private float _ribbonShellDepth = 0.35f;

        [Tooltip("How much the shell has shrunk by the time a bead reaches the lens.")]
        [Range(0f, 1f)] [SerializeField] private float _ribbonShellCollapse = 0.88f;

        [Tooltip("Radians the shell turns as it collapses into the lens.")]
        [Range(0f, 6.3f)] [SerializeField] private float _ribbonCollapseTurn = 2.2f;

        [Tooltip("Share of the stream the ribbon holds on the target's shell before it collapses into the lens.")]
        [Range(0f, 0.9f)] [SerializeField] private float _ribbonHold = 0.12f;

        [Tooltip("A bead's size against the lens mote size.")]
        [Range(0.2f, 5f)] [SerializeField] private float _ribbonBeadShare = 1.6f;

        [Tooltip("Smallest size (m) of a ribbon bead.")]
        [Range(0.002f, 0.05f)] [SerializeField] private float _ribbonBeadMin = 0.012f;

        [Tooltip("Stretch (m) of a bead toward the lens at the ends (x) and middle (y) of its flight.")]
        [Min(0f)] [SerializeField] private Vector2 _ribbonStretch = new Vector2(0.022f, 0.055f);

        [Tooltip("Share of white at the ribbon's head.")]
        [Range(0f, 1f)] [SerializeField] private float _ribbonHeadWhite = 0.24f;

        [Tooltip("A bead's opacity against the lens mote opacity.")]
        [Range(0f, 2f)] [SerializeField] private float _ribbonAlphaShare = 0.55f;

        [Tooltip("Most opacity a bead may reach, kept under bloom.")]
        [Range(0f, 1f)] [SerializeField] private float _ribbonAlphaCap = 0.22f;

        [Tooltip("Share of the stream after which the lens starts to catch the arriving light.")]
        [Range(0f, 1f)] [SerializeField] private float _catchStart = 0.35f;

        [Tooltip("The lens catch's opacity when it starts, against the lens core's (it grows to the full core).")]
        [Range(0f, 1f)] [SerializeField] private float _catchAlphaStart = 0.75f;

        [Tooltip("Most opacity the lens catch may reach, kept under bloom.")]
        [Range(0f, 1f)] [SerializeField] private float _catchAlphaCap = 0.28f;

        [Tooltip("How much the lens catch swells as the last light arrives.")]
        [Range(0f, 1f)] [SerializeField] private float _catchSwell = 0.15f;

        public Vector2 CheekOffset => _cheekOffset;
        public Vector2 LensFrontOffset => _lensFrontOffset;
        public Vector2 ApertureOffset => _apertureOffset;
        public float MinSpanHeight => _minSpanHeight;
        public float CoreWhite => _coreWhite;
        public float DiodeWhite => _diodeWhite;
        public float DiodeAlphaShare => _diodeAlphaShare;
        public float DiodeSizeShare => _diodeSizeShare;
        public float IrisRadiusShare => _irisRadiusShare;
        public float IrisSegmentShare => _irisSegmentShare;
        public float IrisSegmentMin => _irisSegmentMin;
        public float IrisWhite => _irisWhite;
        public float IrisSpinSpeed => _irisSpinSpeed;
        public float IrisProgressTurn => _irisProgressTurn;
        public float CaliperWhite => _caliperWhite;
        public float CaliperAlphaShare => _caliperAlphaShare;
        public float CaliperRadiusShare => _caliperRadiusShare;
        public float CaliperSizeShare => _caliperSizeShare;
        public float CaliperSweepCycles => _caliperSweepCycles;
        public float CaliperIdleCycles => _caliperIdleCycles;
        public float CaliperShift => _caliperShift;
        public float NeedleWhite => _needleWhite;
        public float NeedleWidthShare => _needleWidthShare;
        public float NeedleLevelWidthShare => _needleLevelWidthShare;
        public float NeedleLevelLengthShare => _needleLevelLengthShare;
        public float NeedleUprightBezelShare => _needleUprightBezelShare;
        public Vector2 NeedleLevelAxis => _needleLevelAxis;
        public float MoteCycles => _moteCycles;
        public float MotePhaseStep => _motePhaseStep;
        public float MoteSpin => _moteSpin;
        public float MoteBirthRadiusShare => _moteBirthRadiusShare;
        public float MoteDrift => _moteDrift;
        public Vector2 MoteSizeShares => _moteSizeShares;
        public float WedgeMinLength => _wedgeMinLength;
        public float NozzleLengthShare => _nozzleLengthShare;
        public float ContactLengthShare => _contactLengthShare;
        public float MinGlowDecay => _minGlowDecay;
        public float ScreenClearStart => _screenClearStart;
        public Vector2 ScreenClearRamp => _screenClearRamp;
        public float ScreenClearFloor => _screenClearFloor;
        public float WidthEase => _widthEase;
        public float LensWhite => _lensWhite;
        public float ContactWhite => _contactWhite;
        public Vector2 AmberRamp => _amberRamp;
        public float AmberShare => _amberShare;
        public float TargetEdgeShare => _targetEdgeShare;
        public Vector2 TargetWidthRange => _targetWidthRange;
        public float WrapShare => _wrapShare;
        public Vector2 WrapRadiusRange => _wrapRadiusRange;
        public float WrapLengthShare => _wrapLengthShare;
        public float WrapStart => _wrapStart;
        public Vector2 SheetTilt => _sheetTilt;
        public Vector2 SecondSheetAlpha => _secondSheetAlpha;
        public float SecondSheetArch => _secondSheetArch;
        public float ArchFlatten => _archFlatten;
        public float SideRoundness => _sideRoundness;
        public float Striations => _striations;
        public float StriationDrift => _striationDrift;
        public float StriationFadeIn => _striationFadeIn;
        public float StriationDepth => _striationDepth;
        public float FilamentSharpness => _filamentSharpness;
        public float FilamentLength => _filamentLength;
        public float FilamentAlpha => _filamentAlpha;
        public float TickMinBeamLength => _tickMinBeamLength;
        public float TickSpeedShare => _tickSpeedShare;
        public Vector2 TickTravel => _tickTravel;
        public float TickEvenness => _tickEvenness;
        public float TickTargetSpread => _tickTargetSpread;
        public float TickTargetSpreadMin => _tickTargetSpreadMin;
        public float TickLensSpread => _tickLensSpread;
        public float TickLaneSpacing => _tickLaneSpacing;
        public float TickLaneOffset => _tickLaneOffset;
        public float TickBow => _tickBow;
        public Vector2 TickLength => _tickLength;
        public float TickWidth => _tickWidth;
        public float TickCyanWhite => _tickCyanWhite;
        public float TickCyanShare => _tickCyanShare;
        public float TickAlpha => _tickAlpha;
        public float TickAlphaCap => _tickAlphaCap;
        public Vector2 GlintLifetimeRange => _glintLifetimeRange;
        public Vector2 GlintTwinkleSpread => _glintTwinkleSpread;
        public Vector2 GlintDutySpread => _glintDutySpread;
        public float GlintMinDuty => _glintMinDuty;
        public float GlintMinBeamLength => _glintMinBeamLength;
        public float GlintLensClearance => _glintLensClearance;
        public float GlintTargetSpreadMin => _glintTargetSpreadMin;
        public Vector2 GlintDistanceScale => _glintDistanceScale;
        public Vector2 GlintGrainScale => _glintGrainScale;
        public float GlintAlphaCap => _glintAlphaCap;
        public float GlintWhite => _glintWhite;
        public float RibbonFallbackSpan => _ribbonFallbackSpan;
        public float RibbonShellShare => _ribbonShellShare;
        public Vector2 RibbonShellRange => _ribbonShellRange;
        public int RibbonShellRows => _ribbonShellRows;
        public float RibbonRowSpacing => _ribbonRowSpacing;
        public float RibbonShellDepth => _ribbonShellDepth;
        public float RibbonShellCollapse => _ribbonShellCollapse;
        public float RibbonCollapseTurn => _ribbonCollapseTurn;
        public float RibbonHold => _ribbonHold;
        public float RibbonBeadShare => _ribbonBeadShare;
        public float RibbonBeadMin => _ribbonBeadMin;
        public Vector2 RibbonStretch => _ribbonStretch;
        public float RibbonHeadWhite => _ribbonHeadWhite;
        public float RibbonAlphaShare => _ribbonAlphaShare;
        public float RibbonAlphaCap => _ribbonAlphaCap;
        public float CatchStart => _catchStart;
        public float CatchAlphaStart => _catchAlphaStart;
        public float CatchAlphaCap => _catchAlphaCap;
        public float CatchSwell => _catchSwell;
    }
}
