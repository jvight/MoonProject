namespace MoonProject.Gameplay
{
    /// <summary>
    /// What the opening's objective flow reads of the game (docs/design/opening-v2.md §2–§3), every answer derived from
    /// saved game state, never from the flow's own memory: a loaded game shows the right step at once. Structure ids
    /// are <see cref="StructureIds"/>.
    /// </summary>
    public interface IOpeningProgress
    {
        /// <summary>07 has come within the lander's reach this save (B2's exit).</summary>
        bool ReachedLander { get; }

        /// <summary>The blueprint of <paramref name="structureId"/> is learned.</summary>
        bool Knows(string structureId);

        /// <summary>07's hold covers the build cost of <paramref name="structureId"/>.</summary>
        bool Covers(string structureId);

        /// <summary>At least one <paramref name="structureId"/> stands built.</summary>
        bool Built(string structureId);

        /// <summary>The spare igniter is in 07's hold (B4, from the lander's open crate).</summary>
        bool CarriesIgniter { get; }

        /// <summary>The spare igniter is fitted to 07's cutter (B6): the cutter works.</summary>
        bool IgniterFitted { get; }

        /// <summary>07 has charged on a pad it built (B6): the lander's reserve has been drawn on.</summary>
        bool FirstCharge { get; }

        /// <summary>The lander's one-charge reserve is spent (<c>LANDER RESERVE · 0%</c> has shown).</summary>
        bool ReserveSpent { get; }

        /// <summary>A built solar array powers a built pad (B8).</summary>
        bool PowerOn { get; }

        /// <summary>A built battery bank is connected to the powered base (B9).</summary>
        bool NightReady { get; }

        /// <summary>The first night has fallen over a night-ready base (B10's exit).</summary>
        bool NightFallen { get; }
    }
}
