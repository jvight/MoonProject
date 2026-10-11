using System;

namespace MoonProject.Core.Map
{
    /// <summary>
    /// What 07 did at a place: the tiny icons the crew's hand draws beside an inked name (docs/features/M3-07).
    /// </summary>
    [Flags]
    public enum MapPlaceDeeds
    {
        None = 0,

        /// <summary>A relic was carried home from here.</summary>
        Relic = 1 << 0,

        /// <summary>A friend was found here.</summary>
        Friend = 1 << 1,

        /// <summary>A blueprint tube was found here.</summary>
        Blueprint = 1 << 2,

        /// <summary>The mast here was lit.</summary>
        Mast = 1 << 3,
    }
}
