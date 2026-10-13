using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Shows the network's handshake (north-star §4a beat 7): when a mast lights (<see cref="RelayRestored"/>), a warm
    /// bead runs lamp to lamp along the network's links, out from the new mast to every lit mast and back to home, and
    /// each lamp flashes softly as it passes, so the player sees how far the network now reaches. It publishes
    /// <see cref="NetworkPulseReached"/> as each lamp flashes (home lights its cables when the pulse arrives). The
    /// flashes and the bead are World's own halos and lights at the lamps <see cref="IStationReach"/> reports, made
    /// once for the network's size. Thin: the timing lives in <see cref="NetworkPulse"/>.
    /// </summary>
    public sealed class NetworkPulseSystem : MonoBehaviour, IGameSystem
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int MinAngleTanId = Shader.PropertyToID("_MinAngleTan");
        private static readonly int MinAngleFromId = Shader.PropertyToID("_MinAngleFrom");
        private static readonly int MinAngleFullId = Shader.PropertyToID("_MinAngleFull");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Network Pulse block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The halo quad the flashes and the bead are drawn with (Generated/World/Beacon/BeaconHaloQuad).")]
        [SerializeField] private Mesh _haloMesh;

        [Tooltip("A LofiBeaconHalo material (Generated/World/Beacon/M_BeaconHalo); colour, size and glow are set per " +
            "halo from the Network Pulse block.")]
        [SerializeField] private Material _haloMaterial;

        private GameContext _context;
        private IDisposable _relay;
        private MaterialPropertyBlock _block;
        private NetworkPulse _pulse;
        private Vector3[] _lamps;
        private bool[] _lit;
        private string[] _ids;
        private bool[] _announced;
        private Renderer[] _flashHalos;
        private Light[] _flashLights;
        private Renderer[] _beadHalos;
        private Light[] _beadLights;
        private bool _running;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _haloMesh == null || _haloMaterial == null)
            {
                Debug.LogError($"{nameof(NetworkPulseSystem)}: World Settings, the halo mesh and the halo material " +
                    "must all be assigned (the scene build wires them).", this);
                enabled = false;
                return;
            }

            _context = context;
            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _pulse = new NetworkPulse(_settings.NetworkPulse);
            _relay = context.Events.Subscribe<RelayRestored>(OnRelayRestored);
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            float time = Time.time;
            NetworkPulseSettings tuning = _settings.NetworkPulse;
            for (int i = 0; i < _pulse.Count; i++)
            {
                Announce(i, time);
                float flash = _pulse.Flash(i, time);
                Show(_flashHalos[i], _flashLights[i], _lamps[i], flash, tuning.FlashIntensity, tuning.FlashRadius,
                    tuning.FlashLight);
                bool travelling = _pulse.Bead(i, time, out Vector3 bead, out float level);
                Show(_beadHalos[i], _beadLights[i], bead, travelling ? level : 0f, tuning.BeadIntensity,
                    tuning.BeadRadius, tuning.BeadLight);
            }

            if (!_pulse.IsActive(time))
            {
                _running = false;
                HideAll();
            }
        }

        private void OnRelayRestored(RelayRestored restored)
        {
            if (!_context.TryGet(out IStationReach reach))
            {
                Debug.LogError($"{nameof(NetworkPulseSystem)}: no {nameof(IStationReach)} registered, so the " +
                    "network's handshake cannot find the masts.", this);
                return;
            }

            int count = reach.NodeCount;
            EnsurePool(count);
            int origin = -1;
            for (int i = 0; i < count; i++)
            {
                RelayNode node = reach.GetNode(i);
                _lamps[i] = node.LampPosition;
                _lit[i] = node.Lit;
                _ids[i] = node.Id;
                _announced[i] = false;
                if (node.Id == restored.RelayId)
                {
                    origin = i;
                }
            }

            if (origin <= 0)
            {
                Debug.LogError($"{nameof(NetworkPulseSystem)}: mast '{restored.RelayId}' is not a mast of the " +
                    "station's network.", this);
                return;
            }

            HideAll();
            _pulse.Start(_lamps, _lit, count, origin, Time.time);
            _running = true;
        }

        private void Announce(int node, float time)
        {
            if (_announced[node] || time < _pulse.ArrivalTime(node))
            {
                return;
            }

            _announced[node] = true;
            _context.Events.Publish(new NetworkPulseReached(_ids[node], _lamps[node], _pulse.Hops(node),
                _pulse.FarthestHop));
        }

        private void Show(Renderer halo, Light light, Vector3 position, float level, float intensity, float radius,
            float lightIntensity)
        {
            bool showing = level > 0f;
            halo.enabled = showing;
            light.enabled = showing;
            if (!showing)
            {
                return;
            }

            NetworkPulseSettings tuning = _settings.NetworkPulse;
            halo.transform.position = position;
            _block.Clear();
            _block.SetColor(ColorId, tuning.Color);
            _block.SetFloat(IntensityId, level * intensity);
            _block.SetFloat(RadiusId, radius);
            _block.SetFloat(MinAngleTanId, Mathf.Tan(tuning.MinAngle * Mathf.Deg2Rad));
            _block.SetFloat(MinAngleFromId, tuning.MinAngleFrom);
            _block.SetFloat(MinAngleFullId, tuning.MinAngleFull);
            halo.SetPropertyBlock(_block);
            light.intensity = level * lightIntensity;
        }

        // The network's size is fixed once Gameplay has set it up, so the halos and lights are made once, on the
        // first handshake: one flash per node and one bead per node's link home.
        private void EnsurePool(int count)
        {
            if (_lamps != null && _lamps.Length >= count)
            {
                return;
            }

            NetworkPulseSettings tuning = _settings.NetworkPulse;
            _lamps = new Vector3[count];
            _lit = new bool[count];
            _ids = new string[count];
            _announced = new bool[count];
            _flashHalos = new Renderer[count];
            _flashLights = new Light[count];
            _beadHalos = new Renderer[count];
            _beadLights = new Light[count];
            for (int i = 0; i < count; i++)
            {
                _flashHalos[i] = CreateHalo($"Flash {i}", tuning.FlashLightRange, out _flashLights[i]);
                _beadHalos[i] = CreateHalo($"Bead {i}", tuning.BeadLightRange, out _beadLights[i]);
            }
        }

        private Renderer CreateHalo(string name, float range, out Light light)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            host.AddComponent<MeshFilter>().sharedMesh = _haloMesh;
            var halo = host.AddComponent<MeshRenderer>();
            halo.sharedMaterial = _haloMaterial;
            halo.shadowCastingMode = ShadowCastingMode.Off;
            halo.receiveShadows = false;
            halo.lightProbeUsage = LightProbeUsage.Off;
            halo.reflectionProbeUsage = ReflectionProbeUsage.Off;
            halo.enabled = false;
            light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = _settings.NetworkPulse.Color;
            light.range = range;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return halo;
        }

        private void HideAll()
        {
            if (_flashHalos == null)
            {
                return;
            }

            for (int i = 0; i < _flashHalos.Length; i++)
            {
                _flashHalos[i].enabled = false;
                _flashLights[i].enabled = false;
                _beadHalos[i].enabled = false;
                _beadLights[i].enabled = false;
            }
        }

        private void OnDestroy()
        {
            _relay?.Dispose();
        }
    }
}
