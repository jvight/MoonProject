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
    /// the beam's reach, with no salvage piece, pickup or crew crate before it, holding E cuts a container's glowing
    /// seal
    /// once (the cut's own beats, <see cref="CutState"/>, at the cutter's tier): <see cref="ContainerSealCut"/>, the
    /// lid
    /// swings open about its local X, and as it stands open <see cref="ContainerOpened"/> lets its owner slide the find
    /// out. A save keeps which stand open. Allocation-free after the containers are added.
    /// </summary>
    public sealed class SealedContainers
    {
        private readonly List<SealedContainer> _containers = new List<SealedContainer>();
        private readonly SalvageField _salvage;
        private readonly CrewCrates _crates;
        private readonly CargoTuning _tuning;
        private readonly Cutter _cutter;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly BeamWork _beam;
        private readonly ISaveService _save;
        private readonly PickupReach _pickups;
        private readonly CutState _cut;
        private SealedContainer _cutting;

        public SealedContainers(SalvageField salvage, CrewCrates crates, CargoTuning tuning, Cutter cutter,
            GameplayServices services)
        {
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _crates = crates ?? throw new ArgumentNullException(nameof(crates));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _cutter = cutter ?? throw new ArgumentNullException(nameof(cutter));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _beam = services.Beam;
            _save = services.Save;
            _pickups = services.Pickups;
            _cut = new CutState(_events, FeelKind.Cut, cutter.ChargeSeconds, cutter.ResumeChargeSeconds,
                salvage.Tuning.CutSteps);
        }

        /// <summary>The sealed container a hold of E would cut now, or null.</summary>
        public SealedContainer Candidate { get; private set; }

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
            bool busy = _salvage.Candidate != null || _salvage.Cutting != null || _crates.Candidate != null ||
                        _pickups.TryGet(Time.frameCount, out _, out _);
            Candidate = busy ? null : Nearest();
            for (int i = 0; i < _containers.Count; i++)
            {
                StepLid(_containers[i], now);
            }

            bool wants = _input.ExcavateHeld && Candidate != null && (_cutting != null || _beam.MayStart);
            if (_cutting != null && (!wants || Candidate != _cutting))
            {
                _cut.Release(_cutting.Progress, _cutting.CutPoint.position, _cutting.CutPoint.forward);
                _cutting = null;
            }

            if (wants && _cutting == null)
            {
                _cutting = Candidate;
                _cut.Engage(_cutting.Progress, SalvageMaterial.Metal, _cutting.CutPoint.position,
                    _cutting.CutPoint.forward,
                    _cutter.Plan(_salvage.Tuning.Size(_tuning.ContainerCut).Stages, false));
            }

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

        private SealedContainer Nearest()
        {
            SealedContainer best = null;
            float bestSq = _salvage.Tuning.ReachRadius * _salvage.Tuning.ReachRadius;
            for (int i = 0; i < _containers.Count; i++)
            {
                SealedContainer container = _containers[i];
                if (container.Opened || !container.Available)
                {
                    continue;
                }

                float distanceSq = SurfaceRules.HorizontalDistanceSquared(_rover.Position,
                    container.CutPoint.position);
                if (distanceSq <= bestSq)
                {
                    bestSq = distanceSq;
                    best = container;
                }
            }

            return best;
        }

        /// <summary>
        /// The seal glows while it can be cut; once cut, the lid swings open and the container stands open.
        /// </summary>
        private void StepLid(SealedContainer container, float now)
        {
            container.Seal.Apply(!container.Opened && container.Available ? _tuning.SealGlow : 0f);
            float t = container.Opened ? Mathf.Clamp01((now - container.OpenedAt) / _tuning.ContainerLidSeconds) : 0f;
            container.Lid.localRotation =
                container.LidRest * Quaternion.Euler(-_tuning.ContainerLidSwing * Ease.OutCubic(t), 0f, 0f);
            if (container.Opened && !container.StandsOpen && t >= 1f)
            {
                container.StandsOpen = true;
                _events.Publish(new ContainerOpened(container.Id, container.Kind, container.ItemSocket.position));
            }
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
