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
        private const int PointSlots = CorePointCount + IrisRingCount + IrisCaliperCount + MoteCount + MaxPathGlints;
        private const int LensStreakCount = 6;
        private const int StreakSlots = LensStreakCount + MaxPathGlints;

        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float LensSurfaceStandOff = 0.022f;
        private const float MinWidthShare = 0.65f;
        private const float DefaultPingDuration = 0.38f;
        private const float TwoPi = Mathf.PI * 2f;

        private const float DefaultGroundLineWidth = 0.032f;
        private const float DefaultGroundLineIntensity = 2.05f;
        private const float DefaultGroundSweepAngleDeg = 4.2f;
        private const float DefaultGroundSweepHz = 1.15f;
        private const float DefaultGroundStartOffset = 0.08f;
        private const float DefaultLensIrisSize = 0.145f;
        private const float DefaultLensIrisAlpha = 0.90f;
        private const float DefaultLensGlintLength = 0.44f;
        private const float DefaultLensGlintWidth = 0.015f;
        private const float DefaultLensGlintAlpha = 0.92f;
        private const float DefaultLensSpillIntensity = 0.65f;
        private const float DefaultLensSpillRange = 0.95f;
        private const int DefaultPathGlintCount = 10;
        private const float DefaultPathGlintSize = 0.082f;
        private const float DefaultPathGlintAlpha = 0.96f;
        private const float DefaultPathGlintLifetime = 0.42f;
        private const float DefaultLunarGravity = 1.62f;

        private const float SpillForwardOffset = 0.10f;
        private const float SpillInwardOffset = 0.03f;
        private const float SpillDownwardOffset = -0.02f;
        private const float SpillFlickerShare = 0.06f;
        private const float SpillFlickerHz = 7.5f;
        private const float RoverChassisClearanceBelowLens = 0.12f;
        private const float IrisClosedScale = 0.38f;
        private const float IrisCaliperRadiusFactor = 0.56f;
        private const float PathStartShare = 0.08f;
        private const float PathEndShare = 0.92f;
        private const float PathActiveDutyCycle = 0.82f;
        private const float PathLateralJitter = 0.032f;
        private const float PathUpwardLoftVelocity = 0.085f;
        private const float PathHdrBoost = 2.15f;

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
            float moteRadius = _scanTuning != null ? _scanTuning.LensMoteRadius : 0.065f;
            float moteSizeBase = _scanTuning != null ? _scanTuning.LensMoteSize : 0.020f;
            float moteAlphaBase = _scanTuning != null ? _scanTuning.LensMoteAlpha : 0.65f;

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

            // Sweep fan oscillation shared by the ground line, lens glint, and path glints
            float sweepAngleDeg = _scanTuning != null ? _scanTuning.GroundSweepAngleDeg : DefaultGroundSweepAngleDeg;
            float sweepHz = _scanTuning != null ? _scanTuning.GroundSweepHz : DefaultGroundSweepHz;
            float maxSweepTan = Mathf.Tan(sweepAngleDeg * Mathf.Deg2Rad);
            float sweepOsc = Mathf.Sin((_progress * 1.75f + _elapsed * sweepHz * 0.45f) * TwoPi);
            float currentSweepTan = sweepOsc * maxSweepTan * (1f - 0.40f * ping01);

            // 1. Update ground scan line shader globals (runs from under 07's cheek lens to the target's base)
            UpdateGroundLineGlobals(emitter, sweepLevel, 0f, currentSweepTan, maxSweepTan);

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

            // 3. 07's cheek lens emitter core, outer halo, opening iris ring, and cross-flare glints
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

            // Bright horizontal iris caliper ticks that shift slightly with the sweep oscillation
            Color caliperColor = Color.Lerp(activeTint, Color.white, 0.62f);
            caliperColor.a = Mathf.Min(1f, irisAlpha * 1.15f) * visibility;
            float caliperRadius = irisSize * IrisCaliperRadiusFactor * Mathf.Lerp(IrisClosedScale, 1f, visibility);
            Vector3 sweepShift = right * (currentSweepTan * 0.10f);
            _points.Add(lensFront + right * caliperRadius + sweepShift, irisSegmentSize * 1.45f, caliperColor, Vector3.zero);
            _points.Add(lensFront - right * caliperRadius + sweepShift, irisSegmentSize * 1.45f, caliperColor, Vector3.zero);

            // Razor-thin optical lens flare / cross-glint needles at the lens aperture
            if (glintAlpha > VisibleThreshold)
            {
                Color streakColor = Color.Lerp(activeTint, Color.white, 0.48f);
                streakColor.a = glintAlpha * visibility;
                float streakWidth = glintWidth * widthScale;
                float horizLen = glintLength * 0.62f * widthScale;
                float vertLen = glintLength * 0.46f * widthScale;
                Vector3 sweepDir = (forward + right * currentSweepTan).normalized;
                float exitGlintLen = glintLength * 0.82f * widthScale;

                _streaks.Add(lensFront, streakWidth, streakColor, right * horizLen);
                _streaks.Add(lensFront, streakWidth, streakColor, -right * horizLen);
                _streaks.Add(lensFront, streakWidth * 0.88f, streakColor, orthoUp * vertLen);
                _streaks.Add(lensFront, streakWidth * 0.88f, streakColor, -orthoUp * vertLen);

                // Short aperture exit glint pointing along the active sweep fan direction right out of the bezel
                if (sweepLevel > VisibleThreshold)
                {
                    Color exitColor = Color.Lerp(activeTint, Color.white, 0.35f);
                    exitColor.a = glintAlpha * 0.85f * sweepLevel;
                    _streaks.Add(lensFront + sweepDir * 0.015f, streakWidth * 0.85f, exitColor, sweepDir * exitGlintLen);
                    _streaks.Add(lensFront + sweepDir * (0.015f + exitGlintLen * 0.55f), streakWidth * 0.68f, exitColor, -sweepDir * (exitGlintLen * 0.55f));
                }
            }

            // Near-lens motes
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
                        + forward * (0.025f * life01);
                    Color moteColor = cyanColor;
                    moteColor.a = moteAlphaBase * env * sweepLevel;
                    float size = moteSizeBase * Mathf.Lerp(0.75f, 1.15f, i & 1) * widthScale;
                    _points.Add(lensFront + offset, size, moteColor, Vector3.zero);
                }
            }

            // 4. Sparse, brief ballistic dust glints along the beam path falling under 1/6 g lunar gravity
            StepBallisticPathGlints(lensFront, forward, right, orthoUp, dist, currentSweepTan, cyanColor, sweepLevel, widthScale);
        }

        private void UpdateGroundLineGlobals(
            Vector3 emitter,
            float visibility,
            float ping01,
            float currentSweepTan,
            float maxSweepTan)
        {
            Vector2 toTargetXZ = new Vector2(_target.x - emitter.x, _target.z - emitter.z);
            float distXZ = toTargetXZ.magnitude;
            Vector2 forwardXZ = distXZ > MinDistance ? toTargetXZ / distXZ : new Vector2(0f, 1f);

            float startOffset = _scanTuning != null ? _scanTuning.GroundStartOffset : DefaultGroundStartOffset;
            Vector2 startXZ = new Vector2(emitter.x, emitter.z) + forwardXZ * startOffset;
            float roverGroundCeilingY = emitter.y - RoverChassisClearanceBelowLens;
            float targetBaseY = _target.y - Mathf.Max(_span.y * 0.5f, 0.1f);
            float encodedProgress = _pingRemaining > VisibleThreshold ? (1f + ping01) : _progress;

            float lineWidth = _scanTuning != null ? _scanTuning.GroundLineWidth : DefaultGroundLineWidth;
            float lineIntensity = _scanTuning != null ? _scanTuning.GroundLineIntensity : DefaultGroundLineIntensity;

            var groundStart = new Vector4(startXZ.x, roverGroundCeilingY, startXZ.y, visibility);
            var groundEnd = new Vector4(_target.x, targetBaseY, _target.z, encodedProgress);
            var groundParams = new Vector4(lineWidth, lineIntensity, currentSweepTan, maxSweepTan);
            ScanTuning.SetGroundLineGlobals(groundStart, groundEnd, groundParams);
            _groundGlobalsActive = true;
        }

        private void StepBallisticPathGlints(
            Vector3 lensFront,
            Vector3 forward,
            Vector3 right,
            Vector3 orthoUp,
            float dist,
            float currentSweepTan,
            Color activeTint,
            float visibility,
            float widthScale)
        {
            int count = _scanTuning != null ? Mathf.Clamp(_scanTuning.PathGlintCount, 0, MaxPathGlints) : DefaultPathGlintCount;
            float baseSize = _scanTuning != null ? _scanTuning.PathGlintSize : DefaultPathGlintSize;
            float baseAlpha = _scanTuning != null ? _scanTuning.PathGlintAlpha : DefaultPathGlintAlpha;
            float lifetime = Mathf.Max(0.15f, _scanTuning != null ? _scanTuning.PathGlintLifetime : DefaultPathGlintLifetime);
            float gravity = _scanTuning != null ? _scanTuning.PathGlintGravity : DefaultLunarGravity;

            if (count <= 0 || baseAlpha <= VisibleThreshold || visibility <= VisibleThreshold || dist <= 0.35f)
            {
                return;
            }

            float halfSpanY = Mathf.Max(_span.y * 0.45f, 0.22f);
            Vector3 sweepTargetPoint = new Vector3(
                _target.x,
                Mathf.Lerp(_target.y - halfSpanY, _target.y + halfSpanY, _progress),
                _target.z);

            float distanceClamp = Mathf.Min(1f, 2.8f / Mathf.Max(dist, 1f));
            float effectiveSweepTan = currentSweepTan * distanceClamp;

            for (int i = 0; i < count; i++)
            {
                float seed = i * 0.61803399f;
                float cyclePhase = (_elapsed / lifetime) + i * 0.37f + _progress * 1.15f;
                float cycleIndex = Mathf.Floor(cyclePhase);
                float cycleFrac = cyclePhase - cycleIndex;
                if (cycleFrac > PathActiveDutyCycle)
                {
                    continue;
                }

                float life01 = cycleFrac / PathActiveDutyCycle;
                float ageSeconds = life01 * lifetime;
                float sparkleEnv = Mathf.Sin(life01 * Mathf.PI);

                // Distribute grains sparsely along the path from lensFront (0.08) to the target (0.92)
                float slotShare = (i + 0.5f) / count;
                float jitter = (Mathf.Sin(cycleIndex * 1.73f + i * 2.41f) * 0.45f) / count;
                float u01 = Mathf.Clamp(Mathf.Lerp(PathStartShare, PathEndShare, slotShare + jitter), PathStartShare, PathEndShare);

                // Position along the active straight laser fan plane between 07's lens and the target
                float lateralFan = u01 * dist * effectiveSweepTan;
                float fanHeight = Mathf.Sin((seed + cycleIndex * 0.29f) * TwoPi) * Mathf.Lerp(0.03f, halfSpanY * 0.38f, u01);
                float lateralDrift = Mathf.Cos((seed * 1.7f + cycleIndex * 0.53f) * TwoPi) * PathLateralJitter;

                Vector3 rayAnchor = Vector3.Lerp(lensFront, sweepTargetPoint, u01)
                    + right * (lateralFan + lateralDrift)
                    + orthoUp * fanHeight;

                // 1/6 g lunar ballistic trajectory: y(t) = v0_y * t - 0.5 * g * t^2
                Vector3 initialVelocity = right * (lateralDrift * 0.8f) + Vector3.up * PathUpwardLoftVelocity;
                Vector3 ballisticDisplacement = initialVelocity * ageSeconds
                    + 0.5f * Vector3.down * gravity * ageSeconds * ageSeconds;
                Vector3 instantaneousVelocity = initialVelocity + Vector3.down * gravity * ageSeconds;
                Vector3 grainPos = rayAnchor + ballisticDisplacement;

                Color glintColor = Color.Lerp(activeTint, Color.white, 0.52f) * PathHdrBoost;
                glintColor.a = baseAlpha * sparkleEnv * visibility;
                float size = baseSize * Mathf.Lerp(0.84f, 1.25f, i & 1) * widthScale;

                _points.Add(grainPos, size, glintColor, Vector3.zero);

                // Brief ballistic glint needle showing the grain catching the laser as it falls under 1/6 g
                Vector3 streakVec = instantaneousVelocity * 0.14f + forward * (size * 1.15f);
                Color streakColor = glintColor;
                streakColor.a *= 0.85f;
                _streaks.Add(grainPos, size * 0.24f, streakColor, streakVec);
            }
        }
    }
}

