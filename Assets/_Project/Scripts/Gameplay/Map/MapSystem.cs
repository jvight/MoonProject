using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07), a system of its own initialised right after <see cref="GameplaySystem"/>:
    /// builds the place list from Gameplay's parts, registers <see cref="IMapKnowledge"/> and the map's save section,
    /// and marks places seen and visited. <see cref="OpenLook"/> and <see cref="CloseLook"/> are the look's generic
    /// trigger (where the map hangs and what opens it come with the lander cabin's spec): opening publishes
    /// <see cref="MapLookStarted"/> and lets the chart pen ink what is new (<see cref="MapInkCued"/>), closing
    /// publishes <see cref="MapLookEnded"/>. Nothing allocates per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSystem : MonoBehaviour, IGameSystem
    {
        [Tooltip("The gameplay system whose parts the map reads (initialised before this one).")]
        [SerializeField] private GameplaySystem _gameplay;

        [Tooltip("The map's tuning (Assets/_Project/Data/Tuning/Gameplay/MapTuning.asset).")]
        [SerializeField] private MapTuning _tuning;

        private EventBus _events;
        private IRoverState _rover;
        private MapKnowledge _knowledge;
        private ChartPen _pen;
        private IDisposable _saveToken;
        private float _nextPoll;
        private bool _caughtUp;
        private bool _looked;
        private bool _initialized;

        /// <summary>What the station remembers (tests and capture sessions).</summary>
        public MapKnowledge Knowledge => _knowledge;

        /// <summary>The map's view is open.</summary>
        public bool IsLooking { get; private set; }

        internal void Wire(GameplaySystem gameplay, MapTuning tuning)
        {
            _gameplay = gameplay;
            _tuning = tuning;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            string problem = _gameplay == null ? "GameplaySystem is not assigned."
                : _tuning == null ? "MapTuning is not assigned."
                : !_gameplay.enabled || _gameplay.Relays.Reach == null || _gameplay.Blueprints == null
                    ? "GameplaySystem did not initialise (see its error)."
                : null;
            if (problem != null)
            {
                Debug.LogError($"{nameof(MapSystem)}: {problem}", this);
                enabled = false;
                return;
            }

            _events = context.Events;
            _rover = context.Get<IRoverState>();
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
            _pen = new ChartPen(_events, _knowledge);
            context.Register<IMapKnowledge>(_knowledge);
            _saveToken = context.Get<ISaveService>().Register(new SaveSection<MapSaveData>(GameplaySaveKeys.Map,
                GameplaySaveKeys.MapVersion, () => _knowledge.Capture(_looked), Restore));
            _initialized = true;
        }

        /// <summary>
        /// Opens the view on the map at <paramref name="position"/>: UI shows it and, after the view's ease, the chart
        /// pen inks every place inked since the last look, home first. Does nothing while the view is open.
        /// </summary>
        internal void OpenLook(Vector3 position)
        {
            if (!_initialized || IsLooking)
            {
                return;
            }

            IsLooking = true;
            _events.Publish(new MapLookStarted(position, !_looked));
            _looked = true;
            _pen.Begin(Time.time, _tuning.InkLead);
        }

        /// <summary>Closes the view; what the pen has not drawn yet waits for the next look.</summary>
        internal void CloseLook()
        {
            if (!IsLooking)
            {
                return;
            }

            IsLooking = false;
            _pen.Stop();
            _events.Publish(new MapLookEnded());
        }

        private void Restore(MapSaveData data)
        {
            _knowledge.Restore(data);
            _looked = data.looked;
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
            if (IsLooking)
            {
                _pen.Tick(now, _tuning.InkSeconds);
            }
        }

        private void OnDestroy()
        {
            _saveToken?.Dispose();
        }
    }
}
