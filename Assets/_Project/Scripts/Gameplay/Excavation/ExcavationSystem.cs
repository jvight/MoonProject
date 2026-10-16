using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tractor beam. Holding Excavate within reach of a relic half-sunk in a salvage site's heart asks 07 to ease
    /// to a stop (<see cref="IRoverRig.SetHoldStill"/>); once it is slow enough a soft cone of light reaches from its
    /// eye, a little dust swirls over the heart and the relic rises out of the dust toward the eye: a short, gentle
    /// dig. Letting go keeps the progress (the relic waits, bobbing, where it is). When it is fully up it is handed to
    /// physics with a little hop: <see cref="ExcavationStopped"/> (completed) then <see cref="RelicSurfaced"/>, and the
    /// game saves. The hold is shared with salvage: while <see cref="SalvageField"/> has a piece picked (07 looks at it
    /// more directly than at the heart) or is cutting, there is nothing to dig; nor while a sealed container's glowing
    /// seal is in reach (<see cref="SealedContainers"/>, 0.5.5 §2): one press, one verb, the seal first.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExcavationSystem : MonoBehaviour
    {
        /// <summary>Once lifting, the beam tolerates this much more speed before letting go (no flicker).</summary>
        private const float EngageHysteresis = 1.5f;

        [Tooltip("Tractor beam tuning (Assets/_Project/Data/Tuning/Gameplay/ExcavationTuning.asset).")]
        [SerializeField] private ExcavationTuning _tuning;

        private EventBus _events;
        private InputReader _input;
        private BeamWork _beamWork;
        private IRoverState _rover;
        private IRoverRig _rig;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RelicField _relics;
        private SalvageField _salvage;
        private SealedContainers _containers;
        private Transform _beam;
        private GlowRenderer _beamGlow;
        private DustSwirl _dust;
        private Relic _lifting;
        private Vector3 _segmentFrom;
        private float _segmentStart;
        private CutState _dig;
        private IDisposable _stowed;
        private IDisposable _deposited;
        private float _spin;
        private float _beamLevel;
        private Vector3 _beamTarget;
        private bool _holding;
        private bool _gazing;
        private bool _initialized;

        /// <summary>
        /// The relic close enough to lift right now (null when none): what the Excavate prompt points at.
        /// </summary>
        public Relic Candidate { get; private set; }

        /// <summary>The relic currently rising under the beam, or null.</summary>
        public Relic Lifting => _lifting;

        public ExcavationTuning Tuning => _tuning;

        /// <summary>Current beam brightness (tests and debugging views).</summary>
        public float BeamLevel => _beamLevel;

        internal DustSwirl Dust => _dust;

        internal void Wire(ExcavationTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics, SalvageField salvage,
            SealedContainers containers)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(ExcavationSystem)}: ExcavationTuning is not assigned.", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _beamWork = services.Beam;
            _dig = new CutState(services.Events, FeelKind.Dig, _tuning.ChargeSeconds, _tuning.ResumeChargeSeconds,
                _tuning.DigSteps);
            _stowed = services.Events.Subscribe<RelicStowed>(stowed => Reward(stowed.Position));
            _deposited = services.Events.Subscribe<RelicDeposited>(deposited => Reward(deposited.Position));
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _save = services.Save;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
            MeshRenderer beam = GlowObject.Create("TractorBeam", transform, services.Meshes.Cone,
                services.Visuals.TractorBeam);
            _beam = beam.transform;
            _beamGlow = new GlowRenderer(beam);
            _dust = new DustSwirl(transform, _tuning, services.Visuals.Dust);
            _initialized = true;
            return true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Candidate = _salvage.ClaimsHold || _containers.Candidate != null ? null : NearestLiftable();
            bool wantsBeam = _input.ExcavateHeld && Candidate != null && (_lifting != null || _beamWork.MayStart);
            SetHold(wantsBeam);

            float engageSpeed = _lifting != null ? _tuning.EngageSpeed * EngageHysteresis : _tuning.EngageSpeed;
            bool engaged = wantsBeam && _rover.Speed <= engageSpeed;
            if (_lifting != null && (!engaged || Candidate != _lifting))
            {
                StopLift(true);
            }

            if (engaged && _lifting == null)
            {
                StartLift(Candidate);
            }

            if (_lifting != null)
            {
                // The battery draws only from ignition on: a charge-up costs nothing.
                if (_dig.Draws)
                {
                    StepLift(_lifting, deltaTime);
                    _beamWork.Work(deltaTime);
                }
                else if (_dig.Charge(deltaTime, _lifting.Site.Position, Vector3.up))
                {
                    _events.Publish(new ExcavationStarted(_lifting.Site.Position));
                }
            }

            UpdateBeam(wantsBeam, deltaTime);
            UpdateGaze(wantsBeam);
        }

        /// <summary>
        /// The relic the beam would lift if salvage did not claim the hold (null when none in reach): the one being
        /// lifted while it stays within the hysteresis, else the nearest liftable one within reach.
        /// </summary>
        internal Relic NearestLiftable()
        {
            Vector3 rover = _rover.Position;
            float reach = _tuning.ReachRadius;
            if (_lifting != null && _lifting.CanBeLifted &&
                SurfaceRules.HorizontalDistance(rover, _lifting.Site.Position) <= reach * _tuning.ReachHysteresis)
            {
                return _lifting;
            }

            Relic nearest = null;
            float best = reach * reach;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (!relic.CanBeLifted || !relic.Home.HeartRevealed)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistanceSquared(rover, relic.Site.Position);
                if (distance <= best)
                {
                    best = distance;
                    nearest = relic;
                }
            }

            return nearest;
        }

        private void StartLift(Relic relic)
        {
            relic.BeginLift();
            _lifting = relic;
            _segmentStart = relic.Progress;
            _segmentFrom = relic.transform.position;
            _spin = relic.transform.eulerAngles.y;
            _dig.Engage(relic.Progress, relic.Site.Position, Vector3.up);
        }

        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        private void StopLift(bool announce)
        {
            bool lit = _dig.Draws;
            _lifting.PauseLift();
            if (announce)
            {
                _dig.Release(_lifting.Progress, _lifting.transform.position, Vector3.up);
                if (lit)
                {
                    _events.Publish(new ExcavationStopped(_lifting.Site.Position, false));
                }
            }

            _lifting = null;
        }

        private void StepLift(Relic relic, float deltaTime)
        {
            float duration = _tuning.DurationFor(relic.Definition.Mass);
            float before = relic.Progress;
            float progress = ExcavationRise.Advance(before, deltaTime, duration);
            float segment = ExcavationRise.Segment(progress, _segmentStart);
            Vector3 eye = _rig.TetherOrigin.position;
            Vector3 present = ExcavationRise.Ahead(eye, _rover.Rotation * Vector3.forward, _tuning.PresentDistance);
            present.y = _terrain.SampleHeight(present.x, present.z) + _tuning.PresentHeight;
            present = ClearOfWrecks(eye, present, relic.Radius);
            Vector3 position = ExcavationRise.Position(_segmentFrom, present, segment);
            _spin += _tuning.RiseSpin * deltaTime;
            relic.SetLiftPose(position, Quaternion.Euler(0f, _spin, 0f), progress);
            if (_dig.Advance(before, progress, position, Vector3.up))
            {
                Complete(relic);
            }
        }

        /// <summary>
        /// Where the relic comes up: the point ahead of 07's eye, or short of whatever stands in between (a heart lies
        /// inside its wreck, and the relic must surface where 07 sees it, never inside a wall).
        /// </summary>
        private static Vector3 ClearOfWrecks(Vector3 eye, Vector3 present, float radius)
        {
            Vector3 toPresent = present - eye;
            float distance = toPresent.magnitude;
            if (distance < 1e-3f)
            {
                return present;
            }

            Vector3 direction = toPresent / distance;
            return Physics.SphereCast(eye, radius, direction, out RaycastHit hit, distance, Layers.PropMask,
                QueryTriggerInteraction.Ignore)
                ? eye + direction * hit.distance
                : present;
        }

        private void Complete(Relic relic)
        {
            Vector3 hop = Vector3.up * _tuning.SurfaceHop + new Vector3(_rover.Velocity.x, 0f, _rover.Velocity.z);
            relic.Surface(hop, Vector3.up * (_tuning.SurfaceSpin * Mathf.Deg2Rad));
            // The dig's only Follow is the relic's hop out, a first Follow (strength 0), not a fold.
            _events.Publish(new FeelBeat(FeelKind.Dig, FeelPhase.Follow, relic.transform.position, Vector3.up, 0f,
                0f));
            _lifting = null;
            _events.Publish(new ExcavationStopped(relic.Site.Position, true));
            _events.Publish(new RelicSurfaced(relic.transform.position, relic.Definition.Id));
            SetHold(false);
            _save.SaveNow();
        }

        private void UpdateBeam(bool wantsBeam, float deltaTime)
        {
            bool lit = _lifting != null && _dig.Draws;
            float target = lit ? _tuning.BeamIntensity
                : wantsBeam ? _tuning.ReachingIntensity
                : 0f;
            float timeConstant = target > _beamLevel ? _tuning.BeamFadeIn : _tuning.BeamFadeOut;
            _beamLevel = Damp.Toward(_beamLevel, target, timeConstant, deltaTime);
            if (_lifting != null)
            {
                _beamTarget = _lifting.transform.position;
            }
            else if (Candidate != null)
            {
                _beamTarget = Candidate.Site.Position;
            }

            _beamGlow.Apply(_beamLevel);
            if (_beamGlow.Renderer.enabled)
            {
                Vector3 scoop = _rig.ScoopOrigin.position;
                Vector3 toTarget = _beamTarget - scoop;
                float length = toTarget.magnitude;
                if (length > 1e-3f)
                {
                    _beam.SetPositionAndRotation(scoop, Quaternion.LookRotation(toTarget / length));
                    _beam.localScale = new Vector3(_tuning.BeamRadius, _tuning.BeamRadius, length);
                }
            }

            float dust = lit ? _beamLevel / Mathf.Max(0.01f, _tuning.BeamIntensity) : 0f;
            _dust.Emit(_lifting != null ? _lifting.Site.Position : _dust.Position, dust);
        }

        private void UpdateGaze(bool wantsBeam)
        {
            if (wantsBeam || _lifting != null)
            {
                Vector3 look = _lifting != null ? _lifting.transform.position : Candidate.Site.Position;
                _rig.SetGazeTarget(this, look, GazePriorities.Focus);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void SetHold(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            if (_lifting != null)
            {
                StopLift(false);
            }

            SetHold(false);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDestroy()
        {
            _stowed?.Dispose();
            _deposited?.Dispose();
        }

        /// <summary>A dug relic reached 07's rack or the shelf: the dig's closing Reward beat.</summary>
        private void Reward(Vector3 position)
        {
            _events.Publish(new FeelBeat(FeelKind.Dig, FeelPhase.Reward, position, Vector3.up, 1f, 0f));
        }
    }
}
