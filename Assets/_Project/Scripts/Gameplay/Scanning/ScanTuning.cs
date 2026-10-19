using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scanning (docs/design/0.5.5-owner-feedback.md §1, opening-v2 §1): the ruins' structure sets and story scans,
    /// how long a target takes to scan by its size, and the closing dissolve. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class ScanTuning : ScriptableObject
    {
        /// <summary>The lander cargo bay's scan host id (its fragments' saved ids start with it).</summary>
        public const string LanderCargoId = "LanderCargo";

        [Header("The verb (§1.2)")]
        [Tooltip("Seconds to scan a small fragment (a lamp, a lens).")]
        [Range(0.3f, 10f)] [SerializeField] private float _smallSeconds = 1.5f;

        [Tooltip("Seconds to scan a medium fragment.")]
        [Range(0.3f, 10f)] [SerializeField] private float _mediumSeconds = 2.5f;

        [Tooltip("Seconds to scan a large fragment (a capacitor bank, a pannier).")]
        [Range(0.3f, 10f)] [SerializeField] private float _largeSeconds = 4f;

        [Tooltip("Metres from a fragment's scan point within which a hold of E scans it.")]
        [Range(1f, 20f)] [SerializeField] private float _reach = 8f;

        [Tooltip("Degrees off 07's view a fragment may lie and still be the one it looks at.")]
        [Range(5f, 90f)] [SerializeField] private float _cone = 25f;

        [Tooltip("Degrees of aim score the target (or verb) holding the prompt keeps over a rival that is only " +
                 "slightly squarer, so the brackets do not flicker between two close targets.")]
        [Range(0f, 5f)] [SerializeField] private float _lookStickiness = 1f;

        [Tooltip("How much nearness weighs against looking straight at a fragment when two are in view (0: the one " +
                 "nearest the view's centre wins, story panels included).")]
        [Range(0f, 1f)] [SerializeField] private float _distanceWeight;

        [Tooltip("07 eases to a stop before its lens opens: the scan runs below this speed (m/s).")]
        [Range(0.05f, 2f)] [SerializeField] private float _engageSpeed = 0.4f;

        [Tooltip("Gaze priority requested on IRoverRig while aiming at or scanning a fragment (above ambient curious glances).")]
        [Range(5, 50)] [SerializeField] private int _gazePriority = 25;

        [Tooltip("Maximum angle in degrees between IRoverRig.ScanOrigin.forward and the target point for line-of-sight scan progression.")]
        [Range(1f, 20f)] [SerializeField] private float _aimToleranceDegrees = 8f;

        [Header("Done (§1.2, D2)")]
        [Tooltip("Seconds of the dissolve: the solid fades, the lines stream into 07's lens, the lens flares.")]
        [Range(0.3f, 3f)] [SerializeField] private float _dissolveSeconds = 0.9f;

        [Tooltip("Metal a fragment of an already known blueprint gives after its dissolve, so leftovers still pay.")]
        [Range(0, 12)] [SerializeField] private int _knownMetal = 3;

        [Header("Surface scan look (SCAN v2)")]
        [Tooltip("Cyan colour of the scan: the thin beam, the crisp surface scan line and the soft swept band. The " +
                 "scan's colours all live here and stay cool, never sharing a hue with 07's warm amber lamps.")]
        [SerializeField] private Color _scanLineColor = new Color(0.20f, 0.86f, 0.96f, 1f);

        [Tooltip("Teal accent colour of the scan's faint wireframe pulse, its completion ping outline and the lens " +
                 "pulse: cool like the scan line, never the lamps' amber.")]
        [SerializeField] private Color _completionPingColor = new Color(0.30f, 0.98f, 0.80f, 1f);

        [Tooltip("World-space half-width of the wireframe reveal's bright rising front on the object (m).")]
        [Range(0.003f, 0.05f)] [SerializeField] private float _scanLineWidth = 0.014f;

        [Tooltip("Seconds the thin anti-aliased teal edge traces the object at the completion ping before fading.")]
        [Range(0.1f, 0.9f)] [SerializeField] private float _pingDurationSeconds = 0.38f;

        [Tooltip("Screen-derivative thickness multiplier for the thin anti-aliased completion ping edge.")]
        [Range(0.5f, 3f)] [SerializeField] private float _pingEdgeThickness = 1.35f;

        [Tooltip("Emissive intensity of the teal completion ping edge.")]
        [Range(0.1f, 2f)] [SerializeField] private float _pingEdgeIntensity = 1.25f;

        [Tooltip("World-space cell pitch (m) of the surface wireframe lattice  the scan reveals.")]
        [Range(0.015f, 0.12f)] [SerializeField] private float _surfaceGridPitchMeters = 0.044f;

        [Tooltip("Emissive intensity of the progressive teal wireframe hologram.")]
        [Range(0.2f, 2.0f)] [SerializeField] private float _wireframeRevealIntensity = 0.7f;

        [Tooltip("Speed (cycles/s) of the data ticks streaming along the beam to the lens.")]
        [Range(0.5f, 8.0f)] [SerializeField] private float _beamPulseSpeed = 3.2f;

        [Tooltip("Diameter of the soft emitter glow core at 07's scan lens (m).")]
        [Range(0.01f, 0.35f)] [SerializeField] private float _lensGlowSize = 0.072f;

        [Tooltip("Alpha of the soft emitter glow core at 07's scan lens.")]
        [Range(0.02f, 1f)] [SerializeField] private float _lensGlowAlpha = 0.76f;

        [Tooltip("Diameter of the soft outer halo at 07's scan lens (m).")]
        [Range(0.02f, 0.6f)] [SerializeField] private float _lensHaloSize = 0.16f;

        [Tooltip("Alpha of the soft outer halo at 07's scan lens (kept calm below bloom blowout).")]
        [Range(0.01f, 0.8f)] [SerializeField] private float _lensHaloAlpha = 0.18f;

        [Tooltip("Maximum radius from 07's lens where tiny motes sparkle in vacuum (m).")]
        [Range(0.01f, 0.16f)] [SerializeField] private float _lensMoteRadius = 0.052f;

        [Tooltip("Diameter of the tiny motes sparkling right at 07's lens (m).")]
        [Range(0.003f, 0.06f)] [SerializeField] private float _lensMoteSize = 0.008f;

        [Tooltip("Peak alpha of the tiny motes sparkling right at 07's lens.")]
        [Range(0f, 1f)] [SerializeField] private float _lensMoteAlpha = 0.38f;

        [Header("Stylised scan beam wedge & lens (SCAN v3)")]
        [Tooltip("Width (m) of the narrow scan beam wedge at 07's cheek lens aperture.")]
        [Range(0.01f, 0.12f)] [SerializeField] private float _beamLensWidth = 0.064f;

        [Tooltip("Share of the target's sweep span height spanned by the far edge of the scan beam wedge where it meets the surface scan line.")]
        [Range(0.25f, 1.25f)] [SerializeField] private float _beamTargetWidthShare = 0.85f;

        [Tooltip("Peak additive alpha of the scan beam wedge in the first couple of cm leaving 07's cheek lens.")]
        [Range(0.05f, 0.85f)] [SerializeField] private float _beamLensPeakAlpha = 0.68f;

        [Tooltip("Very low additive alpha in the middle of the scan beam wedge (almost invisible in the middle).")]
        [Range(0.005f, 0.15f)] [SerializeField] private float _beamMidAlpha = 0.06f;

        [Tooltip("Additive alpha where the far edge of the scan beam wedge meets the current surface scan line on the target.")]
        [Range(0.04f, 0.65f)] [SerializeField] private float _beamContactAlpha = 0.6f;

        [Tooltip("Fine scanline band density inside the scan beam wedge.")]
        [Range(12f, 120f)] [SerializeField] private float _beamScanlineDensity = 42f;

        [Tooltip("Contrast (0..1) of the fine scanline texture inside the scan beam wedge.")]
        [Range(0.05f, 0.85f)] [SerializeField] private float _beamScanlineContrast = 0.48f;

        [Tooltip("Diameter (m) of the opening cyan iris ring on 07's brass cheek lens while scanning.")]
        [Range(0.04f, 0.4f)] [SerializeField] private float _lensIrisSize = 0.138f;

        [Tooltip("Peak alpha of the opening cyan iris ring on 07's brass cheek lens.")]
        [Range(0.05f, 1f)] [SerializeField] private float _lensIrisAlpha = 0.82f;

        [Tooltip("Length (m) of the short optical cross-glint flare needles clamped within 07's cheek lens bezel.")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _lensGlintLength = 0.068f;

        [Tooltip("Width (m) of the optical cross-glint flare needles on 07's cheek lens.")]
        [Range(0.003f, 0.05f)] [SerializeField] private float _lensGlintWidth = 0.008f;

        [Tooltip("Peak alpha of the optical cross-glint flare on 07's cheek lens.")]
        [Range(0.05f, 1f)] [SerializeField] private float _lensGlintAlpha = 0.76f;

        [Tooltip("Number of sparse dust grains catching the laser sparkle within a thin band of the beam wedge.")]
        [Range(0, 16)] [SerializeField] private int _pathGlintCount = 5;

        [Tooltip("Diameter (m) of each tiny dust grain sparkle along the beam wedge (1-3 px at chase distance).")]
        [Range(0.004f, 0.08f)] [SerializeField] private float _pathGlintSize = 0.016f;

        [Tooltip("Peak alpha of each brief dust grain sparkle along the beam wedge (capped strictly below bloom threshold).")]
        [Range(0f, 1f)] [SerializeField] private float _pathGlintAlpha = 0.32f;

        [Tooltip("Brief twinkle lifetime (s) of each ballistic dust grain sparkle (0.1-0.3 s).")]
        [Range(0.1f, 0.3f)] [SerializeField] private float _pathGlintLifetime = 0.18f;

        [Tooltip("Downward lunar gravity acceleration (m/s^2, 1/6 g = 1.62) for the ballistic dust grains along the scan path.")]
        [Range(0.2f, 5f)] [SerializeField] private float _pathGlintGravity = 1.62f;

        [Tooltip("The finer shape of the scan's light: the cheek lens, the wedge's fan, the data ticks, the dust " +
                 "glints and the closing ribbon.")]
        [SerializeField] private ScanBeamLook _beamLook = new ScanBeamLook();

        [Header("Lumen station ruins (RUINS-WORLD, opening-v2 §4)")]
        [Tooltip("Each R1 ruin piece's Scan_1..N targets and the structure blueprints it teaches once every one of " +
                 "them is scanned (the workshop and the scatter teach nothing: they hold lockers and panels).")]
        [SerializeField] private RuinScanSet[] _ruins =
        {
            new RuinScanSet("FallenSolarArray", new[] { StructureIds.SolarArray },
                ScanSize.Small, ScanSize.Small, ScanSize.Medium),
            new RuinScanSet("HabitatModules", new[] { StructureIds.BatteryBank },
                ScanSize.Medium, ScanSize.Medium),
            new RuinScanSet("RadioTower", new[] { StructureIds.AntennaMast },
                ScanSize.Medium, ScanSize.Medium),
        };

        [Tooltip("The landed lander's cargo bay (opening-v2 B3): its Scan_1..N targets, which Gameplay stands at " +
                 "OpeningTuning's cargo points, teach the charging pad.")]
        [SerializeField] private RuinScanSet _landerCargo = new RuinScanSet(LanderCargoId,
            new[] { StructureIds.ChargingPad }, ScanSize.Medium, ScanSize.Medium);

        [Tooltip("Story scans on the ruin pieces (opening-v2 B7) and the basin's story props (basin dressing §4): " +
                 "Art's mesh nodes whose readout the CRT shows once scanned; they teach and pay nothing.")]
        [SerializeField] private StoryScan[] _stories =
        {
            new StoryScan("airlock", "HabitatModules", "AirlockPanel", "story.airlock_panel", ScanSize.Small,
                new Vector3(0f, -0.3f, 0f)),
            new StoryScan("scout3", "Prop_OldLander", "Spaceframe", "scan.scout3.manifest", ScanSize.Medium,
                Vector3.zero),
            new StoryScan("seismic", "Prop_SeismicStation", "CentralBox", "db.seismic_station.readout",
                ScanSize.Medium, Vector3.zero),
            new StoryScan("retroreflector", "Prop_Retroreflector", "PanelHousing", "db.retroreflector.readout",
                ScanSize.Medium, Vector3.zero),
            new StoryScan("flag", "Prop_MissionFlag", "FlagSheet", "db.card_ilc.readout", ScanSize.Small,
                Vector3.zero),
            new StoryScan("kestrel3", "Kestrel3_HullPanel", "Kestrel_HullPanel", "db.kestrel3.readout",
                ScanSize.Medium, Vector3.zero),
            new StoryScan("elder06", "Elder06", "Body", "elder.06.manifest", ScanSize.Medium, Vector3.zero),
            new StoryScan("elder01", "Elder01", "Body", "db.elder01.readout", ScanSize.Medium, Vector3.zero),
        };

        private static readonly int ScanLineColorShaderId = Shader.PropertyToID("_MoonScanLineColor");
        private static readonly int ScanPingColorShaderId = Shader.PropertyToID("_MoonScanPingColor");
        private static readonly int ScanParamsShaderId = Shader.PropertyToID("_MoonScanParams");
        private static readonly int ScanPingParamsShaderId = Shader.PropertyToID("_MoonScanPingParams");

        public IReadOnlyList<RuinScanSet> Ruins => _ruins;

        /// <summary>The lander cargo bay's scan set (the charging pad's fragments).</summary>
        public RuinScanSet LanderCargo => _landerCargo;

        public IReadOnlyList<StoryScan> Stories => _stories;
        public float Reach => _reach;
        public float Cone => _cone;
        public float LookStickiness => _lookStickiness;
        public float DistanceWeight => _distanceWeight;
        public float EngageSpeed => _engageSpeed;
        public int GazePriority => _gazePriority;
        public float AimToleranceDegrees => _aimToleranceDegrees;
        public float DissolveSeconds => _dissolveSeconds;
        public int KnownMetal => _knownMetal;
        public Color ScanLineColor => _scanLineColor;
        public Color CompletionPingColor => _completionPingColor;
        public float ScanLineWidth => _scanLineWidth;
        public float PingDurationSeconds => _pingDurationSeconds;
        public float PingEdgeThickness => _pingEdgeThickness;
        public float PingEdgeIntensity => _pingEdgeIntensity;
        public float SurfaceGridPitchMeters => _surfaceGridPitchMeters;
        public float WireframeRevealIntensity => _wireframeRevealIntensity;
        public float BeamPulseSpeed => _beamPulseSpeed;
        public float LensGlowSize => _lensGlowSize;
        public float LensGlowAlpha => _lensGlowAlpha;
        public float LensHaloSize => _lensHaloSize;
        public float LensHaloAlpha => _lensHaloAlpha;
        public float LensMoteRadius => _lensMoteRadius;
        public float LensMoteSize => _lensMoteSize;
        public float LensMoteAlpha => _lensMoteAlpha;
        public float BeamLensWidth => _beamLensWidth;
        public float BeamTargetWidthShare => _beamTargetWidthShare;
        public float BeamLensPeakAlpha => _beamLensPeakAlpha;
        public float BeamMidAlpha => _beamMidAlpha;
        public float BeamContactAlpha => _beamContactAlpha;
        public float BeamScanlineDensity => _beamScanlineDensity;
        public float BeamScanlineContrast => _beamScanlineContrast;
        public float LensIrisSize => _lensIrisSize;
        public float LensIrisAlpha => _lensIrisAlpha;
        public float LensGlintLength => _lensGlintLength;
        public float LensGlintWidth => _lensGlintWidth;
        public float LensGlintAlpha => _lensGlintAlpha;
        public int PathGlintCount => _pathGlintCount;
        public float PathGlintSize => _pathGlintSize;
        public float PathGlintAlpha => _pathGlintAlpha;
        public float PathGlintLifetime => _pathGlintLifetime;
        public float PathGlintGravity => _pathGlintGravity;
        public ScanBeamLook BeamLook => _beamLook;

        /// <summary>Pushes the wireframe reveal and completion ping tuning to shader globals (zero GC).</summary>
        public void ApplyShaderGlobals()
        {
            Shader.SetGlobalColor(ScanLineColorShaderId, _scanLineColor.linear);
            Shader.SetGlobalColor(ScanPingColorShaderId, _completionPingColor.linear);
            Shader.SetGlobalVector(ScanParamsShaderId,
                new Vector4(_scanLineWidth, _surfaceGridPitchMeters, _wireframeRevealIntensity, _pingEdgeThickness));
            Shader.SetGlobalVector(ScanPingParamsShaderId,
                new Vector4(_pingDurationSeconds, _pingEdgeIntensity, 0f, 0f));
        }

        /// <summary>Seconds a fragment of <paramref name="size"/> takes to scan with the plain eye.</summary>
        public float Seconds(ScanSize size)
        {
            switch (size)
            {
                case ScanSize.Small:
                    return _smallSeconds;
                case ScanSize.Medium:
                    return _mediumSeconds;
                case ScanSize.Large:
                    return _largeSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, "Not a scan size.");
            }
        }

        /// <summary>The ruin scan set for <paramref name="ruinId"/>, or false.</summary>
        public bool TryGetRuinSet(string ruinId, out RuinScanSet set)
        {
            for (int i = 0; i < _ruins.Length; i++)
            {
                if (string.Equals(_ruins[i].RuinId, ruinId, StringComparison.Ordinal))
                {
                    set = _ruins[i];
                    return true;
                }
            }

            set = default;
            return false;
        }
    }
}
