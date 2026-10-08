using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The station's relay network (docs/features/M3-06). A dark, leaning mast stands on each of the World's
    /// <c>relay.&lt;n&gt;</c> anchors with its one relay part glinting amber nearby (<see cref="RelayPartPlanner"/>);
    /// driving through the part draws it in like a friend's part. With the part held, holding Interact at the mast's
    /// foot pays the next escalating scrap cost and plays the restoration (<see cref="RelayBeat"/>): 07's beam
    /// stitches while the part glides into the junction box, the mast straightens with a creak and its lamp warms. A
    /// mast that links home (<see cref="StationReach"/>) comes online: a pulse of light runs along the ground toward
    /// the node it links to, and <see cref="RelayRestored"/> and the radio's ticker line follow. One beyond the lit
    /// frontier keeps a low listening glow until a neighbour or a stronger tower reaches it, then lights in turn (a
    /// whole chain one after another). Home's circle is the radio tower's clear-signal radius. This is Core's
    /// <see cref="IStationReach"/> (through <see cref="Reach"/>) and the UI's <see cref="IRelayStatus"/>.
    /// Everything paid, gathered and restored is saved at once and never lost. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelayField : MonoBehaviour, IRelayStatus
    {
        private const string TickerOnline = "ticker.relay.online";
        private const string TickerWaiting = "ticker.relay.waiting";

        /// <summary>A part spins this many times faster in flight than at rest.</summary>
        private const float PartFlightSpin = 4f;

        /// <summary>Degrees between the idle headings of the masts' parts (so they never turn in step).</summary>
        private const float PartHeadingStep = 137f;

        [Tooltip("Relay tuning (Assets/_Project/Data/Tuning/Gameplay/RelayTuning.asset).")]
        [SerializeField] private RelayTuning _tuning;

        [Tooltip("Art's restored mast (Generated/Art/Relay/RelayMast.prefab).")]
        [SerializeField] private GameObject _mastPrefab;

        [Tooltip("Art's broken mast (Generated/Art/Relay/RelayMast_Broken.prefab).")]
        [SerializeField] private GameObject _brokenPrefab;

        [Tooltip("Art's relay part pickup (Generated/Art/Relay/Part_RelayModule.prefab).")]
        [SerializeField] private GameObject _partPrefab;

        private readonly List<RelayMast> _masts = new List<RelayMast>();
        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private ScrapWallet _wallet;
        private UpgradeService _upgrades;
        private FriendTuning _friends;
        private StationReach _reach;
        private RepairBeam _beam;
        private ScrapGlints _glints;
        private RelayBeat _beat;
        private string[] _counts = Array.Empty<string>();
        private RelayMast _restoring;
        private RelayMast _candidate;
        private float _holdTime;
        private bool _holding;
        private bool _gazing;
        private bool _quiet = true;
        private bool _initialized;

        public RelayTuning Tuning => _tuning;

        /// <summary>The station's reach (Core's <see cref="IStationReach"/>).</summary>
        public StationReach Reach => _reach;

        public int MastCount => _masts.Count;

        public int LitMasts => _reach != null ? _reach.LitMasts : 0;

        public int NextCost => _tuning.CostAfter(PaidCount);

        public float RestoreHold => _tuning != null && _tuning.RestoreHold > 0f
            ? Mathf.Clamp01(_holdTime / _tuning.RestoreHold)
            : 0f;

        /// <summary>07 holds a mast's part beside it: where the restore prompt stands (the part socket).</summary>
        public bool TryGetRestore(out Vector3 position, out bool affordable)
        {
            position = _candidate != null ? _candidate.Broken.PartSocket.position : Vector3.zero;
            affordable = _candidate != null && _wallet.CanAfford(NextCost);
            return _candidate != null;
        }

        internal IReadOnlyList<RelayMast> Masts => _masts;

        private int PaidCount
        {
            get
            {
                int paid = 0;
                for (int i = 0; i < _masts.Count; i++)
                {
                    paid += _masts[i].IsRestored ? 1 : 0;
                }

                return paid;
            }
        }

        internal void Wire(RelayTuning tuning, GameObject mastPrefab, GameObject brokenPrefab, GameObject partPrefab)
        {
            _tuning = tuning;
            _mastPrefab = mastPrefab;
            _brokenPrefab = brokenPrefab;
            _partPrefab = partPrefab;
        }

        internal bool Initialize(GameplayServices services, UpgradeService upgrades, FriendTuning friends,
            ScrapTuning scrap)
        {
            List<WorldAnchor> anchors = services.Anchors != null ? RelayAnchors(services.Anchors) : null;
            string problem = _tuning == null ? "RelayTuning is not assigned."
                : _mastPrefab == null || _brokenPrefab == null || _partPrefab == null
                    ? "the relay mast, broken mast and relay part prefabs must all be assigned (relay mast contract)."
                : anchors == null || anchors.Count == 0
                    ? $"the world publishes no '{WorldAnchorIds.RelayPrefix}0' anchor (world anchors contract)."
                : _tuning.Validate(anchors.Count);
            if (problem != null)
            {
                Debug.LogError($"{nameof(RelayField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _wallet = services.Wallet;
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            if (scrap == null)
            {
                throw new ArgumentNullException(nameof(scrap));
            }

            _beat = RelayBeat.For(_tuning);
            Vector3 home = SurfaceRules.OnSurface(services.Terrain, services.Layout.BasePosition.x,
                services.Layout.BasePosition.z);
            var ids = new string[anchors.Count];
            var pads = new Vector3[anchors.Count];
            for (int i = 0; i < anchors.Count; i++)
            {
                ids[i] = anchors[i].Id;
                pads[i] = anchors[i].Position;
            }

            _reach = new StationReach(home, _upgrades.SignalRadius, ids, pads, _tuning.MastReach);
            for (int i = 0; i < anchors.Count; i++)
            {
                RelayMast mast = Spawn(anchors[i], i, services);
                if (mast == null)
                {
                    enabled = false;
                    return false;
                }

                _masts.Add(mast);
            }

            _glints = new ScrapGlints(transform, services.Visuals.PartGlint, scrap, _masts.Count, Layers.Pickup);
            _beam = new RepairBeam("RelayBeam", transform, services.Visuals.TetherBeam, _friends.StitchRate,
                _friends.StitchSpread);
            _counts = new string[_masts.Count + 1];
            for (int i = 0; i < _counts.Length; i++)
            {
                _counts[i] = i.ToString(CultureInfo.InvariantCulture);
            }

            _initialized = true;
            return true;
        }

        internal RelaysSaveData Capture()
        {
            var data = new RelaysSaveData { masts = new RelaySaveData[_masts.Count] };
            for (int i = 0; i < _masts.Count; i++)
            {
                RelayMast mast = _masts[i];
                data.masts[i] = new RelaySaveData
                {
                    id = mast.Id,
                    part = mast.PartState >= RelayPartState.Held,
                    paid = mast.Paid,
                    restored = mast.IsRestored,
                };
            }

            return data;
        }

        /// <summary>After a load: parts held, masts restored and lamps lit exactly as saved, without a beat.</summary>
        internal void Restore(RelaysSaveData data)
        {
            if (data?.masts == null)
            {
                return;
            }

            foreach (RelaySaveData saved in data.masts)
            {
                RelayMast mast = Find(saved.id);
                if (mast == null)
                {
                    Debug.LogWarning($"{nameof(RelayField)}: the save knows a relay '{saved.id}' the world does not.",
                        this);
                    continue;
                }

                mast.Paid = Mathf.Max(0, saved.paid);
                mast.IsRestored = saved.restored || saved.paid > 0;
                if (mast.IsRestored)
                {
                    SettleRestored(mast);
                }
                else if (saved.part)
                {
                    mast.PartState = RelayPartState.Held;
                    mast.Part.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _masts.Count; i++)
            {
                _reach.SetRestored(_masts[i].Node, _masts[i].IsRestored);
            }

            _quiet = true;
            ShowReach(Time.time);
        }

        private static List<WorldAnchor> RelayAnchors(IWorldAnchors anchors)
        {
            var found = new List<WorldAnchor>();
            while (anchors.TryGet(WorldAnchorIds.RelayPrefix + found.Count, out WorldAnchor anchor))
            {
                found.Add(anchor);
            }

            return found;
        }

        private RelayMast Spawn(WorldAnchor anchor, int index, GameplayServices services)
        {
            if (!RelayPartPlanner.TryPlan(services.Terrain, anchor, _tuning, _friends, index, out Vector3 partRest,
                    out string problem))
            {
                Debug.LogError($"{nameof(RelayField)}: '{anchor.Id}' has nowhere for its relay part: {problem} " +
                               "(world anchors contract, RelayTuning part range).", this);
                return null;
            }

            var root = new GameObject("Relay_" + anchor.Id).transform;
            root.SetParent(transform, false);
            Quaternion rotation = Quaternion.LookRotation(anchor.Forward);
            var broken = new RelayRig(Instantiate(_brokenPrefab, anchor.Position, rotation, root));
            var restored = new RelayRig(Instantiate(_mastPrefab, anchor.Position, rotation, root));
            broken.MakeSolid();
            restored.MakeSolid();
            restored.Visible = false;
            broken.SetLamp(0f);
            GameObject part = Instantiate(_partPrefab, partRest + Vector3.up * _friends.PartHover, Quaternion.identity,
                root);
            SetLayer(part.transform, Layers.Pickup);
            var pulse = new LinkPulse("LinkPulse", root, services.Visuals.LinkPulse, services.Terrain, _tuning);
            return new RelayMast(index + 1, anchor, broken, restored, part.transform, partRest, pulse);
        }

        private RelayMast Find(string id)
        {
            for (int i = 0; i < _masts.Count; i++)
            {
                if (string.Equals(_masts[i].Id, id, StringComparison.Ordinal))
                {
                    return _masts[i];
                }
            }

            return null;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            if (!Mathf.Approximately(_upgrades.SignalRadius, _reach.HomeRadius))
            {
                _reach.SetHomeRadius(_upgrades.SignalRadius);
                ShowReach(now);
            }

            _quiet = false;
            _candidate = null;
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = 0; i < _masts.Count; i++)
            {
                RelayMast mast = _masts[i];
                StepPart(mast, i, now, deltaTime);
                if (mast.Restoring)
                {
                    StepRestore(mast, now);
                }
                else if (now >= mast.OnlineAt)
                {
                    mast.OnlineAt = float.PositiveInfinity;
                    WarmLamp(mast, now, _tuning.LampGlow);
                    GoOnline(mast, now);
                }

                mast.Shown.SetLamp(LampAt(mast, now));
                mast.Pulse.Step(now);
                if (!mast.IsRestored && mast.PartState == RelayPartState.Held && _restoring == null &&
                    SurfaceRules.HorizontalDistance(_rover.Position, mast.Broken.PartSocket.position) <=
                    _tuning.RestoreRadius)
                {
                    _candidate = mast;
                }
            }

            _glints.End();
            StepRestoreInput(now, deltaTime);
            bool beaming = _restoring != null && _beat.Beaming(now - _restoring.RestoreStart);
            _beam.Step(beaming, _restoring != null, _rig.TetherOrigin.position,
                _restoring != null ? _restoring.Shown.BeamPoint.position : Vector3.zero, now, deltaTime);
            SetHold(beaming);
            StepGaze();
        }

        /// <summary>The part bobs and glints at rest; driven through, it is drawn into 07 and held.</summary>
        private void StepPart(RelayMast mast, int index, float now, float deltaTime)
        {
            Transform part = mast.Part;
            Vector3 socket = _rig.CargoSocket.position;
            switch (mast.PartState)
            {
                case RelayPartState.Resting:
                    float bob = Mathf.Sin(now * 2f * Mathf.PI * _friends.PartBobFrequency + index) * _friends.PartBob;
                    Vector3 rest = mast.PartRest + Vector3.up * (_friends.PartHover + bob);
                    part.SetPositionAndRotation(rest,
                        Quaternion.Euler(0f, now * _friends.PartSpin + index * PartHeadingStep, 0f));
                    _glints.Add(mast.PartRest, index);
                    if ((rest - _rover.Position).sqrMagnitude <=
                        _friends.PartMagnetRadius * _friends.PartMagnetRadius)
                    {
                        mast.PartState = RelayPartState.Flying;
                        mast.FlightStart = rest;
                        mast.FlightTime = 0f;
                        mast.FlightDuration = ScrapFlight.Duration(Vector3.Distance(rest, socket),
                            _friends.PartFlightDuration, _friends.PartFlightPerMetre);
                    }

                    break;
                case RelayPartState.Flying:
                    mast.FlightTime += deltaTime;
                    float progress = mast.FlightTime / mast.FlightDuration;
                    if (progress >= 1f)
                    {
                        mast.PartState = RelayPartState.Held;
                        part.gameObject.SetActive(false);
                        part.localScale = mast.PartScale;
                        _events.Publish(new RelayCued(RelayCue.PartCollected, mast.Id, socket));
                        _save.SaveNow();
                        break;
                    }

                    part.SetPositionAndRotation(
                        ScrapFlight.Evaluate(mast.FlightStart, socket, progress, index, 1f, _friends.PartFlightLift,
                            _friends.PartSpiralRadius, _friends.PartSpiralTurns),
                        Quaternion.Euler(0f, now * _friends.PartSpin * PartFlightSpin, 0f));
                    part.localScale = mast.PartScale *
                                      Mathf.Lerp(1f, _friends.PartArrivalScale, Ease.InOutSine(progress));
                    break;
            }
        }

        private void StepRestoreInput(float now, float deltaTime)
        {
            if (_candidate == null || !_input.ExcavateHeld || !_wallet.CanAfford(NextCost))
            {
                _holdTime = 0f;
                return;
            }

            _holdTime += deltaTime;
            if (_holdTime < _tuning.RestoreHold)
            {
                return;
            }

            _holdTime = 0f;
            BeginRestore(_candidate, now);
        }

        private void BeginRestore(RelayMast mast, float now)
        {
            int cost = NextCost;
            if (!_wallet.TrySpend(cost))
            {
                return;
            }

            mast.Paid = cost;
            mast.IsRestored = true;
            mast.Restoring = true;
            mast.RestoreStart = now;
            mast.PartState = RelayPartState.Installing;
            mast.Part.gameObject.SetActive(true);
            _restoring = mast;
            _candidate = null;
            _events.Publish(new RelayCued(RelayCue.Stitched, mast.Id, mast.Broken.BeamPoint.position));
            _save.SaveNow();
        }

        /// <summary>The beat: the part glides in, the mast straightens, joins the reach, its lamp warms.</summary>
        private void StepRestore(RelayMast mast, float now)
        {
            float t = now - mast.RestoreStart;
            StepInstall(mast, t);
            if (t < _beat.StitchEnd)
            {
                return;
            }

            if (!mast.Swapped)
            {
                mast.Swapped = true;
                mast.Restored.CapturePoseFrom(mast.Broken);
                mast.Restored.Visible = true;
                mast.Broken.Visible = false;
                _events.Publish(new RelayCued(RelayCue.Straightened, mast.Id, mast.Anchor.Position));
            }

            mast.Restored.Straighten(_beat.Upright(t));
            if (!mast.Joined && t >= _beat.StraightenEnd)
            {
                mast.Joined = true;
                _reach.SetRestored(mast.Node, true);
                bool lit = _reach.IsLit(mast.Node);
                mast.ShownLit = lit;
                WarmLamp(mast, now, lit ? _tuning.LampGlow : _tuning.WaitingGlow);
                ShowReach(mast.RestoreStart + _beat.Duration + _tuning.ChainDelay);
            }

            if (!_beat.Done(t))
            {
                return;
            }

            mast.Restoring = false;
            mast.Restored.Straighten(1f);
            _restoring = null;
            if (mast.ShownLit)
            {
                GoOnline(mast, now);
            }
            else
            {
                _events.Publish(new TickerLine(TickerWaiting));
            }

            _save.SaveNow();
        }

        /// <summary>The held part glides from 07 along the beam into the junction box, then clicks in.</summary>
        private void StepInstall(RelayMast mast, float t)
        {
            if (mast.PartState != RelayPartState.Installing)
            {
                return;
            }

            Transform socket = mast.Broken.PartSocket;
            if (t >= _beat.PartIn)
            {
                Install(mast);
                _events.Publish(new RelayCued(RelayCue.PartSlotted, mast.Id, socket.position));
                return;
            }

            float glide = _beat.Part(t);
            Vector3 from = _rig.CargoSocket.position;
            Vector3 position = Vector3.Lerp(from, socket.position, glide) +
                               Vector3.up * (_tuning.PartArc * Ease.Hump(glide));
            mast.Part.SetPositionAndRotation(position, Quaternion.Slerp(Quaternion.identity, socket.rotation, glide));
            mast.Part.localScale = mast.PartScale;
        }

        private void Install(RelayMast mast)
        {
            mast.PartState = RelayPartState.Installed;
            Transform socket = mast.Restored.PartSocket;
            mast.Part.gameObject.SetActive(true);
            mast.Part.SetPositionAndRotation(socket.position, socket.rotation);
            mast.Part.localScale = mast.PartScale;
        }

        /// <summary>A restored mast as a load finds it: upright, its part slotted in, its lamp already set.</summary>
        private void SettleRestored(RelayMast mast)
        {
            mast.Restoring = false;
            mast.Swapped = true;
            mast.Joined = true;
            mast.Restored.Visible = true;
            mast.Broken.Visible = false;
            mast.Restored.Straighten(1f);
            Install(mast);
            if (_restoring == mast)
            {
                _restoring = null;
            }
        }

        /// <summary>
        /// Follows the reach after a change: restored, joined masts newly linked come online in turn from
        /// <paramref name="chainStart"/>, nearest home first (silently and at once right after a load); one no longer
        /// linked dims to listening.
        /// </summary>
        private void ShowReach(float chainStart)
        {
            int nearest = int.MaxValue;
            for (int i = 0; i < _masts.Count; i++)
            {
                RelayMast mast = _masts[i];
                if (mast.Joined && !mast.Restoring && !mast.ShownLit && _reach.IsLit(mast.Node))
                {
                    nearest = Mathf.Min(nearest, _reach.Depth(mast.Node));
                }
            }

            for (int i = 0; i < _masts.Count; i++)
            {
                RelayMast mast = _masts[i];
                if (!mast.Joined || mast.Restoring)
                {
                    continue;
                }

                bool lit = _reach.IsLit(mast.Node);
                if (_quiet)
                {
                    mast.ShownLit = lit;
                    mast.OnlineAt = float.PositiveInfinity;
                    SetLamp(mast, lit ? _tuning.LampGlow : _tuning.WaitingGlow);
                }
                else if (lit && !mast.ShownLit)
                {
                    mast.ShownLit = true;
                    mast.OnlineAt = chainStart + (_reach.Depth(mast.Node) - nearest) * _tuning.ChainDelay;
                }
                else if (!lit && mast.ShownLit)
                {
                    mast.ShownLit = false;
                    mast.OnlineAt = float.PositiveInfinity;
                    WarmLamp(mast, chainStart, _tuning.WaitingGlow);
                }
            }
        }

        /// <summary>The mast is online: its pulse leaves for the node it links to; the moment is announced.</summary>
        private void GoOnline(RelayMast mast, float now)
        {
            int link = _reach.NearestLink(mast.Node);
            if (link >= 0)
            {
                mast.Pulse.Begin(mast.Anchor.Position, _reach.Position(link), now);
            }

            int lit = _reach.LitMasts;
            _events.Publish(new RelayRestored(mast.Id, mast.Restored.Lamp.position, lit, _masts.Count));
            _events.Publish(new TickerLine(TickerOnline, _counts[Mathf.Clamp(lit, 0, _counts.Length - 1)]));
        }

        private void WarmLamp(RelayMast mast, float now, float glow)
        {
            mast.LampFrom = LampAt(mast, now);
            mast.LampTo = glow;
            mast.LampStart = now;
            _events.Publish(new RelayCued(RelayCue.LampWarmed, mast.Id, mast.Shown.Lamp.position));
        }

        private void SetLamp(RelayMast mast, float glow)
        {
            mast.LampFrom = glow;
            mast.LampTo = glow;
            mast.LampStart = float.NegativeInfinity;
        }

        private float LampAt(RelayMast mast, float now)
        {
            return Mathf.Lerp(mast.LampFrom, mast.LampTo, Ease.InOutSine((now - mast.LampStart) / _tuning.LampWarm));
        }

        private void StepGaze()
        {
            if (_restoring != null)
            {
                _rig.SetGazeTarget(this, _restoring.Shown.BeamPoint.position, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_candidate != null)
            {
                _rig.SetGazeTarget(this, _candidate.Broken.PartSocket.position, GazePriorities.Interest);
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
