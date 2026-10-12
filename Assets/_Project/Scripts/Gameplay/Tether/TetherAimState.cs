namespace MoonProject.Gameplay
{
    /// <summary>What the tether is doing, for the UI reticle.</summary>
    public enum TetherAimState
    {
        /// <summary>Nothing in the aim cone.</summary>
        Idle = 0,

        /// <summary>A relic or drag piece is highlighted: pressing Tether latches onto it.</summary>
        Hovering = 1,

        /// <summary>A relic or drag piece is on the tether.</summary>
        Towing = 2,

        /// <summary>
        /// No tether yet (Kenji's garage port fits the ring, first-hour v2 §3): no aim, no reticle, no icon, whatever
        /// the button does.
        /// </summary>
        Asleep = 3,
    }
}
