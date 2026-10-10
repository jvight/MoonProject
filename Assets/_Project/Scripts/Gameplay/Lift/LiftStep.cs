namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// What one step of the lift's cycle started or finished (the station plays these as beats; Audio's sound beats
    /// are Core's <see cref="MoonProject.Core.Events.LiftCue"/>).
    /// </summary>
    public enum LiftStep
    {
        None = 0,

        /// <summary>The tower reached its third level: the lift is powered, still jammed.</summary>
        Powered = 1,

        /// <summary>07 rests on the feed pad with enough materials: spend them and begin the feed.</summary>
        FeedStarted = 2,

        /// <summary>The stitch finished: platform level, cable taut, winch lamp lit. Save it.</summary>
        Restored = 3,

        /// <summary>07 rests on the platform: hold it and rise.</summary>
        RideStarted = 4,

        /// <summary>At the deck and settled: open the lookout.</summary>
        ReachedTop = 5,

        /// <summary>The player gave input at the top: go back down.</summary>
        Descending = 6,

        /// <summary>At the bottom and settled: let 07 go.</summary>
        ReachedBottom = 7,

        /// <summary>The platform reached a stop, top or bottom; it settles there before the reached step.</summary>
        Arrived = 8,
    }
}
