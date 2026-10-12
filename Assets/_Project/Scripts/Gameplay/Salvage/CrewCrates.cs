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
    /// The crew's supply crates (docs/features/M3-25 T4): one sealed beside each site's heart, its seam glowing. Within
    /// the beam's reach, with no salvage piece picked and no part offered for pickup, holding E charges and cuts the seal once (the cut's own
    /// beats, <see cref="CutState"/>, at the cutter's tier); the lid lifts, the stack goes to the hold like a bundle
    /// (what does not fit waits beside 07) and <see cref="CrewCrateOpened"/> brings the packing note. The frame stays,
    /// empty. Allocation-free.
    /// </summary>
    public sealed class CrewCrates
    {
        private readonly SalvageField _salvage;
        private readonly CargoTuning _tuning;
        private readonly ICutter _cutter;
        private readonly MaterialStock _stock;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly BeamWork _beam;
        private readonly ISaveService _save;
        private readonly PickupReach _pickups;
        private readonly CutState _cut;
        private CrewCrate _cutting;

        public CrewCrates(SalvageField salvage, CargoTuning tuning, ICutter cutter, GameplayServices services)
        {
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _cutter = cutter ?? throw new ArgumentNullException(nameof(cutter));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _stock = services.Materials;
            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _beam = services.Beam;
            _save = services.Save;
            _pickups = services.Pickups;
            _cut = new CutState(_events, FeelKind.Cut, cutter.ChargeSeconds, cutter.ResumeChargeSeconds,
                salvage.Tuning.CutSteps);
        }

        /// <summary>The sealed crate a hold of E would cut now, or null.</summary>
        public CrewCrate Candidate { get; private set; }

        public void Step(float now, float deltaTime)
        {
            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            // A piece to cut or a part to pick up beside it comes first: one press, one verb.
            bool busy = _salvage.Candidate != null || _salvage.Cutting != null ||
                        _pickups.TryGet(Time.frameCount, out _, out _);
            Candidate = busy ? null : Nearest(sites);
            for (int i = 0; i < sites.Count; i++)
            {
                StepCrate(sites[i].Crate, now);
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
                _cut.Tune(_cutter.ChargeSeconds, _cutter.ResumeChargeSeconds);
                _cut.Engage(_cutting.Progress, _cutting.Contents.Material, _cutting.CutPoint.position,
                    _cutting.CutPoint.forward);
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
            bool broke = _cut.Step(deltaTime, SalvageYield.CutSeconds(_tuning.CrateCut, _cutter.SpeedFactor,
                _salvage.Tuning), ref progress, _cutting.CutPoint.position, _cutting.CutPoint.forward, out _);
            _cutting.Progress = progress;
            if (broke)
            {
                Open(_cutting, now);
                _cutting = null;
            }
        }

        public CratesSaveData Capture()
        {
            int opened = 0;
            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                opened += sites[i].Crate.Opened ? 1 : 0;
            }

            var data = new CratesSaveData { opened = new string[opened] };
            int next = 0;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].Crate.Opened)
                {
                    data.opened[next++] = sites[i].Id;
                }
            }

            return data;
        }

        /// <summary>Crates a save says were opened stand open and empty, their lids up.</summary>
        public void Restore(CratesSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                CrewCrate crate = sites[i].Crate;
                crate.Opened = Array.IndexOf(data.opened ?? Array.Empty<string>(), sites[i].Id) >= 0;
                crate.Progress = crate.Opened ? 1f : 0f;
                crate.OpenedAt = float.NegativeInfinity;
            }
        }

        private CrewCrate Nearest(IReadOnlyList<SalvageSite> sites)
        {
            CrewCrate best = null;
            float bestSq = _salvage.Tuning.ReachRadius * _salvage.Tuning.ReachRadius;
            for (int i = 0; i < sites.Count; i++)
            {
                CrewCrate crate = sites[i].Crate;
                if (crate.Opened || !sites[i].Discovered)
                {
                    continue;
                }

                float distanceSq = SurfaceRules.HorizontalDistanceSquared(_rover.Position, crate.CutPoint.position);
                if (distanceSq <= bestSq)
                {
                    bestSq = distanceSq;
                    best = crate;
                }
            }

            return best;
        }

        /// <summary>The lid's lift and the seal's glow: lit while sealed, dark once cut.</summary>
        private void StepCrate(CrewCrate crate, float now)
        {
            crate.Seal.Apply(crate.Opened ? 0f : _tuning.SealGlow);
            float t = Mathf.Clamp01((now - crate.OpenedAt) / _tuning.LidSeconds);
            float lift = crate.Opened ? Ease.OutCubic(t) : 0f;
            crate.Lid.localPosition = crate.LidRest + Vector3.up * (_tuning.LidLift * lift);
            crate.Lid.localRotation = crate.LidRestRotation * Quaternion.Euler(-_tuning.LidTilt * lift, 0f, 0f);
        }

        private void Open(CrewCrate crate, float now)
        {
            crate.Opened = true;
            crate.OpenedAt = now;
            SupplyCrate contents = crate.Contents;
            Vector3 at = crate.CutPoint.position;
            int fitted = Math.Min(contents.Units, _stock.Hold.FreeUnits(contents.Material));
            if (fitted > 0)
            {
                _events.Publish(new MaterialSalvaged(contents.Material, fitted, at, crate.Site.Id, 0,
                    MaterialSource.Crate));
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, at, Vector3.up, 1f, 0f,
                    contents.Material));
                _stock.Collect(contents.Material, fitted);
            }

            int left = contents.Units - fitted;
            if (left > 0)
            {
                _events.Publish(new FeelBeat(FeelKind.Cargo, FeelPhase.Impact, at, Vector3.up, 1f, 0f,
                    contents.Material));
                _events.Publish(new CargoOverflowed(contents.Material, left, at));
            }

            _events.Publish(new CrewCrateOpened(crate.Site.Id, contents.CrewId, contents.Material, contents.Units, at));
            _save.SaveNow();
        }
    }
}
