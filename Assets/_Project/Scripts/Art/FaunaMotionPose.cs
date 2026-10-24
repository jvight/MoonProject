using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Pure value-type snapshot of the procedural idle/move pose evaluated by <see cref="FaunaMotionTuning"/>
    /// for a machine fauna instance at a given time, travel distance, and speed.
    /// </summary>
    public struct FaunaMotionPose
    {
        /// <summary>Additive local translation offset (m) applied to the <c>Body</c> pivot.</summary>
        public Vector3 BodyOffset;

        /// <summary>Additive local Euler rotation (deg) applied to the <c>Body</c> pivot.</summary>
        public Vector3 BodyEuler;

        /// <summary>Forward/back hip swing angle (deg) for tripod leg group A (<c>Leg_L0</c>, <c>Leg_R1</c>, <c>Leg_L2</c>).</summary>
        public float LegSwingADeg;

        /// <summary>Vertical step lift angle (deg) for tripod leg group A (<c>Leg_L0</c>, <c>Leg_R1</c>, <c>Leg_L2</c>).</summary>
        public float LegLiftADeg;

        /// <summary>Forward/back hip swing angle (deg) for tripod leg group B (<c>Leg_R0</c>, <c>Leg_L1</c>, <c>Leg_R2</c>).</summary>
        public float LegSwingBDeg;

        /// <summary>Vertical step lift angle (deg) for tripod leg group B (<c>Leg_R0</c>, <c>Leg_L1</c>, <c>Leg_R2</c>).</summary>
        public float LegLiftBDeg;

        /// <summary>Yaw sweep angle (deg) of the panel mite's front <c>Wiper</c> bar.</summary>
        public float WiperYawDeg;

        /// <summary>Cumulative roll angle (deg around local X) of wheels or track rollers.</summary>
        public float WheelSpinDeg;

        /// <summary>Pitch nod angle (deg around local X) of the sweeper's front <c>BrushArm</c>.</summary>
        public float BrushArmPitchDeg;

        /// <summary>Cumulative spin angle (deg around local X) of the sweeper's front rotary <c>Brush</c>.</summary>
        public float BrushSpinDeg;

        /// <summary>Yaw pan angle (deg around local Y) of the survey drone's underbelly <c>Scanner</c>.</summary>
        public float ScannerYawDeg;

        /// <summary>Gimbal Euler angles (deg) for the drone's front-left cold-gas thruster (<c>Nozzle_FL</c>).</summary>
        public Vector3 NozzleGimbalFL;

        /// <summary>Gimbal Euler angles (deg) for the drone's front-right cold-gas thruster (<c>Nozzle_FR</c>).</summary>
        public Vector3 NozzleGimbalFR;

        /// <summary>Gimbal Euler angles (deg) for the drone's rear-left cold-gas thruster (<c>Nozzle_RL</c>).</summary>
        public Vector3 NozzleGimbalRL;

        /// <summary>Gimbal Euler angles (deg) for the drone's rear-right cold-gas thruster (<c>Nozzle_RR</c>).</summary>
        public Vector3 NozzleGimbalRR;

        /// <summary>Pitch heave angle (deg around local X) of the trencher's <c>TrenchBoom</c>.</summary>
        public float TrenchBoomPitchDeg;

        /// <summary>Cumulative spin angle (deg around local X) of the trencher's <c>TrenchWheel</c>.</summary>
        public float TrenchWheelSpinDeg;

        /// <summary>Cumulative pay-out rotation angle (deg around local X) of the trencher's rear <c>CableSpool</c>.</summary>
        public float CableSpoolSpinDeg;

        /// <summary>Linear <c>_EmissionStrength</c> multiplier pushed to the <c>Lamp</c> renderer via <see cref="MaterialPropertyBlock"/>.</summary>
        public float LampEmissionStrength;
    }
}
