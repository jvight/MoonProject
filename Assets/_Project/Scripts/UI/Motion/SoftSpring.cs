using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// An under-damped spring for the soft overshoot things settle with (VISION pillar 1). Sub-stepped so a long frame
    /// (a hitch, the first frame after a pause) never makes it explode.
    /// </summary>
    internal sealed class SoftSpring
    {
        private const float MaxStep = 1f / 120f;
        private const float RestEpsilon = 1e-4f;

        public SoftSpring(float value)
        {
            Value = value;
        }

        public float Value { get; private set; }

        public float Velocity { get; private set; }

        public bool IsAtRest(float target)
        {
            return Mathf.Abs(Value - target) < RestEpsilon && Mathf.Abs(Velocity) < RestEpsilon;
        }

        public void Snap(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        /// <param name="target">Where the spring is pulled.</param>
        /// <param name="deltaTime">Seconds to advance.</param>
        /// <param name="frequency">Natural frequency in Hz (how quickly it gets there).</param>
        /// <param name="damping">Damping ratio: 1 = no overshoot, lower = a softer, longer settle.</param>
        public void Step(float target, float deltaTime, float frequency, float damping)
        {
            float omega = 2f * Mathf.PI * frequency;
            float remaining = deltaTime;
            while (remaining > 0f)
            {
                float dt = Mathf.Min(remaining, MaxStep);
                float acceleration = omega * omega * (target - Value) - 2f * damping * omega * Velocity;
                Velocity += acceleration * dt;
                Value += Velocity * dt;
                remaining -= dt;
            }

            if (IsAtRest(target))
            {
                Snap(target);
            }
        }
    }
}
