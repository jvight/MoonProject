using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening in the world (docs/design/opening-v2.md): the landed lander (07 reaching it, its cargo bay's scan
    /// points, its port and one-charge reserve, <see cref="LanderPort"/>), the charging pads 07 builds
    /// (<see cref="PadChargers"/>), the sealed supply crates (<see cref="SupplyCaches"/>), the first nightfall over a
    /// night-ready base, and the objective flow that reads all of it (<see cref="OpeningFlow"/>, through
    /// <see cref="IOpeningProgress"/>). Saved as "gameplay.opening". Allocation-free per frame.
    /// </summary>
    public sealed class OpeningField : IOpeningProgress, IDisposable
    {
        /// <summary>The frame of World's service port on the lander, where the apron cable ends (B4's crate).</summary>
        public const string LanderPortFrame = "lander.port";

        private const string CargoTargetPrefix = "Scan_";

        private readonly OpeningTuning _tuning;
        private readonly IRoverState _rover;
        private readonly ISunlight _sunlight;
        private readonly KnownStructures _known;
        private readonly StructureCatalog _catalog;
        private readonly MaterialStock _stock;
        private readonly RoverStartKit _kit;
        private readonly Vector3 _lander;
        private readonly Dictionary<string, Transform> _frames =
            new Dictionary<string, Transform>(StringComparer.Ordinal);

        /// <param name="ruins">The Lumen ruin pieces by name ([World]/Ruins/&lt;id&gt;): frames for their
        /// crates.</param>
        /// <param name="landerPort">World's service port on the lander ([World]/Lander/ServicePort): the open crate's
        /// frame.</param>
        /// <param name="crateReach">Metres within which an open crate still holding something gives the rest.</param>
        /// <param name="kit">07's start kit: the spare igniter carried and fitted (B4, B6).</param>
        public OpeningField(OpeningTuning tuning, GameplayServices services, BuildField building, ISunlight sunlight,
            SealedContainers containers, IReadOnlyList<Transform> ruins, Transform landerPort, Transform parent,
            float crateReach, RoverStartKit kit)
        {
            _kit = kit ?? throw new ArgumentNullException(nameof(kit));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (services == null || building == null || containers == null || ruins == null || parent == null ||
                landerPort == null)
            {
                throw new ArgumentNullException(nameof(services), "The opening is missing an input.");
            }

            _sunlight = sunlight ?? throw new ArgumentNullException(nameof(sunlight));
            _rover = services.Rover;
            _stock = services.Materials;
            _known = building.Known;
            _catalog = building.Catalog;
            foreach (Transform ruin in ruins)
            {
                _frames[ruin.name] = ruin;
            }

            _frames[LanderPortFrame] = landerPort;

            WorldAnchor lander = Anchor(services.Anchors, tuning.LanderAnchor);
            WorldAnchor apron = Anchor(services.Anchors, tuning.ApronAnchor);
            _lander = lander.Position;
            Port = new LanderPort(services.Events, tuning.ReserveCells);
            Pads = new PadChargers(building.PlacedBase, Port, apron.Position, tuning);
            Transform frames = new GameObject("OpeningFrames").transform;
            frames.SetParent(parent, false);
            for (int i = 0; i < services.Anchors.Count; i++)
            {
                WorldAnchor anchor = services.Anchors.Get(i);
                _frames[anchor.Id] = Frame(anchor, frames);
            }

            Caches = new SupplyCaches(tuning, FrameOf, parent, services, containers, crateReach);
            LanderCargo = CargoBay(Caches.RootOf(tuning.CargoCrate));
            Flow = new OpeningFlow(services.Events, this);
        }

        public LanderPort Port { get; }

        public PadChargers Pads { get; }

        public SupplyCaches Caches { get; }

        public OpeningFlow Flow { get; }

        /// <summary>The lander's cargo scan host, its open crate: Scan_1..N on what it holds (the pad's fragments).
        /// </summary>
        public Transform LanderCargo { get; }

        public bool NightFallenSaved { get; private set; }

        public bool ReachedLander => Port.Reached;

        public bool CarriesIgniter => _kit.CarriesIgniter;

        public bool IgniterFitted => _kit.IgniterFitted;

        public bool FirstCharge => Port.Drawn;

        public bool ReserveSpent => Port.Spent;

        public bool PowerOn => Pads.PowerOn;

        public bool NightReady => Pads.NightReady;

        public bool NightFallen => NightFallenSaved;

        public bool Knows(string structureId)
        {
            return _known.IsKnown(structureId);
        }

        public bool Covers(string structureId)
        {
            if (!_catalog.TryGet(structureId, out StructureDefinition definition))
            {
                return false;
            }

            Recipe cost = definition.BuildCost;
            CargoHold hold = _stock.Hold;
            return hold.Units(SalvageMaterial.Metal) >= cost.Metal &&
                   hold.Units(SalvageMaterial.Wiring) >= cost.Wiring &&
                   hold.Units(SalvageMaterial.Optics) >= cost.Optics;
        }

        public bool Built(string structureId)
        {
            return Pads.Built(structureId);
        }

        /// <summary>
        /// The session starts (after the save is applied): the reserve and the reached milestones are stated.
        /// </summary>
        public void Announce()
        {
            Port.Announce();
            Flow.Announce();
        }

        public void Step()
        {
            if (!Port.Reached && SurfaceRules.HorizontalDistance(_rover.Position, _lander) <= _tuning.LanderReach)
            {
                Port.Reach();
            }

            if (!NightFallenSaved && NightReady && _sunlight.Phase == SkyPhase.Earthlit)
            {
                NightFallenSaved = true;
            }

            Caches.Step();
            Flow.Tick();
        }

        public OpeningSaveData Capture()
        {
            var data = new OpeningSaveData { nightFallen = NightFallenSaved };
            Port.Capture(data);
            Caches.Capture(data);
            return data;
        }

        public void Restore(OpeningSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            NightFallenSaved = data.nightFallen;
            Port.Restore(data);
            Caches.Restore(data);
        }

        public void Dispose()
        {
            Caches.Dispose();
        }

        private Transform FrameOf(string id)
        {
            return _frames.TryGetValue(id, out Transform frame) ? frame : null;
        }

        private static WorldAnchor Anchor(IWorldAnchors anchors, string id)
        {
            if (!anchors.TryGet(id, out WorldAnchor anchor))
            {
                throw new InvalidOperationException(
                    $"The opening stands on world anchor '{id}', which the World does not publish (OpeningTuning).");
            }

            return anchor;
        }

        private static Transform Frame(WorldAnchor anchor, Transform parent)
        {
            Transform frame = new GameObject("Frame_" + anchor.Id).transform;
            frame.SetParent(parent, false);
            Vector3 forward = anchor.Forward;
            forward.y = 0f;
            frame.SetPositionAndRotation(anchor.Position, Quaternion.LookRotation(
                forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward));
            return frame;
        }

        /// <summary>The cargo crate's Scan_1..N points in its own frame, on what it holds.</summary>
        private Transform CargoBay(Transform crate)
        {
            for (int i = 0; i < _tuning.CargoScanPoints.Count; i++)
            {
                Transform point = new GameObject(CargoTargetPrefix + (i + 1).ToString(CultureInfo.InvariantCulture))
                    .transform;
                point.SetParent(crate, false);
                point.localPosition = _tuning.CargoScanPoints[i];
            }

            return crate;
        }
    }
}
