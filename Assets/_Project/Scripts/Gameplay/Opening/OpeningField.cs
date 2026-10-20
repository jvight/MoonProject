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
        private const string CargoFragmentPrefix = "CargoFragment_";

        // Art's empty sockets on Crate_Cargo_Open where the pad fragments rest, above the rim: PartSocket_0, _1.
        private const string CargoSocketPrefix = "PartSocket_";

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
        /// <param name="storyProps">World's story props in the basin ([World]/BasinProps/&lt;id&gt;): frames for the
        /// near sites of R5.</param>
        /// <param name="kit">07's start kit: the spare igniter carried and fitted (B7).</param>
        public OpeningField(OpeningTuning tuning, GameplayServices services, BuildField building, ISunlight sunlight,
            SealedContainers containers, IReadOnlyList<Transform> ruins, IReadOnlyList<Transform> storyProps,
            Transform landerPort, Transform parent, float crateReach, RoverStartKit kit)
        {
            _kit = kit ?? throw new ArgumentNullException(nameof(kit));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (services == null || building == null || containers == null || ruins == null || storyProps == null ||
                parent == null || landerPort == null)
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

            foreach (Transform prop in storyProps)
            {
                _frames[prop.name] = prop;
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
            LanderCargo = CargoBay(Caches.RootOf(tuning.CargoCrate), Caches.LevelOf(tuning.CargoCrate));
            Dusk = new DuskCue(services.Events, DuskSites(tuning), tuning.DuskSiteReach);
            Flow = new OpeningFlow(services.Events, this, tuning.StallSeconds, tuning.ReminderSeconds);
        }

        public LanderPort Port { get; }

        public PadChargers Pads { get; }

        public SupplyCaches Caches { get; }

        public OpeningFlow Flow { get; }

        /// <summary>When the first dusk begins (R5–R6).</summary>
        public DuskCue Dusk { get; }

        /// <summary>The lander's cargo scan host, its open crate: Scan_1..N on what it holds (the pad's fragments).
        /// </summary>
        public Transform LanderCargo { get; }

        public bool NightFallenSaved { get; private set; }

        public bool ReachedLander => Port.Reached;

        public bool CarriesIgniter => _kit.CarriesIgniter;

        public bool CutterSputtered => _kit.Sputtered;

        public bool CanFitIgniter => _kit.CanFitIgniter;

        public bool IgniterFitted => _kit.IgniterFitted;

        public bool FirstCharge => Port.Drawn;

        public bool ReserveSpent => Port.Spent;

        public bool PowerOn => Pads.PowerOn;

        public bool DuskBegun => Dusk.Begun;

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
            Dusk.Announce();
        }

        public void Step(float deltaTime)
        {
            if (!Port.Reached && SurfaceRules.HorizontalDistance(_rover.Position, _lander) <= _tuning.LanderReach)
            {
                Port.Reach();
            }

            if (!NightFallenSaved && NightReady && _sunlight.Phase == SkyPhase.Earthlit)
            {
                NightFallenSaved = true;
            }

            Caches.Step(deltaTime);
            Dusk.Step(PowerOn, _rover.Position, Knows(StructureIds.BatteryBank));
            Flow.Tick(deltaTime, _rover.Speed <= _tuning.StillSpeed);
        }

        public OpeningSaveData Capture()
        {
            var data = new OpeningSaveData { nightFallen = NightFallenSaved };
            Port.Capture(data);
            Caches.Capture(data);
            Dusk.Capture(data);
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
            Dusk.Restore(data);
        }

        public void Dispose()
        {
            Caches.Dispose();
            Dusk.Dispose();
        }

        private static Transform FindNode(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>The frame called <paramref name="id"/> (a world anchor, a ruin piece, a story prop), or null.</summary>
        internal Transform FrameOf(string id)
        {
            return _frames.TryGetValue(id, out Transform frame) ? frame : null;
        }

        /// <summary>Where the near sites of R5 stand, each by its frame (a ruin piece or a story prop).</summary>
        private Vector3[] DuskSites(OpeningTuning tuning)
        {
            IReadOnlyList<string> ids = tuning.DuskSites;
            var sites = new Vector3[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                Transform frame = FrameOf(ids[i]) ?? throw new InvalidOperationException(
                    $"The first dusk's site '{ids[i]}' is no ruin piece or story prop in the world (OpeningTuning).");
                sites[i] = frame.position;
            }

            return sites;
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

        /// <summary>
        /// Art's pad fragments lying in the cargo crate, each on Art's PartSocket_N in order, level in the crate's
        /// <paramref name="level"/> frame; each fragment's own Scan_1 node becomes the set's Scan_1..N in order.
        /// </summary>
        private Transform CargoBay(Transform crate, Quaternion level)
        {
            IReadOnlyList<GameObject> fragments = _tuning.CargoFragments;
            for (int i = 0; i < fragments.Count; i++)
            {
                string number = (i + 1).ToString(CultureInfo.InvariantCulture);
                Transform socket = FindNode(crate, CargoSocketPrefix + i.ToString(CultureInfo.InvariantCulture))
                                   ?? throw new InvalidOperationException($"The cargo crate '{crate.name}' has no " +
                                       $"{CargoSocketPrefix}{i} for pad fragment {i + 1} (Art's Crate_Cargo_Open).");
                Transform rest = new GameObject(CargoFragmentPrefix + number).transform;
                rest.SetParent(crate, false);
                rest.SetPositionAndRotation(socket.position, level);
                GameObject fragment = fragments[i] != null ? fragments[i] : throw new InvalidOperationException(
                    $"OpeningTuning's cargo fragment {i + 1} is not assigned.");
                Transform piece = UnityEngine.Object.Instantiate(fragment, rest, false).transform;
                piece.localPosition = Vector3.zero;
                Transform target = FindNode(piece, CargoTargetPrefix + "1") ?? throw new InvalidOperationException(
                    $"Art's pad fragment '{fragment.name}' has no {CargoTargetPrefix}1 node.");
                target.name = CargoTargetPrefix + number;
            }

            return crate;
        }
    }
}
