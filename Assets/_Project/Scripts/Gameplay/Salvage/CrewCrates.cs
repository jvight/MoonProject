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
        private readonly CargoBundles _held;
        private readonly CargoTuning _tuning;
        private readonly Cutter _cutter;
        private readonly MaterialStock _stock;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly BeamWork _beam;
        private readonly ISaveService _save;
        private readonly PickupReach _pickups;
        private readonly CutState _cut;
        private readonly CrateFlight[] _flights;
        private CrewCrate _cutting;
        private bool _holding;
        private bool _gazing;

        private struct CrateFlight
        {
            public bool Active;
            public Transform Bundle;
            public Vector3 BaseScale;
            public Vector3 Start;
            public Quaternion StartRotation;
            public float StartTime;
            public float Duration;
            public int Index;
            public SalvageMaterial Material;
        }

        /// <param name="held">What does not fit waits in the crate's mouth (0.5.5 §2).</param>
        public CrewCrates(SalvageField salvage, CargoTuning tuning, Cutter cutter, GameplayServices services,
            CargoBundles held)
        {
            _held = held ?? throw new ArgumentNullException(nameof(held));
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
            _rig = services.Rig;
            _beam = services.Beam;
            _save = services.Save;
            _pickups = services.Pickups;
            _cut = new CutState(_events, FeelKind.Cut, cutter.ChargeSeconds, cutter.ResumeChargeSeconds,
                salvage.Tuning.CutSteps);

            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            _flights = new CrateFlight[sites.Count];
            for (int i = 0; i < sites.Count; i++)
            {
                SalvageMaterial material = sites[i].Crate.Contents.Material;
                GameObject prefab = _salvage.Catalog != null ? _salvage.Catalog.Bundle(material) : null;
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = UnityEngine.Object.Instantiate(prefab, _salvage.transform);
                instance.name = "Bundle_Crate_" + sites[i].Id;
                SetLayerRecursive(instance.transform, Layers.Pickup);
                instance.SetActive(false);
                _flights[i] = new CrateFlight
                {
                    Bundle = instance.transform,
                    BaseScale = instance.transform.localScale,
                    Index = i,
                    Material = material,
                };
            }
        }

        /// <summary>The sealed crate a hold of E would cut now, or null.</summary>
        public CrewCrate Candidate { get; private set; }

        /// <summary>The crate currently under the cutter beam, or null.</summary>
        public CrewCrate Cutting => _cutting;

        public bool IsCutting => _cutting != null && _cut.Phase == CutPhase.Cutting;

        public bool IsBeamCharging => _cutting != null && _cut.Phase == CutPhase.Charging;

        public float BeamChargeProgress => IsBeamCharging ? _cut.ChargeProgress : 0f;

        /// <summary>Number of crate bundles currently flying along their PickupFlight arc into 07's hold.</summary>
        public int InFlightCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _flights.Length; i++)
                {
                    if (_flights[i].Active)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

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

            StepFlights(now);

            bool wants = _input.ExcavateHeld && Candidate != null && (_cutting != null || _beam.MayStart);
            SetHold(wants);
            float engageSpeed = _cutting != null
                ? _salvage.Tuning.EngageSpeed * _salvage.Tuning.EngageHysteresis
                : _salvage.Tuning.EngageSpeed;
            bool engaged = wants && (_rover == null || _rover.Speed <= engageSpeed);
            if (_cutting != null && (!engaged || Candidate != _cutting))
            {
                _cut.Release(_cutting.Progress, _cutting.CutPoint.position, _cutting.CutPoint.forward);
                _cutting = null;
            }

            if (engaged && _cutting == null)
            {
                _cutting = Candidate;
                _cut.Engage(_cutting.Progress, _cutting.Contents.Material, _cutting.CutPoint.position,
                    _cutting.CutPoint.forward, _cutter.Plan(_salvage.Tuning.Size(_tuning.CrateCut).Stages, false));
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
            bool broke = _cut.Step(deltaTime, SalvageYield.CutSeconds(_tuning.CrateCut, _cutter.SpeedFactor,
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

        public CratesSaveData Capture()
        {
            int opened = 0;
            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                opened += sites[i].Crate.Opened ? 1 : 0;
            }

            var data = new CratesSaveData { opened = new string[opened], held = new int[opened] };
            int next = 0;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].Crate.Opened)
                {
                    data.held[next] = _held.HeldIn(sites[i].Crate);
                    data.opened[next++] = sites[i].Id;
                }
            }

            return data;
        }

        /// <summary>
        /// Crates a save says were opened stand open, their lids up, with what was held in their mouths waiting again.
        /// </summary>
        public void Restore(CratesSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IReadOnlyList<SalvageSite> sites = _salvage.Sites;
            string[] opened = data.opened ?? Array.Empty<string>();
            int[] held = data.held ?? Array.Empty<int>();
            for (int i = 0; i < sites.Count; i++)
            {
                if (_flights[i].Active)
                {
                    _flights[i].Active = false;
                    if (_flights[i].Bundle != null)
                    {
                        _flights[i].Bundle.gameObject.SetActive(false);
                    }
                }

                CrewCrate crate = sites[i].Crate;
                int at = Array.IndexOf(opened, sites[i].Id);
                crate.Opened = at >= 0;
                crate.Progress = crate.Opened ? 1f : 0f;
                crate.OpenedAt = float.NegativeInfinity;
                if (at >= 0 && at < held.Length && held[at] > 0)
                {
                    _held.HoldCrate(crate, crate.Contents.Material, held[at], false);
                }
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
                _stock.Collect(contents.Material, fitted);
                StartFlight(crate, contents.Material, now);
            }

            int left = contents.Units - fitted;
            if (left > 0)
            {
                _held.HoldCrate(crate, contents.Material, left, true);
            }

            _events.Publish(new CrewCrateOpened(crate.Site.Id, contents.CrewId, contents.Material, contents.Units, at));
            _save.SaveNow();
        }

        private void StartFlight(CrewCrate crate, SalvageMaterial material, float now)
        {
            int slot = crate.Site != null ? crate.Site.Index : -1;
            if (slot < 0 || slot >= _flights.Length || _flights[slot].Bundle == null || _rig?.CargoSocket == null)
            {
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, crate.CutPoint.position, Vector3.up,
                    1f, 0f, material));
                return;
            }

            Vector3 start = crate.Root.TransformPoint(crate.LidRest);
            Vector3 socket = _rig.CargoSocket.position;
            float duration = PickupFlight.Duration(Vector3.Distance(start, socket), _salvage.Tuning.FlightDuration,
                _salvage.Tuning.FlightPerMetre);
            ref CrateFlight flight = ref _flights[slot];
            flight.Active = true;
            flight.Start = start;
            flight.StartRotation = crate.Root.rotation;
            flight.StartTime = now;
            flight.Duration = duration;
            flight.Material = material;
            flight.Bundle.SetPositionAndRotation(start, crate.Root.rotation);
            flight.Bundle.localScale = flight.BaseScale;
            flight.Bundle.gameObject.SetActive(true);
            _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Anticipate, start, Vector3.up, 1f, duration,
                material));
        }

        private void StepFlights(float now)
        {
            if (_rig?.CargoSocket == null)
            {
                return;
            }

            Vector3 socket = _rig.CargoSocket.position;
            SalvageTuning tuning = _salvage.Tuning;
            for (int i = 0; i < _flights.Length; i++)
            {
                ref CrateFlight flight = ref _flights[i];
                if (!flight.Active)
                {
                    continue;
                }

                float elapsed = now - flight.StartTime;
                float progress = flight.Duration > 1e-4f ? elapsed / flight.Duration : 1f;
                if (progress >= 1f)
                {
                    flight.Active = false;
                    flight.Bundle.gameObject.SetActive(false);
                    _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, socket, Vector3.up, 1f, 0f,
                        flight.Material));
                    continue;
                }

                float direction = (flight.Index & 1) == 0 ? 1f : -1f;
                Vector3 pos = PickupFlight.Evaluate(flight.Start, socket, progress, flight.Index, direction,
                    tuning.FlightLift, tuning.SpiralRadius, tuning.SpiralTurns);
                Quaternion rot = Quaternion.Euler(0f, elapsed * tuning.FlightSpin, 0f) * flight.StartRotation;
                flight.Bundle.SetPositionAndRotation(pos, rot);
                flight.Bundle.localScale = flight.BaseScale *
                                           Mathf.Lerp(1f, tuning.ArrivalScale, Ease.InOutSine(Mathf.Clamp01(progress)));
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
            CrewCrate look = _cutting ?? (wantsBeam ? Candidate : null);
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

        private static void SetLayerRecursive(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursive(root.GetChild(i), layer);
            }
        }
    }
}
