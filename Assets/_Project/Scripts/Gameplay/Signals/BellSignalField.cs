using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's signals in the world (docs/features/M3-05 "Bell's signals"): once she is home and has greeted 07 for the
    /// first time (or straight away in a session that starts with her home), <see cref="BellSignals"/> points at one
    /// undiscovered thing at a time among the cassettes, crew log caches and relics (<see cref="SignalTargets"/>),
    /// re-checking every frame without allocating. The signal target's own lamp (the wreck's <c>AnswerLamp</c> or its
    /// container's seal, plus a small <see cref="AnswerBlink"/> halo for distance readability) blinks slowly at a calm
    /// interval while Bell's signal points at it, under the bloom threshold; nothing glows on bare ground and nothing
    /// rises in the sky. The current target and the found count are saved ("gameplay.bell_signals").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BellSignalField : MonoBehaviour
    {
        private BellTuning _tuning;
        private FriendField _friends;
        private CassetteField _cassettes;
        private LogCacheField _logs;
        private RelicField _relics;
        private SalvageField _salvage;
        private BellSignals _signals;
        private AnswerBlink _blink;
        private SalvageSite _site;
        private SealedContainer _container;
        private float _nextBlink;
        private int _pointing = -1;
        private bool _initialized;

        /// <summary>Bell's signal logic (tests read its target and count).</summary>
        public BellSignals Signals => _signals;

        /// <summary>The blinking halo on the current signal target's lamp.</summary>
        internal AnswerBlink Blink => _blink;

        /// <summary>Current blink intensity on the signal target's lamp (0 when idle).</summary>
        public float BlinkIntensity => _blink != null ? _blink.Intensity : 0f;

        /// <summary>World position of the signal target's blinking lamp.</summary>
        public Vector3 BlinkPosition => _pointing >= 0 ? LampPosition(_pointing) : Vector3.zero;

        internal bool Initialize(GameplayServices services, FriendField friends, CassetteField cassettes,
            LogCacheField logs, RelicField relics, SalvageField salvage, SonarTuning sonarTuning,
            IRoverAbilities abilities)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _cassettes = cassettes != null ? cassettes : throw new ArgumentNullException(nameof(cassettes));
            _logs = logs != null ? logs : throw new ArgumentNullException(nameof(logs));
            _relics = relics != null ? relics : throw new ArgumentNullException(nameof(relics));
            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            if (sonarTuning == null)
            {
                throw new ArgumentNullException(nameof(sonarTuning));
            }

            if (friends.DialFriend == null || friends.BellTuning == null)
            {
                Debug.LogError($"{nameof(BellSignalField)}: no friend brings the radio dial (Bell), so nobody picks " +
                               "up signals. Check the friend catalog.", this);
                enabled = false;
                return false;
            }

            _tuning = friends.BellTuning;
            var targets = new SignalTargets(cassettes, logs, relics, abilities);
            _signals = new BellSignals(services.Events, targets, services.Layout.BasePosition);
            _blink = new AnswerBlink("SignalBlink", transform, sonarTuning, services.Visuals.HomeHalo,
                services.Meshes.Quad);
            _initialized = true;
            return true;
        }

        internal BellSignalSaveData Capture()
        {
            return _signals.Capture();
        }

        internal void Restore(BellSignalSaveData data)
        {
            ClearTargetLamp();
            _pointing = -1;
            _blink?.Tick(Vector3.zero, false, 0f);
            _signals.Restore(data);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            Friend bell = _friends.DialFriend;
            if (!_signals.Running && bell.IsHome && bell.Progress.Welcomed)
            {
                _signals.Start();
            }

            _signals.Step();
            float now = Time.time;
            if (_signals.Current != _pointing)
            {
                Point(_signals.Current, now);
            }

            if (_pointing < 0)
            {
                _blink.Tick(Vector3.zero, false, now);
                return;
            }

            Vector3 position = LampPosition(_pointing);
            if (!_blink.Blinking(now) && now >= _nextBlink)
            {
                _blink.Answer(position, _tuning.SignalGlow, now);
                _nextBlink = now + _tuning.SignalInterval;
            }

            _blink.Tick(position, true, now);
            ApplyTargetLamp(_blink.Intensity);
        }

        private void Point(int target, float now)
        {
            ClearTargetLamp();
            _pointing = target;
            if (target < 0)
            {
                _blink.Tick(Vector3.zero, false, now);
                return;
            }

            ResolveTargetLamp(target);
            Vector3 position = LampPosition(target);
            _blink.Answer(position, _tuning.SignalGlow, now);
            _nextBlink = now + _tuning.SignalInterval;
            _blink.Tick(position, true, now);
            ApplyTargetLamp(_blink.Intensity);
        }

        private void ResolveTargetLamp(int target)
        {
            int cassetteCount = _cassettes.Count;
            if (target < cassetteCount)
            {
                string anchorId = _cassettes.Definition(target).Anchor.AnchorId;
                _site = _salvage.Find(anchorId) ?? _salvage.FindDerelict(anchorId);
                _container = _cassettes.Container(target);
                return;
            }

            int logIndex = target - cassetteCount;
            int logCount = _logs.Count;
            if (logIndex < logCount)
            {
                string anchorId = _logs.Definition(logIndex).Anchor.AnchorId;
                _site = _salvage.Find(anchorId) ?? _salvage.FindDerelict(anchorId);
                return;
            }

            int relicIndex = logIndex - logCount;
            if (relicIndex >= 0 && relicIndex < _relics.Relics.Count)
            {
                _site = _relics.Relics[relicIndex].Home;
            }
        }

        private Vector3 LampPosition(int target)
        {
            if (_site != null)
            {
                return _site.AnswerPosition;
            }

            if (_container != null)
            {
                return _container.Opened ? _container.PresentPoint : _container.CutPoint.position;
            }

            int logIndex = target - _cassettes.Count;
            if (logIndex >= 0 && logIndex < _logs.Count)
            {
                return _logs.Position(logIndex) + Vector3.up * _logs.Tuning.GlowHeight;
            }

            return _signals.TargetPosition + Vector3.up * 0.5f;
        }

        private void ApplyTargetLamp(float intensity)
        {
            _site?.SetSignalBlink(intensity);
            if (_container != null)
            {
                _container.SignalBlink = intensity;
            }
        }

        private void ClearTargetLamp()
        {
            _site?.SetSignalBlink(0f);
            _site = null;
            if (_container != null)
            {
                _container.SignalBlink = 0f;
                _container = null;
            }
        }

        private void OnDestroy()
        {
            ClearTargetLamp();
            _blink?.Dispose();
        }
    }
}
