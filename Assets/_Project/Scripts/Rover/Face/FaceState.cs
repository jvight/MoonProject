using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Pure C# state machine and parameter evaluator for 07's CRT face (0.5.7 slice 9). Eases eye parameters toward
    /// the active <see cref="FaceFeeling"/>, maintains lagged phosphor-trail ghost parameters, schedules blinks and
    /// micro-saccades with a seeded <see cref="System.Random"/> (never while asleep), advances the power-on boot
    /// sequence, fades corner/status symbols, maps head-local look directions, and computes low-power dimming/jitter.
    /// </summary>
    public sealed class FaceState
    {
        private const float InitialBlinkDelay = 1.5f;
        private const float InitialSaccadeDelay = 2.0f;
        private const float SymbolFadeCutoff = 0.005f;
        private const float BaseFlickerFloor = 0.96f;
        private const float BaseFlickerAmplitude = 0.04f;
        private const float FlickerFreqA = 23.0f;
        private const float FlickerFreqB = 7.0f;
        private const float MaxCombinedLookX = 0.28f;
        private const float MaxCombinedLookY = 0.25f;

        private readonly FaceTuning _tuning;
        private readonly System.Random _random;

        private float _width;
        private float _height;
        private float _open;
        private float _tilt;
        private float _cut;
        private float _squint;
        private float _arc;
        private float _ring;
        private float _closed;
        private float _lookX;
        private float _lookY;
        private float _asymmetry;

        private float _targetExternalLookX;
        private float _targetExternalLookY;
        private float _externalLookX;
        private float _externalLookY;

        private float _saccadeX;
        private float _saccadeY;
        private float _nextSaccadeCountdown;

        private float _nextBlinkCountdown;
        private float _blinkElapsed;
        private float _activeBlinkDuration;
        private bool _isDarkHold;
        private bool _isReadout;

        private float _elapsedTime;

        private enum ScreenPhase
        {
            Eyes = 0,
            OpeningLeadIn = 1,
            OpeningCollapse = 2,
            OpeningLineHold = 3,
            OpeningExpand = 4,
            ScreenContent = 5,
            ClosingCollapse = 6,
            ClosingExpand = 7,
        }

        private bool _isScreenModeOpen;
        private ScreenPhase _screenPhase;
        private float _screenPhaseElapsed;
        private float _closingHappyTimer;
        private bool _scanFocusActive;
        private float _scanFocusBlend;
        private float _scanProgress;

        public FaceState(FaceTuning tuning, int seed = 7)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _random = new System.Random(seed);

            Feeling = FaceFeeling.Calm;
            BootProgress = 1.0f;
            BlinkMultiplier = 1.0f;
            _activeBlinkDuration = _tuning.BlinkDuration;
            _blinkElapsed = _activeBlinkDuration;
            _nextBlinkCountdown = InitialBlinkDelay;
            _nextSaccadeCountdown = InitialSaccadeDelay;

            SnapToFeeling(FaceFeeling.Calm);
        }

        /// <summary>Currently targeted feeling.</summary>
        public FaceFeeling Feeling { get; private set; }

        /// <summary>Current primary left-eye parameters.</summary>
        public FaceEyeParams LeftEye { get; private set; }

        /// <summary>Current primary right-eye parameters.</summary>
        public FaceEyeParams RightEye { get; private set; }

        /// <summary>Lagged phosphor-trail ghost parameters for the left eye.</summary>
        public FaceEyeParams GhostLeftEye { get; private set; }

        /// <summary>Lagged phosphor-trail ghost parameters for the right eye.</summary>
        public FaceEyeParams GhostRightEye { get; private set; }

        /// <summary>Currently active corner/status symbol (persists while fading out).</summary>
        public FaceSymbol ActiveSymbol { get; private set; }

        /// <summary>Current symbol opacity in [0..1].</summary>
        public float SymbolAlpha { get; private set; }

        /// <summary>A scan is running: 07's face shows its scanning look.</summary>
        public bool ScanFocusActive => _scanFocusActive;

        /// <summary>Normalized scan-focus eye narrowing blend in [0..1].</summary>
        public float ScanFocusBlend => _scanFocusBlend;

        /// <summary>Normalized scan progress in [0..1] displayed on the CRT progress ring/tick.</summary>
        public float ScanProgress => _scanProgress;

        /// <summary>Shader-ready scan ring progress value (<c>0</c> when inactive, <c>[0.04..1]</c> while scanning).</summary>
        public float ScanRingValue => _scanFocusBlend > 0.01f ? Math.Max(0.04f, _scanProgress) * _scanFocusBlend : 0.0f;

        /// <summary>Normalized boot progress in [0..1].</summary>
        public float BootProgress { get; private set; }

        /// <summary>True while the power-on boot sequence is still progressing (<c>BootProgress &lt; 1</c>).</summary>
        public bool IsBooting => BootProgress < 1.0f;

        /// <summary>True when an external gaze target is active.</summary>
        public bool HasLookTarget => Math.Abs(_targetExternalLookX) > 1e-5f || Math.Abs(_targetExternalLookY) > 1e-5f;

        /// <summary>True while the boot sequence is in its initial horizontal power-on line stage (<c>&lt; 0.45</c>).</summary>
        public bool IsBootLineStage => BootProgress < _tuning.BootLineEnd;

        /// <summary>Normalized horizontal expansion of the power-on boot line in [0..1].</summary>
        public float BootLineProgress => IsBootLineStage
            ? Clamp01(BootProgress / Math.Max(_tuning.BootLineEnd, 1e-4f))
            : 1.0f;

        /// <summary>Vertical eye openness multiplier contributed by the boot sequence in [0..1].</summary>
        public float BootEyeOpen => IsBootLineStage
            ? 0.0f
            : Clamp01((BootProgress - _tuning.BootLineEnd) / Math.Max(_tuning.BootOpenSpan, 1e-4f));

        /// <summary>Vertical eye openness multiplier from the current blink in [0..1] (1 = open, 0 = closed at mid-blink).</summary>
        public float BlinkMultiplier { get; private set; }

        /// <summary>True while the CRT is held completely dark before the opening's front reveal.</summary>
        public bool IsDarkHold => _isDarkHold;

        /// <summary>True while the CRT shows a plain readout (the self-test's lines) instead of eyes.</summary>
        public bool IsReadout => _isReadout;

        /// <summary>True when a blink cycle is currently in progress.</summary>
        public bool IsBlinking => _blinkElapsed < Math.Max(_activeBlinkDuration, 1e-4f);

        /// <summary>Clamped low-power amount in [0..1].</summary>
        public float LowPower { get; private set; }

        /// <summary>Clamped tether strain amount in [0..1] (narrows/squints 07's eyes under heavy load).</summary>
        public float Strain { get; private set; }

        /// <summary>Current overall CRT phosphor brightness multiplier.</summary>
        public float Brightness { get; private set; }

        /// <summary>Current vertical-hold UV jitter offset caused by low power.</summary>
        public float VerticalJitter { get; private set; }

        /// <summary>Current scanline phase in [0..1).</summary>
        public float ScanlinePhase { get; private set; }

        /// <summary>Current slow rolling refresh band phase in [0..1).</summary>
        public float RollingBandPhase { get; private set; }

        /// <summary>Accumulated animation time in seconds (drives symbol pulse/drift).</summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>True when the CRT face is in (or transitioning into) screen content mode.</summary>
        public bool IsScreenModeOpen => _isScreenModeOpen;

        /// <summary>Blend between 07's procedural eyes (<c>0</c>) and <c>_ContentTex</c> (<c>1</c>).</summary>
        public float ScreenContentMix { get; private set; }

        /// <summary>Vertical CRT deflection pinch in [0..1] (<c>0</c> = full height, <c>1</c> = pinched into a horizontal line).</summary>
        public float ScreenVerticalPinch { get; private set; }

        /// <summary>Horizontal phosphor line intensity in [0..1] during channel-switch transitions.</summary>
        public float ScreenLineGlow { get; private set; }

        /// <summary>Number of closing happy blinks (<c>^ ^</c>) triggered when returning from screen content mode to eyes.</summary>
        public int ClosingHappyBlinkCount { get; private set; }

        /// <summary>Total number of blinks started (periodic, wake, and channel-switch blinks).</summary>
        public int BlinkCount { get; private set; }

        /// <summary>Number of CRT channel-switch open transitions started.</summary>
        public int ScreenOpenCount { get; private set; }

        /// <summary>Number of CRT channel-switch close transitions started.</summary>
        public int ScreenCloseCount { get; private set; }

        /// <summary>
        /// Enters or exits screen content mode with the CRT channel-switch choreography (eyes blink, collapse to a
        /// horizontal phosphor line, and expand into content; closing reverses it and ends with a small happy blink).
        /// </summary>
        public void SetScreenMode(bool open)
        {
            if (_isScreenModeOpen == open)
            {
                return;
            }

            _isScreenModeOpen = open;
            _screenPhaseElapsed = 0.0f;

            if (open)
            {
                ScreenOpenCount++;
                _closingHappyTimer = 0.0f;
                if (_tuning.ScreenLeadInSeconds > 1e-4f)
                {
                    _screenPhase = ScreenPhase.OpeningLeadIn;
                }
                else
                {
                    Blink();
                    _screenPhase = ScreenPhase.OpeningCollapse;
                }

                ScreenContentMix = 0.0f;
                ScreenVerticalPinch = 0.0f;
                ScreenLineGlow = 0.0f;
            }
            else
            {
                ScreenCloseCount++;
                _screenPhase = ScreenPhase.ClosingCollapse;
                ScreenContentMix = 1.0f;
                ScreenVerticalPinch = 0.0f;
                ScreenLineGlow = 0.0f;
            }
        }

        /// <summary>
        /// Shows or ends a plain readout on the CRT: content at once, no channel-switch, and lit even while the opening
        /// holds the face dark, so the self-test can print its lines before the face wakes.
        /// </summary>
        public void SetReadout(bool on)
        {
            _isReadout = on;
            SnapToScreenMode(on);
            if (on)
            {
                BootProgress = 1.0f;
            }
            else if (_isDarkHold)
            {
                BootProgress = 0.0f;
            }

            ComposeEyesAndBrightness();
        }

        /// <summary>Immediately snaps the CRT screen content mode without playing the transition choreography.</summary>
        public void SnapToScreenMode(bool open)
        {
            _isScreenModeOpen = open;
            _screenPhaseElapsed = 0.0f;
            _closingHappyTimer = 0.0f;
            _screenPhase = open ? ScreenPhase.ScreenContent : ScreenPhase.Eyes;
            ScreenContentMix = open ? 1.0f : 0.0f;
            ScreenVerticalPinch = 0.0f;
            ScreenLineGlow = 0.0f;
        }

        /// <summary>Sets the target feeling that eye parameters and symbols ease toward.</summary>
        public void SetFeeling(FaceFeeling feeling)
        {
            Feeling = feeling;
            FaceFeelingSpec spec = _tuning.GetFeelingSpec(feeling);
            if (spec.Symbol != FaceSymbol.None)
            {
                ActiveSymbol = spec.Symbol;
            }

            if (feeling == FaceFeeling.Asleep)
            {
                _blinkElapsed = _tuning.BlinkDuration;
                BlinkMultiplier = 1.0f;
                _saccadeX = 0.0f;
                _saccadeY = 0.0f;
            }
        }

        /// <summary>Immediately snaps all eye parameters, symbols, and ghost trails to <paramref name="feeling"/>.</summary>
        public void SnapToFeeling(FaceFeeling feeling)
        {
            Feeling = feeling;
            FaceFeelingSpec spec = _tuning.GetFeelingSpec(feeling);
            _width = spec.Width;
            _height = spec.Height;
            _open = spec.Open;
            _tilt = spec.Tilt;
            _cut = spec.Cut;
            _squint = spec.Squint;
            _arc = spec.Arc;
            _ring = spec.Ring;
            _closed = spec.Closed;
            _lookX = spec.LookX;
            _lookY = spec.LookY;
            _asymmetry = spec.Asymmetric ? 1.0f : 0.0f;

            ActiveSymbol = spec.Symbol;
            SymbolAlpha = spec.Symbol != FaceSymbol.None ? 1.0f : 0.0f;

            if (feeling == FaceFeeling.Asleep)
            {
                _blinkElapsed = _tuning.BlinkDuration;
                BlinkMultiplier = 1.0f;
                _saccadeX = 0.0f;
                _saccadeY = 0.0f;
            }

            ComposeEyesAndBrightness();
            GhostLeftEye = LeftEye;
            GhostRightEye = RightEye;
        }

        /// <summary>Triggers an immediate blink unless 07 is currently <see cref="FaceFeeling.Asleep"/>.</summary>
        public void Blink()
        {
            if (Feeling == FaceFeeling.Asleep)
            {
                return;
            }

            _activeBlinkDuration = _tuning.BlinkDuration;
            _blinkElapsed = 0.0f;
            _nextBlinkCountdown = NextRange(_tuning.BlinkMinInterval, _tuning.BlinkMaxInterval);
            BlinkCount++;
        }

        /// <summary>Triggers a slow, expressive wake blink unless 07 is currently <see cref="FaceFeeling.Asleep"/>.</summary>
        public void SlowBlink(float durationScale = 2.0f)
        {
            if (Feeling == FaceFeeling.Asleep)
            {
                return;
            }

            _activeBlinkDuration = _tuning.BlinkDuration * Math.Max(1.0f, durationScale);
            _blinkElapsed = 0.0f;
            _nextBlinkCountdown = NextRange(_tuning.BlinkMinInterval, _tuning.BlinkMaxInterval);
            BlinkCount++;
        }

        /// <summary>
        /// Sets the external gaze target from a direction vector in the head's local coordinate frame
        /// (<c>+X</c> right, <c>+Y</c> up, <c>+Z</c> forward).
        /// </summary>
        public void LookAt(Vector3 directionInHeadSpace)
        {
            float sqrMag = directionInHeadSpace.x * directionInHeadSpace.x
                + directionInHeadSpace.y * directionInHeadSpace.y
                + directionInHeadSpace.z * directionInHeadSpace.z;
            if (sqrMag < 1e-6f)
            {
                ClearLookTarget();
                return;
            }

            float invMag = 1.0f / (float)Math.Sqrt(sqrMag);
            float nx = directionInHeadSpace.x * invMag;
            float ny = directionInHeadSpace.y * invMag;

            _targetExternalLookX = Clamp(nx * _tuning.LookMaxOffsetX, -_tuning.LookMaxOffsetX, _tuning.LookMaxOffsetX);
            // Positive Y in head space is UP, which maps to negative Y in top-down CRT canvas space
            _targetExternalLookY = Clamp(-ny * _tuning.LookMaxOffsetY, -_tuning.LookMaxOffsetY, _tuning.LookMaxOffsetY);
        }

        /// <summary>Clears any external gaze target so the eyes return to the feeling's base gaze offset.</summary>
        public void ClearLookTarget()
        {
            _targetExternalLookX = 0.0f;
            _targetExternalLookY = 0.0f;
        }

        /// <summary>Holds the CRT screen completely dark (0 boot progress, 0 brightness) until released for the reveal.</summary>
        public void SetDarkHold(bool hold)
        {
            _isDarkHold = hold;
            if (hold)
            {
                BootProgress = 0.0f;
            }

            ComposeEyesAndBrightness();
            GhostLeftEye = LeftEye;
            GhostRightEye = RightEye;
        }

        /// <summary>Restarts the CRT power-on boot sequence from <c>0</c>.</summary>
        public void Boot()
        {
            _isDarkHold = false;
            BootProgress = 0.0f;
            _nextBlinkCountdown = Math.Max(_nextBlinkCountdown, 2.5f);
            ComposeEyesAndBrightness();
            GhostLeftEye = LeftEye;
            GhostRightEye = RightEye;
        }

        /// <summary>Sets the normalized boot progress directly in [0..1] (useful for deterministic frame captures).</summary>
        public void SetBootProgress(float progress)
        {
            _isDarkHold = false;
            BootProgress = Clamp01(progress);
            ComposeEyesAndBrightness();
        }

        /// <summary>Sets the tether strain amount, clamped to [0..1], narrowing/squinting 07's eyes under load.</summary>
        public void SetStrain(float amount)
        {
            Strain = Clamp01(amount);
            ComposeEyesAndBrightness();
            ApplyStrainToEyes();
        }

        /// <summary>Sets the low-power state amount, clamped to [0..1].</summary>
        public void SetLowPower(float amount)
        {
            LowPower = Clamp01(amount);
            FaceFeelingSpec target = _tuning.GetFeelingSpec(Feeling);
            if (target.Symbol == FaceSymbol.None && LowPower >= 0.5f)
            {
                ActiveSymbol = FaceSymbol.Battery;
                SymbolAlpha = Math.Max(SymbolAlpha, LowPower);
            }
            else if (target.Symbol == FaceSymbol.None && LowPower < 0.01f)
            {
                SymbolAlpha = 0.0f;
                ActiveSymbol = FaceSymbol.None;
            }

            ComposeEyesAndBrightness();
        }

        /// <summary>Sets the active scan-focus state and normalized progress [0..1] for narrowed eyes and CRT progress ring.</summary>
        public void SetScanFocus(bool active, float progress01)
        {
            _scanFocusActive = active;
            _scanProgress = Clamp01(progress01);
        }

        /// <summary>Advances the face state by <paramref name="deltaTime"/> seconds.</summary>
        public void Step(float deltaTime)
        {
            if (deltaTime <= 0.0f)
            {
                return;
            }

            FaceEyeParams prevLeft = LeftEye;
            FaceEyeParams prevRight = RightEye;

            _elapsedTime += deltaTime;
            if (_isDarkHold && !_isReadout)
            {
                BootProgress = 0.0f;
            }
            else
            {
                BootProgress = Clamp01(BootProgress + deltaTime * _tuning.BootRate);
            }

            StepScreenMode(deltaTime);

            FaceFeelingSpec target = _tuning.GetFeelingSpec(Feeling);
            float k = 1.0f - (float)Math.Exp(-deltaTime * _tuning.EaseRate);
            float targetArc = _closingHappyTimer > 0.0f ? 1.0f : target.Arc;
            float targetScanFocus = _scanFocusActive && Feeling == FaceFeeling.Curious ? 1.0f : 0.0f;
            _scanFocusBlend += (targetScanFocus - _scanFocusBlend) * k;

            _width += (target.Width - _width) * k;
            _height += (target.Height - _height) * k;
            _open += (target.Open - _open) * k;
            _tilt += (target.Tilt - _tilt) * k;
            _cut += (target.Cut - _cut) * k;
            _squint += (target.Squint - _squint) * k;
            _arc += (targetArc - _arc) * k;
            _ring += (target.Ring - _ring) * k;
            _closed += (target.Closed - _closed) * k;
            _lookX += (target.LookX - _lookX) * k;
            _lookY += (target.LookY - _lookY) * k;

            float targetAsym = target.Asymmetric ? 1.0f : 0.0f;
            _asymmetry += (targetAsym - _asymmetry) * k;

            _externalLookX += (_targetExternalLookX - _externalLookX) * k;
            _externalLookY += (_targetExternalLookY - _externalLookY) * k;

            // Symbol fade (low power >= 0.5 also activates the battery warning glyph when no feeling symbol overrides it)
            FaceSymbol desiredSymbol = target.Symbol != FaceSymbol.None
                ? target.Symbol
                : (LowPower >= 0.5f ? FaceSymbol.Battery : FaceSymbol.None);
            float kSym = 1.0f - (float)Math.Exp(-deltaTime * _tuning.SymbolEaseRate);
            if (desiredSymbol != FaceSymbol.None)
            {
                ActiveSymbol = desiredSymbol;
                SymbolAlpha += (1.0f - SymbolAlpha) * kSym;
            }
            else
            {
                SymbolAlpha += (0.0f - SymbolAlpha) * kSym;
                if (SymbolAlpha < SymbolFadeCutoff)
                {
                    SymbolAlpha = 0.0f;
                    ActiveSymbol = FaceSymbol.None;
                }
            }

            StepBlink(deltaTime);
            StepSaccade(deltaTime);

            ScanlinePhase = Repeat01(ScanlinePhase + deltaTime * _tuning.ScanlineSpeed);
            RollingBandPhase = Repeat01(RollingBandPhase + deltaTime * _tuning.RollingBandSpeed);

            ComposeEyesAndBrightness();
            ApplyStrainToEyes();

            float kTrail = 1.0f - (float)Math.Exp(-deltaTime * _tuning.TrailEaseRate);
            GhostLeftEye = FaceEyeParams.Lerp(GhostLeftEye, prevLeft, kTrail);
            GhostRightEye = FaceEyeParams.Lerp(GhostRightEye, prevRight, kTrail);
        }

        private void StepScreenMode(float deltaTime)
        {
            if (_closingHappyTimer > 0.0f)
            {
                _closingHappyTimer = Math.Max(0.0f, _closingHappyTimer - deltaTime);
            }

            switch (_screenPhase)
            {
                case ScreenPhase.Eyes:
                    ScreenContentMix = 0.0f;
                    ScreenVerticalPinch = 0.0f;
                    ScreenLineGlow = 0.0f;
                    break;

                case ScreenPhase.OpeningLeadIn:
                {
                    _screenPhaseElapsed += deltaTime;
                    float leadInDur = _tuning.ScreenLeadInSeconds;
                    if (_screenPhaseElapsed < leadInDur)
                    {
                        ScreenContentMix = 0.0f;
                        ScreenVerticalPinch = 0.0f;
                        ScreenLineGlow = 0.0f;
                    }
                    else
                    {
                        float leftover = _screenPhaseElapsed - leadInDur;
                        Blink();
                        _screenPhase = ScreenPhase.OpeningCollapse;
                        _screenPhaseElapsed = 0.0f;
                        StepScreenMode(leftover);
                    }

                    break;
                }

                case ScreenPhase.OpeningCollapse:
                {
                    _screenPhaseElapsed += deltaTime;
                    float collapseDur = Math.Max(_tuning.ScreenCollapseSeconds, 1e-4f);
                    if (_screenPhaseElapsed < collapseDur)
                    {
                        float s = SmoothStep01(_screenPhaseElapsed / collapseDur);
                        ScreenContentMix = 0.0f;
                        ScreenVerticalPinch = s;
                        ScreenLineGlow = s;
                    }
                    else
                    {
                        float leftover = _screenPhaseElapsed - collapseDur;
                        _screenPhase = _tuning.ScreenLineHoldSeconds > 1e-4f
                            ? ScreenPhase.OpeningLineHold
                            : ScreenPhase.OpeningExpand;
                        _screenPhaseElapsed = 0.0f;
                        StepScreenMode(leftover);
                    }

                    break;
                }

                case ScreenPhase.OpeningLineHold:
                {
                    _screenPhaseElapsed += deltaTime;
                    float holdDur = _tuning.ScreenLineHoldSeconds;
                    if (_screenPhaseElapsed < holdDur)
                    {
                        ScreenContentMix = 0.0f;
                        ScreenVerticalPinch = 1.0f;
                        ScreenLineGlow = 1.0f;
                    }
                    else
                    {
                        float leftover = _screenPhaseElapsed - holdDur;
                        _screenPhase = ScreenPhase.OpeningExpand;
                        _screenPhaseElapsed = 0.0f;
                        StepScreenMode(leftover);
                    }

                    break;
                }

                case ScreenPhase.OpeningExpand:
                {
                    _screenPhaseElapsed += deltaTime;
                    float expandDur = Math.Max(_tuning.ScreenExpandSeconds, 1e-4f);
                    if (_screenPhaseElapsed < expandDur)
                    {
                        float u = Clamp01(_screenPhaseElapsed / expandDur);
                        float inv = 1.0f - u;
                        float easeOut = 1.0f - inv * inv * inv;
                        ScreenContentMix = 1.0f;
                        ScreenVerticalPinch = 1.0f - easeOut;
                        ScreenLineGlow = 1.0f - easeOut;
                    }
                    else
                    {
                        _screenPhase = ScreenPhase.ScreenContent;
                        _screenPhaseElapsed = 0.0f;
                        ScreenContentMix = 1.0f;
                        ScreenVerticalPinch = 0.0f;
                        ScreenLineGlow = 0.0f;
                    }

                    break;
                }

                case ScreenPhase.ScreenContent:
                    ScreenContentMix = 1.0f;
                    ScreenVerticalPinch = 0.0f;
                    ScreenLineGlow = 0.0f;
                    break;

                case ScreenPhase.ClosingCollapse:
                {
                    _screenPhaseElapsed += deltaTime;
                    float collapseDur = Math.Max(_tuning.ScreenCloseCollapseSeconds, 1e-4f);
                    if (_screenPhaseElapsed < collapseDur)
                    {
                        float s = SmoothStep01(_screenPhaseElapsed / collapseDur);
                        ScreenContentMix = 1.0f;
                        ScreenVerticalPinch = s;
                        ScreenLineGlow = s;
                    }
                    else
                    {
                        float leftover = _screenPhaseElapsed - collapseDur;
                        _screenPhase = ScreenPhase.ClosingExpand;
                        _screenPhaseElapsed = 0.0f;
                        _closingHappyTimer = _tuning.ScreenCloseHappyHoldSeconds;
                        _arc = 1.0f;
                        Blink();
                        ClosingHappyBlinkCount++;
                        StepScreenMode(leftover);
                    }

                    break;
                }

                case ScreenPhase.ClosingExpand:
                {
                    _screenPhaseElapsed += deltaTime;
                    float expandDur = Math.Max(_tuning.ScreenCloseExpandSeconds, 1e-4f);
                    if (_screenPhaseElapsed < expandDur)
                    {
                        float u = Clamp01(_screenPhaseElapsed / expandDur);
                        float easeOut = 1.0f - (1.0f - u) * (1.0f - u);
                        ScreenContentMix = 0.0f;
                        ScreenVerticalPinch = 1.0f - easeOut;
                        ScreenLineGlow = 1.0f - easeOut;
                    }
                    else
                    {
                        _screenPhase = ScreenPhase.Eyes;
                        _screenPhaseElapsed = 0.0f;
                        ScreenContentMix = 0.0f;
                        ScreenVerticalPinch = 0.0f;
                        ScreenLineGlow = 0.0f;
                    }

                    break;
                }
            }
        }

        private void StepBlink(float deltaTime)
        {
            if (Feeling == FaceFeeling.Asleep || _isDarkHold)
            {
                _blinkElapsed = Math.Max(_activeBlinkDuration, _tuning.BlinkDuration);
                BlinkMultiplier = 1.0f;
                return;
            }

            _nextBlinkCountdown -= deltaTime;
            if (_nextBlinkCountdown <= 0.0f)
            {
                _activeBlinkDuration = _tuning.BlinkDuration;
                _blinkElapsed = 0.0f;
                _nextBlinkCountdown = NextRange(_tuning.BlinkMinInterval, _tuning.BlinkMaxInterval);
                BlinkCount++;
            }

            float duration = Math.Max(_activeBlinkDuration, 1e-4f);
            if (_blinkElapsed < duration)
            {
                _blinkElapsed += deltaTime;
                float b = _blinkElapsed / duration;
                BlinkMultiplier = b < 1.0f ? Math.Abs(1.0f - b * 2.0f) : 1.0f;
            }
            else
            {
                BlinkMultiplier = 1.0f;
            }
        }

        private void ApplyStrainToEyes()
        {
            if (Strain <= 0.0f)
            {
                return;
            }

            float maxOpenByStrain = Lerp(1.0f, 0.80f, Strain) * BlinkMultiplier * BootEyeOpen;
            float leftOpen = _isDarkHold ? 0.0f : Math.Min(LeftEye.Open, maxOpenByStrain);
            float rightOpen = _isDarkHold ? 0.0f : Math.Min(RightEye.Open, maxOpenByStrain);
            float leftCut = Math.Max(LeftEye.Cut, Strain * 0.35f);
            float effectiveSquint = Clamp01(LeftEye.Squint + Strain * 0.55f);
            float effectiveTilt = _tilt - Strain * 0.38f;
            float rightCut = Lerp(leftCut, _tuning.AsymmetricRightCut, _asymmetry);
            float rightTilt = Lerp(effectiveTilt, _tuning.AsymmetricRightTilt, _asymmetry);

            LeftEye = new FaceEyeParams(
                LeftEye.CenterX,
                LeftEye.CenterY,
                LeftEye.Width,
                LeftEye.Height,
                leftOpen,
                effectiveTilt,
                leftCut,
                effectiveSquint,
                LeftEye.Arc,
                LeftEye.Ring,
                LeftEye.Closed,
                LeftEye.LookX,
                LeftEye.LookY,
                LeftEye.Scale);

            RightEye = new FaceEyeParams(
                RightEye.CenterX,
                RightEye.CenterY,
                RightEye.Width,
                RightEye.Height,
                rightOpen,
                rightTilt,
                rightCut,
                effectiveSquint,
                RightEye.Arc,
                RightEye.Ring,
                RightEye.Closed,
                RightEye.LookX,
                RightEye.LookY,
                RightEye.Scale);
        }

        private void StepSaccade(float deltaTime)
        {
            if (Feeling == FaceFeeling.Asleep)
            {
                _saccadeX = 0.0f;
                _saccadeY = 0.0f;
                return;
            }

            _nextSaccadeCountdown -= deltaTime;
            if (_nextSaccadeCountdown <= 0.0f)
            {
                _saccadeX = ((float)_random.NextDouble() - 0.5f) * 2.0f * _tuning.SaccadeRangeX;
                _saccadeY = ((float)_random.NextDouble() - 0.5f) * 2.0f * _tuning.SaccadeRangeY;
                _nextSaccadeCountdown = NextRange(_tuning.SaccadeMinInterval, _tuning.SaccadeMaxInterval);
            }
        }

        private void ComposeEyesAndBrightness()
        {
            float saccadeX = Feeling == FaceFeeling.Asleep ? 0.0f : _saccadeX * (1.0f - 0.7f * _scanFocusBlend);
            float saccadeY = Feeling == FaceFeeling.Asleep ? 0.0f : _saccadeY * (1.0f - 0.7f * _scanFocusBlend);
            float combinedLx = Clamp(_lookX + _externalLookX + saccadeX, -MaxCombinedLookX, MaxCombinedLookX);
            float combinedLy = Clamp(_lookY + _externalLookY + saccadeY, -MaxCombinedLookY, MaxCombinedLookY);
            float sep = _tuning.EyeSeparation;

            float maxOpen = Math.Min(Lerp(1.0f, 0.58f, LowPower), Lerp(1.0f, 0.82f, _scanFocusBlend));
            float effectiveOpen = _isDarkHold ? 0.0f : Math.Min(_open, maxOpen) * BlinkMultiplier * BootEyeOpen;
            float focusCut = Math.Max(_cut, Math.Max(LowPower * 0.25f, 0.28f * _scanFocusBlend));
            float focusSquint = Math.Max(_squint, 0.42f * _scanFocusBlend);
            float asym = _asymmetry * (1.0f - 0.45f * _scanFocusBlend);

            float leftScale = Lerp(1.0f, _tuning.AsymmetricLeftScale, asym);
            float rightScale = Lerp(1.0f, _tuning.AsymmetricRightScale, asym);
            float rightCut = Lerp(focusCut, _tuning.AsymmetricRightCut, asym);
            float rightTilt = Lerp(_tilt, _tuning.AsymmetricRightTilt, asym);

            LeftEye = new FaceEyeParams(
                0.5f - sep + combinedLx, 0.5f + combinedLy, _width, _height, effectiveOpen,
                _tilt, focusCut, focusSquint, _arc, _ring, _closed, combinedLx, combinedLy, leftScale);
            RightEye = new FaceEyeParams(
                0.5f + sep + combinedLx, 0.5f + combinedLy, _width, _height, effectiveOpen,
                rightTilt, rightCut, focusSquint, _arc, _ring, _closed, combinedLx, combinedLy, rightScale);

            if (_isDarkHold && !_isReadout)
            {
                Brightness = 0.0f;
                VerticalJitter = 0.0f;
                return;
            }

            float sleepDim = Feeling == FaceFeeling.Asleep ? _tuning.SleepBrightness : 1.0f;
            float lowPowerDim = Lerp(1.0f, _tuning.LowPowerMinBrightness, LowPower);
            float flicker = BaseFlickerFloor
                + BaseFlickerAmplitude * (float)(Math.Sin(_elapsedTime * FlickerFreqA) * Math.Cos(_elapsedTime * FlickerFreqB));
            Brightness = sleepDim * lowPowerDim * flicker;
            VerticalJitter = LowPower * _tuning.LowPowerJitterAmplitude
                * (float)Math.Sin(_elapsedTime * _tuning.LowPowerJitterFrequency * Math.PI * 2.0);
        }

        private float NextRange(float min, float max)
        {
            float hi = Math.Max(min, max);
            return min + (float)_random.NextDouble() * (hi - min);
        }

        private static float SmoothStep01(float value)
        {
            float t = Clamp01(value);
            return t * t * (3.0f - 2.0f * t);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        private static float Clamp01(float value)
        {
            return value < 0.0f ? 0.0f : (value > 1.0f ? 1.0f : value);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static float Repeat01(float value)
        {
            float floor = (float)Math.Floor(value);
            return value - floor;
        }
    }
}
