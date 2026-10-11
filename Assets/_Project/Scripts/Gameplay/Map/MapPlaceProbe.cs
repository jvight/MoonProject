using System;
using MoonProject.Core.Map;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// How the crew's map reads one place from the rest of Gameplay's state (docs/features/M3-07): each part is
    /// optional and built once at load, so reading it never allocates.
    /// </summary>
    public readonly struct MapPlaceProbe
    {
        public MapPlaceProbe(Func<bool> seen, Func<bool> visited, Func<MapPlaceDeeds> deeds, Func<string> stateLine,
            bool visitable = true)
        {
            Seen = seen;
            Visited = visited;
            Deeds = deeds;
            StateLine = stateLine;
            Visitable = visitable;
        }

        /// <summary>Its light was seen: a site's or friend's answer, the dock's evening pillar.</summary>
        public Func<bool> Seen { get; }

        /// <summary>
        /// The saved state shows 07 was there (a piece cut, a mast restored, a friend stitched): also how a save from
        /// before the map learns what was visited.
        /// </summary>
        public Func<bool> Visited { get; }

        public Func<MapPlaceDeeds> Deeds { get; }

        /// <summary>The card's "what is still there" key, or null.</summary>
        public Func<string> StateLine { get; }

        /// <summary>False beyond the rim: coming near never makes it visited (it stays a pencil note in 0.5).</summary>
        public bool Visitable { get; }
    }
}
