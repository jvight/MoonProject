namespace MoonProject.Core
{
    /// <summary>
    /// What a line of 07's boot readout says about a <see cref="RoverSystem"/> (docs/design/opening-v2.md §10).
    /// </summary>
    public enum RoverSystemState
    {
        /// <summary>Works.</summary>
        Ok = 0,

        /// <summary>Broken by a missing part (the cutter: FAULT · NO IGNITER).</summary>
        Fault = 1,

        /// <summary>Stuck (the solar wing: JAMMED).</summary>
        Jammed = 2,

        /// <summary>Hears, cannot answer (the antenna: RECEIVE ONLY).</summary>
        ReceiveOnly = 3,

        /// <summary>Nothing fitted (sockets 1-2: EMPTY).</summary>
        Empty = 4,
    }
}
