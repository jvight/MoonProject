using System;
using UnityEngine;

namespace MoonProject.Showcase
{
    /// <summary>Identifier for each of the 15 look-dev exhibits in the Showcase scene.</summary>
    public enum ShowcaseExhibitId
    {
        Rover07 = 0,
        CargoLander = 1,
        ChargingPad = 2,
        SolarArray = 3,
        PowerPylon = 4,
        RockCluster = 5,
        Rover07Upgraded = 6,
        BatteryBank = 7,
        AntennaMast = 8,
        RuinsCorner = 9,
    }

    /// <summary>Identifier for each of the 5 ruin prefabs arranged in the Ruins Corner exhibit.</summary>
    public enum ShowcaseRuinPieceId
    {
        HabitatModules = 0,
        RadioTower = 1,
        KenjiWorkshop = 2,
        FallenSolarArray = 3,
        ScatterSet = 4,
    }

    /// <summary>Local offset and yaw within the Ruins Corner exhibit for one ruin prefab.</summary>
    [Serializable]
    public sealed class ShowcaseRuinPiecePlacement
    {
        [Tooltip("Identifier of the ruin prefab piece.")]
        [SerializeField] private ShowcaseRuinPieceId _pieceId;

        [Tooltip("Local horizontal offset (X, 0, Z) relative to the Ruins Corner exhibit root.")]
        [SerializeField] private Vector3 _localOffset;

