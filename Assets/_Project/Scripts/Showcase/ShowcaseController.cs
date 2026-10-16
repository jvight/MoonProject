using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.World;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Top-level runtime bootstrap and input router for <c>Showcase.unity</c>. Wires <see cref="GameContext"/> with
    /// <see cref="ShowcaseInMemorySaveService"/> and <see cref="ShowcaseFocusState"/>, initializes
    /// <see cref="WorldSystem"/> and <see cref="SkyCycleSystem"/>, and drives the 15 look-dev exhibits, camera rig,
    /// slow camera tour (O key), build-stage sequences (J key), auto-demo loop (K key), time-of-day controls, and
    /// Vietnamese help panel.
    /// </summary>
    public sealed class ShowcaseController : MonoBehaviour
    {
        private const string ActionMapName = "Showcase";

        private static readonly ShowcaseSkyPresetId[] TourSkyPresets =
        {
            ShowcaseSkyPresetId.Sunset,
            ShowcaseSkyPresetId.EarthlitNight,
            ShowcaseSkyPresetId.EclipseHold,
        };

        [Tooltip("Showcase tuning asset (Assets/_Project/Data/Tuning/ShowcaseTuning.asset).")]
        [SerializeField] private ShowcaseTuning _tuning;

        [Tooltip("Showcase input actions asset (Assets/_Project/Data/Input/Showcase.inputactions).")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Core game input actions asset (Assets/_Project/Data/Input/Controls.inputactions) for GameContext.")]
        [SerializeField] private InputActionAsset _gameInputActions;

        [Tooltip("WorldSystem in Showcase.unity.")]
        [SerializeField] private WorldSystem _worldSystem;

        [Tooltip("SkyCycleSystem in Showcase.unity.")]
        [SerializeField] private SkyCycleSystem _skyCycleSystem;

        [Tooltip("Camera rig driving the Showcase camera.")]
        [SerializeField] private ShowcaseCameraRig _cameraRig;

        [Tooltip("Vietnamese UI Toolkit help overlay.")]
        [SerializeField] private ShowcaseHelpOverlay _helpOverlay;

        [Tooltip("The 15 exhibit rigs in order (1..15).")]
        [SerializeField] private ShowcaseExhibitRig[] _exhibits = Array.Empty<ShowcaseExhibitRig>();

        private InputReader _inputReader;
        private GameContext _context;
        private ShowcaseInMemorySaveService _saveService;
        private ShowcaseFocusState _focusState;
        private ShowcasePhaseStepper _phaseStepper;
        private InputActionMap _actionMap;

        private InputAction _moveAction;
        private InputAction _verticalAction;
        private InputAction _fastAction;
        private InputAction _lookDeltaAction;
        private InputAction _lookRateAction;
        private InputAction _zoomAction;
        private InputAction _nextExhibitAction;
        private InputAction[] _exhibitNumberActions;
        private InputAction _nextPhaseAction;
        private InputAction _scrubAction;
        private InputAction _fastTimeAction;
        private InputAction _toggleHelpAction;
        private InputAction _faceFeelingAction;
        private InputAction _faceBlinkAction;
        private InputAction _faceBootAction;
        private InputAction _cameraTourAction;
        private InputAction _stageSequenceAction;
        private InputAction _demoModeAction;
        private InputAction _action1;
        private InputAction _action2;
        private InputAction _action3;
        private InputAction _action4;
        private InputAction _action5;
        private InputAction _action6;

        private bool _isTourActive;
        private float _tourVisitTimer;
        private int _tourStepCount;
        private bool _isDemoMode;
        private bool _initialized;

        public GameContext Context => _context;
        public ShowcaseTuning Tuning => _tuning;
        public WorldSystem World => _worldSystem;
        public SkyCycleSystem SkyCycle => _skyCycleSystem;
        public ShowcaseCameraRig CameraRig => _cameraRig;
        public ShowcaseHelpOverlay HelpOverlay => _helpOverlay;
        public ShowcaseExhibitRig[] Exhibits => _exhibits;
        public ShowcasePhaseStepper PhaseStepper => _phaseStepper;
        public bool IsTourActive => _isTourActive;
        public bool IsDemoMode => _isDemoMode;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (_tuning == null || _gameInputActions == null || _worldSystem == null ||
                _skyCycleSystem == null || _cameraRig == null)
            {
                Debug.LogError($"{nameof(ShowcaseController)}: Missing required scene references.", this);
                enabled = false;
                return;
            }

            _inputReader = new InputReader(_gameInputActions);
            _context = new GameContext(new EventBus(), _inputReader);
            _saveService = new ShowcaseInMemorySaveService();
            _focusState = new ShowcaseFocusState(Vector3.zero);
            _context.Register<ISaveService>(_saveService);
            _context.Register<IRoverState>(_focusState);

            _worldSystem.Initialize(_context);
            _skyCycleSystem.Initialize(_context);

            SnapExhibitsToGround();
            for (int i = 0; i < _exhibits.Length; i++)
            {
                if (_exhibits[i] != null)
                {
                    _exhibits[i].Initialize(_tuning);
                }
            }

            _phaseStepper = new ShowcasePhaseStepper(
                _tuning.SkyPresets,
                7200f,
                _tuning.ScrubSecondsPerSecond,
                _tuning.FastTimeScale,
                initialPresetIndex: 3);

            _cameraRig.Initialize(_tuning, _exhibits, _worldSystem.Surface);
            UpdateFocusStateFromCamera();

            ShowcaseSkyPreset initialPreset = _phaseStepper.CurrentPreset;
            _skyCycleSystem.SetClockTime(initialPreset.TimeSeconds, initialPreset.HoldEclipse, snapLighting: true);

            if (_helpOverlay != null)
            {
                _helpOverlay.Initialize();
            }

            BindInputActions();
            _initialized = true;

            StepExhibits(0.05f);
            RefreshUi();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            _actionMap?.Enable();
        }

        private void OnDisable()
        {
            _actionMap?.Disable();
        }

        private void OnDestroy()
        {
            _actionMap?.Disable();
            _inputReader?.Dispose();
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float dt = Time.deltaTime;
            HandleDiscreteInputs();
            if (_isTourActive)
            {
                StepCameraTour(dt);
            }
            HandleTimeControls(dt);
            HandleCameraInput(dt);
            UpdateFocusStateFromCamera();
            StepExhibits(dt);
            RefreshUi();
        }

        public ShowcaseExhibitRig FindExhibitRig(ShowcaseExhibitId id)
        {
            Initialize();
            if (_exhibits == null)
            {
                return null;
            }

            for (int i = 0; i < _exhibits.Length; i++)
            {
                if (_exhibits[i] != null && _exhibits[i].ExhibitId == id)
                {
                    return _exhibits[i];
                }
            }

            return null;
        }

        public int FindExhibitIndex(ShowcaseExhibitId id)
        {
            Initialize();
            if (_exhibits == null)
            {
                return -1;
            }

            for (int i = 0; i < _exhibits.Length; i++)
            {
                if (_exhibits[i] != null && _exhibits[i].ExhibitId == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public void SelectExhibit(
            int index,
            bool immediate = false,
            float? overrideYaw = null,
            float? overridePitch = null,
            float? overrideDistance = null,
            Vector3? overridePivotOffset = null)
        {
            Initialize();
            _isTourActive = false;
            if (immediate)
            {
                _cameraRig.SelectExhibitImmediate(index, overrideYaw, overridePitch, overrideDistance, overridePivotOffset);
            }
            else
            {
                _cameraRig.FocusExhibit(index);
            }

            UpdateFocusStateFromCamera();
            RefreshUi();
        }

        public void SelectExhibitById(
            ShowcaseExhibitId id,
            bool immediate = false,
            float? overrideYaw = null,
            float? overridePitch = null,
            float? overrideDistance = null,
            Vector3? overridePivotOffset = null)
        {
            int idx = FindExhibitIndex(id);
            if (idx >= 0)
            {
                SelectExhibit(idx, immediate, overrideYaw, overridePitch, overrideDistance, overridePivotOffset);
            }
        }

        public bool ToggleCameraTour()
        {
            if (_isTourActive)
            {
                StopCameraTour();
            }
            else
            {
                StartCameraTour();
            }

            return _isTourActive;
        }

        public void StartCameraTour()
        {
            Initialize();
            _isTourActive = true;
            _tourVisitTimer = 0f;
            _tourStepCount = 0;

            SetDemoMode(true);
            SelectSkyPreset(TourSkyPresets[0], snapLighting: true);

            float halfSweep = 0.5f * _tuning.TourOrbitSweepDegrees;
            _cameraRig.FocusExhibit(_cameraRig.FocusedExhibitIndex, useTourTransition: true, yawOffsetDegrees: -halfSweep);
            UpdateFocusStateFromCamera();
            RefreshUi();
        }

        public void StopCameraTour()
        {
            if (!_isTourActive)
            {
                return;
            }

            _isTourActive = false;
            RefreshUi();
        }

        public bool ToggleDemoMode()
        {
            SetDemoMode(!_isDemoMode);
            return _isDemoMode;
        }

        public void SetDemoMode(bool active)
        {
            Initialize();
            _isDemoMode = active;
            if (_exhibits != null)
            {
                for (int i = 0; i < _exhibits.Length; i++)
                {
                    if (_exhibits[i] != null)
                    {
                        _exhibits[i].SetDemoMode(active);
                    }
                }
            }

            RefreshUi();
        }

        public void SelectSkyPreset(ShowcaseSkyPresetId presetId, bool snapLighting = true)
        {
            Initialize();
            ShowcaseSkyPreset preset = _phaseStepper.SelectPreset(presetId);
            _skyCycleSystem.SetClockTime(preset.TimeSeconds, preset.HoldEclipse, snapLighting);
            RefreshUi();
        }

        public void SetClockTime(float timeSeconds, bool holdEclipse = false, bool snapLighting = true)
        {
            Initialize();
            _skyCycleSystem.SetClockTime(timeSeconds, holdEclipse, snapLighting);
            RefreshUi();
        }

        public void StepSimulation(float totalSeconds, float stepSeconds = 0.05f)
        {
            Initialize();
            float step = Mathf.Max(0.01f, stepSeconds);
            float remaining = Mathf.Max(0f, totalSeconds);
            while (remaining > 1e-4f)
            {
                float dt = Mathf.Min(step, remaining);
                if (_isTourActive)
                {
                    StepCameraTour(dt);
                }
                StepExhibits(dt);
                _cameraRig.StepCamera(dt, Vector2.zero, 0f, false, Vector2.zero, Vector2.zero, 0f);
                UpdateFocusStateFromCamera();
                remaining -= dt;
            }

            RefreshUi();
        }

        private void StepCameraTour(float dt)
        {
            if (!_isTourActive || _exhibits == null || _exhibits.Length == 0 || dt <= 0f)
            {
                return;
            }

            float holdSeconds = Mathf.Max(1.0f, _tuning.TourExhibitHoldSeconds);
            float halfSweep = 0.5f * _tuning.TourOrbitSweepDegrees;

            _tourVisitTimer += dt;
            if (_tourVisitTimer >= holdSeconds)
            {
                _tourVisitTimer -= holdSeconds;
                _tourStepCount++;

                ShowcaseSkyPresetId nextPreset = TourSkyPresets[_tourStepCount % TourSkyPresets.Length];
                SelectSkyPreset(nextPreset, snapLighting: true);

                int nextIndex = (_cameraRig.FocusedExhibitIndex + 1) % _exhibits.Length;
                _cameraRig.FocusExhibit(nextIndex, useTourTransition: true, yawOffsetDegrees: -halfSweep);
            }
            else
            {
                float progress01 = ShowcaseEasing.SmoothStep01(_tourVisitTimer / holdSeconds);
                float yawOffset = Mathf.Lerp(-halfSweep, halfSweep, progress01);
                _cameraRig.SetTourOrbitYawOffset(yawOffset);
            }
        }

        private void SnapExhibitsToGround()
        {
            ITerrainQuery surface = _worldSystem.Surface;
            if (surface == null || _exhibits == null)
            {
                return;
            }

            for (int i = 0; i < _exhibits.Length; i++)
            {
                ShowcaseExhibitRig rig = _exhibits[i];
                if (rig == null)
                {
                    continue;
                }

                ShowcaseExhibitDefinition def = _tuning.FindExhibit(rig.ExhibitId);
                Vector3 pos = def != null ? def.PositionOffset : rig.transform.position;
                pos.y = surface.SampleHeight(pos.x, pos.z);
                float yaw = def != null ? def.YawDegrees : rig.transform.eulerAngles.y;
                rig.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            }
        }

        private void BindInputActions()
        {
            if (_inputActions == null)
            {
                return;
            }

            _actionMap = _inputActions.FindActionMap(ActionMapName, false);
            if (_actionMap == null)
            {
                Debug.LogError($"{nameof(ShowcaseController)}: Action map '{ActionMapName}' not found.", this);
                return;
            }

            _moveAction = _actionMap.FindAction("Move", false);
            _verticalAction = _actionMap.FindAction("Vertical", false);
            _fastAction = _actionMap.FindAction("Fast", false);
            _lookDeltaAction = _actionMap.FindAction("LookDelta", false);
            _lookRateAction = _actionMap.FindAction("LookRate", false);
            _zoomAction = _actionMap.FindAction("Zoom", false);
            _nextExhibitAction = _actionMap.FindAction("NextExhibit", false);
            _exhibitNumberActions = new[]
            {
                _actionMap.FindAction("Exhibit1", false),
                _actionMap.FindAction("Exhibit2", false),
                _actionMap.FindAction("Exhibit3", false),
                _actionMap.FindAction("Exhibit4", false),
                _actionMap.FindAction("Exhibit5", false),
                _actionMap.FindAction("Exhibit6", false),
            };
            _nextPhaseAction = _actionMap.FindAction("NextPhase", false);
            _scrubAction = _actionMap.FindAction("Scrub", false);
            _fastTimeAction = _actionMap.FindAction("FastTime", false);
            _toggleHelpAction = _actionMap.FindAction("ToggleHelp", false);
            _faceFeelingAction = _actionMap.FindAction("FaceFeeling", false);
            _faceBlinkAction = _actionMap.FindAction("FaceBlink", false);
            _faceBootAction = _actionMap.FindAction("FaceBoot", false);
            _cameraTourAction = _actionMap.FindAction("CameraTour", false);
            _stageSequenceAction = _actionMap.FindAction("StageSequence", false);
            _demoModeAction = _actionMap.FindAction("DemoMode", false);
            _action1 = _actionMap.FindAction("Action1", false);
            _action2 = _actionMap.FindAction("Action2", false);
            _action3 = _actionMap.FindAction("Action3", false);
            _action4 = _actionMap.FindAction("Action4", false);
            _action5 = _actionMap.FindAction("Action5", false);
            _action6 = _actionMap.FindAction("Action6", false);

            _actionMap.Enable();
        }

        private void HandleDiscreteInputs()
        {
            if (_actionMap == null)
            {
                return;
            }

            if (WasPressed(_cameraTourAction))
            {
                ToggleCameraTour();
            }

            if (WasPressed(_demoModeAction))
            {
                ToggleDemoMode();
            }

            if (WasPressed(_nextExhibitAction))
            {
                _isTourActive = false;
                _cameraRig.FocusNextExhibit();
            }

            if (_exhibitNumberActions != null)
            {
                for (int i = 0; i < _exhibitNumberActions.Length; i++)
                {
                    if (WasPressed(_exhibitNumberActions[i]))
                    {
                        _isTourActive = false;
                        _cameraRig.FocusExhibit(i);
                    }
                }
            }

            if (WasPressed(_nextPhaseAction))
            {
                ShowcaseSkyPreset next = _phaseStepper.NextPreset();
                _skyCycleSystem.SetClockTime(next.TimeSeconds, next.HoldEclipse, snapLighting: true);
            }

            if (WasPressed(_fastTimeAction))
            {
                bool fast = _phaseStepper.ToggleFastTime();
                if (fast && _skyCycleSystem.IsHoldingEclipse)
                {
                    _skyCycleSystem.ReleaseOpeningEclipse();
                }
            }

            if (WasPressed(_toggleHelpAction) && _helpOverlay != null)
            {
                _helpOverlay.ToggleVisible();
            }

            ShowcaseExhibitRig focused = _cameraRig.FocusedExhibit;
            if (focused == null)
            {
                return;
            }

            if (WasPressed(_stageSequenceAction))
            {
                focused.TriggerStageSequence();
            }

            if (WasPressed(_faceFeelingAction))
            {
                focused.CycleFaceFeeling();
            }

            if (WasPressed(_faceBlinkAction))
            {
                focused.TriggerFaceBlink();
            }

            if (WasPressed(_faceBootAction))
            {
                focused.TriggerFaceBoot();
            }

            if (WasPressed(_action1))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action1_Z);
            }

            if (WasPressed(_action2))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action2_X);
            }

            if (WasPressed(_action3))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action3_C);
            }

            if (WasPressed(_action4))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action4_V);
            }

            if (WasPressed(_action5))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action5_G);
            }

            if (WasPressed(_action6))
            {
                focused.TriggerKey(ShowcaseTriggerKey.Action6_R);
            }
        }

        private void HandleTimeControls(float dt)
        {
            float scrub = _scrubAction != null ? _scrubAction.ReadValue<float>() : 0f;
            if (Mathf.Abs(scrub) > 1e-4f)
            {
                float nextTime = _phaseStepper.ComputeScrubbedTime(_skyCycleSystem.TimeSeconds, scrub, dt);
                _skyCycleSystem.SetClockTime(nextTime, holdingEclipse: false, snapLighting: true);
                return;
            }

            if (_phaseStepper.IsFastTime && dt > 0f)
            {
                if (_skyCycleSystem.IsHoldingEclipse)
                {
                    _skyCycleSystem.ReleaseOpeningEclipse();
                }

                float extraSeconds = dt * (_phaseStepper.TimeScale - 1f);
                if (extraSeconds > 0f)
                {
                    _skyCycleSystem.StepFrame(extraSeconds);
                }
            }
        }

        private void HandleCameraInput(float dt)
        {
            Vector2 move = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            float vertical = _verticalAction != null ? _verticalAction.ReadValue<float>() : 0f;
            bool fast = _fastAction != null && _fastAction.IsPressed();
            Vector2 lookDelta = _lookDeltaAction != null ? _lookDeltaAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 lookRate = _lookRateAction != null ? _lookRateAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 scrollVec = _zoomAction != null ? _zoomAction.ReadValue<Vector2>() : Vector2.zero;
            float scrollNotches = Mathf.Abs(scrollVec.y) > 1f ? scrollVec.y / 120f : scrollVec.y;

            if (move.sqrMagnitude > 1e-4f || Mathf.Abs(vertical) > 1e-4f)
            {
                _isTourActive = false;
            }

            _cameraRig.StepCamera(dt, move, vertical, fast, lookDelta, lookRate, scrollNotches);
        }

        private void UpdateFocusStateFromCamera()
        {
            ShowcaseExhibitRig focused = _cameraRig.FocusedExhibit;
            if (focused != null)
            {
                _focusState.SetFocusPose(focused.transform.position, focused.transform.rotation);
            }
            else
            {
                _focusState.SetFocusPose(_cameraRig.transform.position, _cameraRig.transform.rotation);
            }
        }

        private void StepExhibits(float dt)
        {
            Vector3 camPos = _cameraRig.transform.position;
            Vector3 sunDir = _skyCycleSystem.SunDirection;
            SkyPhase phase = _skyCycleSystem.Phase;
            bool holdingEclipse = _skyCycleSystem.IsHoldingEclipse;
            float eclipseFactor = _skyCycleSystem.EclipseFactor;
            float sunlight = _skyCycleSystem.SmoothedSunlight;

            for (int i = 0; i < _exhibits.Length; i++)
            {
                if (_exhibits[i] != null)
                {
                    _exhibits[i].StepFrame(dt, camPos, sunDir, phase, holdingEclipse, eclipseFactor, sunlight);
                }
            }
        }

        private void RefreshUi()
        {
            if (_helpOverlay == null)
            {
                return;
            }

            ShowcaseExhibitRig focused = _cameraRig.FocusedExhibit;
            ShowcaseExhibitDefinition def = focused != null ? focused.Definition : null;
            string clockText = _phaseStepper.FormatClockReadout(
                _skyCycleSystem.TimeSeconds,
                _skyCycleSystem.IsHoldingEclipse);
            string phaseText = ShowcasePhaseStepper.FormatLivePhaseVi(
                _skyCycleSystem.Phase,
                _skyCycleSystem.IsHoldingEclipse,
                _skyCycleSystem.EclipseFactor);

            _helpOverlay.RefreshStatus(
                def,
                _cameraRig.IsFreeFly,
                clockText,
                phaseText,
                _phaseStepper.IsFastTime,
                _isTourActive,
                _isDemoMode);
        }

        private static bool WasPressed(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }
    }
}
