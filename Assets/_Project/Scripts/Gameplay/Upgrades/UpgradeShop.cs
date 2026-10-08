using System;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="IUpgradeShop"/> over the upgrade service and its stations: an upgrade with a station can only be
    /// bought while 07 is parked there, and every purchase is a save checkpoint.
    /// </summary>
    public sealed class UpgradeShop : IUpgradeShop
    {
        private readonly UpgradeService _service;
        private readonly IUpgradeStation[] _stations;
        private readonly ISaveService _save;

        public UpgradeShop(UpgradeService service, IUpgradeStation[] stations, ISaveService save)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public bool IsAtStation => StationUpgrade != null;

        public UpgradeDefinition StationUpgrade => Parked()?.Definition;

        public int StationUpgradeCount
        {
            get
            {
                IUpgradeStation station = Parked();
                return station != null ? station.UpgradeCount : 0;
            }
        }

        public UpgradeDefinition StationUpgradeAt(int index)
        {
            IUpgradeStation station = Parked();
            if (station == null)
            {
                throw new InvalidOperationException("07 is not parked at an upgrade station.");
            }

            return station.UpgradeAt(index);
        }

        public int LevelOf(string upgradeId)
        {
            return _service.LevelOf(upgradeId);
        }

        public bool TryGetOffer(string upgradeId, out UpgradeOffer offer)
        {
            return _service.TryGetOffer(upgradeId, out offer);
        }

        public PurchaseResult Purchase(string upgradeId)
        {
            UpgradeDefinition definition = _service.Find(upgradeId);
            if (definition == null)
            {
                return PurchaseResult.Unknown;
            }

            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i].Sells(definition) && !_stations[i].Occupied)
                {
                    return PurchaseResult.NotAtStation;
                }
            }

            PurchaseResult result = _service.Purchase(upgradeId);
            if (result == PurchaseResult.Purchased)
            {
                _save.SaveNow();
            }

            return result;
        }

        /// <summary>The station 07 is parked at, or null.</summary>
        private IUpgradeStation Parked()
        {
            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i].Occupied)
                {
                    return _stations[i];
                }
            }

            return null;
        }
    }
}
