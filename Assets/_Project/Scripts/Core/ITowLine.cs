using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// What 07's tow cable is doing now (docs/design/opening-v3-purpose.md R6, ruling 4: a physical cable from the
    /// winch, no beam). Registered in the <see cref="GameContext"/> by the Gameplay domain, which owns the tow; Rover
    /// draws the cable from its <see cref="IRoverRig.WinchOrigin"/> to <see cref="LoadHitch"/>, spins the spool and
    /// leans into <see cref="Strain01"/>. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface ITowLine
    {
        /// <summary>The cable holds a load.</summary>
        bool IsTowing { get; }

        /// <summary>Where the cable meets the load (its hitch), in the world; meaningless while not towing.</summary>
        Vector3 LoadHitch { get; }

        /// <summary>Metres of cable paid out: the rope is taut beyond it and slack within it.</summary>
        float Length { get; }

        /// <summary>
        /// How hard the cable works, 0..1: 0 slack or just taut, 1 stretched to the point where the load is caught on
        /// something and the winch gives it a lift.
        /// </summary>
        float Strain01 { get; }

        /// <summary>
        /// The towable load called <paramref name="towId"/> (e.g. the habitat's power module), towed or not; false
        /// when the opening stands no such load. Its root moves while it is towed: read it each frame.
        /// </summary>
        bool TryFindLoad(string towId, out Transform root);
    }
}
