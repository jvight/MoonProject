using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's garage service port (docs/design/first-hour-v2.md §3), Art's GaragePort standing in the garage behind its
    /// painted "07" pad. The garage's last cut opens it: its place card, its pad lit. Parked on the pad, 07 has its spare
    /// tether ring fitted round its eye (<see cref="PortFitting"/>; the Tether ability, asleep until then). Then Ro's
    /// tape deck, dug up beside the port and towed onto its tray, is taken off the tether, glides onto the tray and is
    /// fitted on 07's flank. Before the ring, a loose keepsake near 07 gives one line, once: the tether is gone and
    /// Kenji kept a spare. A save that already used the tether (a keepsake home, or the Cargo Cradle) has the ring.
    /// </summary>
    public sealed class GaragePort : IDisposable
    {
        private enum DeckPhase
        {
            Waiting,
            Claimed,
            Gliding,
            Fitted,
        }

        private readonly ProgressionTuning _tuning;
        private readonly SalvageSite _garage;
        private readonly RelicField _relics;
        private readonly IRoverAbilities _abilities;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly EventBus _events;
        private readonly Transform _model;
        private readonly Transform _tray;
        private readonly Transform _armTip;
        private readonly GameObject _pad;
        private readonly Vector3 _padCentre;
        private readonly Relic _deck;
        private DeckPhase _deckPhase;
        private Vector3 _glideFrom;
        private Quaternion _glideFromRotation;
        private float _glide;
        private float _parked;
        private bool _ringFitted;
        private bool _placeSeen;
        private bool _tetherHinted;

        /// <exception cref="InvalidOperationException">The garage site or Art's port is missing.</exception>
        public GaragePort(Transform parent, GameplayServices services, SalvageField salvage, RelicField relics,
            IRoverAbilities abilities, ProgressionTuning tuning)
        {
            if (services == null || salvage == null)
            {
                throw new ArgumentNullException(services == null ? nameof(services) : nameof(salvage));
            }

            _relics = relics != null ? relics : throw new ArgumentNullException(nameof(relics));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _rover = services.Rover;
            _save = services.Save;
            _events = services.Events;
            _garage = salvage.Find(tuning.PortSite) ?? throw new InvalidOperationException(
                $"Kenji's garage port stands at '{tuning.PortSite}', which is no salvage site.");
            _deck = relics.Find(tuning.PortDeckRelic) ?? throw new InvalidOperationException(
                $"Ro's tape deck is relic '{tuning.PortDeckRelic}', which is no relic.");
            Transform site = _garage.Root;
            _model = Object.Instantiate(services.Visuals.GaragePort, parent).transform;
            _model.name = "GaragePort";
            _model.SetPositionAndRotation(site.TransformPoint(tuning.PortLocal),
                site.rotation * Quaternion.Euler(0f, tuning.PortYaw, 0f));
            _tray = _model.Find(GameplayVisuals.PortTrayNode) ?? throw new InvalidOperationException(
                $"Art's GaragePort has no '{GameplayVisuals.PortTrayNode}' node.");
            _armTip = _model.Find(GameplayVisuals.PortArmTipNode) ?? throw new InvalidOperationException(
                $"Art's GaragePort has no '{GameplayVisuals.PortArmTipNode}' node.");
            _padCentre = site.TransformPoint(tuning.PortPadLocal);
            _pad = Object.Instantiate(services.Visuals.PadFor(tuning.PortPadRadius), parent);
            _pad.name = "GaragePortPad";
            _pad.transform.SetPositionAndRotation(_padCentre, site.rotation);
            _pad.SetActive(false);
        }

        /// <summary>True once the garage's last cut has opened the port.</summary>
        public bool IsOpen => _garage.IsPickedClean;

        /// <summary>True once the port has fitted 07's tether ring.</summary>
        public bool RingFitted => _ringFitted;

        /// <summary>True once the port has fitted Ro's tape deck on 07.</summary>
        public bool DeckFitted => _deckPhase == DeckPhase.Fitted;

        /// <summary>The middle of the port's pad, where 07 parks.</summary>
        public Vector3 PadCentre => _padCentre;

        /// <summary>Where a piece 07 sets down on the tray rests.</summary>
        public Vector3 TrayPosition => _tray.position;

        public GaragePortSaveData Capture()
        {
            return new GaragePortSaveData
            {
                ringFitted = _ringFitted, placeSeen = _placeSeen, tetherHinted = _tetherHinted,
            };
        }

        public void Restore(GaragePortSaveData data)
        {
            _ringFitted = data != null && data.ringFitted;
            _placeSeen = data != null && data.placeSeen;
            _tetherHinted = data != null && data.tetherHinted;
        }

        /// <summary>
        /// The save is applied: the ring (also for a save that already used the tether) and the deck go back on 07
        /// at once, and the tether wakes with the ring.
        /// </summary>
        public void Announce()
        {
            _ringFitted |= UsedTheTether();
            if (_ringFitted)
            {
                _abilities.Grant(RoverAbility.Tether);
                _events.Publish(new PortFitting(RoverKitPiece.TetherRing, _armTip.position, true));
            }

            if (_deck.State == RelicState.Fitted)
            {
                _deckPhase = DeckPhase.Fitted;
                _events.Publish(new PortFitting(RoverKitPiece.TapeDeck, _armTip.position, true));
            }
        }

        public void Step(float deltaTime)
        {
            bool open = IsOpen;
            float toPad = Flat(_rover.Position - _padCentre).magnitude;
            if (open && !_placeSeen && toPad <= _tuning.PortPlaceRadius)
            {
                _placeSeen = true;
                _events.Publish(new PlaceFirstApproached(ProgressionKeys.PlaceGaragePort));
            }

            bool parked = toPad <= _tuning.PortPadRadius && _rover.Speed <= _tuning.PortParkSpeed;
            _parked = parked ? _parked + deltaTime : 0f;
            if (open && !_ringFitted && _parked >= _tuning.PortParkSeconds)
            {
                FitTheRing();
            }

            StepDeck(deltaTime);
            HintTheTether();
            _pad.SetActive(open && (!_ringFitted || DeckWaitsForTheTray()));
        }

        public void Dispose()
        {
            if (_model != null)
            {
                Object.Destroy(_model.gameObject);
            }

            if (_pad != null)
            {
                Object.Destroy(_pad);
            }
        }

        private void FitTheRing()
        {
            _ringFitted = true;
            _abilities.Grant(RoverAbility.Tether);
            _events.Publish(new PortFitting(RoverKitPiece.TetherRing, _armTip.position, false));
            _events.Publish(new TickerLine(ProgressionKeys.TickerTetherBack));
            _save.SaveNow();
        }

        /// <summary>
        /// The deck towed near the tray is taken off the tether; once free it glides onto the tray, and the arm fits it
        /// on 07.
        /// </summary>
        private void StepDeck(float deltaTime)
        {
            switch (_deckPhase)
            {
                case DeckPhase.Waiting:
                    if (_ringFitted && _deck.IsTethered &&
                        Vector3.Distance(_deck.transform.position, _tray.position) <= _tuning.PortTrayRadius)
                    {
                        _deck.Claim();
                        _deckPhase = DeckPhase.Claimed;
                    }

                    break;
                case DeckPhase.Claimed:
                    if (!_deck.IsTethered)
                    {
                        _glideFrom = _deck.transform.position;
                        _glideFromRotation = _deck.transform.rotation;
                        _glide = 0f;
                        _deck.BeginCarry(RelicState.Porting, -1);
                        _deckPhase = DeckPhase.Gliding;
                    }

                    break;
                case DeckPhase.Gliding:
                    _glide += deltaTime;
                    float progress = Mathf.Clamp01(_glide / _tuning.PortTrayGlideSeconds);
                    float eased = Ease.InOutSine(progress);
                    _deck.SetCarryPose(Vector3.Lerp(_glideFrom, _tray.position, eased),
                        Quaternion.Slerp(_glideFromRotation, _tray.rotation, eased));
                    if (progress >= 1f)
                    {
                        _deck.SetFitted();
                        _deckPhase = DeckPhase.Fitted;
                        _events.Publish(new PortFitting(RoverKitPiece.TapeDeck, _armTip.position, false));
                        _save.SaveNow();
                    }

                    break;
            }
        }

        /// <summary>Before the ring, a loose keepsake near 07 says, once, where the tether can be found again.</summary>
        private void HintTheTether()
        {
            if (_tetherHinted || _abilities.Has(RoverAbility.Tether))
            {
                return;
            }

            float reach = _tuning.PortTetherHintRadius;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (relic.State == RelicState.Loose &&
                    (relic.transform.position - _rover.Position).sqrMagnitude <= reach * reach)
                {
                    _tetherHinted = true;
                    _events.Publish(new TickerLine(ProgressionKeys.NotYetTether));
                    return;
                }
            }
        }

        private bool DeckWaitsForTheTray()
        {
            return _deckPhase != DeckPhase.Fitted && _deck.State != RelicState.Buried &&
                   _deck.State != RelicState.Surfacing;
        }

        /// <summary>A save from before the port that already used the tether keeps it (first-hour v2 §3).</summary>
        private bool UsedTheTether()
        {
            if (_abilities.Has(RoverAbility.CargoCradle))
            {
                return true;
            }

            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                if (_relics.Relics[i].State == RelicState.Displayed)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
