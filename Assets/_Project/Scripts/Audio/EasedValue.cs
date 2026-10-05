using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Frame-rate independent exponential easing towards a target: after one time constant 63 % of the gap is
    /// closed whatever the frame rate. Separate rise/fall times give soft attacks with quicker releases.
    /// </summary>
    public sealed class EasedValue
    {
        public EasedValue(float initial)
        {
            Value = initial;
        }

        public float Value { get; private set; }

        public void Snap(float value)
        {
            Value = value;
        }

        public float Step(float target, float deltaTime, float timeConstant)
        {
            return Step(target, deltaTime, timeConstant, timeConstant);
        }

        /// <summary>Moves towards <paramref name="target"/>; a non-positive time constant jumps straight there.</summary>
        public float Step(float target, float deltaTime, float riseTime, float fallTime)
        {
            float tau = target > Value ? riseTime : fallTime;
            if (tau <= 0f)
            {
                Value = target;
            }
            else if (deltaTime > 0f)
            {
                Value += (target - Value) * (1f - Mathf.Exp(-deltaTime / tau));
            }

            return Value;
        }
    }
}
