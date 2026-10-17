using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scanning (docs/design/0.5.5-owner-feedback.md §1): the blueprints' fragment sets, how long a fragment takes to
    /// scan by its size, how far its corner brackets show, and the closing dissolve. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class ScanTuning : ScriptableObject
    {
        [Header("The sets (§1.3)")]
        [Tooltip("Every blueprint's fragments, in Art's set names; the 18 of 0.5.")]
        [SerializeField] private ScanSet[] _sets =
        {
            new ScanSet("CargoCradle", "rover.cargo_cradle", ScanSize.Medium, ScanSize.Medium),
            new ScanSet("WarmHeadlamp", "rover.warm_headlamp", ScanSize.Small, ScanSize.Medium),
            new ScanSet("HoverJump", "rover.hover_jump", ScanSize.Small, ScanSize.Medium, ScanSize.Medium),
            new ScanSet("BoostCoils", "rover.boost_coils", ScanSize.Medium, ScanSize.Medium, ScanSize.Large),
            new ScanSet("CellPack", "rover.cell_pack", ScanSize.Small, ScanSize.Small, ScanSize.Medium),
            new ScanSet("FocusLens", "rover.focus_lens", ScanSize.Small, ScanSize.Small, ScanSize.Small),
            new ScanSet("SideBins", "rover.side_bins", ScanSize.Medium, ScanSize.Large),
        };

        [Header("The verb (§1.2)")]
        [Tooltip("Seconds to scan a small fragment (a lamp, a lens).")]
        [Range(0.3f, 10f)] [SerializeField] private float _smallSeconds = 1.5f;

        [Tooltip("Seconds to scan a medium fragment.")]
        [Range(0.3f, 10f)] [SerializeField] private float _mediumSeconds = 2.5f;

        [Tooltip("Seconds to scan a large fragment (a capacitor bank, a pannier).")]
        [Range(0.3f, 10f)] [SerializeField] private float _largeSeconds = 4f;

        [Tooltip("How much faster the Focus Lens makes every scan (§1.6).")]
        [Range(1f, 3f)] [SerializeField] private float _lensFactor = 1.5f;

        [Tooltip("Metres within which a fragment still to scan shows its corner brackets.")]
        [Range(2f, 60f)] [SerializeField] private float _bracketRange = 15f;

        [Tooltip("The same with the Focus Lens fitted (§1.6).")]
        [Range(2f, 60f)] [SerializeField] private float _lensBracketRange = 25f;

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

        [Tooltip("Seconds the faint dust outline left on the spot takes to fade.")]
        [Range(0.3f, 6f)] [SerializeField] private float _outlineSeconds = 2f;

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
        [Range(0.01f, 0.35f)] [SerializeField] private float _lensGlowSize = 0.085f;

        [Tooltip("Alpha of the soft emitter glow core at 07's scan lens.")]
        [Range(0.02f, 1f)] [SerializeField] private float _lensGlowAlpha = 0.94f;

        [Tooltip("Diameter of the soft outer halo at 07's scan lens (m).")]
        [Range(0.02f, 0.6f)] [SerializeField] private float _lensHaloSize = 0.22f;

        [Tooltip("Alpha of the soft outer halo at 07's scan lens.")]
        [Range(0.01f, 0.8f)] [SerializeField] private float _lensHaloAlpha = 0.36f;

        [Tooltip("Maximum radius from 07's lens where tiny motes sparkle in vacuum (m).")]
        [Range(0.01f, 0.16f)] [SerializeField] private float _lensMoteRadius = 0.065f;

        [Tooltip("Diameter of the tiny motes sparkling right at 07's lens (m).")]
        [Range(0.004f, 0.06f)] [SerializeField] private float _lensMoteSize = 0.020f;

        [Tooltip("Peak alpha of the tiny motes sparkling right at 07's lens.")]
        [Range(0f, 1f)] [SerializeField] private float _lensMoteAlpha = 0.65f;

        [Header("07 scanner & ground link (SCAN v3)")]
        [Tooltip("World-space width of the crisp bright laser intersection line on the ground from under 07's lens to the target's base (m).")]
        [Range(0.005f, 0.10f)] [SerializeField] private float _groundLineWidth = 0.032f;

        [Tooltip("Emissive intensity of the crisp ground laser line linking 07's lens to the target's base.")]
        [Range(0.2f, 4f)] [SerializeField] private float _groundLineIntensity = 2.05f;

        [Tooltip("Low-contrast intensity of the swept fan wake and range ticks on the regolith between 07 and the target.")]
        [Range(0f, 1f)] [SerializeField] private float _groundWakeIntensity = 0.38f;

        [Tooltip("Half-angle (deg) of the vertical laser fan's horizontal sweep across the ground and target.")]
        [Range(0.5f, 15f)] [SerializeField] private float _groundSweepAngleDeg = 4.2f;

        [Tooltip("Oscillation frequency (Hz) of the vertical laser fan sweep across the ground and target.")]
        [Range(0.2f, 4f)] [SerializeField] private float _groundSweepHz = 1.15f;

        [Tooltip("Forward stand-off (m) from under 07's cheek lens where the ground laser line begins on the regolith.")]
        [Range(0.02f, 1.0f)] [SerializeField] private float _groundStartOffset = 0.08f;

        [Tooltip("Diameter (m) of the opening cyan iris ring on 07's brass cheek lens while scanning.")]
        [Range(0.04f, 0.4f)] [SerializeField] private float _lensIrisSize = 0.145f;

        [Tooltip("Peak alpha of the opening cyan iris ring on 07's brass cheek lens.")]
        [Range(0.05f, 1f)] [SerializeField] private float _lensIrisAlpha = 0.90f;

        [Tooltip("Length (m) of the optical cross-glint flare streaks on 07's cheek lens.")]
        [Range(0.05f, 0.8f)] [SerializeField] private float _lensGlintLength = 0.44f;

        [Tooltip("Width (m) of the optical cross-glint flare streaks on 07's cheek lens.")]
        [Range(0.005f, 0.15f)] [SerializeField] private float _lensGlintWidth = 0.015f;

        [Tooltip("Peak alpha of the optical cross-glint flare on 07's cheek lens.")]
        [Range(0.05f, 1f)] [SerializeField] private float _lensGlintAlpha = 0.92f;

        [Tooltip("Intensity of the soft cyan point light spilling onto 07's face, brass bezel, and the ground right in front of the lens.")]
        [Range(0f, 8f)] [SerializeField] private float _lensSpillIntensity = 0.65f;

        [Tooltip("Range (m) of the soft cyan point light spilling onto 07's face, brass bezel, and the ground right in front of the lens.")]
        [Range(0.5f, 8f)] [SerializeField] private float _lensSpillRange = 0.95f;

        [Tooltip("Number of sparse dust grains catching the laser sparkle along the beam path from 07's lens to the target.")]
        [Range(0, 16)] [SerializeField] private int _pathGlintCount = 10;

        [Tooltip("Diameter (m) of each sparse dust grain sparkle along the beam path.")]
        [Range(0.008f, 0.15f)] [SerializeField] private float _pathGlintSize = 0.082f;

        [Tooltip("Peak alpha of each brief dust grain sparkle along the beam path.")]
        [Range(0f, 1f)] [SerializeField] private float _pathGlintAlpha = 0.96f;

        [Tooltip("Brief lifetime (s) of each ballistic dust grain sparkle before falling out of the laser plane.")]
        [Range(0.12f, 1.2f)] [SerializeField] private float _pathGlintLifetime = 0.42f;

        [Tooltip("Downward lunar gravity acceleration (m/s^2, 1/6 g = 1.62) for the ballistic dust grains along the scan path.")]
        [Range(0.2f, 5f)] [SerializeField] private float _pathGlintGravity = 1.62f;

        [Header("The chasm's near lip (§1.3)")]
        [Tooltip("Metres back from the canyon lip, toward the basin, where Kenji's crashed test jumper lies.")]
        [Range(0f, 30f)] [SerializeField] private float _jumperSetback = 6f;

        [Tooltip("Metres to the left of the leap's run-up line (looking at the chasm) the jumper lies: clear of the " +
                 "lane 07 runs at the lip and of the bridge's stub on its right, inside the chasm's floor.")]
        [Range(0f, 10f)] [SerializeField] private float _jumperSide = 6f;

        [Header("Lumen station ruins (RUINS-WORLD)")]
        [Tooltip("Each R1 ruin piece's Scan_1..N targets and the structure blueprints it teaches when fully scanned.")]
        [SerializeField] private RuinScanSet[] _ruins =
        {
            new RuinScanSet("FallenSolarArray", new[] { StructureIds.SolarArray },
                ScanSize.Small, ScanSize.Small, ScanSize.Medium),
            new RuinScanSet("KenjiWorkshop", new[] { StructureIds.Fabricator, StructureIds.StorageRack },
                ScanSize.Small, ScanSize.Medium, ScanSize.Medium),
            new RuinScanSet("HabitatModules", new[] { StructureIds.BatteryBank, StructureIds.Smelter },
                ScanSize.Medium, ScanSize.Medium, ScanSize.Large),
            new RuinScanSet("RadioTower", new[] { StructureIds.AntennaMast, StructureIds.PowerPylon },
                ScanSize.Medium, ScanSize.Medium),
            new RuinScanSet("ScatterSet", Array.Empty<string>(),
                ScanSize.Small, ScanSize.Small, ScanSize.Small),
        };

        [Tooltip("Structure blueprints whose scan targets belong to places later in the map (not at the home ruins).")]
        [SerializeField] private string[] _laterStructureIds =
        {
            StructureIds.UpgradeBay,
            StructureIds.VehiclePad,
        };

        private static readonly int ScanLineColorShaderId = Shader.PropertyToID("_MoonScanLineColor");
        private static readonly int ScanPingColorShaderId = Shader.PropertyToID("_MoonScanPingColor");
        private static readonly int ScanParamsShaderId = Shader.PropertyToID("_MoonScanParams");
        private static readonly int ScanPingParamsShaderId = Shader.PropertyToID("_MoonScanPingParams");
        private static readonly int ScanGroundStartShaderId = Shader.PropertyToID("_MoonScanGroundStart");
        private static readonly int ScanGroundEndShaderId = Shader.PropertyToID("_MoonScanGroundEnd");
        private static readonly int ScanGroundParamsShaderId = Shader.PropertyToID("_MoonScanGroundParams");

        public IReadOnlyList<ScanSet> Sets => _sets;
        public IReadOnlyList<RuinScanSet> Ruins => _ruins;
        public IReadOnlyList<string> LaterStructureIds => _laterStructureIds;
        public float LensFactor => _lensFactor;
        public float BracketRange => _bracketRange;
        public float LensBracketRange => _lensBracketRange;
        public float Reach => _reach;
        public float Cone => _cone;
        public float DistanceWeight => _distanceWeight;
        public float EngageSpeed => _engageSpeed;
        public int GazePriority => _gazePriority;
        public float AimToleranceDegrees => _aimToleranceDegrees;
        public float DissolveSeconds => _dissolveSeconds;
        public float OutlineSeconds => _outlineSeconds;
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
        public float GroundLineWidth => _groundLineWidth;
        public float GroundLineIntensity => _groundLineIntensity;
        public float GroundWakeIntensity => _groundWakeIntensity;
        public float GroundSweepAngleDeg => _groundSweepAngleDeg;
        public float GroundSweepHz => _groundSweepHz;
        public float GroundStartOffset => _groundStartOffset;
        public float LensIrisSize => _lensIrisSize;
        public float LensIrisAlpha => _lensIrisAlpha;
        public float LensGlintLength => _lensGlintLength;
        public float LensGlintWidth => _lensGlintWidth;
        public float LensGlintAlpha => _lensGlintAlpha;
        public float LensSpillIntensity => _lensSpillIntensity;
        public float LensSpillRange => _lensSpillRange;
        public int PathGlintCount => _pathGlintCount;
        public float PathGlintSize => _pathGlintSize;
        public float PathGlintAlpha => _pathGlintAlpha;
        public float PathGlintLifetime => _pathGlintLifetime;
        public float PathGlintGravity => _pathGlintGravity;
        public float JumperSetback => _jumperSetback;
        public float JumperSide => _jumperSide;

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

        /// <summary>Updates the projected ground laser fan intersection shader globals for the current frame (zero GC).</summary>
        public static void SetGroundLineGlobals(Vector4 groundStartAndActive, Vector4 groundEndAndProgress,
            Vector4 groundParams)
        {
            Shader.SetGlobalVector(ScanGroundStartShaderId, groundStartAndActive);
            Shader.SetGlobalVector(ScanGroundEndShaderId, groundEndAndProgress);
            Shader.SetGlobalVector(ScanGroundParamsShaderId, groundParams);
        }

        /// <summary>Clears the projected ground laser fan intersection shader globals when no scan is active (zero GC).</summary>
        public static void ClearGroundLineGlobals()
        {
            Shader.SetGlobalVector(ScanGroundStartShaderId, Vector4.zero);
            Shader.SetGlobalVector(ScanGroundEndShaderId, Vector4.zero);
            Shader.SetGlobalVector(ScanGroundParamsShaderId, Vector4.zero);
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

        /// <summary>The set named <paramref name="blueprint"/> in Art's names, or false.</summary>
        public bool TryGetSet(string blueprint, out ScanSet set)
        {
            for (int i = 0; i < _sets.Length; i++)
            {
                if (string.Equals(_sets[i].Blueprint, blueprint, StringComparison.Ordinal))
                {
                    set = _sets[i];
                    return true;
                }
            }

            set = default;
            return false;
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

        private void OnValidate()
        {
            if (_laterStructureIds == null)
            {
                _laterStructureIds = Array.Empty<string>();
                return;
            }

            int validCount = 0;
            for (int i = 0; i < _laterStructureIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(_laterStructureIds[i]))
                {
                    validCount++;
                }
            }

            if (validCount != _laterStructureIds.Length)
            {
                var cleaned = new string[validCount];
                int dst = 0;
                for (int i = 0; i < _laterStructureIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(_laterStructureIds[i]))
                    {
                        cleaned[dst++] = _laterStructureIds[i];
                    }
                }

                _laterStructureIds = cleaned;
            }
        }
    }
}
