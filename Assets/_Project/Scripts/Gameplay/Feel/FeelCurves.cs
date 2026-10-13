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
        /// The scan ribbon's span along its path a share <paramref name="t"/> into its stream: x the tail, y the head
        /// (0 the fragment, 1 the lens). The head leads, the tail trails <paramref name="length"/> behind, both on an
        /// ease-in, and both reach the lens at t = 1.
        /// </summary>
        public static Vector2 RibbonSpan(float t, float length)
        {
            float u = Mathf.Clamp01(t) * (1f + length);
            float head = Mathf.Clamp01(u);
            float tail = Mathf.Clamp01(u - length);
            return new Vector2(tail * tail, head * head);
        }

        /// <summary>
        /// A point a share <paramref name="s"/> along the scan ribbon's path: a quadratic arc from
        /// <paramref name="from"/> to <paramref name="to"/>, lifted <paramref name="arc"/> of its length at the middle.
        /// </summary>
        public static Vector3 RibbonPoint(Vector3 from, Vector3 to, float arc, float s)
        {
            Vector3 control = (from + to) * 0.5f + Vector3.up * (arc * Vector3.Distance(from, to));
            float u = Mathf.Clamp01(s);
            float v = 1f - u;
            return v * v * from + 2f * v * u * control + u * u * to;
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
