using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Ro's cassette tapes out in the world (docs/features/M3-05 "Cassettes"). A tape at an anchor rests sealed in
    /// Art's container there (0.5.5 §2: the ledge tape in the fallen SupplyPod, the terminus tape in Ro's TapeCase
    /// beside Bell): once its seal is cut and the lid stands open (<see cref="SealedContainers"/>) the tape slides out
    /// onto the ground at the container's front. The basin tape waits half-buried in the dust, leaning back against a
    /// crater rim. A tape in view leans with its label toward open ground and a warm glint that reads from afar.
    /// Driving within reach pops it out: it straightens up, spins a little and drifts into 07, flashes warm, joins the
    /// <see cref="RadioProgram"/> and publishes <see cref="CassetteCollected"/>, then the game saves. A collected tape
    /// never comes back. Anchored tapes need the World's anchors; the basin tape's spot comes from the
    /// <see cref="CassetteSitePlanner"/>, clear of relics and friends. The frame loop allocates nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CassetteField : MonoBehaviour
    {
        /// <summary>Tapes arrive one at a time (they are far apart), so one flash is always free.</summary>
        private const int FlashPoolSize = 1;

        [Tooltip("The cassettes in the world (Assets/_Project/Data/Content/CassetteCatalog.asset).")]
        [SerializeField] private CassetteCatalog _catalog;

        [Tooltip("Cassette tuning (Assets/_Project/Data/Tuning/Gameplay/CassetteTuning.asset).")]
        [SerializeField] private CassetteTuning _tuning;

        [Tooltip("Art's fallen SupplyPod, stood under the Gameplay root by the scene build so World can light its " +
                 "Glow beacon; set on its tape's anchor at boot.")]
        [SerializeField] private Transform _supplyPod;

        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private RadioProgram _radio;
        private PickupGlints _glints;
        private GlowFlashPool _flashes;
        private Transform[] _pieces = Array.Empty<Transform>();
        private CassetteSite[] _sites = Array.Empty<CassetteSite>();
        private Vector3[] _rest = Array.Empty<Vector3>();
        private Quaternion[] _restRotation = Array.Empty<Quaternion>();
        private Vector3[] _scale = Array.Empty<Vector3>();
        private Vector3[] _flightStart = Array.Empty<Vector3>();
        private float[] _flightTime = Array.Empty<float>();
        private float[] _flightDuration = Array.Empty<float>();
        private PieceState[] _state = Array.Empty<PieceState>();
        private SealedContainer[] _containers = Array.Empty<SealedContainer>();
        private float[] _slideStart = Array.Empty<float>();
        private SealedContainers _sealed;
        private bool _glancing;
        private bool _initialized;

        private enum PieceState : byte
        {
            Waiting = 0,
            Flying = 1,
            Collected = 2,
            Sealed = 3,
            Sliding = 4,
        }

        /// <summary>Cassettes in the world (collected or not).</summary>
        public int Count => _pieces.Length;

        public CassetteCatalog Catalog => _catalog;

        public CassetteTuning Tuning => _tuning;

        internal PickupGlints Glints => _glints;

        public CassetteDefinition Definition(int index)
        {
            return _catalog.Cassettes[index];
        }

        /// <summary>Where cassette <paramref name="index"/> waits.</summary>
        public CassetteSite Site(int index)
        {
            return _sites[index];
        }

        /// <summary>In view and waiting for 07 (not sealed in, not yet popped out of the dust).</summary>
        public bool IsWaiting(int index)
        {
            return _state[index] == PieceState.Waiting;
        }

        /// <summary>The sealed container tape <paramref name="index"/> rests in, or null.</summary>
        public SealedContainer Container(int index)
        {
            return _containers[index];
        }

        internal void Wire(CassetteCatalog catalog, CassetteTuning tuning, Transform supplyPod)
        {
            _catalog = catalog;
            _tuning = tuning;
            _supplyPod = supplyPod;
        }

        /// <summary>The save id of the container cassette <paramref name="id"/> rests in.</summary>
        public static string ContainerId(string id)
        {
            return "container.cassette." + id;
        }

        /// <summary>
        /// Finds every cassette's spot (anchors, or the basin planner clear of <paramref name="keepClear"/>) and
        /// spawns the pickups. False (logged) when wiring or an anchor is missing.
        /// </summary>
        internal bool Initialize(GameplayServices services, RadioProgram radio, IReadOnlyList<Vector3> keepClear,
            SealedContainers containers)
        {
            _sealed = containers ?? throw new ArgumentNullException(nameof(containers));
            string problem = _catalog == null ? "CassetteCatalog is not assigned."
                : _tuning == null ? "CassetteTuning is not assigned."
                : _supplyPod == null
                    ? "Art's SupplyPod is not wired (the scene build stands it under the Gameplay root)."
                : _catalog.Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(CassetteField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            if (!PlanSites(services, keepClear ?? throw new ArgumentNullException(nameof(keepClear))))
            {
                enabled = false;
                return false;
            }

            problem = Spawn(services.Visuals);
            if (problem != null)
            {
                Debug.LogError($"{nameof(CassetteField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _glints = new PickupGlints(transform, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                Mathf.Max(1, Count),
                Layers.Pickup);
            _flashes = new GlowFlashPool(transform, services.Meshes.Sphere, services.Visuals.WarmGlow,
                FlashPoolSize, _tuning.FlashDuration, _tuning.FlashRadius, _tuning.FlashIntensity);
            _initialized = true;
            return true;
        }

        /// <summary>
        /// After a load: tapes the radio program owns are already collected, and the containers they came from stand
        /// open (an older save's tapes, from before containers, stay taken).
        /// </summary>
        internal void SyncCollected()
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_state[i] != PieceState.Collected && _radio.Owns(Definition(i).Id))
                {
                    _state[i] = PieceState.Collected;
                    _pieces[i].gameObject.SetActive(false);
                    if (_containers[i] != null)
                    {
                        _sealed.StandOpen(_containers[i].Id);
                    }
                }
            }
        }

        private bool PlanSites(GameplayServices services, IReadOnlyList<Vector3> keepClear)
        {
            IReadOnlyList<CassetteDefinition> cassettes = _catalog.Cassettes;
            _sites = new CassetteSite[cassettes.Count];
            var clear = new List<Vector3>(keepClear);
            for (int i = 0; i < cassettes.Count; i++)
            {
                CassetteDefinition cassette = cassettes[i];
                if (cassette.Site == CassetteSiteRule.Anchor)
                {
                    if (!cassette.Anchor.TryResolve(services.Anchors, services.Terrain, out Vector3 position,
                            out Vector3 facing))
                    {
                        Debug.LogError($"{nameof(CassetteField)}: cassette '{cassette.Id}' waits at world anchor " +
                                       $"'{cassette.Anchor.AnchorId}', which the World does not publish " +
                                       "(world anchors contract).", this);
                        return false;
                    }

                    _sites[i] = new CassetteSite(position, facing, true);
                }
                else
                {
                    _sites[i] = CassetteSitePlanner.Plan(services.Terrain, services.Layout, _tuning,
                        cassette.PlannerSeed, clear);
                    if (!_sites[i].Tucked)
                    {
                        Debug.LogWarning($"{nameof(CassetteField)}: no small crater rim to tuck '{cassette.Id}' " +
                                         "against; it waits on the best open spot found.", this);
                    }
                }

                clear.Add(_sites[i].Position);
            }

            return true;
        }

        /// <summary>Null when every tape and container stands, else what breaks the content.</summary>
        private string Spawn(GameplayVisuals visuals)
        {
            int count = _sites.Length;
            _containers = new SealedContainer[count];
            _slideStart = new float[count];
            _pieces = new Transform[count];
            _rest = new Vector3[count];
            _restRotation = new Quaternion[count];
            _scale = new Vector3[count];
            _flightStart = new Vector3[count];
            _flightTime = new float[count];
            _flightDuration = new float[count];
            _state = new PieceState[count];
            for (int i = 0; i < count; i++)
            {
                CassetteSite site = _sites[i];
                CassetteDefinition cassette = Definition(i);
                Vector3 spot = site.Position;
                if (cassette.InContainer)
                {
                    string problem = Seal(i, cassette, site, visuals);
                    if (problem != null)
                    {
                        return problem;
                    }

                    Transform root = _containers[i].Root;
                    spot = _containers[i].ItemSocket.position + root.forward * _tuning.SlideOut;
                    spot.y = site.Position.y;
                }

                _rest[i] = spot + Vector3.up * _tuning.RestHeight;
                _restRotation[i] = Quaternion.LookRotation(site.Facing) *
                                   Quaternion.Euler(-_tuning.RestLean, 0f, 0f);
                GameObject piece = Instantiate(cassette.Prefab, _rest[i], _restRotation[i], transform);
                piece.name = "Cassette_" + cassette.Id;
                SetLayer(piece.transform, Layers.Pickup);
                _pieces[i] = piece.transform;
                _scale[i] = piece.transform.localScale;
                if (_containers[i] != null)
                {
                    _state[i] = PieceState.Sealed;
                    piece.SetActive(false);
                }
            }

            return null;
        }

        /// <summary>
        /// Stands Art's container for tape <paramref name="i"/> on its spot, its front toward open ground.
        /// </summary>
        private string Seal(int i, CassetteDefinition cassette, CassetteSite site, GameplayVisuals visuals)
        {
            Transform root;
            if (cassette.Container == ContainerKinds.SupplyPod)
            {
                root = _supplyPod;
            }
            else
            {
                GameObject prefab = visuals.Container(cassette.Container);
                if (prefab == null)
                {
                    return $"Art's container '{cassette.Container}' for cassette '{cassette.Id}' is not in " +
                           "GameplayVisuals (run Gameplay/Materials).";
                }

                root = Instantiate(prefab, transform).transform;
                root.name = cassette.Container;
            }

            root.SetPositionAndRotation(site.Position, Quaternion.LookRotation(site.Facing));
            _containers[i] = SealedContainer.Read(ContainerId(cassette.Id), ContainerKinds.Of(cassette.Container),
                root, SealedContainer.ItemSocketPath, () => true, out string problem);
            if (_containers[i] == null)
            {
                return problem;
            }

            _sealed.Add(_containers[i]);
            return null;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Vector3 rover = _rover.Position;
            Vector3 socket = _rig.CargoSocket.position;
            float magnetSq = _tuning.MagnetRadius * _tuning.MagnetRadius;
            float nearestSq = _tuning.GlanceRadius * _tuning.GlanceRadius;
            int nearest = -1;
            _glints.Begin(_view.Camera.transform.position, Time.time);
            for (int i = 0; i < _pieces.Length; i++)
            {
                switch (_state[i])
                {
                    case PieceState.Sealed:
                        if (_containers[i].StandsOpen)
                        {
                            _state[i] = PieceState.Sliding;
                            _slideStart[i] = Time.time;
                            _pieces[i].gameObject.SetActive(true);
                        }

                        break;
                    case PieceState.Sliding:
                        Slide(i);
                        break;
                    case PieceState.Waiting:
                    {
                        _glints.Add(_rest[i], i);
                        float distanceSq = (_rest[i] - rover).sqrMagnitude;
                        if (distanceSq <= magnetSq)
                        {
                            Launch(i, socket);
                        }
                        else if (distanceSq < nearestSq)
                        {
                            nearestSq = distanceSq;
                            nearest = i;
                        }

                        break;
                    }

                    case PieceState.Flying:
                        Fly(i, socket, deltaTime);
                        break;
                }
            }

            _glints.End();
            _flashes.Tick(deltaTime);
            UpdateGlance(nearest);
        }

        /// <summary>The tape slides out of its opened container to where it then waits for 07.</summary>
        private void Slide(int index)
        {
            float t = Mathf.Clamp01((Time.time - _slideStart[index]) / _tuning.SlideSeconds);
            _pieces[index].SetPositionAndRotation(
                Vector3.Lerp(_containers[index].ItemSocket.position, _rest[index], Ease.OutCubic(t)),
                _restRotation[index]);
            if (t >= 1f)
            {
                _state[index] = PieceState.Waiting;
            }
        }

        private void Launch(int index, Vector3 socket)
        {
            _state[index] = PieceState.Flying;
            _flightStart[index] = _rest[index];
            _flightTime[index] = 0f;
            _flightDuration[index] = PickupFlight.Duration(Vector3.Distance(_rest[index], socket),
                _tuning.FlightDuration, _tuning.FlightPerMetre);
        }

        private void Fly(int index, Vector3 socket, float deltaTime)
        {
            _flightTime[index] += deltaTime;
            float progress = _flightTime[index] / _flightDuration[index];
            if (progress >= 1f)
            {
                Collect(index, socket);
                return;
            }

            Vector3 position = PickupFlight.Evaluate(_flightStart[index], socket, progress, index, 1f,
                _tuning.FlightLift, _tuning.SpiralRadius, _tuning.SpiralTurns);
            float yaw = _restRotation[index].eulerAngles.y + _flightTime[index] * _tuning.FlightSpin;
            Quaternion upright = Quaternion.Euler(0f, yaw, 0f);
            Quaternion rotation = Quaternion.Slerp(_restRotation[index], upright, Ease.OutCubic(progress));
            _pieces[index].SetPositionAndRotation(position, rotation);
            _pieces[index].localScale = _scale[index] *
                                        Mathf.Lerp(1f, _tuning.ArrivalScale, Ease.InOutSine(progress));
        }

        private void Collect(int index, Vector3 socket)
        {
            _state[index] = PieceState.Collected;
            _pieces[index].gameObject.SetActive(false);
            _flashes.Spawn(socket);
            string id = Definition(index).Id;
            if (_radio.AddTape(id))
            {
                _events.Publish(new CassetteCollected(id, socket, _radio.OwnedTapeCount, _radio.TotalTapeCount));
                _save.SaveNow();
            }
        }

        private void UpdateGlance(int nearest)
        {
            if (nearest >= 0)
            {
                _rig.SetGazeTarget(this, _rest[nearest], GazePriorities.Glance);
                _glancing = true;
            }
            else if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
            }
        }

        private void OnDisable()
        {
            if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
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
