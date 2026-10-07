using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The outpost's lost machines (docs/features/M3-02-friends-tilly.md). Each friend lies broken at a planned site
    /// with its missing parts scattered around, glinting amber. Driving through a part draws it in like scrap (softer)
    /// and lights a lamp on the friend (<see cref="FriendPartCollected"/>); an item its repair also needs (Bell's
    /// cassette) lights the next lamp as soon as 07 holds it (<see cref="IHeldItems"/>). With everything gathered,
    /// holding Interact near it starts a calm repair that always plays out: 07 holds still while its beam stitches the
    /// friend, which shivers, flickers awake, spins up its rotors, wobbles into the air and looks at 07
    /// (<see cref="FriendRepairStarted"/>, then <see cref="FriendRepaired"/>, then a save). Awake friends live at the
    /// base, come along on trips, greet 07 coming home (the first homecoming of a friend that announces it also puts a
    /// <see cref="TickerLine"/> on the radio) and use their gift (the spotter). Also the spotter's view of the world
    /// (<see cref="ISpotTargets"/>), Core's <see cref="IFriendRoster"/> (live state for audio, UI and rover) and the
    /// UI's <see cref="IFriendStatuses"/> (parts, items and repair readiness).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FriendField : MonoBehaviour, ISpotTargets, IFriendRoster, IFriendStatuses
    {
        /// <summary>Rotor effort while hovering in place (Core's IFriendState.RotorSpeed scale).</summary>
        private const float HoverEffort = 0.4f;

        /// <summary>Points along the stitching beam.</summary>
        private const int BeamPoints = 16;

        /// <summary>Scrap pieces this close (m) to a spotted one count as the same cluster.</summary>
        private const float ScrapClusterRadius = 4f;

        // Shiver frequencies (radians per second) on two axes: unrelated, so it reads as a shudder, not a wobble.
        private const float ShiverPitchRate = 37f;
        private const float ShiverRollRate = 29f;
        private const float WobbleRate = 9f;

        /// <summary>Seconds (time constant) for the stitching beam to brighten and fade.</summary>
        private const float BeamEase = 0.15f;

        /// <summary>Visibility below which a fading friend is hidden instead of scaled to a speck.</summary>
        private const float MinVisible = 0.01f;

        private const float BeamWidth = 0.06f;

        /// <summary>Upward bow (m) of the stitching beam.</summary>
        private const float BeamArc = 0.2f;

        /// <summary>Degrees between the idle headings of a friend's parts (so they never turn in step).</summary>
        private const float PartHeadingStep = 120f;

        /// <summary>A part spins this many times faster in flight than at rest.</summary>
        private const float PartFlightSpin = 4f;

        /// <summary>The roll of the lift-off wobble is this share of its pitch.</summary>
        private const float WobbleRoll = 0.5f;

        /// <summary>The stitch's up-and-down sweep is this share of its sideways sweep, at twice the rate.</summary>
        private const float StitchLift = 0.5f;

        [Tooltip("Every friend (Assets/_Project/Data/Content/FriendCatalog.asset).")]
        [SerializeField] private FriendCatalog _catalog;

        [Tooltip("Friend tuning (Assets/_Project/Data/Tuning/Gameplay/FriendTuning.asset).")]
        [SerializeField] private FriendTuning _tuning;

        [Tooltip("Each friend's home socket at the base (FriendSocket_<id>, BellCorner), in catalog order.")]
        [SerializeField] private Transform[] _perches = Array.Empty<Transform>();

        private readonly List<Friend> _friends = new List<Friend>();
        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private IHeldItems _items;
        private RelicField _relics;
        private ScrapField _scrap;
        private HomeBase _home;
        private SonarSystem _sonar;
        private ScrapGlints _glints;
        private LineRenderer _beam;
        private GlowRenderer _beamGlow;
        private Vector3[] _beamPoints = Array.Empty<Vector3>();
        private GlowRenderer[] _cones = Array.Empty<GlowRenderer>();
        private bool[] _spottedRelics = Array.Empty<bool>();
        private bool[] _spottedScrap = Array.Empty<bool>();
        private float _holdTime;
        private float _beamLevel;
        private bool _holding;
        private bool _gazing;
        private bool _initialized;

        public int Count => _friends.Count;

        /// <summary>The broken friend 07 could repair right now (every part gathered, within reach), or null.</summary>
        public Friend RepairCandidate { get; private set; }

        /// <summary>0..1 how far Interact has been held toward starting a repair.</summary>
        public float RepairHold => _tuning != null && _tuning.RepairHold > 0f ? _holdTime / _tuning.RepairHold : 0f;

        public FriendTuning Tuning => _tuning;

        internal IReadOnlyList<Friend> Friends => _friends;

        internal ScrapGlints Glints => _glints;

        internal void Wire(FriendCatalog catalog, FriendTuning tuning, Transform[] perches)
        {
            _catalog = catalog;
            _tuning = tuning;
            _perches = perches;
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

        internal bool Initialize(GameplayServices services, IHeldItems items, RelicField relics, ScrapField scrap,
            HomeBase home)
        {
            string problem = _catalog == null ? "FriendCatalog is not assigned."
                : _tuning == null ? "FriendTuning is not assigned."
                : _catalog.Validate() ?? PerchProblem();
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
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _scrap = scrap != null ? scrap : throw new ArgumentNullException(nameof(scrap));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _spottedRelics = new bool[relics.Relics.Count];
            _spottedScrap = new bool[scrap.Count];

            int partCount = 0;
            IReadOnlyList<FriendDefinition> definitions = _catalog.Friends;
            _cones = new GlowRenderer[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
            {
                FriendSite site = FriendSitePlanner.Plan(_terrain, services.Layout, definitions[i].Placement, _tuning,
                    relics.Sites, definitions[i].Parts.Count);
                if (!site.InCrater || !site.Visible)
                {
                    Debug.LogWarning($"{nameof(FriendField)}: '{definitions[i].Id}' lies at the best spot found " +
                                     $"(in a crater: {site.InCrater}, seen from the base: {site.Visible}).", this);
                }

                _friends.Add(Spawn(definitions[i], i, site, services));
                _cones[i] = new GlowRenderer(GlowObject.Create("SpotLight_" + definitions[i].Id, transform,
                    services.Meshes.Cone, services.Visuals.TractorBeam));
                partCount += definitions[i].Parts.Count;
            }

            _glints = new ScrapGlints(transform, services.Visuals.PartGlint, scrap.Tuning, Mathf.Max(1, partCount),
                Layers.Pickup);
            _beamPoints = new Vector3[BeamPoints];
            _beam = CreateBeam(services.Visuals.TetherBeam);
            _beamGlow = new GlowRenderer(_beam);
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

        /// <summary>Applies saved progress by id; awake friends come back on their perches.</summary>
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

                if (friend.Progress.State == FriendState.Awake)
                {
                    SettleAtHome(friend);
                }

                friend.Activity = friend.Progress.State == FriendState.Awake ? FriendActivity.Home
                    : FriendActivity.Dormant;
                friend.RepairProgress = friend.Progress.State == FriendState.Awake ? 1f : 0f;
            }
        }

        public bool TryFind(Vector3 around, float radius, out SpotTarget target)
        {
            float radiusSq = radius * radius;
            target = default;
            float best = radiusSq;
            bool found = false;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                bool hidden = (relic.State == RelicState.Buried || relic.State == RelicState.Surfacing) &&
                              !relic.Discovered && !_spottedRelics[i];
                float distance = SurfaceRules.HorizontalDistanceSquared(around, relic.Site.Position);
                if (hidden && distance <= best)
                {
                    best = distance;
                    target = new SpotTarget(SpotKind.Relic, i, -1, relic.Site.Position);
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

            if (found)
            {
                return true;
            }

            for (int i = 0; i < _scrap.Count; i++)
            {
                if (_spottedScrap[i] || !_scrap.IsResting(i))
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistanceSquared(around, _scrap.RestPosition(i));
                if (distance <= best)
                {
                    best = distance;
                    target = new SpotTarget(SpotKind.Scrap, i, -1, _scrap.RestPosition(i));
                    found = true;
                }
            }

            return found;
        }

        public void MarkSpotted(SpotTarget target)
        {
            switch (target.Kind)
            {
                case SpotKind.Relic:
                    _spottedRelics[target.Index] = true;
                    _sonar?.Reveal(target.Index, _tuning.SpotPillar);
                    break;
                case SpotKind.Part:
                    _friends[target.Index].PartSpotted[target.Part] = true;
                    break;
                default:
                    float clusterSq = ScrapClusterRadius * ScrapClusterRadius;
                    for (int i = 0; i < _scrap.Count; i++)
                    {
                        if (SurfaceRules.HorizontalDistanceSquared(_scrap.RestPosition(i), target.Position) <=
                            clusterSq)
                        {
                            _spottedScrap[i] = true;
                        }
                    }

                    break;
            }
        }

        private string PerchProblem()
        {
            IReadOnlyList<FriendDefinition> friends = _catalog.Friends;
            if (_perches.Length != friends.Count)
            {
                return $"{_perches.Length} perches wired for {friends.Count} friends.";
            }

            for (int i = 0; i < friends.Count; i++)
            {
                if (_perches[i] == null)
                {
                    return $"'{friends[i].Id}' has no perch ({friends[i].HomeSocket} on the lander).";
                }
            }

            return null;
        }

        private Friend Spawn(FriendDefinition definition, int index, FriendSite site, GameplayServices services)
        {
            var root = new GameObject("Friend_" + definition.Id).transform;
            root.SetParent(transform, false);
            Vector3 toHome = services.Layout.BasePosition - site.Position;
            float yaw = Mathf.Atan2(toHome.x, toHome.z) * Mathf.Rad2Deg;
            GameObject broken = Instantiate(definition.BrokenPrefab, site.Position, Quaternion.Euler(0f, yaw, 0f),
                root);
            GameObject repaired = Instantiate(definition.RepairedPrefab, site.Position, Quaternion.Euler(0f, yaw, 0f),
                root);
            int lamps = definition.Parts.Count + definition.Items.Count;
            var brokenRig = new FriendRig(broken, lamps);
            var repairedRig = new FriendRig(repaired, lamps) { Visible = false };

            var parts = new Transform[definition.Parts.Count];
            for (int p = 0; p < parts.Length; p++)
            {
                Vector3 spot = site.Parts[p] + Vector3.up * _tuning.PartHover;
                GameObject piece = Instantiate(definition.Parts[p].Prefab, spot, Quaternion.identity, root);
                SetLayer(piece.transform, Layers.Pickup);
                parts[p] = piece.transform;
            }

            var behaviour = new FriendBehaviour(_tuning, _terrain, this, definition.Placement.Seed + index);
            return new Friend(definition, index, site, brokenRig, repairedRig, parts, _perches[index], behaviour,
                RepairSequence.For(definition, _tuning));
        }

        private LineRenderer CreateBeam(Material material)
        {
            var host = new GameObject("RepairBeam");
            host.transform.SetParent(transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = BeamPoints;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.widthMultiplier = BeamWidth;
            line.shadowCastingMode = ShadowCastingMode.Off;
            GlowObject.Configure(line, material);
            return line;
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

                StepCone(friend, deltaTime);
            }

            _glints.End();
            StepRepairInput(deltaTime);
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
                        friend.PartFlightDuration[p] = ScrapFlight.Duration(Vector3.Distance(rest, socket),
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

                Vector3 position = ScrapFlight.Evaluate(friend.PartStart[p], socket, progress, p, 1f,
                    _tuning.PartFlightLift, _tuning.PartSpiralRadius, _tuning.PartSpiralTurns);
                piece.SetPositionAndRotation(position,
                    Quaternion.Euler(0f, now * _tuning.PartSpin * PartFlightSpin, 0f));
                piece.localScale = friend.PartScale[p] *
                                   Mathf.Lerp(1f, _tuning.PartArrivalScale, Ease.InOutSine(progress));
            }

            CollectHeldItems(friend);
            StepLamps(friend.Broken, friend, deltaTime);
            friend.Activity = FriendActivity.Dormant;
            friend.RotorSpeed = 0f;
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
        private void StepLamps(FriendRig rig, Friend friend, float deltaTime)
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
                rig.SetLamp(lamp, friend.LampLevels[lamp]);
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
            friend.Swapped = false;
            SetHold(true);
            _events.Publish(new FriendRepairStarted(friend.Definition.Id));
        }

        private void StepRepair(Friend friend, float now, float deltaTime)
        {
            RepairSequence sequence = friend.Sequence;
            float t = now - friend.RepairStart;
            friend.Activity = FriendActivity.Repairing;
            friend.RepairProgress = Mathf.Clamp01(t / sequence.Duration);
            friend.RotorSpeed = HoverEffort * sequence.Rotors(t);
            if (sequence.Stitching(t))
            {
                float shiver = _tuning.Shiver * Ease.InOutSine(sequence.StitchProgress(t));
                friend.Broken.Root.rotation = friend.BrokenRotation * Quaternion.Euler(
                    Mathf.Sin(now * ShiverPitchRate) * shiver, 0f, Mathf.Sin(now * ShiverRollRate) * shiver);
                StepLamps(friend.Broken, friend, deltaTime);
                return;
            }

            if (!friend.Swapped)
            {
                Swap(friend);
                SetHold(false);
            }

            float lift = sequence.Lift(t);
            Vector3 toRover = _rover.Position - friend.Site.Position;
            float roverYaw = Mathf.Atan2(toRover.x, toRover.z) * Mathf.Rad2Deg;
            float yaw = Mathf.LerpAngle(friend.BrokenRotation.eulerAngles.y, roverYaw, sequence.Look(t));
            float wobble = _tuning.LiftWobble * Ease.Hump(lift) * Mathf.Sin(now * WobbleRate);
            Vector3 position = friend.Site.Position + Vector3.up * (_tuning.HoverHeight * lift);
            friend.Repaired.Root.SetPositionAndRotation(position,
                Quaternion.Euler(wobble, yaw, wobble * WobbleRoll));
            friend.Repaired.BlendPose(lift);
            friend.Repaired.SetEye(sequence.Eye(t));
            friend.Repaired.SpinRotors(_tuning.RotorSpeed * sequence.Rotors(t) * deltaTime);
            StepLamps(friend.Repaired, friend, deltaTime);
            if (!sequence.Done(t))
            {
                return;
            }

            friend.Progress.FinishRepair();
            friend.RepairProgress = 1f;
            friend.Repaired.BlendPose(1f);
            friend.Motion.Teleport(position, yaw);
            friend.RotorLevel = 1f;
            friend.EyeLevel = 1f;
            friend.Behaviour.Wake(Senses(friend, now));
            _events.Publish(new FriendRepaired(friend.Definition.Id));
            _save.SaveNow();
        }

        private void Swap(Friend friend)
        {
            friend.Swapped = true;
            friend.Repaired.Root.SetPositionAndRotation(friend.Site.Position, friend.BrokenRotation);
            friend.Repaired.CapturePoseFrom(friend.Broken);
            friend.Repaired.BlendPose(0f);
            friend.Repaired.Visible = true;
            friend.Broken.Visible = false;
        }

        private void SettleAtHome(Friend friend)
        {
            friend.Swapped = true;
            friend.Broken.Visible = false;
            friend.Repaired.Visible = true;
            friend.Repaired.BlendPose(1f);
            Vector3 perch = friend.Perch.position;
            friend.Motion.Teleport(perch, friend.Perch.eulerAngles.y);
            friend.Repaired.Root.SetPositionAndRotation(perch, friend.Motion.Rotation);
            friend.RotorLevel = 0f;
            friend.EyeLevel = 1f;
            friend.Behaviour.Settle(Senses(friend, Time.time));
        }

        private void StepAwake(Friend friend, float now, float deltaTime)
        {
            FriendIntent intent = friend.Behaviour.Step(Senses(friend, now), deltaTime);
            if (intent.Teleport)
            {
                friend.Motion.Teleport(intent.Target, friend.Motion.Yaw);
            }

            Vector3 target = intent.Target;
            float floor = intent.Rotors > 0f ? _terrain.SampleHeight(target.x, target.z) + _tuning.MinClearance
                : float.MinValue;
            friend.Motion.Step(target, intent.Look, intent.Speed, floor, _tuning, deltaTime);
            friend.RotorLevel = Damp.Toward(friend.RotorLevel, intent.Rotors, _tuning.RotorEase, deltaTime);
            friend.EyeLevel = Damp.Toward(friend.EyeLevel, intent.Eye, _tuning.RotorEase, deltaTime);
            friend.ConeLevel = Damp.Toward(friend.ConeLevel, intent.Cone, _tuning.RotorEase, deltaTime);
            float bob = Mathf.Sin(now * 2f * Mathf.PI * _tuning.BobFrequency + friend.Index) * _tuning.Bob *
                        friend.RotorLevel;
            Transform root = friend.Repaired.Root;
            root.SetPositionAndRotation(friend.Motion.Position + Vector3.up * bob, friend.Motion.Rotation);
            bool visible = intent.Visibility > MinVisible;
            friend.Repaired.Visible = visible;
            root.localScale = Vector3.one * Mathf.Max(MinVisible, intent.Visibility);
            friend.Repaired.SpinRotors(_tuning.RotorSpeed * friend.RotorLevel * deltaTime);
            friend.Repaired.SetEye(friend.EyeLevel);
            StepLamps(friend.Repaired, friend, deltaTime);

            friend.Activity = Activity(friend.Behaviour, friend.RotorLevel);
            float dash = Mathf.Clamp01(friend.Motion.Velocity.magnitude / Mathf.Max(0.01f, _tuning.CatchUpSpeed));
            friend.RotorSpeed = friend.RotorLevel * Mathf.Lerp(HoverEffort, 1f, dash);
            if (intent.Greeted)
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

            if (intent.Spotted)
            {
                MarkSpotted(intent.Spot);
                _events.Publish(new FriendSpotted(friend.Definition.Id, intent.Spot.Position));
            }
        }

        private void StepCone(Friend friend, float deltaTime)
        {
            GlowRenderer cone = _cones[friend.Index];
            if (friend.Progress.State != FriendState.Awake)
            {
                friend.ConeLevel = Damp.Toward(friend.ConeLevel, 0f, _tuning.RotorEase, deltaTime);
            }

            cone.Apply(friend.ConeLevel * _tuning.SpotConeGlow);
            if (!cone.Renderer.enabled)
            {
                return;
            }

            Vector3 from = friend.Repaired.Root.position;
            float ground = _terrain.SampleHeight(from.x, from.z);
            float length = Mathf.Max(0.1f, from.y - ground);
            Transform host = cone.Renderer.transform;
            host.SetPositionAndRotation(from, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            host.localScale = new Vector3(_tuning.SpotConeRadius, _tuning.SpotConeRadius, length);
        }

        private void StepBeam(Friend repairing, float now, float deltaTime)
        {
            bool stitching = repairing != null && repairing.Sequence.Stitching(now - repairing.RepairStart);
            _beamLevel = Damp.Toward(_beamLevel, stitching ? 1f : 0f, BeamEase, deltaTime);
            _beamGlow.Apply(_beamLevel);
            if (!_beam.enabled || repairing == null)
            {
                return;
            }

            Vector3 start = _rig.TetherOrigin.position;
            Transform body = repairing.Broken.Visible ? repairing.Broken.TetherPoint : repairing.Repaired.TetherPoint;
            float phase = now * _tuning.StitchRate * 2f * Mathf.PI;
            Vector3 across = Vector3.Cross(Vector3.up, body.position - start).normalized;
            Vector3 end = body.position + across * (Mathf.Sin(phase) * _tuning.StitchSpread) +
                          Vector3.up * (Mathf.Sin(phase * 2f) * _tuning.StitchSpread * StitchLift);
            for (int i = 0; i < BeamPoints; i++)
            {
                float t = (float)i / (BeamPoints - 1);
                _beamPoints[i] = Vector3.Lerp(start, end, t) + Vector3.up * (Ease.Hump(t) * BeamArc);
            }

            _beam.SetPositions(_beamPoints);
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

        private static FriendActivity Activity(FriendBehaviour behaviour, float rotorLevel)
        {
            switch (behaviour.Current)
            {
                case FriendBehaviour.Mode.Following:
                case FriendBehaviour.Mode.Greeting:
                    return FriendActivity.Following;
                case FriendBehaviour.Mode.Spotting:
                    return FriendActivity.Spotting;
                case FriendBehaviour.Mode.Perched:
                    return rotorLevel < GlowRenderer.VisibleThreshold ? FriendActivity.Napping : FriendActivity.Home;
                default:
                    return FriendActivity.Home;
            }
        }

        private FriendSenses Senses(Friend friend, float now)
        {
            return new FriendSenses
            {
                Now = now,
                Position = friend.Motion.Position,
                Rover = _rover.Position,
                RoverForward = _rover.Rotation * Vector3.forward,
                Camera = _view.Camera.transform.position,
                Home = _home.LanderPosition,
                Perch = friend.Perch.position,
                PerchForward = friend.Perch.forward,
                Shelf = _home.ShelfPosition,
                ShelfForward = _home.ShelfForward,
            };
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
