namespace MoonProject.Art
{
    /// <summary>
    /// Identifies the four 0.6 autonomous machine species in the <c>Art</c> domain
    /// (<c>docs/design/0.6-living-moon.md</c> §1a.1).
    /// </summary>
    public enum FaunaSpecies
    {
        /// <summary>Palm-sized 6-legged solar-panel dust-wiper crawler (Lumen family).</summary>
        PanelMite = 0,

        /// <summary>Slow 4-wheeled utility sweeper with a wide front rotary brush head (Lumen family).</summary>
        Sweeper = 1,

        /// <summary>Small hexagonal cold-gas thruster drone with 4 De Laval nozzles (Tycho / Meridian family).</summary>
        SurveyDrone = 2,

        /// <summary>Heavy tracked crawler with a nose trenching cutter wheel and rear cable spool (Lumen family).</summary>
        Trencher = 3,
    }
}
