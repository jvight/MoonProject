using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// The call's point of light beside dark Earth (opening v3 purpose §2), at <see cref="ICallSource.Direction"/>:
    /// keys in phase with the carrier by reading <see cref="ICallSignal.Keying"/> (Audio's playhead, its break-up while
    /// broken in it), sends a ring out as each tone begins (<see cref="SignalPulse"/>) and holds a dim ember between the
    /// tones (<see cref="CallSkyPointLevel"/>); it clears the stars round itself through the sky's
    /// _MoonCallPointClear global. With no call signal in the scene it holds its ember, steady; with no call it stays
    /// dark and clears no stars. Drawn by LofiSkyPoint at the far plane. Thin: allocation-free per frame.
    /// </summary>
    public sealed class CallSkyPoint : MonoBehaviour, IGameSystem
    {
        private static readonly int DirectionId = Shader.PropertyToID("_SkyPointDirection");
        private static readonly int LevelId = Shader.PropertyToID("_Level");
        private static readonly int KeyId = Shader.PropertyToID("_Key");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int ClearId = Shader.PropertyToID("_MoonCallPointClear");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its sky's call point.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The point's renderer (LofiSkyPoint on the never-culled sky point quad).")]
        [SerializeField] private Renderer _renderer;

        private readonly CallSkyPointLevel _level = new CallSkyPointLevel();
        private readonly SignalPulse _pulse = new SignalPulse();
        private GameContext _context;
        private ICallSource _call;
        private ICallSignal _signal;
        private IDisposable _subscription;
        private MaterialPropertyBlock _block;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _renderer == null)
            {
                Debug.LogError($"{nameof(CallSkyPoint)}: World Settings and the point's renderer must be assigned " +
                    "(the scene build wires them).", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            _renderer.enabled = false;
            Shader.SetGlobalVector(ClearId, Vector4.zero);
            _subscription = context.Events.Subscribe<CallStateChanged>(OnCallStateChanged);
        }

        /// <summary>The point's level (0..1 of its peak) on the last frame.</summary>
        public float Level { get; private set; }

        /// <summary>How far through its bloom the ring is, 0 (just begun) to 1 (none) on the last frame.</summary>
        public float Pulse { get; private set; } = 1f;

        private void Update()
        {
            if (_block == null)
            {
                return;
            }

            if (_call == null)
            {
                if (!_context.TryGet(out _call))
                {
                    return;
                }

                _level.OnCall(_call.State, true);
                _renderer.enabled = true;
            }

            if (_signal == null)
            {
                _context.TryGet(out _signal);
            }

            CallSkyPointSettings point = _settings.Sky.CallPoint;
            float keying = _signal != null ? Mathf.Clamp01(_signal.Keying) : 0f;
            _level.Step(Time.deltaTime, point.StateFadeSeconds);
            _pulse.Step(keying, Time.deltaTime, point.ToneOn, point.ToneOff);
            Level = _level.Level(keying, point.Rest, point.BrokenRest);
            Pulse = _pulse.Progress(point.PulseSeconds);
            Vector3 direction = _call.Direction;
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(DirectionId, direction);
            _block.SetFloat(LevelId, Level);
            _block.SetFloat(KeyId, keying);
            _block.SetFloat(PulseId, Pulse);
            _renderer.SetPropertyBlock(_block);
            Shader.SetGlobalVector(ClearId, new Vector4(direction.x, direction.y, direction.z,
                point.ClearDegrees * Mathf.Deg2Rad));
        }

        private void OnCallStateChanged(CallStateChanged changed)
        {
            _level.OnCall(changed.State, changed.Restored);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
            Shader.SetGlobalVector(ClearId, Vector4.zero);
        }
    }
}
