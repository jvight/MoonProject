namespace MoonProject.Gameplay
{
    /// <summary>What happened when the UI asked to buy an upgrade level.</summary>
    public enum PurchaseResult
    {
        /// <summary>Bought: scrap spent, level raised, effects applied.</summary>
        Purchased = 0,

        /// <summary>07 is not parked at the upgrade's station (the tower pad).</summary>
        NotAtStation = 1,

        /// <summary>Not enough scrap yet; nothing changed.</summary>
        CannotAfford = 2,

        /// <summary>Every level is already bought.</summary>
        Maxed = 3,

        /// <summary>No upgrade with that id.</summary>
        Unknown = 4,
    }
}
