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

        [Tooltip("How much nearness weighs against looking straight at a fragment when two are in view.")]
        [Range(0f, 1f)] [SerializeField] private float _distanceWeight = 0.2f;

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
        [Tooltip("Cyan colour of the crisp surface scan line and low-contrast swept raster band (same palette as the face).")]
        [SerializeField] private Color _scanLineColor = new Color(0.20f, 0.86f, 0.96f, 1f);

        [Tooltip("Warm amber accent colour for the completion ping outline and lens pulse (same palette as the CRT face).")]
        [SerializeField] private Color _completionPingColor = new Color(1.00f, 0.682f, 0.220f, 1f);

        [Tooltip("World-space half-width of the crisp bright scan line where the laser touches the object (m).")]
        [Range(0.003f, 0.05f)] [SerializeField] private float _scanLineWidth = 0.014f;

        [Tooltip("Emissive intensity of the crisp surface scan line (calm, under bloom blowout).")]
        [Range(0.1f, 2f)] [SerializeField] private float _scanLineIntensity = 1.35f;

        [Tooltip("World-space vertical spacing between anti-aliased horizontal surface raster lines (m), chosen so they never alias at chase distance.")]
        [Range(0.02f, 0.2f)] [SerializeField] private float _rasterSpacingMeters = 0.055f;

        [Tooltip("Low-contrast intensity of the swept surface raster band behind the scan line.")]
        [Range(0.02f, 0.5f)] [SerializeField] private float _rasterContrast = 0.18f;

        [Tooltip("Seconds over which the swept surface raster band fades out behind the moving scan line.")]
        [Range(0.2f, 3f)] [SerializeField] private float _rasterFadeSeconds = 1.0f;

        [Tooltip("Seconds the thin anti-aliased warm-amber edge traces the object at the completion ping before fading.")]
        [Range(0.1f, 0.9f)] [SerializeField] private float _pingDurationSeconds = 0.38f;

        [Tooltip("Screen-derivative thickness multiplier for the thin anti-aliased completion ping edge.")]
        [Range(0.5f, 3f)] [SerializeField] private float _pingEdgeThickness = 1.35f;

        [Tooltip("Emissive intensity of the warm-amber completion ping edge.")]
        [Range(0.1f, 2f)] [SerializeField] private float _pingEdgeIntensity = 1.25f;

        [Tooltip("Diameter of the soft emitter glow core at 07's scan lens (m).")]
        [Range(0.01f, 0.2f)] [SerializeField] private float _lensGlowSize = 0.06f;

        [Tooltip("Alpha of the soft emitter glow core at 07's scan lens.")]
        [Range(0.02f, 1f)] [SerializeField] private float _lensGlowAlpha = 0.32f;

        [Tooltip("Diameter of the soft outer halo at 07's scan lens (m).")]
        [Range(0.02f, 0.4f)] [SerializeField] private float _lensHaloSize = 0.13f;

        [Tooltip("Alpha of the soft outer halo at 07's scan lens.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _lensHaloAlpha = 0.08f;

        [Tooltip("Maximum radius from 07's lens where tiny motes sparkle in vacuum (m).")]
        [Range(0.01f, 0.12f)] [SerializeField] private float _lensMoteRadius = 0.042f;

        [Tooltip("Diameter of the tiny motes sparkling right at 07's lens (m).")]
        [Range(0.004f, 0.04f)] [SerializeField] private float _lensMoteSize = 0.014f;

        [Tooltip("Peak alpha of the tiny motes sparkling right at 07's lens.")]
        [Range(0f, 0.5f)] [SerializeField] private float _lensMoteAlpha = 0.16f;

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

        [Tooltip("Story scans on the ruin pieces (opening-v2 B7): Art's mesh nodes whose readout the CRT shows once " +
                 "scanned; they teach and pay nothing.")]
        [SerializeField] private StoryScan[] _stories =
        {
            new StoryScan("airlock", "HabitatModules", "AirlockPanel", "story.airlock_panel", ScanSize.Small),
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
        public float DistanceWeight => _distanceWeight;
        public float EngageSpeed => _engageSpeed;
        public int GazePriority => _gazePriority;
        public float AimToleranceDegrees => _aimToleranceDegrees;
        public float DissolveSeconds => _dissolveSeconds;
        public int KnownMetal => _knownMetal;
        public Color ScanLineColor => _scanLineColor;
        public Color CompletionPingColor => _completionPingColor;
        public float ScanLineWidth => _scanLineWidth;
        public float ScanLineIntensity => _scanLineIntensity;
        public float RasterSpacingMeters => _rasterSpacingMeters;
        public float RasterContrast => _rasterContrast;
        public float RasterFadeSeconds => _rasterFadeSeconds;
        public float PingDurationSeconds => _pingDurationSeconds;
        public float PingEdgeThickness => _pingEdgeThickness;
        public float PingEdgeIntensity => _pingEdgeIntensity;
        public float LensGlowSize => _lensGlowSize;
        public float LensGlowAlpha => _lensGlowAlpha;
        public float LensHaloSize => _lensHaloSize;
        public float LensHaloAlpha => _lensHaloAlpha;
        public float LensMoteRadius => _lensMoteRadius;
        public float LensMoteSize => _lensMoteSize;
        public float LensMoteAlpha => _lensMoteAlpha;

        /// <summary>Pushes the surface scan line, raster band, and completion ping tuning to shader globals (zero GC).</summary>
        public void ApplyShaderGlobals(float scanDurationSeconds)
        {
            float duration = Mathf.Max(scanDurationSeconds, 0.25f);
            float fadeBandShare = Mathf.Clamp(_rasterFadeSeconds / duration, 0.08f, 0.95f);

            Shader.SetGlobalColor(ScanLineColorShaderId, _scanLineColor.linear);
            Shader.SetGlobalColor(ScanPingColorShaderId, _completionPingColor.linear);
            Shader.SetGlobalVector(ScanParamsShaderId,
                new Vector4(_scanLineWidth, _rasterSpacingMeters, _rasterContrast, fadeBandShare));
            Shader.SetGlobalVector(ScanPingParamsShaderId,
                new Vector4(_scanLineIntensity, _pingDurationSeconds, _pingEdgeThickness, _pingEdgeIntensity));
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
