using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The sonar ping. A press (remembered briefly if the calm cooldown is still running) publishes
    /// <see cref="SonarPinged"/> and rolls a ring over the ground; every salvage site with something left to find (a
    /// piece to cut or a relic in its heart) answers when the ring has touched it (closer ones sooner and brighter),
    /// publishing <see cref="SiteAnswered"/> and raising a light pillar on the horizon that stands for a while. A relic
    /// lying loose out in the world answers on its own (<see cref="RelicAnswered"/>), and a broken friend with its
    /// broken chirp (<see cref="FriendAnswered"/>) and a warm pillar. 07 turns to the nearest answer, then keeps
    /// glancing at the nearest standing pillar now and then. A spotter friend can also reveal a site softly. With the
    /// Warm Headlamp fitted, a site's or relic's pillar 07 is heading toward stands a little longer.
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
        private FriendField _friends;
        private SalvageField _salvage;
        private IRoverAbilities _abilities;
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

        /// <summary>One marker per relic (by relic index), then one per friend, then one per salvage site.</summary>
        internal SiteMarker[] Markers => _markers;

        internal void Wire(SonarTuning tuning)
        {
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services, RelicField relics, FriendField friends,
            SalvageField salvage, IRoverAbilities abilities)
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
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            int relicCount = relics.Relics.Count;
            int answerers = relicCount + friends.Count + salvage.Sites.Count;
            _schedule = new SonarSchedule(answerers);
            _rings = new SonarRing[_tuning.RingPoolSize];
            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i] = new SonarRing(transform, _tuning, services.Terrain, services.Visuals.SonarRing);
            }

            _markers = new SiteMarker[answerers];
            int friendsEnd = relicCount + friends.Count;
            for (int i = 0; i < _markers.Length; i++)
            {
                bool friend = i >= relicCount && i < friendsEnd;
                string id = i < relicCount ? relics.Relics[i].Definition.Id
                    : friend ? friends.Friends[i - relicCount].Definition.Id
                    : salvage.Sites[i - friendsEnd].Id;
                _markers[i] = new SiteMarker("Marker_" + id, transform, _tuning, services.Terrain,
                    friend ? services.Visuals.FriendPillar : services.Visuals.SitePillar,
                    friend ? services.Visuals.WarmRing : services.Visuals.SiteRing, services.Meshes.Pillar,
                    friend ? _tuning.FriendGlowScale : 1f);
            }

            _initialized = true;
            return true;
        }

        /// <summary>After a load: discovered sites with something left to find breathe on the sonar again.</summary>
        internal void RefreshDiscoveredSites()
        {
            int first = SiteMarkerStart;
            for (int i = 0; i < _salvage.Sites.Count; i++)
            {
                SalvageSite site = _salvage.Sites[i];
                if (site.Discovered && site.AnswersSonar)
                {
                    _markers[first + i].Place(site.Position);
                }
            }
        }

        /// <summary>Index of the first salvage site's marker.</summary>
        private int SiteMarkerStart => _relics.Relics.Count + _friends.Count;

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

            if (_abilities.Has(RoverAbility.WarmHeadlamp))
            {
                LingerFacedPillars(now, Time.deltaTime);
            }

            int relicCount = _relics.Relics.Count;
            for (int i = 0; i < relicCount; i++)
            {
                Relic relic = _relics.Relics[i];
                _markers[i].Tick(relic.SonarPosition, relic.AnswersSonar, false, now);
            }

            for (int i = 0; i < _friends.Count; i++)
            {
                FriendProgress progress = _friends.Friends[i].Progress;
                _markers[relicCount + i].Tick(_friends.Friends[i].Site.Position, progress.AnswersSonar,
                    progress.AnswersSonar && progress.Discovered, now);
            }

            int first = SiteMarkerStart;
            for (int i = 0; i < _salvage.Sites.Count; i++)
            {
                SalvageSite site = _salvage.Sites[i];
                bool answers = site.AnswersSonar;
                _markers[first + i].Tick(site.Position, answers, answers && site.Discovered, now);
            }

            UpdateGaze(now);
        }

        /// <summary>
        /// The Warm Headlamp's gift to the sonar: a site's or relic's pillar 07 is heading toward stands a little
        /// longer (friends' warm pillars keep their own time).
        /// </summary>
        private void LingerFacedPillars(float now, float deltaTime)
        {
            Vector3 heading = _rover.Rotation * Vector3.forward;
            heading.y = 0f;
            if (heading.sqrMagnitude < 1e-6f)
            {
                return;
            }

            heading.Normalize();
            float minCos = Mathf.Cos(_tuning.HeadlampCone * Mathf.Deg2Rad);
            float seconds = deltaTime * _tuning.HeadlampLinger;
            Vector3 rover = _rover.Position;
            int friendsEnd = _relics.Relics.Count + _friends.Count;
            for (int i = 0; i < _markers.Length; i++)
            {
                if (i >= _relics.Relics.Count && i < friendsEnd)
                {
                    continue;
                }

                SiteMarker marker = _markers[i];
                Vector3 toMarker = marker.Position - rover;
                toMarker.y = 0f;
                float distance = toMarker.magnitude;
                if (distance > 1e-3f && Vector3.Dot(heading, toMarker) >= minCos * distance)
                {
                    marker.Linger(seconds, _tuning.HeadlampMaxLinger, now);
                }
            }
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

            int relicCount = _relics.Relics.Count;
            for (int i = 0; i < _friends.Count; i++)
            {
                Friend friend = _friends.Friends[i];
                if (!friend.Progress.AnswersSonar)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(origin, friend.Site.Position);
                _firstAnswerPending |= _schedule.Add(relicCount + i, distance, now, _tuning.Range,
                    _tuning.RingDuration, _tuning.AnswerLag);
            }

            int first = SiteMarkerStart;
            for (int i = 0; i < _salvage.Sites.Count; i++)
            {
                SalvageSite site = _salvage.Sites[i];
                if (!site.AnswersSonar)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(origin, site.Position);
                _firstAnswerPending |= _schedule.Add(first + i, distance, now, _tuning.Range, _tuning.RingDuration,
                    _tuning.AnswerLag);
            }
        }

        /// <summary>
        /// A spotter friend found salvage site <paramref name="siteIndex"/>: it shows on the sonar (a softer pillar and
        /// its breathing ring) without a ping.
        /// </summary>
        internal void RevealSite(int siteIndex, float brightness)
        {
            SalvageSite site = _salvage.Sites[siteIndex];
            site.MarkDiscovered();
            _markers[SiteMarkerStart + siteIndex].Answer(site.Position, brightness, Time.time);
        }

        private void Answer(int index, float distance, float now)
        {
            Vector3 position;
            int relicCount = _relics.Relics.Count;
            int first = SiteMarkerStart;
            if (index >= first)
            {
                SalvageSite site = _salvage.Sites[index - first];
                if (!site.AnswersSonar)
                {
                    return;
                }

                position = site.Position;
                site.MarkDiscovered();
                _events.Publish(new SiteAnswered(site.Id, position, distance, site.HoldsRelic));
            }
            else if (index >= relicCount)
            {
                Friend friend = _friends.Friends[index - relicCount];
                if (!friend.Progress.AnswersSonar)
                {
                    return;
                }

                position = friend.Site.Position;
                friend.Progress.MarkDiscovered();
                _events.Publish(new FriendAnswered(friend.Definition.Id, position));
            }
            else
            {
                Relic relic = _relics.Relics[index];
                if (!relic.AnswersSonar)
                {
                    return;
                }

                position = relic.SonarPosition;
                relic.MarkDiscovered();
                _events.Publish(new RelicAnswered(position, distance, relic.Definition.Id));
            }

            _markers[index].Answer(position, _tuning.BrightnessAt(distance), now);
            if (_firstAnswerPending)
            {
                // Answers arrive nearest first, so the first one of a ping is the closest.
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
