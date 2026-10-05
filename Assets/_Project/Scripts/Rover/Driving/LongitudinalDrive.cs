using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Forward/backward acceleration of the rover along its heading. Three regimes:
    /// <list type="bullet">
    /// <item>Driving: a = A * |throttle| * (1 - (v / target)^2), an eased tanh curve that settles on
    /// target = |throttle| * topSpeed; the time to 90% is the same for any stick deflection.</item>
    /// <item>Coasting (no throttle, or faster than the throttle asks for): decel = C * (excess / top)^ease, which
    /// stops in exactly CoastStopTime from top speed and tapers to zero so the last metres roll out softly.</item>
    /// <item>Braking (input against the motion): same shape as coasting, scaled by input, until ReverseEngageSpeed.</item>
    /// </list>
    /// Decelerations are clamped so one step never pushes the speed through zero (no jitter at rest).
    /// </summary>
    public static class LongitudinalDrive
    {
        /// <summary>Acceleration (m/s^2, + = forward) for the current signed forward speed and eased throttle.</summary>
        public static float Acceleration(DriveSettings settings, float forwardSpeed, float throttle, float deltaTime)
        {
            float speed = Mathf.Abs(forwardSpeed);
            float motion = forwardSpeed >= 0f ? 1f : -1f;
            float input = Mathf.Abs(throttle);

            if (input <= settings.InputDeadZone)
            {
                return -motion * Decelerate(settings.CoastDeceleration, settings.CoastEase, speed, speed,
                    settings.TopSpeed, deltaTime);
            }

            float inputDirection = throttle > 0f ? 1f : -1f;
            bool against = speed > settings.ReverseEngageSpeed && inputDirection != motion;
            if (against)
            {
                return -motion * input * Decelerate(settings.BrakeDeceleration, settings.BrakeEase, speed, speed,
                    settings.TopSpeed, deltaTime);
            }

            bool forward = inputDirection > 0f;
            float top = forward ? settings.TopSpeed : settings.ReverseTopSpeed;
            float peak = forward ? settings.ForwardAcceleration : settings.ReverseAcceleration;
            float target = input * top;

            // Signed speed in the input's direction; a slight roll the other way (below ReverseEngageSpeed) counts as
            // "behind" the target and is driven through smoothly.
            float along = forwardSpeed * inputDirection;
            if (along <= target)
            {
                float ratio = Mathf.Max(0f, along) / target;
                return inputDirection * peak * input * (1f - ratio * ratio);
            }

            float excess = along - target;
            return -inputDirection * Decelerate(settings.CoastDeceleration, settings.CoastEase, excess, excess,
                settings.TopSpeed, deltaTime);
        }

        /// <summary>Power-law deceleration magnitude, clamped so it removes at most <paramref name="limit"/> this step.</summary>
        private static float Decelerate(float peak, float ease, float amount, float limit, float topSpeed, float deltaTime)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float decel = peak * Mathf.Pow(Mathf.Min(1f, amount / topSpeed), ease);
            return deltaTime > 0f ? Mathf.Min(decel, limit / deltaTime) : decel;
        }
    }
}
