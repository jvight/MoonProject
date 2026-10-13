namespace MoonProject.Gameplay
{
    /// <summary>Where a part goes onto 07 (docs/design/0.5.5-owner-feedback.md §3.5).</summary>
    public enum PartStation
    {
        /// <summary>Kenji's Rover Bay: crafted, waits in the parts row, fitted by hand on the turntable.</summary>
        Bay = 0,

        /// <summary>Kenji's garage port: his found parts, fitted by the port's arm in its own moment.</summary>
        Port = 1,

        /// <summary>A friend's gift: it fits itself the next time 07 is home.</summary>
        Gift = 2,
    }
}
