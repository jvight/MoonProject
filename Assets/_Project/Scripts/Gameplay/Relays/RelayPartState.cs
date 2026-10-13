namespace MoonProject.Gameplay
{
    /// <summary>Where a relay mast's part is.</summary>
    internal enum RelayPartState
    {
        /// <summary>In its mast's service cabinet: sealed in, then glinting behind the open door.</summary>
        Resting = 0,

        /// <summary>Drawn in toward 07.</summary>
        Flying = 1,

        /// <summary>07 carries it.</summary>
        Held = 2,

        /// <summary>Gliding along 07's beam into the mast's part socket.</summary>
        Installing = 3,

        /// <summary>Slotted into the junction box.</summary>
        Installed = 4,
    }
}
