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

        /// <summary>
        /// Prefix of the salvage sites (docs/features/M3-13): "site.depot", "site.kestrel", "site.drill", "site.garage",
        /// "site.lander". Forward points the way 07 approaches; the radius is the site's footprint.
        /// </summary>
        public const string SitePrefix = "site.";

        /// <summary>
        /// The far (home-side) end of Kestrel-3's debris furrow; Forward is the fall line toward the crater. A map
        /// place, kept clear for driving up to <c>site.kestrel</c>. Not a site.
        /// </summary>
        public const string KestrelTrail = "trail.kestrel";

        /// <summary>
        /// The crew's antenna farm in the mid-west (M3-18 L7): toppled dish frames, a place the lookout and the map
        /// can name. Forward faces on from home.
        /// </summary>
        public const string PlaceAntennaFarm = "place.antenna_farm";

        /// <summary>
        /// Prefix of the places beyond the rim that the crew's map pencils at the paper's edge (docs/features/M3-07
        /// step 3): <see cref="RegionPeak"/>, <see cref="RegionTerraces"/>.
        /// </summary>
        public const string RegionPrefix = "region.";

        /// <summary>The Peak's summit plateau; the radius is the plateau's.</summary>
        public const string RegionPeak = "region.peak";

        /// <summary>
        /// The foot of the rim terraces' wall (0.6's region), the last drivable floor below it; Forward points up the
        /// wall. Where the dock's evening raises 0.5's last pillar.
        /// </summary>
        public const string RegionTerraces = "region.terraces";

        /// <summary>
        /// Prefix of the crew's sealed supply crates (M3-25 T4), one per salvage site beside its heart:
        /// <c>crate.</c> + the site's name (depot, kestrel, drill, garage, lander). Forward faces on from home.
        /// </summary>
        public const string CratePrefix = "crate.";

        /// <summary>
        /// The basin's old vent and its field of orange glass beads (M3-27, Apollo 17's orange soil): seen from 0.5,
        /// dug in 0.6. The radius covers the bead field.
        /// </summary>
        public const string PlaceOrangeVent = "place.orange_vent";

        /// <summary>The dark basalt band on the rim terraces where 0.6's Core Drill finds ilmenite (M3-27).</summary>
        public const string PlaceIlmeniteBand = "place.ilmenite_band";

        /// <summary>The permanently shadowed crater whose floor holds 0.7's water ice (M3-27).</summary>
        public const string PlaceShadowedCrater = "place.shadowed_crater";
    }
}
