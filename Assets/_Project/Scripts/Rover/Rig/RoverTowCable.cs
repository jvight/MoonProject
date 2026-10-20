using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's tow cable, seen (docs/design/opening-v3-plan.md 3.2b, ruling 4: a physical cable, no beam): a dark rope from
    /// the winch (<see cref="IRoverRig.WinchOrigin"/>) to the load's hitch (<see cref="ITowLine.LoadHitch"/>) with a small
    /// clamp at its end; it lies sagged along the ground while it is slack and runs straight when it is taut, and winds
    /// back in to the winch when the load is let go. The winch drum's turn follows the rope's extension, so it spools
    /// out as the load falls behind and in as it closes up (<see cref="SpoolDegrees"/>, which body language puts on the
    /// drum), and <see cref="Strain01"/> leans 07 into the pull. Mechanical: no glow. Allocation-free per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverTowCable : MonoBehaviour
    {
        [Tooltip("Rig tuning (its Tow Cable settings).")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("The rope: a world-space line, dark matte rubber.")]
        [SerializeField] private LineRenderer _rope;

        [Tooltip("The clamp at the load's end of the rope: a short, thicker line.")]
        [SerializeField] private LineRenderer _hook;

        [Tooltip("RoverModel 'WinchOrigin': where the rope leaves 07.")]
        [SerializeField] private Transform _winch;

        private GameContext _context;
        private ITowLine _line;
        private ITerrainQuery _terrain;
        private Vector3[] _points;
        private readonly Vector3[] _hookPoints = new Vector3[2];
        private bool _wasTowing;
        private Vector3 _lastHitch;
        private float _lastDistance;
        private float _retract = 1f;
        private float _length;

        /// <summary>Degrees the winch drum has turned for the rope's travel so far (out positive).</summary>
        public float SpoolDegrees { get; private set; }

        /// <summary>How hard the cable works now, 0..1 (0 when nothing is towed).</summary>
        public float Strain01 { get; private set; }

        /// <summary>The rope is on show (towing, or winding back in).</summary>
        public bool IsVisible => _rope != null && _rope.enabled;

        /// <summary>Metres of rope between the winch and the clamp now.</summary>
        public float Extension { get; private set; }

        /// <summary>The clamp's place now (tests).</summary>
        public Vector3 HookPoint { get; private set; }

        /// <summary>How far the rope's middle sags below the line between its ends, in metres (tests).</summary>
        public float Sag { get; private set; }

        public bool Initialize(GameContext context)
        {
            if (_tuning == null || _rope == null || _hook == null || _winch == null)
            {
                Debug.LogError($"{nameof(RoverTowCable)}: needs RoverRigTuning, the rope and the clamp lines and the " +
                               "WinchOrigin node (run Rover/Rover).", this);
                return false;
            }

            _context = context;
            TowCableSettings settings = _tuning.TowCable;
            _points = new Vector3[settings.Points];
            _rope.useWorldSpace = true;
            _rope.positionCount = _points.Length;
            _rope.startWidth = settings.Width;
            _rope.endWidth = settings.Width;
            _hook.useWorldSpace = true;
            _hook.positionCount = 2;
            _hook.startWidth = settings.HookWidth;
            _hook.endWidth = settings.HookWidth;
            Show(false);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_line == null)
            {
                _context.TryGet(out _line);
                if (_line == null)
                {
                    return;
                }
            }

            if (_terrain == null)
            {
                _context.TryGet(out _terrain);
            }

            TowCableSettings settings = _tuning.TowCable;
            bool towing = _line.IsTowing;
            if (towing)
            {
                if (!_wasTowing)
                {
                    _lastDistance = Distance(_winch.position, _line.LoadHitch);
                    _retract = 0f;
                }

                _wasTowing = true;
                _lastHitch = _line.LoadHitch;
                _length = _line.Length;
                Strain01 = Mathf.Clamp01(_line.Strain01);
                Place(_lastHitch, settings);
                return;
            }

            Strain01 = 0f;
            if (_wasTowing)
            {
                _wasTowing = false;
                _retract = 0f;
            }

            if (_retract < 1f)
            {
                _retract = Mathf.Min(1f, _retract + deltaTime / settings.RetractSeconds);
                float eased = _retract * _retract * (3f - 2f * _retract);
                Place(Vector3.Lerp(_lastHitch, _winch.position, eased), settings);
                if (_retract >= 1f)
                {
                    Show(false);
                }
            }
        }

        private void Place(Vector3 hitch, TowCableSettings settings)
        {
            Vector3 winch = _winch.position;
            float distance = Distance(winch, hitch);
            SpoolDegrees += (distance - _lastDistance) / settings.MetresPerTurn * 360f;
            _lastDistance = distance;
            Extension = distance;
            float length = _wasTowing ? _length : distance;
            Sag = TowCableShape.Sag(distance, length, settings.MaxSag);
            TowCableShape.Fill(winch, hitch, Sag, _points);
            if (_terrain != null)
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    float ground = _terrain.SampleHeight(_points[i].x, _points[i].z) + settings.GroundGap;
                    if (_points[i].y < ground)
                    {
                        _points[i].y = ground;
                    }
                }
            }

            _rope.SetPositions(_points);
            Vector3 back = _points[_points.Length - 2];
            Vector3 along = hitch - back;
            Vector3 clampStart = along.sqrMagnitude > 1e-6f ? hitch - along.normalized * settings.HookLength : hitch;
            _hookPoints[0] = clampStart;
            _hookPoints[1] = hitch;
            _hook.SetPositions(_hookPoints);
            HookPoint = hitch;
            Show(true);
        }

        private void Show(bool shown)
        {
            _rope.enabled = shown;
            _hook.enabled = shown;
        }

        private static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }
    }
}
