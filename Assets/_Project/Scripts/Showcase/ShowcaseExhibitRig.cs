using System;
using UnityEngine;
using MoonProject.Core.Events;
using MoonProject.Rover;
using MoonProject.World;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Pre-bound Transform reference for one <see cref="ShowcaseNodeRule"/> inside a <see cref="ShowcaseExhibitRig"/>.
    /// Wired by <c>ShowcaseSceneBuilder</c> so runtime never searches the hierarchy by name.
    /// </summary>
    [Serializable]
    public struct ShowcaseBoundNode
    {
        [Tooltip("Rule ID matching a ShowcaseNodeRule in ShowcaseTuning.")]
        [SerializeField] private string _ruleId;

        [Tooltip("Target Transform inside the exhibit prefab instance.")]
        [SerializeField] private Transform _target;

        [Tooltip("Authored rest localPosition captured at scene build time.")]
        [SerializeField] private Vector3 _restLocalPosition;

        [Tooltip("Authored rest localRotation captured at scene build time.")]
        [SerializeField] private Quaternion _restLocalRotation;

        public ShowcaseBoundNode(
            string ruleId,
            Transform target,
            Vector3 restLocalPosition,
            Quaternion restLocalRotation)
        {
            _ruleId = ruleId ?? string.Empty;
            _target = target;
            _restLocalPosition = restLocalPosition;
            _restLocalRotation = restLocalRotation;
        }

        public string RuleId => _ruleId;
        public Transform Target => _target;
        public Vector3 RestLocalPosition => _restLocalPosition;
        public Quaternion RestLocalRotation => _restLocalRotation;
    }

    /// <summary>
    /// Runtime controller attached to each of the 6 look-dev exhibits in the Showcase scene. Drives the exhibit's
    /// <see cref="ShowcaseNodeSequencer"/>, <see cref="ShowcaseLampDriver"/>, 07's <see cref="FaceDisplay"/> and
    /// camera look-at, the Cargo Lander's broken/repaired bay swap and solar wings, and the Solar Array's live Sun
    /// tracking and night/eclipse folding.
    /// </summary>
    public sealed class ShowcaseExhibitRig : MonoBehaviour
    {
        public const string RoverHeadLookRuleId = "rover.neck.yaw";
        public const string RoverHeadPitchRuleId = "rover.head.pitch";
        public const string LanderWingLeftPitchRuleId = "lander.wing.l.pitch";
        public const string LanderWingRightPitchRuleId = "lander.wing.r.pitch";
        public const string LanderWingLeftFoldRuleId = "lander.wing.l.fold";
        public const string LanderBayRepairRuleId = "lander.bay.contacts";
        public const string SolarAzimuthRuleId = "solar.azimuth";
        public const string SolarElevationRuleId = "solar.elevation";
        public const string SolarWingLeftRuleId = "solar.wing.l";
        public const string SolarWingRightRuleId = "solar.wing.r";

        private static readonly FaceFeeling[] AllFeelings =
        {
            FaceFeeling.Calm,
            FaceFeeling.Curious,
            FaceFeeling.Happy,
            FaceFeeling.Proud,
            FaceFeeling.Worried,
            FaceFeeling.Tired,
            FaceFeeling.Sad,
            FaceFeeling.Startled,
            FaceFeeling.Asleep,
        };

        [Tooltip("Which exhibit this rig controls.")]
        [SerializeField] private ShowcaseExhibitId _exhibitId;

        [Tooltip("Shared Showcase tuning asset.")]
        [SerializeField] private ShowcaseTuning _tuning;

        [Tooltip("Pre-bound Transform nodes matching this exhibit's rules in ShowcaseTuning.")]
        [SerializeField] private ShowcaseBoundNode[] _boundNodes = Array.Empty<ShowcaseBoundNode>();

        [Tooltip("Primary R1_LampAmber MeshRenderers driven via MaterialPropertyBlock (_EmissionStrength).")]
        [SerializeField] private MeshRenderer[] _lampRenderers = Array.Empty<MeshRenderer>();

        [Tooltip("Secondary R1_LampAmber MeshRenderers (e.g. ChargingPad guide strips).")]
        [SerializeField] private MeshRenderer[] _secondaryLampRenderers = Array.Empty<MeshRenderer>();

        [Tooltip("Accent point/spot lights paired with this exhibit's lamps.")]
        [SerializeField] private Light[] _accentLights = Array.Empty<Light>();

        [Tooltip("Maximum intensity for each light in _accentLights when fully lit.")]
        [SerializeField] private float[] _accentLightMaxIntensities = Array.Empty<float>();

        [Tooltip("Trigger key that toggles this exhibit's lamp state (None if always on or not applicable).")]
        [SerializeField] private ShowcaseTriggerKey _lampTriggerKey = ShowcaseTriggerKey.None;

        [Tooltip("Whether this exhibit's lamps start lit on boot.")]
        [SerializeField] private bool _lampsDefaultLit = true;

        [Header("Rover 07 Specific")]
        [Tooltip("07's CRT FaceDisplay component (only assigned on Rover07).")]
        [SerializeField] private FaceDisplay _faceDisplay;

        [Tooltip("07's Head transform used to compute local camera gaze direction (only assigned on Rover07).")]
        [SerializeField] private Transform _roverHeadTransform;

        [Header("Cargo Lander Specific")]
        [Tooltip("Broken-state bay meshes (Bay_CableGap, Bay_ConnectorSocket) on CargoLander.")]
        [SerializeField] private GameObject[] _brokenBayObjects = Array.Empty<GameObject>();

        [Tooltip("Repaired-state bay meshes (Bay_CableFixed, Bay_Connector) on CargoLander.")]
        [SerializeField] private GameObject[] _repairedBayObjects = Array.Empty<GameObject>();

        [Header("Solar Array Specific")]
        [Tooltip("Stage_4 transform on SolarArray used as the reference frame for Sun tracking.")]
        [SerializeField] private Transform _solarStage4Transform;

        private ShowcaseExhibitDefinition _definition;
        private ShowcaseNodeSequencer _sequencer;
        private ShowcaseLampDriver _lampDriver;
        private bool _lampsLit;
        private int _feelingIndex;
        private bool _roverCameraTracking = true;
        private bool _solarManualFold;
        private bool _solarInspectPose;
        private float _elapsedTime;
        private bool _initialized;

        public ShowcaseExhibitId ExhibitId => _exhibitId;
        public ShowcaseExhibitDefinition Definition => _definition;
        public ShowcaseNodeSequencer Sequencer => _sequencer;
        public bool AreLampsLit => _lampsLit;
        public FaceFeeling CurrentFeeling => AllFeelings[_feelingIndex];
        public Vector3 OrbitPivotWorld =>
            transform.position + (_definition != null ? _definition.OrbitPivotOffset : new Vector3(0f, 1.0f, 0f));

        public void Initialize(ShowcaseTuning tuning)
        {
            if (tuning != null)
            {
                _tuning = tuning;
            }

            _definition = _tuning != null ? _tuning.FindExhibit(_exhibitId) : null;
            ShowcaseNodeRule[] rules = _definition != null ? _definition.Nodes : Array.Empty<ShowcaseNodeRule>();
            _sequencer = new ShowcaseNodeSequencer(rules);
            _lampsLit = _lampsDefaultLit;
            _lampDriver = new ShowcaseLampDriver(_lampsLit);
            _feelingIndex = 0;
            _roverCameraTracking = true;
            _solarManualFold = false;
            _solarInspectPose = false;
            _initialized = true;

            if (_faceDisplay != null)
            {
                _faceDisplay.SetFeeling(AllFeelings[_feelingIndex]);
                _faceDisplay.Step(0.02f);
            }

            UpdateLanderBayObjects();
            ApplyBoundNodes();
            ApplyLamps(0f);
        }

        private void Awake()
        {
            if (!_initialized && _tuning != null)
            {
                Initialize(_tuning);
            }
        }

        public void TriggerKey(ShowcaseTriggerKey key)
        {
            if (!_initialized || key == ShowcaseTriggerKey.None)
            {
                return;
            }

            if (_exhibitId == ShowcaseExhibitId.Rover07 && key == ShowcaseTriggerKey.Action1_Z)
            {
                _roverCameraTracking = !_roverCameraTracking;
                if (!_roverCameraTracking)
                {
                    _sequencer.ClearExternalTarget(RoverHeadLookRuleId);
                    _sequencer.ClearExternalTarget(RoverHeadPitchRuleId);
                    _sequencer.SetRuleActive(RoverHeadLookRuleId, true);
                    _sequencer.SetRuleActive(RoverHeadPitchRuleId, true);
                }
                return;
            }

            if (_exhibitId == ShowcaseExhibitId.SolarArray)
            {
                if (key == ShowcaseTriggerKey.Action1_Z)
                {
                    _solarManualFold = !_solarManualFold;
                    return;
                }

                if (key == ShowcaseTriggerKey.Action2_X)
                {
                    _solarInspectPose = !_solarInspectPose;
                    return;
                }
            }

            if (_lampTriggerKey == key)
            {
                _lampsLit = !_lampsLit;
            }

            _sequencer.ToggleKey(key);
        }

        public void SetLampsLit(bool lit)
        {
            _lampsLit = lit;
        }

        public FaceFeeling CycleFaceFeeling()
        {
            if (_faceDisplay == null)
            {
                return FaceFeeling.Calm;
            }

            _feelingIndex = (_feelingIndex + 1) % AllFeelings.Length;
            FaceFeeling next = AllFeelings[_feelingIndex];
            _faceDisplay.SetFeeling(next);
            return next;
        }

        public void SetFaceFeeling(FaceFeeling feeling)
        {
            for (int i = 0; i < AllFeelings.Length; i++)
            {
                if (AllFeelings[i] == feeling)
                {
                    _feelingIndex = i;
                    break;
                }
            }

            if (_faceDisplay != null)
            {
                _faceDisplay.SetFeeling(feeling);
            }
        }

        public void SnapFaceFeeling(FaceFeeling feeling)
        {
            SetFaceFeeling(feeling);
            if (_faceDisplay != null && _faceDisplay.State != null)
            {
                _faceDisplay.State.SnapToFeeling(FaceFeeling.Asleep);
                _faceDisplay.State.SnapToFeeling(feeling);
                _faceDisplay.ApplyToRenderer();
            }
        }

        public void TriggerFaceBlink()
        {
            if (_faceDisplay != null)
            {
                _faceDisplay.Blink();
            }
        }

        public void TriggerFaceBoot()
        {
            if (_faceDisplay != null)
            {
                _faceDisplay.Boot();
            }
        }

        public void StepFrame(
            float deltaTime,
            Vector3 cameraWorldPosition,
            Vector3 sunDirection,
            SkyPhase skyPhase,
            bool isHoldingEclipse,
            float eclipseFactor,
            float sunlightFactor)
        {
            if (!_initialized)
            {
                return;
            }

            _elapsedTime += Mathf.Max(0f, deltaTime);

            switch (_exhibitId)
            {
                case ShowcaseExhibitId.Rover07:
                    UpdateRoverTracking(deltaTime, cameraWorldPosition);
                    break;
                case ShowcaseExhibitId.CargoLander:
                    UpdateLanderSolarAndBay(sunDirection, skyPhase, isHoldingEclipse, eclipseFactor, sunlightFactor);
                    break;
                case ShowcaseExhibitId.SolarArray:
                    UpdateSolarArrayTracking(sunDirection, skyPhase, isHoldingEclipse, eclipseFactor, sunlightFactor);
                    break;
            }

            _sequencer.Step(deltaTime);
            ApplyBoundNodes();
            ApplyLamps(deltaTime);
        }

        private void UpdateRoverTracking(float deltaTime, Vector3 cameraWorldPosition)
        {
            Vector3 localCamFromRover = transform.InverseTransformPoint(cameraWorldPosition);
            Vector3 toCam = localCamFromRover - new Vector3(0f, 1.22f, 0.25f);
            float horizDist = Mathf.Sqrt(toCam.x * toCam.x + toCam.z * toCam.z);
            bool camInFront = toCam.z > 0.05f && horizDist > 0.05f && horizDist < 8.5f;

            if (_faceDisplay != null && _roverHeadTransform != null)
            {
                if (camInFront)
                {
                    Vector3 localCamFromHead = _roverHeadTransform.InverseTransformPoint(cameraWorldPosition);
                    _faceDisplay.LookAt(localCamFromHead);
                }
                else
                {
                    _faceDisplay.ClearLookTarget();
                }

                if (!Application.isPlaying)
                {
                    _faceDisplay.Step(deltaTime);
                }
            }

            if (!_roverCameraTracking)
            {
                return;
            }

            if (camInFront)
            {
                float rawYaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
                float clampedYaw = Mathf.Clamp(rawYaw, -65f, 65f);
                float rawPitch = -Mathf.Atan2(toCam.y, horizDist) * Mathf.Rad2Deg;
                float clampedPitch = Mathf.Clamp(rawPitch, -12f, 16f);
                _sequencer.SetExternalTarget(RoverHeadLookRuleId, clampedYaw);
                _sequencer.SetExternalTarget(RoverHeadPitchRuleId, clampedPitch);
            }
            else
            {
                _sequencer.SetExternalTarget(RoverHeadLookRuleId, 0f);
                _sequencer.SetExternalTarget(RoverHeadPitchRuleId, 0f);
            }
        }

        private void UpdateLanderSolarAndBay(
            Vector3 sunDirection,
            SkyPhase skyPhase,
            bool isHoldingEclipse,
            float eclipseFactor,
            float sunlightFactor)
        {
            ShowcaseNodeChannel foldChannel = _sequencer.FindChannel(LanderWingLeftFoldRuleId);
            bool wingsFolded = foldChannel != null && foldChannel.IsActive;
            bool isSunlit = !ShowcaseSolarTracker.ShouldFoldForSky(
                skyPhase, isHoldingEclipse, eclipseFactor, sunlightFactor);
            Vector3 localSun = transform.InverseTransformDirection(sunDirection);
            float wingPitch = ShowcaseSolarTracker.ComputeLanderWingSunPitch(localSun, wingsFolded, isSunlit);
            _sequencer.SetExternalTarget(LanderWingLeftPitchRuleId, wingPitch);
            _sequencer.SetExternalTarget(LanderWingRightPitchRuleId, wingPitch);

            UpdateLanderBayObjects();
        }

        private void UpdateLanderBayObjects()
        {
            if (_exhibitId != ShowcaseExhibitId.CargoLander)
            {
                return;
            }

            ShowcaseNodeChannel repairChannel = _sequencer.FindChannel(LanderBayRepairRuleId);
            bool repaired = repairChannel == null || repairChannel.Progress01 >= 0.5f;

            if (_brokenBayObjects != null)
            {
                for (int i = 0; i < _brokenBayObjects.Length; i++)
                {
                    if (_brokenBayObjects[i] != null && _brokenBayObjects[i].activeSelf == repaired)
                    {
                        _brokenBayObjects[i].SetActive(!repaired);
                    }
                }
            }

            if (_repairedBayObjects != null)
            {
                for (int i = 0; i < _repairedBayObjects.Length; i++)
                {
                    if (_repairedBayObjects[i] != null && _repairedBayObjects[i].activeSelf != repaired)
                    {
                        _repairedBayObjects[i].SetActive(repaired);
                    }
                }
            }
        }

        private void UpdateSolarArrayTracking(
            Vector3 sunDirection,
            SkyPhase skyPhase,
            bool isHoldingEclipse,
            float eclipseFactor,
            float sunlightFactor)
        {
            Transform frame = _solarStage4Transform != null ? _solarStage4Transform : transform;
            Vector3 localSun = frame.InverseTransformDirection(sunDirection);
            bool skyFold = ShowcaseSolarTracker.ShouldFoldForSky(
                skyPhase, isHoldingEclipse, eclipseFactor, sunlightFactor);
            bool shouldFold = _solarManualFold || skyFold;

            float azimuthDeg;
            float elevationOffsetDeg;
            float wingFoldDeg;
            if (_solarInspectPose && !shouldFold)
            {
                azimuthDeg = 48f;
                elevationOffsetDeg = 18f;
                wingFoldDeg = 0f;
            }
            else
            {
                ShowcaseSolarTracker.ComputeSolarArrayTargets(
                    localSun,
                    shouldFold,
                    out azimuthDeg,
                    out elevationOffsetDeg,
                    out wingFoldDeg);
            }

            _sequencer.SetExternalTarget(SolarAzimuthRuleId, azimuthDeg);
            _sequencer.SetExternalTarget(SolarElevationRuleId, elevationOffsetDeg);
            _sequencer.SetExternalTarget(SolarWingLeftRuleId, wingFoldDeg);
            _sequencer.SetExternalTarget(SolarWingRightRuleId, -wingFoldDeg);
        }

        private void ApplyBoundNodes()
        {
            if (_boundNodes == null || _sequencer == null)
            {
                return;
            }

            for (int i = 0; i < _boundNodes.Length; i++)
            {
                Transform target = _boundNodes[i].Target;
                if (target != null)
                {
                    target.localPosition = _boundNodes[i].RestLocalPosition;
                    target.localRotation = _boundNodes[i].RestLocalRotation;
                }
            }

            for (int i = 0; i < _boundNodes.Length; i++)
            {
                ShowcaseBoundNode bound = _boundNodes[i];
                if (bound.Target == null)
                {
                    continue;
                }

                ShowcaseNodeChannel channel = _sequencer.FindChannel(bound.RuleId);
                if (channel == null)
                {
                    continue;
                }

                if (channel.Rule.MotionKind == ShowcaseMotionKind.LocalPosition)
                {
                    bound.Target.localPosition += channel.NormalizedAxis * channel.CurrentValue;
                }
                else
                {
                    bound.Target.localRotation =
                        bound.Target.localRotation * Quaternion.AngleAxis(channel.CurrentValue, channel.NormalizedAxis);
                }
            }
        }

        private void ApplyLamps(float deltaTime)
        {
            if (_lampDriver == null || _tuning == null)
            {
                return;
            }

            _lampDriver.StepTowards(_lampsLit, _tuning.LampEaseSeconds, deltaTime);

            if (_exhibitId == ShowcaseExhibitId.ChargingPad)
            {
                _lampDriver.ApplyPulsing(
                    _lampRenderers,
                    _secondaryLampRenderers,
                    _tuning.LampOffEmission,
                    _tuning.LampPulseMinEmission,
                    _tuning.LampPulseMaxEmission,
                    _tuning.LampPulseSpeed,
                    _elapsedTime,
                    _accentLights,
                    _accentLightMaxIntensities);
                return;
            }

            _lampDriver.ApplyUniform(
                _lampRenderers,
                _tuning.LampOffEmission,
                _tuning.LampOnEmission,
                _accentLights,
                _accentLightMaxIntensities);

            if (_secondaryLampRenderers != null && _secondaryLampRenderers.Length > 0)
            {
                _lampDriver.ApplyUniform(
                    _secondaryLampRenderers,
                    _tuning.LampOffEmission,
                    _tuning.LampOnEmission * 0.85f);
            }
        }
    }
}
