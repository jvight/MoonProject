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
    /// fitted and empty, the press lifts an aimed relic into the rack instead (<see cref="CargoCradle"/>); facing the
    /// lab's keepsake shelf it places the relic that rode up the belly lift, or the one in the rack, and the belly lift
    /// takes a towed relic aboard (<see cref="HandOver"/>). 07 looks at what it aims at and fixes on what it tows.
    /// Asleep until Kenji's garage port fits 07's spare ring (<see cref="RoverAbility.Tether"/>, first-hour v2 §3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TetherSystem : MonoBehaviour, ITetherAim
    {
        private const int OcclusionMask = Layers.GroundMask | Layers.PropMask;

        [Tooltip("Tether tuning (Assets/_Project/Data/Tuning/Gameplay/TetherTuning.asset).")]
        [SerializeField] private TetherTuning _tuning;

        private EventBus _events;
        private InputReader _input;
        private IRoverAbilities _abilities;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ITerrainQuery _terrain;
        private ITowable[] _towables = Array.Empty<ITowable>();
        private CargoCradle _cradle;
        private HomeBase _home;
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
        private ITowable _reaching;
        private float _reachLeft;
        private int _tensionStep;
        private bool _retracting;
        private Vector3 _beamEnd;
        private float _savedLinearDamping;
        private float _savedAngularDamping;
        private bool _gazing;
        private bool _initialized;

        public TetherAimState State => !_initialized || !_abilities.Has(RoverAbility.Tether) ? TetherAimState.Asleep
            : _towed != null ? TetherAimState.Towing
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

        /// <summary>
        /// Times the player pressed Tether before the ring was fitted: the moment 07 says it has no tether (a counter,
        /// so a reader in any update order sees each press).
        /// </summary>
        public int AsleepPresses { get; private set; }

        /// <summary>Current beam brightness (tests and debugging views).</summary>
        public float BeamIntensity => _glow != null ? _glow.Intensity : 0f;

        /// <summary>Current upward arc fraction of the tether beam (pulls taut toward 0 as strain builds).</summary>
        public float CurrentArc { get; private set; }

        internal void Wire(TetherTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics, SalvageField salvage,
            IRoverAbilities abilities)
        {
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
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
        /// The Cargo Cradle and the lab's keepsake shelf share the Tether press (connected once all are initialised);
        /// the tether runs from then on.
        /// </summary>
        internal void Connect(CargoCradle cradle, HomeBase home)
        {
            _cradle = cradle != null ? cradle : throw new ArgumentNullException(nameof(cradle));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
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

            // Asleep until Kenji's garage port fits the spare ring (first-hour v2 §3): nothing to hold with.
            if (!_abilities.Has(RoverAbility.Tether))
            {
                _armed = false;
                if (_input.TetherHeld && !_wasHeld)
                {
                    AsleepPresses++;
                }

                _wasHeld = _input.TetherHeld;
                SetHovered(null);
                DrawBeam(Time.deltaTime);
                return;
            }

            // Arm on the held edge rather than the one-frame "pressed" signal, so a hitch never eats a press.
            bool held = _input.TetherHeld;
            if (held && !_wasHeld)
            {
                // Facing the lab's keepsake shelf, the press places the relic that rode up (or the one in the cradle)
                // rather than reaching for another.
                _armed = !_home.TryPlace() && !_cradle.TryUnload();
            }

            _wasHeld = held;
            if (!held)
            {
                _armed = false;
                if (_towed != null)
                {
                    Release(false, true, false);
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
                        Reach(_hovered, Time.deltaTime);
                    }
                }
                else
                {
                    CancelReach();
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
            Vector3 anchor = _rig.WinchOrigin.position;
            Vector3 position = body.position;
            float ground = _terrain.SampleHeight(position.x, position.z);
            float ceiling = _rover.Position.y + _tuning.MaxLiftHeight;
            Vector3 target = TetherPhysics.Target(anchor, position, _winch.Length, ground, _tuning.HoverHeight,
                ceiling);
            body.AddForce(TetherPhysics.Force(position, body.linearVelocity, target, _rover.Velocity, body.mass,
                -Physics.gravity.y, _tuning));
            float distance = Vector3.Distance(position, anchor);
            bool snapped = _snap.Step(distance, _winch.Length, Time.fixedDeltaTime);
            StepTension(_snap.Strain, body.position);
            if (snapped)
            {
                Release(true, true, false);
            }
            else if (_towed.WantsRelease)
            {
                // Done: the held button must not latch straight back on.
                _armed = false;
                Release(false, true, true);
            }
        }

        /// <summary>
        /// The belly lift takes the towed relic aboard (docs/features/M3-19 §4): the tether lets it go softly and the
        /// held button must not latch onto anything else. Returns the relic, or null when nothing (or a salvage piece)
        /// is towed.
        /// </summary>
        internal Relic HandOver()
        {
            if (!(_towed is Relic relic))
            {
                return null;
            }

            _armed = false;
            Release(false, true, true);
            return relic;
        }

        private ITowable FindTarget()
        {
            Transform view = _view.Camera.transform;
            Vector3 eye = view.position;
            Vector3 forward = view.forward;

            // Aimed from the view, while the tether is anchored at 07's winch (and what 07 can see from its eye or
            // winch is in reach).
            Vector3 beamOrigin = _rig.WinchOrigin.position;
            Vector3 sightOrigin = _rig.TetherOrigin != null ? _rig.TetherOrigin.position : beamOrigin;
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
                        out float score) && score < bestScore
                    && (InSight(beamOrigin, position, towable.Radius) || InSight(sightOrigin, position, towable.Radius)))
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

        /// <summary>
        /// The held button reaches for <paramref name="towable"/> (feel-pass §2.5): a short reach (Anticipate, the
        /// pulse running out to it), then the latch.
        /// </summary>
        private void Reach(ITowable towable, float deltaTime)
        {
            if (_reaching != towable)
            {
                CancelReach();
                _reaching = towable;
                _reachLeft = _tuning.ReachSeconds;
                Publish(FeelPhase.Anticipate, towable.Position, 0f, _tuning.ReachSeconds);
            }

            _reachLeft -= deltaTime;
            if (_reachLeft <= 0f)
            {
                _reaching = null;
                Attach(towable);
            }
        }

        /// <summary>A reach let go of (or whose target slipped away) before it latched.</summary>
        private void CancelReach()
        {
            if (_reaching == null)
            {
                return;
            }

            Publish(FeelPhase.Cancel, _reaching.Position, 0f, 0f);
            _reaching = null;
        }

        /// <summary>An Act beat each time the tether's tension crosses one of its steps (the hum, the rumble).</summary>
        private void StepTension(float tension, Vector3 at)
        {
            float clamped = Mathf.Clamp01(tension);
            int step = Mathf.FloorToInt(clamped * _tuning.TensionSteps);
            if (step == _tensionStep)
            {
                return;
            }

            _tensionStep = step;
            Publish(FeelPhase.Act, at, clamped, 0f);
        }

        private void Publish(FeelPhase phase, Vector3 at, float strength, float seconds)
        {
            _events.Publish(new FeelBeat(FeelKind.Tether, phase, at, Vector3.up, strength, seconds));
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
            _winch.Reset(Vector3.Distance(body.position, _rig.WinchOrigin.position));
            _snap.Reset();
            _reach = 0f;
            _retracting = false;
            _events.Publish(new TetherAttached(body.position, body.mass));
            _tensionStep = 0;
            float weight = Mathf.Clamp01(body.mass / _tuning.HeavyMass);
            Publish(FeelPhase.Impact, body.position, weight, 0f);
            Publish(FeelPhase.Follow, body.position, weight, _tuning.LiftSeconds);
        }

        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        /// <param name="delivered">The tow ended where it was going (the shelf, the rack): Reward, not Cancel.</param>
        private void Release(bool snapped, bool announce, bool delivered)
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
                Publish(delivered ? FeelPhase.Reward : FeelPhase.Cancel, _beamEnd, snapped ? 1f : 0f, 0f);
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

            float strain = Strain;
            float baseIntensity = _towed != null ? _tuning.BeamIntensity : _tuning.BeamIntensity * Ease.InOutSine(_reach);
            float intensity = baseIntensity + (_towed != null ? strain * _tuning.StrainIntensityBoost : 0f);
            _glow.Apply(_retracting || _towed != null ? intensity : 0f);
            if (!_line.enabled)
            {
                CurrentArc = 0f;
                return;
            }

            Vector3 start = _rig.WinchOrigin.position;
            Vector3 end = Vector3.LerpUnclamped(start, _beamEnd, Ease.OutCubic(_reach));
            float arc = _tuning.BeamArc * (1f - strain * _tuning.StrainTighten);
            CurrentArc = arc;
            float wobble = _tuning.Wobble + strain * _tuning.StrainWobble;
            float frequency = _tuning.WobbleFrequency * (1f + strain * _tuning.StrainFrequencyBoost);
            Vector3 control = TetherBeamShape.Control(start, end, arc, wobble, frequency, Time.time);
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
                Release(false, false, false);
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
