using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tower's components in the world (docs/features/M3-16 P1), Art's Part_* models. A part in a site's heart
    /// eases into view once the heart is revealed (the depot's last cut; the garage's transformer only once 07 has
    /// looked across the chasm, P2); the relay spare on the pad of the first mast 07 restores, once the tower's next
    /// level is its own (07 charges on that pad long before, M3-20). Waiting, a part hovers, bobs, turns and glints
    /// like a friend's part; driven through, it is drawn into 07 and rides home on its back (<see
    /// cref="TowerParts.PickUp"/>; Rover shows it). Parked on the tower's pad, 07 hands each carried part to the port,
    /// lowest level first: it leaves 07's back (<see cref="TowerParts.Deliver"/>) and glides on a soft arc into the
    /// hopper while 07 watches it go.
    /// </summary>
    public sealed class TowerPartField : IDisposable
    {
        private enum Phase
        {
            Hidden,
            Resting,
            Flying,
            Riding,
            Feeding,
            Fed,
        }

        private readonly TowerParts _parts;
        private readonly SalvageSite[] _sites;
        private readonly StationReach _reach;
        private readonly RadioTower _tower;
        private readonly FriendTuning _friends;
        private readonly ProgressionTuning _tuning;
        private readonly Func<bool> _chasmSeen;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly ITerrainQuery _terrain;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly PickupGlints _glints;
        private readonly Transform[] _models;
        private readonly Vector3[] _scales;
        private readonly Phase[] _phases;
        private readonly Vector3[] _rest;
        private readonly Vector3[] _from;
        private readonly float[] _since;
        private readonly float[] _flight;
        private int _feeding = -1;

        /// <exception cref="InvalidOperationException">A part's model or site is missing (broken content).</exception>
        public TowerPartField(TowerParts parts, Transform parent, GameplayServices services, SalvageField salvage,
            StationReach reach, RadioTower tower, FriendTuning friends, ProgressionTuning tuning, Func<bool> chasmSeen)
        {
            _chasmSeen = chasmSeen ?? throw new ArgumentNullException(nameof(chasmSeen));
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _tower = tower != null ? tower : throw new ArgumentNullException(nameof(tower));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _view = services.View;
            _save = services.Save;
            int count = parts.Count;
            _sites = new SalvageSite[count];
            _models = new Transform[count];
            _scales = new Vector3[count];
            _phases = new Phase[count];
            _rest = new Vector3[count];
            _from = new Vector3[count];
            _since = new float[count];
            _flight = new float[count];
            for (int i = 0; i < count; i++)
            {
                TowerPart part = parts.PartAt(i);
                if (part.Source == TowerPartSource.SiteHeart)
                {
                    _sites[i] = salvage.Find(part.SiteId) ?? throw new InvalidOperationException(
                        $"Tower part '{part.PartId}' lies in '{part.SiteId}', which is not a salvage site.");
                }

                GameObject model = services.Visuals.ComponentModel(part.PartId);
                if (model == null)
                {
                    throw new InvalidOperationException($"Art's model for tower part '{part.PartId}' is not in " +
                                                        "GameplayVisuals (run Gameplay/Materials).");
                }

                _models[i] = Object.Instantiate(model, parent).transform;
                _models[i].name = part.PartId;
                _scales[i] = _models[i].localScale;
                _models[i].gameObject.SetActive(false);
            }

            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                Mathf.Max(1, count),
                Layers.Pickup);
            Sync();
        }

        /// <summary>The part being handed to the port, or -1.</summary>
        public int Feeding => _feeding;

        /// <summary>True while the part at <paramref name="index"/> waits in view to be picked up.</summary>
        public bool IsResting(int index)
        {
            return _phases[index] == Phase.Resting;
        }

        /// <summary>Where the part at <paramref name="index"/> rests once in view (tests and debugging views).</summary>
        public Vector3 RestPosition(int index)
        {
            return _rest[index];
        }

        /// <summary>Shows each part as the save says (boot, load): carried and fed parts are out of view.</summary>
        public void Sync()
        {
            ClearFeed();
            for (int i = 0; i < _models.Length; i++)
            {
                TowerPartState state = _parts.StateOf(i);
                _phases[i] = state == TowerPartState.Waiting ? Phase.Hidden
                    : state == TowerPartState.Carried ? Phase.Riding
                    : Phase.Fed;
                _models[i].gameObject.SetActive(false);
                _models[i].localScale = _scales[i];
            }
        }

        public void Step(float now)
        {
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = 0; i < _models.Length; i++)
            {
                StepPart(i, now);
            }

            _glints.End();
            StepFeed(now);
        }

        public void Dispose()
        {
            ClearFeed();
            _glints.Dispose();
            for (int i = 0; i < _models.Length; i++)
            {
                if (_models[i] != null)
                {
                    Object.Destroy(_models[i].gameObject);
                }
            }
        }

        private void StepPart(int i, float now)
        {
            Transform model = _models[i];
            switch (_phases[i])
            {
                case Phase.Hidden:
                    if (TryRest(i, out _rest[i]))
                    {
                        _phases[i] = Phase.Resting;
                        _since[i] = now;
                        model.gameObject.SetActive(true);
                    }

                    break;
                case Phase.Resting:
                    float bob = Mathf.Sin(now * 2f * Mathf.PI * _friends.PartBobFrequency + i) * _friends.PartBob;
                    Vector3 at = _rest[i] + Vector3.up * (_friends.PartHover + bob);
                    float heading = now * _friends.PartSpin + i * 360f / _models.Length;
                    float appear = Mathf.Clamp01((now - _since[i]) / _tuning.PartAppearSeconds);
                    model.SetPositionAndRotation(at, Quaternion.Euler(0f, heading, 0f));
                    model.localScale = _scales[i] * Ease.OutCubic(appear);
                    _glints.Add(_rest[i], i);
                    if ((at - _rover.Position).sqrMagnitude <= _friends.PartMagnetRadius * _friends.PartMagnetRadius)
                    {
                        _phases[i] = Phase.Flying;
                        _from[i] = at;
                        _since[i] = now;
                        _flight[i] = PickupFlight.Duration(Vector3.Distance(at, _rig.CargoSocket.position),
                            _friends.PartFlightDuration, _friends.PartFlightPerMetre);
                    }

                    break;
                case Phase.Flying:
                    float progress = (now - _since[i]) / _flight[i];
                    if (progress >= 1f)
                    {
                        _phases[i] = Phase.Riding;
                        model.gameObject.SetActive(false);
                        model.localScale = _scales[i];
                        _parts.PickUp(i, _from[i]);
                        _save.SaveNow();
                        break;
                    }

                    model.SetPositionAndRotation(
                        PickupFlight.Evaluate(_from[i], _rig.CargoSocket.position, progress, i, 1f,
                            _friends.PartFlightLift, _friends.PartSpiralRadius, _friends.PartSpiralTurns),
                        Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
                    model.localScale = _scales[i] *
                                       Mathf.Lerp(1f, _friends.PartArrivalScale, Ease.InOutSine(progress));
                    break;
            }
        }

        /// <summary>Parked on the tower's pad, 07 hands its carried parts to the port one after another.</summary>
        private void StepFeed(float now)
        {
            if (_feeding < 0)
            {
                int next = _parts.FirstCarried();
                if (next < 0 || !_tower.Occupied)
                {
                    return;
                }

                _feeding = next;
                _phases[next] = Phase.Feeding;
                _since[next] = now;
                _from[next] = _rig.CargoSocket.position;
                _models[next].gameObject.SetActive(true);
                _rig.SetGazeTarget(this, _tower.HopperMouth, GazePriorities.Focus);
                _parts.Deliver(next, _tower.HopperMouth);
                _save.SaveNow();
            }

            Transform model = _models[_feeding];
            float t = (now - _since[_feeding]) / _tuning.PartFeedSeconds;
            if (t >= 1f)
            {
                _phases[_feeding] = Phase.Fed;
                model.gameObject.SetActive(false);
                model.localScale = _scales[_feeding];
                ClearFeed();
                return;
            }

            float glide = Ease.InOutSine(t);
            Vector3 mouth = _tower.HopperMouth;
            model.SetPositionAndRotation(
                Vector3.Lerp(_from[_feeding], mouth, glide) + Vector3.up * (_tuning.PartFeedArc * Ease.Hump(glide)),
                Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
            model.localScale = _scales[_feeding] * Mathf.Lerp(_friends.PartArrivalScale, 1f, Ease.Hump(glide));
        }

        private void ClearFeed()
        {
            if (_feeding < 0)
            {
                return;
            }

            _rig.ClearGazeTarget(this);
            _feeding = -1;
        }

        /// <summary>Where the part at <paramref name="i"/> rests, once it is there to be found.</summary>
        private bool TryRest(int i, out Vector3 rest)
        {
            TowerPart part = _parts.PartAt(i);
            if (part.Source == TowerPartSource.SiteHeart)
            {
                SalvageSite site = _sites[i];
                Transform root = site.Root;
                Vector3 heart = site.Heart + root.right * part.HeartOffset.x + root.forward * part.HeartOffset.y;
                rest = SurfaceRules.OnSurface(_terrain, heart.x, heart.z);
                return site.HeartRevealed && (!part.AfterTheChasm || _chasmSeen());
            }

            bool needed = _tower.ShownLevel + 1 >= part.TowerLevel;
            for (int node = 0; needed && node < _reach.NodeCount; node++)
            {
                if (node != StationReach.Home && _reach.IsLit(node))
                {
                    Vector3 pad = _reach.Position(node);
                    rest = SurfaceRules.OnSurface(_terrain, pad.x, pad.z);
                    return true;
                }
            }

            rest = Vector3.zero;
            return false;
        }
    }
}
