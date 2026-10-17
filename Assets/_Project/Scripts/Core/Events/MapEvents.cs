using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// The view opened on the crew's map (docs/features/M3-07): UI shows the map, Rover holds 07 still and frames
    /// <see cref="Position"/>. Published by Gameplay's map system when its trigger opens the look (where the map hangs
    /// is decided by the lander's cabin spec); Audio warms the light table's hum.
    /// </summary>
    public readonly struct MapLookStarted
    {
        public MapLookStarted(Vector3 position, bool firstLook)
        {
            Position = position;
            FirstLook = firstLook;
        }

        /// <summary>World position of the map (its light table).</summary>
        public Vector3 Position { get; }

        /// <summary>The first look this save: the view drifts to the paper's edge after the ink (step 3).</summary>
        public bool FirstLook { get; }
    }

    /// <summary>The map's view closed: UI hides the map, Rover eases back.</summary>
    public readonly struct MapLookEnded
    {
    }

    /// <summary>
    /// The chart pen drew one place (docs/features/M3-07 steps 1 and 4): Audio plays the pen's scratch, one step up the
    /// pentatonic scale per <see cref="Order"/>; UI animates the stroke. Published in ink order, a pen's stroke apart.
    /// </summary>
    public readonly struct MapInkCued
    {
        public MapInkCued(string placeId, Vector3 position, int order)
        {
            PlaceId = placeId;
            Position = position;
            Order = order;
        }

        public string PlaceId { get; }

        public Vector3 Position { get; }

        /// <summary>0 for the first place the pen draws in this look, then 1, 2...</summary>
        public int Order { get; }
    }
}
