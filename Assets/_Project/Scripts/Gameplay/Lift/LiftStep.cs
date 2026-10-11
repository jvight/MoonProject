namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// What one step of the lift's cycle started or finished (the station plays these as beats; Audio's sound beats
    /// are Core's <see cref="MoonProject.Core.Events.LiftCue"/>).
    /// </summary>
    public enum LiftStep
    {
        None = 0,

        /// <summary>Home has power: the lift wakes jammed.</summary>
        Powered = 1,

        /// <summary>07 came home carrying something for the lab: light the pad and ask for the stitch.</summary>
        Called = 2,

        /// <summary>07 rests on the pad: its beam starts the stitch.</summary>
        StitchStarted = 3,

        /// <summary>The stitch finished: the platform is level and the cable taut. Save it.</summary>
        Restored = 4,

        /// <summary>07 rests on the platform: hold it, open the hatch and rise.</summary>
        RideStarted = 5,

        /// <summary>Settled in the lab: the turntable takes over.</summary>
        ReachedInside = 6,

        /// <summary>The player asked to go down: leave the lab.</summary>
        Descending = 7,

        /// <summary>Down and settled: let 07 go.</summary>
        ReachedBottom = 8,

        /// <summary>The platform reached a stop, top or bottom; it settles there before the reached step.</summary>
        Arrived = 9,
    }
}
