using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The glowing scrap scattered over the basin. Every resting piece carries a twinkling glint that reads from far
    /// away (see <see cref="ScrapGlints"/>). Pieces near 07 bob and turn; within the magnet radius they lift
    /// off and spiral into the cargo socket, where each one flashes, adds its value to the wallet and publishes
    /// <see cref="ScrapCollected"/> with the climbing melody step. 07 glances at the nearest piece. Every piece is
    /// instantiated once at initialisation; the frame loop walks plain arrays and allocates nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScrapField : MonoBehaviour
    {
        /// <summary>Turns faster in flight than at rest, so a piece visibly "wakes" when it lifts off.</summary>
        private const float FlightSpinFactor = 6f;

        /// <summary>Resting tilt (degrees) so pieces never sit perfectly flat.</summary>
        private const float RestTilt = 12f;

        /// <summary>Golden ratio: decorrelates each piece's spiral start from its bob phase.</summary>
        private const float SpiralPhaseScale = 1.618034f;

        [Tooltip("Scrap tuning (Assets/_Project/Data/Tuning/Gameplay/ScrapTuning.asset).")]
        [SerializeField] private ScrapTuning _tuning;

        [Tooltip("Scrap variants (Assets/_Project/Data/Content/ScrapCatalog.asset).")]
        [SerializeField] private ScrapCatalog _catalog;

        private EventBus _events;
        private ScrapWallet _wallet;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ScrapGlints _glints;
        private ComboCounter _combo;
        private PickupCadence _cadence;
        private GlowFlashPool _flashes;

        private Transform[] _pieces = Array.Empty<Transform>();
        private Vector3[] _rest = Array.Empty<Vector3>();
        private Vector3[] _scale = Array.Empty<Vector3>();
        private Vector3[] _flightStart = Array.Empty<Vector3>();
        private float[] _phase = Array.Empty<float>();
        private float[] _yaw = Array.Empty<float>();
        private float[] _flightTime = Array.Empty<float>();
        private float[] _flightDuration = Array.Empty<float>();
        private float[] _spiralPhase = Array.Empty<float>();
        private int[] _value = Array.Empty<int>();
        private PieceState[] _state = Array.Empty<PieceState>();
        private bool _glancing;
        private bool _initialized;

        private enum PieceState : byte
        {
            Resting = 0,
            Flying = 1,
            Collected = 2,
        }

        /// <summary>Pieces in the field (collected or not).</summary>
        public int Count => _pieces.Length;

        /// <summary>Pieces still waiting to be picked up (resting or in flight).</summary>
        public int Remaining { get; private set; }

        /// <summary>Wallet value of all pieces not yet collected.</summary>
        public int RemainingValue { get; private set; }

        /// <summary>
        /// Identifies this exact field (seed, world and tuning); saved indices only apply to the same one.
        /// </summary>
        public int LayoutSignature { get; private set; }

        /// <summary>The horizon glints (tests read how many were drawn and how bright).</summary>
        public ScrapGlints Glints => _glints;

        /// <summary>Where piece <paramref name="index"/> floats when at rest.</summary>
        internal Vector3 RestPosition(int index)
        {
            return _rest[index];
        }

        internal bool IsCollected(int index)
        {
            return _state[index] == PieceState.Collected;
        }

        internal int ValueOf(int index)
        {
            return _value[index];
        }

        internal void Wire(ScrapTuning tuning, ScrapCatalog catalog)
        {
            _tuning = tuning;
            _catalog = catalog;
        }

        /// <summary>
        /// Plans the field around the relic <paramref name="sites"/>, clear of the <paramref name="lander"/>, and
        /// spawns every piece.
        /// </summary>
        internal bool Initialize(GameplayServices services, IReadOnlyList<RelicSite> sites, Vector3 lander)
        {
            string problem = _tuning == null ? "ScrapTuning is not assigned."
                : _catalog == null ? "ScrapCatalog is not assigned."
                : _catalog.Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(ScrapField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _wallet = services.Wallet;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _combo = new ComboCounter(_tuning.ComboWindow);
            _cadence = new PickupCadence(_tuning.MinPickupInterval);
            _flashes = new GlowFlashPool(transform, services.Meshes.Sphere, services.Visuals.Flash,
                _tuning.FlashPoolSize, _tuning.FlashDuration, _tuning.FlashRadius, _tuning.FlashIntensity);

            List<ScrapSpawn> spawns = ScrapFieldPlanner.Plan(services.Terrain, services.Layout, _tuning, sites,
                _catalog.Variants, lander);
            Spawn(spawns);
            _glints = new ScrapGlints(transform, services.Visuals.ScrapGlint, _tuning, spawns.Count, Layers.Pickup);
            _initialized = true;
            return true;
        }

        internal ScrapSaveData Capture()
        {
            var collected = new List<int>();
            for (int i = 0; i < _state.Length; i++)
            {
                if (_state[i] == PieceState.Collected)
                {
                    collected.Add(i);
                }
            }

            return new ScrapSaveData { layoutSignature = LayoutSignature, collected = collected.ToArray() };
        }

        internal void Restore(ScrapSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.layoutSignature != LayoutSignature)
            {
                Debug.LogWarning($"{nameof(ScrapField)}: the saved scrap belongs to a different field layout " +
                                 "(world or scrap tuning changed); the field starts full.", this);
                return;
            }

            foreach (int index in data.collected)
            {
                if (index >= 0 && index < _state.Length && _state[index] != PieceState.Collected)
                {
                    MarkCollected(index);
                }
            }
        }

        private void Spawn(List<ScrapSpawn> spawns)
        {
            int count = spawns.Count;
            _pieces = new Transform[count];
            _rest = new Vector3[count];
            _scale = new Vector3[count];
            _flightStart = new Vector3[count];
            _phase = new float[count];
            _yaw = new float[count];
            _flightTime = new float[count];
            _flightDuration = new float[count];
            _spiralPhase = new float[count];
            _value = new int[count];
            _state = new PieceState[count];

            uint signature = Fnv.Offset;
            IReadOnlyList<ScrapVariant> variants = _catalog.Variants;
            for (int i = 0; i < count; i++)
            {
                ScrapSpawn spawn = spawns[i];
                ScrapVariant variant = variants[spawn.Variant];
                GameObject piece = Instantiate(variant.Prefab, spawn.Position, RestRotation(spawn.Yaw, spawn.Phase),
                    transform);
                SetLayer(piece.transform, Layers.Pickup);
                _pieces[i] = piece.transform;
                _rest[i] = spawn.Position;
                _scale[i] = piece.transform.localScale;
                _phase[i] = spawn.Phase;
                _yaw[i] = spawn.Yaw;
                _spiralPhase[i] = spawn.Phase * SpiralPhaseScale;
                _value[i] = variant.Value;
                RemainingValue += variant.Value;
                signature = Fnv.Add(signature, spawn.Variant);
                signature = Fnv.Add(signature, Mathf.RoundToInt(spawn.Position.x * 100f));
                signature = Fnv.Add(signature, Mathf.RoundToInt(spawn.Position.z * 100f));
            }

            Remaining = count;
            LayoutSignature = (int)Fnv.Add(signature, count);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            Vector3 rover = _rover.Position;
            Vector3 socket = _rig.CargoSocket.position;
            float magnetSq = _tuning.MagnetRadius * _tuning.MagnetRadius;
            float animateSq = _tuning.AnimateRadius * _tuning.AnimateRadius;
            float nearestSq = _tuning.GlanceRadius * _tuning.GlanceRadius;
            float bobOmega = 2f * Mathf.PI * _tuning.BobFrequency;
            int nearest = -1;
            _glints.Begin(_view.Camera.transform.position, now);

            for (int i = 0; i < _pieces.Length; i++)
            {
                switch (_state[i])
                {
                    case PieceState.Resting:
                    {
                        _glints.Add(_rest[i], _phase[i]);
                        float distanceSq = (_rest[i] - rover).sqrMagnitude;
                        if (distanceSq > animateSq)
                        {
                            break;
                        }

                        Vector3 position = _rest[i] + Vector3.up *
                            (Mathf.Sin(now * bobOmega + _phase[i]) * _tuning.BobAmplitude);
                        if (distanceSq <= magnetSq)
                        {
                            Launch(i, position, socket);
                            break;
                        }

                        _pieces[i].SetPositionAndRotation(position,
                            RestRotation(_yaw[i] + now * _tuning.SpinSpeed, _phase[i]));
                        if (distanceSq < nearestSq)
                        {
                            nearestSq = distanceSq;
                            nearest = i;
                        }

                        break;
                    }

                    case PieceState.Flying:
                        Fly(i, socket, now, deltaTime);
                        break;
                }
            }

            _glints.End();
            _flashes.Tick(deltaTime);
            UpdateGlance(nearest);
        }

        private void Launch(int index, Vector3 position, Vector3 socket)
        {
            _state[index] = PieceState.Flying;
            _flightStart[index] = position;
            _flightTime[index] = 0f;
            _flightDuration[index] = ScrapFlight.Duration(Vector3.Distance(position, socket),
                _tuning.FlightBaseDuration, _tuning.FlightSecondsPerMetre);
        }

        private void Fly(int index, Vector3 socket, float now, float deltaTime)
        {
            _flightTime[index] += deltaTime;
            float progress = _flightTime[index] / _flightDuration[index];
            if (progress >= 1f && _cadence.TryClaim(now))
            {
                Collect(index, socket, now);
                return;
            }

            float direction = (index & 1) == 0 ? 1f : -1f;
            Vector3 position = ScrapFlight.Evaluate(_flightStart[index], socket, progress, _spiralPhase[index],
                direction, _tuning.FlightLift, _tuning.SpiralRadius, _tuning.SpiralTurns);
            float spin = _yaw[index] + now * _tuning.SpinSpeed * FlightSpinFactor;
            _pieces[index].SetPositionAndRotation(position, RestRotation(spin, _phase[index]));
            _pieces[index].localScale = _scale[index] *
                                        Mathf.Lerp(1f, _tuning.ArrivalScale, Ease.InOutSine(progress));
        }

        private void Collect(int index, Vector3 socket, float now)
        {
            MarkCollected(index);
            _flashes.Spawn(socket);
            int step = _combo.Register(now);
            _events.Publish(new ScrapCollected(socket, _value[index], step));
            _wallet.Add(_value[index]);
        }

        private void MarkCollected(int index)
        {
            _state[index] = PieceState.Collected;
            _pieces[index].gameObject.SetActive(false);
            Remaining--;
            RemainingValue -= _value[index];
        }

        private void UpdateGlance(int nearest)
        {
            if (nearest >= 0)
            {
                _rig.SetGazeTarget(this, _pieces[nearest].position, GazePriorities.Glance);
                _glancing = true;
            }
            else if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
            }
        }

        private void OnDestroy()
        {
            _glints?.Dispose();
        }

        private void OnDisable()
        {
            if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
            }
        }

        private static Quaternion RestRotation(float yaw, float phase)
        {
            return Quaternion.Euler(RestTilt * Mathf.Sin(phase), yaw, RestTilt * Mathf.Cos(phase));
        }

        private static void SetLayer(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayer(root.GetChild(i), layer);
            }
        }

        /// <summary>FNV-1a over integers: a stable signature of the planned field.</summary>
        private static class Fnv
        {
            public const uint Offset = 2166136261u;
            private const uint Prime = 16777619u;

            public static uint Add(uint hash, int value)
            {
                unchecked
                {
                    for (int shift = 0; shift < 32; shift += 8)
                    {
                        hash ^= (uint)(value >> shift) & 0xFFu;
                        hash *= Prime;
                    }

                    return hash;
                }
            }
        }
    }
}
