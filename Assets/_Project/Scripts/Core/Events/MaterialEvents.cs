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

        /// <summary>
        /// Whose hand packed it ("kenji", ...). Its packing note is keyed by site.
        /// </summary>
        public string CrewId { get; }

        public SalvageMaterial Material { get; }

        public int Units { get; }

        public Vector3 Position { get; }
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
