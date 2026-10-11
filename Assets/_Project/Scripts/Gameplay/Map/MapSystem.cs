using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Map;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07), a system of its own initialised right after <see cref="GameplaySystem"/>:
    /// builds the place list from Gameplay's parts, registers <see cref="IMapKnowledge"/> and the map's save section,
    /// marks places seen and visited, and plays 07's time on the deck (<see cref="DeckLook"/>): the lookout's sweep
    /// with its faint blinks, the hatch swinging open on the first ride, the look at the map
    /// (<see cref="MapLookStarted"/>, <see cref="MapLookEnded"/>) and the chart pen's strokes
    /// (<see cref="MapInkCued"/>). While it holds the deck, the lift leaves look input to it and rides down once it lets
    /// the deck go. Nothing allocates per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSystem : MonoBehaviour, IGameSystem
    {
        [Tooltip("The gameplay system whose parts the map reads (initialised before this one).")]
        [SerializeField] private GameplaySystem _gameplay;

        [Tooltip("The map's tuning (Assets/_Project/Data/Tuning/Gameplay/MapTuning.asset).")]
        [SerializeField] private MapTuning _tuning;

        [Tooltip("Soft glow of the lookout sweep's blinks (GameplayVisuals' flash material).")]
        [SerializeField] private Material _blinkMaterial;

        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IRoverStillness _stillness;
        private IViewCamera _view;
        private LiftStation _lift;
        private MapKnowledge _knowledge;
        private DeckLook _deck;
        private LookoutSweep _sweep;
        private ChartPen _pen;
        private ISightline _sightline;
        private GlowMeshSet _meshes;
        private GlowFlashPool _blinks;
        private IDisposable _saveToken;
        private float _nextPoll;
        private bool _caughtUp;
        private bool _gazing;
        private bool _initialized;

        /// <summary>What the station remembers (tests and capture sessions).</summary>
        public MapKnowledge Knowledge => _knowledge;

        /// <summary>Where 07's time on the deck is (tests and capture sessions).</summary>
        public DeckLookPhase Phase => _deck != null ? _deck.Phase : DeckLookPhase.Off;

        internal void Wire(GameplaySystem gameplay, MapTuning tuning, Material blinkMaterial)
        {
            _gameplay = gameplay;
            _tuning = tuning;
            _blinkMaterial = blinkMaterial;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string problem = _gameplay == null ? "GameplaySystem is not assigned."
                : _tuning == null ? "MapTuning is not assigned."
                : _blinkMaterial == null ? "the blink material is not assigned."
                : !_gameplay.isActiveAndEnabled || _gameplay.Lift == null || _gameplay.Relays.Reach == null
                    || _gameplay.Blueprints == null ? "GameplaySystem did not initialise (see its error)."
                : null;
            if (problem != null)
            {
                Debug.LogError($"{nameof(MapSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            _events = context.Events;
            _input = context.Input;
            _rover = context.Get<IRoverState>();
            _rig = context.Get<IRoverRig>();
            _stillness = context.Get<IRoverStillness>();
            _view = context.Get<IViewCamera>();
            _lift = _gameplay.Lift;
            var places = new List<MapPlace>();
            var probes = new List<MapPlaceProbe>();
            try
            {
                MapPlaces.Build(_tuning, context.Get<IWorldAnchors>(), _gameplay.Relays.Reach, _gameplay.Salvage.Sites,
                    _gameplay.Friends.Friends, _gameplay.Blueprints, _gameplay.Progression.Tuning.Tubes, places,
                    probes);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(MapSystem)}: {exception.Message}", this);
                enabled = false;
                return;
            }

            _knowledge = new MapKnowledge(_events, _gameplay.Relays.Reach, places, probes);
            _deck = new DeckLook(_tuning);
            _sweep = new LookoutSweep();
            _pen = new ChartPen(_events, _knowledge);
            _sightline = context.Get<ISightline>();
            _meshes = new GlowMeshSet();
            _blinks = new GlowFlashPool(transform, _meshes.Sphere, _blinkMaterial, _tuning.BlinkPool,
                _tuning.BlinkSeconds, _tuning.BlinkRadius, _tuning.BlinkIntensity);
            _lift.HoldDeckWith(() => _deck.HoldsDeck);
            context.Register<IMapKnowledge>(_knowledge);
            _saveToken = context.Get<ISaveService>().Register(new SaveSection<MapSaveData>(GameplaySaveKeys.Map,
                GameplaySaveKeys.MapVersion, () => _knowledge.Capture(_deck.HatchOpen), Restore));
            _initialized = true;
        }

        private void Restore(MapSaveData data)
        {
            _knowledge.Restore(data);
            _deck.Restore(data.hatchOpen);
        }

        /// <summary>The save is loaded: Art shows a hatch the save already opened.</summary>
        private void Start()
        {
            if (_initialized && _deck.HatchOpen)
            {
                _events.Publish(new MapHatchOpened(HatchWindow(), true));
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            bool readState = !_caughtUp || now >= _nextPoll;
            if (readState)
            {
                _nextPoll = now + _tuning.PollSeconds;
            }

            _knowledge.Poll(_rover.Position, _tuning.VisitMargin, readState, _caughtUp);
            _caughtUp = true;
            StepDeck(now);
            _blinks.Tick(Time.deltaTime);
        }

        private void StepDeck(float now)
        {
            float deadzone = _tuning.LeaveDeadzone;
            var input = new DeckLookInput(_lift.AtDeck, _sweep.IsFinished(now), LookingAtHatch(), _stillness.StillSeconds,
                _input.Drive.sqrMagnitude >= deadzone * deadzone, now);
            switch (_deck.Step(input))
            {
                case DeckLookStep.SweepStarted:
                    Vector3 top = _lift.TopSpot;
                    _sweep.Begin(_knowledge, _sightline, top + Vector3.up * _tuning.LookoutEyeHeight,
                        Flat(top - _lift.LanderPosition), !_deck.HatchOpen, _tuning, now, _tuning.SweepDelay);
                    break;
                case DeckLookStep.HatchOpened:
                    _events.Publish(new MapHatchOpened(HatchWindow(), false));
                    Gaze(HatchWindow());
                    break;
                case DeckLookStep.LookStarted:
                    _events.Publish(new MapLookStarted(HatchWindow(), _deck.FirstLook));
                    _pen.Begin(now, _tuning.InkLead);
                    Gaze(HatchWindow());
                    break;
                case DeckLookStep.LookEnded:
                    _pen.Stop();
                    _events.Publish(new MapLookEnded());
                    ClearGaze();
                    break;
                case DeckLookStep.Released:
                    _sweep.Stop();
                    ClearGaze();
                    break;
            }

            if (_deck.Phase == DeckLookPhase.Sweeping)
            {
                StepSweep(now);
            }
            else if (_deck.Phase == DeckLookPhase.Looking)
            {
                _pen.Tick(now, _tuning.InkSeconds);
            }
        }

        /// <summary>The next place in the sweep blinks faintly, is pencilled as seen and 07's head turns to it.</summary>
        private void StepSweep(float now)
        {
            int index = _sweep.Tick(now, _tuning.SweepInterval);
            if (index < 0)
            {
                return;
            }

            Vector3 light = _knowledge.GetPlace(index).Position + Vector3.up * _tuning.LightHeight;
            _blinks.Spawn(light);
            _knowledge.MarkSeen(index);
            Gaze(light);
        }

        /// <summary>First ride: the view's centre rests on the hatch window.</summary>
        private bool LookingAtHatch()
        {
            if (_deck.Phase != DeckLookPhase.Breathing)
            {
                return false;
            }

            Transform eye = _view.Camera.transform;
            return Vector3.Angle(eye.forward, HatchWindow() - eye.position) <= _tuning.HatchLookAngle;
        }

        /// <summary>
        /// The hatch window: in from the lift's top stop toward the lander, at 07's lens height (a stand-in until the
        /// hatch model carries its own window node).
        /// </summary>
        private Vector3 HatchWindow()
        {
            Vector3 top = _lift.TopSpot;
            return top - Flat(top - _lift.LanderPosition) * _tuning.HatchInset + Vector3.up * _tuning.HatchHeight;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.forward;
        }

        private void Gaze(Vector3 target)
        {
            _rig.SetGazeTarget(this, target, GazePriorities.Focus);
            _gazing = true;
        }

        private void ClearGaze()
        {
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDisable()
        {
            if (_initialized)
            {
                ClearGaze();
            }
        }

        private void OnDestroy()
        {
            _saveToken?.Dispose();
            _meshes?.Dispose();
        }
    }
}
