using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Pure C# conductor for 07's CRT face (0.5.7). Chooses <see cref="FaceFeeling"/> from game events and rover state
    /// using tuned priorities and hold times so the face never flickers, drives the eye look target with a slight head
    /// lead, ramps low-power CRT effects, and computes the rear vent strip's mood echo (<c>_EmissionStrength</c>).
    /// </summary>
    public sealed class FaceDirector
    {
        private enum BeatKind
        {
            None = 0,
            EarthGlance = 1,
            Happy = 2,
            Proud = 3,
            Startled = 4,
        }

        private readonly FaceState _state;
        private readonly FaceDirectorTuning _tuning;

        private FaceFeeling _currentFeeling;
        private int _currentPriority;
        private float _feelingHoldTimer;
        private bool _preemptHold;

        private bool _isCharging;
        private bool _isLowPower;
        private bool _isLowBattery;
        private bool _isScanning;
        private float _scanHoldTimer;

        private BeatKind _activeBeat;
        private float _beatTimer;

        private bool _earthInView;
        private float _earthGlanceCooldownTimer;

        private float _airborneSeconds;
        private float _nightIdleSeconds;
        private float _nightLookPhase;
        private bool _isNightIdleSlowLookActive;

        private bool _hasExplicitGaze;
        private Vector3 _explicitGazeHeadSpace;
        private float _lastLeadRatio;

        private float _lowPowerAmount;
        private float _rearVentEmissionStrength;

        private bool _hasBootedOnWake;
        private int _bootCount;
        private int _wakeBlinkCount;

        private bool _batteryPolled;
        private float _lastPolledCharge01 = 1f;
        private bool _lastPolledLowPower;
        private bool _lastPolledCharging;

        public FaceDirector(FaceState state, FaceDirectorTuning tuning)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));

            _currentFeeling = FaceFeeling.Calm;
            _currentPriority = _tuning.CalmPriority;
            _state.SetFeeling(FaceFeeling.Calm);
            _rearVentEmissionStrength = ComputeRearVentEmission();
        }

        public FaceDirector(FaceTuning faceTuning, FaceDirectorTuning directorTuning, int seed = 7)
            : this(new FaceState(faceTuning, seed), directorTuning)
        {
        }

        /// <summary>The underlying <see cref="FaceState"/> driven by this director.</summary>
        public FaceState State => _state;

        /// <summary>The authoritative <see cref="FaceDirectorTuning"/> asset.</summary>
        public FaceDirectorTuning Tuning => _tuning;

        /// <summary>07's currently selected <see cref="FaceFeeling"/>.</summary>
        public FaceFeeling CurrentFeeling => _currentFeeling;

        /// <summary>Priority of the currently active feeling or beat.</summary>
        public int CurrentPriority => _currentPriority;

        /// <summary>Current low-power CRT amount in [0..1] passed to <see cref="FaceState.SetLowPower"/>.</summary>
        public float LowPowerAmount => _lowPowerAmount;

        /// <summary>Current <c>_EmissionStrength</c> for <c>RearVentStrip</c>, echoing the screen's brightness and mood.</summary>
        public float RearVentEmissionStrength => _rearVentEmissionStrength;

        /// <summary>True when 07 has been stationary at night past <see cref="FaceDirectorTuning.NightIdleDelaySeconds"/>.</summary>
        public bool IsNightIdleSlowLookActive => _isNightIdleSlowLookActive;

        /// <summary>True once a wake event or fresh-game start has triggered the CRT boot sequence.</summary>
        public bool HasBootedOnWake => _hasBootedOnWake;

        /// <summary>Number of times the CRT boot sequence has been started.</summary>
        public int BootCount => _bootCount;

        /// <summary>Number of wake blinks triggered (e.g. when unplugging from a charger or on wake blink cue).</summary>
        public int WakeBlinkCount => _wakeBlinkCount;

        /// <summary>Ratio of eye look magnitude with head lead vs without head lead on the last gaze update.</summary>
        public float LastLeadRatio => _lastLeadRatio;

        /// <summary>Current head-space look vector computed from <see cref="RoverBodyLanguage"/> gaze.</summary>
        public Vector3 CurrentLookDirection => _explicitGazeHeadSpace;

        /// <summary>
        /// Scene start notification. Starts the CRT boot sequence only when <paramref name="isNewGame"/> is true,
        /// then settles into <see cref="FaceFeeling.Calm"/>.
        /// </summary>
        public void OnSceneStart(bool isNewGame)
        {
            if (!isNewGame)
            {
                return;
            }

            StartWakeBoot();
        }

        /// <summary>Handles <see cref="SessionStarted"/>; fresh games play the boot sequence, then <see cref="FaceFeeling.Calm"/>.</summary>
        public void OnSessionStarted(SessionStarted started)
        {
            if (started.FreshGame)
            {
                StartWakeBoot();
            }
        }

        /// <summary>Handles <see cref="RoverWakeBeatReached"/>.</summary>
        public void OnWakeBeatReached(RoverWakeBeatReached evt)
        {
            OnWakeBeat(evt.Beat);
        }

        /// <summary>
        /// Handles a <see cref="WakeBeat"/> from the opening sequence. Starts the CRT boot sequence on the first wake
        /// beat (or when a new wake begins at <see cref="WakeBeat.Crackle"/> / <see cref="WakeBeat.Stirred"/>) and
        /// triggers a blink on <see cref="WakeBeat.Blinked"/>.
        /// </summary>
        public void OnWakeBeat(WakeBeat beat)
        {
            if (!_hasBootedOnWake || beat == WakeBeat.Crackle || beat == WakeBeat.Stirred)
            {
                StartWakeBoot();
            }

            if (beat == WakeBeat.Blinked)
            {
                _state.Blink();
                _wakeBlinkCount++;
            }
        }

        /// <summary>Handles <see cref="ChargingChanged"/>.</summary>
        public void OnChargingChanged(ChargingChanged evt)
        {
            OnChargingChanged(evt.Charging);
        }

        /// <summary>
        /// Charging on: <see cref="FaceFeeling.Asleep"/> (<c>zzz</c>), overriding low battery and short beats.
        /// Charging off: a blink (<see cref="FaceState.Blink"/>), then <see cref="FaceFeeling.Calm"/>.
        /// </summary>
        public void OnChargingChanged(bool charging)
        {
            if (charging)
            {
                _isCharging = true;
                _isScanning = false;
                _scanHoldTimer = 0f;
                _activeBeat = BeatKind.None;
                _beatTimer = 0f;
                _preemptHold = true;
                EvaluateImmediate();
                return;
            }

            bool wasCharging = _isCharging;
            _isCharging = false;
            if (wasCharging)
            {
                _isLowPower = false;
                _isLowBattery = false;
                _activeBeat = BeatKind.None;
                _beatTimer = 0f;
                _isScanning = false;
                _scanHoldTimer = 0f;
                _currentFeeling = FaceFeeling.Calm;
                _currentPriority = _tuning.CalmPriority;
                _feelingHoldTimer = _tuning.MinFeelingHoldSeconds;
                _state.SetFeeling(FaceFeeling.Calm);
                _state.Blink();
                _wakeBlinkCount++;
                _rearVentEmissionStrength = ComputeRearVentEmission();
                return;
            }

            EvaluateImmediate();
        }

        /// <summary>Handles <see cref="BatteryWarned"/>.</summary>
        public void OnBatteryWarned(BatteryWarned evt)
        {
            OnBatteryWarned(evt.Warning);
        }

        /// <summary>
        /// Low battery warning: enters <see cref="FaceFeeling.Worried"/> (battery symbol), preempting any active short
        /// beat.
        /// </summary>
        public void OnBatteryWarned(BatteryWarning warning = BatteryWarning.Low)
        {
            _isLowBattery = true;
            _activeBeat = BeatKind.None;
            _beatTimer = 0f;
            _isScanning = false;
            _scanHoldTimer = 0f;
            _preemptHold = true;
            EvaluateImmediate();
        }

        /// <summary>Handles <see cref="LowPowerChanged"/>.</summary>
        public void OnLowPowerChanged(LowPowerChanged evt)
        {
            OnLowPowerChanged(evt.LowPower);
        }

        /// <summary>
        /// Empty battery or crawl mode: enters <see cref="FaceFeeling.Tired"/> while <see cref="LowPowerAmount"/>
        /// ramps in via <see cref="Step"/>.
        /// </summary>
        public void OnLowPowerChanged(bool isLowPower)
        {
            bool entering = isLowPower && !_isLowPower;
            _isLowPower = isLowPower;
            if (entering)
            {
                _activeBeat = BeatKind.None;
                _beatTimer = 0f;
                _isScanning = false;
                _scanHoldTimer = 0f;
                _preemptHold = true;
            }

            EvaluateImmediate();
        }

        /// <summary>
        /// Updates the continuous normalized battery charge [0..1] using tuned threshold hysteresis.
        /// </summary>
        public void SetBatteryCharge(float charge01)
        {
            float clamped = Mathf.Clamp01(charge01);
            if (clamped <= 0f)
            {
                bool enteringLowPower = !_isLowPower;
                _isLowPower = true;
                _isLowBattery = true;
                if (enteringLowPower)
                {
                    _activeBeat = BeatKind.None;
                    _beatTimer = 0f;
                    _isScanning = false;
                    _scanHoldTimer = 0f;
                    _preemptHold = true;
                }
            }
            else if (clamped < _tuning.LowBatteryThreshold)
            {
                bool enteringLowBattery = !_isLowBattery;
                _isLowBattery = true;
                if (enteringLowBattery)
                {
                    _activeBeat = BeatKind.None;
                    _beatTimer = 0f;
                    _isScanning = false;
                    _scanHoldTimer = 0f;
                    _preemptHold = true;
                }
            }
            else if (clamped >= _tuning.LowBatteryClearThreshold)
            {
                _isLowBattery = false;
                _isLowPower = false;
            }

            EvaluateImmediate();
        }

        /// <summary>
        /// Polls <see cref="IRoverBattery"/> state using edge detection so continuous battery changes drive the face
        /// without overwriting discrete <see cref="EventBus"/> events when the battery service is static in tests.
        /// </summary>
        public void PollBattery(float charge01, bool isLowPower, bool isCharging)
        {
            if (!_batteryPolled)
            {
                _batteryPolled = true;
                _lastPolledCharge01 = charge01;
                _lastPolledLowPower = isLowPower;
                _lastPolledCharging = isCharging;

                if (isCharging)
                {
                    OnChargingChanged(true);
                }

                if (isLowPower)
                {
                    OnLowPowerChanged(true);
                }

                if (charge01 < _tuning.LowBatteryThreshold)
                {
                    SetBatteryCharge(charge01);
                }

                return;
            }

            if (isCharging != _lastPolledCharging)
            {
                _lastPolledCharging = isCharging;
                OnChargingChanged(isCharging);
            }

            if (isLowPower != _lastPolledLowPower)
            {
                _lastPolledLowPower = isLowPower;
                OnLowPowerChanged(isLowPower);
            }

            if (Mathf.Abs(charge01 - _lastPolledCharge01) > 1e-4f)
            {
                bool crossedDown = charge01 < _tuning.LowBatteryThreshold && _lastPolledCharge01 >= _tuning.LowBatteryThreshold;
                bool crossedZero = charge01 <= 0f && _lastPolledCharge01 > 0f;
                bool crossedUp = charge01 >= _tuning.LowBatteryClearThreshold && _lastPolledCharge01 < _tuning.LowBatteryClearThreshold;
                _lastPolledCharge01 = charge01;

                if (crossedDown || crossedZero || crossedUp)
                {
                    SetBatteryCharge(charge01);
                }
            }
        }

        /// <summary>Handles <see cref="ScanStarted"/>.</summary>
        public void OnScanStarted(ScanStarted evt)
        {
            OnScanStarted();
        }

        /// <summary>Scanning started: enters <see cref="FaceFeeling.Curious"/> with minimum hold anti-flicker protection.</summary>
        public void OnScanStarted()
        {
            _isScanning = true;
            _scanHoldTimer = Mathf.Max(_scanHoldTimer, _tuning.ScanMinHoldSeconds);
            EvaluateImmediate();
        }

        /// <summary>Handles <see cref="ScanStopped"/>.</summary>
        public void OnScanStopped(ScanStopped evt)
        {
            OnScanStopped();
        }

        /// <summary>Scanning stopped: releases the active scan flag; <see cref="FaceDirectorTuning.ScanMinHoldSeconds"/> prevents flicker.</summary>
        public void OnScanStopped()
        {
            _isScanning = false;
            EvaluateImmediate();
        }

        /// <summary>Handles <see cref="FragmentScanned"/>.</summary>
        public void OnFragmentScanned(FragmentScanned evt)
        {
            OnFragmentScanned();
        }

        /// <summary>Fragment scanned: plays a short <see cref="FaceFeeling.Happy"/> beat, then returns to the ongoing state.</summary>
        public void OnFragmentScanned()
        {
            _isScanning = false;
            _scanHoldTimer = 0f;
            TriggerBeat(BeatKind.Happy, _tuning.FragmentHappyHoldSeconds);
        }

        /// <summary>Handles <see cref="BlueprintCompleted"/>.</summary>
        public void OnBlueprintCompleted(BlueprintCompleted evt)
        {
            OnBlueprintCompleted();
        }

        /// <summary>Blueprint completed: plays a longer <see cref="FaceFeeling.Happy"/> beat, then returns to the ongoing state.</summary>
        public void OnBlueprintCompleted()
        {
            _isScanning = false;
            _scanHoldTimer = 0f;
            TriggerBeat(BeatKind.Happy, _tuning.BlueprintHappyHoldSeconds);
        }

        /// <summary>Handles <see cref="UpgradePurchased"/>.</summary>
        public void OnUpgradePurchased(UpgradePurchased evt)
        {
            OnUpgradePurchased();
        }

        /// <summary>Upgrade purchased: plays a <see cref="FaceFeeling.Proud"/> beat.</summary>
        public void OnUpgradePurchased()
        {
            TriggerBeat(BeatKind.Proud, _tuning.ProudHoldSeconds);
        }

        /// <summary>Handles <see cref="PartFitted"/>.</summary>
        public void OnPartFitted(PartFitted evt)
        {
            OnPartFitted(restored: false);
        }

        /// <summary>Equipment fitted: plays a <see cref="FaceFeeling.Proud"/> beat when newly fitted (not restored on load).</summary>
        public void OnPartFitted(bool restored = false)
        {
            if (restored)
            {
                return;
            }

            TriggerBeat(BeatKind.Proud, _tuning.ProudHoldSeconds);
        }

        /// <summary>Handles <see cref="RoverLanded"/>.</summary>
        public void OnLanded(RoverLanded evt)
        {
            OnLanded(evt.ImpactSpeed);
        }

        /// <summary>Hard landing above <see cref="FaceDirectorTuning.HardLandingImpactThreshold"/>: brief <see cref="FaceFeeling.Startled"/>.</summary>
        public void OnLanded(float impactSpeed)
        {
            _airborneSeconds = 0f;
            if (impactSpeed >= _tuning.HardLandingImpactThreshold)
            {
                TriggerBeat(BeatKind.Startled, _tuning.StartledHoldSeconds);
            }
        }

        /// <summary>Handles <see cref="RoverRecovering"/>.</summary>
        public void OnRecovering(RoverRecovering evt)
        {
            OnRecovering();
        }

        /// <summary>Rover recovering from a tumble or fall: brief <see cref="FaceFeeling.Startled"/>.</summary>
        public void OnRecovering()
        {
            TriggerBeat(BeatKind.Startled, _tuning.StartledHoldSeconds);
        }

        /// <summary>Handles <see cref="FeelBeat"/> for scan and install cues.</summary>
        public void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Reward)
            {
                TriggerBeat(BeatKind.Proud, _tuning.ProudHoldSeconds);
            }
            else if (beat.Kind == FeelKind.Scan && beat.Phase == FeelPhase.Anticipate)
            {
                OnScanStarted();
            }
        }

        /// <summary>Updates whether Earth is within 07's forward view cone.</summary>
        public void UpdateEarthView(Vector3 headForward, Vector3 earthDirection)
        {
            if (headForward.sqrMagnitude < 1e-6f || earthDirection.sqrMagnitude < 1e-6f)
            {
                SetEarthInView(false);
                return;
            }

            float dot = Vector3.Dot(headForward.normalized, earthDirection.normalized);
            SetEarthInView(dot >= _tuning.EarthViewMinDot);
        }

        /// <summary>Directly sets whether Earth is in view; triggers a gentle <see cref="FaceFeeling.Curious"/> glance when off cooldown.</summary>
        public void SetEarthInView(bool inView)
        {
            _earthInView = inView;
            if (_earthInView && _earthGlanceCooldownTimer <= 0f && CanStartEarthGlance())
            {
                _activeBeat = BeatKind.EarthGlance;
                _beatTimer = _tuning.EarthGlanceHoldSeconds;
                _earthGlanceCooldownTimer = _tuning.EarthGlanceHoldSeconds + _tuning.EarthGlanceCooldownSeconds;
                EvaluateImmediate();
            }
        }

        /// <summary>
        /// Steps motion and environment sensors: detects airborne falls (<see cref="FaceFeeling.Startled"/>) and
        /// accumulates stationary time at night for slow <see cref="FaceFeeling.Calm"/> looks.
        /// </summary>
        public void StepMotion(bool isGrounded, float verticalSpeed, float planarSpeed, bool inSunlight, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (!isGrounded)
            {
                _airborneSeconds += deltaTime;
                if (_airborneSeconds >= _tuning.FallMinAirborneSeconds &&
                    verticalSpeed <= -_tuning.FallVerticalSpeedThreshold)
                {
                    TriggerBeat(BeatKind.Startled, _tuning.StartledHoldSeconds);
                }
            }
            else
            {
                _airborneSeconds = 0f;
            }

            bool stationaryAtNight = isGrounded &&
                planarSpeed <= _tuning.IdleSpeedThreshold &&
                !inSunlight &&
                !_isCharging &&
                !_isScanning &&
                _activeBeat == BeatKind.None;

            if (stationaryAtNight)
            {
                _nightIdleSeconds += deltaTime;
            }
            else
            {
                _nightIdleSeconds = 0f;
            }
        }

        /// <summary>
        /// Updates the eye look target from <see cref="RoverBodyLanguage"/>'s chosen gaze angles and current slewed
        /// head angles. Because the un-slewed angular delta is included along with <see cref="FaceDirectorTuning.EyeLeadFactor"/>,
        /// the eyes lead the head slightly whenever a look begins or changes.
        /// </summary>
        public Vector3 UpdateGaze(
            bool hasExplicitGaze,
            float targetYawDeg,
            float targetPitchDeg,
            float currentYawDeg,
            float currentPitchDeg)
        {
            if (!hasExplicitGaze)
            {
                _hasExplicitGaze = false;
                _explicitGazeHeadSpace = Vector3.forward;
                _lastLeadRatio = 1f;
                return _explicitGazeHeadSpace;
            }

            _hasExplicitGaze = true;
            float scale = 1f / Mathf.Max(1f, _tuning.LookDirectionFullScaleDegrees);

            float deltaYaw = targetYawDeg - currentYawDeg;
            float deltaPitch = targetPitchDeg - currentPitchDeg;

            float baseYaw = targetYawDeg * scale;
            float basePitch = targetPitchDeg * scale;

            float leadYaw = (targetYawDeg + deltaYaw * (1f + _tuning.EyeLeadFactor)) * scale;
            float leadPitch = (targetPitchDeg + deltaPitch * (1f + _tuning.EyeLeadFactor)) * scale;

            float baseMag = Mathf.Sqrt(baseYaw * baseYaw + basePitch * basePitch);
            float leadMag = Mathf.Sqrt(leadYaw * leadYaw + leadPitch * leadPitch);
            _lastLeadRatio = baseMag > 1e-4f ? leadMag / baseMag : (leadMag > 1e-4f ? 1f + _tuning.EyeLeadFactor : 1f);

            _explicitGazeHeadSpace = new Vector3(
                Mathf.Clamp(leadYaw, -1f, 1f),
                Mathf.Clamp(leadPitch, -1f, 1f),
                1f);

            return _explicitGazeHeadSpace;
        }

        /// <summary>Clears any explicit gaze target from <see cref="RoverBodyLanguage"/>.</summary>
        public void ClearGaze()
        {
            _hasExplicitGaze = false;
            _explicitGazeHeadSpace = Vector3.forward;
            _lastLeadRatio = 1f;
        }

        /// <summary>
        /// Advances all hold timers, low-power ramp, night-idle slow looks, <see cref="FaceState"/>, and the rear vent
        /// strip emission strength by <paramref name="deltaTime"/> seconds.
        /// </summary>
        public void Step(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                return;
            }

            if (deltaTime > 0f)
            {
                if (_feelingHoldTimer > 0f)
                {
                    _feelingHoldTimer = Mathf.Max(0f, _feelingHoldTimer - deltaTime);
                }

                if (_scanHoldTimer > 0f)
                {
                    _scanHoldTimer = Mathf.Max(0f, _scanHoldTimer - deltaTime);
                }

                if (_beatTimer > 0f)
                {
                    _beatTimer = Mathf.Max(0f, _beatTimer - deltaTime);
                    if (_beatTimer <= 0f)
                    {
                        _activeBeat = BeatKind.None;
                    }
                }

                if (_earthGlanceCooldownTimer > 0f)
                {
                    _earthGlanceCooldownTimer = Mathf.Max(0f, _earthGlanceCooldownTimer - deltaTime);
                }
            }

            if (_earthInView && _earthGlanceCooldownTimer <= 0f && CanStartEarthGlance())
            {
                _activeBeat = BeatKind.EarthGlance;
                _beatTimer = _tuning.EarthGlanceHoldSeconds;
                _earthGlanceCooldownTimer = _tuning.EarthGlanceHoldSeconds + _tuning.EarthGlanceCooldownSeconds;
            }

            EvaluateImmediate();

            bool wantLowPower = _isLowPower && !_isCharging;
            float rampSeconds = wantLowPower
                ? Mathf.Max(0.01f, _tuning.LowPowerRampSeconds)
                : Mathf.Max(0.01f, _tuning.LowPowerRecoverSeconds);
            if (deltaTime > 0f)
            {
                _lowPowerAmount = Mathf.MoveTowards(_lowPowerAmount, wantLowPower ? 1f : 0f, deltaTime / rampSeconds);
            }

            _state.SetLowPower(_lowPowerAmount);

            ApplyLookTarget(deltaTime);

            _state.Step(deltaTime);
            _rearVentEmissionStrength = ComputeRearVentEmission();
        }

        private void StartWakeBoot()
        {
            bool alreadyBootingAtStart = _hasBootedOnWake && _state.IsBooting && _state.BootProgress < 0.05f;
            _hasBootedOnWake = true;
            if (!alreadyBootingAtStart)
            {
                _bootCount++;
            }

            _isLowBattery = false;
            _isLowPower = false;
            _activeBeat = BeatKind.None;
            _beatTimer = 0f;
            _isScanning = false;
            _scanHoldTimer = 0f;
            _currentFeeling = FaceFeeling.Calm;
            _currentPriority = _tuning.CalmPriority;
            _feelingHoldTimer = _tuning.MinFeelingHoldSeconds;
            _state.SetFeeling(FaceFeeling.Calm);
            _state.Boot();
            _rearVentEmissionStrength = ComputeRearVentEmission();
        }

        private void TriggerBeat(BeatKind kind, float holdSeconds)
        {
            int newPriority = GetBeatPriority(kind);
            int currentBeatPriority = GetBeatPriority(_activeBeat);

            if (_activeBeat == BeatKind.None || newPriority > currentBeatPriority)
            {
                _activeBeat = kind;
                _beatTimer = holdSeconds;
                _preemptHold = true;
            }
            else if (newPriority == currentBeatPriority)
            {
                _activeBeat = kind;
                _beatTimer = Mathf.Max(_beatTimer, holdSeconds);
            }

            EvaluateImmediate();
        }

        private bool CanStartEarthGlance()
        {
            if (_isCharging || _isLowPower || _isLowBattery || _isScanning || _scanHoldTimer > 0f || _activeBeat != BeatKind.None)
            {
                return false;
            }

            return _tuning.EarthGlancePriority > _tuning.CalmPriority;
        }

        private void EvaluateImmediate()
        {
            GetOngoingFeeling(out FaceFeeling desiredFeeling, out int desiredPriority);

            if ((_isScanning || _scanHoldTimer > 0f) && _tuning.ScanningPriority >= desiredPriority)
            {
                desiredFeeling = FaceFeeling.Curious;
                desiredPriority = _tuning.ScanningPriority;
            }

            if (_activeBeat != BeatKind.None && _beatTimer > 0f)
            {
                FaceFeeling beatFeeling = GetBeatFeeling(_activeBeat);
                int beatPriority = GetBeatPriority(_activeBeat);
                if (beatPriority >= desiredPriority)
                {
                    desiredFeeling = beatFeeling;
                    desiredPriority = beatPriority;
                }
            }

            if (desiredFeeling != _currentFeeling)
            {
                if (desiredPriority > _currentPriority || _preemptHold || _feelingHoldTimer <= 0f)
                {
                    _currentFeeling = desiredFeeling;
                    _currentPriority = desiredPriority;
                    _feelingHoldTimer = _tuning.MinFeelingHoldSeconds;
                    _preemptHold = false;
                    _state.SetFeeling(_currentFeeling);
                }
            }
            else
            {
                _currentPriority = desiredPriority;
                _preemptHold = false;
                _state.SetFeeling(_currentFeeling);
            }

            _rearVentEmissionStrength = ComputeRearVentEmission();
        }

        private void GetOngoingFeeling(out FaceFeeling feeling, out int priority)
        {
            if (_isCharging)
            {
                feeling = FaceFeeling.Asleep;
                priority = _tuning.ChargingPriority;
                return;
            }

            if (_isLowPower)
            {
                feeling = FaceFeeling.Tired;
                priority = _tuning.LowPowerPriority;
                return;
            }

            if (_isLowBattery)
            {
                feeling = FaceFeeling.Worried;
                priority = _tuning.LowBatteryPriority;
                return;
            }

            feeling = FaceFeeling.Calm;
            priority = _tuning.CalmPriority;
        }

        private FaceFeeling GetBeatFeeling(BeatKind kind)
        {
            switch (kind)
            {
                case BeatKind.EarthGlance:
                    return FaceFeeling.Curious;
                case BeatKind.Happy:
                    return FaceFeeling.Happy;
                case BeatKind.Proud:
                    return FaceFeeling.Proud;
                case BeatKind.Startled:
                    return FaceFeeling.Startled;
                default:
                    return FaceFeeling.Calm;
            }
        }

        private int GetBeatPriority(BeatKind kind)
        {
            switch (kind)
            {
                case BeatKind.EarthGlance:
                    return _tuning.EarthGlancePriority;
                case BeatKind.Happy:
                    return _tuning.HappyPriority;
                case BeatKind.Proud:
                    return _tuning.ProudPriority;
                case BeatKind.Startled:
                    return _tuning.StartledPriority;
                default:
                    return 0;
            }
        }

        private void ApplyLookTarget(float deltaTime)
        {
            _isNightIdleSlowLookActive = _nightIdleSeconds >= _tuning.NightIdleDelaySeconds &&
                _currentFeeling == FaceFeeling.Calm &&
                !_isCharging;

            if (_hasExplicitGaze)
            {
                _state.LookAt(_explicitGazeHeadSpace);
                return;
            }

            if (_isNightIdleSlowLookActive)
            {
                if (deltaTime > 0f)
                {
                    _nightLookPhase = Mathf.Repeat(
                        _nightLookPhase + deltaTime / Mathf.Max(0.1f, _tuning.NightSlowLookPeriodSeconds),
                        1f);
                }

                float angle = _nightLookPhase * Mathf.PI * 2f;
                Vector3 slowLook = new Vector3(
                    Mathf.Sin(angle) * _tuning.NightSlowLookAmplitudeX,
                    Mathf.Cos(angle * 0.5f) * _tuning.NightSlowLookAmplitudeY,
                    1f);
                _state.LookAt(slowLook);
                return;
            }

            _state.ClearLookTarget();
        }

        private float ComputeRearVentEmission()
        {
            float screenLuminance = Mathf.Clamp01(_state.Brightness * Mathf.Clamp01(_state.BootProgress));
            float emission = Mathf.Lerp(_tuning.RearVentMinEmission, _tuning.RearVentBaseEmission, screenLuminance);

            if (_currentFeeling == FaceFeeling.Happy || _currentFeeling == FaceFeeling.Proud)
            {
                emission *= _tuning.RearVentHappyBoost;
            }
            else if (_currentFeeling == FaceFeeling.Worried)
            {
                float pulse = Mathf.Sin(_state.ElapsedTime * _tuning.RearVentWorriedPulseHz * Mathf.PI * 2f)
                    * _tuning.RearVentWorriedPulseAmplitude;
                emission = Mathf.Max(_tuning.RearVentMinEmission, emission + pulse);
            }

            return emission;
        }
    }
}
