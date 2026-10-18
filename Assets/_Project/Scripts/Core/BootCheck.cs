namespace MoonProject.Core
{
    /// <summary>
    /// The lines of 07's power-on self-test, in the order the New Game wake runs them. Rover publishes a
    /// <see cref="Events.BootCheckStarted"/> and a <see cref="Events.BootCheckCompleted"/> for each; UI prints the CRT
    /// line and Audio voices it, without Rover knowing either.
    /// </summary>
    public enum BootCheckStep
    {
        /// <summary>Black, a faint carrier tone: why 07 woke.</summary>
        Carrier = 0,

        /// <summary>The lens opens: 07 resumes where it stopped.</summary>
        Resume = 1,

        /// <summary>The restored agenda: what 07 has to do.</summary>
        Agenda = 2,

        /// <summary>The iris focuses.</summary>
        Lens = 3,

        /// <summary>The lamps flicker, then come on.</summary>
        Lamps = 4,

        /// <summary>The neck and head sweep and nod.</summary>
        Head = 5,

        /// <summary>The four wheels twitch in turn (one is mismatched).</summary>
        Wheels = 6,

        /// <summary>The motor shudders and 07 creeps forward a little.</summary>
        Motor = 7,

        /// <summary>The cutter's turret tries to ignite and fails: its igniter is missing.</summary>
        Cutter = 8,

        /// <summary>The solar wing jerks half open, is stuck, and folds back.</summary>
        SolarWing = 9,

        /// <summary>The antenna rises and hears the orbit but cannot answer.</summary>
        Antenna = 10,

        /// <summary>The cell lamps fill to half.</summary>
        Battery = 11,

        /// <summary>The assembly system reports no blueprints.</summary>
        Assembly = 12,

        /// <summary>07 turns toward the lander's beacon, shivers the dust off and the recovery begins.</summary>
        Begin = 13,
    }

    /// <summary>How a <see cref="BootCheckStep"/> came out.</summary>
    public enum BootCheckResult
    {
        /// <summary>The part works.</summary>
        Ok = 0,

        /// <summary>Broken by a missing part (the cutter).</summary>
        Fault = 1,

        /// <summary>Stuck (the solar wing).</summary>
        Jammed = 2,

        /// <summary>Hears but cannot answer (the antenna).</summary>
        ReceiveOnly = 3,

        /// <summary>A line of information, not a check (the carrier, the resumed day, the agenda, the assembly, the start).</summary>
        Info = 4,
    }
}
