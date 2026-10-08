using System;
using UnityEngine;
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
            InteractionKind.Deposit, InteractionKind.Repair, InteractionKind.Restore, InteractionKind.Tune,
            InteractionKind.Hop, InteractionKind.Upgrade, InteractionKind.Salvage, InteractionKind.Excavate,
            InteractionKind.Tether, InteractionKind.Reel, InteractionKind.Ping,
        };

        private readonly IRoverState _rover;
        private readonly SonarSystem _sonar;
        private readonly ExcavationSystem _excavation;
        private readonly SalvageField _salvage;
        private readonly TetherSystem _tether;
        private readonly HomeBase _home;
        private readonly IUpgradeStation[] _stations;
        private readonly UpgradeService _upgrades;
        private readonly FriendField _friends;
        private readonly RelayField _relays;

        public InteractionHints(IRoverState rover, SonarSystem sonar, ExcavationSystem excavation,
            SalvageField salvage, TetherSystem tether, HomeBase home, IUpgradeStation[] stations,
            UpgradeService upgrades, FriendField friends, RelayField relays)
        {
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _sonar = sonar != null ? sonar : throw new ArgumentNullException(nameof(sonar));
            _excavation = excavation != null ? excavation : throw new ArgumentNullException(nameof(excavation));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _tether = tether != null ? tether : throw new ArgumentNullException(nameof(tether));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _stations = stations ?? throw new ArgumentNullException(nameof(stations));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _relays = relays != null ? relays : throw new ArgumentNullException(nameof(relays));
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
            ITowable towed = _tether.TowedBody;
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
                case InteractionKind.Salvage:
                    SalvagePiece piece = _salvage.Cutting ?? _salvage.Candidate;
                    hint = piece != null ? new InteractionHint(kind, piece.CutPosition, true) : InteractionHint.None;
                    return piece != null;
                case InteractionKind.Tether:
                    ITowable hovered = _tether.HoveredBody;
                    hint = hovered != null
                        ? new InteractionHint(kind, hovered.Position, true)
                        : InteractionHint.None;
                    return hovered != null;
                case InteractionKind.Deposit:
                    Relic relic = _tether.Towed;
                    bool deposit = relic != null && _home.InDepositZone(relic.transform.position);
                    hint = deposit ? new InteractionHint(kind, _home.ShelfPosition, true) : InteractionHint.None;
                    return deposit;
                case InteractionKind.Repair:
                    Friend broken = _friends.RepairCandidate;
                    hint = broken != null
                        ? new InteractionHint(kind, broken.Site.Position, true)
                        : InteractionHint.None;
                    return broken != null;
                case InteractionKind.Tune:
                    bool tune = _friends.TryGetDial(out Vector3 dial);
                    hint = tune ? new InteractionHint(kind, dial, true) : InteractionHint.None;
                    return tune;
                case InteractionKind.Restore:
                    bool restore = _relays.TryGetRestore(out Vector3 socket, out bool affordable);
                    hint = restore ? new InteractionHint(kind, socket, affordable) : InteractionHint.None;
                    return restore;
                case InteractionKind.Hop:
                    IRadioHop hop = _relays.Hop;
                    bool open = hop.Phase == RadioHopPhase.Choosing;
                    bool hopHere = open || hop.CanOpen;
                    hint = hopHere
                        ? new InteractionHint(kind, _relays.Reach.Position(hop.Here), true)
                        : InteractionHint.None;
                    return hopHere;
                case InteractionKind.Reel:
                    hint = towed != null
                        ? new InteractionHint(kind, towed.Position, true)
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
