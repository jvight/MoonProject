using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Moves the art box's RoverModel on top of the physics sphere. Hierarchy (built by the Rover builder):
    /// <c>Visual (this: follows the sphere, heading + ground alignment) / Chassis (jelly lean + bob) / RoverModel</c>.
    /// The 4 wheels ray-cast the ground every frame from the leaned chassis, so they stay planted while the body squashes,
    /// leans and bobs above them; the 4 trailing suspension arms pitch around local X and the 4 shock struts compress
    /// along local Y to follow each wheel. Neck, NeckLift, Head, Eye, CutterTurret, DigScoop, WinchSpool and SolarWing
    /// belong to <see cref="RoverBodyLanguage"/>. Ticked by <see cref="RoverController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverVisualRig : MonoBehaviour
    {
        private const float ShockRestLength = 0.30f;
        private static readonly int DustAmountId = Shader.PropertyToID("_DustAmount");

        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("Child between this transform and RoverModel; carries the jelly lean and bob.")]
        [SerializeField] private Transform _chassis;

        [Tooltip("RoverModel wheels in rig order: Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR.")]
        [SerializeField] private Transform[] _wheels = new Transform[RoverModelNodes.WheelCount];

        [Tooltip("RoverModel trailing suspension arms in rig order: Arm_FL, Arm_FR, Arm_RL, Arm_RR (pitch = local X).")]
        [SerializeField] private Transform[] _arms = new Transform[RoverModelNodes.WheelCount];

        [Tooltip("RoverModel shock struts in rig order: Shock_FL, Shock_FR, Shock_RL, Shock_RR (compress = local Y).")]
        [SerializeField] private Transform[] _shocks = new Transform[RoverModelNodes.WheelCount];

        [Tooltip("RoverModel 'Antenna' node (pivot at its base).")]
        [SerializeField] private Transform _antenna;

        private RoverController _rover;
        private BodyAttitude _attitude;
        private ChassisJelly _jelly;
        private Vector3[] _wheelRestPositions;
        private Quaternion[] _wheelRestRotations;
        private float[] _wheelRadii;
        private float[] _wheelSteerFactors;
        private float[] _wheelTargets;
        private float[] _wheelSpin;
        private DampedSpring[] _wheelSuspension;
        private Vector3[] _armRestPositions;
        private Quaternion[] _armRestRotations;
        private float[] _heldArmPitch;
        private Vector3[] _shockRestScales;
        private Vector3[] _contacts;
        private Vector3 _chassisRestPosition;
        private Quaternion _chassisRestRotation;
        private Quaternion _antennaRestRotation;
        private ImpactNudge _nudge;
        private ChargeBrace _brace;
        private float _sag;
        private float _sagTarget;
        private float _sagHalfLife = 1f;
        private Vector3 _antennaRestPosition;
        private float _antennaLift;
        private Vector3 _lastPosition;
        private Renderer[] _lowerBodyRenderers;
        private MaterialPropertyBlock _dustBlock;
        private float _dustLevel;
        private float _appliedDustAmount = -1f;

        public Vector3 RootPosition => transform.position;

        public Quaternion RootRotation => transform.rotation;

        /// <summary>Normalized lower-body regolith dust accumulation [0..1] from driving; brushes off while docked.</summary>
        public float DustLevel => _dustLevel;

        /// <summary>Current <c>_DustAmount</c> applied to 07's lower-body renderers (wheels, arms, shock struts).</summary>
        public float AppliedDustAmount => _appliedDustAmount >= 0f ? _appliedDustAmount : (_tuning != null ? _tuning.DustMinAmount : 0f);

        /// <summary>Validates the wiring and snaps the model onto the sphere; false (and logged) when broken.</summary>
        public bool Initialize(RoverController rover)
        {
            _rover = rover;
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _attitude = new BodyAttitude(_tuning);
            _jelly = new ChassisJelly(_tuning);
            CacheRestPose();
            Snap(Vector3.up);
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverRigTuning is not assigned.")
                & Require(_chassis != null, "Chassis transform is not assigned.")
                & Require(_antenna != null, "Antenna node is not assigned.")
                & Require(_wheels != null && _wheels.Length == RoverModelNodes.WheelCount,
                    $"Exactly {RoverModelNodes.WheelCount} wheels are required.")
                & Require(_arms != null && _arms.Length == RoverModelNodes.WheelCount,
                    $"Exactly {RoverModelNodes.WheelCount} suspension arms are required.")
                & Require(_shocks != null && _shocks.Length == RoverModelNodes.WheelCount,
                    $"Exactly {RoverModelNodes.WheelCount} shock struts are required.");
            if (_wheels != null)
            {
                for (int i = 0; i < _wheels.Length; i++)
                {
                    ok &= Require(_wheels[i] != null, $"Wheel {i} ({SafeWheelName(i)}) is not assigned.");
                }
            }

            if (_arms != null)
            {
                for (int i = 0; i < _arms.Length; i++)
                {
                    ok &= Require(_arms[i] != null, $"Arm {i} ({SafeArmName(i)}) is not assigned.");
                }
            }

            if (_shocks != null)
            {
                for (int i = 0; i < _shocks.Length; i++)
                {
                    ok &= Require(_shocks[i] != null, $"Shock {i} ({SafeShockName(i)}) is not assigned.");
                }
            }

            return ok;
        }

        private static string SafeWheelName(int index)
        {
            return index < RoverModelNodes.WheelCount ? RoverModelNodes.Wheel(index) : "extra";
        }

        private static string SafeArmName(int index)
        {
            return index < RoverModelNodes.WheelCount ? RoverModelNodes.Arm(index) : "extra";
        }

        private static string SafeShockName(int index)
        {
            return index < RoverModelNodes.WheelCount ? RoverModelNodes.Shock(index) : "extra";
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverVisualRig)}: {message}", this);
            }

            return condition;
        }

        private void CacheRestPose()
        {
            int count = RoverModelNodes.WheelCount;
            _wheelRestPositions = new Vector3[count];
            _wheelRestRotations = new Quaternion[count];
            _wheelRadii = new float[count];
            _wheelSteerFactors = new float[count];
            _wheelTargets = new float[count];
            _wheelSpin = new float[count];
            _wheelSuspension = new DampedSpring[count];
            _armRestPositions = new Vector3[count];
            _armRestRotations = new Quaternion[count];
            _heldArmPitch = new float[count];
            _shockRestScales = new Vector3[count];
            _contacts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                _wheelRestPositions[i] = _wheels[i].localPosition;
                _wheelRestRotations[i] = _wheels[i].localRotation;
                _wheelRadii[i] = _tuning.WheelRadius;
                int row = RoverModelNodes.WheelRow(i);
                _wheelSteerFactors[i] = row == 0 ? 1f : -_tuning.RearSteerFactor;
                _armRestPositions[i] = _arms[i].localPosition;
                _armRestRotations[i] = _arms[i].localRotation;
                _shockRestScales[i] = _shocks[i].localScale;
            }

            _chassisRestPosition = _chassis.localPosition;
            _chassisRestRotation = _chassis.localRotation;
            _antennaRestRotation = _antenna.localRotation;
            _antennaRestPosition = _antenna.localPosition;
            CacheLowerBodyRenderers();
        }

        private void CacheLowerBodyRenderers()
        {
            var seen = new HashSet<Renderer>();
            var list = new List<Renderer>(16);
            CollectRenderers(_wheels, seen, list);
            CollectRenderers(_arms, seen, list);
            CollectRenderers(_shocks, seen, list);
            for (int i = 0; i < _wheels.Length; i++)
            {
                Transform parent = _wheels[i] != null ? _wheels[i].parent : null;
                if (parent != null && parent != _chassis && parent.TryGetComponent(out Renderer parentRenderer)
                    && seen.Add(parentRenderer))
                {
                    list.Add(parentRenderer);
                }
            }

            _lowerBodyRenderers = list.ToArray();
            _dustBlock = new MaterialPropertyBlock();
            _dustLevel = 0f;
            _appliedDustAmount = -1f;
            ApplyLowerBodyDust();
        }

        private static void CollectRenderers(Transform[] roots, HashSet<Renderer> seen, List<Renderer> list)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                Renderer[] childRenderers = roots[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < childRenderers.Length; j++)
                {
                    Renderer r = childRenderers[j];
                    if (r != null && seen.Add(r))
                    {
                        list.Add(r);
                    }
                }
            }
        }

        /// <summary>
        /// Places the model on the sphere, tilted to the ground of <paramref name="groundNormal"/> (world space), with
        /// every spring at rest (spawn, placement).
        /// </summary>
        public void Snap(Vector3 groundNormal)
        {
            Quaternion heading = _rover.HeadingRotation;
            _attitude.ResetTo(Quaternion.Inverse(heading) * groundNormal);
            _jelly.Reset();
            Quaternion rotation = heading * GroundPlaneFit.Tilt(_attitude.Pitch, _attitude.Roll);
            Vector3 position = _rover.SpherePosition - rotation * Vector3.up * _rover.SphereRadius;
            transform.SetPositionAndRotation(position, rotation);
            _lastPosition = position;
            for (int i = 0; i < _wheelSuspension.Length; i++)
            {
                _wheelSuspension[i].Reset(0f);
                _wheelTargets[i] = 0f;
            }

            ApplyChassis();
            ApplyWheels(0f, 0f);
            ApplySuspensionLinks();
        }

        /// <summary>A quick antenna wiggle (deg/s), e.g. when 07 perks up.</summary>
        public void KickAntenna(float degreesPerSecond)
        {
            _jelly.KickAntenna(degreesPerSecond);
        }

        /// <summary>Raises the antenna <paramref name="metres"/> along its own axis (07 listening); 0 rests it.</summary>
        public void SetAntennaLift(float metres)
        {
            _antennaLift = metres;
        }

        /// <summary>An extra chassis squash (m/s downward when negative), e.g. a hard-landing "oof".</summary>
        public void KickHeave(float metresPerSecond)
        {
            _jelly.KickHeave(metresPerSecond);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Quaternion heading = _rover.HeadingRotation;
            Vector3 sphere = _rover.SpherePosition;
            float radius = _rover.SphereRadius;

            // Ray-cast from last frame's attitude at the new position; the springs absorb the one-frame lag.
            Quaternion rotation = heading * GroundPlaneFit.Tilt(_attitude.Pitch, _attitude.Roll);
            transform.SetPositionAndRotation(sphere - rotation * Vector3.up * radius, rotation);
            int contacts = CastWheels(heading);

            if (_rover.IsGrounded)
            {
                Vector3 localNormal = GroundPlaneFit.TryFit(_contacts, contacts, out Vector3 fitted)
                    ? fitted
                    : Quaternion.Inverse(heading) * _rover.GroundNormal;
                _attitude.StepGrounded(localNormal, deltaTime);
            }
            else
            {
                _attitude.StepAirborne(_rover.Velocity.y, _rover.ForwardSpeed, deltaTime);
            }

            rotation = heading * GroundPlaneFit.Tilt(_attitude.Pitch, _attitude.Roll);
            Vector3 position = sphere - rotation * Vector3.up * radius;
            Vector3 deltaPos = position - _lastPosition;
            float travelled = Vector3.Dot(deltaPos, rotation * Vector3.forward);
            float planarMeters = new Vector2(deltaPos.x, deltaPos.z).magnitude;
            _lastPosition = position;
            transform.SetPositionAndRotation(position, rotation);

            _jelly.Step(_rover.LocalAcceleration, deltaTime);
            _sag = Smoothing.Damp(_sag, _sagTarget, _sagHalfLife, deltaTime);
            ApplyChassis();
            StepSuspension(deltaTime);
            ApplyWheels(travelled, deltaTime);
            ApplySuspensionLinks();
            StepLowerBodyDust(planarMeters, deltaTime);
            _antenna.localRotation =
                GroundPlaneFit.Tilt(_jelly.AntennaPitch, _jelly.AntennaRoll) * _antennaRestRotation;
            _antenna.localPosition = _antennaRestPosition + _antennaRestRotation * Vector3.up * _antennaLift;
        }

        private void StepLowerBodyDust(float planarMeters, float deltaTime)
        {
            if (_rover.IsDocked)
            {
                _dustLevel = Smoothing.Damp(_dustLevel, 0f, _tuning.DustCleanHalfLife, deltaTime);
                if (_dustLevel < 1e-4f)
                {
                    _dustLevel = 0f;
                }
            }
            else if (_rover.IsGrounded && planarMeters > 0f)
            {
                _dustLevel = Mathf.Clamp01(_dustLevel + planarMeters / _tuning.DustFullMeters);
            }

            ApplyLowerBodyDust();
        }

        private void ApplyLowerBodyDust()
        {
            if (_lowerBodyRenderers == null || _lowerBodyRenderers.Length == 0 || _dustBlock == null)
            {
                return;
            }

            float targetAmount = Mathf.Lerp(_tuning.DustMinAmount, _tuning.DustMaxAmount, _dustLevel);
            if (Mathf.Abs(targetAmount - _appliedDustAmount) <= 1e-4f)
            {
                return;
            }

            _appliedDustAmount = targetAmount;
            _dustBlock.Clear();
            _dustBlock.SetFloat(DustAmountId, targetAmount);
            for (int i = 0; i < _lowerBodyRenderers.Length; i++)
            {
                _lowerBodyRenderers[i].SetPropertyBlock(_dustBlock);
            }
        }

        private int CastWheels(Quaternion heading)
        {
            float castHeight = _tuning.SuspensionCastHeight;
            float droop = _tuning.SuspensionDroop;
            Quaternion toHeading = Quaternion.Inverse(heading);
            Vector3 root = transform.position;
            int count = 0;

            for (int i = 0; i < _wheels.Length; i++)
            {
                float wheelRadius = _wheelRadii[i];
                float length = castHeight + wheelRadius + droop;
                Transform parent = _wheels[i].parent;
                Vector3 origin = parent.TransformPoint(_wheelRestPositions[i] + Vector3.up * castHeight);
                if (Physics.Raycast(origin, -parent.up, out RaycastHit hit, length, Layers.DriveableMask,
                        QueryTriggerInteraction.Ignore))
                {
                    _wheelTargets[i] = Mathf.Clamp(castHeight + wheelRadius - hit.distance, -droop,
                        _tuning.SuspensionCompression);
                    _contacts[count++] = toHeading * (hit.point - root);
                }
                else
                {
                    _wheelTargets[i] = -droop;
                }
            }

            return count;
        }

        private void StepSuspension(float deltaTime)
        {
            for (int i = 0; i < _wheelSuspension.Length; i++)
            {
                _wheelSuspension[i].Step(_wheelTargets[i], _tuning.SuspensionFrequency, _tuning.SuspensionDamping,
                    deltaTime);
                _wheelSuspension[i].Clamp(-_tuning.SuspensionDroop, _tuning.SuspensionCompression);
            }
        }

        /// <summary>The rig's tuning (the feel kit reads its cut settings).</summary>
        public RoverRigTuning Tuning => _tuning;

        /// <summary>
        /// The feel kit's chassis response (<see cref="ImpactNudge"/>: recoil, dips, leans), added on top of the
        /// springs' pose; null leaves the chassis to the springs alone.
        /// </summary>
        public void SetNudge(ImpactNudge nudge)
        {
            _nudge = nudge;
        }

        /// <summary>
        /// The cut's charge-up brace (<see cref="ChargeBrace"/>): the chassis squats and settles, the wheels stop
        /// turning and the suspension links hold where they were; null leaves the body free.
        /// </summary>
        public void SetChargeBrace(ChargeBrace brace)
        {
            _brace = brace;
        }

        /// <summary>The cargo's weight: the chassis settles <paramref name="metres"/> lower over the half-life.</summary>
        public void SetLoadSag(float metres, float halfLife)
        {
            _sagTarget = metres;
            _sagHalfLife = halfLife;
        }

        private void ApplyChassis()
        {
            Vector3 pivot = Vector3.up * _tuning.LeanPivotHeight;
            float nudgePitch = _nudge != null ? _nudge.Pitch : 0f;
            Vector3 nudge = (_nudge != null ? new Vector3(0f, _nudge.Heave, _nudge.Surge) : Vector3.zero)
                + Vector3.down * _sag + Vector3.up * (_brace != null ? _brace.Heave : 0f);
            Quaternion lean = GroundPlaneFit.Tilt(_jelly.Pitch - nudgePitch, _jelly.Roll);
            _chassis.localPosition = _chassisRestPosition + pivot - lean * pivot + Vector3.up * _jelly.Heave + nudge;
            _chassis.localRotation = lean * _chassisRestRotation;
        }

        /// <summary>
        /// Each trailing arm pitches around local X to follow its wheel's vertical travel and each shock strut
        /// compresses/extends along local Y; braced for a charge, they stiffen toward the pose they had when braced.
        /// </summary>
        private void ApplySuspensionLinks()
        {
            float stiffness = _brace != null ? _brace.Stiffness : 0f;
            for (int i = 0; i < _wheels.Length; i++)
            {
                float travel = _wheelSuspension[i].Value;
                float dz = _wheelRestPositions[i].z - _armRestPositions[i].z;
                float span = Mathf.Max(Mathf.Abs(dz), 0.1f);
                float sign = dz >= 0f ? -1f : 1f;
                float pitch = sign * Mathf.Atan2(travel, span) * Mathf.Rad2Deg;
                if (stiffness <= 0f)
                {
                    _heldArmPitch[i] = pitch;
                }

                float appliedPitch = Mathf.Lerp(pitch, _heldArmPitch[i], stiffness);
                _arms[i].localRotation = _armRestRotations[i] * Quaternion.AngleAxis(appliedPitch, Vector3.right);

                float shockScaleY = Mathf.Clamp(1f - travel / ShockRestLength, 0.4f, 1.4f);
                Vector3 restScale = _shockRestScales[i];
                _shocks[i].localScale = new Vector3(restScale.x, restScale.y * shockScaleY, restScale.z);
            }
        }

        private void ApplyWheels(float travelled, float deltaTime)
        {
            float yawRadians = _rover.YawRate * Mathf.Deg2Rad * deltaTime;
            float steer = Mathf.Clamp(_rover.SteerInput + _rover.ScanTurnScrub * 0.45f, -1f, 1f)
                * _tuning.FrontSteerAngle;
            bool braced = _brace != null && _brace.IsBraced;

            for (int i = 0; i < _wheels.Length; i++)
            {
                Vector3 rest = _wheelRestPositions[i];
                float degreesPerMetre = braced ? 0f : Mathf.Rad2Deg / _wheelRadii[i];

                // Turning right moves the left wheels forward and the right wheels back (skid-steer pivot).
                float sideX = i % 2 == 0 ? -0.61f : 0.61f;
                float distance = travelled - yawRadians * sideX;
                _wheelSpin[i] = Mathf.Repeat(_wheelSpin[i] + distance * degreesPerMetre, 360f);

                Transform wheel = _wheels[i];
                wheel.localPosition = rest + Vector3.up * _wheelSuspension[i].Value;
                wheel.localRotation = Quaternion.AngleAxis(steer * _wheelSteerFactors[i], Vector3.up)
                    * _wheelRestRotations[i]
                    * Quaternion.AngleAxis(_wheelSpin[i], Vector3.right);
            }
        }
    }
}
