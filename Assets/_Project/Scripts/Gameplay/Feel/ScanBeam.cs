using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's stylised scan beam wedge, live cheek lens emitter, and ballistic path glints (Task SCAN v3):
    /// 1. Projects a narrow, additive tapered scan fan wedge from 07's cheek lens (<c>ScanOrigin</c>) to the target,
    ///    with its far edge spanning and climbing the current surface scan line on the target in lockstep with
    ///    <c>_ScanProgress</c>. Brightest in the first couple of cm leaving the lens and where it meets the target,
    ///    almost invisible in the middle, with soft feathered edges and fine internal scanline texture.
    /// 2. Keeps 07's brass cheek lens alive: opening iris ring, hot diode core, short bezel-clamped flare needles,
    ///    and a soft cyan point light spilling onto 07's face and bezel, all capped below bloom blowout.
    /// 3. Spawns sparse, brief dust grain glints strictly inside a thin band of the beam wedge that fall ballistically
    ///    under 1/6 g lunar gravity, with emission capped well below the bloom threshold.
    /// Its shape is <see cref="ScanTuning"/> and its <see cref="ScanBeamLook"/>. Zero allocations per frame.
    /// </summary>
    public sealed class ScanBeam
    {
        private const int CorePointCount = 3;
        private const int IrisRingCount = 12;
        private const int IrisCaliperCount = 2;
        private const int MoteCount = 6;
        private const int MaxPathGlints = 16;
        private const int PointSlots = CorePointCount + IrisRingCount + IrisCaliperCount + MoteCount + MaxPathGlints;
        private const int LensStreakCount = 4;
        private const int DataTickCount = 12;
        private const int DataTickLanes = 3;

        /// <summary>Share of the teal ping colour in the lens's cyan while it sweeps (the wireframe it reveals is teal).</summary>
        private const float BeamPingShare = 0.35f;
        private const int StreakSlots = LensStreakCount + DataTickCount;

        private const int WedgeRings = 16;
        private const int WedgeSlices = 9;
        private const int WedgeSheetCount = 2;
        private const int VertsPerSheet = WedgeRings * WedgeSlices;
        private const int TotalWedgeVerts = VertsPerSheet * WedgeSheetCount;

        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float MinProfileLength = 0.1f;
        private const float MinWidthShare = 0.65f;
        private const float WedgeBoundsPadding = 2f;
        private const float TwoPi = Mathf.PI * 2f;

        private const float MaxBloomSafeHaloAlpha = 0.24f;
        private const float MaxBloomSafeCoreAlpha = 0.72f;
        private const float MaxBloomSafeIrisAlpha = 0.72f;
        private const float MaxBloomSafeStreakAlpha = 0.70f;
        private const float MaxBloomSafeGlintAlpha = 0.22f;

        private const float LensCoreDecayMeters = 0.048f;
        private const float LensNozzleDecayMeters = 0.095f;
        private const float LensNozzleShare = 0.46f;
        private const float ContactGlowDecayMeters = 0.42f;
        private const float ContactGlowShare = 0.56f;
        private const float ContactLipDecayMeters = 0.08f;
        private const float ContactLipShare = 0.44f;
        private const float WedgeMaterialIntensity = 1.35f;
        private const float WedgeAcrossFade = 0.42f;
        private const float WedgeBandSpeed = 2.4f;
        private const float WedgeArchHeight = 0.012f;

        private const float IrisClosedScale = 0.38f;
        private const float PathStartShare = 0.18f;
        private const float PathEndShare = 0.88f;
        private const float PathActiveDutyCycle = 0.50f;
        private const float PathWedgeWidthShare = 0.55f;
        private const float PathVerticalBandHalfHeight = 0.014f;
        private const float PathUpwardLoftVelocity = 0.010f;

        // Non-uniform longitudinal ring stations (0..1) with extra resolution in the first few cm at the lens
        // and at the target contact lip, and exact midpoint at index 8 (0.500f).
        private static readonly float[] RingFractions =
        {
            0.000f, 0.004f, 0.009f, 0.015f, 0.024f, 0.055f,
            0.140f, 0.290f, 0.500f, 0.680f, 0.820f, 0.910f,
            0.955f, 0.980f, 0.992f, 1.000f
        };

        private static readonly int ColorShaderId = Shader.PropertyToID("_Color");
        private static readonly int IntensityShaderId = Shader.PropertyToID("_Intensity");
        private static readonly int AcrossInShaderId = Shader.PropertyToID("_AcrossIn");
        private static readonly int AcrossOutShaderId = Shader.PropertyToID("_AcrossOut");
        private static readonly int LengthFadeShaderId = Shader.PropertyToID("_LengthFade");
        private static readonly int BandCountShaderId = Shader.PropertyToID("_BandCount");
        private static readonly int BandSpeedShaderId = Shader.PropertyToID("_BandSpeed");
        private static readonly int BandStrengthShaderId = Shader.PropertyToID("_BandStrength");
        private static readonly int FresnelMixShaderId = Shader.PropertyToID("_FresnelMix");
        private static readonly int CoreMixShaderId = Shader.PropertyToID("_CoreMix");
        private static readonly int RadialMaskShaderId = Shader.PropertyToID("_RadialMask");
        private static readonly int SrcBlendShaderId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendShaderId = Shader.PropertyToID("_DstBlend");
        private static readonly int CullShaderId = Shader.PropertyToID("_Cull");

        private readonly FeelVfxTuning _tuning;
        private readonly ScanTuning _scanTuning;
        private readonly ScanBeamLook _look;
        private readonly Material _wedgeMaterial;
        private readonly Mesh _wedgeMesh;
        private readonly MeshRenderer _wedgeRenderer;
        private readonly Vector3[] _wedgeVertices = new Vector3[TotalWedgeVerts];
        private readonly Vector3[] _wedgeNormals = new Vector3[TotalWedgeVerts];
        private readonly Vector2[] _wedgeUvs = new Vector2[TotalWedgeVerts];
        private readonly Color[] _wedgeColors = new Color[TotalWedgeVerts];
        private readonly FeelSprites _points;
        private readonly FeelSprites _streaks;

        private Vector3 _target;
        private Vector3 _span;
        private Vector3 _lastContactCenter;
        private Vector3 _contact;
        private bool _hasContact;
        private float _lastTargetWidth;
        private float _progress;
        private float _rate;
        private float _level;
        private float _elapsed;
        private float _pingRemaining;
        private float _pingDuration;
        private bool _active;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial,
            ScanTuning scanTuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning != null ? scanTuning : throw new ArgumentNullException(nameof(scanTuning));
            _look = scanTuning.BeamLook;
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

            _pingDuration = scanTuning.PingDurationSeconds;
            _wedgeMaterial = CreateWedgeMaterial(beamMaterial, _scanTuning);
            _wedgeMesh = CreateWedgeMesh(parent, _wedgeMaterial, out _wedgeRenderer);
            _points = new FeelSprites("ScanBeamPoints", parent, spriteMaterial, PointSlots, false, 0f);
            _streaks = new FeelSprites("ScanBeamStreaks", parent, spriteMaterial, StreakSlots, true, 1f);
        }

        /// <summary>True while the scan beam wedge, lens emitter glow, or completion ping is active or easing out.</summary>
        public bool Shown => _level > VisibleThreshold || _pingRemaining > VisibleThreshold;

        /// <summary>Current eased visibility level (0..1).</summary>
        public float Level => Ease.InOutSine(_level);

        /// <summary>True while the completion ping flash is active.</summary>
        public bool IsPinging => _pingRemaining > VisibleThreshold;

        /// <summary>World-space center of the far edge of the scan beam wedge on the target's current scan line.</summary>
        public Vector3 WedgeTargetCenter => _lastContactCenter;

        /// <summary>Starts or resumes the scan beam wedge and lens emitter at <paramref name="progress"/> (0..1) over <paramref name="remainingSeconds"/>.</summary>
        public void Begin(Vector3 target, Vector3 span, float progress, float remainingSeconds)
        {
            _target = target;
            _span = span;
            _hasContact = false;
            _progress = Mathf.Clamp01(progress);
            _rate = remainingSeconds > MinFadeSeconds ? (1f - _progress) / remainingSeconds : 0f;
            _pingRemaining = 0f;
            _active = true;
        }

        /// <summary>
        /// The beam's far end this frame: where it meets its target's real surface (set before
        /// <see cref="Step"/>). Ignored once the scan ends, completes or is let go: the fading beam stays where it last
        /// met its target, which may be dissolving, collected or gone.
        /// </summary>
        public void Meet(Vector3 contact)
        {
            if (!_active)
            {
                return;
            }

            _contact = contact;
            _hasContact = true;
        }

        /// <summary>Keeps progress in step with quarter beats.</summary>
        public void Sync(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>Eases the scan beam and lens glow out on let-go.</summary>
        public void End()
        {
            _active = false;
        }

        /// <summary>Triggers the single clean completion ping pulse at the lens, then fades out.</summary>
        public void Complete()
        {
            _progress = 1f;
            _active = false;
            _pingDuration = _scanTuning.PingDurationSeconds;
            _pingRemaining = _pingDuration;
        }

        public void Step(float deltaTime, Vector3 emitter)
        {
            _elapsed += deltaTime;
            float fadeSeconds = _active ? _tuning.ScanEaseInSeconds : _tuning.ScanEaseOutSeconds;
            _level = Mathf.MoveTowards(_level, _active ? 1f : 0f, deltaTime / Mathf.Max(fadeSeconds, MinFadeSeconds));
            if (_active)
            {
                _progress = Mathf.Clamp01(_progress + _rate * deltaTime);
            }

            if (_pingRemaining > 0f)
            {
                _pingRemaining = Mathf.Max(0f, _pingRemaining - deltaTime);
            }

            _points.Begin();
            _streaks.Begin();
            if (Shown)
            {
                StepScanVisuals(emitter);
            }
            else if (_wedgeRenderer.enabled)
            {
                _wedgeRenderer.enabled = false;
            }

            _points.End();
            _streaks.End();
        }

        /// <summary>The vertical ballistic displacement (m) of a dust grain after <paramref name="ageSeconds"/> in 1/6 g.</summary>
        private float EvaluateBallisticVerticalOffset(float ageSeconds)
        {
            return PathUpwardLoftVelocity * ageSeconds - 0.5f * _scanTuning.PathGlintGravity * ageSeconds * ageSeconds;
        }

        /// <summary>
        /// The longitudinal alpha profile along the scan beam wedge at <paramref name="distanceFromLens"/> (m)
        /// out of <paramref name="totalLength"/> (m): brightest in the first couple of cm at the lens and where it meets
        /// the target scan line, and very low alpha in the middle.
        /// </summary>
        private float EvaluateWedgeLongitudinalAlpha(float distanceFromLens, float totalLength)
        {
            float len = Mathf.Max(totalLength, MinProfileLength);
            float d = Mathf.Clamp(distanceFromLens, 0f, len);

            float nozzleDecay = Mathf.Min(LensNozzleDecayMeters, len * _look.NozzleLengthShare);
            float lensCore = Mathf.Exp(-d / LensCoreDecayMeters) * (1f - LensNozzleShare);
            float lensNozzle = Mathf.Exp(-d / Mathf.Max(nozzleDecay, _look.MinGlowDecay)) * LensNozzleShare;
            float lensTerm = _scanTuning.BeamLensPeakAlpha * Mathf.Clamp01(lensCore + lensNozzle);

            float distFromTarget = Mathf.Max(0f, len - d);
            float contactDecay = Mathf.Min(ContactGlowDecayMeters, len * _look.ContactLengthShare);
            float contactGlow = Mathf.Exp(-distFromTarget / Mathf.Max(contactDecay, _look.MinGlowDecay)) *
                                ContactGlowShare;
            float contactLip = Mathf.Exp(-distFromTarget / ContactLipDecayMeters) * ContactLipShare;
            float contactTerm = _scanTuning.BeamContactAlpha * Mathf.Clamp01(contactGlow + contactLip);

            return Mathf.Clamp01(_scanTuning.BeamMidAlpha + lensTerm + contactTerm);
        }

        private void StepScanVisuals(Vector3 emitter)
        {
            ScanBeamLook look = _look;
            Color cyanColor = _scanTuning.ScanLineColor;
            Color pingColor = _scanTuning.CompletionPingColor;
            float coreSize = _scanTuning.LensGlowSize;
            float coreAlpha = Mathf.Min(_scanTuning.LensGlowAlpha, MaxBloomSafeCoreAlpha);
            float haloSize = _scanTuning.LensHaloSize;
            float haloAlpha = Mathf.Min(_scanTuning.LensHaloAlpha, MaxBloomSafeHaloAlpha);
            float irisSize = _scanTuning.LensIrisSize;
            float irisAlpha = Mathf.Min(_scanTuning.LensIrisAlpha, MaxBloomSafeIrisAlpha);
            float glintLength = _scanTuning.LensGlintLength;
            float glintWidth = _scanTuning.LensGlintWidth;
            float glintAlpha = Mathf.Min(_scanTuning.LensGlintAlpha, MaxBloomSafeStreakAlpha);
            float moteRadius = _scanTuning.LensMoteRadius;
            float moteSizeBase = _scanTuning.LensMoteSize;
            float moteAlphaBase = Mathf.Min(_scanTuning.LensMoteAlpha, MaxBloomSafeGlintAlpha);

            float eased = Ease.InOutSine(_level);
            float ping01 = _pingRemaining > 0f
                ? Mathf.Sin((1f - _pingRemaining / Mathf.Max(_pingDuration, MinFadeSeconds)) * Mathf.PI)
                : 0f;
            float sweepLevel = _pingRemaining > 0f ? 0f : eased;
            float visibility = Mathf.Max(sweepLevel, ping01);
            float widthScale = Mathf.Lerp(MinWidthShare, 1f, visibility);

            float spanHeight = Mathf.Max(Mathf.Abs(_span.y), look.MinSpanHeight);
            Vector3 sweepSpanVec = _span.sqrMagnitude > MinDistance ? _span : new Vector3(0f, spanHeight, 0f);
            Vector3 lineCentre = _hasContact ? _contact : _target + sweepSpanVec * (_progress - 0.5f);

            Vector3 toTarget = lineCentre - emitter;
            float dist = toTarget.magnitude;
            Vector3 forward = dist > MinDistance ? toTarget / dist : Vector3.forward;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward);
            right = right.sqrMagnitude > MinDistance ? right.normalized : Vector3.right;
            Vector3 orthoUp = Vector3.Cross(forward, right).normalized;
            // EyeOrigin (emitter) is flush with Head's side wall at the front rim of the cheek porthole, while the
            // circular brass porthole sits back along -forward and its convex glass extends along -right.
            Vector3 cheekCenter = emitter - right * look.CheekOffset.x - forward * look.CheekOffset.y;
            Vector3 lensFront = cheekCenter - right * look.LensFrontOffset.x + forward * look.LensFrontOffset.y;
            Vector3 wedgeOrigin = emitter - right * look.ApertureOffset.x - forward * look.ApertureOffset.y;

            Vector3 contactCenter = lineCentre;
            _lastContactCenter = contactCenter;

            // 1. Stylised narrow scan beam wedge from 07's cheek lens to the current surface scan line on the target
            StepWedgeMesh(wedgeOrigin, contactCenter, right, orthoUp, spanHeight, cyanColor, pingColor, sweepLevel, widthScale);

            // 2. No point light: a light at the lens would tint 07's cream body and head cyan during a scan
            Color activeTint = _pingRemaining > 0f ? pingColor : Color.Lerp(cyanColor, pingColor, BeamPingShare);

            // 3. 07's cheek lens emitter core, calm outer halo, opening iris ring, and short bezel-clamped flare needles
            Color halo = activeTint;
            halo.a = haloAlpha * visibility;
            _points.Add(cheekCenter, haloSize * widthScale, halo, Vector3.zero);

            Color core = Color.Lerp(activeTint, Color.white, look.CoreWhite);
            core.a = coreAlpha * visibility;
            _points.Add(cheekCenter, coreSize * widthScale, core, Vector3.zero);

            Color diodeHot = Color.Lerp(activeTint, Color.white, look.DiodeWhite);
            diodeHot.a = Mathf.Min(MaxBloomSafeCoreAlpha, coreAlpha * look.DiodeAlphaShare) * visibility;
            _points.Add(lensFront, coreSize * look.DiodeSizeShare * widthScale, diodeHot, Vector3.zero);

            // Opening iris aperture ring coaxial with the circular cheek lens porthole (in the forward/orthoUp cheek plane)
            float irisOpen = Mathf.Lerp(IrisClosedScale, 1f, visibility);
            float openIrisRadius = irisSize * look.IrisRadiusShare * irisOpen;
            float irisSegmentSize = Mathf.Max(look.IrisSegmentMin, irisSize * look.IrisSegmentShare) * widthScale;
            Color irisColor = Color.Lerp(activeTint, Color.white, look.IrisWhite);
            irisColor.a = irisAlpha * visibility;
            float irisSpin = _elapsed * look.IrisSpinSpeed + _progress * look.IrisProgressTurn;
            for (int i = 0; i < IrisRingCount; i++)
            {
                float angle = irisSpin + i * (TwoPi / IrisRingCount);
                Vector3 ringOffset = (forward * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * openIrisRadius;
                _points.Add(cheekCenter + ringOffset, irisSegmentSize, irisColor, Vector3.zero);
            }

            // Vertical iris caliper ticks at the front and rear of the cheek aperture that shift with the vertical scan sweep
            Color caliperColor = Color.Lerp(activeTint, Color.white, look.CaliperWhite);
            caliperColor.a = Mathf.Min(MaxBloomSafeIrisAlpha, irisAlpha * look.CaliperAlphaShare) * visibility;
            float caliperRadius = irisSize * look.CaliperRadiusShare * irisOpen;
            float caliperCycles = _progress * look.CaliperSweepCycles + _elapsed * look.CaliperIdleCycles;
            Vector3 sweepShift = orthoUp * (Mathf.Sin(caliperCycles * TwoPi) * look.CaliperShift);
            float caliperSize = irisSegmentSize * look.CaliperSizeShare;
            _points.Add(cheekCenter + forward * caliperRadius + sweepShift, caliperSize, caliperColor, Vector3.zero);
            _points.Add(cheekCenter - forward * caliperRadius + sweepShift, caliperSize, caliperColor, Vector3.zero);

            // Short optical lens flare needles clamped strictly to the lens bezel
            if (glintAlpha > VisibleThreshold)
            {
                Color streakColor = Color.Lerp(activeTint, Color.white, look.NeedleWhite);
                streakColor.a = glintAlpha * visibility;
                float streakWidth = glintWidth * look.NeedleWidthShare * widthScale;
                float levelWidth = streakWidth * look.NeedleLevelWidthShare;
                float bezelRadius = irisSize * 0.5f;
                float levelShare = look.NeedleLevelLengthShare;
                float horizLen = Mathf.Min(glintLength * levelShare, bezelRadius * levelShare) * widthScale;
                float vertLen = Mathf.Min(glintLength, bezelRadius * look.NeedleUprightBezelShare) * widthScale;
                Vector3 horizAxis = (forward * look.NeedleLevelAxis.x - right * look.NeedleLevelAxis.y).normalized;

                _streaks.Add(lensFront, levelWidth, streakColor, horizAxis * horizLen);
                _streaks.Add(lensFront, levelWidth, streakColor, -horizAxis * horizLen);
                _streaks.Add(lensFront, streakWidth, streakColor, orthoUp * vertLen);
                _streaks.Add(lensFront, streakWidth, streakColor, -orthoUp * vertLen);
            }

            // Tiny near-lens motes outside the cheek bezel
            if (moteAlphaBase > VisibleThreshold && sweepLevel > VisibleThreshold)
            {
                for (int i = 0; i < MoteCount; i++)
                {
                    float phase = _elapsed * look.MoteCycles + i * look.MotePhaseStep;
                    float life01 = phase - Mathf.Floor(phase);
                    float env = Mathf.Sin(life01 * Mathf.PI);
                    float angle = i * (TwoPi / MoteCount) + _elapsed * look.MoteSpin;
                    float radial = moteRadius * Mathf.Lerp(look.MoteBirthRadiusShare, 1f, life01);
                    Vector3 offset = (forward * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * radial
                        - right * (look.MoteDrift * life01);
                    Color moteColor = activeTint;
                    moteColor.a = moteAlphaBase * env * sweepLevel;
                    float size = moteSizeBase * Mathf.Lerp(look.MoteSizeShares.x, look.MoteSizeShares.y, i & 1) *
                                 widthScale;
                    _points.Add(cheekCenter + offset, size, moteColor, Vector3.zero);
                }
            }

            // 4. Small data ticks streaming along the beam from the target to the lens
            StepStreamingDataTicks(wedgeOrigin, contactCenter, right, orthoUp, cyanColor, pingColor, sweepLevel, widthScale);

            // 5. Sparse, brief tiny dust sparkles strictly within a thin band of the beam wedge, capped well below
            //    the bloom threshold
            StepBallisticPathGlints(wedgeOrigin, contactCenter, right, orthoUp, activeTint, sweepLevel, widthScale);
        }

        private void StepWedgeMesh(
            Vector3 lensFront,
            Vector3 contactCenter,
            Vector3 right,
            Vector3 orthoUp,
            float spanHeight,
            Color cyanColor,
            Color pingColor,
            float sweepLevel,
            float widthScale)
        {
            ScanBeamLook look = _look;
            Vector3 beamVec = contactCenter - lensFront;
            float beamLen = beamVec.magnitude;
            if (sweepLevel <= VisibleThreshold || beamLen <= look.WedgeMinLength)
            {
                if (_wedgeRenderer.enabled)
                {
                    _wedgeRenderer.enabled = false;
                }

                return;
            }

            _wedgeRenderer.enabled = true;
            Vector3 beamDir = beamVec / beamLen;

            float lensWidth = _scanTuning.BeamLensWidth * widthScale;
            float scanlineContrast = _scanTuning.BeamScanlineContrast;

            _wedgeMaterial.SetFloat(BandCountShaderId, _scanTuning.BeamScanlineDensity);
            _wedgeMaterial.SetFloat(BandStrengthShaderId, scanlineContrast);

            // Far edge width spans the current horizontal surface scan line on the target
            float targetProfile = Mathf.Lerp(look.TargetEdgeShare, 1.0f, Mathf.Sin(Mathf.Clamp01(_progress) * Mathf.PI));
            Vector2 targetRange = look.TargetWidthRange;
            float targetWidth = Mathf.Clamp(spanHeight * _scanTuning.BeamTargetWidthShare, targetRange.x, targetRange.y)
                                * targetProfile * widthScale;
            _lastTargetWidth = targetWidth;

            float lensHalfW = lensWidth * 0.5f;
            float targetHalfW = targetWidth * 0.5f;
            Vector2 wrapRange = look.WrapRadiusRange;
            float wrapRadius = Mathf.Min(Mathf.Clamp(spanHeight * look.WrapShare, wrapRange.x, wrapRange.y),
                beamLen * look.WrapLengthShare);
            Vector2 clearRamp = look.ScreenClearRamp;
            float striationDepth = scanlineContrast * look.StriationDepth;

            for (int r = 0; r < WedgeRings; r++)
            {
                float u = RingFractions[r];
                float d = u * beamLen;
                Vector3 ringCenter = lensFront + beamDir * d;

                float widthT = Mathf.Lerp(u, u * u * (3f - 2f * u), look.WidthEase);
                float halfW = Mathf.Lerp(lensHalfW, targetHalfW, widthT);

                // Keep the first couple of cm at the lens bright, dip to near-zero while passing 07's CRT screen,
                // and carry the faint mid-beam wedge across open space to the target scan line.
                float screenClearance = d <= look.ScreenClearStart
                    ? 1f
                    : Mathf.SmoothStep(look.ScreenClearFloor, 1.0f, Mathf.InverseLerp(clearRamp.x, clearRamp.y, d));
                float alongAlpha = EvaluateWedgeLongitudinalAlpha(d, beamLen) * screenClearance;
                float lensHotMix = Mathf.Exp(-d / LensCoreDecayMeters) * look.LensWhite;
                float contactHotMix = Mathf.Exp(-Mathf.Max(0f, beamLen - d) / ContactLipDecayMeters) * look.ContactWhite;
                float ping = Mathf.SmoothStep(look.PingRamp.x, look.PingRamp.y, u) * look.PingShare;
                Color ringColor = Color.Lerp(cyanColor, pingColor, ping);
                Color baseTint = Color.Lerp(ringColor, Color.white, Mathf.Clamp01(lensHotMix + contactHotMix));

                // Crossed fan sheets tilted in (right, orthoUp) so the wedge fans out visibly from low
                // chase camera angles while spanning and wrapping the surface scan line on the target.
                float verticalTilt = Mathf.Lerp(look.SheetTilt.x, look.SheetTilt.y, u);
                Vector3 sheetDir0 = (right + orthoUp * verticalTilt).normalized;
                Vector3 sheetDir1 = (right - orthoUp * verticalTilt).normalized;
                float sheet1AlphaShare = Mathf.Lerp(look.SecondSheetAlpha.x, look.SecondSheetAlpha.y, u);
                float targetWrapShare = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(look.WrapStart, 1f, u));
                float scanlineFade = Mathf.Clamp01(d / look.StriationFadeIn);
                float filamentFade = Mathf.Exp(-d / look.FilamentLength) * look.FilamentAlpha;
                float archFall = 1f - u * look.ArchFlatten;

                float uvX = u;
                int rowBase0 = r * WedgeSlices;
                int rowBase1 = VertsPerSheet + rowBase0;

                for (int s = 0; s < WedgeSlices; s++)
                {
                    float v01 = (float)s / (WedgeSlices - 1);
                    float v = v01 * 2f - 1f;
                    float baseLat = Mathf.Sqrt(Mathf.Clamp01(1f - v * v * look.SideRoundness));
                    float lateralEnvelope = Mathf.Lerp(baseLat * baseLat, baseLat, targetWrapShare);

                    // Fine radial scanline striations inside the fan wedge
                    float radialWave = 0.5f + 0.5f * Mathf.Cos((v * look.Striations + _progress * look.StriationDrift) * TwoPi);
                    float scanlineMod = 1f - striationDepth * scanlineFade * radialWave;
                    float axisFilament = Mathf.Pow(1f - Mathf.Abs(v), look.FilamentSharpness) * filamentFade;

                    float alpha0 = Mathf.Clamp01((alongAlpha * scanlineMod + axisFilament) * lateralEnvelope) * sweepLevel;
                    Color c0 = baseTint;
                    c0.a = alpha0;

                    // Curve the far rings forward around the 3D target hull so the contact lip wraps the object
                    float arch = (1f - v * v) * WedgeArchHeight * archFall;
                    Vector3 wrapOffset = beamDir * (v * v * wrapRadius * targetWrapShare);
                    int idx0 = rowBase0 + s;
                    _wedgeVertices[idx0] = ringCenter + sheetDir0 * (v * halfW) + orthoUp * arch + wrapOffset;
                    _wedgeNormals[idx0] = orthoUp;
                    _wedgeUvs[idx0] = new Vector2(uvX, v01);
                    _wedgeColors[idx0] = c0;

                    Color c1 = baseTint;
                    c1.a = alpha0 * sheet1AlphaShare;

                    int idx1 = rowBase1 + s;
                    _wedgeVertices[idx1] = ringCenter + sheetDir1 * (v * halfW) - orthoUp * (arch * look.SecondSheetArch)
                                           + wrapOffset;
                    _wedgeNormals[idx1] = orthoUp;
                    _wedgeUvs[idx1] = new Vector2(uvX, v01);
                    _wedgeColors[idx1] = c1;
                }
            }

            _wedgeMesh.SetVertices(_wedgeVertices);
            _wedgeMesh.SetNormals(_wedgeNormals);
            _wedgeMesh.SetUVs(0, _wedgeUvs);
            _wedgeMesh.SetColors(_wedgeColors);
            _wedgeMesh.bounds = new Bounds((lensFront + contactCenter) * 0.5f,
                Vector3.one * (beamLen + WedgeBoundsPadding));
        }

        private void StepStreamingDataTicks(
            Vector3 lensFront,
            Vector3 contactCenter,
            Vector3 right,
            Vector3 orthoUp,
            Color cyanColor,
            Color pingColor,
            float sweepLevel,
            float widthScale)
        {
            ScanBeamLook look = _look;
            Vector3 beamVec = contactCenter - lensFront;
            float beamLen = beamVec.magnitude;
            if (sweepLevel <= VisibleThreshold || beamLen <= look.TickMinBeamLength)
            {
                return;
            }

            Vector3 beamDir = beamVec / beamLen;
            float tickSpeed = _scanTuning.BeamPulseSpeed * look.TickSpeedShare;
            float targetHalfW = Mathf.Max(_lastTargetWidth * look.TickTargetSpread, look.TickTargetSpreadMin);
            float lensHalfW = _scanTuning.BeamLensWidth * look.TickLensSpread * widthScale;
            float tickWidth = look.TickWidth * widthScale;
            Color coolColor = Color.Lerp(cyanColor, Color.white, look.TickCyanWhite);

            for (int i = 0; i < DataTickCount; i++)
            {
                // Stream from the target toward 07's cheek lens
                float phase = Mathf.Repeat(_elapsed * tickSpeed + (float)i / DataTickCount, 1f);
                float travel = phase * phase * Mathf.Lerp(look.TickEvenness, 1f, phase);
                float u = Mathf.Lerp(look.TickTravel.x, look.TickTravel.y, travel);
                float env = Mathf.Sin(phase * Mathf.PI);

                float laneSign = (i & 1) == 0 ? -1f : 1f;
                float laneShare = ((i % DataTickLanes) - 1) * look.TickLaneSpacing + laneSign * look.TickLaneOffset;
                float halfW = Mathf.Lerp(lensHalfW, targetHalfW, u);
                Vector3 pos = lensFront + beamDir * (u * beamLen)
                    + right * (laneShare * halfW)
                    + orthoUp * (Mathf.Sin(u * Mathf.PI) * look.TickBow * laneSign);

                float tickLen = Mathf.Lerp(look.TickLength.x, look.TickLength.y, env) * widthScale;
                Color tickColor = Color.Lerp(pingColor, coolColor, (1f - u) * look.TickCyanShare);
                tickColor.a = Mathf.Min(look.TickAlphaCap, look.TickAlpha * env * sweepLevel);
                _streaks.Add(pos, tickWidth, tickColor, -beamDir * tickLen);
            }
        }

        private void StepBallisticPathGlints(
            Vector3 lensFront,
            Vector3 contactCenter,
            Vector3 right,
            Vector3 orthoUp,
            Color activeTint,
            float visibility,
            float widthScale)
        {
            ScanBeamLook look = _look;
            Vector2 lifeRange = look.GlintLifetimeRange;
            int count = Mathf.Clamp(_scanTuning.PathGlintCount, 0, MaxPathGlints);
            float baseSize = _scanTuning.PathGlintSize;
            float baseAlpha = Mathf.Min(_scanTuning.PathGlintAlpha, MaxBloomSafeGlintAlpha);
            float lifetime = Mathf.Clamp(_scanTuning.PathGlintLifetime, lifeRange.x, lifeRange.y);

            float dist = Vector3.Distance(lensFront, contactCenter);
            if (count <= 0 || baseAlpha <= VisibleThreshold || visibility <= VisibleThreshold ||
                dist <= look.GlintMinBeamLength)
            {
                return;
            }

            float lensHalfW = _scanTuning.BeamLensWidth * 0.5f * widthScale;
            float targetHalfW = Mathf.Max(_lastTargetWidth * 0.5f, look.GlintTargetSpreadMin);
            float minStartShare = Mathf.Max(PathStartShare, look.GlintLensClearance / dist);
            Color glintColor = Color.Lerp(activeTint, Color.white, look.GlintWhite);

            for (int i = 0; i < count; i++)
            {
                // Brief twinkle with asynchronous cycle periods so only 1-2 subtle glints appear at a time
                float twinkleSpread = Mathf.Lerp(look.GlintTwinkleSpread.x, look.GlintTwinkleSpread.y,
                    Hash01(i * 17.13f + 0.31f));
                float twinkleSeconds = Mathf.Clamp(lifetime * twinkleSpread, lifeRange.x, lifeRange.y);
                float dutyCycle = PathActiveDutyCycle *
                                  Mathf.Lerp(look.GlintDutySpread.x, look.GlintDutySpread.y, Hash01(i * 29.47f + 0.53f));
                float cyclePeriod = twinkleSeconds / Mathf.Max(dutyCycle, look.GlintMinDuty);
                float cyclePhase = (_elapsed / cyclePeriod) + Hash01(i * 43.91f + 0.73f);
                float cycleIndex = Mathf.Floor(cyclePhase);
                float cycleFrac = cyclePhase - cycleIndex;
                if (cycleFrac > dutyCycle)
                {
                    continue;
                }

                float life01 = cycleFrac / dutyCycle;
                float ageSeconds = life01 * twinkleSeconds;
                float sinLife = Mathf.Sin(life01 * Mathf.PI);
                float sparkleEnv = sinLife * sinLife;

                float hU = Hash01(i * 53.37f + cycleIndex * 97.13f + 0.19f);
                float scrambledU = Mathf.Repeat(hU + i * 0.38196601f, 1f);
                float u01 = Mathf.Lerp(minStartShare, PathEndShare, scrambledU);

                // Keep every glint strictly inside a thin band of the beam wedge
                float wedgeHalfWidthAtU = Mathf.Lerp(lensHalfW, targetHalfW, u01);
                float hLat = Hash01(i * 89.17f + cycleIndex * 37.59f + 0.67f) * 2f - 1f;
                float lateralOffset = hLat * wedgeHalfWidthAtU * PathWedgeWidthShare;

                float hVert = Hash01(i * 71.29f + cycleIndex * 61.43f + 0.41f) * 2f - 1f;
                float verticalBandOffset = hVert * PathVerticalBandHalfHeight;

                Vector3 rayAnchor = Vector3.Lerp(lensFront, contactCenter, u01)
                    + right * lateralOffset
                    + orthoUp * verticalBandOffset;

                // 1/6 g lunar ballistic trajectory kept within the thin wedge band
                float verticalOffset = Mathf.Clamp(
                    EvaluateBallisticVerticalOffset(ageSeconds),
                    -PathVerticalBandHalfHeight,
                    PathVerticalBandHalfHeight);
                Vector3 grainPos = rayAnchor + Vector3.up * verticalOffset;

                float distanceScale = Mathf.Lerp(look.GlintDistanceScale.x, look.GlintDistanceScale.y, u01);
                float grainScale = Mathf.Lerp(look.GlintGrainScale.x, look.GlintGrainScale.y,
                    Hash01(i * 97.41f + cycleIndex * 19.83f));
                float size = baseSize * distanceScale * grainScale * widthScale;

                // Single sprite per glint, capped strictly below the bloom threshold
                Color color = glintColor;
                color.a = Mathf.Min(look.GlintAlphaCap, baseAlpha * sparkleEnv) * visibility;
                _points.Add(grainPos, size, color, Vector3.zero);
            }
        }

        private static Material CreateWedgeMaterial(Material source, ScanTuning scanTuning)
        {
            var mat = new Material(source)
            {
                name = "M_ScanBeamWedge",
                renderQueue = (int)RenderQueue.Transparent,
            };
            mat.SetColor(ColorShaderId, Color.white);
            mat.SetFloat(IntensityShaderId, WedgeMaterialIntensity);
            mat.SetFloat(AcrossInShaderId, WedgeAcrossFade);
            mat.SetFloat(AcrossOutShaderId, WedgeAcrossFade);
            mat.SetFloat(LengthFadeShaderId, 0.0f);
            mat.SetFloat(BandCountShaderId, scanTuning.BeamScanlineDensity);
            mat.SetFloat(BandSpeedShaderId, WedgeBandSpeed);
            mat.SetFloat(BandStrengthShaderId, scanTuning.BeamScanlineContrast);
            mat.SetFloat(FresnelMixShaderId, 0.0f);
            mat.SetFloat(CoreMixShaderId, 0.0f);
            mat.SetFloat(RadialMaskShaderId, 0.0f);
            mat.SetFloat(SrcBlendShaderId, (float)BlendMode.One);
            mat.SetFloat(DstBlendShaderId, (float)BlendMode.One);
            mat.SetFloat(CullShaderId, (float)CullMode.Off);
            return mat;
        }

        private static Mesh CreateWedgeMesh(Transform parent, Material material, out MeshRenderer renderer)
        {
            var host = new GameObject("ScanBeamWedge");
            host.transform.SetParent(parent, false);
            MeshFilter filter = host.AddComponent<MeshFilter>();
            renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;

            int quadsPerSheet = (WedgeRings - 1) * (WedgeSlices - 1);
            var triangles = new int[quadsPerSheet * 6 * WedgeSheetCount];
            int t = 0;
            for (int sheet = 0; sheet < WedgeSheetCount; sheet++)
            {
                int sheetOffset = sheet * VertsPerSheet;
                for (int r = 0; r < WedgeRings - 1; r++)
                {
                    int row0 = sheetOffset + r * WedgeSlices;
                    int row1 = row0 + WedgeSlices;
                    for (int s = 0; s < WedgeSlices - 1; s++)
                    {
                        int v00 = row0 + s;
                        int v01 = v00 + 1;
                        int v10 = row1 + s;
                        int v11 = v10 + 1;

                        triangles[t++] = v00;
                        triangles[t++] = v10;
                        triangles[t++] = v11;

                        triangles[t++] = v00;
                        triangles[t++] = v11;
                        triangles[t++] = v01;
                    }
                }
            }

            var mesh = new Mesh { name = "ScanBeamWedgeMesh" };
            mesh.MarkDynamic();
            mesh.vertices = new Vector3[TotalWedgeVerts];
            mesh.normals = new Vector3[TotalWedgeVerts];
            mesh.uv = new Vector2[TotalWedgeVerts];
            mesh.colors = new Color[TotalWedgeVerts];
            mesh.triangles = triangles;
            filter.sharedMesh = mesh;
            return mesh;
        }

        private static float Hash01(float seed)
        {
            float s = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }
    }
}
