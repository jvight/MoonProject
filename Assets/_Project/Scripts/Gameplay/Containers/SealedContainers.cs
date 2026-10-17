using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The world's sealed containers (docs/design/0.5.5-owner-feedback.md §2: no find rests on open regolith). Within
    /// the beam's reach and ahead of 07, with no salvage piece or pickup before it, holding E cuts a container's
    /// glowing seal once (the cut's own beats, <see cref="CutState"/>, at the cutter's tier):
    /// <see cref="ContainerSealCut"/>, the lid swings open about its local X, and as it stands open
    /// <see cref="ContainerOpened"/> lets its owner slide the find out. A save keeps which stand open. Allocation-free
    /// after the containers are added.
    /// </summary>
    public sealed class SealedContainers : ICutterAimOwner
    {
        private readonly List<SealedContainer> _containers = new List<SealedContainer>();
        private readonly SalvageField _salvage;
        private readonly CargoTuning _tuning;
        private readonly Cutter _cutter;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly BeamWork _beam;
        private readonly ISaveService _save;
        private readonly PickupReach _pickups;
        private readonly CutState _cut;
        private SealedContainer _cutting;
        private bool _holding;
        private bool _gazing;

        /// <summary>Made before the finds' owners initialise, so each can add its containers.</summary>
        public SealedContainers(SalvageField salvage, CargoTuning tuning, Cutter cutter, GameplayServices services)
        {
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _cutter = cutter ?? throw new ArgumentNullException(nameof(cutter));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _beam = services.Beam;
            _save = services.Save;
            _pickups = services.Pickups;
            _cut = new CutState(_events, FeelKind.Cut, cutter.ChargeSeconds, cutter.ResumeChargeSeconds,
                salvage.Tuning.CutSteps);
        }

        /// <summary>The sealed container a hold of E would cut now, or null.</summary>
        public SealedContainer Candidate { get; private set; }

        /// <summary>The sealed container currently under the cutter beam, or null.</summary>
        public SealedContainer Cutting => _cutting;

        public bool IsCutting => _cutting != null && _cut.Phase == CutPhase.Cutting;

        public bool IsBeamCharging => _cutting != null && _cut.Phase == CutPhase.Charging;

        public float BeamChargeProgress => IsBeamCharging ? _cut.ChargeProgress : 0f;

        public int Count => _containers.Count;

        public SealedContainer Get(int index)
        {
            return _containers[index];
        }

        /// <summary>
        /// Adds a container read from Art's prefab (at initialisation). A duplicate id is a wiring bug.
        /// </summary>
        public void Add(SealedContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (Find(container.Id) != null)
            {
                throw new ArgumentException($"Container '{container.Id}' is added twice.", nameof(container));
            }

            _containers.Add(container);
        }

        /// <summary>The container with <paramref name="id"/>, or null.</summary>
        public SealedContainer Find(string id)
        {
            for (int i = 0; i < _containers.Count; i++)
            {
                if (string.Equals(_containers[i].Id, id, StringComparison.Ordinal))
                {
                    return _containers[i];
                }
            }

            return null;
        }

        public void Step(float now, float deltaTime)
        {
            bool busy = _salvage.Candidate != null || _salvage.Cutting != null ||
                        _pickups.TryGet(Time.frameCount, out _, out _);
            Candidate = busy ? null : Nearest();
            for (int i = 0; i < _containers.Count; i++)
            {
                StepLid(_containers[i], now);
            }

            if (_input.ExcavateHeld && Candidate != null && !_cutter.Ready)
            {
                _cutter.Refuse(Candidate.CutPoint.position);
            }

            bool wants = _input.ExcavateHeld && Candidate != null && _cutter.Ready &&
                         (_cutting != null || _beam.MayStart);
            SetHold(wants);
            UpdateGaze(wants);
            float engageSpeed = _cutting != null
                ? _salvage.Tuning.EngageSpeed * _salvage.Tuning.EngageHysteresis
                : _salvage.Tuning.EngageSpeed;
            SealedContainer targetContainer = _cutting ?? Candidate;
            bool hasLineOfSight = targetContainer != null &&
                                  (_rig == null || _rig.HasCutterLineOfSight(targetContainer.CutPoint.position));
            bool engaged = wants && (_rover == null || _rover.Speed <= engageSpeed) && hasLineOfSight;
            if (_cutting != null && (!engaged || Candidate != _cutting))
            {
                _cut.Release(_cutting.Progress, _cutting.CutPoint.position, _cutting.CutPoint.forward);
                _cutting = null;
            }

            if (engaged && _cutting == null)
            {
                _cutting = Candidate;
                _cut.Engage(_cutting.Progress, SalvageMaterial.Metal, _cutting.CutPoint.position,
                    _cutting.CutPoint.forward,
                    _cutter.Plan(_salvage.Tuning.Size(_tuning.ContainerCut).Stages, false));
            }

            UpdateGaze(wants);
            if (_cutting == null)
            {
                return;
            }

            if (_cut.Draws)
            {
                _beam.Work(deltaTime * _cutter.DrainFactor);
            }

            float progress = _cutting.Progress;
            bool broke = _cut.Step(deltaTime, SalvageYield.CutSeconds(_tuning.ContainerCut, _cutter.SpeedFactor,
                _salvage.Tuning), ref progress, _cutting.CutPoint.position, _cutting.CutPoint.forward, out _);
            _cutting.Progress = progress;
            if (broke)
            {
                Open(_cutting, now);
                _cutting = null;
                SetHold(false);
                UpdateGaze(false);
            }
        }

        private void SetHold(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            _rig?.SetHoldStill(this, hold);
        }

        private void UpdateGaze(bool wantsBeam)
        {
            SealedContainer look = _cutting ?? (wantsBeam ? Candidate : null);
            if (look != null)
            {
                _rig?.SetGazeTarget(this, look.CutPoint.position, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig?.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        /// <summary>
        /// A container whose find a save already took stands open at once (an older save, before containers): its
        /// owner says so after the load. Publishes nothing.
        /// </summary>
        public void StandOpen(string id)
        {
            SealedContainer container = Find(id) ?? throw new ArgumentException($"No container '{id}'.", nameof(id));
            LayOpen(container);
        }

        public ContainersSaveData Capture()
        {
            int opened = 0;
            for (int i = 0; i < _containers.Count; i++)
            {
                opened += _containers[i].Opened ? 1 : 0;
            }

            var data = new ContainersSaveData { opened = new string[opened] };
            int next = 0;
            for (int i = 0; i < _containers.Count; i++)
            {
                if (_containers[i].Opened)
                {
                    data.opened[next++] = _containers[i].Id;
                }
            }

            return data;
        }

        /// <summary>Containers a save says were opened stand open, their lids back; nothing is published.</summary>
        public void Restore(ContainersSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string[] opened = data.opened ?? Array.Empty<string>();
            for (int i = 0; i < _containers.Count; i++)
            {
                if (Array.IndexOf(opened, _containers[i].Id) >= 0)
                {
                    LayOpen(_containers[i]);
                }
            }
        }

        private static void LayOpen(SealedContainer container)
        {
            container.Opened = true;
            container.StandsOpen = true;
            container.Progress = 1f;
            container.OpenedAt = float.NegativeInfinity;
        }

        /// <summary>
        /// The sealed container in reach that 07 faces most squarely: two seals side by side (Tilly's sled's clamp
        /// and pod) are told apart by where 07 looks, not by a few centimetres. One outside the claim angle off 07's
        /// heading is not offered.
        /// </summary>
        private SealedContainer Nearest()
        {
            SealedContainer best = null;
            float reachSq = _salvage.Tuning.ReachRadius * _salvage.Tuning.ReachRadius;
            float bestFacing = Mathf.Cos(_tuning.ContainerClaimAngle * Mathf.Deg2Rad);
            bestFacing = bestFacing > -1f ? bestFacing : float.NegativeInfinity;
            Vector3 forward = _rover.Rotation * Vector3.forward;
            forward.y = 0f;
            for (int i = 0; i < _containers.Count; i++)
            {
                SealedContainer container = _containers[i];
                if (container.Opened || !container.Available)
                {
                    continue;
                }

                Vector3 toward = container.CutPoint.position - _rover.Position;
                toward.y = 0f;
                if (toward.sqrMagnitude > reachSq)
                {
                    continue;
                }

                float facing = toward.sqrMagnitude > 1e-6f ? Vector3.Dot(forward.normalized, toward.normalized) : 1f;
                if (facing > bestFacing)
                {
                    bestFacing = facing;
                    best = container;
                }
            }

            return best;
        }

        /// <summary>
        /// The seal glows while it can be cut, a small mark that eases down to a whisper as 07 arrives (the find's own
        /// glint takes over); once cut, the lid swings open and the container stands open.
        /// </summary>
        private void StepLid(SealedContainer container, float now)
        {
            float glow = !container.Opened && container.Available ? SealGlow(container) : 0f;
            container.Seal.Apply(Mathf.Max(glow, container.SignalBlink));
            float t = container.Opened ? Mathf.Clamp01((now - container.OpenedAt) / _tuning.ContainerLidSeconds) : 0f;
            container.Lid.localRotation = container.LidRest *
                                          Quaternion.AngleAxis(-_tuning.ContainerLidSwing * Ease.OutCubic(t),
                                              container.LidAxis);
            if (container.Opened && !container.StandsOpen && t >= 1f)
            {
                container.StandsOpen = true;
                _events.Publish(new ContainerOpened(container.Id, container.Kind, container.ItemSocket.position));
            }
        }

        private float SealGlow(SealedContainer container)
        {
            Vector2 fade = _tuning.ContainerSealFade;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, container.CutPoint.position);
            float far = Ease.InOutSine(Mathf.InverseLerp(fade.x, fade.y, distance));
            return Mathf.Lerp(_tuning.ContainerSealNearGlow, _tuning.ContainerSealGlow, far);
        }

        private void Open(SealedContainer container, float now)
        {
            container.Opened = true;
            container.OpenedAt = now;
            _events.Publish(new ContainerSealCut(container.Id, container.Kind, container.CutPoint.position));
            _save.SaveNow();
        }
    }
}
