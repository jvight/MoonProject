using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's battery on its back (docs/features/M3-20 "How 07 shows it"): a short segmented warm light bar along the
    /// lid's rear-top edge, eight segments built once and shown per capacity (four at the start, six with Cell
    /// Pack II), lit by <see cref="FlankLampStrip"/> from <see cref="IRoverBattery"/> and eased so a cell fills and
    /// empties softly. 07 sleeps under the title with its lamps dark; in the opening they stay dark until O5's flicker,
    /// then light one by one (<see cref="FlankLampLit"/>, a quiet tick each), and the short wake lights them at once. A
    /// scene without Gameplay has no battery: the strip stays hidden there. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverFlankLamps : MonoBehaviour
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [Tooltip("Rig tuning (its Flank Lamps settings).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("RoverModel 'BatteryLamp_0..7': each light-bar segment's tray, shown per capacity.")]
        [SerializeField] private Transform[] _lamps = Array.Empty<Transform>();

        [Tooltip("RoverModel 'BatteryGlass_0..7': each segment's glass, its own glow renderer.")]
        [SerializeField] private Renderer[] _glasses = Array.Empty<Renderer>();

        private BatteryLink _battery;
        private EventBus _events;
        private IDisposable _wake;
        private IDisposable _title;
        private float _wakeStart = float.NegativeInfinity;
        private int _wakeLit = RoverModelNodes.FlankLampCount;
        private MaterialPropertyBlock _block;
        private float[] _levels = Array.Empty<float>();
        private int _shownCells = -1;

        /// <summary>The middle of the bar, where 07 glances when its leap is refused.</summary>
        public Vector3 GlancePoint => _lamps.Length > 0 ? _lamps[_lamps.Length / 2].position : transform.position;

        public bool Initialize(GameContext context)
        {
            int sockets = RoverModelNodes.FlankLampCount;
            if (_tuning == null || _lamps == null || _glasses == null || _lamps.Length != sockets ||
                _glasses.Length != sockets)
            {
                Debug.LogError($"{nameof(RoverFlankLamps)}: needs RoverRigTuning and {sockets} lamps and glasses " +
                               "(RoverModel 'BatteryLamp_*' / 'BatteryGlass_*'; run Art/Rover then Rover/Rover).", this);
                return false;
            }

            for (int i = 0; i < sockets; i++)
            {
                if (_lamps[i] == null || _glasses[i] == null)
                {
                    Debug.LogError($"{nameof(RoverFlankLamps)}: battery lamp {i} is missing.", this);
                    return false;
                }
            }

            _battery = new BatteryLink(context);
            _events = context.Events;
            _wake = _events.Subscribe<RoverWakeBeatReached>(OnWakeBeat);
            _title = _events.Subscribe<TitleShown>(OnTitleShown);
            _block = new MaterialPropertyBlock();
            _levels = new float[RoverModelNodes.FlankLampCount];
            for (int i = 0; i < RoverModelNodes.FlankLampCount; i++)
            {
                Apply(i, 0f);
            }

            ShowCells(0);
            return true;
        }

        public void Tick(float deltaTime)
        {
            IRoverBattery battery = _battery.Battery;
            if (battery == null)
            {
                return;
            }

            ShowCells(battery.Cells);
            FlankLampSettings settings = _tuning.FlankLamps;
            float time = Time.time;
            StepWake(settings, time);
            for (int i = 0; i < _shownCells; i++)
            {
                float target = i < _wakeLit
                    ? FlankLampStrip.Level(settings, i, battery.Charge, battery.IsLowPower, time)
                    : 0f;
                _levels[i] = Smoothing.Damp(_levels[i], target, settings.HalfLife, deltaTime);
                Apply(i, _levels[i]);
            }
        }

        /// <summary>After O5's flicker the lamps light one by one, each with a tick, up to the battery's cells.</summary>
        private void StepWake(FlankLampSettings settings, float time)
        {
            if (_wakeLit >= _shownCells || time < _wakeStart)
            {
                return;
            }

            int due = Mathf.Min(_shownCells, Mathf.FloorToInt((time - _wakeStart) / settings.WakeInterval) + 1);
            while (_wakeLit < due)
            {
                _events.Publish(new FlankLampLit(_wakeLit, _lamps[_wakeLit].position));
                _wakeLit++;
            }
        }

        /// <summary>
        /// The opening's beats: dark from the fade to black (or from O1's crackle, after New Game's reload under
        /// the black), lit one by one from O5's flicker (or, when a hurried opening skips it, from the blink); the
        /// short wake lights them at once.
        /// </summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            switch (reached.Beat)
            {
                case WakeBeat.FadedOut:
                case WakeBeat.Crackle:
                    _wakeLit = 0;
                    _wakeStart = float.PositiveInfinity;
                    break;
                case WakeBeat.Flicker:
                case WakeBeat.Blinked:
                    _wakeStart = Mathf.Min(_wakeStart, Time.time);
                    break;
                case WakeBeat.Stirred:
                    _wakeLit = RoverModelNodes.FlankLampCount;
                    break;
            }
        }

        /// <summary>07 sleeps under the title, fresh or over a save: dark until a wake beat lights the lamps.</summary>
        private void OnTitleShown(TitleShown shown)
        {
            _wakeLit = 0;
            _wakeStart = float.PositiveInfinity;
        }

        private void OnDestroy()
        {
            _wake?.Dispose();
            _title?.Dispose();
        }

        /// <summary>Shows one segment per cell (the rest stay hidden).</summary>
        private void ShowCells(int cells)
        {
            cells = Mathf.Clamp(cells, 0, RoverModelNodes.FlankLampCount);
            if (cells == _shownCells)
            {
                return;
            }

            _shownCells = cells;
            for (int i = 0; i < RoverModelNodes.FlankLampCount; i++)
            {
                _lamps[i].gameObject.SetActive(i < cells);
            }
        }

        private void Apply(int socket, float level)
        {
            _block.SetFloat(EmissionStrengthId, Mathf.Clamp01(level));
            _glasses[socket].SetPropertyBlock(_block);
        }
    }
}
