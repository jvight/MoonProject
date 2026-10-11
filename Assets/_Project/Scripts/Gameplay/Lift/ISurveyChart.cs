namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The lab's survey chart as the lift and the step flow see it (docs/features/M3-19 §5, M3-07, M3-21): the crew's
    /// map, which the map system connects once it is up.
    /// </summary>
    internal interface ISurveyChart
    {
        /// <summary>07 has looked at the map at least once in this save.</summary>
        bool Looked { get; }

        /// <summary>The map has something new to show: never looked at, or ink drawn in since the last look.</summary>
        bool HasNews { get; }

        /// <summary>
        /// The station has seen place <paramref name="placeId"/> (from home, the sonar or a visit): the next line may
        /// name it (docs/features/M3-21 §3).
        /// </summary>
        bool HasSeen(string placeId);
    }
}
