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
    /// The outpost's lost machines (docs/features/M3-02-friends-tilly.md, M3-05). Each friend lies broken at its site
    /// (planned on the basin floor, or at the World's anchors) with its missing parts around it, glinting amber.
    /// A press of E beside a part (<see cref="PickupReach"/>) draws it in (softly, amber) and lights a lamp on the friend
    /// (<see cref="FriendPartCollected"/>); an item its repair also needs (Bell's cassette) lights the next lamp as
    /// soon as 07 holds it (<see cref="IHeldItems"/>). With everything gathered, holding Interact near it starts a
    /// calm repair that always plays out: 07 holds still while its beam reaches the friend
    /// (<see cref="FriendRepairStarted"/>), the friend's body plays its own beat, and once it is up
    /// <see cref="FriendRepaired"/>, its gift and a save follow. Awake friends live at the base and greet 07 coming
    /// home (the first homecoming of a friend that announces it also puts a <see cref="TickerLine"/> on the radio).
    /// How each one looks and moves is its body's (<see cref="IFriendBody"/>: Tilly a <see cref="DroneBody"/>, Bell a
    /// <see cref="RadioCabinetBody"/>). Gifts: Tilly's spotter (this is its <see cref="ISpotTargets"/>), and Bell's
    /// radio dial (parked in front of her at home, Interact turns it one detent; a new relic makes her crackle). Also
    /// Core's <see cref="IFriendRoster"/> and the UI's <see cref="IFriendStatuses"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FriendField : MonoBehaviour, ISpotTargets, IFriendRoster, IFriendStatuses
    {
        /// <summary>Degrees between the idle headings of a friend's parts (so they never turn in step).</summary>
        private const float PartHeadingStep = 120f;

        /// <summary>Art's TillySled contract (0.5.5 §2): its clamp, its sensor pod and the part sockets.</summary>
        private const string SledClamp = "Clamp";
        private const string SledPod = "SensorPod";
        private const string SledPodLid = "SensorPod/Lid";
        private const string SledSocket = "PartSocket_";
        private const int SledParts = 3;
        private const int SledClampPart = 0;
        private const int SledHullPart = 1;
        private const int SledPodPart = 2;
        private const string DigSpotNode = "DigSpot";

        /// <summary>Degrees the buried part leans while half under the dust, easing upright as it is dug.</summary>
        private const float BuriedTilt = 28f;

        /// <summary>A part spins this many times faster in flight than at rest.</summary>
        private const float PartFlightSpin = 4f;

        /// <summary>Seeds a radio cabinet's life (foot tap timing) apart from its friend index.</summary>
        private const int CabinetSeed = 0x6E11;

        [Tooltip("Every friend (Assets/_Project/Data/Content/FriendCatalog.asset).")]
        [SerializeField] private FriendCatalog _catalog;

        [Tooltip("Friend tuning (Assets/_Project/Data/Tuning/Gameplay/FriendTuning.asset).")]
        [SerializeField] private FriendTuning _tuning;

        [Tooltip("Bell's tuning (Assets/_Project/Data/Tuning/Gameplay/BellTuning.asset), for radio cabinets.")]
        [SerializeField] private BellTuning _bellTuning;

        [Tooltip("Each friend's home socket at the base (FriendSocket_<id>, BellCorner), in catalog order.")]
        [SerializeField] private Transform[] _homes = Array.Empty<Transform>();

        private readonly List<Friend> _friends = new List<Friend>();
        private EventBus _events;
        private InputReader _input;
        private PickupReach _pickups;
        private BeamWork _beamWork;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RadioProgram _radio;
        private IHeldItems _items;
        private RelicField _relics;
        private SealedContainers _containers;
        private SalvageField _salvage;
        private SonarSystem _sonar;
        private PickupGlints _glints;
        private RepairBeam _beam;
        private RepairBeam _dialBeam;
        private float _tapStart = float.NegativeInfinity;
        private bool _tapping;
        private IDisposable _relicSubscription;
        private RadioCabinetBody _cabinet;
        private bool[] _spottedSites = Array.Empty<bool>();
        private float _holdTime;
        private bool _holding;
        private bool _gazing;
        private bool _interactHeld;
        private bool _initialized;

        public int Count => _friends.Count;

        /// <summary>The broken friend 07 could repair right now (every part gathered, within reach), or null.</summary>
        public Friend RepairCandidate { get; private set; }

        /// <summary>True while 07 is near a friend's buried part: the beam would dig it.</summary>
        public bool HasDigCandidate { get; private set; }

        /// <summary>Where the buried part 07 is near lies (valid while <see cref="HasDigCandidate"/>).</summary>
        public Vector3 DigCandidate { get; private set; }

        /// <summary>0..1 how far Interact has been held toward starting a repair.</summary>
        public float RepairHold => _tuning != null && _tuning.RepairHold > 0f ? _holdTime / _tuning.RepairHold : 0f;

        /// <summary>07 is parked in front of Bell's dial at home and could turn it now.</summary>
        public bool CanTune { get; private set; }

        /// <summary>True while 07's beam taps Bell's tuning knob, before she turns it.</summary>
        public bool TappingDial => _tapping;

        /// <summary>Current brightness of the dial-tapping beam (tests and debugging views).</summary>
        public float DialBeamLevel => _dialBeam != null ? _dialBeam.Level : 0f;

        public FriendTuning Tuning => _tuning;

        public BellTuning BellTuning => _bellTuning;

        /// <summary>The friend whose gift is the radio dial (Bell), or null.</summary>
        public Friend DialFriend { get; private set; }

        internal IReadOnlyList<Friend> Friends => _friends;

        internal PickupGlints Glints => _glints;

        internal void Wire(FriendCatalog catalog, FriendTuning tuning, BellTuning bellTuning, Transform[] homes)
        {
            _catalog = catalog;
            _tuning = tuning;
            _bellTuning = bellTuning;
            _homes = homes;
        }

        public Friend Find(string id)
        {
            for (int i = 0; i < _friends.Count; i++)
            {
                if (string.Equals(_friends[i].Definition.Id, id, StringComparison.Ordinal))
                {
                    return _friends[i];
                }
            }

            return null;
        }

        public IFriendState Get(int index)
        {
            return _friends[index];
        }

        public FriendDefinition Definition(int index)
        {
            return _friends[index].Definition;
        }

        public FriendStatus Status(int index)
        {
            Friend friend = _friends[index];
            FriendProgress progress = friend.Progress;
            return new FriendStatus(progress.State, progress.Collected, progress.PartCount, progress.ItemsCollected,
                progress.ItemCount, progress.Discovered, progress.CanRepair, friend.Position);
        }

        /// <summary>Where Bell's dial is when 07 could turn it now (the Tune prompt).</summary>
        public bool TryGetDial(out Vector3 position)
        {
            position = CanTune ? _cabinet.Position : Vector3.zero;
            return CanTune;
        }

        internal bool Initialize(GameplayServices services, RadioProgram radio, CassetteCatalog cassettes,
            RelicField relics, SalvageField salvage, HomeBase home, SealedContainers containers)
        {
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            string problem = _catalog == null ? "FriendCatalog is not assigned."
                : _tuning == null ? "FriendTuning is not assigned."
                : _catalog.Validate() ?? HomeProblem() ?? CabinetProblem(cassettes);
            if (problem != null)
            {
                Debug.LogError($"{nameof(FriendField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _pickups = services.Pickups;
            _beamWork = services.Beam;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _terrain = services.Terrain;
            _save = services.Save;
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _items = radio;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            if (home == null)
            {
                throw new ArgumentNullException(nameof(home));
            }

            _spottedSites = new bool[salvage.Sites.Count];

            int partCount = 0;
            IReadOnlyList<FriendDefinition> definitions = _catalog.Friends;
            for (int i = 0; i < definitions.Count; i++)
            {
                Friend friend = Spawn(definitions[i], i, services, cassettes, home);
                if (friend == null)
                {
                    enabled = false;
                    return false;
                }

                _friends.Add(friend);
                partCount += definitions[i].Parts.Count;
            }

            _glints = new PickupGlints(transform, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                Mathf.Max(1, partCount),
                Layers.Pickup);
            _beam = new RepairBeam("RepairBeam", transform, services.Visuals.TetherBeam, _tuning.StitchRate,
                _tuning.StitchSpread);
            _dialBeam = new RepairBeam("DialBeam", transform, services.Visuals.TetherBeam, 0f, 0f);
            if (_cabinet != null)
            {
                _relicSubscription = _events.Subscribe<RelicDeposited>(OnRelicDeposited);
            }

            _initialized = true;
            return true;
        }

        /// <summary>The sonar reveals relics the spotter finds (connected once the sonar is initialised).</summary>
        internal void Connect(SonarSystem sonar)
        {
            _sonar = sonar != null ? sonar : throw new ArgumentNullException(nameof(sonar));
        }

        internal FriendsSaveData Capture()
        {
            var data = new FriendsSaveData { friends = new FriendSaveData[_friends.Count] };
            for (int i = 0; i < _friends.Count; i++)
            {
                data.friends[i] = _friends[i].Progress.Capture();
            }

            return data;
        }

        /// <summary>Applies saved progress by id; awake friends come back at home, their gifts with them.</summary>
        internal void Restore(FriendsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (FriendSaveData saved in data.friends)
            {
                Friend friend = saved != null ? Find(saved.id) : null;
                if (friend == null)
                {
                    continue;
                }

                friend.Progress.Restore(saved);
                for (int part = 0; part < friend.Parts.Length; part++)
                {
                    bool collected = friend.Progress.IsCollected(part);
                    SealedContainer box = friend.PartContainers[part];
                    friend.PartFlying[part] = false;
                    friend.PartDug[part] = collected ? _tuning.PartDigSeconds : 0f;
                    friend.Parts[part].gameObject.SetActive(!collected && (box == null || box.StandsOpen));
                    if (collected && box != null)
                    {
                        _containers.StandOpen(box.Id);
                    }
                }

                bool awake = friend.Progress.State == FriendState.Awake;
                if (awake)
                {
                    friend.Body.SettleAtHome(Time.time);
                    Gift(friend);
                }

                friend.Activity = friend.Body.Activity;
                friend.RotorSpeed = friend.Body.Motor;
                friend.RepairProgress = awake ? 1f : 0f;
            }
        }

        public bool TryFind(Vector3 around, float radius, out SpotTarget target)
        {
            float radiusSq = radius * radius;
            target = default;
            float best = radiusSq;
            bool found = false;
            for (int i = 0; i < _salvage.Sites.Count; i++)
            {
                SalvageSite site = _salvage.Sites[i];
                bool hidden = site.AnswersSonar && !site.Discovered && !_spottedSites[i];
                float distance = SurfaceRules.HorizontalDistanceSquared(around, site.Position);
                if (hidden && distance <= best)
                {
                    best = distance;
                    target = new SpotTarget(SpotKind.Site, i, -1, site.Position);
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            for (int f = 0; f < _friends.Count; f++)
            {
                Friend friend = _friends[f];
                for (int p = 0; p < friend.Parts.Length; p++)
                {
                    bool waiting = !friend.Progress.IsCollected(p) && !friend.PartFlying[p] && !friend.PartSpotted[p];
                    float distance = SurfaceRules.HorizontalDistanceSquared(around, friend.PartRest[p]);
                    if (waiting && distance <= best)
                    {
                        best = distance;
                        target = new SpotTarget(SpotKind.Part, f, p, friend.PartRest[p]);
                        found = true;
                    }
                }
            }

            return found;
        }

        public void MarkSpotted(SpotTarget target)
        {
            switch (target.Kind)
            {
                case SpotKind.Site:
                    _spottedSites[target.Index] = true;
                    _sonar?.RevealSite(target.Index, _tuning.SpotPillar);
                    break;
                case SpotKind.Part:
                    _friends[target.Index].PartSpotted[target.Part] = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target.Kind, "Unknown spot kind.");
            }
        }

        private string HomeProblem()
        {
            IReadOnlyList<FriendDefinition> friends = _catalog.Friends;
            if (_homes.Length != friends.Count)
            {
                return $"{_homes.Length} home sockets wired for {friends.Count} friends.";
            }

            for (int i = 0; i < friends.Count; i++)
            {
                if (_homes[i] == null)
                {
                    return $"'{friends[i].Id}' has no home socket ({friends[i].HomeSocket}).";
                }
            }

            return null;
        }

        /// <summary>
        /// The radio dial lives on a radio cabinet (and only there), which needs Bell's tuning and its tape among the
        /// cassettes; there is one radio; a drone is placed by the planner.
        /// </summary>
        private string CabinetProblem(CassetteCatalog cassettes)
        {
            if (cassettes == null)
            {
                return "no CassetteCatalog was handed over.";
            }

            int dials = 0;
            foreach (FriendDefinition friend in _catalog.Friends)
            {
                bool dial = friend.AbilityId == FriendDefinition.RadioDialAbility;
                bool cabinet = friend.Body == FriendBodyKind.RadioCabinet;
                if (dial != cabinet)
                {
                    return $"'{friend.Id}': the radio dial and the radio cabinet body go together.";
                }

                if (!cabinet && friend.SiteRule != FriendSiteRule.Planner)
                {
                    return $"'{friend.Id}': a drone lies where the basin planner puts it (its seed drives its life).";
                }

                if (!cabinet)
                {
                    continue;
                }

                dials++;

                if (_bellTuning == null)
                {
                    return $"'{friend.Id}' is a radio cabinet but BellTuning is not assigned.";
                }

                if (Tape(cassettes, friend.Items[0]) == null)
                {
                    return $"'{friend.Id}' slides in tape '{friend.Items[0]}', which is not in the cassette catalog.";
                }
            }

            return dials > 1 ? $"{dials} friends bring the radio dial; there is one radio." : null;
        }

        private static GameObject Tape(CassetteCatalog cassettes, string id)
        {
            foreach (CassetteDefinition cassette in cassettes.Cassettes)
            {
                if (string.Equals(cassette.Id, id, StringComparison.Ordinal))
                {
                    return cassette.Prefab;
                }
            }

            return null;
        }

        /// <summary>Its site, parts and body; null (logged) when the World lacks an anchor it needs.</summary>
        private Friend Spawn(FriendDefinition definition, int index, GameplayServices services,
            CassetteCatalog cassettes, HomeBase home)
        {
            FriendSite site = PlanSite(definition, services);
            if (site == null)
            {
                return null;
            }

            if (definition.Body == FriendBodyKind.RadioCabinet)
            {
                site = AgainstTheWall(site);
            }

            var root = new GameObject("Friend_" + definition.Id).transform;
            root.SetParent(transform, false);
            var parts = new Transform[definition.Parts.Count];
            var boxes = new SealedContainer[parts.Length];
            var pockets = new Transform[parts.Length];
            var spots = new Vector3[parts.Length];
            bool inSled = definition.Id == _tuning.BuriedPartFriend;
            bool inPockets = definition.Id == _tuning.RockfallPartFriend;
            if (inSled && !LayTheSled(definition, site, root, services.Visuals, boxes, spots))
            {
                return null;
            }

            if (inPockets && !LayThePockets(definition, site, root, services, pockets, spots))
            {
                return null;
            }

            for (int p = 0; p < parts.Length; p++)
            {
                GameObject piece = Instantiate(definition.Parts[p].Prefab, spots[p],
                    Quaternion.Euler(0f, p * PartHeadingStep, 0f), root);
                SetLayer(piece.transform, Layers.Pickup);
                parts[p] = piece.transform;
            }

            var progress = new FriendProgress(definition.Id, definition.Parts.Count, definition.Items.Count);
            IFriendBody body;
            if (definition.Body == FriendBodyKind.Drone)
            {
                body = SpawnDrone(definition, index, site, root, services, home);
            }
            else
            {
                body = SpawnCabinet(definition, index, site, root, services, progress, cassettes);
                if (body == null)
                {
                    return null;
                }
            }

            var friend = new Friend(definition, index, site, progress, parts, _homes[index], body);
            if (inSled)
            {
                friend.InSled = true;
                friend.BuriedPart = SledHullPart;
                friend.PartBuried[SledHullPart] = true;
                for (int p = 0; p < parts.Length; p++)
                {
                    friend.PartContainers[p] = boxes[p];
                    parts[p].gameObject.SetActive(boxes[p] == null);
                }
            }

            if (inPockets)
            {
                friend.BuriedPart = 0;
                for (int p = 0; p < parts.Length; p++)
                {
                    friend.PartBuried[p] = true;
                    friend.PartPockets[p] = pockets[p];
                }
            }

            if (definition.AbilityId == FriendDefinition.RadioDialAbility)
            {
                DialFriend = friend;
            }

            return friend;
        }

        /// <summary>
        /// Lays Art's rockfall pockets at the friend's part anchors (0.5.5 §2), each set against the alcove's back wall
        /// facing out toward the canyon floor (-alcove.Forward), grounded on the terrain, with the part resting at its
        /// DigSpot. False (logged) when the pocket or its DigSpot node is missing.
        /// </summary>
        private bool LayThePockets(FriendDefinition definition, FriendSite site, Transform parent,
            GameplayServices services, Transform[] pockets, Vector3[] spots)
        {
            GameObject prefab = services.Visuals.Container(ContainerKinds.RockfallPocket);
            if (prefab == null)
            {
                Debug.LogError($"{nameof(FriendField)}: '{definition.Id}' rests her parts in Art's " +
                               $"{ContainerKinds.RockfallPocket}, which is not in GameplayVisuals " +
                               "(run Art/Props and Gameplay/Materials).", this);
                return false;
            }

            for (int p = 0; p < spots.Length; p++)
            {
                Vector3 back = -site.Facing;
                float maxSetback = _tuning.PocketMaxSetback;
                if (definition.SiteRule == FriendSiteRule.Anchors &&
                    services.Anchors.TryGet(definition.Anchors.PartAnchorPrefix + p, out WorldAnchor alcove))
                {
                    var flat = new Vector3(alcove.Forward.x, 0f, alcove.Forward.z);
                    if (flat.sqrMagnitude > 1e-4f)
                    {
                        back = flat.normalized;
                    }

                    maxSetback = Mathf.Min(alcove.Radius, _tuning.PocketMaxSetback);
                }

                Vector3 at = site.Parts[p];
                Vector3 origin = at + Vector3.up * _tuning.PocketWallProbeHeight;
                if (Physics.Raycast(origin, back, out RaycastHit wall, _tuning.PocketWallProbe, Layers.DriveableMask,
                        QueryTriggerInteraction.Ignore))
                {
                    float reach = Mathf.Clamp(wall.distance - _tuning.PocketBackReach, 0f, maxSetback);
                    at += back * reach;
                }

                Transform pocket = Instantiate(prefab, parent).transform;
                pocket.name = ContainerKinds.RockfallPocket + "_" + p;
                pocket.SetPositionAndRotation(SurfaceRules.OnSurface(_terrain, at.x, at.z),
                    Quaternion.LookRotation(-back));
                Transform digSpot = pocket.Find(DigSpotNode);
                if (digSpot == null)
                {
                    Debug.LogError($"{nameof(FriendField)}: '{pocket.name}' has no '{DigSpotNode}'.", this);
                    return false;
                }

                pockets[p] = pocket;
                spots[p] = digSpot.position;
            }

            return true;
        }

        /// <summary>
        /// Lays Art's crashed survey sled by the friend (0.5.5 §2) and her parts in it: behind its clamp
        /// (PartSocket_0, cut free), in the gap under its tipped hull (PartSocket_1, dug) and in its sensor pod
        /// (PartSocket_2, cut open). False (logged) when the sled or its contract is missing.
        /// </summary>
        private bool LayTheSled(FriendDefinition definition, FriendSite site, Transform parent, GameplayVisuals visuals,
            SealedContainer[] boxes, Vector3[] spots)
        {
            GameObject prefab = visuals.Container(ContainerKinds.TillySled);
            if (prefab == null || spots.Length != SledParts)
            {
                Debug.LogError($"{nameof(FriendField)}: '{definition.Id}' rests her {SledParts} parts in Art's " +
                               $"{ContainerKinds.TillySled}, which is not in GameplayVisuals " +
                               $"(run Gameplay/Materials), or she has {spots.Length} parts.", this);
                return false;
            }

            Vector3 facing = site.Facing;
            Vector3 right = new Vector3(facing.z, 0f, -facing.x);
            Vector3 at = site.Position + right * _tuning.SledOffset.x + facing * _tuning.SledOffset.y;
            Transform sled = Instantiate(prefab, parent).transform;
            sled.name = ContainerKinds.TillySled;
            sled.SetPositionAndRotation(SurfaceRules.OnSurface(_terrain, at.x, at.z),
                Quaternion.LookRotation(facing) * Quaternion.Euler(0f, _tuning.SledYaw, 0f));
            ContainerKind kind = ContainerKinds.Of(ContainerKinds.TillySled);
            boxes[SledClampPart] = SealedContainer.Read($"container.{definition.Id}.clamp", kind, sled, SledClamp,
                SledSocket + SledClampPart, () => true, out string problem);
            boxes[SledPodPart] = problem == null
                ? SealedContainer.Read($"container.{definition.Id}.sensor_pod", kind, sled, SledPodLid,
                    SledPod + "/" + SledSocket + SledPodPart, () => true, out problem)
                : null;
            Transform hull = problem == null ? sled.Find(SledSocket + SledHullPart) : null;
            if (problem != null || hull == null)
            {
                string missing = problem ?? $"'{sled.name}' has no '{SledSocket}{SledHullPart}'";
                Debug.LogError($"{nameof(FriendField)}: {missing}", this);
                return false;
            }

            _containers.Add(boxes[SledClampPart]);
            _containers.Add(boxes[SledPodPart]);
            spots[SledClampPart] = boxes[SledClampPart].PresentPoint;
            spots[SledHullPart] = hull.position;
            spots[SledPodPart] = boxes[SledPodPart].PresentPoint;
            return true;
        }

        private FriendSite PlanSite(FriendDefinition definition, GameplayServices services)
        {
            if (definition.SiteRule == FriendSiteRule.Anchors)
            {
                FriendSite anchored = FriendAnchorPlanner.Plan(services.Anchors, _terrain, definition.Anchors,
                    definition.Parts.Count, out string problem);
                if (anchored == null)
                {
                    Debug.LogError($"{nameof(FriendField)}: '{definition.Id}' cannot be placed: {problem} " +
                                   "(world anchors contract).", this);
                }

                return anchored;
            }

            FriendSite site = FriendSitePlanner.Plan(_terrain, services.Layout, definition.Placement, _tuning,
                _relics.Sites);
            if (!site.InCrater || !site.Visible)
            {
                Debug.LogWarning($"{nameof(FriendField)}: '{definition.Id}' lies at the best spot found " +
                                 $"(in a crater: {site.InCrater}, seen from the base: {site.Visible}).", this);
            }

            return site;
        }

        /// <summary>
        /// Bell lies tipped back against a wall: feel for the wall's real surface (its collider, which the low-poly
        /// rendering follows, not the analytic height) behind her placed spot at the height of her farthest-back point
        /// and set her down so that point just meets it, never deeper than she was placed. Nothing behind her within
        /// reach (open ground): she stays where she was placed.
        /// </summary>
        private FriendSite AgainstTheWall(FriendSite site)
        {
            Vector3 back = -site.Facing;
            Vector3 origin = site.Position - back * _bellTuning.WallProbe + Vector3.up * _bellTuning.WallProbeHeight;
            if (!Physics.Raycast(origin, back, out RaycastHit wall, _bellTuning.WallProbe * 3f, Layers.DriveableMask,
                    QueryTriggerInteraction.Ignore))
            {
                return site;
            }

            float reach = wall.distance - _bellTuning.BrokenBackReach - _bellTuning.WallGap;
            if (reach >= _bellTuning.WallProbe)
            {
                return site;
            }

            Vector3 spot = origin + back * reach;
            return new FriendSite(SurfaceRules.OnSurface(_terrain, spot.x, spot.z), site.Normal, site.Facing,
                site.Parts, site.InCrater, site.Visible);
        }

        private DroneBody SpawnDrone(FriendDefinition definition, int index, FriendSite site, Transform root,
            GameplayServices services, HomeBase home)
        {
            Quaternion facing = Quaternion.LookRotation(site.Facing);
            GameObject broken = Instantiate(definition.BrokenPrefab, site.Position, facing, root);
            GameObject repaired = Instantiate(definition.RepairedPrefab, site.Position, facing, root);
            int lamps = definition.Parts.Count + definition.Items.Count;
            var brokenRig = new FriendRig(broken, lamps);
            var repairedRig = new FriendRig(repaired, lamps) { Visible = false };
            var cone = new GlowRenderer(GlowObject.Create("SpotLight_" + definition.Id, transform,
                services.Meshes.Cone, services.Visuals.TractorBeam));
            var behaviour = new FriendBehaviour(_tuning, _terrain, this, definition.Placement.Seed + index);
            return new DroneBody(_tuning, _terrain, _rover, _view, home, _homes[index], brokenRig, repairedRig,
                behaviour, RepairSequence.For(definition, _tuning), cone, site.Position, index);
        }

        private RadioCabinetBody SpawnCabinet(FriendDefinition definition, int index, FriendSite site,
            Transform root, GameplayServices services, FriendProgress progress, CassetteCatalog cassettes)
        {
            Vector3[] wayHome = { _homes[index].position };
            if (definition.SiteRule == FriendSiteRule.Anchors)
            {
                wayHome = FriendAnchorPlanner.WayHome(services.Anchors, _terrain, definition.Anchors,
                    _homes[index].position, _bellTuning.BelowStep, _tuning, out string problem);
                if (wayHome == null)
                {
                    Debug.LogError($"{nameof(FriendField)}: '{definition.Id}' has no way home: {problem} " +
                                   "(world anchors contract).", this);
                    return null;
                }
            }

            int lamps = definition.Parts.Count + definition.Items.Count;
            var broken = new BellRig(Instantiate(definition.BrokenPrefab, root), lamps);
            var repaired = new BellRig(Instantiate(definition.RepairedPrefab, root), lamps);
            broken.MakeSolid(_bellTuning.BodyPadding);
            repaired.MakeSolid(_bellTuning.BodyPadding);
            GameObject tape = Instantiate(Tape(cassettes, definition.Items[0]), root);
            tape.name = "Tape_" + definition.Items[0];
            var life = new BellLife(_bellTuning, _tuning, CabinetSeed + index);
            _cabinet = new RadioCabinetBody(_tuning, _bellTuning, _terrain, _rover, _rig, _view, _radio, _events,
                progress, _homes[index], broken, repaired, tape.transform,
                BellRepairSequence.For(definition, _bellTuning), life, site, wayHome);
            return _cabinet;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            _glints.Begin(_view.Camera.transform.position, now);
            RepairCandidate = null;
            HasDigCandidate = false;
            Friend repairing = null;
            for (int i = 0; i < _friends.Count; i++)
            {
                Friend friend = _friends[i];
                switch (friend.Progress.State)
                {
                    case FriendState.Repairing:
                        repairing = friend;
                        StepRepair(friend, now, deltaTime);
                        break;
                    case FriendState.Awake:
                        StepAwake(friend, now, deltaTime);
                        break;
                    default:
                        StepBroken(friend, now, deltaTime);
                        break;
                }
            }

            _glints.End();
            StepRepairInput(deltaTime);
            StepTune(now, deltaTime);
            StepBeam(repairing, now, deltaTime);
            StepGaze(repairing);
        }

        private void StepBroken(Friend friend, float now, float deltaTime)
        {
            Vector3 socket = _rig.CargoSocket.position;
            Vector3 rover = _rover.Position;
            for (int p = 0; p < friend.Parts.Length; p++)
            {
                if (friend.Progress.IsCollected(p))
                {
                    continue;
                }

                Transform piece = friend.Parts[p];
                SealedContainer box = friend.PartContainers[p];
                if (box != null && !friend.PartFlying[p] && !Shown(friend, p, box, now))
                {
                    continue;
                }

                if (friend.IsPartBuried(p) && friend.PartDug[p] < _tuning.PartDigSeconds)
                {
                    StepBuried(friend, p, piece, rover, deltaTime);
                    continue;
                }

                if (!friend.PartFlying[p])
                {
                    Vector3 rest = InTheSled(friend, p, box, now);
                    piece.SetPositionAndRotation(rest, Quaternion.Euler(0f, p * PartHeadingStep, 0f));
                    _glints.Add(friend.PartRest[p], p);
                    if (_pickups.Offer(Time.frameCount, _input.ExcavateHeld, PickupReach.Friends,
                            friend.Index * FriendProgress.MaxParts + p, rest, rover, _tuning.PartReachRadius,
                            friend.PartNouns[p]))
                    {
                        friend.PartFlying[p] = true;
                        friend.PartStart[p] = rest;
                        friend.PartFlightTime[p] = 0f;
                        friend.PartFlightDuration[p] = PickupFlight.Duration(Vector3.Distance(rest, socket),
                            _tuning.PartFlightDuration, _tuning.PartFlightPerMetre);
                        PickupReach.Lifted(_events, rest, friend.PartFlightDuration[p]);
                    }

                    continue;
                }

                friend.PartFlightTime[p] += deltaTime;
                float progress = friend.PartFlightTime[p] / friend.PartFlightDuration[p];
                if (progress >= 1f)
                {
                    PickupReach.Landed(_events, socket);
                    Collect(friend, p);
                    continue;
                }

                Vector3 position = PickupFlight.Evaluate(friend.PartStart[p], socket, progress, p, 1f,
                    _tuning.PartFlightLift, _tuning.PartSpiralRadius, _tuning.PartSpiralTurns);
                piece.SetPositionAndRotation(position,
                    Quaternion.Euler(0f, now * _tuning.PartSpin * PartFlightSpin, 0f));
                piece.localScale = friend.PartScale[p] *
                                   Mathf.Lerp(1f, _tuning.PartArrivalScale, Ease.InOutSine(progress));
            }

            CollectHeldItems(friend);
            friend.Body.StepBroken(now, deltaTime);
            StepLamps(friend, deltaTime);
            friend.Activity = friend.Body.Activity;
            friend.RotorSpeed = friend.Body.Motor;
            friend.RepairProgress = 0f;
            if (friend.Progress.CanRepair &&
                SurfaceRules.HorizontalDistance(_rover.Position, friend.Site.Position) <= _tuning.RepairRadius)
            {
                RepairCandidate = friend;
            }
        }

        private void Collect(Friend friend, int part)
        {
            friend.PartFlying[part] = false;
            friend.Parts[part].gameObject.SetActive(false);
            if (friend.Progress.CollectPart(part))
            {
                _events.Publish(new FriendPartCollected(friend.Definition.Id, part, friend.Progress.Collected,
                    friend.Progress.PartCount));
            }
        }

        /// <summary>A required item counts as soon as 07 holds it, wherever it was found.</summary>
        private void CollectHeldItems(Friend friend)
        {
            IReadOnlyList<string> items = friend.Definition.Items;
            for (int item = 0; item < items.Count; item++)
            {
                if (!friend.Progress.IsItemCollected(item) && _items.Holds(items[item]))
                {
                    friend.Progress.CollectItem(item);
                }
            }
        }

        /// <summary>Part lamps fill in order as parts arrive; each item's lamp follows its own item.</summary>
        private void StepLamps(Friend friend, float deltaTime)
        {
            FriendProgress progress = friend.Progress;
            int lit = progress.Collected;
            int lamps = progress.PartCount + progress.ItemCount;
            for (int lamp = 0; lamp < lamps; lamp++)
            {
                bool on = lamp < progress.PartCount ? lamp < lit : progress.IsItemCollected(lamp - progress.PartCount);
                float target = on ? _tuning.PartLampGlow : 0f;
                friend.LampLevels[lamp] = Damp.Toward(friend.LampLevels[lamp], target, _tuning.PartLampEase,
                    deltaTime);
                friend.Body.SetLamp(lamp, friend.LampLevels[lamp]);
            }
        }

        /// <summary>
        /// The buried part sits half under the dust, glinting; holding the beam near it digs it up, rising as it goes,
        /// and once free it is collected like the others.
        /// </summary>
        private void StepBuried(Friend friend, int p, Transform piece, Vector3 rover, float deltaTime)
        {
            Vector3 rest = friend.PartRest[p];
            float reach = _tuning.PartDigRadius;
            // One press, one verb: a seal in reach (her sled's clamp or pod) is cut before the beam digs.
            bool near = (rest - rover).sqrMagnitude <= reach * reach && _containers.Candidate == null;
            if (near)
            {
                HasDigCandidate = true;
                DigCandidate = rest;
                if (_input.ExcavateHeld && _beamWork.MayStart)
                {
                    friend.PartDug[p] += deltaTime;
                }
            }

            float dug = Mathf.Clamp01(friend.PartDug[p] / _tuning.PartDigSeconds);
            piece.SetPositionAndRotation(rest + Vector3.down * (_tuning.BuriedPartSink * (1f - Ease.OutCubic(dug))),
                Quaternion.Euler(0f, p * PartHeadingStep, BuriedTilt * (1f - dug)));
            _glints.Add(rest, p);
        }

        /// <summary>
        /// A part sealed in its clamp or pod stays hidden until the container stands open, then comes into view. True
        /// once it is in view.
        /// </summary>
        private static bool Shown(Friend friend, int p, SealedContainer box, float now)
        {
            Transform piece = friend.Parts[p];
            if (!box.StandsOpen)
            {
                if (piece.gameObject.activeSelf)
                {
                    piece.gameObject.SetActive(false);
                }

                return false;
            }

            if (!piece.gameObject.activeSelf)
            {
                piece.gameObject.SetActive(true);
                friend.PartShownAt[p] = now;
            }

            return true;
        }

        /// <summary>
        /// Where a sled part rests: still at its socket, or rising off its container's socket to the open mouth.
        /// </summary>
        private Vector3 InTheSled(Friend friend, int p, SealedContainer box, float now)
        {
            if (box == null)
            {
                return friend.PartRest[p];
            }

            float rise = Ease.OutCubic(Mathf.Clamp01((now - friend.PartShownAt[p]) / _tuning.PartRiseSeconds));
            return Vector3.Lerp(box.ItemSocket.position, friend.PartRest[p], rise);
        }

        private void StepRepairInput(float deltaTime)
        {
            if (RepairCandidate == null || !_input.ExcavateHeld || !_beamWork.MayStart)
            {
                _holdTime = 0f;
                return;
            }

            _holdTime += deltaTime;
            if (_holdTime >= _tuning.RepairHold)
            {
                _holdTime = 0f;
                BeginRepair(RepairCandidate);
            }
        }

        private void BeginRepair(Friend friend)
        {
            friend.Progress.MarkDiscovered();
            friend.Progress.BeginRepair();
            friend.RepairStart = Time.time;
            SetHold(true);
            _events.Publish(new FriendRepairStarted(friend.Definition.Id));
        }

        private void StepRepair(Friend friend, float now, float deltaTime)
        {
            IFriendBody body = friend.Body;
            float t = now - friend.RepairStart;
            body.StepRepair(t, now, deltaTime);
            if (!body.Beaming(t))
            {
                SetHold(false);
            }

            StepLamps(friend, deltaTime);
            friend.Activity = body.Activity;
            friend.RotorSpeed = body.Motor;
            friend.RepairProgress = Mathf.Clamp01(t / body.RepairDuration);
            if (t < body.RepairDuration)
            {
                return;
            }

            friend.Progress.FinishRepair();
            friend.RepairProgress = 1f;
            body.Wake(now);
            Gift(friend);
            _events.Publish(new FriendRepaired(friend.Definition.Id));
            _save.SaveNow();
        }

        /// <summary>An awake friend's gift: the radio dial appears once Bell is awake.</summary>
        private void Gift(Friend friend)
        {
            if (friend.Definition.AbilityId == FriendDefinition.RadioDialAbility)
            {
                _radio.UnlockDial();
            }
        }

        private void StepAwake(Friend friend, float now, float deltaTime)
        {
            FriendBeat beat = friend.Body.StepAwake(now, deltaTime);
            StepLamps(friend, deltaTime);
            friend.Activity = friend.Body.Activity;
            friend.RotorSpeed = friend.Body.Motor;
            if (beat.Greeted)
            {
                _events.Publish(new FriendGreeted(friend.Definition.Id));
                if (friend.Progress.Welcome())
                {
                    if (friend.Definition.AnnouncesHomecoming)
                    {
                        _events.Publish(new TickerLine(friend.HomecomingLine));
                    }

                    _save.SaveNow();
                }
            }

            if (beat.Spotted)
            {
                MarkSpotted(beat.Spot);
                _events.Publish(new FriendSpotted(friend.Definition.Id, beat.Spot.Position));
            }
        }

        /// <summary>
        /// Parked in front of Bell at home with her dial: a press of Interact sends 07's beam to tap her tuning knob,
        /// and a beat later she turns it one detent, the knob turning with the needle (07 has no hands, VISION ruling
        /// 14).
        /// </summary>
        private void StepTune(float now, float deltaTime)
        {
            bool held = _input.ExcavateHeld;
            bool pressed = held && !_interactHeld;
            _interactHeld = held;
            CanTune = _cabinet != null && _radio.DialUnlocked && _cabinet.IsHome && RepairCandidate == null &&
                      _rover.Speed <= _bellTuning.TuneMaxSpeed &&
                      SurfaceRules.HorizontalDistance(_rover.Position, _cabinet.DialFront) <= _bellTuning.TuneRadius;
            if (CanTune && pressed && !_tapping)
            {
                _tapping = true;
                _tapStart = now;
                _cabinet.KnobTapped();
            }

            if (_tapping && now - _tapStart >= _bellTuning.DialTapTime)
            {
                _tapping = false;
                if (_cabinet.IsHome && _radio.TurnDial())
                {
                    _cabinet.DialTurned(now);
                }
            }

            _dialBeam.Step(_tapping, _cabinet != null, _rig.TetherOrigin.position,
                _cabinet != null ? _cabinet.Knob : Vector3.zero, now, deltaTime);
        }

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            _cabinet.NoticeNewRelic(Time.time);
        }

        private void StepBeam(Friend repairing, float now, float deltaTime)
        {
            bool beaming = repairing != null && repairing.Body.Beaming(now - repairing.RepairStart);
            if (beaming)
            {
                _beamWork.Work(deltaTime);
            }

            _beam.Step(beaming, repairing != null, _rig.TetherOrigin.position,
                repairing != null ? repairing.Body.BeamTarget : Vector3.zero, now, deltaTime);
        }

        private void StepGaze(Friend repairing)
        {
            if (repairing != null)
            {
                _rig.SetGazeTarget(this, repairing.Position, GazePriorities.Focus);
                _gazing = true;
            }
            else if (RepairCandidate != null)
            {
                _rig.SetGazeTarget(this, RepairCandidate.Site.Position, GazePriorities.Interest);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void SetHold(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            SetHold(false);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDestroy()
        {
            _relicSubscription?.Dispose();
            _glints?.Dispose();
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayer(root.GetChild(i), layer);
            }
        }
    }
}
