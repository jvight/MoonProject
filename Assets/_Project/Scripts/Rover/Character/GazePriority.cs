namespace MoonProject.Rover
{
    /// <summary>
    /// Who wants 07's attention. The highest priority with an active target wins; with none, 07 looks where it is
    /// going (or, when left alone, up at Earth).
    /// </summary>
    public enum GazePriority
    {
        /// <summary>Passing interest: glinting scrap nearby.</summary>
        Glance = 0,

        /// <summary>Something answered: a pinged relic.</summary>
        Interest = 1,

        /// <summary>Hands-on: the tether target or the relic being excavated.</summary>
        Focus = 2,
    }
}
