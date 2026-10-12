using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>A material catalog and stock as the game builds them, for EditMode tests (destroy the catalog after).</summary>
    internal static class CargoFixtures
    {
        public const int HoldSlots = 8;
        public const int ShelfSlots = 12;
        public const int BuiltInShelves = 2;

        /// <summary>Metal, Wiring and Optics in stacks of 4; Orange Beads in stacks of 6, seen but not gathered.</summary>
        public static MaterialCatalog Catalog()
        {
            var catalog = ScriptableObject.CreateInstance<MaterialCatalog>();
            catalog.Populate(new[]
            {
                new MaterialEntry(SalvageMaterial.Metal, "metal", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Wiring, "wiring", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Optics, "optics", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.OrangeBeads, "orange_beads", MaterialFamily.Lunar, 6, false),
            });
            return catalog;
        }

        /// <summary>An empty stock: an 8-slot hold, two built-in shelves of 12 and two free sockets.</summary>
        public static MaterialStock Stock(EventBus events, IMaterialCatalog catalog)
        {
            var hold = new CargoHold(events, catalog, HoldSlots);
            var home = new HomeStorage(events, catalog, new[] { ShelfSlots, ShelfSlots, ShelfSlots, ShelfSlots },
                BuiltInShelves);
            return new MaterialStock(events, hold, home);
        }
    }
}
