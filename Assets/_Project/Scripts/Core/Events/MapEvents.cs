using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// A place's light was seen for the first time this save (docs/features/M3-07): the lookout's sweep, a sonar answer
    /// or the dock's evening pillar. Published by Gameplay once per save per place; the map draws a pencil ring.
    /// </summary>
    public readonly struct MapPlaceSeen
    {
        public MapPlaceSeen(string placeId, Vector3 position)
        {
            PlaceId = placeId;
            Position = position;
        }

        /// <summary>The place's id (<see cref="Map.MapPlace.Id"/>).</summary>
        public string PlaceId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>07 reached a place for the first time this save (docs/features/M3-07). Published by Gameplay once.</summary>
    public readonly struct MapPlaceVisited
    {
        public MapPlaceVisited(string placeId, Vector3 position)
        {
            PlaceId = placeId;
            Position = position;
        }

        public string PlaceId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// The deck hatch's view opened on the crew's map (docs/features/M3-07): UI shows the map, Rover holds 07 still with
    /// its lens on the hatch window at <see cref="Window"/>. Published by Gameplay's lift flow.
    /// </summary>
    public readonly struct MapLookStarted
    {
        public MapLookStarted(Vector3 window, bool firstLook)
        {
            Window = window;
            FirstLook = firstLook;
        }

        /// <summary>World position of the hatch window the map is seen through.</summary>
        public Vector3 Window { get; }

        /// <summary>The first look this save: the view drifts to the paper's edge after the ink (step 3).</summary>
        public bool FirstLook { get; }
    }

    /// <summary>The map's view closed (any input): UI hides the map, Rover eases back to the deck.</summary>
    public readonly struct MapLookEnded
    {
    }

    /// <summary>
    /// The deck hatch is open (docs/features/M3-07, cutscenes §6 shot 4) and stays open. Published by Gameplay once
    /// when its motor swings it open on the first ride (Audio plays the motor, Art swings the hatch and lights the
    /// table behind it), and at load when the save already has it open (<see cref="Instant"/>: no sound, no swing).
    /// </summary>
    public readonly struct MapHatchOpened
    {
        public MapHatchOpened(Vector3 position, bool instant)
        {
            Position = position;
            Instant = instant;
        }

        /// <summary>World position of the hatch window.</summary>
        public Vector3 Position { get; }

        /// <summary>Restored from the save: show it open at once.</summary>
        public bool Instant { get; }
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
