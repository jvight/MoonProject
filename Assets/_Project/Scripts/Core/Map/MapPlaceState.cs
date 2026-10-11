namespace MoonProject.Core.Map
{
    /// <summary>
    /// What the crew's map knows of a place (docs/features/M3-07), in order: each state only ever moves forward.
    /// Inked is not a state: a place is inked while it is <see cref="Visited"/> and inside the station's reach
    /// (<see cref="IStationReach.IsInReach"/>).
    /// </summary>
    public enum MapPlaceState
    {
        /// <summary>Only Ines's pencil sketch: the crew drew it, 07 has not seen it yet.</summary>
        Sketched = 0,

        /// <summary>Its light was seen (the lookout's sweep, a sonar answer, the dock's evening pillar), not reached.</summary>
        Seen = 1,

        /// <summary>07 has been there.</summary>
        Visited = 2,
    }
}
