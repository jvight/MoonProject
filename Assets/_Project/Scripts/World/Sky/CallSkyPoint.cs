using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// The call's point of light beside dark Earth (opening v3 purpose §2), at <see cref="ICallSource.Direction"/>:
    /// keys in phase with the carrier by reading <see cref="ICallSignal.Keying"/> (Audio's playhead, its break-up while
    /// broken in it) and holds a calm glow between the tones (<see cref="CallSkyPointLevel"/>). With no call signal in
    /// the scene it holds its glow, steady; with no call it stays dark. Drawn by LofiSkyPoint at the far plane. Thin:
    /// allocation-free per frame.
    /// </summary>
    public sealed class CallSkyPoint : MonoBehaviour, IGameSystem
    {
        private static readonly int DirectionId = Shader.PropertyToID("_SkyPointDirection");
        private static readonly int LevelId = Shader.PropertyToID("_Level");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its sky's call point.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The point's renderer (LofiSkyPoint on the never-culled sky point quad).")]
        [SerializeField] private Renderer _renderer;

        private readonly CallSkyPointLevel _level = new CallSkyPointLevel();
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
            _subscription = context.Events.Subscribe<CallStateChanged>(OnCallStateChanged);
        }

        /// <summary>The point's level (0..1 of its peak) on the last frame.</summary>
        public float Level { get; private set; }

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
            _level.Step(Time.deltaTime, point.StateFadeSeconds);
            float keying = _signal != null ? _signal.Keying : 0f;
            Level = _level.Level(keying, point.Rest, point.BrokenRest);
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(DirectionId, _call.Direction);
            _block.SetFloat(LevelId, Level);
            _renderer.SetPropertyBlock(_block);
        }

        private void OnCallStateChanged(CallStateChanged changed)
        {
            _level.OnCall(changed.State, changed.Restored);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
