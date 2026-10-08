namespace MoonProject.Gameplay
{
    /// <summary>
    /// The upgrade shop the UI drives, registered in the GameContext. Purchases happen at a station (the radio tower's
    /// pad, Kenji's workbench): while 07 is parked on one, <see cref="StationUpgrade"/> is what it offers (its
    /// <see cref="UpgradeDefinition.Station"/> says which station, for dressing the panel) and the UI calls
    /// <see cref="Purchase"/> on confirmation. Gameplay applies the effects, publishes the events and saves.
    /// </summary>
    public interface IUpgradeShop
    {
        /// <summary>True while 07 is parked at an upgrade station.</summary>
        bool IsAtStation { get; }

        /// <summary>What the station 07 is parked at offers now, or null.</summary>
        UpgradeDefinition StationUpgrade { get; }

        /// <summary>How many upgrades the station 07 is parked at sells (0 while not parked).</summary>
        int StationUpgradeCount { get; }

        /// <summary>
        /// The parked station's upgrade <paramref name="index"/>, in the station's own order (bought ones included), for
        /// the bench's choosing panel. Allocation-free.
        /// </summary>
        UpgradeDefinition StationUpgradeAt(int index);

        int LevelOf(string upgradeId);

        bool TryGetOffer(string upgradeId, out UpgradeOffer offer);

        PurchaseResult Purchase(string upgradeId);
    }
}
