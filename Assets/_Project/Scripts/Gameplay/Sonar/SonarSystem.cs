using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The sonar ping. A press (remembered briefly if the calm cooldown is still running) publishes
    /// <see cref="SonarPinged"/> and rolls a ring over the ground; every relic still out in the world within range
    /// answers when the ring has touched it (closer ones sooner and brighter), publishing <see cref="RelicAnswered"/>
    /// and raising a light pillar on the horizon that stands for a while. 07 turns to the nearest answer, then keeps
    /// glancing at the nearest standing pillar now and then.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SonarSystem : MonoBehaviour
    {
        [Tooltip("Sonar tuning (Assets/_Project/Data/Tuning/Gameplay/SonarTuning.asset).")]
        [SerializeField] private SonarTuning _tuning;

        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private RelicField _relics;
        private SonarSchedule _schedule;
        private SonarRing[] _rings = Array.Empty<SonarRing>();
        private SiteMarker[] _markers = Array.Empty<SiteMarker>();
        private float _cooldownLeft;
        private bool _queued;
        private bool _firstAnswerPending;
        private float _gazeUntil = float.NegativeInfinity;
        private float _nextGlance;
        private bool _gazing;
        private bool _initialized;

        /// <summary>True when a press would ping right away.</summary>
        public bool IsReady => _initialized && _cooldownLeft <= 0f;

        /// <summary>Seconds until the next ping is possible.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _cooldownLeft);

        public SonarTuning Tuning => _tuning;

        /// <summary>The ring pool (tests and debug views read ring radius and brightness).</summary>
        internal SonarRing[] Rings => _rings;

        /// <summary>One marker per relic, by relic index.</summary>
        internal SiteMarker[] Markers => _markers;

        internal void Wire(SonarTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(SonarSystem)}: SonarTuning is not assigned.", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _schedule = new SonarSchedule(relics.Relics.Count);
            _rings = new SonarRing[_tuning.RingPoolSize];
            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i] = new SonarRing(transform, _tuning, services.Terrain, services.Visuals.SonarRing);
            }

            _markers = new SiteMarker[relics.Relics.Count];
            for (int i = 0; i < _markers.Length; i++)
            {
                _markers[i] = new SiteMarker("Marker_" + relics.Relics[i].Definition.Id, transform, _tuning,
                    services.Terrain, services.Visuals, services.Meshes.Pillar);
            }

            _initialized = true;
            return true;
        }

        /// <summary>After a load: discovered relics still in the ground show their breathing ring again.</summary>
        internal void RefreshDiscoveredSites()
        {
            for (int i = 0; i < _markers.Length; i++)
            {
                Relic relic = _relics.Relics[i];
                if (relic.Discovered && (relic.State == RelicState.Buried || relic.State == RelicState.Surfacing))
                {
                    _markers[i].Place(relic.Site.Position);
                }
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            if (_input.PingPressed && _cooldownLeft <= _tuning.InputBuffer)
            {
                _queued = true;
            }

            _cooldownLeft -= Time.deltaTime;
            if (_queued && _cooldownLeft <= 0f)
            {
                Ping(now);
            }

            while (_schedule.TryPop(now, out int relic, out float distance))
            {
                Answer(relic, distance, now);
            }

            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i].Tick(now);
            }

            for (int i = 0; i < _markers.Length; i++)
            {
                _markers[i].Tick(_relics.Relics[i], now);
            }

            UpdateGaze(now);
        }

        private void Ping(float now)
        {
            _queued = false;
            _cooldownLeft = _tuning.Cooldown;
            Vector3 origin = _rover.Position;
            _events.Publish(new SonarPinged(origin, _tuning.Range));
            OldestRing().Begin(origin, now);

            _firstAnswerPending = false;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (!relic.AnswersSonar)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(origin, relic.SonarPosition);
                _firstAnswerPending |= _schedule.Add(i, distance, now, _tuning.Range, _tuning.RingDuration,
                    _tuning.AnswerLag);
            }
        }

        private void Answer(int index, float distance, float now)
        {
            Relic relic = _relics.Relics[index];
            if (!relic.AnswersSonar)
            {
                return;
            }

            Vector3 position = relic.SonarPosition;
            relic.MarkDiscovered();
            _events.Publish(new RelicAnswered(position, distance));
            _markers[index].Answer(position, _tuning.BrightnessAt(distance), now);
            if (_firstAnswerPending)
            {
                // Answers arrive nearest first, so the first one of a ping is the closest relic.
                _firstAnswerPending = false;
                _rig.SetGazeTarget(this, _markers[index].Position, GazePriorities.Interest);
                _gazing = true;
                _gazeUntil = now + _tuning.AnswerGaze;
                _nextGlance = now + _tuning.GlanceInterval;
            }
        }

        private void UpdateGaze(float now)
        {
            if (now < _gazeUntil)
            {
                return;
            }

            if (now >= _nextGlance)
            {
                _nextGlance = now + _tuning.GlanceInterval;
                int nearest = NearestStandingPillar(now);
                if (nearest >= 0)
                {
                    _rig.SetGazeTarget(this, _markers[nearest].Position, GazePriorities.Interest);
                    _gazing = true;
                    _gazeUntil = now + _tuning.GlanceDuration;
                    return;
                }
            }

            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private int NearestStandingPillar(float now)
        {
            Vector3 rover = _rover.Position;
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < _markers.Length; i++)
            {
                if (!_markers[i].PillarStanding(now))
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistanceSquared(rover, _markers[i].Position);
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private SonarRing OldestRing()
        {
            SonarRing oldest = _rings[0];
            for (int i = 0; i < _rings.Length; i++)
            {
                if (!_rings[i].Active)
                {
                    return _rings[i];
                }

                if (_rings[i].StartTime < oldest.StartTime)
                {
                    oldest = _rings[i];
                }
            }

            return oldest;
        }

        private void OnDisable()
        {
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private void OnDestroy()
        {
            foreach (SonarRing ring in _rings)
            {
                ring.Dispose();
            }

            foreach (SiteMarker marker in _markers)
            {
                marker.Dispose();
            }
        }
    }
}
