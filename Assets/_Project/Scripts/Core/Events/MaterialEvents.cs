using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>07's hold changed (a bundle in, an unload, a spend, a kit's new slots).</summary>
    public readonly struct CargoChanged
    {
        public CargoChanged(int usedSlots, int slotCount)
        {
            UsedSlots = usedSlots;
            SlotCount = slotCount;
        }

        public int UsedSlots { get; }

        public int SlotCount { get; }
    }

    /// <summary>
    /// A bundle did not fit in 07's hold (M3-25 §2): it settles at <see cref="Position"/> as a glowing pickup and waits.
    /// </summary>
    public readonly struct CargoOverflowed
    {
        public CargoOverflowed(SalvageMaterial material, int unitsLeft, Vector3 position)
        {
            Material = material;
            UnitsLeft = unitsLeft;
            Position = position;
        }

        public SalvageMaterial Material { get; }

        /// <summary>Units in the waiting bundle (what did not fit).</summary>
        public int UnitsLeft { get; }

        public Vector3 Position { get; }
    }

    /// <summary>A waiting bundle was taken into 07's hold later, now that there was room.</summary>
    public readonly struct CargoPickupTaken
    {
        public CargoPickupTaken(SalvageMaterial material, int units)
        {
            Material = material;
            Units = units;
        }

        public SalvageMaterial Material { get; }

        public int Units { get; }
    }

    /// <summary>07 unloaded at the dock onto home's shelves (M3-25 §3.2), with no input.</summary>
    public readonly struct CargoUnloaded
    {
        public CargoUnloaded(int unitsMoved, int unitsKept, bool homeFull)
        {
            UnitsMoved = unitsMoved;
            UnitsKept = unitsKept;
            HomeFull = homeFull;
        }

        public int UnitsMoved { get; }

        /// <summary>Units that stayed in 07 because home had no room.</summary>
        public int UnitsKept { get; }

        public bool HomeFull { get; }
    }

    /// <summary>Home's shelves changed (an unload, a fetch, a spend, a new shelf).</summary>
    public readonly struct HomeStorageChanged
    {
        public HomeStorageChanged(int usedSlots, int slotCount)
        {
            UsedSlots = usedSlots;
            SlotCount = slotCount;
        }

        public int UsedSlots { get; }

        public int SlotCount { get; }
    }

    /// <summary>A shelf was built on a free socket at home (M3-25 §3.1); its slots appear on the cargo screen.</summary>
    public readonly struct ShelfBuilt
    {
        public ShelfBuilt(int socketIndex, int tier)
        {
            SocketIndex = socketIndex;
            Tier = tier;
        }

        /// <summary>The shelf's index in <see cref="IHomeStorage"/>.</summary>
        public int SocketIndex { get; }

        public int Tier { get; }
    }

    /// <summary>Tilly set a waiting bundle on home's shelves (M3-25 T3).</summary>
    public readonly struct BundleFetched
    {
        public BundleFetched(SalvageMaterial material, int units)
        {
            Material = material;
            Units = units;
        }

        public SalvageMaterial Material { get; }

        public int Units { get; }
    }

    /// <summary>
    /// A crew supply crate was cut open (M3-25 T4): its stack goes to the hold like a bundle and the packing note
    /// shows in the crew member's hand.
    /// </summary>
    public readonly struct CrewCrateOpened
    {
        public CrewCrateOpened(string siteId, string crewId, SalvageMaterial material, int units, Vector3 position)
        {
            SiteId = siteId;
            CrewId = crewId;
            Material = material;
            Units = units;
            Position = position;
        }

        /// <summary>The site whose heart it sat beside ("site.depot", ...).</summary>
        public string SiteId { get; }

        /// <summary>Whose hand packed it ("kenji", ...); its packing note's key is "crate.&lt;site&gt;.note".</summary>
        public string CrewId { get; }

        public SalvageMaterial Material { get; }

        public int Units { get; }

        public Vector3 Position { get; }
    }

    /// <summary>A home building refined a material, with no input (M3-27, 0.6).</summary>
    public readonly struct MaterialRefined
    {
        public MaterialRefined(SalvageMaterial material, int units, string building)
        {
            Material = material;
            Units = units;
            Building = building;
        }

        public SalvageMaterial Material { get; }

        public int Units { get; }

        /// <summary>The building's id ("smelter", ...).</summary>
        public string Building { get; }
    }

    /// <summary>07's cutter tier changed (M3-26): a kit was fitted, or a load restored it.</summary>
    public readonly struct CutterTierChanged
    {
        public CutterTierChanged(int tier, bool restored)
        {
            Tier = tier;
            Restored = restored;
        }

        public int Tier { get; }

        /// <summary>True on a load: show the tier at once, with no install moment.</summary>
        public bool Restored { get; }
    }
}
