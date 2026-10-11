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
    /// The lab's reader finished decoding the blueprint tube of kit <see cref="KitId"/> (docs/features/M3-19 §5): the
    /// paper tape stops and the board's sheet appears. Published by Gameplay; the tech tree marks the node decoded
    /// (docs/features/M3-21).
    /// </summary>
    public readonly struct FindDecoded
    {
        public FindDecoded(string kitId)
        {
            KitId = kitId;
        }

        public string KitId { get; }
    }

    /// <summary>
    /// The decode's hand-off began (docs/features/M3-19 §5): a lamp pulse runs from the lab down the lander's leg and
    /// along the ground cable to Kenji's Rover Bay over <see cref="Seconds"/>, and the plotter starts drawing as it
    /// arrives (or when the bay wakes, if it is still asleep). World and Art light the pulse along the art's
    /// HandOffPath. Published by Gameplay.
    /// </summary>
    public readonly struct DecodeHandOff
    {
        public DecodeHandOff(string kitId, float seconds)
        {
            KitId = kitId;
            Seconds = seconds;
        }

        public string KitId { get; }

        /// <summary>How long the pulse takes from the reader to the bay.</summary>
        public float Seconds { get; }
    }
}
