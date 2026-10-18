using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The four kinds of lunar surface landmarks that pass along the Moon's horizon rim behind 07.</summary>
    public enum TitleLandmarkKind
    {
        Lander = 0,
        Tower = 1,
        Solar = 2,
        Marker = 3,
    }

    /// <summary>A twinkling star in the title CRT sky.</summary>
    [Serializable]
    public struct TitleStarEntry
    {
        [Tooltip("Horizontal viewport coordinate (0..1 across the CRT screen).")]
        [Range(0f, 1f)]
        [SerializeField] private float _u;

        [Tooltip("Vertical viewport coordinate (0..1 from the top of the CRT screen).")]
        [Range(0f, 1f)]
        [SerializeField] private float _v;

        [Tooltip("Base radius (px at 720p) of the star dot.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _size;

        [Tooltip("Phase offset (radians) of the star's twinkle cycle.")]
        [Range(0f, 6.2832f)]
        [SerializeField] private float _phase;

        [Tooltip("Twinkle frequency (Hz).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _frequency;

        [Tooltip("Whether the star draws four-point cross flare rays.")]
        [SerializeField] private bool _flare;

        public TitleStarEntry(float u, float v, float size, float phase, float frequency, bool flare)
        {
            _u = u;
            _v = v;
            _size = size;
            _phase = phase;
            _frequency = frequency;
            _flare = flare;
        }

        public float U => _u;

        public float V => _v;

        public float Size => _size;

        public float Phase => _phase;

        public float Frequency => _frequency;

        public bool Flare => _flare;
    }

    /// <summary>An elliptical crater rotating with the Moon body.</summary>
    [Serializable]
    public struct TitleCraterEntry
    {
        [Tooltip("Angular position (radians) on the Moon disc at t = 0.")]
        [Range(0f, 6.2832f)]
        [SerializeField] private float _angle;

        [Tooltip("Radial distance from the Moon centre as a share of the Moon radius (0..1).")]
        [Range(0.1f, 0.99f)]
        [SerializeField] private float _radialShare;

        [Tooltip("Major radius (px at 320px Moon radius) of the crater ellipse.")]
        [Range(4f, 60f)]
        [SerializeField] private float _radiusX;

        [Tooltip("Minor radius (px at 320px Moon radius) of the crater ellipse before limb foreshortening.")]
        [Range(2f, 40f)]
        [SerializeField] private float _radiusY;

        [Tooltip("Local tilt offset (radians) from the Moon's tangent.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _tilt;

        [Tooltip("Whether the crater draws a sunlit inner rim highlight arc.")]
        [SerializeField] private bool _sunlitRim;

        public TitleCraterEntry(float angle, float radialShare, float radiusX, float radiusY, float tilt, bool sunlitRim)
        {
            _angle = angle;
            _radialShare = radialShare;
            _radiusX = radiusX;
            _radiusY = radiusY;
            _tilt = tilt;
            _sunlitRim = sunlitRim;
        }

        public float Angle => _angle;

        public float RadialShare => _radialShare;

        public float RadiusX => _radiusX;

        public float RadiusY => _radiusY;

        public float Tilt => _tilt;

        public bool SunlitRim => _sunlitRim;
    }

    /// <summary>A surface ridge arc rotating near the Moon's horizon limb.</summary>
    [Serializable]
    public struct TitleRidgeEntry
    {
        [Tooltip("Centre angle (radians) on the Moon disc at t = 0.")]
        [Range(0f, 6.2832f)]
        [SerializeField] private float _angle;

        [Tooltip("Radial share of the Moon radius (0..1) where the ridge arc sits.")]
        [Range(0.5f, 0.99f)]
        [SerializeField] private float _radialShare;

        [Tooltip("Angular span (radians) of the ridge arc.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _span;

        public TitleRidgeEntry(float angle, float radialShare, float span)
        {
            _angle = angle;
            _radialShare = radialShare;
            _span = span;
        }

        public float Angle => _angle;

        public float RadialShare => _radialShare;

        public float Span => _span;
    }

    /// <summary>A surface landmark placed on the Moon's rim that rolls past behind 07.</summary>
    [Serializable]
    public struct TitleLandmarkEntry
    {
        [Tooltip("Seconds into the 48 s Moon turn when this landmark passes the top crown (-pi/2).")]
        [Range(0f, 48f)]
        [SerializeField] private float _passTimeSeconds;

        [Tooltip("Which silhouette to draw on the Moon limb.")]
        [SerializeField] private TitleLandmarkKind _kind;

        [Tooltip("Scale multiplier relative to the reference landmark size.")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float _scale;

        public TitleLandmarkEntry(float passTimeSeconds, TitleLandmarkKind kind, float scale)
        {
            _passTimeSeconds = passTimeSeconds;
            _kind = kind;
            _scale = scale;
        }

        public float PassTimeSeconds => _passTimeSeconds;

        public TitleLandmarkKind Kind => _kind;

        public float Scale => _scale;
    }

    /// <summary>
    /// The title screen (TITLE-SCREEN v1, docs/design/title-screen.md): 07's full-monitor CRT with curvature,
    /// scanlines, dark warm bezel, left-column terminal menu under the "LUNARION" wordmark, and the 2D line-art
    /// animation of 07 driving across a turning Moon on the right.
    /// </summary>
    [Serializable]
    public sealed class TitleSettings
    {
        [Tooltip("The wordmark and subtitle easing in and out (slow in, 0.5 s out into the lens push-in).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(2.2f, 0.5f, 14f, 0.985f, 0.6f, 0.8f);

        [Tooltip("The dark drawn over everything before New Game, Quit to title or Quit, and lifted after a boot.")]
        [SerializeField] private RevealSettings _curtain = new RevealSettings(0.9f, 1.4f, 0f, 1f, 1f, 1f);

        [Tooltip("Seconds the title CRT takes to switch off like an old tube when the game starts or continues: " +
                 "a squeeze to a line, then a dot (docs/design/opening-cutscene-research.md Q1).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _switchOffSeconds = 0.6f;

        [Tooltip("The share of the switch-off spent squeezing the picture to a line (the rest shrinks it to a dot).")]
        [Range(0.2f, 0.9f)]
        [SerializeField] private float _switchOffLineShare = 0.6f;

        [Tooltip("The switched-off line's height as a share of the screen's.")]
        [Range(0.002f, 0.05f)]
        [SerializeField] private float _switchOffLineThickness = 0.012f;

        [Tooltip("How much of the menu blur the world takes behind the title (the pause takes all of it).")]
        [Range(0f, 1f)]
        [SerializeField] private float _backdropWeight = 0.55f;

        [Header("Moon turn and rover motion timings")]
        [Tooltip("Seconds for one full revolution of the Moon under 07's wheels.")]
        [Range(10f, 120f)]
        [SerializeField] private float _moonTurnPeriodSeconds = 48f;

        [Tooltip("Period (seconds) of the Morse '· · · —' beacon on 07's rear whip antenna.")]
        [Range(1f, 10f)]
        [SerializeField] private float _morsePeriodSeconds = 4.2f;

        [Tooltip("Period (seconds) between blinks of 07's CRT monitor eye.")]
        [Range(2f, 15f)]
        [SerializeField] private float _eyeBlinkPeriodSeconds = 6.5f;

        [Tooltip("Time within the eye blink period (seconds) when the eyelid closes.")]
        [Range(1f, 15f)]
        [SerializeField] private float _eyeBlinkCloseThreshold = 6.32f;

        [Tooltip("Period (seconds) of the blinking '>' cursor on the focused title menu row.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _cursorBlinkPeriodSeconds = 0.9f;

        [Tooltip("Share of each cursor blink cycle during which '>' is fully lit.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _cursorVisibleDuty = 0.72f;

        [Tooltip("Lag (seconds) of the warm phosphor persistence trail behind rotating Moon features.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _trailLagSeconds = 0.14f;

        [Header("Title menu framing (right-pane scene at 1920x1080 reference)")]
        [Tooltip("Reference canvas size (px) for the full-monitor v1 title layout.")]
        [SerializeField] private Vector2 _menuReferenceSize = new Vector2(1920f, 1080f);

        [Tooltip("Moon centre (px in 1920x1080 reference) when framed in the right pane beside the menu.")]
        [SerializeField] private Vector2 _menuMoonCenter = new Vector2(1310f, 980f);

        [Tooltip("Moon radius (px in 1920x1080 reference) when framed in the right pane.")]
        [Range(100f, 900f)]
        [SerializeField] private float _menuMoonRadius = 450f;

        [Tooltip("Pixels per metre for 07 in the 1920x1080 right-pane title layout.")]
        [Range(40f, 240f)]
        [SerializeField] private float _menuRoverScale = 82f;

        [Tooltip("Earth centre (px in 1920x1080 reference) in the right-pane sky.")]
        [SerializeField] private Vector2 _menuEarthCenter = new Vector2(955f, 192f);

        [Tooltip("Earth disc radius (px in 1920x1080 reference).")]
        [Range(20f, 160f)]
        [SerializeField] private float _menuEarthRadius = 38f;

        [Tooltip("Offset (px in 1920x1080 reference) from Earth's centre to the cyan L4 Ark beacon.")]
        [SerializeField] private Vector2 _menuArkOffset = new Vector2(66f, -18f);

        [Tooltip("Share of the bottom-anchored vertical offset applied to the sky (Earth and L4 Ark) on taller screens.")]
        [Range(0f, 1f)]
        [SerializeField] private float _skyVerticalOffsetShare = 0.35f;

        [Tooltip("Minimum horizontal gap (px) between the laid-out right edge of the left menu column and the painted Moon scene.")]
        [Range(8f, 120f)]
        [SerializeField] private float _menuSceneLeftGap = 32f;

        [Header("Title left-column header and menu spacing (px inside #menu-frame)")]
        [Tooltip("Horizontal offset (px inside #menu-frame) of the wordmark, subtitle, and divider rule.")]
        [Range(0f, 400f)]
        [SerializeField] private float _titleScreenLeft = 150f;

        [Tooltip("Vertical offset (px inside #menu-frame) of the wordmark block.")]
        [Range(0f, 400f)]
        [SerializeField] private float _titleScreenTop = 126f;

        [Tooltip("Vertical gap (px) between the wordmark and the '//' subtitle row.")]
        [Range(-20f, 40f)]
        [SerializeField] private float _subtitleTopMargin = 4f;

        [Tooltip("Vertical gap (px) between the '//' subtitle row and #title-rule.")]
        [Range(0f, 60f)]
        [SerializeField] private float _ruleTopMargin = 16f;

        [Tooltip("Total width (px) of the horizontal #title-rule under the subtitle.")]
        [Range(200f, 800f)]
        [SerializeField] private float _ruleWidth = 510f;

        [Tooltip("Height (px) of #title-rule.")]
        [Range(1f, 6f)]
        [SerializeField] private float _ruleHeight = 2f;

        [Tooltip("Width (px) of the bright warm-amber accent segment on the left of #title-rule.")]
        [Range(0f, 200f)]
        [SerializeField] private float _ruleAccentWidth = 48f;

        [Tooltip("Left padding (px inside #menu-frame) of the title menu button stack.")]
        [Range(0f, 400f)]
        [SerializeField] private float _menuFrameLeftPadding = 144f;

        [Tooltip("Top padding (px inside #menu-frame) of the title menu button stack so #title-rule sits cleanly above Continue.")]
        [Range(100f, 600f)]
        [SerializeField] private float _menuFrameTopPadding = 342f;

        [Header("Standalone animation framing (1280x720 reference matching anim.html)")]
        [Tooltip("Reference canvas size (px) for the standalone 1280x720 animation stills.")]
        [SerializeField] private Vector2 _standaloneReferenceSize = new Vector2(1280f, 720f);

        [Tooltip("Moon centre (px in 1280x720 reference) in standalone framing.")]
        [SerializeField] private Vector2 _standaloneMoonCenter = new Vector2(640f, 678f);

        [Tooltip("Moon radius (px in 1280x720 reference) in standalone framing.")]
        [Range(100f, 700f)]
        [SerializeField] private float _standaloneMoonRadius = 320f;

        [Tooltip("Pixels per metre for 07 in standalone 1280x720 framing.")]
        [Range(40f, 200f)]
        [SerializeField] private float _standaloneRoverScale = 82f;

        [Tooltip("Earth centre (px in 1280x720 reference) in standalone framing.")]
        [SerializeField] private Vector2 _standaloneEarthCenter = new Vector2(278f, 132f);

        [Tooltip("Earth disc radius (px in 1280x720 reference) in standalone framing.")]
        [Range(20f, 120f)]
        [SerializeField] private float _standaloneEarthRadius = 30f;

        [Tooltip("Offset (px in 1280x720 reference) from Earth's centre to the L4 Ark beacon in standalone framing.")]
        [SerializeField] private Vector2 _standaloneArkOffset = new Vector2(54f, -14f);

        [Header("07 rover geometry and suspension (in rover metres)")]
        [Tooltip("Outer tire radius (m) of 07's wheels.")]
        [Range(0.15f, 0.6f)]
        [SerializeField] private float _wheelRadius = 0.35f;

        [Tooltip("Inner hub rim radius (m) of 07's wheels.")]
        [Range(0.08f, 0.4f)]
        [SerializeField] private float _wheelRimRadius = 0.185f;

        [Tooltip("Half-wheelbase (m) from 07's centre to the front and rear axles.")]
        [Range(0.25f, 0.9f)]
        [SerializeField] private float _wheelbaseHalf = 0.53f;

        [Tooltip("Number of chevron tread lugs around each tire circumference.")]
        [Range(4, 24)]
        [SerializeField] private int _wheelLugCount = 12;

        [Tooltip("Number of radial spokes inside each wheel rim.")]
        [Range(3, 12)]
        [SerializeField] private int _wheelSpokeCount = 6;

        [Tooltip("Scale multiplier for the far-side (left) wheel pair.")]
        [Range(0.7f, 1f)]
        [SerializeField] private float _farWheelScale = 0.94f;

        [Tooltip("Opacity multiplier for the far-side wheel pair and scoop arm.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _farWheelAlpha = 0.42f;

        [Tooltip("Height (m above ground) of the chassis pitch pivot.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _bodyPivotHeight = 0.58f;

        [Tooltip("How strongly front/rear surface bump difference pitches the chassis.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bodyPitchFactor = 0.38f;

        [Tooltip("Amplitude (radians) of the front dig scoop's gentle hydraulic nod.")]
        [Range(0f, 0.15f)]
        [SerializeField] private float _scoopNodAmplitude = 0.025f;

        [Tooltip("Amplitude (m) of the roof solar panel's subtle tilt oscillation.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _solarTiltAmplitude = 0.014f;

        [Tooltip("Primary sway amplitude (m) of the rear whip antenna tip.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _antennaSwayAmplitude = 0.045f;

        [Tooltip("Base forward nod angle (radians) of 07's CRT monitor head.")]
        [Range(-0.3f, 0.3f)]
        [SerializeField] private float _headNodBase = -0.06f;

        [Tooltip("Oscillation amplitude (radians) of 07's CRT monitor head nod.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _headNodAmplitude = 0.018f;

        [Header("Moon schematic grid and landmark visibility window")]
        [Tooltip("Outer dashed schematic grid ring radius as a share of the Moon radius.")]
        [Range(0.3f, 0.98f)]
        [SerializeField] private float _gridOuterRadiusShare = 0.86f;

        [Tooltip("Inner dashed schematic grid ring radius as a share of the Moon radius.")]
        [Range(0.2f, 0.85f)]
        [SerializeField] private float _gridInnerRadiusShare = 0.58f;

        [Tooltip("Maximum angular distance (radians from top crown) within which rim landmarks are drawn.")]
        [Range(0.5f, 1.57f)]
        [SerializeField] private float _landmarkMaxAngle = 1.38f;

        [Tooltip("Minimum angular clearance (radians from top crown) so landmarks fade out under 07's wheels.")]
        [Range(0f, 0.25f)]
        [SerializeField] private float _landmarkRoverClearAngle;

        [Tooltip("Angular span (radians) over which landmarks fade in/out near the horizon limb.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _landmarkEdgeFadeSpan = 0.35f;

        [Tooltip("Angular span (radians) over which landmarks fade out approaching 07 at the crown.")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _landmarkRoverFadeSpan = 0.05f;

        [Header("Full-monitor CRT pass (curvature bezel, scanlines, rolling band, vignette)")]
        [Tooltip("CRT barrel curvature strength k (r_distorted = r * (1 + k * r^2)).")]
        [Range(0f, 0.15f)]
        [SerializeField] private float _crtBarrelStrength = 0.055f;

        [Tooltip("Normalized curved coordinate threshold (0..1) where the dark CRT bezel begins.")]
        [Range(0.90f, 1f)]
        [SerializeField] private float _crtBezelThreshold = 0.985f;

        [Tooltip("Number of horizontal CRT scanlines across the monitor height.")]
        [Range(60, 360)]
        [SerializeField] private int _crtScanlineCount = 180;

        [Tooltip("Opacity of the horizontal CRT scanlines.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _crtScanlineAlpha = 0.24f;

        [Tooltip("Opacity of the dark warm edge vignette inside the CRT bezel.")]
        [Range(0f, 1f)]
        [SerializeField] private float _crtVignetteAlpha = 0.58f;

        [Tooltip("Vertical drift speed (px/s at 720p) of the subtle CRT refresh band.")]
        [Range(0f, 120f)]
        [SerializeField] private float _crtRollSpeed = 36f;

        [Tooltip("Height (px at 720p) of the subtle rolling CRT refresh band.")]
        [Range(20f, 240f)]
        [SerializeField] private float _crtRollBandHeight = 90f;

        [Tooltip("Inner CRT bezel padding (px at 1280x720 reference) on the full-monitor title screen.")]
        [SerializeField] private Vector2 _crtBezelInset = new Vector2(36f, 26f);

        [Tooltip("Inner CRT bezel padding (px at 1280x720 reference) in standalone animation framing.")]
        [SerializeField] private Vector2 _standaloneBezelInset = new Vector2(14f, 12f);

        [Header("Phosphor vector bloom and persistence trail")]
        [Tooltip("Line-width multiplier for the wide outer warm amber halo (#C86A14) behind glowing vector strokes.")]
        [Range(1f, 10f)]
        [SerializeField] private float _glowOuterWidthMultiplier = 4.6f;

        [Tooltip("Opacity multiplier for the wide outer warm amber halo (#C86A14) behind glowing vector strokes.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowOuterAlpha = 0.22f;

        [Tooltip("Line-width multiplier for the tight inner amber halo (#FFB547) behind glowing vector strokes.")]
        [Range(1f, 6f)]
        [SerializeField] private float _glowInnerWidthMultiplier = 2.4f;

        [Tooltip("Opacity multiplier for the tight inner amber halo (#FFB547) behind glowing vector strokes.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowInnerAlpha = 0.42f;

        [Tooltip("Opacity multiplier for the warm phosphor persistence trail behind rotating Moon features.")]
        [Range(0f, 1f)]
        [SerializeField] private float _trailOpacity = 0.34f;

        [Header("R1CrtFace.shader & Variant C phosphor palette")]
        [Tooltip("Deepest CRT bezel and space background (#0E0905).")]
        [SerializeField] private Color _bgDeep = new Color(14f / 255f, 9f / 255f, 5f / 255f, 1f);

        [Tooltip("Warm amber CRT screen centre glow (#20140A).")]
        [SerializeField] private Color _bgWarm = new Color(32f / 255f, 20f / 255f, 10f / 255f, 1f);

        [Tooltip("Dark warm CRT screen corner tone (#120B05).")]
        [SerializeField] private Color _bgCorner = new Color(18f / 255f, 11f / 255f, 5f / 255f, 1f);

        [Tooltip("Occluded lunar disc interior fill (#301E0E).")]
        [SerializeField] private Color _bgMoon = new Color(48f / 255f, 30f / 255f, 14f / 255f, 1f);

        [Tooltip("Primary warm amber phosphor stroke (#FFB547).")]
        [SerializeField] private Color _amber = new Color(1f, 181f / 255f, 71f / 255f, 1f);

        [Tooltip("Bright gold-amber highlight stroke (#FFD680).")]
        [SerializeField] private Color _amberHi = new Color(1f, 214f / 255f, 128f / 255f, 1f);

        [Tooltip("Hot cream phosphor core (#FFF3CC).")]
        [SerializeField] private Color _cream = new Color(1f, 243f / 255f, 204f / 255f, 1f);

        [Tooltip("Secondary dim amber stroke (#D99A45).")]
        [SerializeField] private Color _amberDim = new Color(217f / 255f, 154f / 255f, 69f / 255f, 1f);

        [Tooltip("Faint amber structural rule and grid colour (#8A6030).")]
        [SerializeField] private Color _amberFaint = new Color(138f / 255f, 96f / 255f, 48f / 255f, 1f);

        [Tooltip("Warm orange-amber phosphor persistence trail colour (#C86A14).")]
        [SerializeField] private Color _amberEdge = new Color(200f / 255f, 106f / 255f, 20f / 255f, 1f);

        [Tooltip("Calm cyan telemetry, L4 Ark, and relay beacon accent (#5FF3FF).")]
        [SerializeField] private Color _cyan = new Color(95f / 255f, 243f / 255f, 1f, 1f);

        [Tooltip("Dimmer cyan secondary telemetry accent (#38B2C6).")]
        [SerializeField] private Color _cyanDim = new Color(56f / 255f, 178f / 255f, 198f / 255f, 1f);

        [Header("Deterministic sky and lunar surface tables")]
        [Tooltip("The 12 twinkling stars across the CRT sky.")]
        [SerializeField] private TitleStarEntry[] _stars = DefaultStars();

        [Tooltip("The 16 craters rotating across the Moon disc.")]
        [SerializeField] private TitleCraterEntry[] _craters = DefaultCraters();

        [Tooltip("The 9 ridge arcs rotating near the Moon limb.")]
        [SerializeField] private TitleRidgeEntry[] _ridges = DefaultRidges();

        [Tooltip("The 8 surface landmarks that pass along the Moon's horizon rim during each 48 s turn.")]
        [SerializeField] private TitleLandmarkEntry[] _landmarks = DefaultLandmarks();

        public RevealSettings Reveal => _reveal;

        public RevealSettings Curtain => _curtain;

        public float BackdropWeight => _backdropWeight;

        public float SwitchOffSeconds => _switchOffSeconds;

        public float SwitchOffLineShare => _switchOffLineShare;

        public float SwitchOffLineThickness => _switchOffLineThickness;

        public float MoonTurnPeriodSeconds => _moonTurnPeriodSeconds;

        public float MorsePeriodSeconds => _morsePeriodSeconds;

        public float EyeBlinkPeriodSeconds => _eyeBlinkPeriodSeconds;

        public float EyeBlinkCloseThreshold => _eyeBlinkCloseThreshold;

        public float CursorBlinkPeriodSeconds => _cursorBlinkPeriodSeconds;

        public float CursorVisibleDuty => _cursorVisibleDuty;

        public float TrailLagSeconds => _trailLagSeconds;

        public Vector2 MenuReferenceSize => _menuReferenceSize;

        public Vector2 MenuMoonCenter => _menuMoonCenter;

        public float MenuMoonRadius => _menuMoonRadius;

        public float MenuRoverScale => _menuRoverScale;

        public Vector2 MenuEarthCenter => _menuEarthCenter;

        public float MenuEarthRadius => _menuEarthRadius;

        public Vector2 MenuArkOffset => _menuArkOffset;

        public float SkyVerticalOffsetShare => _skyVerticalOffsetShare;

        public float MenuSceneLeftGap => _menuSceneLeftGap;

        public float TitleScreenLeft => _titleScreenLeft;

        public float TitleScreenTop => _titleScreenTop;

        public float SubtitleTopMargin => _subtitleTopMargin;

        public float RuleTopMargin => _ruleTopMargin;

        public float RuleWidth => _ruleWidth;

        public float RuleHeight => _ruleHeight;

        public float RuleAccentWidth => _ruleAccentWidth;

        public float MenuFrameLeftPadding => _menuFrameLeftPadding;

        public float MenuFrameTopPadding => _menuFrameTopPadding;

        public Vector2 StandaloneReferenceSize => _standaloneReferenceSize;

        public Vector2 StandaloneMoonCenter => _standaloneMoonCenter;

        public float StandaloneMoonRadius => _standaloneMoonRadius;

        public float StandaloneRoverScale => _standaloneRoverScale;

        public Vector2 StandaloneEarthCenter => _standaloneEarthCenter;

        public float StandaloneEarthRadius => _standaloneEarthRadius;

        public Vector2 StandaloneArkOffset => _standaloneArkOffset;

        public float WheelRadius => _wheelRadius;

        public float WheelRimRadius => _wheelRimRadius;

        public float WheelbaseHalf => _wheelbaseHalf;

        public int WheelLugCount => _wheelLugCount;

        public int WheelSpokeCount => _wheelSpokeCount;

        public float FarWheelScale => _farWheelScale;

        public float FarWheelAlpha => _farWheelAlpha;

        public float BodyPivotHeight => _bodyPivotHeight;

        public float BodyPitchFactor => _bodyPitchFactor;

        public float ScoopNodAmplitude => _scoopNodAmplitude;

        public float SolarTiltAmplitude => _solarTiltAmplitude;

        public float AntennaSwayAmplitude => _antennaSwayAmplitude;

        public float HeadNodBase => _headNodBase;

        public float HeadNodAmplitude => _headNodAmplitude;

        public float GridOuterRadiusShare => _gridOuterRadiusShare;

        public float GridInnerRadiusShare => _gridInnerRadiusShare;

        public float LandmarkMaxAngle => _landmarkMaxAngle;

        public float LandmarkRoverClearAngle => _landmarkRoverClearAngle;

        public float LandmarkEdgeFadeSpan => _landmarkEdgeFadeSpan;

        public float LandmarkRoverFadeSpan => _landmarkRoverFadeSpan;

        public float CrtBarrelStrength => _crtBarrelStrength;

        public float CrtBezelThreshold => _crtBezelThreshold;

        public int CrtScanlineCount => _crtScanlineCount;

        public float CrtScanlineAlpha => _crtScanlineAlpha;

        public float CrtVignetteAlpha => _crtVignetteAlpha;

        public float CrtRollSpeed => _crtRollSpeed;

        public float CrtRollBandHeight => _crtRollBandHeight;

        public Vector2 CrtBezelInset => _crtBezelInset;

        public Vector2 StandaloneBezelInset => _standaloneBezelInset;

        public float GlowOuterWidthMultiplier => _glowOuterWidthMultiplier;

        public float GlowOuterAlpha => _glowOuterAlpha;

        public float GlowInnerWidthMultiplier => _glowInnerWidthMultiplier;

        public float GlowInnerAlpha => _glowInnerAlpha;

        public float TrailOpacity => _trailOpacity;

        public Color BgDeep => _bgDeep;

        public Color BgWarm => _bgWarm;

        public Color BgCorner => _bgCorner;

        public Color BgMoon => _bgMoon;

        public Color Amber => _amber;

        public Color AmberHi => _amberHi;

        public Color Cream => _cream;

        public Color AmberDim => _amberDim;

        public Color AmberFaint => _amberFaint;

        public Color AmberEdge => _amberEdge;

        public Color Cyan => _cyan;

        public Color CyanDim => _cyanDim;

        public TitleStarEntry[] Stars => _stars;

        public TitleCraterEntry[] Craters => _craters;

        public TitleRidgeEntry[] Ridges => _ridges;

        public TitleLandmarkEntry[] Landmarks => _landmarks;

        public string Validate()
        {
            if (_reveal == null || _curtain == null)
            {
                return "title reveal or curtain is missing";
            }

            if (_moonTurnPeriodSeconds <= 0f || _morsePeriodSeconds <= 0f || _eyeBlinkPeriodSeconds <= 0f ||
                _cursorBlinkPeriodSeconds <= 0f || _wheelRadius <= 0f || _menuMoonRadius <= 0f ||
                _standaloneMoonRadius <= 0f || _menuRoverScale <= 0f || _standaloneRoverScale <= 0f ||
                _ruleWidth <= 0f || _ruleHeight <= 0f || _glowOuterWidthMultiplier <= 0f ||
                _glowInnerWidthMultiplier <= 0f || _menuReferenceSize.x <= 0f || _menuReferenceSize.y <= 0f ||
                _standaloneReferenceSize.x <= 0f || _standaloneReferenceSize.y <= 0f || _menuSceneLeftGap < 0f)
            {
                return "title animation timing or scale is non-positive";
            }

            if (_stars == null || _stars.Length == 0 || _craters == null || _craters.Length == 0 ||
                _ridges == null || _ridges.Length == 0 || _landmarks == null || _landmarks.Length == 0)
            {
                return "title scene tables are empty";
            }

            return null;
        }

        private static TitleStarEntry[] DefaultStars()
        {
            return new[]
            {
                new TitleStarEntry(148f / 1280f, 86f / 720f, 1.6f, 0.2f, 0.19f, true),
                new TitleStarEntry(295f / 1280f, 62f / 720f, 1.2f, 1.7f, 0.14f, false),
                new TitleStarEntry(442f / 1280f, 128f / 720f, 1.4f, 2.9f, 0.23f, false),
                new TitleStarEntry(560f / 1280f, 74f / 720f, 1.1f, 4.1f, 0.16f, false),
                new TitleStarEntry(748f / 1280f, 58f / 720f, 1.8f, 0.9f, 0.21f, true),
                new TitleStarEntry(892f / 1280f, 112f / 720f, 1.3f, 3.3f, 0.15f, false),
                new TitleStarEntry(1045f / 1280f, 78f / 720f, 1.7f, 5.0f, 0.18f, true),
                new TitleStarEntry(1148f / 1280f, 146f / 720f, 1.2f, 2.2f, 0.13f, false),
                new TitleStarEntry(210f / 1280f, 208f / 720f, 1.1f, 1.1f, 0.25f, false),
                new TitleStarEntry(378f / 1280f, 244f / 720f, 1.3f, 3.8f, 0.17f, false),
                new TitleStarEntry(985f / 1280f, 218f / 720f, 1.2f, 0.4f, 0.20f, false),
                new TitleStarEntry(1122f / 1280f, 278f / 720f, 1.4f, 4.7f, 0.16f, false),
            };
        }

        private static TitleCraterEntry[] DefaultCraters()
        {
            return new[]
            {
                new TitleCraterEntry(0.12f, 0.78f, 30f, 13f, 0.00f, true),
                new TitleCraterEntry(0.58f, 0.62f, 42f, 20f, 0.00f, true),
                new TitleCraterEntry(1.05f, 0.82f, 25f, 11f, 0.00f, false),
                new TitleCraterEntry(1.48f, 0.50f, 36f, 18f, 0.00f, true),
                new TitleCraterEntry(1.95f, 0.75f, 28f, 12f, 0.00f, false),
                new TitleCraterEntry(2.38f, 0.66f, 46f, 22f, 0.00f, true),
                new TitleCraterEntry(2.88f, 0.80f, 24f, 10f, 0.00f, false),
                new TitleCraterEntry(3.42f, 0.58f, 38f, 19f, 0.00f, true),
                new TitleCraterEntry(3.96f, 0.77f, 31f, 14f, 0.00f, true),
                new TitleCraterEntry(4.45f, 0.64f, 29f, 14f, 0.00f, false),
                new TitleCraterEntry(4.98f, 0.83f, 34f, 14f, 0.00f, true),
                new TitleCraterEntry(5.52f, 0.54f, 40f, 20f, 0.00f, true),
                new TitleCraterEntry(5.95f, 0.72f, 22f, 10f, 0.00f, false),
                new TitleCraterEntry(0.85f, 0.30f, 26f, 15f, 0.00f, false),
                new TitleCraterEntry(2.65f, 0.34f, 32f, 18f, 0.00f, true),
                new TitleCraterEntry(4.72f, 0.28f, 24f, 14f, 0.00f, false),
            };
        }

        private static TitleRidgeEntry[] DefaultRidges()
        {
            return new[]
            {
                new TitleRidgeEntry(0.30f, 0.89f, 0.42f),
                new TitleRidgeEntry(1.18f, 0.91f, 0.38f),
                new TitleRidgeEntry(2.10f, 0.88f, 0.46f),
                new TitleRidgeEntry(3.05f, 0.90f, 0.40f),
                new TitleRidgeEntry(4.10f, 0.89f, 0.45f),
                new TitleRidgeEntry(5.15f, 0.91f, 0.38f),
                new TitleRidgeEntry(0.70f, 0.70f, 0.35f),
                new TitleRidgeEntry(2.50f, 0.72f, 0.44f),
                new TitleRidgeEntry(4.60f, 0.68f, 0.40f),
            };
        }

        private static TitleLandmarkEntry[] DefaultLandmarks()
        {
            return new[]
            {
                new TitleLandmarkEntry(0.6f, TitleLandmarkKind.Lander, 1.75f),
                new TitleLandmarkEntry(4.6f, TitleLandmarkKind.Tower, 1.80f),
                new TitleLandmarkEntry(8.4f, TitleLandmarkKind.Solar, 2.05f),
                new TitleLandmarkEntry(13.0f, TitleLandmarkKind.Marker, 1.35f),
                new TitleLandmarkEntry(21.0f, TitleLandmarkKind.Lander, 1.75f),
                new TitleLandmarkEntry(29.0f, TitleLandmarkKind.Tower, 1.80f),
                new TitleLandmarkEntry(37.0f, TitleLandmarkKind.Solar, 2.05f),
                new TitleLandmarkEntry(43.5f, TitleLandmarkKind.Marker, 1.35f),
            };
        }
    }
}
