using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// The warm pools on the relay masts' charge rings (docs/features/M3-20 "Chargers"): one light disc (Art's
    /// LightPool) on every relay pad, dark while its mast is dormant, warm once the station's reach shows it lit, and
    /// warmer while the pad fills 07's cells (<see cref="ChargingChanged"/>). The reach comes from Gameplay
    /// (<see cref="IStationReach"/>), resolved at the first frame; a world without a relay network keeps its pools
    /// dark. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChargerPools : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("World settings (its Charger Pools section).")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Art's warm light disc (Generated/Art/Props/LightPool.prefab), one placed on each relay pad.")]
        [SerializeField] private GameObject _discPrefab;

        private GameContext _context;
        private IStationReach _reach;
        private MaterialPropertyBlock _block;
        private IDisposable _charging;
        private Renderer[] _discs = Array.Empty<Renderer>();
        private Vector3[] _pads = Array.Empty<Vector3>();
        private int[] _nodes = Array.Empty<int>();
        private float[] _levels = Array.Empty<float>();
        private Vector3 _chargingAt;
        private bool _chargingNow;
        private bool _resolved;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _discPrefab == null)
            {
                Debug.LogError($"{nameof(ChargerPools)}: WorldSettings and the LightPool prefab must be assigned.",
                    this);
                enabled = false;
                return;
            }

            _context = context ?? throw new ArgumentNullException(nameof(context));
            _block = new MaterialPropertyBlock();
            PlaceDiscs(context.Get<IWorldAnchors>());
            _charging = context.Events.Subscribe<ChargingChanged>(OnChargingChanged);
        }

        /// <summary>The glow of the pool on relay pad <paramref name="index"/> (tests and captures).</summary>
        public float Level(int index)
        {
            return _levels[index];
        }

        public int Count => _discs.Length;

        private void PlaceDiscs(IWorldAnchors anchors)
        {
            int count = 0;
            while (anchors.TryGet(WorldAnchorIds.RelayPrefix + count, out _))
            {
                count++;
            }

            ChargerPoolSettings settings = _settings.ChargerPools;
            _discs = new Renderer[count];
            _pads = new Vector3[count];
            _nodes = new int[count];
            _levels = new float[count];
            for (int i = 0; i < count; i++)
            {
                anchors.TryGet(WorldAnchorIds.RelayPrefix + i, out WorldAnchor anchor);
                _pads[i] = anchor.Position;
                GameObject disc = Instantiate(_discPrefab, transform);
                disc.name = "ChargerPool_" + anchor.Id;
                disc.transform.SetPositionAndRotation(anchor.Position + Vector3.up * settings.Lift,
                    Quaternion.identity);
                disc.transform.localScale = Vector3.one * settings.Radius;
                _discs[i] = disc.GetComponent<Renderer>();
                _discs[i].enabled = false;
                _nodes[i] = -1;
            }
        }

        private void Update()
        {
            if (_block == null || !Resolve())
            {
                return;
            }

            ChargerPoolSettings settings = _settings.ChargerPools;
            float deltaTime = Time.deltaTime;
            float near = settings.Radius * settings.Radius;
            for (int i = 0; i < _discs.Length; i++)
            {
                bool lit = _nodes[i] >= 0 && _reach.GetNode(_nodes[i]).Lit;
                bool charging = _chargingNow && (_chargingAt - _pads[i]).sqrMagnitude < near;
                float target = !lit ? 0f : charging ? settings.ChargingGlow : settings.LitGlow;
                float level = _levels[i] + (target - _levels[i]) * (1f - Mathf.Exp(-deltaTime / settings.Ease));
                if (Mathf.Approximately(level, _levels[i]))
                {
                    continue;
                }

                _levels[i] = level;
                _block.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
                _discs[i].SetPropertyBlock(_block);
                _discs[i].enabled = level > 0f;
            }
        }

        /// <summary>The station's reach, matched to the pads by mast id, once Gameplay has registered it.</summary>
        private bool Resolve()
        {
            if (_resolved)
            {
                return _reach != null;
            }

            _resolved = true;
            if (!_context.TryGet(out _reach))
            {
                return false;
            }

            for (int node = 0; node < _reach.NodeCount; node++)
            {
                string id = _reach.GetNode(node).Id;
                for (int i = 0; i < _discs.Length; i++)
                {
                    if (id == WorldAnchorIds.RelayPrefix + i)
                    {
                        _nodes[i] = node;
                    }
                }
            }

            return true;
        }

        private void OnChargingChanged(ChargingChanged changed)
        {
            _chargingNow = changed.Charging && changed.Charger == ChargerKind.Mast;
            _chargingAt = changed.Position;
        }

        private void OnDestroy()
        {
            _charging?.Dispose();
        }
    }
}
