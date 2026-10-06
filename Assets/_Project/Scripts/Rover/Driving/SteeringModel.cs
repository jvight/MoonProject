using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>Speed-dependent yaw rate and the forward/backing-up steering convention.</summary>
    public static class SteeringModel
    {
        /// <summary>
        /// Full-lock turn rate (deg/s) at <paramref name="speed"/> (m/s, unsigned): eases from the pivot rate at
        /// standstill up to the peak, then down to the top-speed rate.
        /// </summary>
        public static float TurnRate(SteeringSettings settings, float speed, float topSpeed)
        {
            speed = Mathf.Abs(speed);
            if (speed <= settings.PeakTurnSpeed)
            {
                float t = Smoothing.SmoothStep(0f, settings.PeakTurnSpeed, speed);
                return Mathf.Lerp(settings.PivotTurnRate, settings.PeakTurnRate, t);
            }

            float u = Smoothing.SmoothStep(settings.PeakTurnSpeed, Mathf.Max(settings.PeakTurnSpeed, topSpeed), speed);
            return Mathf.Lerp(settings.PeakTurnRate, settings.TopSpeedTurnRate, u);
        }

        /// <summary>
        /// +1 when steering should turn the nose toward the input (driving forward or pivoting), -1 when backing up
        /// (moving backwards, or about to: holding reverse near standstill).
        /// </summary>
        public static float SteerDirection(SteeringSettings settings, float forwardSpeed, float throttle,
            float inputDeadZone)
        {
            bool movingBack = forwardSpeed < -settings.ReverseSteerSpeed;
            bool startingBack = forwardSpeed < settings.ReverseSteerSpeed && throttle < -inputDeadZone;
            return movingBack || startingBack ? -1f : 1f;
        }

        /// <summary>Yaw rate (deg/s, + = clockwise seen from above, i.e. turning right).</summary>
        public static float YawRate(SteeringSettings settings, float steer, float steerDirection, float forwardSpeed,
            float topSpeed, bool grounded)
        {
            float rate = steer * steerDirection * TurnRate(settings, forwardSpeed, topSpeed);
            return grounded ? rate : rate * settings.AirTurnFactor;
        }
    }
}
