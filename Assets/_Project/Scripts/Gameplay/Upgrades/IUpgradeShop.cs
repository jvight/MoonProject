namespace MoonProject.Gameplay
{
    /// <summary>
    /// The upgrade shop the UI drives, registered in the GameContext. Purchases happen at a station (the radio tower's
    /// pad): the UI offers the station's upgrade while 07 is parked there and calls <see cref="Purchase"/> on
    /// confirmation. Gameplay applies the effects, publishes the events and saves.
    /// </summary>
    public interface IUpgradeShop
    {
        /// <summary>True while 07 is parked at an upgrade station.</summary>
        bool IsAtStation { get; }

        /// <summary>The upgrade sold at the station 07 is parked at, or null.</summary>
        UpgradeDefinition StationUpgrade { get; }

        int LevelOf(string upgradeId);

        bool TryGetOffer(string upgradeId, out UpgradeOffer offer);

        PurchaseResult Purchase(string upgradeId);
    }
}
