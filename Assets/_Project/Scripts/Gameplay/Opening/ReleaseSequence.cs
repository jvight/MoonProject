using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The release after the answer (docs/design/opening-v3-purpose.md R9, EditMode-tested), staggered so the player
    /// takes in one thing at a time: first the full range ring, shown once; then the view from the rise, 07 looking at
    /// relay.1, the Peak's dish and Kestrel-3 in turn; then, at the next dock, the socket rack (slice 4.3 listens for
    /// <see cref="SocketRackRevealed"/>). It begins when the answer finishes live; a game saved after the answer but
    /// before the rack was shown starts at the rack, and one that has shown it is done. Wear starts with the call's
    /// answer (the charge floor lifts with <see cref="CallState.Answered"/>), not here. Allocation-free per frame.
    /// </summary>
    public sealed class ReleaseSequence : IDisposable
    {
        private readonly EventBus _events;
        private readonly ICallSource _call;
        private readonly IRangeRing _ring;
        private readonly Action<RangeRingMode> _showRing;
        private readonly CallAnswerSettings _settings;
        private readonly Vector3 _relay;
        private readonly Vector3 _dish;
        private readonly Vector3 _kestrel;
        private readonly IDisposable[] _tokens;
        private float _left;
        private bool _rackShown;
        private bool _announced;
        private bool _pendingView;

        public ReleaseSequence(EventBus events, ICallSource call, IRangeRing ring, Action<RangeRingMode> showRing,
            CallAnswerSettings settings, Vector3 relay, Vector3 dish, Vector3 kestrel)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _call = call ?? throw new ArgumentNullException(nameof(call));
            _ring = ring ?? throw new ArgumentNullException(nameof(ring));
            _showRing = showRing ?? throw new ArgumentNullException(nameof(showRing));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _relay = relay;
            _dish = dish;
            _kestrel = kestrel;
            _tokens = new[]
            {
                events.Subscribe<CallAnswerPhaseChanged>(OnAnswerPhase),
                events.Subscribe<RoverDockChanged>(OnDock),
            };
        }

        public ReleaseStage Stage { get; private set; }

        /// <summary>The rack was shown (saved, so a loaded game never shows it twice).</summary>
        public bool RackShown => _rackShown;

        public void Capture(CallSaveData data)
        {
            data.rackShown = _rackShown;
        }

        public void Restore(CallSaveData data)
        {
            _rackShown = data != null && data.rackShown;
        }

        /// <summary>The session starts (after the save is applied): a loaded release finds its stage.</summary>
        public void Announce()
        {
            if (_announced)
            {
                return;
            }

            _announced = true;
            if (_call.State >= CallState.Answered)
            {
                SetStage(_rackShown ? ReleaseStage.Done : ReleaseStage.RackPending, true);
            }
        }

        public void Step(float deltaTime)
        {
            switch (Stage)
            {
                case ReleaseStage.Ring:
                    if (!_ring.IsShown)
                    {
                        _left = _settings.ReleaseGapSeconds;
                        Stage = ReleaseStage.View;
                        _events.Publish(new ReleaseStageChanged(ReleaseStage.View, false));
                        _pendingView = true;
                    }

                    break;
                case ReleaseStage.View:
                    _left -= deltaTime;
                    if (_pendingView && _left <= 0f)
                    {
                        _pendingView = false;
                        _left = _settings.ViewSecondsEach * 3f;
                        _events.Publish(new ReleaseViewOpened(_relay, _dish, _kestrel, _settings.ViewSecondsEach));
                    }
                    else if (!_pendingView && _left <= 0f)
                    {
                        SetStage(ReleaseStage.RackPending, false);
                    }

                    break;
            }
        }

        public void Dispose()
        {
            foreach (IDisposable token in _tokens)
            {
                token.Dispose();
            }
        }

        private void OnAnswerPhase(CallAnswerPhaseChanged changed)
        {
            if (changed.Phase == AnswerPhase.Done && !changed.Restored && Stage == ReleaseStage.Waiting)
            {
                SetStage(ReleaseStage.Ring, false);
                _showRing(RangeRingMode.Full);
            }
        }

        private void OnDock(RoverDockChanged dock)
        {
            if (dock.Docked && Stage == ReleaseStage.RackPending)
            {
                _rackShown = true;
                _events.Publish(new SocketRackRevealed(dock.Position, dock.Rotation));
                SetStage(ReleaseStage.Done, false);
            }
        }

        private void SetStage(ReleaseStage stage, bool restored)
        {
            Stage = stage;
            _events.Publish(new ReleaseStageChanged(stage, restored));
        }
    }
}
