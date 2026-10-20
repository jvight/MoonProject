using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// relay.1's repair (docs/design/opening-v3-purpose.md R10): its open service cabinet holds the labelled spare
    /// (RELAY SPARE · FEED UNIT), offered with a held E within reach; the mast box's empty feed socket takes it from
    /// the hold with a held E within reach (<see cref="InteractionKind.Install"/>). Fitted, the relay is lit
    /// (<see cref="RelayRelit"/>, once, and replayed as Restored at each session start of a game where it is): its
    /// contact pad charges 07 like a pad (<see cref="ChargerKind.Mast"/>) and it counts as a charge point, so the range
    /// ring grows. The props are World's: the spare in the cabinet hides once taken, the fitted unit in the socket
    /// shows once fitted. Allocation-free per step.
    /// </summary>
    public sealed class RelayRepair : IChargePointSource
    {
        private const string SpareNoun = "pickup.part.feedunit";

        private readonly EventBus _events;
        private readonly CargoHold _hold;
        private readonly IRoverState _rover;
        private readonly PickupReach _pickups;
        private readonly RelayRepairSettings _settings;
        private readonly Vector3 _spare;
        private readonly Vector3 _socket;
        private readonly Vector3 _pad;
        private readonly GameObject _spareProp;
        private readonly GameObject _fittedProp;
        private bool _carriedAtLoad;

        // The E held to take the spare must be let go before it fits it: one press, one act.
        private bool _awaitRelease;

        /// <param name="spare">Where the spare lies in the open cabinet.</param>
        /// <param name="socket">The mast box's feed socket.</param>
        /// <param name="pad">The relay's contact pad's centre.</param>
        /// <param name="spareProp">World's spare in the cabinet: hidden once taken.</param>
        /// <param name="fittedProp">World's fitted unit in the socket: shown once fitted.</param>
        public RelayRepair(EventBus events, CargoHold hold, IRoverState rover, PickupReach pickups,
            RelayRepairSettings settings, Vector3 spare, Vector3 socket, Vector3 pad, GameObject spareProp,
            GameObject fittedProp)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _spareProp = spareProp != null ? spareProp : throw new ArgumentNullException(nameof(spareProp));
            _fittedProp = fittedProp != null ? fittedProp : throw new ArgumentNullException(nameof(fittedProp));
            _spare = spare;
            _socket = socket;
            _pad = pad;
            ShowProps();
        }

        /// <summary>07 took the spare from the cabinet.</summary>
        public bool SpareTaken { get; private set; }

        /// <summary>The spare is fitted: the relay is lit.</summary>
        public bool Fitted { get; private set; }

        /// <summary>The relay's id (relay.1).</summary>
        public string RelayId => _settings.RelayId;

        /// <summary>
        /// One frame: a held E (<paramref name="held"/>) takes the spare in reach, or, once let go and held again,
        /// fits it from the hold at the socket in reach.
        /// </summary>
        public void Step(int frame, bool held)
        {
            _awaitRelease &= held;
            if (Fitted)
            {
                return;
            }

            if (!SpareTaken)
            {
                if (_pickups.Offer(frame, held, PickupReach.Relay, 0, _spare, _rover.Position, _settings.TakeReach,
                        SpareNoun) && _hold.TryAddPart(_settings.FeedPart))
                {
                    SpareTaken = true;
                    _awaitRelease = true;
                    ShowProps();
                    _events.Publish(new StoryPartCollected(_settings.FeedPart, _spare));
                }

                return;
            }

            if (held && !_awaitRelease && TryGetInstall(out _))
            {
                _hold.RemovePart(_settings.FeedPart);
                Fitted = true;
                ShowProps();
                _events.Publish(new RelayRelit(_settings.RelayId, false));
            }
        }

        /// <summary>The feed socket offers the spare: it is in the hold and 07 is within the fit reach.</summary>
        public bool TryGetInstall(out Vector3 at)
        {
            at = _socket;
            return !Fitted && _hold.HasPart(_settings.FeedPart) &&
                   SurfaceRules.HorizontalDistanceSquared(_rover.Position, _socket) <=
                   _settings.FitReach * _settings.FitReach;
        }

        /// <summary>The lit relay's contact pad under <paramref name="rover"/>, if any.</summary>
        public bool TryFindUnder(Vector3 rover, out Vector3 position)
        {
            position = _pad;
            return Fitted &&
                   SurfaceRules.HorizontalDistanceSquared(rover, _pad) <= _settings.PadRadius * _settings.PadRadius;
        }

        public void CopyChargePoints(List<Vector3> points)
        {
            if (Fitted)
            {
                points.Add(_pad);
            }
        }

        /// <summary>
        /// The session starts (after the save is applied): a carried spare is back in the hold, and a lit relay is
        /// stated, Restored.
        /// </summary>
        public void Announce()
        {
            if (_carriedAtLoad && !_hold.HasPart(_settings.FeedPart))
            {
                _hold.TryAddPart(_settings.FeedPart);
            }

            _carriedAtLoad = false;
            if (Fitted)
            {
                _events.Publish(new RelayRelit(_settings.RelayId, true));
            }
        }

        public RelaySaveData Capture()
        {
            return new RelaySaveData
            {
                spareTaken = SpareTaken,
                spareCarried = SpareTaken && !Fitted && _hold.HasPart(_settings.FeedPart),
                fitted = Fitted,
            };
        }

        public void Restore(RelaySaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Fitted = data.fitted;
            SpareTaken = data.spareTaken || data.fitted;
            _carriedAtLoad = data.spareCarried && !data.fitted;
            ShowProps();
        }

        private void ShowProps()
        {
            Show(_spareProp, !SpareTaken);
            Show(_fittedProp, Fitted);
        }

        private static void Show(GameObject prop, bool visible)
        {
            if (prop.activeSelf != visible)
            {
                prop.SetActive(visible);
            }
        }
    }
}
