using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The energy tether. The most central towable inside a soft aim cone around the screen centre (a loose relic, or
    /// a salvage drag piece still hanging on its wreck) is highlighted and stays picked while it is roughly there;
    /// holding Tether latches a glowing beam from 07's eye onto it (<see cref="TetherAttached"/>). A PD spring floats
    /// it along behind 07 at the winch length, mass and all. Letting go releases it where it is; if it gets caught or
    /// falls far behind, the tether lets go softly by itself and it stays right there (<see cref="TetherReleased"/>,
    /// snapped). A drag piece pulled clear of its wreck is let go softly too (not snapped). With the Cargo Cradle
    /// fitted and empty, the press lifts an aimed relic into the rack instead (<see cref="CargoCradle"/>), and at the
    /// museum shelf it sets the carried relic down. 07 looks at what it aims at and fixes on what it tows.
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
        private ITowable[] _towables = Array.Empty<ITowable>();
        private CargoCradle _cradle;
        private WinchControl _winch;
        private TetherSnapRule _snap;
        private LineRenderer _line;
        private GlowRenderer _glow;
        private Vector3[] _points = Array.Empty<Vector3>();
        private ITowable _hovered;
        private ITowable _towed;
        private bool _armed;
        private bool _wasHeld;
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

        public Vector3 TargetPosition => _towed != null ? _towed.Position
            : _hovered != null ? _hovered.Position
            : Vector3.zero;

        public float Strain => _towed != null ? _snap.Strain : 0f;

        public float Length => _towed != null ? _winch.Length : 0f;

        /// <summary>The relic on the tether, or null (also null while it tows a drag piece).</summary>
        public Relic Towed => _towed as Relic;

        /// <summary>The highlighted relic a press would latch onto, or null (also null over a drag piece).</summary>
        public Relic Hovered => _hovered as Relic;

        /// <summary>Whatever is on the tether (a relic or a drag piece), or null.</summary>
        internal ITowable TowedBody => _towed;

        /// <summary>Whatever a press would latch onto (a relic or a drag piece), or null.</summary>
        internal ITowable HoveredBody => _hovered;

        public TetherTuning Tuning => _tuning;

        /// <summary>Current beam brightness (tests and debugging views).</summary>
        public float BeamIntensity => _glow != null ? _glow.Intensity : 0f;

        internal void Wire(TetherTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics, SalvageField salvage)
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
            if (relics == null)
            {
                throw new ArgumentNullException(nameof(relics));
            }

            if (salvage == null)
            {
                throw new ArgumentNullException(nameof(salvage));
            }

            _towables = new ITowable[relics.Relics.Count + salvage.Drags.Count];
            for (int i = 0; i < relics.Relics.Count; i++)
            {
                _towables[i] = relics.Relics[i];
            }

            for (int i = 0; i < salvage.Drags.Count; i++)
            {
                _towables[relics.Relics.Count + i] = salvage.Drags[i];
            }

            _winch = new WinchControl(_tuning.MinLength, _tuning.MaxLength, _tuning.WinchStep, _tuning.ReelSpeed,
                _tuning.WinchLead);
            _snap = new TetherSnapRule(_tuning.SnapStretch, _tuning.SnapGrace, _tuning.SnapDistance);
            _points = new Vector3[_tuning.BeamPoints];
            _line = CreateLine(services.Visuals.TetherBeam);
            _glow = new GlowRenderer(_line);
            return true;
        }

        /// <summary>
        /// The Cargo Cradle shares the Tether press (connected once both are initialised); the tether runs from then
        /// on.
        /// </summary>
        internal void Connect(CargoCradle cradle)
        {
            _cradle = cradle != null ? cradle : throw new ArgumentNullException(nameof(cradle));
            _initialized = true;
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

            // Arm on the held edge rather than the one-frame "pressed" signal, so a hitch never eats a press.
            bool held = _input.TetherHeld;
            if (held && !_wasHeld)
            {
                // At the shelf, the press sets down the relic riding in the cradle rather than reaching for another.
                _armed = !_cradle.TryUnload();
            }

            _wasHeld = held;
            if (!held)
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
                    if (_cradle.TryStow(_hovered))
                    {
                        // Into the rack instead of onto the tether; the held button must not grab anything else.
                        _armed = false;
                        SetHovered(null);
                    }
                    else
                    {
                        Attach(_hovered);
                    }
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
            else if (_towed.WantsRelease)
            {
                // Done: the held button must not latch straight back on.
                _armed = false;
                Release(false, true);
            }
        }

        private ITowable FindTarget()
        {
            Transform view = _view.Camera.transform;
            Vector3 eye = view.position;
            Vector3 forward = view.forward;

            // Aimed from the view, but the beam leaves 07's eye: what 07 can see is in reach (a relic surfacing under
            // a wreck's roof is hidden from the camera above, never from 07 beside it).
            Vector3 beamOrigin = _rig.TetherOrigin.position;
            ITowable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _towables.Length; i++)
            {
                ITowable towable = _towables[i];
                if (!towable.IsTetherable)
                {
                    continue;
                }

                float cone = towable == _hovered ? _tuning.StickyCone : _tuning.AimCone;
                Vector3 position = towable.Position;
                if (TetherAim.TryScore(eye, forward, position, cone, _tuning.AimRange, _tuning.DistanceWeight,
                        out float score) && score < bestScore && InSight(beamOrigin, position, towable.Radius))
                {
                    best = towable;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool InSight(Vector3 origin, Vector3 target, float radius)
        {
            Vector3 toTarget = target - origin;
            float distance = toTarget.magnitude - radius;
            return distance <= 0f || !Physics.Raycast(origin, toTarget.normalized, distance, OcclusionMask,
                QueryTriggerInteraction.Ignore);
        }

        private void SetHovered(ITowable towable)
        {
            if (towable == _hovered)
            {
                return;
            }

            if (_hovered != null)
            {
                _hovered.SetAimHighlight(0f);
            }

            _hovered = towable;
            if (_hovered != null)
            {
                _hovered.SetAimHighlight(_tuning.HoverHighlight);
            }
        }

        private void Attach(ITowable towable)
        {
            SetHovered(null);
            _towed = towable;
            towable.BeginTow();
            towable.SetAimHighlight(_tuning.TowHighlight);
            Rigidbody body = towable.Body;
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
            ITowable towable = _towed;
            Rigidbody body = towable.Body;
            body.linearDamping = _savedLinearDamping;
            body.angularDamping = _savedAngularDamping;
            if (snapped)
            {
                body.linearVelocity *= _tuning.SnapCarry;
                body.angularVelocity *= _tuning.SnapCarry;
                _armed = false;
            }

            towable.EndTow();
            towable.SetAimHighlight(0f);
            _towed = null;
            _beamEnd = towable.Position;
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
                _beamEnd = _towed.Position;
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
                _rig.SetGazeTarget(this, _towed.Position, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_hovered != null)
            {
                _rig.SetGazeTarget(this, _hovered.Position, GazePriorities.Interest);
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
