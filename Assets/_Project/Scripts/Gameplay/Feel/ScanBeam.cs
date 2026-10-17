using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's vacuum scan emitter, ground line driver, and ballistic path glints (Task SCAN v3):
    /// establishes a clear visual connection from 07 to the scanned object without breaking lunar vacuum logic:
    /// 1. Drives the anti-aliased ground/prop laser intersection line from under 07's cheek lens to the target's base.
    /// 2. Brings 07's brass cheek lens alive: opening iris ring, hot emitter core, cross-flare glint streaks, and a
    ///    soft cyan point light spilling onto 07's face, bezel, and the regolith right in front of the lens.
    /// 3. Spawns sparse, brief dust grain glints along the beam path that fall ballistically under 1/6 g lunar gravity.
    /// Zero allocations per frame.
    /// </summary>
    public sealed class ScanBeam
    {
        private const int CorePointCount = 3;
        private const int IrisRingCount = 12;
        private const int IrisCaliperCount = 2;
        private const int MoteCount = 6;
        private const int MaxPathGlints = 16;
        private const int PointSlots = CorePointCount + IrisRingCount + IrisCaliperCount + MoteCount + MaxPathGlints * 2;
        private const int LensStreakCount = 6;
        private const int StreakSlots = LensStreakCount + MaxPathGlints;

        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float LensSurfaceStandOff = 0.022f;
        private const float MinWidthShare = 0.65f;
        private const float DefaultPingDuration = 0.38f;
        private const float TwoPi = Mathf.PI * 2f;

        private const float DefaultGroundLineWidth = 0.015f;
        private const float DefaultGroundLineIntensity = 1.65f;
        private const float DefaultGroundStartOffset = 0.85f;
        private const float DefaultLensIrisSize = 0.145f;
        private const float DefaultLensIrisAlpha = 0.90f;
        private const float DefaultLensGlintLength = 0.068f;
        private const float DefaultLensGlintWidth = 0.009f;
        private const float DefaultLensGlintAlpha = 0.85f;
        private const float DefaultLensSpillIntensity = 0.65f;
        private const float DefaultLensSpillRange = 0.95f;
        private const int DefaultPathGlintCount = 5;
        private const float DefaultPathGlintSize = 0.020f;
        private const float DefaultPathGlintAlpha = 0.95f;
        private const float DefaultPathGlintLifetime = 0.20f;
        private const float DefaultLunarGravity = 1.62f;

        private const float SpillForwardOffset = 0.10f;
        private const float SpillInwardOffset = 0.03f;
        private const float SpillDownwardOffset = -0.02f;
        private const float SpillFlickerShare = 0.06f;
        private const float SpillFlickerHz = 7.5f;
        private const float RoverChassisClearanceBelowLens = 0.12f;
        private const float IrisClosedScale = 0.38f;
        private const float IrisCaliperRadiusFactor = 0.56f;
        private const float PathStartShare = 0.22f;
        private const float PathEndShare = 0.90f;
        private const float PathActiveDutyCycle = 0.54f;
        private const float PathLateralJitter = 0.014f;
        private const float PathUpwardLoftVelocity = 0.012f;

        private static readonly Color DefaultCyanColor = new Color(0.20f, 0.86f, 0.96f, 1f);
        private static readonly Color DefaultAmberPingColor = new Color(1.00f, 0.682f, 0.220f, 1f);

        private readonly FeelVfxTuning _tuning;
        private readonly ScanTuning _scanTuning;
        private readonly FeelSprites _points;
        private readonly FeelSprites _streaks;
        private readonly LightPulse _lensLight;

        private Vector3 _target;
        private Vector3 _span;
        private float _progress;
        private float _rate;
        private float _level;
        private float _elapsed;
        private float _pingRemaining;
        private float _pingDuration = DefaultPingDuration;
        private bool _active;
        private bool _groundGlobalsActive;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial,
            ScanTuning scanTuning = null)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning;
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

            _points = new FeelSprites("ScanBeamPoints", parent, spriteMaterial, PointSlots, false, 0f);
            _streaks = new FeelSprites("ScanBeamStreaks", parent, spriteMaterial, StreakSlots, true, 1f);

            Color initialLightColor = _scanTuning != null ? _scanTuning.ScanLineColor : DefaultCyanColor;
            float initialLightRange = _scanTuning != null ? _scanTuning.LensSpillRange : DefaultLensSpillRange;
            _lensLight = new LightPulse("ScanLensLight", parent, initialLightColor, initialLightRange);
            _lensLight.Flicker(SpillFlickerShare, SpillFlickerHz);
        }

        /// <summary>True while the lens emitter glow or its completion ping is active or easing out.</summary>
        public bool Shown => _level > VisibleThreshold || _pingRemaining > VisibleThreshold;

        /// <summary>Current eased visibility level (0..1).</summary>
        public float Level => Ease.InOutSine(_level);

        /// <summary>True while the completion ping flash is active.</summary>
        public bool IsPinging => _pingRemaining > VisibleThreshold;

        /// <summary>Number of point sprites emitted on the most recent frame (for tests and diagnostics).</summary>
        public int ActivePointCount => _points.Count;

        /// <summary>Number of streak sprites emitted on the most recent frame (for tests and diagnostics).</summary>
        public int ActiveStreakCount => _streaks.Count;

        /// <summary>Current intensity of 07's cheek lens spill light.</summary>
        public float LensLightIntensity => _lensLight.Intensity;

        /// <summary>Computes the vertical ballistic displacement (meters) of a dust grain after <paramref name="ageSeconds"/> in 1/6 g.</summary>
        public float EvaluateBallisticVerticalOffset(float ageSeconds)
        {
            float gravity = _scanTuning != null ? _scanTuning.PathGlintGravity : DefaultLunarGravity;
            return PathUpwardLoftVelocity * ageSeconds - 0.5f * gravity * ageSeconds * ageSeconds;
        }

        /// <summary>Starts or resumes the scan emitter glow at <paramref name="progress"/> (0..1) over <paramref name="remainingSeconds"/>.</summary>
        public void Begin(Vector3 target, Vector3 span, float progress, float remainingSeconds)
        {
            _target = target;
            _span = span;
            _progress = Mathf.Clamp01(progress);
            _rate = remainingSeconds > MinFadeSeconds ? (1f - _progress) / remainingSeconds : 0f;
            _pingRemaining = 0f;
            _active = true;
        }

        /// <summary>Keeps progress in step with quarter beats.</summary>
        public void Sync(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>Eases the lens glow out on let-go.</summary>
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
            else
            {
                if (_groundGlobalsActive)
                {
                    ScanTuning.ClearGroundLineGlobals();
                    _groundGlobalsActive = false;
                }

                _lensLight.Ramp(0f, 0f, false);
                _lensLight.Step(deltaTime);
            }

            _points.End();
            _streaks.End();
        }

        private void StepScanVisuals(float deltaTime, Vector3 emitter)
        {
            Color cyanColor = _scanTuning != null ? _scanTuning.ScanLineColor : DefaultCyanColor;
            Color amberColor = _scanTuning != null ? _scanTuning.CompletionPingColor : DefaultAmberPingColor;
            float coreSize = _scanTuning != null ? _scanTuning.LensGlowSize : _tuning.ScanLensCoreSize;
            float coreAlpha = _scanTuning != null ? _scanTuning.LensGlowAlpha : _tuning.ScanLensCoreAlpha;
            float haloSize = _scanTuning != null ? _scanTuning.LensHaloSize : _tuning.ScanLensHaloSize;
            float haloAlpha = _scanTuning != null ? _scanTuning.LensHaloAlpha : _tuning.ScanLensHaloAlpha;
            float irisSize = _scanTuning != null ? _scanTuning.LensIrisSize : DefaultLensIrisSize;
            float irisAlpha = _scanTuning != null ? _scanTuning.LensIrisAlpha : DefaultLensIrisAlpha;
            float glintLength = _scanTuning != null ? _scanTuning.LensGlintLength : DefaultLensGlintLength;
            float glintWidth = _scanTuning != null ? _scanTuning.LensGlintWidth : DefaultLensGlintWidth;
            float glintAlpha = _scanTuning != null ? _scanTuning.LensGlintAlpha : DefaultLensGlintAlpha;
            float moteRadius = _scanTuning != null ? _scanTuning.LensMoteRadius : 0.052f;
            float moteSizeBase = _scanTuning != null ? _scanTuning.LensMoteSize : 0.007f;
            float moteAlphaBase = _scanTuning != null ? _scanTuning.LensMoteAlpha : 0.42f;

            float eased = Ease.InOutSine(_level);
            float ping01 = _pingRemaining > 0f
                ? Mathf.Sin((1f - _pingRemaining / Mathf.Max(_pingDuration, MinFadeSeconds)) * Mathf.PI)
                : 0f;
            float sweepLevel = _pingRemaining > 0f ? 0f : eased;
            float visibility = Mathf.Max(sweepLevel, ping01);
            float widthScale = Mathf.Lerp(MinWidthShare, 1f, visibility);

            Vector3 toTarget = _target - emitter;
            float dist = toTarget.magnitude;
            Vector3 forward = dist > MinDistance ? toTarget / dist : Vector3.forward;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward);
            right = right.sqrMagnitude > MinDistance ? right.normalized : Vector3.right;
            Vector3 orthoUp = Vector3.Cross(forward, right).normalized;
            Vector3 lensFront = emitter + forward * LensSurfaceStandOff;

            // 1. Update straight ground scan line shader globals (runs from lens's ground projection to the target)
            UpdateGroundLineGlobals(emitter, sweepLevel, 0f);

            // 2. Soft cyan spill light on 07's CRT face, brass bezel, and the ground right in front of the lens
            Color activeTint = _pingRemaining > 0f ? amberColor : cyanColor;
            float spillIntensity = (_scanTuning != null ? _scanTuning.LensSpillIntensity : DefaultLensSpillIntensity) * sweepLevel;
            float spillRange = _scanTuning != null ? _scanTuning.LensSpillRange : DefaultLensSpillRange;
            Vector3 spillPos = emitter
                + forward * SpillForwardOffset
                + right * SpillInwardOffset
                - orthoUp * SpillDownwardOffset;
            _lensLight.MoveTo(spillPos);
            _lensLight.SetColor(Color.Lerp(activeTint, Color.white, 0.12f));
            _lensLight.SetRange(spillRange);
            _lensLight.Ramp(spillIntensity, 0f, false);
            _lensLight.Step(deltaTime);

            // 3. 07's cheek lens emitter core, outer halo, opening iris ring, and short bezel-clamped flare needles
            Color halo = activeTint;
            halo.a = haloAlpha * visibility;
            _points.Add(lensFront, haloSize * widthScale, halo, Vector3.zero);

            Color core = Color.Lerp(activeTint, Color.white, 0.40f);
            core.a = coreAlpha * visibility;
            _points.Add(lensFront, coreSize * widthScale, core, Vector3.zero);

            Color diodeHot = Color.Lerp(activeTint, Color.white, 0.78f);
            diodeHot.a = Mathf.Min(1f, coreAlpha * 1.12f) * visibility;
            _points.Add(lensFront + forward * 0.008f, coreSize * 0.45f * widthScale, diodeHot, Vector3.zero);

            // Opening iris aperture ring around the cheek lens bezel
            float openIrisRadius = irisSize * 0.5f * Mathf.Lerp(IrisClosedScale, 1f, visibility);
            float irisSegmentSize = Mathf.Max(0.013f, irisSize * 0.11f) * widthScale;
            Color irisColor = Color.Lerp(activeTint, Color.white, 0.30f);
            irisColor.a = irisAlpha * visibility;
            float irisSpin = _elapsed * 0.35f + _progress * 0.8f;
            for (int i = 0; i < IrisRingCount; i++)
            {
                float angle = irisSpin + i * (TwoPi / IrisRingCount);
                Vector3 ringOffset = (right * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * openIrisRadius;
                _points.Add(lensFront + ringOffset, irisSegmentSize, irisColor, Vector3.zero);
            }

            // Bright vertical iris caliper ticks that shift slightly with the vertical fan sweep
            Color caliperColor = Color.Lerp(activeTint, Color.white, 0.62f);
            caliperColor.a = Mathf.Min(1f, irisAlpha * 1.15f) * visibility;
            float caliperRadius = irisSize * IrisCaliperRadiusFactor * Mathf.Lerp(IrisClosedScale, 1f, visibility);
            float verticalTickShift = Mathf.Sin((_progress * 1.75f + _elapsed * 0.5f) * TwoPi) * 0.006f;
            Vector3 sweepShift = orthoUp * verticalTickShift;
            _points.Add(lensFront + right * caliperRadius + sweepShift, irisSegmentSize * 1.45f, caliperColor, Vector3.zero);
            _points.Add(lensFront - right * caliperRadius + sweepShift, irisSegmentSize * 1.45f, caliperColor, Vector3.zero);

            // Short optical lens flare needles clamped strictly to the lens bezel (never crossing 07's CRT screen or face)
            if (glintAlpha > VisibleThreshold)
            {
                Color streakColor = Color.Lerp(activeTint, Color.white, 0.45f);
                streakColor.a = glintAlpha * visibility;
                float streakWidth = glintWidth * widthScale;
                float bezelRadius = irisSize * 0.5f;
                float horizLen = Mathf.Min(glintLength * 0.28f, bezelRadius * 0.24f) * widthScale;
                float vertLen = Mathf.Min(glintLength, bezelRadius * 0.94f) * widthScale;

                _streaks.Add(lensFront, streakWidth * 0.85f, streakColor, right * horizLen);
                _streaks.Add(lensFront, streakWidth * 0.85f, streakColor, -right * horizLen);
                _streaks.Add(lensFront, streakWidth, streakColor, orthoUp * vertLen);
                _streaks.Add(lensFront, streakWidth, streakColor, -orthoUp * vertLen);
            }

            // Tiny near-lens motes
            if (moteAlphaBase > VisibleThreshold && sweepLevel > VisibleThreshold)
            {
                for (int i = 0; i < MoteCount; i++)
                {
                    float phase = _elapsed * 0.95f + i * 0.41f;
                    float life01 = phase - Mathf.Floor(phase);
                    float env = Mathf.Sin(life01 * Mathf.PI);
                    float angle = i * 1.0471976f + _elapsed * 0.55f;
                    float radial = moteRadius * (0.35f + 0.65f * life01);
                    Vector3 offset = (right * Mathf.Cos(angle) + orthoUp * Mathf.Sin(angle)) * radial
                        + forward * (0.018f * life01);
                    Color moteColor = cyanColor;
                    moteColor.a = moteAlphaBase * env * sweepLevel;
                    float size = moteSizeBase * Mathf.Lerp(0.75f, 1.10f, i & 1) * widthScale;
                    _points.Add(lensFront + offset, size, moteColor, Vector3.zero);
                }
            }

            // 4. Sparse, brief (0.1-0.3 s) tiny dust sparkles along the beam path falling ballistically under 1/6 g
            StepBallisticPathGlints(lensFront, right, orthoUp, dist, cyanColor, sweepLevel, widthScale);
        }

        private void UpdateGroundLineGlobals(Vector3 emitter, float visibility, float ping01)
        {
            float startOffset = _scanTuning != null ? _scanTuning.GroundStartOffset : DefaultGroundStartOffset;
            float roverGroundCeilingY = emitter.y - RoverChassisClearanceBelowLens;
            float targetBaseY = _target.y - Mathf.Max(_span.y * 0.5f, 0.1f);
            float encodedProgress = _pingRemaining > VisibleThreshold ? (1f + ping01) : _progress;

            float lineWidth = _scanTuning != null ? _scanTuning.GroundLineWidth : DefaultGroundLineWidth;
            float lineIntensity = _scanTuning != null ? _scanTuning.GroundLineIntensity : DefaultGroundLineIntensity;

            var groundStart = new Vector4(emitter.x, roverGroundCeilingY, emitter.z, visibility);
            var groundEnd = new Vector4(_target.x, targetBaseY, _target.z, encodedProgress);
            var groundParams = new Vector4(lineWidth, lineIntensity, startOffset, 0f);
            ScanTuning.SetGroundLineGlobals(groundStart, groundEnd, groundParams);
            _groundGlobalsActive = true;
        }

        private void StepBallisticPathGlints(
            Vector3 lensFront,
            Vector3 right,
            Vector3 orthoUp,
            float dist,
            Color activeTint,
            float visibility,
            float widthScale)
        {
            int count = _scanTuning != null ? Mathf.Clamp(_scanTuning.PathGlintCount, 0, MaxPathGlints) : DefaultPathGlintCount;
            float baseSize = _scanTuning != null ? _scanTuning.PathGlintSize : DefaultPathGlintSize;
            float baseAlpha = _scanTuning != null ? _scanTuning.PathGlintAlpha : DefaultPathGlintAlpha;
            float lifetime = Mathf.Clamp(_scanTuning != null ? _scanTuning.PathGlintLifetime : DefaultPathGlintLifetime, 0.10f, 0.30f);

            if (count <= 0 || baseAlpha <= VisibleThreshold || visibility <= VisibleThreshold || dist <= 0.65f)
            {
                return;
            }

            float halfSpanY = Mathf.Max(_span.y * 0.45f, 0.22f);
            Vector3 sweepTargetPoint = new Vector3(
                _target.x,
                Mathf.Lerp(_target.y - halfSpanY, _target.y + halfSpanY, _progress),
                _target.z);

            for (int i = 0; i < count; i++)
            {
                // Brief twinkle (0.10-0.30 s) with asynchronous cycle periods so only a few sparkles appear at a time
                float twinkleSeconds = Mathf.Clamp(lifetime * Mathf.Lerp(0.68f, 1.32f, Hash01(i * 17.13f + 0.31f)), 0.10f, 0.30f);
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

                // Random position along the path (no regular slot spacing) and random height inside the vertical laser fan
                float hU = Hash01(i * 53.37f + cycleIndex * 97.13f + 0.19f);
                float scrambledU = Mathf.Repeat(hU + i * 0.38196601f, 1f);
                float minStartShare = Mathf.Max(PathStartShare, 0.95f / dist);
                float u01 = Mathf.Lerp(minStartShare, PathEndShare, scrambledU);

                float hHeight = Hash01(i * 71.29f + cycleIndex * 61.43f + 0.41f) * 2f - 1f;
                float fanHeight = hHeight * Mathf.Lerp(0.06f, Mathf.Max(halfSpanY * 0.65f, 0.24f), u01);
                float hLat = Hash01(i * 89.17f + cycleIndex * 37.59f + 0.67f) * 2f - 1f;
                float lateralDrift = hLat * PathLateralJitter;

                Vector3 rayAnchor = Vector3.Lerp(lensFront, sweepTargetPoint, u01)
                    + right * lateralDrift
                    + orthoUp * fanHeight;

                // 1/6 g lunar ballistic trajectory: y(t) = v0_y * t - 0.5 * g * t^2
                float verticalOffset = EvaluateBallisticVerticalOffset(ageSeconds);
                Vector3 grainPos = rayAnchor + Vector3.up * verticalOffset;

                // Tiny 1-3 px sparkle at chase distance, scaled gently with u01 so near grains never bloat into balls
                float distanceScale = Mathf.Lerp(0.68f, 1.12f, u01);
                float grainScale = Mathf.Lerp(0.84f, 1.14f, Hash01(i * 97.41f + cycleIndex * 19.83f));
                float size = baseSize * distanceScale * grainScale * widthScale;

                float alpha = Mathf.Min(1f, baseAlpha * sparkleEnv * 1.15f) * visibility;
                Color glintColor = Color.Lerp(activeTint, Color.white, 0.15f);
                glintColor.a = alpha;
                _points.Add(grainPos, size, glintColor, Vector3.zero);

                Color pinpointColor = Color.Lerp(activeTint, Color.white, 0.58f);
                pinpointColor.a = alpha;
                _points.Add(grainPos, size * 0.78f, pinpointColor, Vector3.zero);
            }
        }

        private static float Hash01(float seed)
        {
            float s = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }
    }
}

