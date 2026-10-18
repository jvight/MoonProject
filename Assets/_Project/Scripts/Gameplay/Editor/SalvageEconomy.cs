using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The salvage content (docs/features/M3-13, opening-v2 §5): the materials of this version and the opening's one
    /// wreck, the Kestrel impact, with its pieces' sizes. The content builder writes it into the material and salvage
    /// catalogs.
    /// </summary>
    internal static class SalvageEconomy
    {
        /// <summary>
        /// Units of a salvage bundle one slot holds (docs/features/M3-25 §2.2): ten, so the opening's handfuls sit in one
        /// slot each instead of splitting across the hold's eight.
        /// </summary>
        public const int SalvageStack = 10;

        /// <summary>Units of a lunar ore one slot holds (M3-27: beads and ores pack denser than bundles).</summary>
        public const int OreStack = 12;

        /// <summary>The opening's wreck (Art's Site_kestrel on site.kestrel).</summary>
        public const string Kestrel = "kestrel";

        /// <summary>
        /// The materials of this version (docs/features/M3-27 §8): the three salvage materials, and the basin's orange
        /// beads, seen in their vent field but not gathered yet.
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

        /// <summary>The Kestrel's piece sizes (docs/features/M3-26 §4.2), dealt biggest to biggest: large
        /// plates.</summary>
        public static IReadOnlyList<PieceSize> KestrelPieceClasses { get; } = new[]
        {
            PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Medium, PieceSize.Large, PieceSize.Large,
            PieceSize.Large,
        };

        /// <summary>The site id of <paramref name="name"/> (its World anchor).</summary>
        public static string SiteId(string name)
        {
            return WorldAnchorIds.SitePrefix + name;
        }
    }
}
