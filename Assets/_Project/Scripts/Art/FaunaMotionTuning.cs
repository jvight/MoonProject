using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Tuning parameters for the procedural idle and move motion loops of the four 0.6 machine fauna species
    /// (<see cref="FaunaSpecies.PanelMite"/>, <see cref="FaunaSpecies.Sweeper"/>, <see cref="FaunaSpecies.SurveyDrone"/>,
    /// and <see cref="FaunaSpecies.Trencher"/>). Pure visual tuning in the <c>Art</c> domain.
    /// </summary>
    [CreateAssetMenu(fileName = "FaunaMotionTuning", menuName = "MoonProject/Art/Fauna Motion Tuning")]
    public sealed class FaunaMotionTuning : ScriptableObject
    {
        private const float TwoPi = Mathf.PI * 2f;

        [Header("Shared Lamp Pulse")]
        [Tooltip("Base linear _EmissionStrength on each machine's single status/work Lamp while idling.")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float _lampBaseEmissionStrength = 1.0f;

        [Tooltip("Additional linear _EmissionStrength added when the machine is moving at or above its reference speed.")]
        [Range(0.0f, 1.5f)]
        [SerializeField] private float _lampMoveEmissionBoost = 0.28f;

        [Tooltip("Amplitude of the slow breathing pulse on _EmissionStrength (kept well below 3 Hz).")]
        [Range(0.0f, 0.8f)]
        [SerializeField] private float _lampBreathAmplitude = 0.14f;

        [Tooltip("Frequency (Hz) of the slow lamp breathing cycle.")]
        [Range(0.05f, 2.5f)]
        [SerializeField] private float _lampBreathFrequencyHz = 0.36f;

        [Header("Panel Mite (6-Legged Solar Wiper)")]
        [Tooltip("Reference crawling speed (m/s) at which the panel mite reaches its full tripod stride amplitude.")]
        [Min(0.05f)]
        [SerializeField] private float _miteReferenceSpeed = 0.35f;

        [Tooltip("Tripod gait cycle frequency (cycles per metre of travel) for the panel mite's 6 legs.")]
        [Range(2.0f, 20.0f)]
        [SerializeField] private float _miteCyclesPerMeter = 8.0f;

        [Tooltip("Maximum forward/back hip swing angle (deg) of each leg during a full stride.")]
        [Range(2.0f, 45.0f)]
        [SerializeField] private float _miteLegSwingAmplitudeDeg = 18.0f;

        [Tooltip("Maximum vertical step lift angle (deg) of each leg during the swing phase.")]
        [Range(1.0f, 35.0f)]
        [SerializeField] private float _miteLegLiftAmplitudeDeg = 12.0f;

        [Tooltip("Subtle idle posture breathing angle (deg) on the legs when the mite is stationary.")]
        [Range(0.0f, 8.0f)]
        [SerializeField] private float _miteIdleLegBreathDeg = 1.8f;

        [Tooltip("Yaw oscillation amplitude (deg) of the front electrostatic Wiper bar while wiping.")]
        [Range(0.0f, 25.0f)]
        [SerializeField] private float _miteWiperSweepAmplitudeDeg = 8.5f;

        [Tooltip("Oscillation frequency (Hz) of the front electrostatic Wiper bar.")]
        [Range(0.1f, 4.0f)]
        [SerializeField] private float _miteWiperFrequencyHz = 1.4f;

        [Tooltip("Vertical carapace bob amplitude (m) during the 6-legged tripod gait.")]
        [Range(0.0f, 0.02f)]
        [SerializeField] private float _miteBodyBobAmplitudeMeters = 0.0035f;

        [Header("The Sweeper (Wheeled Road Sweeper)")]
        [Tooltip("Reference travel speed (m/s) for the sweeper's full brush spin and arm nod.")]
        [Min(0.1f)]
        [SerializeField] private float _sweeperReferenceSpeed = 1.4f;

        [Tooltip("Rolling radius (m) of the sweeper's four lunar tyres (Wheel_FL..RR).")]
        [Min(0.05f)]
        [SerializeField] private float _sweeperWheelRadiusMeters = 0.28f;

        [Tooltip("Spin rate (deg/s) of the wide front rotary Brush when sweeping at reference speed.")]
        [Range(30.0f, 720.0f)]
        [SerializeField] private float _sweeperBrushSpinDegPerSecond = 260.0f;

        [Tooltip("Slow keep-alive spin rate (deg/s) of the front rotary Brush while idling.")]
        [Range(0.0f, 180.0f)]
        [SerializeField] private float _sweeperIdleBrushSpinDegPerSecond = 36.0f;

        [Tooltip("Pitch nod amplitude (deg) of the front articulated BrushArm following the regolith.")]
        [Range(0.0f, 12.0f)]
        [SerializeField] private float _sweeperBrushArmNodAmplitudeDeg = 2.6f;

        [Tooltip("Frequency (Hz) of the front BrushArm surface-following nod.")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float _sweeperBrushArmNodFrequencyHz = 0.85f;

        [Tooltip("Subtle chassis roll/rock amplitude (deg) while the sweeper trundles forward.")]
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _sweeperChassisRockAmplitudeDeg = 1.1f;

        [Header("Survey Flock Drone (Tycho Cold-Gas Hex Drone)")]
        [Tooltip("Reference horizontal cruise speed (m/s) for full forward pitch and nozzle vectoring.")]
        [Min(0.1f)]
        [SerializeField] private float _droneReferenceSpeed = 2.4f;

        [Tooltip("Mean hover altitude offset (m) added to Body.localPosition.y above its landed skid rest pose.")]
        [Range(0.0f, 1.5f)]
        [SerializeField] private float _droneHoverAltitudeMeters = 0.18f;

        [Tooltip("Vertical hover bob amplitude (m) on cold-gas thruster pulses.")]
        [Range(0.0f, 0.25f)]
        [SerializeField] private float _droneHoverBobAmplitudeMeters = 0.036f;

        [Tooltip("Frequency (Hz) of the vertical cold-gas hover bob.")]
        [Range(0.1f, 2.5f)]
        [SerializeField] private float _droneHoverBobFrequencyHz = 0.65f;

        [Tooltip("Gentle attitude sway amplitude (deg) in pitch and roll while hovering.")]
        [Range(0.0f, 15.0f)]
        [SerializeField] private float _droneHoverSwayAmplitudeDeg = 3.2f;

        [Tooltip("Forward nose-down pitch angle (deg) when cruising at reference speed.")]
        [Range(0.0f, 25.0f)]
        [SerializeField] private float _droneForwardPitchAtRefSpeedDeg = 9.5f;

        [Tooltip("Gimbal deflection amplitude (deg) of the 4 corner De Laval cold-gas nozzles (Nozzle_FL..RR).")]
        [Range(0.0f, 20.0f)]
        [SerializeField] private float _droneNozzleGimbalAmplitudeDeg = 6.5f;

        [Tooltip("Frequency (Hz) of the cold-gas thruster attitude-control gimbal corrections.")]
        [Range(0.2f, 4.0f)]
        [SerializeField] private float _droneNozzleGimbalFrequencyHz = 1.35f;

        [Tooltip("Yaw pan amplitude (deg) of the underbelly optical Scanner turret.")]
        [Range(0.0f, 90.0f)]
        [SerializeField] private float _droneScannerPanAmplitudeDeg = 28.0f;

        [Tooltip("Frequency (Hz) of the underbelly optical Scanner pan cycle.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _droneScannerPanFrequencyHz = 0.28f;

        [Header("The Trencher (Tracked Cable-Layer)")]
        [Tooltip("Reference crawling speed (m/s) for the trencher's full cutting and cable-laying motion.")]
        [Min(0.05f)]
        [SerializeField] private float _trencherReferenceSpeed = 0.65f;

        [Tooltip("Effective radius (m) of the track drive sprockets (Roller_FL..RR).")]
        [Min(0.05f)]
        [SerializeField] private float _trencherRollerRadiusMeters = 0.22f;

        [Tooltip("Effective winding radius (m) of the rear CableSpool drum for distance-matched pay-out rotation.")]
        [Min(0.05f)]
        [SerializeField] private float _trencherSpoolRadiusMeters = 0.36f;

        [Tooltip("Spin rate (deg/s) of the nose TrenchWheel while excavating at reference speed.")]
        [Range(15.0f, 360.0f)]
        [SerializeField] private float _trencherCutterWheelSpinDegPerSecond = 115.0f;

        [Tooltip("Slow idle creep spin rate (deg/s) of the nose TrenchWheel when stationary.")]
        [Range(0.0f, 90.0f)]
        [SerializeField] private float _trencherIdleCutterSpinDegPerSecond = 18.0f;

        [Tooltip("Pitch heave amplitude (deg) of the forward TrenchBoom as the cutter teeth bite regolith.")]
        [Range(0.0f, 8.0f)]
        [SerializeField] private float _trencherBoomHeaveAmplitudeDeg = 1.6f;

        [Tooltip("Frequency (Hz) of the TrenchBoom cutting heave.")]
        [Range(0.1f, 2.5f)]
        [SerializeField] private float _trencherBoomHeaveFrequencyHz = 0.75f;

        [Tooltip("Vertical chassis rumble amplitude (m) of the heavy tracked hull.")]
        [Range(0.0f, 0.03f)]
        [SerializeField] private float _trencherChassisRumbleMeters = 0.005f;

        /// <summary>Base linear <c>_EmissionStrength</c> on each machine's <c>Lamp</c> while idling.</summary>
        public float LampBaseEmissionStrength => _lampBaseEmissionStrength;

        /// <summary>Additional linear <c>_EmissionStrength</c> added at reference travel speed.</summary>
        public float LampMoveEmissionBoost => _lampMoveEmissionBoost;

        /// <summary>Amplitude of the slow breathing pulse on <c>_EmissionStrength</c>.</summary>
        public float LampBreathAmplitude => _lampBreathAmplitude;

        /// <summary>Frequency (Hz) of the slow lamp breathing cycle.</summary>
        public float LampBreathFrequencyHz => _lampBreathFrequencyHz;

        /// <summary>Reference crawling speed (m/s) for the panel mite.</summary>
        public float MiteReferenceSpeed => _miteReferenceSpeed;

        /// <summary>Tripod gait cycles per metre of travel for the panel mite.</summary>
        public float MiteCyclesPerMeter => _miteCyclesPerMeter;

        /// <summary>Maximum leg hip swing amplitude (deg) for the panel mite.</summary>
        public float MiteLegSwingAmplitudeDeg => _miteLegSwingAmplitudeDeg;

        /// <summary>Maximum leg vertical lift amplitude (deg) for the panel mite.</summary>
        public float MiteLegLiftAmplitudeDeg => _miteLegLiftAmplitudeDeg;

        /// <summary>Reference travel speed (m/s) for the sweeper.</summary>
        public float SweeperReferenceSpeed => _sweeperReferenceSpeed;

        /// <summary>Wheel rolling radius (m) for the sweeper.</summary>
        public float SweeperWheelRadiusMeters => _sweeperWheelRadiusMeters;

        /// <summary>Rotary brush spin rate (deg/s) at reference speed.</summary>
        public float SweeperBrushSpinDegPerSecond => _sweeperBrushSpinDegPerSecond;

        /// <summary>Rotary brush spin rate (deg/s) while idling.</summary>
        public float SweeperIdleBrushSpinDegPerSecond => _sweeperIdleBrushSpinDegPerSecond;

        /// <summary>Reference cruise speed (m/s) for the survey drone.</summary>
        public float DroneReferenceSpeed => _droneReferenceSpeed;

        /// <summary>Mean hover altitude offset (m) for the survey drone's body.</summary>
        public float DroneHoverAltitudeMeters => _droneHoverAltitudeMeters;

        /// <summary>Vertical hover bob amplitude (m) for the survey drone.</summary>
        public float DroneHoverBobAmplitudeMeters => _droneHoverBobAmplitudeMeters;

        /// <summary>Cold-gas nozzle gimbal amplitude (deg) for the survey drone.</summary>
        public float DroneNozzleGimbalAmplitudeDeg => _droneNozzleGimbalAmplitudeDeg;

        /// <summary>Reference crawling speed (m/s) for the trencher.</summary>
        public float TrencherReferenceSpeed => _trencherReferenceSpeed;

        /// <summary>Track roller radius (m) for the trencher.</summary>
        public float TrencherRollerRadiusMeters => _trencherRollerRadiusMeters;

        /// <summary>Rear cable spool winding radius (m) for the trencher.</summary>
        public float TrencherSpoolRadiusMeters => _trencherSpoolRadiusMeters;

        /// <summary>Nose cutter wheel spin rate (deg/s) at reference speed.</summary>
        public float TrencherCutterWheelSpinDegPerSecond => _trencherCutterWheelSpinDegPerSecond;

        /// <summary>
        /// Evaluates the pure procedural motion pose for <paramref name="species"/> at <paramref name="time"/> (s),
        /// cumulative <paramref name="travelDistance"/> (m), and current <paramref name="speed"/> (m/s).
        /// Performs zero allocations.
        /// </summary>
        public FaunaMotionPose EvaluatePose(FaunaSpecies species, float time, float travelDistance, float speed)
        {
            float clampedSpeed = Mathf.Max(0f, speed);
            var pose = new FaunaMotionPose();

            float refSpeed = GetReferenceSpeed(species);
            float moveFactor = Mathf.Clamp01(clampedSpeed / Mathf.Max(0.01f, refSpeed));

            // Shared slow lamp breathing + movement glow boost (never touches _EmissionColor)
            float breathWave = Mathf.Sin(time * _lampBreathFrequencyHz * TwoPi);
            pose.LampEmissionStrength = Mathf.Max(
                0.05f,
                _lampBaseEmissionStrength + moveFactor * _lampMoveEmissionBoost + breathWave * _lampBreathAmplitude);

            switch (species)
            {
                case FaunaSpecies.PanelMite:
                    EvaluatePanelMite(ref pose, time, travelDistance, moveFactor);
                    break;
                case FaunaSpecies.Sweeper:
                    EvaluateSweeper(ref pose, time, travelDistance, moveFactor);
                    break;
                case FaunaSpecies.SurveyDrone:
                    EvaluateSurveyDrone(ref pose, time, moveFactor);
                    break;
                case FaunaSpecies.Trencher:
                    EvaluateTrencher(ref pose, time, travelDistance, moveFactor);
                    break;
            }

            return pose;
        }

        private float GetReferenceSpeed(FaunaSpecies species)
        {
            switch (species)
            {
                case FaunaSpecies.PanelMite:
                    return _miteReferenceSpeed;
                case FaunaSpecies.Sweeper:
                    return _sweeperReferenceSpeed;
                case FaunaSpecies.SurveyDrone:
                    return _droneReferenceSpeed;
                case FaunaSpecies.Trencher:
                    return _trencherReferenceSpeed;
                default:
                    return 1.0f;
            }
        }

        private void EvaluatePanelMite(ref FaunaMotionPose pose, float time, float travelDistance, float moveFactor)
        {
            float gaitPhase = travelDistance * _miteCyclesPerMeter * TwoPi + time * 1.6f;
            float sinA = Mathf.Sin(gaitPhase);
            float cosA = Mathf.Cos(gaitPhase);
            float idleBreath = Mathf.Sin(time * 1.1f * TwoPi) * _miteIdleLegBreathDeg * (1f - moveFactor);

            // Alternating tripod gait: Group A (L0, R1, L2) and Group B (R0, L1, R2) are 180 deg out of phase
            pose.LegSwingADeg = sinA * _miteLegSwingAmplitudeDeg * moveFactor + idleBreath;
            pose.LegLiftADeg = Mathf.Max(0f, cosA) * _miteLegLiftAmplitudeDeg * moveFactor;

            pose.LegSwingBDeg = -sinA * _miteLegSwingAmplitudeDeg * moveFactor - idleBreath;
            pose.LegLiftBDeg = Mathf.Max(0f, -cosA) * _miteLegLiftAmplitudeDeg * moveFactor;

            // Front electrostatic wiper sweeps continuously (faster while crawling)
            float wiperAmp = _miteWiperSweepAmplitudeDeg * (0.35f + 0.65f * moveFactor);
            pose.WiperYawDeg = Mathf.Sin(time * _miteWiperFrequencyHz * TwoPi) * wiperAmp;

            // Subtle carapace bob twice per gait cycle + slight body roll into the active tripod
            float bobY = Mathf.Abs(sinA) * _miteBodyBobAmplitudeMeters * moveFactor;
            pose.BodyOffset = new Vector3(0f, bobY, 0f);
            pose.BodyEuler = new Vector3(
                Mathf.Sin(time * 0.9f * TwoPi) * 0.8f,
                0f,
                cosA * 1.8f * moveFactor);
        }

        private void EvaluateSweeper(ref FaunaMotionPose pose, float time, float travelDistance, float moveFactor)
        {
            float wheelCircumference = Mathf.Max(0.05f, _sweeperWheelRadiusMeters) * TwoPi;
            pose.WheelSpinDeg = (travelDistance / wheelCircumference) * 360f;

            float brushRate = Mathf.Lerp(_sweeperIdleBrushSpinDegPerSecond, _sweeperBrushSpinDegPerSecond, moveFactor);
            pose.BrushSpinDeg = time * brushRate + pose.WheelSpinDeg * 1.35f;

            float nodPhase = time * _sweeperBrushArmNodFrequencyHz * TwoPi;
            pose.BrushArmPitchDeg = Mathf.Sin(nodPhase) * _sweeperBrushArmNodAmplitudeDeg * (0.25f + 0.75f * moveFactor);

            float rockAngle = Mathf.Sin(nodPhase * 1.5f) * _sweeperChassisRockAmplitudeDeg * moveFactor;
            float heaveY = Mathf.Sin(nodPhase * 2.0f) * 0.0045f * moveFactor;
            pose.BodyOffset = new Vector3(0f, heaveY, 0f);
            pose.BodyEuler = new Vector3(0f, 0f, rockAngle);
        }

        private void EvaluateSurveyDrone(ref FaunaMotionPose pose, float time, float moveFactor)
        {
            float bobPhase = time * _droneHoverBobFrequencyHz * TwoPi;
            float bobY = _droneHoverAltitudeMeters + Mathf.Sin(bobPhase) * _droneHoverBobAmplitudeMeters;
            float driftX = Mathf.Cos(bobPhase * 0.65f) * _droneHoverBobAmplitudeMeters * 0.45f;
            pose.BodyOffset = new Vector3(driftX, bobY, 0f);

            float pitchForward = _droneForwardPitchAtRefSpeedDeg * moveFactor;
            float swayPitch = Mathf.Cos(bobPhase * 0.85f) * _droneHoverSwayAmplitudeDeg;
            float swayRoll = Mathf.Sin(bobPhase * 0.72f) * _droneHoverSwayAmplitudeDeg;
            pose.BodyEuler = new Vector3(pitchForward + swayPitch, 0f, swayRoll);

            // 4 De Laval cold-gas nozzles gimbal to counter-stabilize hover & vector thrust
            float gPhase = time * _droneNozzleGimbalFrequencyHz * TwoPi;
            float gAmp = _droneNozzleGimbalAmplitudeDeg * (0.45f + 0.55f * moveFactor);
            float collectivePitch = -pitchForward * 0.65f;

            pose.NozzleGimbalFL = new Vector3(
                collectivePitch + Mathf.Sin(gPhase) * gAmp,
                0f,
                Mathf.Cos(gPhase * 1.1f) * gAmp);
            pose.NozzleGimbalFR = new Vector3(
                collectivePitch + Mathf.Cos(gPhase + 0.8f) * gAmp,
                0f,
                -Mathf.Sin(gPhase * 1.1f) * gAmp);
            pose.NozzleGimbalRL = new Vector3(
                collectivePitch - Mathf.Sin(gPhase + 1.4f) * gAmp,
                0f,
                Mathf.Sin(gPhase * 0.9f) * gAmp);
            pose.NozzleGimbalRR = new Vector3(
                collectivePitch - Mathf.Cos(gPhase + 2.1f) * gAmp,
                0f,
                -Mathf.Cos(gPhase * 0.9f) * gAmp);

            pose.ScannerYawDeg = Mathf.Sin(time * _droneScannerPanFrequencyHz * TwoPi) * _droneScannerPanAmplitudeDeg;
        }

        private void EvaluateTrencher(ref FaunaMotionPose pose, float time, float travelDistance, float moveFactor)
        {
            float rollerCircumference = Mathf.Max(0.05f, _trencherRollerRadiusMeters) * TwoPi;
            pose.WheelSpinDeg = (travelDistance / rollerCircumference) * 360f;

            // Nose cutter wheel spins steadily to excavate regolith
            float cutterRate = Mathf.Lerp(_trencherIdleCutterSpinDegPerSecond, _trencherCutterWheelSpinDegPerSecond, moveFactor);
            pose.TrenchWheelSpinDeg = time * cutterRate + pose.WheelSpinDeg * 0.85f;

            // Rear cable spool pays out cable in reverse rotation proportional to distance travelled
            float spoolCircumference = Mathf.Max(0.05f, _trencherSpoolRadiusMeters) * TwoPi;
            pose.CableSpoolSpinDeg = -(travelDistance / spoolCircumference) * 360f - time * 8.0f * moveFactor;

            float heavePhase = time * _trencherBoomHeaveFrequencyHz * TwoPi;
            pose.TrenchBoomPitchDeg = Mathf.Sin(heavePhase) * _trencherBoomHeaveAmplitudeDeg * (0.2f + 0.8f * moveFactor);

            float rumbleY = Mathf.Sin(heavePhase * 2.3f) * _trencherChassisRumbleMeters * moveFactor;
            float rumblePitch = Mathf.Cos(heavePhase * 1.7f) * 0.65f * moveFactor;
            pose.BodyOffset = new Vector3(0f, rumbleY, 0f);
            pose.BodyEuler = new Vector3(rumblePitch, 0f, 0f);
        }
    }
}
