using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The feel kit's pure maths (docs/design/feel-pass.md §3.1): how hot a seam point is by age, where a charge mote
    /// is on its curving way in, how far the beam has reached, and one vacuum-ballistic
    /// step (lunar gravity, no drag, R11). No Unity objects, so every curve is tested in EditMode.
    /// </summary>
    public static class FeelCurves
    {
        /// <summary>
        /// A seam point's heat by its age since the hot point passed: 1 (white-yellow) at 0 s, 2/3 (orange) at
        /// <paramref name="orange"/>, 1/3 (dull red) at <paramref name="red"/>, 0 (dark) from <paramref name="dark"/>.
        /// </summary>
        public static float Heat(float age, float orange, float red, float dark)
        {
            if (age <= 0f)
            {
                return 1f;
            }

            if (age < orange)
            {
                return Mathf.Lerp(1f, 2f / 3f, age / orange);
            }

            if (age < red)
            {
                return Mathf.Lerp(2f / 3f, 1f / 3f, (age - orange) / Mathf.Max(red - orange, 1e-4f));
            }

            return age < dark ? Mathf.Lerp(1f / 3f, 0f, (age - red) / Mathf.Max(dark - red, 1e-4f)) : 0f;
        }

        /// <summary>
        /// A charge mote's offset from the emitter at charge time <paramref name="t"/> (0..1): it starts at
        /// <paramref name="start"/> on the sphere, curls <paramref name="swirl"/> turns round <paramref name="axis"/>
        /// and is drawn in on an ease-in of power <paramref name="ease"/>, reaching the emitter at t = 1.
        /// </summary>
        public static Vector3 MoteOffset(Vector3 start, Vector3 axis, float t, float ease, float swirl)
        {
            float u = Mathf.Clamp01(t);
            float inward = Mathf.Pow(u, ease);
            Quaternion curl = Quaternion.AngleAxis(swirl * 360f * inward, axis);
            return curl * start * (1f - inward);
        }

        /// <summary>
        /// The beam's reach (0..1) a share <paramref name="t"/> into its extend: a fast cubic ease-out.
        /// </summary>
        public static float Extend(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u * u;
        }

        /// <summary>
        /// One step of a particle under lunar gravity with no drag. When the step crosses the ground plane at
        /// <paramref name="groundY"/>, the particle lands there and bounces with <paramref name="restitution"/> of
        /// its vertical speed (its horizontal speed keeps the same share); returns true on that landing.
        /// </summary>
        public static bool Ballistic(ref Vector3 position, ref Vector3 velocity, float deltaTime, float gravity,
            float groundY, float restitution)
        {
            velocity.y -= gravity * deltaTime;
            Vector3 next = position + velocity * deltaTime;
            if (next.y > groundY || velocity.y > 0f)
            {
                position = next;
                return false;
            }

            position = new Vector3(next.x, groundY, next.z);
            velocity = new Vector3(velocity.x, -velocity.y, velocity.z) * restitution;
            return true;
        }
    }
}
