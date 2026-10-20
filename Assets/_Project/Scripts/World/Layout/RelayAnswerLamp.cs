using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's lamp (opening v3 R8): the dead west mast stays dark until the call is answered; from then on, in this
    /// session and every later one, its halo keys with <see cref="ICallSignal.Keying"/> (the answered loop's own sign,
    /// so the lamp and the sound agree). With no signal it stays dark: no error. The scene build stands the halo at the
    /// mast's lamp node and wires it. Once the relay is relit (<see cref="RelayRelit"/>) it stops keying, warms up with a
    /// short flicker and holds a calm steady glow (<see cref="RelayLampState"/>); a relit relay loaded from a save is
    /// steady at once. Allocation-free per frame.
    /// </summary>
    public sealed class RelayAnswerLamp : MonoBehaviour, IGameSystem
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int MinAngleTanId = Shader.PropertyToID("_MinAngleTan");
        private static readonly int MinAngleFromId = Shader.PropertyToID("_MinAngleFrom");
        private static readonly int MinAngleFullId = Shader.PropertyToID("_MinAngleFull");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its landmarks' relay lamp.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The lamp's halo at the dead mast's lamp node (LofiBeaconHalo, amber).")]
        [SerializeField] private Renderer _halo;

        // The relay whose lamp this is (its world anchor's id).
        private const string RelayId = WorldAnchorIds.RelayPrefix + "1";

        private readonly RelayLampState _state = new RelayLampState();
        private GameContext _context;
        private ICallSource _call;
        private IDisposable _relit;
        private ICallSignal _signal;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _halo == null)
            {
                Debug.LogError($"{nameof(RelayAnswerLamp)}: World Settings and the halo must be assigned (the scene " +
                    "build wires them at relay.1's mast).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            RelayLampSettings lamp = _settings.Landmarks.RelayLamp;
            _halo.GetPropertyBlock(_block);
            _block.SetFloat(RadiusId, lamp.HaloRadius);
            _block.SetFloat(MinAngleTanId, Mathf.Tan(lamp.HaloMinAngle * Mathf.Deg2Rad));
            _block.SetFloat(MinAngleFromId, lamp.HaloMinFrom);
            _block.SetFloat(MinAngleFullId, lamp.HaloMinFull);
            _block.SetFloat(IntensityId, 0f);
            _halo.SetPropertyBlock(_block);
            _relit = context.Events.Subscribe<RelayRelit>(OnRelit);
        }

        /// <summary>The lamp's level (0..1) on the last frame.</summary>
        public float Level { get; private set; }

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            if (_call == null)
            {
                _context.TryGet(out _call);
            }

            if (_signal == null)
            {
                _context.TryGet(out _signal);
            }

            RelayLampSettings lamp = _settings.Landmarks.RelayLamp;
            bool answered = _call != null && _call.State >= CallState.Answered;
            float keying = answered && _signal != null ? _signal.Keying : 0f;
            _state.Step(Time.deltaTime);
            Level = _state.Level(keying, lamp);
            _halo.GetPropertyBlock(_block);
            _block.SetFloat(IntensityId, Level * lamp.HaloIntensity);
            _halo.SetPropertyBlock(_block);
        }

        private void OnRelit(RelayRelit relit)
        {
            if (relit.RelayId == RelayId)
            {
                _state.OnRelit(relit.Restored);
            }
        }

        private void OnDestroy()
        {
            _relit?.Dispose();
            _relit = null;
        }
    }
}
