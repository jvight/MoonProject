using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The salvage economy's content (docs/features/M3-13): what 07 crafts at the stations takes, and where the
    /// materials come from (the sites and the trail bits). The content builder writes it into the upgrade definitions
    /// and the salvage catalog, and the economy test holds the recipes against the yield (VISION ruling 5: the sites
    /// give at least twice every recipe). The relay masts' recipes live in the relay tuning.
    /// </summary>
    internal static class SalvageEconomy
    {
        /// <summary>Hover-Jump: its coils (metal and wiring) and its charge lamp (optics).</summary>
        public static Recipe HoverJump => new Recipe(4, 2, 1);

        /// <summary>The radio tower's three levels: wiring and optics, a little more each time.</summary>
        public static IReadOnlyList<Recipe> RadioTower { get; } = new[]
        {
            new Recipe(0, 1, 1), new Recipe(0, 2, 1), new Recipe(0, 2, 2),
        };

        /// <summary>The salvage sites in catalog order (Art's Site_&lt;name&gt; on site.&lt;name&gt;).</summary>
        public static IReadOnlyList<string> SiteNames { get; } = new[]
        {
            "depot", "kestrel", "drill", "garage", "lander",
        };

        /// <summary>The site standing past the Hover-Jump gate (the canyon supply lander).</summary>
        public const string GatedSite = "lander";

        /// <summary>
        /// Kestrel-3's loose trail bits in catalog order (Art's Debris_&lt;Material&gt;_&lt;n&gt;), the materials
        /// taking turns down the trail.
        /// </summary>
        public static IReadOnlyList<string> TrailBits { get; } = new[]
        {
            "Debris_Metal_0", "Debris_Optics_0", "Debris_Wiring_0", "Debris_Metal_1", "Debris_Optics_1",
        };

        /// <summary>Metres along the trail anchor's forward that the trail bits spread over.</summary>
        public const float TrailLength = 84f;

        /// <summary>The site id of <paramref name="name"/> (its World anchor).</summary>
        public static string SiteId(string name)
        {
            return WorldAnchorIds.SitePrefix + name;
        }
    }
}