        [Tooltip("Local yaw rotation in degrees relative to the Ruins Corner exhibit root.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _localYawDegrees;

        public ShowcaseRuinPiecePlacement()
        {
        }

        public ShowcaseRuinPiecePlacement(ShowcaseRuinPieceId pieceId, Vector3 localOffset, float localYawDegrees)
        {
            _pieceId = pieceId;
            _localOffset = localOffset;
            _localYawDegrees = localYawDegrees;
        }

        public ShowcaseRuinPieceId PieceId => _pieceId;
        public Vector3 LocalOffset => _localOffset;
        public float LocalYawDegrees => _localYawDegrees;
    }

    /// <summary>How a node animation rule drives its target Transform relative to its authored rest pose.</summary>
    public enum ShowcaseMotionKind
    {
        LocalRotation = 0,
        LocalPosition = 1,
        ContinuousSpin = 2,
    }

    /// <summary>Input trigger key slot that toggles an exhibit node animation rule when focused.</summary>
    public enum ShowcaseTriggerKey
    {
        None = 0,
        Action1_Z = 1,
        Action2_X = 2,
        Action3_C = 3,
        Action4_V = 4,
        Action5_G = 5,
        Action6_R = 6,
    }

    /// <summary>Ordered sky-cycle phase presets cycled by pressing T in the Showcase.</summary>
    public enum ShowcaseSkyPresetId
    {
        EclipseHold = 0,
        DawnBead = 1,
        Sunrise = 2,
        FullSunlit = 3,
        Sunset = 4,
        EarthlitNight = 5,
    }

    /// <summary>One time-of-day preset for the T key phase jumper.</summary>
    [Serializable]
    public sealed class ShowcaseSkyPreset
    {
        [Tooltip("Identifier of this sky phase preset.")]
        [SerializeField] private ShowcaseSkyPresetId _presetId;

        [Tooltip("Vietnamese label shown in the Showcase help panel.")]
        [SerializeField] private string _labelVi = string.Empty;

        [Tooltip("Lunar clock time in seconds into Day 1.")]
        [SerializeField] private float _timeSeconds;

        [Tooltip("Whether the opening eclipse hold is active at this preset.")]
        [SerializeField] private bool _holdEclipse;

        public ShowcaseSkyPreset()
        {
        }

        public ShowcaseSkyPreset(ShowcaseSkyPresetId presetId, string labelVi, float timeSeconds, bool holdEclipse)
        {
            _presetId = presetId;
            _labelVi = labelVi ?? string.Empty;
            _timeSeconds = timeSeconds;
            _holdEclipse = holdEclipse;
        }

        public ShowcaseSkyPresetId PresetId => _presetId;
        public string LabelVi => _labelVi;
        public float TimeSeconds => _timeSeconds;
        public bool HoldEclipse => _holdEclipse;
    }

    /// <summary>One Vietnamese key-hint line displayed in the help panel for the focused exhibit.</summary>
    [Serializable]
    public sealed class ShowcaseKeyHint
    {
        [Tooltip("Key label shown on the left (e.g. 'Z', 'F', 'B').")]
        [SerializeField] private string _keyLabel = string.Empty;

        [Tooltip("Vietnamese description of what pressing the key does on this exhibit.")]
        [SerializeField] private string _descriptionVi = string.Empty;

        public ShowcaseKeyHint()
        {
        }

        public ShowcaseKeyHint(string keyLabel, string descriptionVi)
        {
            _keyLabel = keyLabel ?? string.Empty;
            _descriptionVi = descriptionVi ?? string.Empty;
        }

        public string KeyLabel => _keyLabel;
        public string DescriptionVi => _descriptionVi;
    }

    /// <summary>
    /// Data-driven node animation rule for a single Transform inside an exhibit prefab: specifies its relative path,
    /// motion kind, local axis, rest/active values, clamped mechanical range, ease time, and trigger key.
    /// </summary>
    [Serializable]
    public sealed class ShowcaseNodeRule
    {
        [Tooltip("Stable rule identifier used by tests and exhibit controllers (e.g. 'lander.ramp').")]
        [SerializeField] private string _ruleId = string.Empty;

        [Tooltip("Vietnamese label describing this animated mechanism.")]
        [SerializeField] private string _labelVi = string.Empty;

        [Tooltip("Transform path relative to the exhibit prefab root (e.g. 'Stage_4/ContactShoes').")]
        [SerializeField] private string _nodePath = string.Empty;

        [Tooltip("How the channel drives the Transform: local rotation, local position offset, or continuous spin.")]
        [SerializeField] private ShowcaseMotionKind _motionKind = ShowcaseMotionKind.LocalRotation;

        [Tooltip("Local-space axis along or around which the node moves or rotates.")]
        [SerializeField] private Vector3 _axis = Vector3.right;

        [Tooltip("Value (degrees or metres) when the channel is in its rest state.")]
        [SerializeField] private float _restValue;

        [Tooltip("Value (degrees, metres, or deg/sec for ContinuousSpin) when the channel is active.")]
        [SerializeField] private float _activeValue = 45f;

        [Tooltip("Lower mechanical clamp limit (degrees or metres).")]
        [SerializeField] private float _minLimit = -180f;

        [Tooltip("Upper mechanical clamp limit (degrees or metres).")]
        [SerializeField] private float _maxLimit = 180f;

        [Tooltip("Ease duration in seconds to transition between rest and active states.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _easeDuration = 0.9f;

        [Tooltip("Trigger key that toggles this node rule while its exhibit is focused.")]
        [SerializeField] private ShowcaseTriggerKey _triggerKey = ShowcaseTriggerKey.None;

        [Tooltip("Whether this channel starts in the active state on boot.")]
        [SerializeField] private bool _defaultActive;

        public ShowcaseNodeRule()
        {
        }

        public ShowcaseNodeRule(
            string ruleId,
            string labelVi,
            string nodePath,
            ShowcaseMotionKind motionKind,
            Vector3 axis,
            float restValue,
            float activeValue,
            float minLimit,
            float maxLimit,
            float easeDuration,
            ShowcaseTriggerKey triggerKey,
            bool defaultActive = false)
        {
            _ruleId = ruleId ?? string.Empty;
            _labelVi = labelVi ?? string.Empty;
            _nodePath = nodePath ?? string.Empty;
            _motionKind = motionKind;
            _axis = axis;
            _restValue = restValue;
            _activeValue = activeValue;
            _minLimit = minLimit;
            _maxLimit = maxLimit;
            _easeDuration = Mathf.Max(0.05f, easeDuration);
            _triggerKey = triggerKey;
            _defaultActive = defaultActive;
        }

        public string RuleId => _ruleId;
        public string LabelVi => _labelVi;
        public string NodePath => _nodePath;
        public ShowcaseMotionKind MotionKind => _motionKind;
        public Vector3 Axis => _axis;
        public float RestValue => _restValue;
        public float ActiveValue => _activeValue;
        public float MinLimit => _minLimit;
        public float MaxLimit => _maxLimit;
        public float EaseDuration => _easeDuration;
        public ShowcaseTriggerKey TriggerKey => _triggerKey;
        public bool DefaultActive => _defaultActive;
    }

    /// <summary>
    /// Placement, orbit camera framing, Vietnamese labels, and node animation rules for one exhibit in Showcase.
    /// </summary>
    [Serializable]
    public sealed class ShowcaseExhibitDefinition
    {
        [Tooltip("Identifier of the exhibit.")]
        [SerializeField] private ShowcaseExhibitId _exhibitId;

        [Tooltip("1-based index (1..15) of this exhibit; keys 1..6 jump directly to the first 6 exhibits and Tab cycles all 15.")]
        [Range(1, 15)]
        [SerializeField] private int _numberKey = 1;

        [Tooltip("Vietnamese title shown in the help overlay when focused.")]
        [SerializeField] private string _titleVi = string.Empty;

        [Tooltip("Vietnamese subtitle describing the exhibit.")]
        [SerializeField] private string _subtitleVi = string.Empty;

        [Tooltip("Horizontal ground offset (X, 0, Z) on the open basin floor; Y is sampled from MoonSurface.")]
        [SerializeField] private Vector3 _positionOffset;

        [Tooltip("World yaw rotation in degrees applied to the exhibit root on the ground.")]
        [SerializeField] private float _yawDegrees;

        [Tooltip("Local offset from the exhibit ground root to the camera orbit pivot point.")]
        [SerializeField] private Vector3 _orbitPivotOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("Default world yaw (degrees) of the camera look direction when jumping to this exhibit.")]
        [SerializeField] private float _defaultOrbitYaw = 155f;

        [Tooltip("Default camera pitch (degrees, positive looks downward) when jumping to this exhibit.")]
        [SerializeField] private float _defaultOrbitPitch = 12f;

        [Tooltip("Default orbit distance in metres from the exhibit pivot.")]
        [SerializeField] private float _defaultOrbitDistance = 4.2f;

        [Tooltip("Minimum orbit distance in metres when zooming in.")]
        [SerializeField] private float _minOrbitDistance = 1.6f;

        [Tooltip("Maximum orbit distance in metres when zooming out.")]
        [SerializeField] private float _maxOrbitDistance = 14f;

        [Tooltip("Animated node rules belonging to this exhibit.")]
        [SerializeField] private ShowcaseNodeRule[] _nodes = Array.Empty<ShowcaseNodeRule>();

        [Tooltip("Vietnamese key-hint lines displayed in the help panel when this exhibit is focused.")]
        [SerializeField] private ShowcaseKeyHint[] _keyHints = Array.Empty<ShowcaseKeyHint>();

        public ShowcaseExhibitDefinition()
        {
        }

        public ShowcaseExhibitDefinition(
            ShowcaseExhibitId exhibitId,
            int numberKey,
            string titleVi,
            string subtitleVi,
            Vector3 positionOffset,
            float yawDegrees,
            Vector3 orbitPivotOffset,
            float defaultOrbitYaw,
            float defaultOrbitPitch,
            float defaultOrbitDistance,
            float minOrbitDistance,
            float maxOrbitDistance,
            ShowcaseNodeRule[] nodes,
            ShowcaseKeyHint[] keyHints)
        {
            _exhibitId = exhibitId;
            _numberKey = numberKey;
            _titleVi = titleVi ?? string.Empty;
            _subtitleVi = subtitleVi ?? string.Empty;
            _positionOffset = positionOffset;
            _yawDegrees = yawDegrees;
            _orbitPivotOffset = orbitPivotOffset;
            _defaultOrbitYaw = defaultOrbitYaw;
            _defaultOrbitPitch = defaultOrbitPitch;
            _defaultOrbitDistance = defaultOrbitDistance;
            _minOrbitDistance = minOrbitDistance;
            _maxOrbitDistance = maxOrbitDistance;
            _nodes = nodes ?? Array.Empty<ShowcaseNodeRule>();
            _keyHints = keyHints ?? Array.Empty<ShowcaseKeyHint>();
        }

        public ShowcaseExhibitId ExhibitId => _exhibitId;
        public int NumberKey => _numberKey;
        public string TitleVi => _titleVi;
        public string SubtitleVi => _subtitleVi;
        public Vector3 PositionOffset => _positionOffset;
        public float YawDegrees => _yawDegrees;
        public Vector3 OrbitPivotOffset => _orbitPivotOffset;
        public float DefaultOrbitYaw => _defaultOrbitYaw;
        public float DefaultOrbitPitch => _defaultOrbitPitch;
        public float DefaultOrbitDistance => _defaultOrbitDistance;
        public float MinOrbitDistance => _minOrbitDistance;
        public float MaxOrbitDistance => _maxOrbitDistance;
        public ShowcaseNodeRule[] Nodes => _nodes;
        public ShowcaseKeyHint[] KeyHints => _keyHints;
    }

    /// <summary>
    /// Tuning ScriptableObject (`Assets/_Project/Data/Tuning/ShowcaseTuning.asset`) for the R1 Look-Dev Showcase.
    /// Lists camera feel, time-of-day phase presets, lamp emission strengths, build-stage animation, camera tour,
    /// ruin cluster layout, and every exhibit's animated node rules.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Showcase Tuning", fileName = "ShowcaseTuning")]
    public sealed class ShowcaseTuning : ScriptableObject
    {
        [Header("Camera (Free-fly & Orbit)")]
        [Tooltip("Base horizontal free-fly camera speed in m/s (WASD).")]
        [Range(1f, 40f)]
        [SerializeField] private float _freeFlySpeed = 8.5f;

        [Tooltip("Speed multiplier when holding Shift in free-fly camera mode.")]
        [Range(1.5f, 10f)]
        [SerializeField] private float _freeFlyFastMultiplier = 3.2f;

        [Tooltip("Vertical free-fly camera speed in m/s (Q down, E up).")]
        [Range(1f, 30f)]
        [SerializeField] private float _freeFlyVerticalSpeed = 6.0f;

        [Tooltip("Mouse look sensitivity in degrees per pixel of mouse delta.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _lookSensitivity = 0.16f;

        [Tooltip("Gamepad stick look rate in degrees per second.")]
        [Range(30f, 300f)]
        [SerializeField] private float _gamepadLookRate = 120f;

        [Tooltip("Orbit distance change in metres per mouse scroll notch.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _orbitZoomStep = 0.45f;

        [Tooltip("Ease duration in seconds when jumping between exhibits (Tab / 1-6) or smoothing orbit look.")]
        [Range(0.15f, 3f)]
        [SerializeField] private float _orbitTransitionSeconds = 0.75f;

        [Tooltip("Minimum camera pitch angle in degrees (looking upward).")]
        [Range(-85f, -5f)]
        [SerializeField] private float _minPitchDegrees = -75f;

        [Tooltip("Maximum camera pitch angle in degrees (looking downward).")]
        [Range(5f, 85f)]
        [SerializeField] private float _maxPitchDegrees = 80f;

        [Header("Time of Day & Sky Cycle")]
        [Tooltip("Lunar clock scrub speed in clock-seconds per real second when holding [ or ].")]
        [Range(60f, 3600f)]
        [SerializeField] private float _scrubSecondsPerSecond = 600f;

        [Tooltip("Time speed multiplier toggled by pressing P (60x speed).")]
        [Range(10f, 240f)]
        [SerializeField] private float _fastTimeScale = 60f;

        [Tooltip("Ordered sky phase presets cycled by pressing T.")]
        [SerializeField] private ShowcaseSkyPreset[] _skyPresets = Array.Empty<ShowcaseSkyPreset>();

        [Header("R1 Amber Lamps (_EmissionStrength)")]
        [Tooltip("_EmissionStrength value written via MaterialPropertyBlock when an R1_LampAmber surface is off.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _lampOffEmission = 0f;

        [Tooltip("_EmissionStrength value written via MaterialPropertyBlock when an R1_LampAmber surface is lit.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lampOnEmission = 2.2f;

        [Tooltip("Minimum _EmissionStrength during Charging Pad totem pulse.")]
        [Range(0f, 2f)]
        [SerializeField] private float _lampPulseMinEmission = 0.25f;

        [Tooltip("Maximum _EmissionStrength during Charging Pad totem pulse.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _lampPulseMaxEmission = 2.5f;

        [Tooltip("Pulse speed in cycles per second for pulsing totem bars.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _lampPulseSpeed = 0.85f;

        [Tooltip("Ease duration in seconds when lamps switch on or off.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _lampEaseSeconds = 0.55f;

        [Header("Build-Stage Sequence & Auto-Demo")]
        [Tooltip("Vertical distance in metres below rest from which each build stage (Stage_1..4) rises during the stage sequence.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stageRiseDepthMeters = 0.42f;

        [Tooltip("Upward overshoot in metres at the peak of the soft rise-and-settle curve for each build stage.")]
        [Range(0f, 0.25f)]
        [SerializeField] private float _stageSettleOvershootMeters = 0.035f;

        [Tooltip("Duration in seconds for one build stage to rise and settle into its rest pose.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _stageRiseDurationSeconds = 0.65f;

        [Tooltip("Delay in seconds between consecutive build stages starting their rise.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _stageStepIntervalSeconds = 0.55f;

        [Tooltip("Hold duration in seconds at Stage 4 (with active moving parts) before the auto-demo loop restarts the stage sequence.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _stageAutoLoopHoldSeconds = 2.4f;

        [Tooltip("Interval in seconds between rover face feeling transitions when Demo Mode is active.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _feelingDemoIntervalSeconds = 2.2f;

        [Header("Slow Camera Tour (O Key)")]
        [Tooltip("Duration in seconds spent visiting each exhibit during the O-key slow camera tour.")]
        [Range(1.5f, 15f)]
        [SerializeField] private float _tourExhibitHoldSeconds = 4.2f;

        [Tooltip("Eased camera transition duration in seconds when moving between exhibits during the O-key tour.")]
        [Range(0.3f, 5f)]
        [SerializeField] private float _tourTransitionSeconds = 1.6f;

        [Tooltip("Gentle horizontal orbit sweep angle in degrees across each exhibit visit during the O-key tour.")]
        [Range(0f, 90f)]
        [SerializeField] private float _tourOrbitSweepDegrees = 22f;

        [Header("Ruins Corner Cluster Layout")]
        [Tooltip("Local offset and yaw of each of the 5 ruin prefabs arranged inside the Ruins Corner exhibit.")]
        [SerializeField] private ShowcaseRuinPiecePlacement[] _ruinPieces = Array.Empty<ShowcaseRuinPiecePlacement>();

        [Header("Exhibits")]
        [Tooltip("The 15 look-dev exhibits placed in the Showcase scene.")]
        [SerializeField] private ShowcaseExhibitDefinition[] _exhibits = Array.Empty<ShowcaseExhibitDefinition>();

        public float FreeFlySpeed => _freeFlySpeed;
        public float FreeFlyFastMultiplier => _freeFlyFastMultiplier;
        public float FreeFlyVerticalSpeed => _freeFlyVerticalSpeed;
        public float LookSensitivity => _lookSensitivity;
        public float GamepadLookRate => _gamepadLookRate;
        public float OrbitZoomStep => _orbitZoomStep;
        public float OrbitTransitionSeconds => _orbitTransitionSeconds;
        public float MinPitchDegrees => _minPitchDegrees;
        public float MaxPitchDegrees => _maxPitchDegrees;
        public float ScrubSecondsPerSecond => _scrubSecondsPerSecond;
        public float FastTimeScale => _fastTimeScale;
        public ShowcaseSkyPreset[] SkyPresets => _skyPresets;
        public float LampOffEmission => _lampOffEmission;
        public float LampOnEmission => _lampOnEmission;
        public float LampPulseMinEmission => _lampPulseMinEmission;
        public float LampPulseMaxEmission => _lampPulseMaxEmission;
        public float LampPulseSpeed => _lampPulseSpeed;
        public float LampEaseSeconds => _lampEaseSeconds;
        public float StageRiseDepthMeters => _stageRiseDepthMeters;
        public float StageSettleOvershootMeters => _stageSettleOvershootMeters;
        public float StageRiseDurationSeconds => _stageRiseDurationSeconds;
        public float StageStepIntervalSeconds => _stageStepIntervalSeconds;
        public float StageAutoLoopHoldSeconds => _stageAutoLoopHoldSeconds;
        public float FeelingDemoIntervalSeconds => _feelingDemoIntervalSeconds;
        public float TourExhibitHoldSeconds => _tourExhibitHoldSeconds;
        public float TourTransitionSeconds => _tourTransitionSeconds;
        public float TourOrbitSweepDegrees => _tourOrbitSweepDegrees;
        public ShowcaseRuinPiecePlacement[] RuinPieces => _ruinPieces;
        public ShowcaseExhibitDefinition[] Exhibits => _exhibits;

        public void Configure(
            ShowcaseSkyPreset[] skyPresets,
            ShowcaseExhibitDefinition[] exhibits,
            ShowcaseRuinPiecePlacement[] ruinPieces = null,
            float freeFlySpeed = 8.5f,
            float freeFlyFastMultiplier = 3.2f,
            float freeFlyVerticalSpeed = 6.0f,
            float lookSensitivity = 0.16f,
            float gamepadLookRate = 120f,
            float orbitZoomStep = 0.45f,
            float orbitTransitionSeconds = 0.75f,
            float scrubSecondsPerSecond = 600f,
            float fastTimeScale = 60f,
            float lampOffEmission = 0f,
            float lampOnEmission = 2.2f,
            float lampPulseMinEmission = 0.25f,
            float lampPulseMaxEmission = 2.5f,
            float lampPulseSpeed = 0.85f,
            float lampEaseSeconds = 0.55f,
            float stageRiseDepthMeters = 0.42f,
            float stageSettleOvershootMeters = 0.035f,
            float stageRiseDurationSeconds = 0.65f,
            float stageStepIntervalSeconds = 0.55f,
            float stageAutoLoopHoldSeconds = 2.4f,
            float feelingDemoIntervalSeconds = 2.2f,
            float tourExhibitHoldSeconds = 4.2f,
            float tourTransitionSeconds = 1.6f,
            float tourOrbitSweepDegrees = 22f)
        {
            _skyPresets = skyPresets ?? Array.Empty<ShowcaseSkyPreset>();
            _exhibits = exhibits ?? Array.Empty<ShowcaseExhibitDefinition>();
            _ruinPieces = ruinPieces ?? Array.Empty<ShowcaseRuinPiecePlacement>();
            _freeFlySpeed = freeFlySpeed;
            _freeFlyFastMultiplier = freeFlyFastMultiplier;
            _freeFlyVerticalSpeed = freeFlyVerticalSpeed;
            _lookSensitivity = lookSensitivity;
            _gamepadLookRate = gamepadLookRate;
            _orbitZoomStep = orbitZoomStep;
            _orbitTransitionSeconds = orbitTransitionSeconds;
            _scrubSecondsPerSecond = scrubSecondsPerSecond;
            _fastTimeScale = fastTimeScale;
            _lampOffEmission = lampOffEmission;
            _lampOnEmission = lampOnEmission;
            _lampPulseMinEmission = lampPulseMinEmission;
            _lampPulseMaxEmission = lampPulseMaxEmission;
            _lampPulseSpeed = lampPulseSpeed;
            _lampEaseSeconds = lampEaseSeconds;
            _stageRiseDepthMeters = stageRiseDepthMeters;
            _stageSettleOvershootMeters = stageSettleOvershootMeters;
            _stageRiseDurationSeconds = stageRiseDurationSeconds;
            _stageStepIntervalSeconds = stageStepIntervalSeconds;
            _stageAutoLoopHoldSeconds = stageAutoLoopHoldSeconds;
            _feelingDemoIntervalSeconds = feelingDemoIntervalSeconds;
            _tourExhibitHoldSeconds = tourExhibitHoldSeconds;
            _tourTransitionSeconds = tourTransitionSeconds;
            _tourOrbitSweepDegrees = tourOrbitSweepDegrees;
        }

        public ShowcaseExhibitDefinition FindExhibit(ShowcaseExhibitId id)
        {
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

        public string Validate()
        {
            if (_skyPresets == null || _skyPresets.Length < 6)
            {
                return "ShowcaseTuning must define all 6 sky phase presets.";
            }

            if (_exhibits == null || _exhibits.Length != 15)
            {
                return "ShowcaseTuning must define all 15 exhibits.";
            }

            if (_ruinPieces == null || _ruinPieces.Length != 5)
            {
                return "ShowcaseTuning must define all 5 ruin piece placements.";
            }

            for (int i = 0; i < _exhibits.Length; i++)
            {
                ShowcaseExhibitDefinition ex = _exhibits[i];
                if (ex == null)
                {
                    return $"Exhibit at index {i} is null.";
                }

                if (string.IsNullOrWhiteSpace(ex.TitleVi))
                {
                    return $"Exhibit {ex.ExhibitId} is missing TitleVi.";
                }

                ShowcaseNodeRule[] rules = ex.Nodes;
                if (rules == null)
                {
                    continue;
                }

                for (int r = 0; r < rules.Length; r++)
                {
                    ShowcaseNodeRule rule = rules[r];
                    if (rule == null || string.IsNullOrWhiteSpace(rule.RuleId) || string.IsNullOrWhiteSpace(rule.NodePath))
                    {
                        return $"Exhibit {ex.ExhibitId} has an invalid node rule at index {r}.";
                    }

                    if (rule.Axis.sqrMagnitude < 1e-6f)
                    {
                        return $"Rule '{rule.RuleId}' has a zero axis.";
                    }

                    if (rule.EaseDuration <= 0f)
                    {
                        return $"Rule '{rule.RuleId}' must have positive EaseDuration.";
                    }

                    if (rule.MinLimit > rule.MaxLimit)
                    {
                        return $"Rule '{rule.RuleId}' has MinLimit > MaxLimit.";
                    }
                }
            }

            return null;
        }
    }
}
