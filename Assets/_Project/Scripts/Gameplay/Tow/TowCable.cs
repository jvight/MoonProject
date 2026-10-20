using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tow cable's pull (ruling 4: a rope from the winch, no beam): slack within its length, it does nothing; taut,
    /// it draws the load's hitch along the rope at 07's own speed plus a catch-up that grows with the stretch but never
    /// past a gentle cap, so a load that fell behind closes in calmly and never surges past 07; the pull is capped so a
    /// sudden stretch never yanks, and it lifts the load's front by a share of the flat pull so it rides over bumps instead of digging in, but
    /// never by more than a share of the Moon's gravity, so the load keeps its weight on its skids and is never lifted
    /// off the ground. The pull is an acceleration on the load only: 07 never feels a reaction (the speed cap carries
    /// the weight), so it is never yanked or flipped. Pure functions.
    /// </summary>
    public static class TowCable
    {
        /// <summary>
        /// Acceleration (m/s²) the cable gives the load at its hitch this physics step.
        /// </summary>
        /// <param name="winch">07's winch (where the cable leaves 07).</param>
        /// <param name="hitch">Where the cable meets the load.</param>
        /// <param name="hitchVelocity">The load's velocity at its hitch.</param>
        /// <param name="winchVelocity">07's velocity.</param>
        /// <param name="length">Metres of cable paid out.</param>
        /// <param name="gravity">The Moon's gravity (m/s², positive).</param>
        public static Vector3 Pull(Vector3 winch, Vector3 hitch, Vector3 hitchVelocity, Vector3 winchVelocity,
            float length, float gravity, TowSettings settings)
        {
            Vector3 rope = winch - hitch;
            float distance = rope.magnitude;
            if (distance <= length || distance <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 along = rope / distance;
            float wanted = Vector3.Dot(winchVelocity, along) + CatchUp(distance, length, settings);
            float pull = 2f * settings.Stiffness * (wanted - Vector3.Dot(hitchVelocity, along));
            if (pull <= 0f)
            {
                // A rope never pushes: the load already comes in as fast as the rope would draw it.
                return Vector3.zero;
            }

            pull = Mathf.Min(pull, settings.MaxPull);
            Vector3 force = along * pull;
            float flat = new Vector2(force.x, force.z).magnitude;
            force.y = Mathf.Min(force.y + flat * settings.LiftShare, gravity * settings.MaxLift);
            return force;
        }

        /// <summary>
        /// How much faster than 07 the load may come in (m/s) at <paramref name="distance"/> from the winch: nothing
        /// while the rope is slack, more the further it has fallen behind, never past the catch-up.
        /// </summary>
        public static float CatchUp(float distance, float length, TowSettings settings)
        {
            return Mathf.Clamp(settings.Stiffness * (distance - length), 0f, settings.CatchUp);
        }

        /// <summary>
        /// The skids' bite while towed (m/s², flat): in the Moon's low gravity a sliding load's friction alone would let
        /// it run on down a slope, close in on 07 and bump it, so speed beyond 07's own plus the load's
        /// <paramref name="catchUp"/> (<see cref="CatchUp"/>) is braked away, as briskly as the rope takes up and never
        /// harder than its cap. Nothing at or under that speed.
        /// </summary>
        public static Vector3 SkidBite(Vector3 loadVelocity, float roverSpeed, float catchUp, TowSettings settings)
        {
            Vector3 flat = new Vector3(loadVelocity.x, 0f, loadVelocity.z);
            float speed = flat.magnitude;
            float excess = speed - (roverSpeed + catchUp);
            if (excess <= 0f)
            {
                return Vector3.zero;
            }

            return -flat / speed * Mathf.Min(2f * settings.Stiffness * excess, settings.MaxPull);
        }

        /// <summary>
        /// How hard the cable works, 0..1: 0 slack or just taut, 1 at the stretch where the load counts as caught.
        /// </summary>
        public static float Strain01(Vector3 winch, Vector3 hitch, float length, TowSettings settings)
        {
            float stretch = Vector3.Distance(winch, hitch) - length;
            return settings.SnagStretch <= 0f ? 0f : Mathf.Clamp01(stretch / settings.SnagStretch);
        }
    }
}
