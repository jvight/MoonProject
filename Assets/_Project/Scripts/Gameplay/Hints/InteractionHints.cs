using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// <see cref="IInteractionHints"/> read straight from the gameplay systems' state (nothing cached).
    /// </summary>
    public sealed class InteractionHints : IInteractionHints
    {
        private static readonly InteractionKind[] Priority =
        {
            InteractionKind.Deposit, InteractionKind.Repair, InteractionKind.Upgrade, InteractionKind.Excavate,
            InteractionKind.Tether, InteractionKind.Reel, InteractionKind.Ping,
        };

        private readonly IRoverState _rover;
        private readonly SonarSystem _sonar;
        private readonly ExcavationSystem _excavation;
        private readonly TetherSystem _tether;
        private readonly HomeBase _home;
        private readonly IUpgradeStation[] _stations;
        private readonly UpgradeService _upgrades;
        private readonly FriendField _friends;

        public InteractionHints(IRoverState rover, SonarSystem sonar, ExcavationSystem excavation, TetherSystem tether,
            HomeBase home, IUpgradeStation[] stations, UpgradeService upgrades, FriendField friends)
        {
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _sonar = sonar != null ? sonar : throw new ArgumentNullException(nameof(sonar));
            _excavation = excavation != null ? excavation : throw new ArgumentNullException(nameof(excavation));
            _tether = tether != null ? tether : throw new ArgumentNullException(nameof(tether));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
        }

        public InteractionHint Primary
        {
            get
            {
                for (int i = 0; i < Priority.Length; i++)
                {
                    if (TryGet(Priority[i], out InteractionHint hint))
                    {
                        return hint;
                    }
                }

                return InteractionHint.None;
            }
        }

        public bool TryGet(InteractionKind kind, out InteractionHint hint)
        {
            Relic towed = _tether.Towed;
            switch (kind)
            {
                case InteractionKind.Ping:
                    hint = new InteractionHint(kind, _rover.Position, _sonar.IsReady);
                    return _sonar.enabled;
                case InteractionKind.Excavate:
                    Relic candidate = _excavation.Candidate;
                    hint = candidate != null
                        ? new InteractionHint(kind, candidate.Site.Position, true)
                        : InteractionHint.None;
                    return candidate != null;
                case InteractionKind.Tether:
                    Relic hovered = _tether.Hovered;
                    hint = hovered != null
                        ? new InteractionHint(kind, hovered.transform.position, true)
                        : InteractionHint.None;
                    return hovered != null;
                case InteractionKind.Deposit:
                    bool deposit = towed != null && _home.InDepositZone(towed.transform.position);
                    hint = deposit ? new InteractionHint(kind, _home.ShelfPosition, true) : InteractionHint.None;
                    return deposit;
                case InteractionKind.Repair:
                    Friend broken = _friends.RepairCandidate;
                    hint = broken != null
                        ? new InteractionHint(kind, broken.Site.Position, true)
                        : InteractionHint.None;
                    return broken != null;
                case InteractionKind.Reel:
                    hint = towed != null
                        ? new InteractionHint(kind, towed.transform.position, true)
                        : InteractionHint.None;
                    return towed != null;
                case InteractionKind.Upgrade:
                    for (int i = 0; i < _stations.Length; i++)
                    {
                        IUpgradeStation station = _stations[i];
                        if (station.Occupied && _upgrades.TryGetOffer(station.Definition.Id, out UpgradeOffer offer) &&
                            !offer.IsMaxed)
                        {
                            hint = new InteractionHint(kind, station.PadCentre, offer.CanAfford);
                            return true;
                        }
                    }

                    hint = InteractionHint.None;
                    return false;
                default:
                    hint = InteractionHint.None;
                    return false;
            }
        }
    }
}
