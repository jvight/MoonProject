using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The answer to the call from the sky (docs/design/opening-v3-purpose.md R8, EditMode-tested). Once the antenna
    /// mast stands raised, the night has fallen, the call is clear and 07 has tried and failed its reply, 07 may
    /// transmit: it holds at the mast, the transmission goes out as a pulse of the old network to relay.1 on the west
    /// rim, relay.1 answers (<see cref="RelayAcked"/>, and the call is <see cref="OpeningMilestone.CallAnswered"/>),
    /// and the pulse runs on to glint on the Peak's dark dish. It happens once, and what makes it happen is saved by the
    /// call itself: a loaded game whose call is already answered is done and silent. Allocation-free per frame.
    /// </summary>
    public sealed class CallAnswer : IAnswerStatus, IDisposable
    {
        private readonly EventBus _events;
        private readonly ICallSource _call;
        private readonly CallReply _reply;
        private readonly MastRaise _mast;
        private readonly CallAnswerSettings _settings;
        private readonly Vector3 _relay;
        private readonly Vector3 _dish;
        private readonly IDisposable _built;
        private Vector3 _mastPoint;
        private bool _mastKnown;
        private bool _prompted;
        private float _seconds;
        private bool _acked;
        private bool _routed;
        private float _legSeconds;
        private float _routeLegSeconds;

        public CallAnswer(EventBus events, ICallSource call, CallReply reply, MastRaise mast,
            CallAnswerSettings settings, Vector3 relay, Vector3 peakPosition)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _call = call ?? throw new ArgumentNullException(nameof(call));
            _reply = reply ?? throw new ArgumentNullException(nameof(reply));
            _mast = mast ?? throw new ArgumentNullException(nameof(mast));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _relay = relay;
            _dish = peakPosition + Vector3.up * settings.DishHeight;
            _built = events.Subscribe<StructureBuilt>(OnBuilt);
        }

        public AnswerPhase Phase { get; private set; }

        public float HoldProgress01 { get; private set; }

        public bool AtTheMast { get; private set; }

        public Vector3 MastPoint => _mastPoint;

        /// <summary>Whether the opening can reach the answer: everything R8 needs has come.</summary>
        private bool Ready(bool nightFallen)
        {
            return _mastKnown && _mast.Done && _reply.Tried && nightFallen && _call.State == CallState.Clear;
        }

        /// <param name="nightFallen">The first night has fallen over the night-ready base.</param>
        /// <param name="held">The transmit key (E) is held.</param>
        public void Step(float deltaTime, bool nightFallen, bool held, Vector3 rover, float speed)
        {
            if (Phase == AnswerPhase.Done)
            {
                return;
            }

            if (Phase < AnswerPhase.Transmitting && _call.State >= CallState.Answered)
            {
                SetPhase(AnswerPhase.Done, true);
                return;
            }

            switch (Phase)
            {
                case AnswerPhase.NotReady:
                    if (Ready(nightFallen))
                    {
                        SetPhase(AnswerPhase.Ready, false);
                    }

                    break;
                case AnswerPhase.Ready:
                    StepReady(deltaTime, held, rover, speed);
                    break;
                case AnswerPhase.Transmitting:
                case AnswerPhase.Answered:
                    StepSequence(deltaTime);
                    break;
            }
        }

        public void Dispose()
        {
            _built.Dispose();
        }

        private void StepReady(float deltaTime, bool held, Vector3 rover, float speed)
        {
            Vector3 offset = rover - _mastPoint;
            offset.y = 0f;
            AtTheMast = offset.magnitude <= _settings.TransmitRadius && speed <= _settings.StillSpeed;
            if (AtTheMast && !_prompted)
            {
                _prompted = true;
                _events.Publish(new TransmitPrompted());
            }
            else if (!AtTheMast && offset.magnitude > _settings.TransmitRadius * 1.5f)
            {
                _prompted = false;
            }

            if (AtTheMast && held)
            {
                HoldProgress01 = Mathf.Min(1f, HoldProgress01 + deltaTime / _settings.HoldSeconds);
            }
            else
            {
                HoldProgress01 = Mathf.Max(0f, HoldProgress01 - deltaTime * _settings.HoldDrainRate / _settings.HoldSeconds);
            }

            if (HoldProgress01 >= 1f)
            {
                Launch();
            }
        }

        private void Launch()
        {
            HoldProgress01 = 0f;
            AtTheMast = false;
            _seconds = 0f;
            _acked = false;
            _routed = false;
            _legSeconds = _settings.LegSeconds(Vector3.Distance(_mastPoint, _relay));
            _routeLegSeconds = _settings.LegSeconds(Vector3.Distance(_relay, _dish));
            SetPhase(AnswerPhase.Transmitting, false);
            _events.Publish(new NetworkPulseLaunched(_mastPoint, _relay, _legSeconds, 0));
        }

        private void StepSequence(float deltaTime)
        {
            _seconds += deltaTime;
            if (!_acked && _seconds >= _legSeconds)
            {
                _acked = true;
                SetPhase(AnswerPhase.Answered, false);
                _events.Publish(new RelayAcked(_relay));
                _events.Publish(new OpeningMilestoneReached(OpeningMilestone.CallAnswered, false));
            }

            float routeAt = _legSeconds + _settings.RouteDelaySeconds;
            if (_acked && !_routed && _seconds >= routeAt)
            {
                _routed = true;
                _events.Publish(new NetworkPulseLaunched(_relay, _dish, _routeLegSeconds, 1));
            }

            if (_routed && _seconds >= routeAt + _routeLegSeconds)
            {
                _events.Publish(new PeakDishGlinted(_dish));
                SetPhase(AnswerPhase.Done, false);
            }
        }

        private void SetPhase(AnswerPhase phase, bool restored)
        {
            Phase = phase;
            _events.Publish(new CallAnswerPhaseChanged(phase, restored));
        }

        private void OnBuilt(StructureBuilt built)
        {
            if (string.Equals(built.StructureId, StructureIds.AntennaMast, StringComparison.Ordinal))
            {
                _mastPoint = built.Position;
                _mastKnown = true;
            }
        }
    }
}
