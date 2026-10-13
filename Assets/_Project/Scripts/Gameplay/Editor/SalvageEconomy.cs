using System;
using System.Collections.Generic;
using MoonProject.Art.Editor;
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

        /// <summary>The Focus Lens (M3-26 §3): a ringed lens on the emitter, its glass and its coil.</summary>
        public static Recipe FocusLens => new Recipe(0, 1, 3);

        /// <summary>The Side Bins (M3-25 §2.2): two low metal panniers and their latch wiring.</summary>
        public static Recipe SideBins => new Recipe(3, 1, 0);

        /// <summary>
        /// The radio tower's three levels, more each time: L1 from the depot alone, L2 once the garage is cut, and L3,
        /// the big one (its lift winch is heavy metal), from the Kestrel's optics and what the rest of the basin gives.
        /// </summary>
        public static IReadOnlyList<Recipe> RadioTower { get; } = new[]
        {
            new Recipe(1, 1, 1), new Recipe(1, 3, 1), new Recipe(4, 5, 6),
        };

        /// <summary>Units of a salvage bundle one slot holds (docs/features/M3-25 §2.2).</summary>
        public const int SalvageStack = 4;

        /// <summary>Units of a lunar ore one slot holds (M3-27: beads and ores pack denser than bundles).</summary>
        public const int OreStack = 6;

        /// <summary>
        /// The materials of this version (docs/features/M3-27 §8): the three salvage materials, and the basin's orange
        /// beads, seen in their vent field but not gathered until the dig on bead patches (0.6).
        /// </summary>
        public static MaterialEntry[] Materials()
        {
            return new[]
            {
                new MaterialEntry(SalvageMaterial.Metal, "metal", MaterialFamily.Salvage, SalvageStack, true),
                new MaterialEntry(SalvageMaterial.Wiring, "wiring", MaterialFamily.Salvage, SalvageStack, true),
                new MaterialEntry(SalvageMaterial.Optics, "optics", MaterialFamily.Salvage, SalvageStack, true),
                new MaterialEntry(SalvageMaterial.OrangeBeads, "orange_beads", MaterialFamily.Lunar, OreStack, false),
            };
        }

        /// <summary>The salvage sites in catalog order (Art's Site_&lt;name&gt; on site.&lt;name&gt;).</summary>
        public static IReadOnlyList<string> SiteNames { get; } = new[]
        {
            "depot", "kestrel", "drill", "garage", "lander",
        };

        /// <summary>
        /// Kenji's garage has three rich seams (first-hour v2 §4: one trip from a full battery), each worth this many
        /// times its size's units, so the 2x economy holds.
        /// </summary>
        public const int GarageYieldScale = 2;

        /// <summary>How many times its size's units each piece of site <paramref name="name"/> yields.</summary>
        public static int YieldScale(string name)
        {
            return name == "garage" ? GarageYieldScale : 1;
        }

        /// <summary>
        /// Each site's piece sizes (docs/features/M3-26 §4.2), dealt biggest to biggest: the depot small and snappy
        /// (nothing over ~3.2 s in the first 30 minutes), the drill's first two-segment plate at ~40 min, the Kestrel's
        /// large plates, and the lander's first hull plate at ~60 min, when the Focus Lens arrives.
        /// </summary>
        public static IReadOnlyList<PieceSize> PieceClasses(string name)
        {
            switch (name)
            {
                case "depot":
                    return new[] { PieceSize.Small, PieceSize.Small, PieceSize.Medium, PieceSize.Medium };
                case "garage":
                    return new[] { PieceSize.Medium, PieceSize.Medium, PieceSize.Medium };
                case "drill":
                    return new[]
                    {
                        PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Large,
                    };
                case "kestrel":
                    return new[]
                    {
                        PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Large,
                        PieceSize.Large, PieceSize.Large,
                    };
                case "lander":
                    return new[]
                    {
                        PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Large,
                        PieceSize.Large, PieceSize.Hull,
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "No such salvage site.");
            }
        }

        /// <summary>Units of the site's signature material in each crew supply crate (docs/features/M3-25 T4).</summary>
        public const int CrateUnits = 6;

        /// <summary>
        /// Each site's crew supply crate (M3-25 T4): whose hand packed it and its signature material. Its packing note
        /// is "crate.&lt;site&gt;.note" (story).
        /// </summary>
        public static SupplyCrate Crate(string name)
        {
            switch (name)
            {
                case "depot":
                    return new SupplyCrate("theo", SalvageMaterial.Metal, CrateUnits);
                case "garage":
                    return new SupplyCrate("kenji", SalvageMaterial.Wiring, CrateUnits);
                case "drill":
                    return new SupplyCrate("ro", SalvageMaterial.Metal, CrateUnits);
                case "kestrel":
                    return new SupplyCrate("ines", SalvageMaterial.Optics, CrateUnits);
                case "lander":
                    return new SupplyCrate("kenji", SalvageMaterial.Metal, CrateUnits);
                default:
                    throw new ArgumentOutOfRangeException(nameof(name), name, "No such salvage site.");
            }
        }

        /// <summary>The site whose last cut reveals its heart (M3-16 P10: the supply depot).</summary>
        public const string HeartUnderPiecesSite = "depot";

        /// <summary>The site standing past the Hover-Jump gate (the canyon supply lander).</summary>
        public const string GatedSite = "lander";

        /// <summary>
        /// The size of each of Kestrel-3's four hull fragments in its scrap field (0.5.5 §2): small plates, a handful
        /// of Metal on the way to the wreck.
        /// </summary>
        public const PieceSize TrailScrapSize = PieceSize.Small;

        /// <summary>The haul bot's id: it stands inside the drill site, on no anchor of its own.</summary>
        public const string HaulBot = "derelict.haul_bot";

        /// <summary>
        /// The derelicts left on the routes (0.5.5 §1.7), in catalog order (saves keep it, append only): each one's
        /// id, the World anchor it stands on (or the wreck it stands inside, on the wreck's node), and Art's robot.
        /// </summary>
        public static IReadOnlyList<(string Id, string AnchorId, string HostSocket, string Prefab)> Derelicts { get; } =
            new[]
            {
                (WorldAnchorIds.Micro06, WorldAnchorIds.Micro06, string.Empty, DerelictBuilder.ElderName),
                (WorldAnchorIds.MicroMule, WorldAnchorIds.MicroMule, string.Empty, DerelictBuilder.MuleName),
                (WorldAnchorIds.MicroDrone, WorldAnchorIds.MicroDrone, string.Empty, DerelictBuilder.DroneName),
                (HaulBot, SiteId("drill"), SiteModelBuilder.HaulBotSocketName, DerelictBuilder.HaulBotName),
            };

        /// <summary>
        /// A derelict's piece sizes: a small cut where Art gives it one (the Mule's track skirt, the drone's camera
        /// boom, the haul bot's strapped crate and cable drum); 06 has nothing to cut.
        /// </summary>
        public static IReadOnlyList<PieceSize> DerelictPieceClasses(string id)
        {
            switch (id)
            {
                case WorldAnchorIds.Micro06:
                    return Array.Empty<PieceSize>();
                case WorldAnchorIds.MicroMule:
                case WorldAnchorIds.MicroDrone:
                    return new[] { PieceSize.Small };
                case HaulBot:
                    return new[] { PieceSize.Small, PieceSize.Small };
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "No such derelict.");
            }
        }

        /// <summary>The site id of <paramref name="name"/> (its World anchor).</summary>
        public static string SiteId(string name)
        {
            return WorldAnchorIds.SitePrefix + name;
        }
    }
}
