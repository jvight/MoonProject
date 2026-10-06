using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tractor beam. Holding Excavate within reach of a buried relic asks 07 to ease to a stop
    /// (<see cref="IRoverRig.SetHoldStill"/>); once it is slow enough a soft cone of light reaches from its eye, dust
    /// swirls over the site and the relic rises out of the ground toward the eye. Letting go keeps the progress (the
    /// relic waits, bobbing, where it is). When it is fully up it is handed to physics with a little hop:
    /// <see cref="ExcavationStopped"/> (completed) then <see cref="RelicSurfaced"/>, and the game saves.
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
        private IRoverState _rover;
        private IRoverRig _rig;
        private ITerrainQuery _terrain;
        private ISaveService _save;
        private RelicField _relics;
        private Transform _beam;
        private GlowRenderer _beamGlow;
        private DustSwirl _dust;
        private Relic _lifting;
        private Vector3 _segmentFrom;
        private float _segmentStart;
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

        internal bool Initialize(GameplayServices services, RelicField relics)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(ExcavationSystem)}: ExcavationTuning is not assigned.", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _save = services.Save;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
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
            Candidate = FindCandidate();
            bool wantsBeam = _input.ExcavateHeld && Candidate != null;
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
                StepLift(_lifting, deltaTime);
            }

            UpdateBeam(wantsBeam, deltaTime);
            UpdateGaze(wantsBeam);
        }

        private Relic FindCandidate()
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
                if (!relic.CanBeLifted)
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
            _events.Publish(new ExcavationStarted(relic.Site.Position));
        }

        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        private void StopLift(bool announce)
        {
            _lifting.PauseLift();
            if (announce)
            {
                _events.Publish(new ExcavationStopped(_lifting.Site.Position, false));
            }

            _lifting = null;
        }

        private void StepLift(Relic relic, float deltaTime)
        {
            float duration = _tuning.DurationFor(relic.Definition.Mass);
            float progress = ExcavationRise.Advance(relic.Progress, deltaTime, duration);
            float segment = ExcavationRise.Segment(progress, _segmentStart);
            Vector3 eye = _rig.TetherOrigin.position;
            Vector3 present = ExcavationRise.Ahead(eye, _rover.Rotation * Vector3.forward, _tuning.PresentDistance);
            present.y = _terrain.SampleHeight(present.x, present.z) + _tuning.PresentHeight;
            Vector3 position = ExcavationRise.Position(_segmentFrom, present, segment);
            _spin += _tuning.RiseSpin * deltaTime;
            relic.SetLiftPose(position, Quaternion.Euler(0f, _spin, 0f), progress);
            if (progress >= 1f)
            {
                Complete(relic);
            }
        }

        private void Complete(Relic relic)
        {
            Vector3 hop = Vector3.up * _tuning.SurfaceHop + new Vector3(_rover.Velocity.x, 0f, _rover.Velocity.z);
            relic.Surface(hop, Vector3.up * (_tuning.SurfaceSpin * Mathf.Deg2Rad));
            _lifting = null;
            _events.Publish(new ExcavationStopped(relic.Site.Position, true));
            _events.Publish(new RelicSurfaced(relic.transform.position, relic.Definition.Id));
            SetHold(false);
            _save.SaveNow();
        }

        private void UpdateBeam(bool wantsBeam, float deltaTime)
        {
            float target = _lifting != null ? _tuning.BeamIntensity
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
                Vector3 eye = _rig.TetherOrigin.position;
                Vector3 toTarget = _beamTarget - eye;
                float length = toTarget.magnitude;
                if (length > 1e-3f)
                {
                    _beam.SetPositionAndRotation(eye, Quaternion.LookRotation(toTarget / length));
                    _beam.localScale = new Vector3(_tuning.BeamRadius, _tuning.BeamRadius, length);
                }
            }

            float dust = _lifting != null ? _beamLevel / Mathf.Max(0.01f, _tuning.BeamIntensity) : 0f;
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
    }
}
