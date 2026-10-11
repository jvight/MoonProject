using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The blueprint tubes waiting on the ground at world anchors (docs/features/M3-21 §4: Hover-Jump's at the chasm's
    /// near lip), Art's BlueprintTube model. While its kit is neither found nor fitted, a tube rests a few metres back
    /// from its anchor, hovering, turning and glinting like a component; driven through, it is drawn into 07 and rides
    /// home on its back (<see cref="Blueprints.PickUp"/>; Rover shows it), and the game saves.
    /// </summary>
    public sealed class GroundTubes : IDisposable
    {
        private enum Phase
        {
            Gone,
            Resting,
            Flying,
        }

        private readonly Blueprints _blueprints;
        private readonly UpgradeService _upgrades;
        private readonly FriendTuning _friends;
        private readonly ProgressionTuning _tuning;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly PickupGlints _glints;
        private readonly BlueprintTube[] _tubes;
        private readonly Transform[] _models;
        private readonly Vector3[] _scales;
        private readonly Vector3[] _rest;
        private readonly Phase[] _phases;
        private readonly Vector3[] _from;
        private readonly float[] _since;
        private readonly float[] _flight;

        /// <exception cref="InvalidOperationException">A tube's anchor or Art's tube model is missing.</exception>
        public GroundTubes(Blueprints blueprints, UpgradeService upgrades, IReadOnlyList<BlueprintTube> tubes,
            Transform parent, GameplayServices services, FriendTuning friends, ProgressionTuning tuning)
        {
            _blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (services == null || tubes == null)
            {
                throw new ArgumentNullException(services == null ? nameof(services) : nameof(tubes));
            }

            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            var onGround = new List<BlueprintTube>();
            foreach (BlueprintTube tube in tubes)
            {
                if (tube.IsAtAnchor)
                {
                    onGround.Add(tube);
                }
            }

            _tubes = onGround.ToArray();
            int count = _tubes.Length;
            _models = new Transform[count];
            _scales = new Vector3[count];
            _rest = new Vector3[count];
            _phases = new Phase[count];
            _from = new Vector3[count];
            _since = new float[count];
            _flight = new float[count];
            for (int i = 0; i < count; i++)
            {
                BlueprintTube tube = _tubes[i];
                if (!services.Anchors.TryGet(tube.AnchorId, out WorldAnchor anchor))
                {
                    throw new InvalidOperationException($"Blueprint tube '{tube.KitId}' waits at '{tube.AnchorId}', " +
                                                        "which is no world anchor.");
                }

                Vector3 back = anchor.Position - anchor.Forward * tube.Setback;
                _rest[i] = SurfaceRules.OnSurface(services.Terrain, back.x, back.z);
                _models[i] = Object.Instantiate(services.Visuals.Tube, parent).transform;
                _models[i].name = "BlueprintTube_" + tube.KitId;
                _scales[i] = _models[i].localScale;
                _models[i].gameObject.SetActive(false);
            }

            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                Mathf.Max(1, count),
                Layers.Pickup);
            Sync();
        }

        /// <summary>True while the tube of <paramref name="kitId"/> rests in view, waiting to be picked up.</summary>
        public bool IsResting(string kitId)
        {
            int index = IndexOf(kitId);
            return index >= 0 && _phases[index] == Phase.Resting;
        }

        /// <summary>Where the tube of <paramref name="kitId"/> rests (tests and debugging views).</summary>
        public Vector3 RestPosition(string kitId)
        {
            int index = IndexOf(kitId);
            return index >= 0 ? _rest[index] : Vector3.zero;
        }

        /// <summary>
        /// Shows each tube as the game says (boot, load), at once: one whose kit is found or fitted is gone.
        /// </summary>
        public void Sync()
        {
            for (int i = 0; i < _tubes.Length; i++)
            {
                bool waits = _blueprints.IsWaiting(_tubes[i].KitId) && _upgrades.LevelOf(_tubes[i].KitId) == 0;
                _phases[i] = waits ? Phase.Resting : Phase.Gone;
                _since[i] = float.NegativeInfinity;
                _models[i].localScale = _scales[i];
                _models[i].gameObject.SetActive(waits);
            }
        }

        public void Step(float now)
        {
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = 0; i < _tubes.Length; i++)
            {
                StepTube(i, now);
            }

            _glints.End();
        }

        public void Dispose()
        {
            _glints.Dispose();
            foreach (Transform model in _models)
            {
                if (model != null)
                {
                    Object.Destroy(model.gameObject);
                }
            }
        }

        private void StepTube(int i, float now)
        {
            Transform model = _models[i];
            switch (_phases[i])
            {
                case Phase.Resting:
                    float bob = Mathf.Sin(now * 2f * Mathf.PI * _friends.PartBobFrequency + i) * _friends.PartBob;
                    Vector3 at = _rest[i] + Vector3.up * (_friends.PartHover + bob);
                    float appear = Mathf.Clamp01((now - _since[i]) / _tuning.PartAppearSeconds);
                    model.SetPositionAndRotation(at, Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
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
                        _phases[i] = Phase.Gone;
                        model.gameObject.SetActive(false);
                        model.localScale = _scales[i];
                        _blueprints.PickUp(_tubes[i].KitId, _tubes[i].AnchorId, _from[i]);
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

        private int IndexOf(string kitId)
        {
            for (int i = 0; i < _tubes.Length; i++)
            {
                if (_tubes[i].KitId == kitId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
