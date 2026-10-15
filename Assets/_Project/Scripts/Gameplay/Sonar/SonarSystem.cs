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
    /// is published (the antenna lifts, the kalimba sounds); every salvage site and every derelict standing alone with
    /// something left to find (a piece to cut, a relic in its heart, fragments to scan, D2) answers in distance order
    /// (closer ones sooner and brighter); emptied, it falls quiet, publishing <see cref="SiteAnswered"/> and giving a
    /// short, calm blink of its own lamp. A relic lying loose out in the world answers on its own
    /// (<see cref="RelicAnswered"/>), and a broken friend with its broken chirp (<see cref="FriendAnswered"/>) and a
    /// blink of its eye or dial lamp. 07 turns its head toward the nearest answer for a moment and then releases back
    /// to normal driving look. A spotter friend can also reveal a site softly. With the Warm Headlamp fitted, an answer
    /// lit by the headlamp keeps blinking longer.
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
        private AnswerBlink[] _blinks = Array.Empty<AnswerBlink>();
        private bool _firstAnswerPending;
        private float _gazeUntil = -1f;
        private bool _gazing;
        private bool _initialized;

        public SonarTuning Tuning => _tuning;

        /// <summary>One blink per relic (by relic index), then one per friend, then one per salvage site.</summary>
        internal AnswerBlink[] Blinks => _blinks;

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
            _blinks = new AnswerBlink[answerers];
            int friendsEnd = relicCount + friends.Count;
            for (int i = 0; i < _blinks.Length; i++)
            {
                bool friend = i >= relicCount && i < friendsEnd;
                string id = i < relicCount ? relics.Relics[i].Definition.Id
                    : friend ? friends.Friends[i - relicCount].Definition.Id
                    : _answering[i - friendsEnd].Id;
                _blinks[i] = new AnswerBlink("Blink_" + id, transform, _tuning, services.Visuals.HomeHalo,
                    services.Meshes.Quad);
            }

            _initialized = true;
            return true;
        }

        /// <summary>Index of the first salvage site's blink.</summary>
        private int SiteBlinkStart => _relics.Relics.Count + _friends.Count;

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

            if (_abilities.Has(RoverAbility.WarmHeadlamp))
            {
                LingerFacedBlinks(now, Time.deltaTime);
            }

            int relicCount = _relics.Relics.Count;
            for (int i = 0; i < relicCount; i++)
            {
                Relic relic = _relics.Relics[i];
                Vector3 position = relic.SonarPosition + Vector3.up * _tuning.RelicBlinkLift;
                _blinks[i].Tick(position, relic.AnswersSonar, now);
            }

            for (int i = 0; i < _friends.Count; i++)
            {
                Friend friend = _friends.Friends[i];
                AnswerBlink blink = _blinks[relicCount + i];
                blink.Tick(friend.AnswerPosition, friend.Progress.AnswersSonar, now);
                friend.SetAnswerBlink(blink.Intensity);
            }

            int first = SiteBlinkStart;
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite site = _answering[i];
                AnswerBlink blink = _blinks[first + i];
                blink.Tick(site.AnswerPosition, site.AnswersSonar, now);
                site.SetAnswerBlink(blink.Intensity);
            }

            UpdateGaze(now);
        }

        /// <summary>
        /// The Warm Headlamp's gift to the sonar: an answer lit by 07's headlamp keeps blinking a little longer.
        /// </summary>
        private void LingerFacedBlinks(float now, float deltaTime)
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
            for (int i = 0; i < _blinks.Length; i++)
            {
                AnswerBlink blink = _blinks[i];
                if (!blink.Blinking(now))
                {
                    continue;
                }

                Vector3 toBlink = blink.Position - rover;
                toBlink.y = 0f;
                float distance = toBlink.magnitude;
                if (distance > 1e-3f && Vector3.Dot(heading, toBlink) >= minCos * distance)
                {
                    blink.Linger(seconds, _tuning.HeadlampMaxLinger, now);
                }
            }
        }

        private void Listen(float now)
        {
            Vector3 origin = _rover.Position;
            _events.Publish(new SonarPinged(origin, _tuning.Range));

            _firstAnswerPending = false;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (!relic.AnswersSonar)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(origin, relic.SonarPosition);
                _firstAnswerPending |= _schedule.Add(i, distance, now, _tuning.Range, _tuning.SweepDuration,
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
                    _tuning.SweepDuration, _tuning.AnswerLag);
            }

            int first = SiteBlinkStart;
            for (int i = 0; i < _answering.Count; i++)
            {
                SalvageSite site = _answering[i];
                if (!site.AnswersSonar)
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(origin, site.Position);
                _firstAnswerPending |= _schedule.Add(first + i, distance, now, _tuning.Range, _tuning.SweepDuration,
                    _tuning.AnswerLag);
            }
        }

        /// <summary>
        /// A spotter friend found salvage site <paramref name="siteIndex"/> (its index among the wrecks, which come first
        /// among what answers): it blinks on the sonar without a listen.
        /// </summary>
        internal void RevealSite(int siteIndex, float brightness)
        {
            SalvageSite site = _answering[siteIndex];
            site.MarkDiscovered();
            _blinks[SiteBlinkStart + siteIndex].Answer(site.AnswerPosition, brightness, Time.time);
        }

        /// <summary>
        /// The tower's listening sweep at the dock's evening (M3-16 P6): the nearest salvage site or derelict to
        /// <paramref name="home"/> that still has something to find and has not been heard yet blinks its lamp, or,
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
        /// A spotter friend saw friend <paramref name="friendIndex"/>'s light from afar (M3-16 P2): its lamp blinks
        /// without a listen, and blinks again each time this is called.
        /// </summary>
        internal void RevealFriend(int friendIndex, float brightness)
        {
            Friend friend = _friends.Friends[friendIndex];
            friend.Progress.MarkDiscovered();
            _blinks[_relics.Relics.Count + friendIndex].Answer(friend.AnswerPosition, brightness, Time.time);
        }

        private void Answer(int index, float distance, float now)
        {
            Vector3 position;
            Vector3 blinkPosition;
            int relicCount = _relics.Relics.Count;
            int first = SiteBlinkStart;
            if (index >= first)
            {
                SalvageSite site = _answering[index - first];
                if (!site.AnswersSonar)
                {
                    return;
                }

                position = site.Position;
                blinkPosition = site.AnswerPosition;
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
                blinkPosition = friend.AnswerPosition;
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
                blinkPosition = position + Vector3.up * _tuning.RelicBlinkLift;
                relic.MarkDiscovered();
                _events.Publish(new RelicAnswered(position, distance, relic.Definition.Id));
            }

            _blinks[index].Answer(blinkPosition, _tuning.BrightnessAt(distance), now);
            if (_firstAnswerPending)
            {
                // Answers arrive nearest first, so the first one of a listen is the closest.
                _firstAnswerPending = false;
                _rig.SetGazeTarget(this, blinkPosition, GazePriorities.Interest);
                _gazing = true;
                _gazeUntil = now + _tuning.AnswerGaze;
            }
        }

        private void UpdateGaze(float now)
        {
            if (_gazing && now >= _gazeUntil)
            {
                _gazing = false;
                _gazeUntil = -1f;
                _rig.ClearGazeTarget(this);
            }
        }

        private void OnDisable()
        {
            if (_gazing)
            {
                _gazing = false;
                _gazeUntil = -1f;
                _rig.ClearGazeTarget(this);
            }
        }

        private void OnDestroy()
        {
            foreach (AnswerBlink blink in _blinks)
            {
                blink.Dispose();
            }
        }
    }
}
