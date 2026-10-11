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
        private readonly IBlueprintBoard _board;
        private readonly IComponentGate _parts;

        /// <param name="board">Kenji's board: a kit is built only once its blueprint is pinned.</param>
        /// <param name="parts">The tower's components: a level that needs one is built only once it is delivered.</param>
        public UpgradeShop(UpgradeService service, IUpgradeStation[] stations, ISaveService save,
            IBlueprintBoard board, IComponentGate parts)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
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

            if (!_board.IsPinned(upgradeId))
            {
                return PurchaseResult.NotPinned;
            }

            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i].Sells(definition) && !_stations[i].Occupied)
                {
                    return PurchaseResult.NotAtStation;
                }
            }

            if (_parts.NeedsPart(upgradeId, _service.LevelOf(upgradeId) + 1))
            {
                return PurchaseResult.NeedsPart;
            }

            PurchaseResult result = _service.Purchase(upgradeId);
            if (result == PurchaseResult.Purchased)
            {
                _save.SaveNow();
            }

            return result;
        }

        public string ComponentNeeded(string upgradeId)
        {
            return _parts.NeededComponent(upgradeId, _service.LevelOf(upgradeId) + 1);
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
