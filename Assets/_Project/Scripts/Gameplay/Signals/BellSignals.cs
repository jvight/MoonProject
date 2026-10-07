using System;
using System.Globalization;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's signals once she is home (docs/features/M3-05 "Bell's signals"), as a pure state machine. When they start
    /// (each session with Bell home, or her first homecoming) she keeps pointing at the target saved last time if it
    /// is still waiting and reachable, and otherwise picks one (<see cref="BellSignalPicker"/>):
    /// <see cref="BellSignalPicked"/> and the ticker line "bearing {0}" from home. When that target is found she
    /// publishes <see cref="BellSignalFound"/> and one of her three kind lines in turn, then picks the next. One target
    /// at a time, it never expires, and when nothing reachable is left she rests until something is (a new ability
    /// opens a region). Allocation-free per step; the ticker text allocates only when a signal is picked.
    /// </summary>
    public sealed class BellSignals
    {
        public const string PickedLine = "ticker.bell.signal";
        public const string FoundLine0 = "ticker.bell.found_0";
        public const string FoundLine1 = "ticker.bell.found_1";
        public const string FoundLine2 = "ticker.bell.found_2";

        /// <summary>Bell's "found" lines, said in turn.</summary>
        public const int FoundLines = 3;

        private const float FullCircle = 360f;

        private readonly EventBus _events;
        private readonly ISignalTargets _targets;
        private readonly Vector3 _home;
        private BellSignalTarget _savedKind;
        private string _savedId = string.Empty;

        public BellSignals(EventBus events, ISignalTargets targets, Vector3 home)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _home = home;
        }

        /// <summary>Bell is home and listening.</summary>
        public bool Running { get; private set; }

        /// <summary>Index (into the targets) of what she points at now, or -1.</summary>
        public int Current { get; private set; } = -1;

        /// <summary>Signals found so far.</summary>
        public int FoundCount { get; private set; }

        /// <summary>Where the current target is (valid while <see cref="Current"/> is not -1).</summary>
        public Vector3 TargetPosition => Current >= 0 ? _targets.Get(Current).Position : Vector3.zero;

        /// <summary>
        /// Whole degrees (0..359, clockwise from +Z) from <paramref name="home"/> to <paramref name="target"/>.
        /// </summary>
        public static int BearingFrom(Vector3 home, Vector3 target)
        {
            Vector3 direction = target - home;
            int bearing = Mathf.RoundToInt(Mathf.Repeat(SurfaceRules.Bearing(direction), FullCircle));
            return bearing % (int)FullCircle;
        }

        /// <summary>Bell starts listening (idempotent): she resumes her saved signal or picks one.</summary>
        public void Start()
        {
            if (Running)
            {
                return;
            }

            Running = true;
            Current = SavedTarget();
            if (Current < 0)
            {
                Current = BellSignalPicker.Pick(_targets, _home);
            }

            if (Current >= 0)
            {
                Announce(Current);
            }
        }

        /// <summary>Checks the current target; once found, Bell says so and picks the next.</summary>
        public void Step()
        {
            if (!Running)
            {
                return;
            }

            if (Current >= 0)
            {
                SignalCandidate target = _targets.Get(Current);
                if (target.Waiting)
                {
                    return;
                }

                _events.Publish(new BellSignalFound(target.Kind, target.Position));
                _events.Publish(new TickerLine(FoundLine(FoundCount)));
                FoundCount++;
                _savedId = string.Empty;
            }

            Current = BellSignalPicker.Pick(_targets, _home);
            if (Current >= 0)
            {
                Announce(Current);
            }
        }

        public BellSignalSaveData Capture()
        {
            if (Current < 0)
            {
                return new BellSignalSaveData { targetKind = (int)_savedKind, targetId = _savedId, found = FoundCount };
            }

            SignalCandidate target = _targets.Get(Current);
            return new BellSignalSaveData { targetKind = (int)target.Kind, targetId = target.Id, found = FoundCount };
        }

        /// <summary>
        /// Applies saved progress (before <see cref="Start"/>): Bell resumes the target when she starts.
        /// </summary>
        public void Restore(BellSignalSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _savedKind = (BellSignalTarget)data.targetKind;
            _savedId = data.targetId ?? string.Empty;
            FoundCount = Mathf.Max(FoundCount, data.found);
        }

        private int SavedTarget()
        {
            if (_savedId.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < _targets.Count; i++)
            {
                SignalCandidate candidate = _targets.Get(i);
                if (candidate.Kind == _savedKind && candidate.Waiting && candidate.Reachable &&
                    string.Equals(candidate.Id, _savedId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void Announce(int index)
        {
            SignalCandidate target = _targets.Get(index);
            _savedKind = target.Kind;
            _savedId = target.Id;
            _events.Publish(new BellSignalPicked(target.Kind, target.Position));
            string bearing = BearingFrom(_home, target.Position).ToString(CultureInfo.InvariantCulture);
            _events.Publish(new TickerLine(PickedLine, bearing));
        }

        private static string FoundLine(int found)
        {
            switch (found % FoundLines)
            {
                case 0:
                    return FoundLine0;
                case 1:
                    return FoundLine1;
                default:
                    return FoundLine2;
            }
        }
    }
}
