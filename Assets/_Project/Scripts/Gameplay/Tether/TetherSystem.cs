using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The energy tether. The most central loose relic inside a soft aim cone around the screen centre is
    /// highlighted (and stays picked while it is roughly there); holding Tether latches a glowing beam from 07's eye
    /// onto it (<see cref="TetherAttached"/>). A PD spring floats it along behind 07 at the winch length, mass and all.
    /// Letting go releases it where it is; if it gets caught or falls far behind, the tether lets go softly by itself
    /// and the relic stays right there (<see cref="TetherReleased"/>, snapped). 07 looks at what it aims at and fixes
    /// on what it tows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherSystem : MonoBehaviour, ITetherAim
    {
        private const int OcclusionMask = Layers.GroundMask | Layers.PropMask;

        [Tooltip("Tether tuning (Assets/_Project/Data/Tuning/Gameplay/TetherTuning.asset).")]
        [SerializeField] private TetherTuning _tuning;

        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ITerrainQuery _terrain;
        private RelicField _relics;
        private WinchControl _winch;
        private TetherSnapRule _snap;
        private LineRenderer _line;
        private GlowRenderer _glow;
        private Vector3[] _points = Array.Empty<Vector3>();
        private Relic _hovered;
        private Relic _towed;
        private bool _armed;
        private float _reach;
        private bool _retracting;
        private Vector3 _beamEnd;
        private float _savedLinearDamping;
        private float _savedAngularDamping;
        private bool _gazing;
        private bool _initialized;

        public TetherAimState State => _towed != null ? TetherAimState.Towing
            : _hovered != null ? TetherAimState.Hovering
            : TetherAimState.Idle;

        public Vector3 TargetPosition => _towed != null ? _towed.transform.position
            : _hovered != null ? _hovered.transform.position
            : Vector3.zero;

        public float Strain => _towed != null ? _snap.Strain : 0f;

        public float Length => _towed != null ? _winch.Length : 0f;

        /// <summary>The relic on the tether, or null.</summary>
        public Relic Towed => _towed;

        /// <summary>The highlighted relic a press would latch onto, or null.</summary>
        public Relic Hovered => _hovered;

        public TetherTuning Tuning => _tuning;

        /// <summary>Current beam brightness (tests and debugging views).</summary>
        public float BeamIntensity => _glow != null ? _glow.Intensity : 0f;

        internal void Wire(TetherTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(TetherSystem)}: TetherTuning is not assigned.", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _terrain = services.Terrain;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _winch = new WinchControl(_tuning.MinLength, _tuning.MaxLength, _tuning.WinchStep, _tuning.ReelSpeed,
                _tuning.WinchLead);
            _snap = new TetherSnapRule(_tuning.SnapStretch, _tuning.SnapGrace, _tuning.SnapDistance);
            _points = new Vector3[_tuning.BeamPoints];
            _line = CreateLine(services.Visuals.TetherBeam);
            _glow = new GlowRenderer(_line);
            _initialized = true;
            return true;
        }

        private LineRenderer CreateLine(Material material)
        {
            var host = new GameObject("TetherBeam");
            host.transform.SetParent(transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = _points.Length;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.widthCurve = AnimationCurve.Linear(0f, _tuning.BeamWidth.x, 1f, _tuning.BeamWidth.y);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            GlowObject.Configure(line, material);
            return line;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (_input.TetherPressed)
            {
                _armed = true;
            }

            if (!_input.TetherHeld)
            {
                _armed = false;
                if (_towed != null)
                {
                    Release(false, true);
                }
            }

            if (_towed == null)
            {
                SetHovered(FindTarget());
                if (_armed && _hovered != null)
                {
                    Attach(_hovered);
                }
            }
            else
            {
                _winch.Step(_input.Winch, Time.deltaTime);
            }

            DrawBeam(Time.deltaTime);
            UpdateGaze();
        }

        private void FixedUpdate()
        {
            if (_towed == null)
            {
                return;
            }

            Rigidbody body = _towed.Body;
            Vector3 anchor = _rig.TetherOrigin.position;
            Vector3 position = body.position;
            float ground = _terrain.SampleHeight(position.x, position.z);
            float ceiling = _rover.Position.y + _tuning.MaxLiftHeight;
            Vector3 target = TetherPhysics.Target(anchor, position, _winch.Length, ground, _tuning.HoverHeight,
                ceiling);
            body.AddForce(TetherPhysics.Force(position, body.linearVelocity, target, _rover.Velocity, body.mass,
                -Physics.gravity.y, _tuning));
            if (_snap.Step(Vector3.Distance(position, anchor), _winch.Length, Time.fixedDeltaTime))
            {
                Release(true, true);
            }
        }

        private Relic FindTarget()
        {
            Transform view = _view.Camera.transform;
            Vector3 eye = view.position;
            Vector3 forward = view.forward;
            Relic best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (!relic.IsTetherable)
                {
                    continue;
                }

                float cone = relic == _hovered ? _tuning.StickyCone : _tuning.AimCone;
                Vector3 position = relic.transform.position;
                if (TetherAim.TryScore(eye, forward, position, cone, _tuning.AimRange, _tuning.DistanceWeight,
                        out float score) && score < bestScore && InSight(eye, position, relic.Radius))
                {
                    best = relic;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool InSight(Vector3 eye, Vector3 target, float radius)
        {
            Vector3 toTarget = target - eye;
            float distance = toTarget.magnitude - radius;
            return distance <= 0f || !Physics.Raycast(eye, toTarget.normalized, distance, OcclusionMask,
                QueryTriggerInteraction.Ignore);
        }

        private void SetHovered(Relic relic)
        {
            if (relic == _hovered)
            {
                return;
            }

            if (_hovered != null)
            {
                _hovered.SetAimHighlight(0f);
            }

            _hovered = relic;
            if (_hovered != null)
            {
                _hovered.SetAimHighlight(_tuning.HoverHighlight);
            }
        }

        private void Attach(Relic relic)
        {
            SetHovered(null);
            _towed = relic;
            relic.IsTethered = true;
            relic.SetAimHighlight(_tuning.TowHighlight);
            Rigidbody body = relic.Body;
            _savedLinearDamping = body.linearDamping;
            _savedAngularDamping = body.angularDamping;
            body.linearDamping = _tuning.TowLinearDamping;
            body.angularDamping = _tuning.TowAngularDamping;
            body.WakeUp();
            _winch.Reset(Vector3.Distance(body.position, _rig.TetherOrigin.position));
            _snap.Reset();
            _reach = 0f;
            _retracting = false;
            _events.Publish(new TetherAttached(body.position, body.mass));
        }

        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        private void Release(bool snapped, bool announce)
        {
            Relic relic = _towed;
            Rigidbody body = relic.Body;
            body.linearDamping = _savedLinearDamping;
            body.angularDamping = _savedAngularDamping;
            if (snapped)
            {
                body.linearVelocity *= _tuning.SnapCarry;
                body.angularVelocity *= _tuning.SnapCarry;
                _armed = false;
            }

            relic.IsTethered = false;
            relic.SetAimHighlight(0f);
            _towed = null;
            _beamEnd = relic.transform.position;
            _retracting = true;
            _snap.Reset();
            if (announce)
            {
                _events.Publish(new TetherReleased(_beamEnd, snapped));
            }
        }

        private void DrawBeam(float deltaTime)
        {
            if (_towed != null)
            {
                _reach = Mathf.MoveTowards(_reach, 1f, deltaTime / _tuning.BeamExtend);
                _beamEnd = _towed.transform.position;
            }
            else if (_retracting)
            {
                _reach = Mathf.MoveTowards(_reach, 0f, deltaTime / _tuning.BeamRetract);
                _retracting = _reach > 0f;
            }

            float intensity = _towed != null ? _tuning.BeamIntensity : _tuning.BeamIntensity * Ease.InOutSine(_reach);
            _glow.Apply(_retracting || _towed != null ? intensity : 0f);
            if (!_line.enabled)
            {
                return;
            }

            Vector3 start = _rig.TetherOrigin.position;
            Vector3 end = Vector3.LerpUnclamped(start, _beamEnd, Ease.OutCubic(_reach));
            float wobble = _tuning.Wobble + Strain * _tuning.StrainWobble;
            Vector3 control = TetherBeamShape.Control(start, end, _tuning.BeamArc, wobble, _tuning.WobbleFrequency,
                Time.time);
            int last = _points.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                _points[i] = TetherBeamShape.Point(start, control, end, (float)i / last);
            }

            _line.SetPositions(_points);
        }

        private void UpdateGaze()
        {
            if (_towed != null)
            {
                _rig.SetGazeTarget(this, _towed.transform.position, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_hovered != null)
            {
                _rig.SetGazeTarget(this, _hovered.transform.position, GazePriorities.Interest);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            if (_towed != null)
            {
                Release(false, false);
            }

            SetHovered(null);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }
    }
}
