using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew's map (docs/features/M3-07), a system of its own initialised right after <see cref="GameplaySystem"/>:
    /// builds the place list from Gameplay's parts, registers <see cref="IMapKnowledge"/>, the chart's chargers and
    /// Link line (<see cref="IChartOverlay"/>, docs/features/M3-21) and the map's save section, and marks places seen
    /// and visited. The map hangs on the lab's survey chart (docs/features/M3-19 §5): looking at
    /// the chart once the tower's third level has woken it (<see cref="LabStationLooked"/>) opens the view
    /// (<see cref="OpenLook"/>: <see cref="MapLookStarted"/>, then the chart pen inks what is new,
    /// <see cref="MapInkCued"/>); looking away or riding down closes it (<see cref="CloseLook"/>,
    /// <see cref="MapLookEnded"/>). The dock's evening (P6) sweeps what home can see: the places in view blink seen in
    /// turn (<see cref="MapKnowledge.MarkSeen"/>, <see cref="MapPlaceSeen"/>). Nothing allocates per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSystem : MonoBehaviour, IGameSystem, ISurveyChart
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
        private IDisposable[] _subscriptions = Array.Empty<IDisposable>();
        private ISightline _sightline;
        private readonly List<int> _sweep = new List<int>();
        private Comparison<int> _byBearing;
        private Vector3 _sweepFrom;
        private int _sweepNext;
        private float _sweepAt = float.PositiveInfinity;
        private float _nextPoll;
        private bool _caughtUp;
        private bool _looked;
        private bool _initialized;

        /// <summary>What the station remembers (tests and capture sessions).</summary>
        public MapKnowledge Knowledge => _knowledge;

        /// <summary>The map's view is open.</summary>
        public bool IsLooking { get; private set; }

        bool ISurveyChart.Looked => _looked;

        bool ISurveyChart.HasNews => _initialized && (!_looked || _knowledge.InkedSinceLastLook > 0);

        bool ISurveyChart.HasSeen(string placeId)
        {
            return _initialized && _knowledge.GetState(placeId) >= MapPlaceState.Seen;
        }

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
            _sightline = context.Get<ISightline>();
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
            context.Register<IChartOverlay>(new ChartOverlay(_gameplay.Relays.Reach, _gameplay.Relays,
                _gameplay.Home.Dock, _gameplay.Battery.Model, _gameplay.Battery.Tuning.MetresPerCell,
                _tuning.ChargerRangeShare, context.Get<IWorldLayout>().PeakPosition));
            _saveToken = context.Get<ISaveService>().Register(new SaveSection<MapSaveData>(GameplaySaveKeys.Map,
                GameplaySaveKeys.MapVersion, () => _knowledge.Capture(_looked), Restore));
            _byBearing = CompareBearing;
            _subscriptions = new[]
            {
                _events.Subscribe<LabStationLooked>(OnLabLooked),
                _events.Subscribe<LabLeft>(_ => CloseLook()),
                _events.Subscribe<DockEveningOpened>(OnEvening),
                _events.Subscribe<RoverDockChanged>(OnDock),
            };
            _gameplay.Lift.ConnectChart(this);
            _gameplay.Progression.ConnectChart(this);
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

        /// <summary>Looking at the survey chart opens the map once the tower's third level has woken it.</summary>
        private void OnLabLooked(LabStationLooked looked)
        {
            if (looked.Station != LabStation.Chart)
            {
                return;
            }

            if (!looked.Looking)
            {
                CloseLook();
            }
            else if (_gameplay.Power.HasReached(BasePowerStage.Chart))
            {
                OpenLook(_gameplay.Lift.StationPosition(LabStation.Chart));
            }
        }

        /// <summary>
        /// The dock's evening opened: every place still a sketch whose light home can see from the lab's porthole,
        /// within the evening's view, waits to blink seen in turn, sweeping round from the north.
        /// </summary>
        private void OnEvening(DockEveningOpened evening)
        {
            _sweep.Clear();
            _sweepFrom = evening.DockPosition;
            Vector3 eye = _sweepFrom + Vector3.up * _tuning.SweepEyeHeight;
            for (int i = 0; i < _knowledge.PlaceCount; i++)
            {
                MapPlace place = _knowledge.GetPlace(i);
                if (_knowledge.GetState(i) != MapPlaceState.Sketched || !_knowledge.IsVisitable(i) ||
                    SurfaceRules.HorizontalDistance(place.Position, _sweepFrom) > _tuning.SweepRange)
                {
                    continue;
                }

                Vector3 light = place.Position + Vector3.up * _tuning.SweepLightHeight;
                if (_sightline.IsClear(eye, light, place.Radius, _tuning.SightStep, _tuning.SightClearance))
                {
                    _sweep.Add(i);
                }
            }

            _sweep.Sort(_byBearing);
            _sweepNext = 0;
            _sweepAt = Time.time + _tuning.SweepDelay;
        }

        /// <summary>Leaving the dock lets the evening go: what has not blinked yet waits for the next one.</summary>
        private void OnDock(RoverDockChanged change)
        {
            if (!change.Docked)
            {
                _sweep.Clear();
                _sweepAt = float.PositiveInfinity;
            }
        }

        private int CompareBearing(int a, int b)
        {
            return Bearing(a).CompareTo(Bearing(b));
        }

        private float Bearing(int index)
        {
            Vector3 offset = _knowledge.GetPlace(index).Position - _sweepFrom;
            return Mathf.Repeat(Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg, 360f);
        }

        private void StepSweep(float now)
        {
            while (_sweepNext < _sweep.Count && now >= _sweepAt)
            {
                _knowledge.MarkSeen(_sweep[_sweepNext]);
                _sweepNext++;
                _sweepAt += _tuning.SweepGap;
            }
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

            StepSweep(now);
        }

        private void OnDestroy()
        {
            _saveToken?.Dispose();
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
