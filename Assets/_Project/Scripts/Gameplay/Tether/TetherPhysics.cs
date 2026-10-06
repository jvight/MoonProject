using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tether's pull, a PD spring on the relic only (07 never feels a reaction, so it is never yanked or flipped):
    /// the relic is drawn toward a point at the tether's length from 07's eye, floating a little above the ground and
    /// never higher than the lift limit, with its velocity damped toward 07's own so it trails smoothly. Heavier
    /// relics get a slower spring and the same force cap, so mass is felt.
    /// </summary>
    public static class TetherPhysics
    {
        /// <summary>
        /// Where the tether wants the relic: at <paramref name="length"/> from <paramref name="anchor"/> in the
        /// direction it already is, lifted to <paramref name="hoverHeight"/> above <paramref name="groundHeight"/> and
        /// no higher than <paramref name="ceiling"/>.
        /// </summary>
        public static Vector3 Target(Vector3 anchor, Vector3 relic, float length, float groundHeight,
            float hoverHeight, float ceiling)
        {
            Vector3 direction = relic - anchor;
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.back;
            Vector3 target = anchor + direction * length;
            target.y = Mathf.Min(Mathf.Max(target.y, groundHeight + hoverHeight), ceiling);
            return target;
        }

        /// <summary>Natural frequency (rad/s) for a relic of <paramref name="mass"/> kg.</summary>
        public static float Frequency(float mass, TetherTuning tuning)
        {
            float ratio = tuning.ReferenceMass / Mathf.Max(0.01f, mass);
            return tuning.NaturalFrequency * Mathf.Pow(ratio, tuning.MassExponent);
        }

        /// <summary>Force (N) to apply to the relic this physics step.</summary>
        public static Vector3 Force(Vector3 position, Vector3 velocity, Vector3 target, Vector3 targetVelocity,
            float mass, float gravity, TetherTuning tuning)
        {
            float omega = Frequency(mass, tuning);
            Vector3 acceleration = omega * omega * (target - position) +
                                   2f * tuning.DampingRatio * omega * (targetVelocity - velocity);

            // Hold the relic up against gravity on top of the spring, so it floats at the target instead of sagging.
            acceleration.y += gravity;
            acceleration = Vector3.ClampMagnitude(acceleration, tuning.MaxAcceleration);
            Vector3 force = acceleration * mass;
            force.y = Mathf.Min(force.y, mass * gravity * tuning.LiftMultiplier);
            return Vector3.ClampMagnitude(force, tuning.MaxForce);
        }
    }
}
