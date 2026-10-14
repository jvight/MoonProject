namespace MoonProject.Core.Events
{
    /// <summary>
    /// The belly lift settled 07 in the crew's lab (docs/features/M3-19). Rover moves to the ring-rail camera, UI hides
    /// the HUD, Audio swaps to the lab's room tone. Published by Gameplay.
    /// </summary>
    public readonly struct LabEntered
    {
        public LabEntered(LabStation facing)
        {
            Facing = facing;
        }

        /// <summary>The station the turntable faces on arrival (the one that wants what 07 carries, or the chart).</summary>
        public LabStation Facing { get; }
    }

    /// <summary>
    /// The platform started down out of the lab (docs/features/M3-19): Rover eases back along the crane path, UI shows
    /// the HUD again, Audio lets the room tone go. Published by Gameplay.
    /// </summary>
    public readonly struct LabLeft
    {
    }

    /// <summary>The turntable came to rest facing <see cref="Station"/> (docs/features/M3-19 §5).</summary>
    public readonly struct LabStationFaced
    {
        public LabStationFaced(LabStation station)
        {
            Station = station;
        }

        public LabStation Station { get; }
    }

    /// <summary>
    /// 07 looks at the station the turntable faces, or comes away from it (docs/features/M3-19 §5, §6): Rover eases the
    /// ring camera in to the station and back, UI swaps the lab's prompts. Looking at the survey chart also opens the
    /// map (<see cref="MapLookStarted"/>). Published by Gameplay.
    /// </summary>
    public readonly struct LabStationLooked
    {
        public LabStationLooked(LabStation station, bool looking)
        {
            Station = station;
            Looking = looking;
        }

        public LabStation Station { get; }

        public bool Looking { get; }
    }
}
