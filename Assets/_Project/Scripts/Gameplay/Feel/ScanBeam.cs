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
    /// Zero allocations per frame.
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
        private const int StreakSlots = LensStreakCount + DataTickCount;

        private const int WedgeRings = 16;
        private const int WedgeSlices = 9;
        private const int WedgeSheetCount = 2;
        private const int VertsPerSheet = WedgeRings * WedgeSlices;
        private const int TotalWedgeVerts = VertsPerSheet * WedgeSheetCount;

        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float LensSurfaceStandOff = 0.020f;
        private const float MinWidthShare = 0.65f;
        private const float DefaultPingDuration = 0.38f;
        private const float TwoPi = Mathf.PI * 2f;

        private const float DefaultBeamLensWidth = 0.064f;
        private const float DefaultBeamTargetWidthShare = 0.85f;
        private const float DefaultBeamLensPeakAlpha = 0.68f;
        private const float DefaultBeamMidAlpha = 0.022f;
        private const float DefaultBeamContactAlpha = 0.38f;
        private const float DefaultBeamScanlineDensity = 42f;
        private const float DefaultBeamScanlineContrast = 0.48f;

        private const float DefaultLensIrisSize = 0.138f;
        private const float DefaultLensIrisAlpha = 0.82f;
        private const float DefaultLensGlintLength = 0.068f;
        private const float DefaultLensGlintWidth = 0.008f;
        private const float DefaultLensGlintAlpha = 0.76f;
        private const int DefaultPathGlintCount = 5;
        private const float DefaultPathGlintSize = 0.016f;
        private const float DefaultPathGlintAlpha = 0.32f;
        private const float DefaultPathGlintLifetime = 0.18f;
        private const float DefaultLunarGravity = 1.62f;

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
        private const float IrisCaliperRadiusFactor = 0.56f;
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

        private static readonly Color DefaultCyanColor = new Color(0.20f, 0.86f, 0.96f, 1f);
        private static readonly Color DefaultAmberPingColor = new Color(1.00f, 0.682f, 0.220f, 1f);

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
        private Vector3 _lastLensFront;
        private Vector3 _lastContactCenter;
        private Vector3 _contact;
        private bool _hasContact;
        private float _lastTargetWidth;
        private float _progress;
        private float _rate;
        private float _level;
        private float _elapsed;
        private float _pingRemaining;
        private float _pingDuration = DefaultPingDuration;
        private bool _active;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial,
            ScanTuning scanTuning = null)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning;
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

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

        /// <summary>True while the procedural scan beam wedge mesh renderer is active.</summary>
        public bool WedgeEnabled => _wedgeRenderer != null && _wedgeRenderer.enabled;

        /// <summary>World-space apex of the scan beam wedge right in front of 07's cheek lens on the most recent frame.</summary>
        public Vector3 WedgeLensApex => _lastLensFront;

        /// <summary>World-space center of the far edge of the scan beam wedge on the target's current scan line.</summary>
        public Vector3 WedgeTargetCenter => _lastContactCenter;

        /// <summary>World-space width (m) of the far edge of the scan beam wedge on the target's current scan line.</summary>
        public float WedgeTargetWidth => _lastTargetWidth;

        /// <summary>Number of point sprites emitted on the most recent frame (for tests and diagnostics).</summary>
        public int ActivePointCount => _points.Count;

        /// <summary>Number of streak sprites emitted on the most recent frame (for tests and diagnostics).</summary>
        public int ActiveStreakCount => _streaks.Count;

        /// <summary>Computes the vertical ballistic displacement (meters) of a dust grain after <paramref name="ageSeconds"/> in 1/6 g.</summary>
        public float EvaluateBallisticVerticalOffset(float ageSeconds)
        {
            float gravity = _scanTuning != null ? _scanTuning.PathGlintGravity : DefaultLunarGravity;
            return PathUpwardLoftVelocity * ageSeconds - 0.5f * gravity * ageSeconds * ageSeconds;
        }

        /// <summary>
        /// Evaluates the longitudinal alpha profile along the scan beam wedge at <paramref name="distanceFromLens"/> (m)
        /// out of <paramref name="totalLength"/> (m): brightest in the first couple of cm at the lens and where it meets
        /// the target scan line, and very low alpha in the middle.
        /// </summary>
        public float EvaluateWedgeLongitudinalAlpha(float distanceFromLens, float totalLength)
        {
            float len = Mathf.Max(totalLength, 0.1f);
            float d = Mathf.Clamp(distanceFromLens, 0f, len);
            float lensPeak = _scanTuning != null ? _scanTuning.BeamLensPeakAlpha : DefaultBeamLensPeakAlpha;
            float midAlpha = _scanTuning != null ? _scanTuning.BeamMidAlpha : DefaultBeamMidAlpha;
            float contactAlpha = _scanTuning != null ? _scanTuning.BeamContactAlpha : DefaultBeamContactAlpha;

            float nozzleDecay = Mathf.Min(LensNozzleDecayMeters, len * 0.14f);
            float lensCore = Mathf.Exp(-d / LensCoreDecayMeters) * (1f - LensNozzleShare);
            float lensNozzle = Mathf.Exp(-d / Mathf.Max(nozzleDecay, 0.02f)) * LensNozzleShare;
            float lensTerm = lensPeak * Mathf.Clamp01(lensCore + lensNozzle);

            float distFromTarget = Mathf.Max(0f, len - d);
            float contactDecay = Mathf.Min(ContactGlowDecayMeters, len * 0.12f);
            float contactGlow = Mathf.Exp(-distFromTarget / Mathf.Max(contactDecay, 0.02f)) * ContactGlowShare;
            float contactLip = Mathf.Exp(-distFromTarget / ContactLipDecayMeters) * ContactLipShare;
            float contactTerm = contactAlpha * Mathf.Clamp01(contactGlow + contactLip);

            return Mathf.Clamp01(midAlpha + lensTerm + contactTerm);
        }

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
        /// <see cref="Step"/>).
        /// </summary>
        public void Meet(Vector3 contact)
        {
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
            _pingDuration = _scanTuning != null ? _scanTuning.PingDurationSeconds : DefaultPingDuration;
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
                StepScanVisuals(deltaTime, emitter);
            }
            else if (_wedgeRenderer.enabled)
            {
                _wedgeRenderer.enabled = false;
            }

            _points.End();
            _streaks.End();
        }

        private void StepScanVisuals(float deltaTime, Vector3 emitter)
        {
            Color cyanColor = _scanTuning != null ? _scanTuning.ScanLineColor : DefaultCyanColor;
            Color amberColor = _scanTuning != null ? _scanTuning.CompletionPingColor : DefaultAmberPingColor;
            float coreSize = _scanTuning != null ? _scanTuning.LensGlowSize : _tuning.ScanLensCoreSize;
            float coreAlpha = Mathf.Min(_scanTuning != null ? _scanTuning.LensGlowAlpha : _tuning.ScanLensCoreAlpha, MaxBloomSafeCoreAlpha);
            float haloSize = _scanTuning != null ? _scanTuning.LensHaloSize : _tuning.ScanLensHaloSize;
            float haloAlpha = Mathf.Min(_scanTuning != null ? _scanTuning.LensHaloAlpha : _tuning.ScanLensHaloAlpha, MaxBloomSafeHaloAlpha);
            float irisSize = _scanTuning != null ? _scanTuning.LensIrisSize : DefaultLensIrisSize;
            float irisAlpha = Mathf.Min(_scanTuning != null ? _scanTuning.LensIrisAlpha : DefaultLensIrisAlpha, MaxBloomSafeIrisAlpha);
            float glintLength = _scanTuning != null ? _scanTuning.LensGlintLength : DefaultLensGlintLength;
            float glintWidth = _scanTuning != null ? _scanTuning.LensGlintWidth : DefaultLensGlintWidth;
            float glintAlpha = Mathf.Min(_scanTuning != null ? _scanTuning.LensGlintAlpha : DefaultLensGlintAlpha, MaxBloomSafeStreakAlpha);
            float moteRadius = _scanTuning != null ? _scanTuning.LensMoteRadius : 0.048f;
            float moteSizeBase = _scanTuning != null ? _scanTuning.LensMoteSize : 0.007f;
            float moteAlphaBase = Mathf.Min(_scanTuning != null ? _scanTuning.LensMoteAlpha : 0.22f, MaxBloomSafeGlintAlpha);

            float eased = Ease.InOutSine(_level);
            float ping01 = _pingRemaining > 0f
                ? Mathf.Sin((1f - _pingRemaining / Mathf.Max(_pingDuration, MinFadeSeconds)) * Mathf.PI)
                : 0f;
            float sweepLevel = _pingRemaining > 0f ? 0f : eased;
            float visibility = Mathf.Max(sweepLevel, ping01);
            float widthScale = Mathf.Lerp(MinWidthShare, 1f, visibility);

            float spanHeight = Mathf.Max(Mathf.Abs(_span.y), 0.36f);
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
            // circular brass porthole is centered ~7.2 cm back along -forward and its convex glass extends ~7.0 cm along -right.
            Vector3 cheekCenter = emitter - right * 0.078f - forward * 0.072f;
            Vector3 lensFront = cheekCenter - right * 0.008f + forward * 0.010f;
            Vector3 wedgeOrigin = emitter - right * 0.074f - forward * 0.056f;

            Vector3 contactCenter = lineCentre;
            _lastLensFront = wedgeOrigin;
            _lastContactCenter = contactCenter;

            // 1. Stylised narrow scan beam wedge from 07's cheek lens to the current surface scan line on the target
            StepWedgeMesh(wedgeOrigin, contactCenter, right, orthoUp, spanHeight, cyanColor, amberColor, sweepLevel, widthScale);

            // 2. No point light: a light at the lens would tint 07's cream body and head cyan during a scan
            ScanVariant variant = _scanTuning != null ? _scanTuning.Variant : ScanVariant.ProfilerSweep;
            Color activeTint = _pingRemaining > 0f
                ? amberColor
                : (variant == ScanVariant.WireframeReveal ? Color.Lerp(cyanColor, amberColor, 0.35f) : cyanColor);

            // 3. 07's cheek lens emitter core, calm outer halo, opening iris ring, and short bezel-clamped flare needles
            Color halo = activeTint;
            halo.a = haloAlpha * visibility;
            _points.Add(cheekCenter, haloSize * widthScale, halo, Vector3.zero);

            Color core = Color.Lerp(activeTint, Color.white, 0.46f);
            core.a = coreAlpha * visibility;
            _points.Add(cheekCenter, coreSize * widthScale, core, Vector3.zero);

            Color diodeHot = Color.Lerp(activeTint, Color.white, 0.78f);
            diodeHot.a = Mathf.Min(MaxBloomSafeCoreAlpha, coreAlpha * 0.98f) * visibility;
            _points.Add(lensFront, coreSize * 0.56f * widthScale, diodeHot, Vector3.zero);

            // Opening iris aperture ring coaxial with the circular cheek lens porthole (in the forward/orthoUp cheek plane)
            float openIrisRadius = irisSize * 0.38f * Mathf.Lerp(IrisClosedScale, 1f, visibility);
            float irisSegmentSize = Mathf.Max(0.014f, irisSize * 0.105f) * widthScale;
            Color irisColor = Color.Lerp(activeTint, Color.white, 0.32f);
            irisColor.a = irisAlpha * visibility;
            float irisSpin = _elapsed * 0.35f + _progress * 0.8f;
            for (int i = 0; i < IrisRingCount; i++)
            {
                float angle = irisSpin + i * (TwoPi / IrisRingCount);
                Vector3 ringOffset = (forward * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * openIrisRadius;
                _points.Add(cheekCenter + ringOffset, irisSegmentSize, irisColor, Vector3.zero);
            }

            // Vertical iris caliper ticks at the front and rear of the cheek aperture that shift with the vertical scan sweep
            Color caliperColor = Color.Lerp(activeTint, Color.white, 0.56f);
            caliperColor.a = Mathf.Min(MaxBloomSafeIrisAlpha, irisAlpha * 1.05f) * visibility;
            float caliperRadius = irisSize * 0.42f * Mathf.Lerp(IrisClosedScale, 1f, visibility);
            float verticalTickShift = Mathf.Sin((_progress * 1.75f + _elapsed * 0.5f) * TwoPi) * 0.005f;
            Vector3 sweepShift = orthoUp * verticalTickShift;
            _points.Add(cheekCenter + forward * caliperRadius + sweepShift, irisSegmentSize * 1.35f, caliperColor, Vector3.zero);
            _points.Add(cheekCenter - forward * caliperRadius + sweepShift, irisSegmentSize * 1.35f, caliperColor, Vector3.zero);

            // Short optical lens flare needles clamped strictly to the lens bezel
            if (glintAlpha > VisibleThreshold)
            {
                Color streakColor = Color.Lerp(activeTint, Color.white, 0.58f);
                streakColor.a = glintAlpha * visibility;
                float streakWidth = glintWidth * 1.55f * widthScale;
                float bezelRadius = irisSize * 0.5f;
                float horizLen = Mathf.Min(glintLength * 0.56f, bezelRadius * 0.56f) * widthScale;
                float vertLen = Mathf.Min(glintLength, bezelRadius * 0.94f) * widthScale;
                Vector3 horizAxis = (forward * 0.90f - right * 0.44f).normalized;

                _streaks.Add(lensFront, streakWidth * 1.15f, streakColor, horizAxis * horizLen);
                _streaks.Add(lensFront, streakWidth * 1.15f, streakColor, -horizAxis * horizLen);
                _streaks.Add(lensFront, streakWidth, streakColor, orthoUp * vertLen);
                _streaks.Add(lensFront, streakWidth, streakColor, -orthoUp * vertLen);
            }

            // Tiny near-lens motes outside the cheek bezel
            if (moteAlphaBase > VisibleThreshold && sweepLevel > VisibleThreshold)
            {
                for (int i = 0; i < MoteCount; i++)
                {
                    float phase = _elapsed * 0.95f + i * 0.41f;
                    float life01 = phase - Mathf.Floor(phase);
                    float env = Mathf.Sin(life01 * Mathf.PI);
                    float angle = i * 1.0471976f + _elapsed * 0.55f;
                    float radial = moteRadius * (0.35f + 0.65f * life01);
                    Vector3 offset = (forward * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * radial
                        - right * (0.015f * life01);
                    Color moteColor = activeTint;
                    moteColor.a = moteAlphaBase * env * sweepLevel;
                    float size = moteSizeBase * Mathf.Lerp(0.75f, 1.08f, i & 1) * widthScale;
                    _points.Add(cheekCenter + offset, size, moteColor, Vector3.zero);
                }
            }

            // 4. Variant B ("Wireframe reveal"): small data ticks streaming along the beam from the target to the lens
            if (variant == ScanVariant.WireframeReveal)
            {
                StepStreamingDataTicks(wedgeOrigin, contactCenter, right, orthoUp, cyanColor, amberColor, sweepLevel, widthScale);
            }
            else if (variant == ScanVariant.PointScan)
            {
                StepPulsingLaserLine(wedgeOrigin, contactCenter, cyanColor, amberColor, sweepLevel, widthScale);
            }

            // 5. Sparse, brief (0.1-0.3 s) tiny dust sparkles strictly within a thin band of the beam wedge,
            //    capped well below the bloom threshold
            StepBallisticPathGlints(wedgeOrigin, contactCenter, right, orthoUp, activeTint, sweepLevel, widthScale);
        }

        private void StepWedgeMesh(
            Vector3 lensFront,
            Vector3 contactCenter,
            Vector3 right,
            Vector3 orthoUp,
            float spanHeight,
            Color cyanColor,
            Color amberColor,
            float sweepLevel,
            float widthScale)
        {
            Vector3 beamVec = contactCenter - lensFront;
            float beamLen = beamVec.magnitude;
            if (sweepLevel <= VisibleThreshold || beamLen <= 0.12f)
            {
                if (_wedgeRenderer.enabled)
                {
                    _wedgeRenderer.enabled = false;
                }

                return;
            }

            _wedgeRenderer.enabled = true;
            Vector3 beamDir = beamVec / beamLen;

            ScanVariant variant = _scanTuning != null ? _scanTuning.Variant : ScanVariant.ProfilerSweep;
            float lensWidth = (_scanTuning != null ? _scanTuning.BeamLensWidth : DefaultBeamLensWidth) * widthScale;
            float targetWidthShare = _scanTuning != null ? _scanTuning.BeamTargetWidthShare : DefaultBeamTargetWidthShare;
            float scanlineDensity = _scanTuning != null ? _scanTuning.BeamScanlineDensity : DefaultBeamScanlineDensity;
            float scanlineContrast = _scanTuning != null ? _scanTuning.BeamScanlineContrast : DefaultBeamScanlineContrast;

            _wedgeMaterial.SetFloat(BandCountShaderId, scanlineDensity);
            _wedgeMaterial.SetFloat(BandStrengthShaderId, scanlineContrast);

            // Far edge width spans the current horizontal surface scan line on the target
            float targetProfile = Mathf.Lerp(0.75f, 1.0f, Mathf.Sin(Mathf.Clamp01(_progress) * Mathf.PI));
            float targetWidth = Mathf.Clamp(spanHeight * targetWidthShare, 0.32f, 0.92f) * targetProfile * widthScale;
            _lastTargetWidth = targetWidth;

            float lensHalfW = lensWidth * 0.5f;
            float targetHalfW = targetWidth * 0.5f;
            float wrapRadius = Mathf.Min(Mathf.Clamp(spanHeight * 0.26f, 0.06f, 0.24f), beamLen * 0.25f);

            for (int r = 0; r < WedgeRings; r++)
            {
                float u = RingFractions[r];
                float d = u * beamLen;
                Vector3 ringCenter = lensFront + beamDir * d;

                float widthT = Mathf.Lerp(u, u * u * (3f - 2f * u), 0.18f);
                float halfW = Mathf.Lerp(lensHalfW, targetHalfW, widthT);

                // Keep the first couple of cm at the lens bright, dip to near-zero while passing 07's CRT screen,
                // and carry the faint mid-beam wedge across open space to the target scan line.
                float screenClearance = d <= 0.095f
                    ? 1f
                    : Mathf.SmoothStep(0.18f, 1.0f, Mathf.InverseLerp(0.115f, 0.48f, d));
                float alongAlpha = EvaluateWedgeLongitudinalAlpha(d, beamLen) * screenClearance;
                float lensHotMix = Mathf.Exp(-d / LensCoreDecayMeters) * 0.62f;
                float contactHotMix = Mathf.Exp(-Mathf.Max(0f, beamLen - d) / ContactLipDecayMeters) * 0.34f;
                Color ringColor = variant == ScanVariant.WireframeReveal
                    ? Color.Lerp(cyanColor, amberColor, Mathf.SmoothStep(0.25f, 0.95f, u) * 0.82f)
                    : cyanColor;
                Color baseTint = Color.Lerp(ringColor, Color.white, Mathf.Clamp01(lensHotMix + contactHotMix));

                // Crossed fan sheets tilted in (right, orthoUp) so the wedge fans out visibly from low
                // chase camera angles while spanning and wrapping the surface scan line on the target.
                float verticalTilt = Mathf.Lerp(0.68f, 0.34f, u);
                Vector3 sheetDir0 = (right + orthoUp * verticalTilt).normalized;
                Vector3 sheetDir1 = (right - orthoUp * verticalTilt).normalized;
                float sheet1AlphaShare = Mathf.Lerp(0.78f, 0.52f, u);
                float targetWrapShare = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1f, u));

                float uvX = u;
                int rowBase0 = r * WedgeSlices;
                int rowBase1 = VertsPerSheet + rowBase0;

                for (int s = 0; s < WedgeSlices; s++)
                {
                    float v01 = (float)s / (WedgeSlices - 1);
                    float v = v01 * 2f - 1f;
                    float baseLat = Mathf.Sqrt(Mathf.Clamp01(1f - v * v * 0.85f));
                    float lateralEnvelope = variant == ScanVariant.PointScan
                        ? Mathf.Lerp(Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 5.2f), baseLat, targetWrapShare * 0.72f)
                        : Mathf.Lerp(baseLat * baseLat, baseLat, targetWrapShare);

                    // Fine radial scanline striations inside the fan wedge
                    float radialWave = 0.5f + 0.5f * Mathf.Cos((v * 7.0f + _progress * 1.5f) * TwoPi);
                    float scanlineFade = Mathf.Clamp01(d / 0.06f);
                    float scanlineMod = 1f - (scanlineContrast * 0.32f * scanlineFade) * radialWave;
                    float axisFilament = Mathf.Pow(1f - Mathf.Abs(v), 2.2f) * Mathf.Exp(-d / 0.085f) * 0.26f;

                    float alpha0 = Mathf.Clamp01((alongAlpha * scanlineMod + axisFilament) * lateralEnvelope) * sweepLevel;
                    Color c0 = baseTint;
                    c0.a = alpha0;

                    // Curve the far rings forward around the 3D target hull so the contact lip wraps the object
                    float arch = (1f - v * v) * WedgeArchHeight * (1f - u * 0.5f);
                    Vector3 wrapOffset = beamDir * (v * v * wrapRadius * targetWrapShare);
                    int idx0 = rowBase0 + s;
                    _wedgeVertices[idx0] = ringCenter + sheetDir0 * (v * halfW) + orthoUp * arch + wrapOffset;
                    _wedgeNormals[idx0] = orthoUp;
                    _wedgeUvs[idx0] = new Vector2(uvX, v01);
                    _wedgeColors[idx0] = c0;

                    float alpha1 = alpha0 * sheet1AlphaShare;
                    Color c1 = baseTint;
                    c1.a = alpha1;

                    int idx1 = rowBase1 + s;
                    _wedgeVertices[idx1] = ringCenter + sheetDir1 * (v * halfW) - orthoUp * (arch * 0.5f) + wrapOffset;
                    _wedgeNormals[idx1] = orthoUp;
                    _wedgeUvs[idx1] = new Vector2(uvX, v01);
                    _wedgeColors[idx1] = c1;
                }
            }

            _wedgeMesh.SetVertices(_wedgeVertices);
            _wedgeMesh.SetNormals(_wedgeNormals);
            _wedgeMesh.SetUVs(0, _wedgeUvs);
            _wedgeMesh.SetColors(_wedgeColors);
            _wedgeMesh.bounds = new Bounds((lensFront + contactCenter) * 0.5f, Vector3.one * (beamLen + 2f));
        }

        private void StepStreamingDataTicks(
            Vector3 lensFront,
            Vector3 contactCenter,
            Vector3 right,
            Vector3 orthoUp,
            Color cyanColor,
            Color amberColor,
            float sweepLevel,
            float widthScale)
        {
            Vector3 beamVec = contactCenter - lensFront;
            float beamLen = beamVec.magnitude;
            if (sweepLevel <= VisibleThreshold || beamLen <= 0.35f)
            {
                return;
            }

            Vector3 beamDir = beamVec / beamLen;
            float pulseSpeed = _scanTuning != null ? _scanTuning.BeamPulseSpeed : 3.2f;
            float targetHalfW = Mathf.Max(_lastTargetWidth * 0.36f, 0.08f);
            float lensHalfW = (_scanTuning != null ? _scanTuning.BeamLensWidth : DefaultBeamLensWidth) * 0.35f * widthScale;

            for (int i = 0; i < DataTickCount; i++)
            {
                // Stream from target (u = 0.96) toward 07's cheek lens (u = 0.06)
                float phase = Mathf.Repeat(_elapsed * (pulseSpeed * 0.42f) + (float)i / DataTickCount, 1f);
                float u = Mathf.Lerp(0.96f, 0.06f, phase * phase * (0.65f + 0.35f * phase));
                float env = Mathf.Sin(phase * Mathf.PI);

                float laneSign = (i & 1) == 0 ? -1f : 1f;
                float laneShare = ((i % 3) - 1) * 0.42f + laneSign * 0.24f;
                float halfW = Mathf.Lerp(lensHalfW, targetHalfW, u);
                Vector3 pos = lensFront + beamDir * (u * beamLen)
                    + right * (laneShare * halfW)
                    + orthoUp * (Mathf.Sin(u * Mathf.PI) * 0.012f * laneSign);

                float tickLen = Mathf.Lerp(0.032f, 0.068f, env) * widthScale;
                float tickWidth = 0.011f * widthScale;
                Color tickColor = Color.Lerp(amberColor, Color.Lerp(cyanColor, Color.white, 0.55f), (1f - u) * 0.35f);
                tickColor.a = Mathf.Min(0.32f, 0.28f * env * sweepLevel);
                _streaks.Add(pos, tickWidth, tickColor, -beamDir * tickLen);
            }
        }

        private void StepPulsingLaserLine(
            Vector3 lensFront,
            Vector3 contactCenter,
            Color cyanColor,
            Color amberColor,
            float sweepLevel,
            float widthScale)
        {
            Vector3 beamVec = contactCenter - lensFront;
            float beamLen = beamVec.magnitude;
            if (sweepLevel <= VisibleThreshold || beamLen <= 0.35f)
            {
                return;
            }

            Vector3 beamDir = beamVec / beamLen;
            float pulseSpeed = _scanTuning != null ? _scanTuning.BeamPulseSpeed : 3.2f;
            float segLen = (beamLen / DataTickCount) * 0.62f;

            for (int i = 0; i < DataTickCount; i++)
            {
                float u = (i + 0.5f) / DataTickCount;
                float wave = 0.5f + 0.5f * Mathf.Sin((u * 3.5f - _elapsed * (pulseSpeed * 0.55f)) * TwoPi);
                float endTaper = Mathf.Sin(u * Mathf.PI);
                Vector3 pos = lensFront + beamDir * (u * beamLen);

                float lineWidth = Mathf.Lerp(0.008f, 0.012f, wave) * widthScale;
                Color pulseColor = Color.Lerp(cyanColor, Color.Lerp(amberColor, Color.white, 0.65f), wave * 0.48f);
                pulseColor.a = Mathf.Min(0.24f, (0.09f + 0.13f * wave) * (0.45f + 0.55f * endTaper) * sweepLevel);
                _streaks.Add(pos, lineWidth, pulseColor, beamDir * segLen);
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
            int count = _scanTuning != null ? Mathf.Clamp(_scanTuning.PathGlintCount, 0, MaxPathGlints) : DefaultPathGlintCount;
            float baseSize = _scanTuning != null ? _scanTuning.PathGlintSize : DefaultPathGlintSize;
            float baseAlpha = Mathf.Min(_scanTuning != null ? _scanTuning.PathGlintAlpha : DefaultPathGlintAlpha, MaxBloomSafeGlintAlpha);
            float lifetime = Mathf.Clamp(_scanTuning != null ? _scanTuning.PathGlintLifetime : DefaultPathGlintLifetime, 0.10f, 0.25f);

            float dist = Vector3.Distance(lensFront, contactCenter);
            if (count <= 0 || baseAlpha <= VisibleThreshold || visibility <= VisibleThreshold || dist <= 0.65f)
            {
                return;
            }

            float lensHalfW = (_scanTuning != null ? _scanTuning.BeamLensWidth : DefaultBeamLensWidth) * 0.5f * widthScale;
            float targetHalfW = Mathf.Max(_lastTargetWidth * 0.5f, 0.12f);

            for (int i = 0; i < count; i++)
            {
                // Brief twinkle (0.10-0.25 s) with asynchronous cycle periods so only 1-2 subtle glints appear at a time
                float twinkleSeconds = Mathf.Clamp(lifetime * Mathf.Lerp(0.75f, 1.22f, Hash01(i * 17.13f + 0.31f)), 0.10f, 0.25f);
                float dutyCycle = PathActiveDutyCycle * Mathf.Lerp(0.85f, 1.15f, Hash01(i * 29.47f + 0.53f));
                float cyclePeriod = twinkleSeconds / Mathf.Max(dutyCycle, 0.20f);
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
                float minStartShare = Mathf.Max(PathStartShare, 0.75f / dist);
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

                float distanceScale = Mathf.Lerp(0.72f, 1.08f, u01);
                float grainScale = Mathf.Lerp(0.86f, 1.12f, Hash01(i * 97.41f + cycleIndex * 19.83f));
                float size = baseSize * distanceScale * grainScale * widthScale;

                // Single sprite per glint, capped strictly below the bloom threshold (alpha * 2.2 <= 0.53 < 1.25)
                float alpha = Mathf.Min(0.24f, baseAlpha * sparkleEnv) * visibility;
                Color glintColor = Color.Lerp(activeTint, Color.white, 0.22f);
                glintColor.a = alpha;
                _points.Add(grainPos, size, glintColor, Vector3.zero);
            }
        }

        private static Material CreateWedgeMaterial(Material source, ScanTuning scanTuning)
        {
            float density = scanTuning != null ? scanTuning.BeamScanlineDensity : DefaultBeamScanlineDensity;
            float contrast = scanTuning != null ? scanTuning.BeamScanlineContrast : DefaultBeamScanlineContrast;

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
            mat.SetFloat(BandCountShaderId, density);
            mat.SetFloat(BandSpeedShaderId, WedgeBandSpeed);
            mat.SetFloat(BandStrengthShaderId, contrast);
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
