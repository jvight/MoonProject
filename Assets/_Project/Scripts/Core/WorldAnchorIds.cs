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
        /// Prefix of the relay mast sites; only "relay.1", the dead west mast the opening shows, stands today.
        /// Forward points from the mast toward home; the radius is the mast's flat pad.
        /// </summary>
        public const string RelayPrefix = "relay.";

        /// <summary>
        /// Prefix of the salvage sites (docs/features/M3-13); only "site.kestrel" stands today. Forward points the way
        /// 07 approaches; the radius is the site's footprint.
        /// </summary>
        public const string SitePrefix = "site.";

        /// <summary>
        /// The far (home-side) end of Kestrel-3's debris furrow; Forward is the fall line toward the crater. A map
        /// place, kept clear for driving up to <c>site.kestrel</c>. Not a site.
        /// </summary>
        public const string KestrelTrail = "trail.kestrel";

        /// <summary>06's spot by the old canyon-mouth mast (Rover's elder glance); Forward faces home.</summary>
        public const string Micro06 = "micro.06";

        /// <summary>
        /// Opening v2 (docs/design/opening-v2.md §4): 07's wake point on the old flat pad, where its dead charging post
        /// stands; Forward points toward the landed lander.
        /// </summary>
        public const string PlaceWake = "place.wake";

        /// <summary>
        /// Opening v2: the landed cargo lander's centre. Forward points out of its service-port side, toward the build
        /// apron and 07's approach.
        /// </summary>
        public const string PlaceLander = "place.lander";

        /// <summary>
        /// Opening v2: the build site, the lander's old unloading apron (flat, no rocks); its radius is the build area.
        /// Forward points toward the lander.
        /// </summary>
        public const string PlaceApron = "place.apron";

        /// <summary>Opening v2: the Lumen station ruins cluster's centre; Forward runs on from the apron.</summary>
        public const string PlaceRuins = "place.ruins";

        /// <summary>
        /// Opening v2: the first drive's point of interest in the Kestrel furrow (debris and a crate); Forward runs out
        /// from the wake.
        /// </summary>
        public const string PlaceMidway = "place.midway";
    }
}
