using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The salvage economy's content (docs/features/M3-13): what 07 crafts at the stations takes, and where the
    /// materials come from (the wreck sites). The content builder writes it into the upgrade definitions
    /// and the salvage catalog, and the economy test holds the recipes against the yield (VISION ruling 5, M3-16 P9:
    /// the moon gives about twice what steps 0-8 take, each site leaning on a signature material). The relay masts'
    /// recipes live in the relay tuning.
    /// </summary>
    internal static class SalvageEconomy
    {
        /// <summary>Hover-Jump: its coils (metal and wiring) and its charge lamp (optics).</summary>
        public static Recipe HoverJump => new Recipe(4, 2, 1);

        /// <summary>The Cargo Cradle (M3-11): a strapped rack basket, almost all metal.</summary>
        public static Recipe CargoCradle => new Recipe(4, 0, 0);

        /// <summary>The Warm Headlamp (M3-11): a caged lamp bar, lenses and lamps on a metal cage.</summary>
        public static Recipe WarmHeadlamp => new Recipe(1, 0, 3);

        /// <summary>The Boost Coils (M3-11): wound coils in two capacitor drums with a glow window.</summary>
        public static Recipe BoostCoils => new Recipe(1, 2, 1);

        /// <summary>Cell Pack II (M3-20): two cream cells in a metal spine, their contacts and gauge lenses.</summary>
        public static Recipe CellPack => new Recipe(3, 0, 2);

        /// <summary>
        /// The radio tower's three levels, more each time: L1 from the depot alone, L2 once the garage is cut, and L3,
        /// the big one (its lift winch is heavy metal), from the Kestrel's optics and what the rest of the basin gives.
        /// </summary>
        public static IReadOnlyList<Recipe> RadioTower { get; } = new[]
        {
            new Recipe(1, 1, 1), new Recipe(1, 3, 1), new Recipe(4, 5, 6),
        };

        /// <summary>The salvage sites in catalog order (Art's Site_&lt;name&gt; on site.&lt;name&gt;).</summary>
        public static IReadOnlyList<string> SiteNames { get; } = new[]
        {
            "depot", "kestrel", "drill", "garage", "lander",
        };

        /// <summary>The site whose last cut reveals its heart (M3-16 P10: the supply depot).</summary>
        public const string HeartUnderPiecesSite = "depot";

        /// <summary>The site standing past the Hover-Jump gate (the canyon supply lander).</summary>
        public const string GatedSite = "lander";

        /// <summary>The site id of <paramref name="name"/> (its World anchor).</summary>
        public static string SiteId(string name)
        {
            return WorldAnchorIds.SitePrefix + name;
        }
    }
}
