namespace MoonProject.Core
{
    /// <summary>Ids of the <see cref="WorldAnchor"/>s the World domain publishes (docs/features/M3-04).</summary>
    public static class WorldAnchorIds
    {
        /// <summary>Where the canyon opens off the basin floor, facing into it.</summary>
        public const string CanyonMouth = "canyon.mouth";

        /// <summary>Top of the take-off ramp on the near side of the chasm, facing across it.</summary>
        public const string CanyonLip = "canyon.lip";

        /// <summary>Centre of the flat landing apron on the far side of the chasm, facing into the canyon.</summary>
        public const string CanyonLanding = "canyon.landing";

        /// <summary>The glinting ledge visible from the base (a relic waits here).</summary>
        public const string CanyonLedge = "canyon.ledge";

        /// <summary>The terminus chamber at the canyon's far end (Bell and the crew log cache, M3-05).</summary>
        public const string CanyonTerminus = "canyon.terminus";

        /// <summary>Top of the no-jump way back out to the basin, facing down it.</summary>
        public const string CanyonExit = "canyon.exit";

        /// <summary>Prefix of the side alcoves, numbered from the mouth inward: "canyon.alcove_0", "canyon.alcove_1"...</summary>
        public const string CanyonAlcovePrefix = "canyon.alcove_";

        /// <summary>
        /// Prefix of the relay mast sites (docs/features/M3-06), numbered in restoring order: "relay.0", "relay.1"...
        /// Forward points from the mast toward home; the radius is the mast's flat pad.
        /// </summary>
        public const string RelayPrefix = "relay.";
    }
}
