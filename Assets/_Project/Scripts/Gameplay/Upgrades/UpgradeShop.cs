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

        public UpgradeDefinition StationUpgrade
        {
            get
            {
                for (int i = 0; i < _stations.Length; i++)
                {
                    if (_stations[i].Occupied)
                    {
                        return _stations[i].Definition;
                    }
                }

                return null;
            }
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
    }
}
