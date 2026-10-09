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
    /// Driving through a part draws it in (softly, amber) and lights a lamp on the friend
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
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RadioProgram _radio;
        private IHeldItems _items;
        private RelicField _relics;
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
            RelicField relics, SalvageField salvage, HomeBase home)
        {
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

            _glints = new PickupGlints(transform, services.Visuals.PartGlint, services.Glints, Mathf.Max(1, partCount),
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
                    friend.PartFlying[part] = false;
                    friend.Parts[part].gameObject.SetActive(!friend.Progress.IsCollected(part));
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
            for (int p = 0; p < parts.Length; p++)
            {
                Vector3 spot = site.Parts[p] + Vector3.up * _tuning.PartHover;
                GameObject piece = Instantiate(definition.Parts[p].Prefab, spot, Quaternion.identity, root);
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
            if (definition.AbilityId == FriendDefinition.RadioDialAbility)
            {
                DialFriend = friend;
            }

            return friend;
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
                _relics.Sites, definition.Parts.Count);
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
            float magnetSq = _tuning.PartMagnetRadius * _tuning.PartMagnetRadius;
            float bobOmega = 2f * Mathf.PI * _tuning.PartBobFrequency;
            for (int p = 0; p < friend.Parts.Length; p++)
            {
                if (friend.Progress.IsCollected(p))
                {
                    continue;
                }

                Transform piece = friend.Parts[p];
                if (!friend.PartFlying[p])
                {
                    Vector3 rest = friend.PartRest[p] + Vector3.up * (Mathf.Sin(now * bobOmega + p) * _tuning.PartBob);
                    piece.SetPositionAndRotation(rest,
                        Quaternion.Euler(0f, now * _tuning.PartSpin + p * PartHeadingStep, 0f));
                    _glints.Add(friend.PartRest[p], p);
                    if ((rest - rover).sqrMagnitude <= magnetSq)
                    {
                        friend.PartFlying[p] = true;
                        friend.PartStart[p] = rest;
                        friend.PartFlightTime[p] = 0f;
                        friend.PartFlightDuration[p] = PickupFlight.Duration(Vector3.Distance(rest, socket),
                            _tuning.PartFlightDuration, _tuning.PartFlightPerMetre);
                    }

                    continue;
                }

                friend.PartFlightTime[p] += deltaTime;
                float progress = friend.PartFlightTime[p] / friend.PartFlightDuration[p];
                if (progress >= 1f)
                {
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

        private void StepRepairInput(float deltaTime)
        {
            if (RepairCandidate == null || !_input.ExcavateHeld)
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
