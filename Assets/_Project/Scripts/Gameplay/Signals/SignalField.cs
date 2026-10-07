using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's signals in the world (docs/features/M3-05 "Bell's signals"): once she is home and has greeted 07 for the
    /// first time (or straight away in a session that starts with her home), <see cref="BellSignals"/> points at one
    /// undiscovered thing at a time among the cassettes, crew log caches and relics (<see cref="SignalTargets"/>),
    /// re-checking every frame without allocating. An amber pillar rises over the current target and fades once it
    /// is found while the next one rises elsewhere (the dimmest of three pillars takes the new target, so a fading one
    /// is never yanked away). The current target and the found count are saved ("gameplay.bell_signals").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignalField : MonoBehaviour
    {
        private BellTuning _tuning;
        private FriendField _friends;
        private BellSignals _signals;
        private SignalPillar[] _pillars = Array.Empty<SignalPillar>();
        private int _lit = -1;
        private int _pointing = -1;
        private bool _initialized;

        /// <summary>Bell's signal logic (tests read its target and count).</summary>
        public BellSignals Signals => _signals;

        /// <summary>The pillar over the current target, or null.</summary>
        internal SignalPillar Pillar => _lit >= 0 ? _pillars[_lit] : null;

        internal bool Initialize(GameplayServices services, FriendField friends, CassetteField cassettes,
            LogCacheField logs, RelicField relics, IRoverAbilities abilities, SonarTuning sonar)
        {
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            if (friends.DialFriend == null || friends.BellTuning == null)
            {
                Debug.LogError($"{nameof(SignalField)}: no friend brings the radio dial (Bell), so nobody picks up " +
                               "signals. Check the friend catalog.", this);
                enabled = false;
                return false;
            }

            _tuning = friends.BellTuning;
            var targets = new SignalTargets(cassettes, logs, relics, abilities);
            _signals = new BellSignals(services.Events, targets, services.Layout.BasePosition);
            _pillars = new[]
            {
                CreatePillar("SignalPillar_A", services, sonar), CreatePillar("SignalPillar_B", services, sonar),
                CreatePillar("SignalPillar_C", services, sonar),
            };
            _initialized = true;
            return true;
        }

        internal BellSignalSaveData Capture()
        {
            return _signals.Capture();
        }

        internal void Restore(BellSignalSaveData data)
        {
            _signals.Restore(data);
        }

        private SignalPillar CreatePillar(string name, GameplayServices services, SonarTuning sonar)
        {
            return new SignalPillar(name, transform, sonar, services.Terrain, services.Visuals.FriendPillar,
                services.Visuals.WarmRing, services.Meshes.Pillar);
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
            if (_signals.Current != _pointing)
            {
                Point(_signals.Current);
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _pillars.Length; i++)
            {
                _pillars[i].Tick(now, deltaTime, _tuning);
            }
        }

        /// <summary>The lit pillar fades; the dimmest rises over the new target (if there is one).</summary>
        private void Point(int target)
        {
            _pointing = target;
            if (_lit >= 0)
            {
                _pillars[_lit].Hide();
            }

            _lit = -1;
            if (target < 0)
            {
                return;
            }

            for (int i = 0; i < _pillars.Length; i++)
            {
                if (_lit < 0 || _pillars[i].Level < _pillars[_lit].Level)
                {
                    _lit = i;
                }
            }

            _pillars[_lit].Show(_signals.TargetPosition);
        }

        private void OnDestroy()
        {
            foreach (SignalPillar pillar in _pillars)
            {
                pillar.Dispose();
            }
        }
    }
}
