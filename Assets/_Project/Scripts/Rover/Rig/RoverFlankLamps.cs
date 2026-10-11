using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's battery on its flanks (docs/features/M3-20 "How 07 shows it"): a strip of small amber lamps along the
    /// orange stripe on each side, eight sockets built once and shown per capacity (four at the start, six with Cell
    /// Pack II), lit by <see cref="FlankLampStrip"/> from <see cref="IRoverBattery"/> and eased so a cell fills and
    /// empties softly. A scene without Gameplay has no battery: the strip stays hidden there. Allocation-free per
    /// frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverFlankLamps : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Rig tuning (its Flank Lamps settings).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("RoverModel 'FlankLamp_L0..7' then 'FlankLamp_R0..7': each lamp's housing, shown per capacity.")]
        [SerializeField] private Transform[] _lamps = Array.Empty<Transform>();

        [Tooltip("RoverModel 'FlankGlass_L0..7' then 'FlankGlass_R0..7': each lamp's glass, its own glow renderer.")]
        [SerializeField] private Renderer[] _glasses = Array.Empty<Renderer>();

        private BatteryLink _battery;
        private MaterialPropertyBlock _block;
        private float[] _levels = Array.Empty<float>();
        private int _shownCells = -1;

        /// <summary>The middle of the right strip, where 07 glances when its leap is refused.</summary>
        public Vector3 GlancePoint => _lamps.Length > 0
            ? _lamps[_lamps.Length - RoverModelNodes.FlankLampCount / 2].position
            : transform.position;

        public bool Initialize(GameContext context)
        {
            int sockets = 2 * RoverModelNodes.FlankLampCount;
            if (_tuning == null || _lamps == null || _glasses == null || _lamps.Length != sockets ||
                _glasses.Length != sockets)
            {
                Debug.LogError($"{nameof(RoverFlankLamps)}: needs RoverRigTuning and {sockets} lamps and glasses " +
                               "(RoverModel 'FlankLamp_*' / 'FlankGlass_*'; run Art/Rover then Rover/Rover).", this);
                return false;
            }

            for (int i = 0; i < sockets; i++)
            {
                if (_lamps[i] == null || _glasses[i] == null)
                {
                    Debug.LogError($"{nameof(RoverFlankLamps)}: flank lamp {i} is missing.", this);
                    return false;
                }
            }

            _battery = new BatteryLink(context);
            _block = new MaterialPropertyBlock();
            _levels = new float[RoverModelNodes.FlankLampCount];
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
            for (int i = 0; i < _shownCells; i++)
            {
                float target = FlankLampStrip.Level(settings, i, battery.Charge, battery.IsLowPower, time);
                _levels[i] = Smoothing.Damp(_levels[i], target, settings.HalfLife, deltaTime);
                Apply(i, _levels[i]);
                Apply(i + RoverModelNodes.FlankLampCount, _levels[i]);
            }
        }

        /// <summary>Shows one socket per cell on each flank (the rest stay hidden).</summary>
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
                bool shown = i < cells;
                _lamps[i].gameObject.SetActive(shown);
                _lamps[i + RoverModelNodes.FlankLampCount].gameObject.SetActive(shown);
            }
        }

        private void Apply(int socket, float level)
        {
            _block.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
            _glasses[socket].SetPropertyBlock(_block);
        }
    }
}
