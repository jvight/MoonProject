using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// A 1D mass-spring-damper stepped with the exact analytic solution, so it is unconditionally stable and gives the
    /// same motion at any frame rate. Frequency is in Hz; a damping ratio below 1 overshoots (about 0.45 gives one soft
    /// overshoot, the "jelly" settle the rover body uses), 1 is critically damped, above 1 is sluggish.
    /// </summary>
    public struct DampedSpring
    {
        private const float CriticalBand = 1e-3f;

        public DampedSpring(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        public float Value { get; private set; }

        public float Velocity { get; private set; }

        /// <summary>Teleports the spring to <paramref name="value"/> at rest.</summary>
        public void Reset(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        /// <summary>Adds an instantaneous velocity change (an impulse per unit mass).</summary>
        public void AddVelocity(float deltaVelocity)
        {
            Velocity += deltaVelocity;
        }

        /// <summary>Clamps the value; velocity pointing further out of range is removed so it does not stick.</summary>
        public void Clamp(float min, float max)
        {
            if (Value < min)
            {
                Value = min;
                Velocity = Mathf.Max(Velocity, 0f);
            }
            else if (Value > max)
            {
                Value = max;
                Velocity = Mathf.Min(Velocity, 0f);
            }
        }

        /// <summary>Advances the spring toward <paramref name="target"/> (held constant over the step).</summary>
        public void Step(float target, float frequency, float dampingRatio, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (frequency <= 0f)
            {
                Value += Velocity * deltaTime;
                return;
            }

            float omega = 2f * Mathf.PI * frequency;
            float zeta = Mathf.Max(0f, dampingRatio);
            float y0 = Value - target;
            float v0 = Velocity;
            float t = deltaTime;
            float y;
            float v;

            if (zeta < 1f - CriticalBand)
            {
                float omegaD = omega * Mathf.Sqrt(1f - zeta * zeta);
                float decay = Mathf.Exp(-zeta * omega * t);
                float cos = Mathf.Cos(omegaD * t);
                float sin = Mathf.Sin(omegaD * t);
                y = decay * (y0 * cos + (v0 + zeta * omega * y0) / omegaD * sin);
                v = decay * (v0 * cos - (v0 * zeta * omega + omega * omega * y0) / omegaD * sin);
            }
            else if (zeta > 1f + CriticalBand)
            {
                float root = omega * Mathf.Sqrt(zeta * zeta - 1f);
                float r1 = -zeta * omega + root;
                float r2 = -zeta * omega - root;
                float c1 = (v0 - r2 * y0) / (r1 - r2);
                float c2 = y0 - c1;
                float e1 = Mathf.Exp(r1 * t);
                float e2 = Mathf.Exp(r2 * t);
                y = c1 * e1 + c2 * e2;
                v = c1 * r1 * e1 + c2 * r2 * e2;
            }
            else
            {
                float decay = Mathf.Exp(-omega * t);
                float c = v0 + omega * y0;
                y = decay * (y0 + c * t);
                v = decay * (v0 - omega * c * t);
            }

            Value = target + y;
            Velocity = v;
        }
    }
}
