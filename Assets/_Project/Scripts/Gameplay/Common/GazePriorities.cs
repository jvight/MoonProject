namespace MoonProject.Gameplay
{
    /// <summary>
    /// Priorities gameplay passes to <c>IRoverRig.SetGazeTarget</c>, matching the rover's attention ladder: higher
    /// wins, so 07 glances at scrap, turns to a relic that answered, and fixes on what it is working on.
    /// </summary>
    public static class GazePriorities
    {
        /// <summary>Passing interest: glinting scrap nearby.</summary>
        public const int Glance = 0;

        /// <summary>Something answered or is aimed at: a pinged relic, the tether's aimed target.</summary>
        public const int Interest = 1;

        /// <summary>Hands-on: the relic being excavated or towed.</summary>
        public const int Focus = 2;
    }
}
