using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's listening, the sonar with no button (docs/design/first-hour-v2.md §3). From tower level 1 on, once 07 has
    /// rested a moment after driving somewhere new (<see cref="ListenGate"/>), it listens: <see cref="SonarPinged"/>
    /// is published (the antenna lifts, the kalimba sounds) and a ring rolls over the ground; every salvage site and
    /// every derelict standing alone with something left to find (a piece to cut, a relic in its heart, fragments to
    /// scan, D2) answers when the ring has touched it (closer ones sooner and brighter); emptied, it falls quiet,
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

        private readonly ListenGate _gate = new ListenGate();
        private EventBus _events;
        private IBasePower _power;
        private IRoverState _rover;
        private IRoverRig _rig;
        private RelicField _relics;
        private FriendField _friends;
        /// <summary>Every salvage site, then every derelict standing alone: what answers as a site.</summary>
        private readonly List<SalvageSite> _answering = new List<SalvageSite>();
        private IRoverAbilities _abilities;
        private SonarSchedule _schedule;
        private SonarRing[] _rings = Array.Empty<SonarRing>();
        private SiteMarker[] _markers = Array.Empty<SiteMarker>();
        private bool _firstAnswerPending;
        private float _gazeUntil = float.NegativeInfinity;
        private float _nextGlance;
        private bool _gazing;
        private bool _initialized;

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
            SalvageField salvage, IRoverAbilities abilities, IBasePower power)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(SonarSystem)}: SonarTuning is not assigned.", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _power = power ?? throw new ArgumentNullException(nameof(power));
            _rover = services.Rover;
            _rig = services.Rig;
            _relics = relics ?? throw new ArgumentNullException(nameof(relics));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            if (salvage == null)
            {
                throw new ArgumentNullException(nameof(salvage));
            }

            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            int relicCount = relics.Relics.Count;
            _answering.AddRange(salvage.Sites);
            foreach (SalvageSite derelict in salvage.Derelicts)
            {
                if (salvage.StandsAlone(derelict))
                {
                    _answering.Add(derelict);
                }
            }

            int answerers = relicCount + friends.Count + _answering.Count;
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
                    : _answering[i - friendsEnd].Id;
                _markers[i] = new SiteMarker("Marker_" + id, transform, _tuning, services.Terrain,
                    friend ? services.Visuals.FriendPillar : services.Visuals.SitePillar, services.Meshes.Pillar,
                    friend ? _tuning.FriendGlowScale : 1f);
            }

            _initialized = true;
            return true;
        }

        /// <summary>
        /// After a load: discovered sites and derelicts with something left to find breathe on the sonar again.
        /// </summary>
        internal void RefreshDiscoveredSites()
        {
            int first = SiteMarkerStart;
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite site = _answering[i];
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
            if (_gate.Tick(_power.HasReached(BasePowerStage.Home), _rover.Position, _rover.Speed, Time.deltaTime,
                    _tuning))
            {
                Listen(now);
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
                _markers[i].Tick(relic.SonarPosition, relic.AnswersSonar, now);
            }

            for (int i = 0; i < _friends.Count; i++)
            {
                FriendProgress progress = _friends.Friends[i].Progress;
                _markers[relicCount + i].Tick(_friends.Friends[i].Site.Position, progress.AnswersSonar, now);
            }

            int first = SiteMarkerStart;
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite site = _answering[i];
                _markers[first + i].Tick(site.Position, site.AnswersSonar, now);
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

        private void Listen(float now)
        {
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
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite site = _answering[i];
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
        /// A spotter friend found salvage site <paramref name="siteIndex"/> (its index among the wrecks, which come first
        /// among what answers): it shows on the sonar (a softer pillar) without a listen.
        /// </summary>
        internal void RevealSite(int siteIndex, float brightness)
        {
            SalvageSite site = _answering[siteIndex];
            site.MarkDiscovered();
            _markers[SiteMarkerStart + siteIndex].Answer(site.Position, brightness, Time.time);
        }

        /// <summary>
        /// The tower's listening sweep at the dock's evening (M3-16 P6): the nearest salvage site or derelict to
        /// <paramref name="home"/> that still has something to find and has not been heard yet raises its pillar, or,
        /// with none left, the nearest broken friend not heard yet. False when nothing is left unheard.
        /// </summary>
        internal bool RevealNextUnheard(Vector3 home, float brightness)
        {
            int site = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite candidate = _answering[i];
                float distance = SurfaceRules.HorizontalDistance(home, candidate.Position);
                if (!candidate.Discovered && candidate.AnswersSonar && distance < best)
                {
                    site = i;
                    best = distance;
                }
            }

            if (site >= 0)
            {
                RevealSite(site, brightness);
                return true;
            }

            int friend = -1;
            for (int i = 0; i < _friends.Friends.Count; i++)
            {
                Friend candidate = _friends.Friends[i];
                float distance = SurfaceRules.HorizontalDistance(home, candidate.Site.Position);
                if (!candidate.Progress.Discovered && candidate.Progress.AnswersSonar && distance < best)
                {
                    friend = i;
                    best = distance;
                }
            }

            if (friend < 0)
            {
                return false;
            }

            RevealFriend(friend, brightness);
            return true;
        }

        /// <summary>
        /// A spotter friend saw friend <paramref name="friendIndex"/>'s light from afar (M3-16 P2): its warm pillar
        /// rises without a listen, and rises again (a pulse) each time this is called.
        /// </summary>
        internal void RevealFriend(int friendIndex, float brightness)
        {
            Friend friend = _friends.Friends[friendIndex];
            friend.Progress.MarkDiscovered();
            _markers[_relics.Relics.Count + friendIndex].Answer(friend.Site.Position, brightness, Time.time);
        }

        private void Answer(int index, float distance, float now)
        {
            Vector3 position;
            int relicCount = _relics.Relics.Count;
            int first = SiteMarkerStart;
            if (index >= first)
            {
                SalvageSite site = _answering[index - first];
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
                // Answers arrive nearest first, so the first one of a listen is the closest.
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
        }
    }
}
