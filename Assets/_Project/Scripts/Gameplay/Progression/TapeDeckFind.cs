using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Tilly's glint (docs/features/M3-24 C): an old tape deck lying in the dust behind a world anchor (the garage),
    /// Art's Kit_TapeDeck. It is there only once Tilly is awake and out with 07, glinting like a component; driven
    /// through, it is drawn onto 07 (<see cref="TapeDeckFound"/>: Rover fits it on the flank with the gift's soft
    /// moment and the deck plays the first tape), and the game saves. A loaded game that has it announces it once.
    /// </summary>
    public sealed class TapeDeckFind : IDisposable
    {
        private enum Phase
        {
            Waiting,
            Resting,
            Flying,
            Gone,
        }

        private readonly ProgressionTuning _tuning;
        private readonly FriendTuning _friends;
        private readonly IFriendRoster _roster;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly EventBus _events;
        private readonly PickupGlints _glints;
        private readonly Transform _model;
        private readonly Vector3 _scale;
        private readonly Vector3 _rest;
        private readonly Quaternion _lie;
        private IFriendState _friend;
        private Phase _phase;
        private Vector3 _from;
        private float _since;
        private float _flight;

        /// <exception cref="InvalidOperationException">The deck's anchor is no world anchor.</exception>
        public TapeDeckFind(Transform parent, GameplayServices services, IFriendRoster roster, FriendTuning friends,
            ProgressionTuning tuning)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _events = services.Events;
            if (!services.Anchors.TryGet(tuning.TapeDeckAnchor, out WorldAnchor anchor))
            {
                throw new InvalidOperationException($"The tape deck waits at '{tuning.TapeDeckAnchor}', which is no " +
                                                    "world anchor.");
            }

            Vector3 back = anchor.Position - anchor.Forward * tuning.TapeDeckSetback;
            _rest = SurfaceRules.OnSurface(services.Terrain, back.x, back.z) + Vector3.up * tuning.TapeDeckLift;
            _lie = Quaternion.LookRotation(anchor.Forward, Vector3.up) * Quaternion.Euler(tuning.TapeDeckTilt);
            _model = Object.Instantiate(services.Visuals.TapeDeck, parent).transform;
            _model.name = "TapeDeck_Find";
            _scale = _model.localScale;
            _model.gameObject.SetActive(false);
            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil, 1,
                Layers.Pickup);
        }

        /// <summary>True once 07 has picked the deck up (saved).</summary>
        public bool Found { get; private set; }

        /// <summary>True while the deck lies in view, waiting to be picked up.</summary>
        public bool IsResting => _phase == Phase.Resting;

        /// <summary>Where the deck lies (tests and debugging views).</summary>
        public Vector3 RestPosition => _rest;

        public TapeDeckSaveData Capture()
        {
            return new TapeDeckSaveData { found = Found };
        }

        public void Restore(TapeDeckSaveData data)
        {
            Found = data != null && data.found;
            _phase = Found ? Phase.Gone : Phase.Waiting;
            _model.gameObject.SetActive(false);
            _model.localScale = _scale;
        }

        /// <summary>The save is applied: a deck already found goes back on 07 at once.</summary>
        public void Announce()
        {
            if (Found)
            {
                _events.Publish(new TapeDeckFound(_rest, true));
            }
        }

        public void Step(float now)
        {
            _glints.Begin(_view.Camera.transform.position, now);
            switch (_phase)
            {
                case Phase.Waiting:
                    if (FriendIsOut())
                    {
                        _phase = Phase.Resting;
                        _since = now;
                        _model.SetPositionAndRotation(_rest, _lie);
                        _model.localScale = Vector3.zero;
                        _model.gameObject.SetActive(true);
                    }

                    break;
                case Phase.Resting:
                    Rest(now);
                    break;
                case Phase.Flying:
                    Fly(now);
                    break;
            }

            _glints.End();
        }

        public void Dispose()
        {
            _glints.Dispose();
            if (_model != null)
            {
                Object.Destroy(_model.gameObject);
            }
        }

        private void Rest(float now)
        {
            float appear = Mathf.Clamp01((now - _since) / _tuning.PartAppearSeconds);
            _model.localScale = _scale * Ease.OutCubic(appear);
            _glints.Add(_rest, 0f);
            float reach = _friends.PartMagnetRadius;
            if ((_rest - _rover.Position).sqrMagnitude <= reach * reach)
            {
                _phase = Phase.Flying;
                _from = _rest;
                _since = now;
                _flight = PickupFlight.Duration(Vector3.Distance(_rest, _rig.CargoSocket.position),
                    _friends.PartFlightDuration, _friends.PartFlightPerMetre);
            }
        }

        private void Fly(float now)
        {
            float progress = (now - _since) / _flight;
            if (progress >= 1f)
            {
                _phase = Phase.Gone;
                _model.gameObject.SetActive(false);
                _model.localScale = _scale;
                Found = true;
                _events.Publish(new TapeDeckFound(_from, false));
                _save.SaveNow();
                return;
            }

            float eased = Ease.InOutSine(progress);
            _model.SetPositionAndRotation(
                PickupFlight.Evaluate(_from, _rig.CargoSocket.position, progress, 0, 1f, _friends.PartFlightLift,
                    _friends.PartSpiralRadius, _friends.PartSpiralTurns),
                Quaternion.Slerp(_lie, _rig.CargoSocket.rotation, eased));
            _model.localScale = _scale * Mathf.Lerp(1f, _friends.PartArrivalScale, eased);
        }

        /// <summary>The deck shows once its friend is awake and out of her repair: she spots it with 07.</summary>
        private bool FriendIsOut()
        {
            if (_friend == null)
            {
                for (int i = 0; i < _roster.Count; i++)
                {
                    if (_roster.Get(i).Id == _tuning.TapeDeckFriend)
                    {
                        _friend = _roster.Get(i);
                    }
                }

                if (_friend == null)
                {
                    Debug.LogError($"{nameof(TapeDeckFind)}: no friend '{_tuning.TapeDeckFriend}' in the roster, so " +
                                   "the tape deck never shows.");
                    _phase = Phase.Gone;
                    return false;
                }
            }

            return _friend.Activity != FriendActivity.Dormant && _friend.Activity != FriendActivity.Repairing;
        }
    }
}
