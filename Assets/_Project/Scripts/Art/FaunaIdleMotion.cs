using System;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Procedural idle and move motion component for the four 0.6 machine fauna species
    /// (<see cref="FaunaSpecies.PanelMite"/>, <see cref="FaunaSpecies.Sweeper"/>, <see cref="FaunaSpecies.SurveyDrone"/>,
    /// and <see cref="FaunaSpecies.Trencher"/>). Pure visual component in the <c>Art</c> domain: driven by <see cref="Speed"/>
    /// (or optional transform displacement), modulates the single <c>Lamp</c> child via <c>_EmissionStrength</c> on a
    /// pre-allocated <see cref="MaterialPropertyBlock"/> without overwriting <c>_EmissionColor</c>, and performs zero
    /// GC allocations per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FaunaIdleMotion : MonoBehaviour
    {
        private static readonly int EmissionStrengthPropertyId = Shader.PropertyToID("_EmissionStrength");

        [Header("Species & Tuning")]
        [Tooltip("Which 0.6 machine fauna species this component drives.")]
        [SerializeField] private FaunaSpecies _species;

        [Tooltip("Shared ScriptableObject tuning asset for all four 0.6 machine fauna species.")]
        [SerializeField] private FaunaMotionTuning _tuning;

        [Header("Motion Input")]
        [Tooltip("Current travel speed in metres per second. Set by Gameplay/AI or derived from transform delta.")]
        [Min(0f)]
        [SerializeField] private float _speed;

        [Tooltip("Time phase offset in seconds so multiple machines in a line or flock do not move in lockstep.")]
        [Range(0f, 100f)]
        [SerializeField] private float _phaseOffsetSeconds;

        [Tooltip("When enabled, Speed is automatically measured from world-space Transform displacement each Update.")]
        [SerializeField] private bool _deriveSpeedFromTransformDelta;

        [Header("Shared Hierarchy Pivots")]
        [Tooltip("Main chassis/carapace Body pivot transform.")]
        [SerializeField] private Transform _body;

        [Tooltip("Renderer on the machine's single status/work Lamp child (driven via _EmissionStrength).")]
        [SerializeField] private Renderer _lampRenderer;

        [Header("Panel Mite Pivots")]
        [Tooltip("Front-left leg pivot (Leg_L0, tripod group A).")]
        [SerializeField] private Transform _legL0;

        [Tooltip("Middle-left leg pivot (Leg_L1, tripod group B).")]
        [SerializeField] private Transform _legL1;

        [Tooltip("Rear-left leg pivot (Leg_L2, tripod group A).")]
        [SerializeField] private Transform _legL2;

        [Tooltip("Front-right leg pivot (Leg_R0, tripod group B).")]
        [SerializeField] private Transform _legR0;

        [Tooltip("Middle-right leg pivot (Leg_R1, tripod group A).")]
        [SerializeField] private Transform _legR1;

        [Tooltip("Rear-right leg pivot (Leg_R2, tripod group B).")]
        [SerializeField] private Transform _legR2;

        [Tooltip("Front electrostatic wiper bar pivot (Wiper).")]
        [SerializeField] private Transform _wiper;

        [Header("Sweeper & Trencher Wheel / Roller Pivots")]
        [Tooltip("Front-left wheel or track sprocket pivot (Wheel_FL or Roller_FL).")]
        [SerializeField] private Transform _wheelFL;

        [Tooltip("Front-right wheel or track sprocket pivot (Wheel_FR or Roller_FR).")]
        [SerializeField] private Transform _wheelFR;

        [Tooltip("Rear-left wheel or track sprocket pivot (Wheel_RL or Roller_RL).")]
        [SerializeField] private Transform _wheelRL;

        [Tooltip("Rear-right wheel or track sprocket pivot (Wheel_RR or Roller_RR).")]
        [SerializeField] private Transform _wheelRR;

        [Header("Sweeper Pivots")]
        [Tooltip("Articulated front brush arm pivot (BrushArm).")]
        [SerializeField] private Transform _brushArm;

        [Tooltip("Wide rotary cylindrical brush pivot (Brush).")]
        [SerializeField] private Transform _brush;

        [Header("Survey Drone Pivots")]
        [Tooltip("Underbelly optical scanner turret pivot (Scanner).")]
        [SerializeField] private Transform _scanner;

        [Tooltip("Front-left cold-gas thruster nozzle pivot (Nozzle_FL).")]
        [SerializeField] private Transform _nozzleFL;

        [Tooltip("Front-right cold-gas thruster nozzle pivot (Nozzle_FR).")]
        [SerializeField] private Transform _nozzleFR;

        [Tooltip("Rear-left cold-gas thruster nozzle pivot (Nozzle_RL).")]
        [SerializeField] private Transform _nozzleRL;

        [Tooltip("Rear-right cold-gas thruster nozzle pivot (Nozzle_RR).")]
        [SerializeField] private Transform _nozzleRR;

        [Header("Trencher Pivots")]
        [Tooltip("Forward cutter boom pivot (TrenchBoom).")]
        [SerializeField] private Transform _trenchBoom;

        [Tooltip("Toothed excavating wheel pivot (TrenchWheel).")]
        [SerializeField] private Transform _trenchWheel;

        [Tooltip("Rear cable-laying spool drum pivot (CableSpool).")]
        [SerializeField] private Transform _cableSpool;

        private MaterialPropertyBlock _lampPropertyBlock;
        private bool _restPoseCached;
        private float _elapsedTime;
        private float _travelDistance;
        private float _lastAppliedEmissionStrength = -1f;
        private Vector3 _previousWorldPosition;
        private bool _hasPreviousWorldPosition;

        private Vector3 _bodyRestLocalPosition;
        private Quaternion _bodyRestLocalRotation;
        private Quaternion _legL0RestRotation;
        private Quaternion _legL1RestRotation;
        private Quaternion _legL2RestRotation;
        private Quaternion _legR0RestRotation;
        private Quaternion _legR1RestRotation;
        private Quaternion _legR2RestRotation;
        private Quaternion _wiperRestRotation;
        private Quaternion _wheelFLRestRotation;
        private Quaternion _wheelFRRestRotation;
        private Quaternion _wheelRLRestRotation;
        private Quaternion _wheelRRRestRotation;
        private Quaternion _brushArmRestRotation;
        private Quaternion _brushRestRotation;
        private Quaternion _scannerRestRotation;
        private Quaternion _nozzleFLRestRotation;
        private Quaternion _nozzleFRRestRotation;
        private Quaternion _nozzleRLRestRotation;
        private Quaternion _nozzleRRRestRotation;
        private Quaternion _trenchBoomRestRotation;
        private Quaternion _trenchWheelRestRotation;
        private Quaternion _cableSpoolRestRotation;

        /// <summary>Which 0.6 machine fauna species this component drives.</summary>
        public FaunaSpecies Species => _species;

        /// <summary>Shared tuning asset governing the procedural motion loop.</summary>
        public FaunaMotionTuning Tuning => _tuning;

        /// <summary>Current travel speed in metres per second (clamped to &gt;= 0).</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }

        /// <summary>Time phase offset in seconds used to desynchronize groups of mites or drones.</summary>
        public float PhaseOffsetSeconds
        {
            get => _phaseOffsetSeconds;
            set => _phaseOffsetSeconds = Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>Whether <see cref="Speed"/> is automatically computed from world-space transform motion.</summary>
        public bool DeriveSpeedFromTransformDelta
        {
            get => _deriveSpeedFromTransformDelta;
            set => _deriveSpeedFromTransformDelta = value;
        }

        /// <summary>Main chassis/carapace <c>Body</c> pivot transform.</summary>
        public Transform Body => _body;

        /// <summary>Renderer on the single <c>Lamp</c> child.</summary>
        public Renderer LampRenderer => _lampRenderer;

        /// <summary>Most recent linear <c>_EmissionStrength</c> applied to <see cref="LampRenderer"/>.</summary>
        public float CurrentLampEmissionStrength => _lastAppliedEmissionStrength;

        /// <summary> Cumulative distance (m) integrated by <see cref="Step"/>.</summary>
        public float TravelDistance => _travelDistance;

        private void Awake()
        {
            ValidateOrThrow();
            CacheRestPose();
            Step(0f);
        }

        private void OnEnable()
        {
            _previousWorldPosition = transform.position;
            _hasPreviousWorldPosition = true;
        }

        private void OnValidate()
        {
            _speed = Mathf.Max(0f, _speed);
            _phaseOffsetSeconds = Mathf.Clamp(_phaseOffsetSeconds, 0f, 100f);

            // In the Editor, OnValidate also runs when adding or inspecting the component; only throw when
            // already configured or in play mode, matching the project's SerializeField validation contract.
            if (Application.isPlaying || _tuning != null)
            {
                ValidateOrThrow();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_deriveSpeedFromTransformDelta && dt > 1e-6f)
            {
                Vector3 currentPos = transform.position;
                if (_hasPreviousWorldPosition)
                {
                    float deltaDist = Vector3.Distance(currentPos, _previousWorldPosition);
                    _speed = deltaDist / dt;
                }

                _previousWorldPosition = currentPos;
                _hasPreviousWorldPosition = true;
            }

            Step(dt);
        }

        /// <summary>
        /// Advances the procedural motion clock by <paramref name="deltaTime"/> seconds, integrates travel distance
        /// from <see cref="Speed"/>, evaluates <see cref="FaunaMotionTuning.EvaluatePose"/>, and applies the resulting
        /// pose to all child pivots and the <c>Lamp</c> property block with zero GC allocations.
        /// </summary>
        public void Step(float deltaTime)
        {
            if (!_restPoseCached)
            {
                CacheRestPose();
            }

            float clampedDt = Mathf.Max(0f, deltaTime);
            float clampedSpeed = Mathf.Max(0f, _speed);
            _elapsedTime += clampedDt;
            _travelDistance += clampedSpeed * clampedDt;

            FaunaMotionPose pose = _tuning.EvaluatePose(
                _species,
                _elapsedTime + _phaseOffsetSeconds,
                _travelDistance,
                clampedSpeed);

            ApplyPose(in pose);
        }

        /// <summary>
        /// Applies a pre-evaluated <see cref="FaunaMotionPose"/> directly to the hierarchy pivots and lamp renderer.
        /// Performs zero GC allocations.
        /// </summary>
        public void ApplyPose(in FaunaMotionPose pose)
        {
            if (!_restPoseCached)
            {
                CacheRestPose();
            }

            _body.localPosition = _bodyRestLocalPosition + pose.BodyOffset;
            _body.localRotation = Quaternion.Euler(pose.BodyEuler.x, pose.BodyEuler.y, pose.BodyEuler.z) * _bodyRestLocalRotation;

            switch (_species)
            {
                case FaunaSpecies.PanelMite:
                    ApplyPanelMitePose(in pose);
                    break;
                case FaunaSpecies.Sweeper:
                    ApplySweeperPose(in pose);
                    break;
                case FaunaSpecies.SurveyDrone:
                    ApplySurveyDronePose(in pose);
                    break;
                case FaunaSpecies.Trencher:
                    ApplyTrencherPose(in pose);
                    break;
            }

            ApplyLampEmissionStrength(pose.LampEmissionStrength);
        }

        /// <summary>
        /// Captures the current local positions and rotations of all assigned pivots as the rest pose.
        /// </summary>
        public void CacheRestPose()
        {
            if (_body != null)
            {
                _bodyRestLocalPosition = _body.localPosition;
                _bodyRestLocalRotation = _body.localRotation;
            }

            if (_legL0 != null) _legL0RestRotation = _legL0.localRotation;
            if (_legL1 != null) _legL1RestRotation = _legL1.localRotation;
            if (_legL2 != null) _legL2RestRotation = _legL2.localRotation;
            if (_legR0 != null) _legR0RestRotation = _legR0.localRotation;
            if (_legR1 != null) _legR1RestRotation = _legR1.localRotation;
            if (_legR2 != null) _legR2RestRotation = _legR2.localRotation;
            if (_wiper != null) _wiperRestRotation = _wiper.localRotation;

            if (_wheelFL != null) _wheelFLRestRotation = _wheelFL.localRotation;
            if (_wheelFR != null) _wheelFRRestRotation = _wheelFR.localRotation;
            if (_wheelRL != null) _wheelRLRestRotation = _wheelRL.localRotation;
            if (_wheelRR != null) _wheelRRRestRotation = _wheelRR.localRotation;

            if (_brushArm != null) _brushArmRestRotation = _brushArm.localRotation;
            if (_brush != null) _brushRestRotation = _brush.localRotation;

            if (_scanner != null) _scannerRestRotation = _scanner.localRotation;
            if (_nozzleFL != null) _nozzleFLRestRotation = _nozzleFL.localRotation;
            if (_nozzleFR != null) _nozzleFRRestRotation = _nozzleFR.localRotation;
            if (_nozzleRL != null) _nozzleRLRestRotation = _nozzleRL.localRotation;
            if (_nozzleRR != null) _nozzleRRRestRotation = _nozzleRR.localRotation;

            if (_trenchBoom != null) _trenchBoomRestRotation = _trenchBoom.localRotation;
            if (_trenchWheel != null) _trenchWheelRestRotation = _trenchWheel.localRotation;
            if (_cableSpool != null) _cableSpoolRestRotation = _cableSpool.localRotation;

            if (_lampPropertyBlock == null)
            {
                _lampPropertyBlock = new MaterialPropertyBlock();
            }

            _restPoseCached = true;
        }

        /// <summary>
        /// Auto-wires all serialized child pivot references by name from this GameObject's hierarchy for
        /// <paramref name="species"/> and assigns <paramref name="tuning"/>, then validates every required reference.
        /// Used by <c>R1FaunaBuilder</c> when constructing prefabs.
        /// </summary>
        public void ConfigureFromHierarchy(FaunaSpecies species, FaunaMotionTuning tuning)
        {
            _species = species;
            _tuning = tuning;

            Transform root = transform;
            _body = FindChildRecursive(root, "Body");
            Transform lampTransform = FindChildRecursive(root, "Lamp");
            _lampRenderer = lampTransform != null ? lampTransform.GetComponent<Renderer>() : null;

            switch (species)
            {
                case FaunaSpecies.PanelMite:
                    _legL0 = FindChildRecursive(root, "Leg_L0");
                    _legL1 = FindChildRecursive(root, "Leg_L1");
                    _legL2 = FindChildRecursive(root, "Leg_L2");
                    _legR0 = FindChildRecursive(root, "Leg_R0");
                    _legR1 = FindChildRecursive(root, "Leg_R1");
                    _legR2 = FindChildRecursive(root, "Leg_R2");
                    _wiper = FindChildRecursive(root, "Wiper");
                    break;

                case FaunaSpecies.Sweeper:
                    _wheelFL = FindChildRecursive(root, "Wheel_FL");
                    _wheelFR = FindChildRecursive(root, "Wheel_FR");
                    _wheelRL = FindChildRecursive(root, "Wheel_RL");
                    _wheelRR = FindChildRecursive(root, "Wheel_RR");
                    _brushArm = FindChildRecursive(root, "BrushArm");
                    _brush = FindChildRecursive(root, "Brush");
                    break;

                case FaunaSpecies.SurveyDrone:
                    _scanner = FindChildRecursive(root, "Scanner");
                    _nozzleFL = FindChildRecursive(root, "Nozzle_FL");
                    _nozzleFR = FindChildRecursive(root, "Nozzle_FR");
                    _nozzleRL = FindChildRecursive(root, "Nozzle_RL");
                    _nozzleRR = FindChildRecursive(root, "Nozzle_RR");
                    break;

                case FaunaSpecies.Trencher:
                    _wheelFL = FindChildRecursive(root, "Roller_FL");
                    _wheelFR = FindChildRecursive(root, "Roller_FR");
                    _wheelRL = FindChildRecursive(root, "Roller_RL");
                    _wheelRR = FindChildRecursive(root, "Roller_RR");
                    _trenchBoom = FindChildRecursive(root, "TrenchBoom");
                    _trenchWheel = FindChildRecursive(root, "TrenchWheel");
                    _cableSpool = FindChildRecursive(root, "CableSpool");
                    break;
            }

            _restPoseCached = false;
            ValidateOrThrow();
            CacheRestPose();
        }

        /// <summary>
        /// Validates all required serialized references for the configured <see cref="Species"/> and throws
        /// <see cref="InvalidOperationException"/> naming the missing field and GameObject if any reference is null.
        /// </summary>
        public void ValidateOrThrow()
        {
            RequireReference(_tuning, nameof(_tuning));
            RequireReference(_body, nameof(_body));
            RequireReference(_lampRenderer, nameof(_lampRenderer));

            switch (_species)
            {
                case FaunaSpecies.PanelMite:
                    RequireReference(_legL0, nameof(_legL0));
                    RequireReference(_legL1, nameof(_legL1));
                    RequireReference(_legL2, nameof(_legL2));
                    RequireReference(_legR0, nameof(_legR0));
                    RequireReference(_legR1, nameof(_legR1));
                    RequireReference(_legR2, nameof(_legR2));
                    RequireReference(_wiper, nameof(_wiper));
                    break;

                case FaunaSpecies.Sweeper:
                    RequireReference(_wheelFL, nameof(_wheelFL));
                    RequireReference(_wheelFR, nameof(_wheelFR));
                    RequireReference(_wheelRL, nameof(_wheelRL));
                    RequireReference(_wheelRR, nameof(_wheelRR));
                    RequireReference(_brushArm, nameof(_brushArm));
                    RequireReference(_brush, nameof(_brush));
                    break;

                case FaunaSpecies.SurveyDrone:
                    RequireReference(_scanner, nameof(_scanner));
                    RequireReference(_nozzleFL, nameof(_nozzleFL));
                    RequireReference(_nozzleFR, nameof(_nozzleFR));
                    RequireReference(_nozzleRL, nameof(_nozzleRL));
                    RequireReference(_nozzleRR, nameof(_nozzleRR));
                    break;

                case FaunaSpecies.Trencher:
                    RequireReference(_wheelFL, nameof(_wheelFL));
                    RequireReference(_wheelFR, nameof(_wheelFR));
                    RequireReference(_wheelRL, nameof(_wheelRL));
                    RequireReference(_wheelRR, nameof(_wheelRR));
                    RequireReference(_trenchBoom, nameof(_trenchBoom));
                    RequireReference(_trenchWheel, nameof(_trenchWheel));
                    RequireReference(_cableSpool, nameof(_cableSpool));
                    break;
            }
        }

        private void ApplyPanelMitePose(in FaunaMotionPose pose)
        {
            Quaternion leftRotA = Quaternion.Euler(0f, -pose.LegSwingADeg, -pose.LegLiftADeg);
            Quaternion rightRotA = Quaternion.Euler(0f, pose.LegSwingADeg, pose.LegLiftADeg);
            Quaternion leftRotB = Quaternion.Euler(0f, -pose.LegSwingBDeg, -pose.LegLiftBDeg);
            Quaternion rightRotB = Quaternion.Euler(0f, pose.LegSwingBDeg, pose.LegLiftBDeg);

            _legL0.localRotation = leftRotA * _legL0RestRotation;
            _legR1.localRotation = rightRotA * _legR1RestRotation;
            _legL2.localRotation = leftRotA * _legL2RestRotation;

            _legR0.localRotation = rightRotB * _legR0RestRotation;
            _legL1.localRotation = leftRotB * _legL1RestRotation;
            _legR2.localRotation = rightRotB * _legR2RestRotation;

            _wiper.localRotation = Quaternion.Euler(0f, pose.WiperYawDeg, 0f) * _wiperRestRotation;
        }

        private void ApplySweeperPose(in FaunaMotionPose pose)
        {
            Quaternion wheelSpin = Quaternion.Euler(pose.WheelSpinDeg, 0f, 0f);
            _wheelFL.localRotation = wheelSpin * _wheelFLRestRotation;
            _wheelFR.localRotation = wheelSpin * _wheelFRRestRotation;
            _wheelRL.localRotation = wheelSpin * _wheelRLRestRotation;
            _wheelRR.localRotation = wheelSpin * _wheelRRRestRotation;

            _brushArm.localRotation = Quaternion.Euler(pose.BrushArmPitchDeg, 0f, 0f) * _brushArmRestRotation;
            _brush.localRotation = Quaternion.Euler(pose.BrushSpinDeg, 0f, 0f) * _brushRestRotation;
        }

        private void ApplySurveyDronePose(in FaunaMotionPose pose)
        {
            _scanner.localRotation = Quaternion.Euler(0f, 0f, pose.ScannerYawDeg) * _scannerRestRotation;
            _nozzleFL.localRotation = Quaternion.Euler(pose.NozzleGimbalFL.x, pose.NozzleGimbalFL.z, pose.NozzleGimbalFL.y) * _nozzleFLRestRotation;
            _nozzleFR.localRotation = Quaternion.Euler(pose.NozzleGimbalFR.x, pose.NozzleGimbalFR.z, pose.NozzleGimbalFR.y) * _nozzleFRRestRotation;
            _nozzleRL.localRotation = Quaternion.Euler(pose.NozzleGimbalRL.x, pose.NozzleGimbalRL.z, pose.NozzleGimbalRL.y) * _nozzleRLRestRotation;
            _nozzleRR.localRotation = Quaternion.Euler(pose.NozzleGimbalRR.x, pose.NozzleGimbalRR.z, pose.NozzleGimbalRR.y) * _nozzleRRRestRotation;
        }

        private void ApplyTrencherPose(in FaunaMotionPose pose)
        {
            Quaternion rollerSpin = Quaternion.Euler(pose.WheelSpinDeg, 0f, 0f);
            _wheelFL.localRotation = rollerSpin * _wheelFLRestRotation;
            _wheelFR.localRotation = rollerSpin * _wheelFRRestRotation;
            _wheelRL.localRotation = rollerSpin * _wheelRLRestRotation;
            _wheelRR.localRotation = rollerSpin * _wheelRRRestRotation;

            _trenchBoom.localRotation = Quaternion.Euler(pose.TrenchBoomPitchDeg, 0f, 0f) * _trenchBoomRestRotation;
            _trenchWheel.localRotation = Quaternion.Euler(pose.TrenchWheelSpinDeg, 0f, 0f) * _trenchWheelRestRotation;
            _cableSpool.localRotation = Quaternion.Euler(pose.CableSpoolSpinDeg, 0f, 0f) * _cableSpoolRestRotation;
        }

        private void ApplyLampEmissionStrength(float emissionStrength)
        {
            if (Mathf.Abs(emissionStrength - _lastAppliedEmissionStrength) < 1e-5f)
            {
                return;
            }

            _lastAppliedEmissionStrength = emissionStrength;
            _lampRenderer.GetPropertyBlock(_lampPropertyBlock);
            _lampPropertyBlock.SetFloat(EmissionStrengthPropertyId, emissionStrength);
            _lampRenderer.SetPropertyBlock(_lampPropertyBlock);
        }

        private void RequireReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference == null)
            {
                throw new InvalidOperationException(
                    $"FaunaIdleMotion on GameObject '{gameObject.name}' (species '{_species}') is missing required reference '{fieldName}'.");
            }
        }

        private static Transform FindChildRecursive(Transform parent, string targetName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, targetName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, targetName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
