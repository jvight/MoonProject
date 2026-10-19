using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The call from the sky (docs/design/opening-v3-purpose.md §2) as a state machine (EditMode-tested). It follows
    /// the opening's milestones: Broken on reserve, Locked once 07 first charges, Clear once the base is night-ready,
    /// Answered once the call is answered; it never steps back. Its sender's identity decodes as purpose §2 says: a
    /// first character at the first charge (<c>A?? ??</c>), more at the answer, then one each time 07 docks until it
    /// reads in full. Each milestone sets a floor on the decode, never an increment, so a replayed or repeated
    /// milestone decodes nothing twice. Publishes <see cref="CallStateChanged"/> and <see cref="CallDecoded"/>, saves
    /// both, and replays them at session start with Restored = true (<see cref="Announce"/>).
    /// </summary>
    public sealed class CallProgress : ICallSource, IDisposable
    {
        private readonly EventBus _events;
        private readonly CallSettings _settings;
        private readonly IWorldLayout _layout;
        private readonly IDisposable[] _tokens;

        public CallProgress(EventBus events, CallSettings settings, IWorldLayout layout)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Total = settings.Total;
            _tokens = new[]
            {
                events.Subscribe<OpeningMilestoneReached>(OnMilestone),
                events.Subscribe<RoverDockChanged>(OnDockChanged),
            };
        }

        public CallState State { get; private set; } = CallState.Broken;

        public int Decoded { get; private set; }

        public int Total { get; }

        /// <summary>Earth's direction turned round the world's up, then lifted: the point of light beside Earth.</summary>
        public Vector3 Direction
        {
            get
            {
                Vector3 beside = Quaternion.AngleAxis(_settings.YawFromEarth, Vector3.up) * _layout.EarthDirection;
                Vector3 across = Vector3.Cross(Vector3.up, beside);
                if (across.sqrMagnitude < 1e-8f)
                {
                    return beside.normalized;
                }

                return (Quaternion.AngleAxis(-_settings.PitchFromEarth, across.normalized) * beside).normalized;
            }
        }

        /// <summary>The session starts (after the save is applied): the call's state and decode, replayed.</summary>
        public void Announce()
        {
            _events.Publish(new CallStateChanged(State, true));
            _events.Publish(new CallDecoded(Decoded, Total, true));
        }

        public CallSaveData Capture()
        {
            return new CallSaveData { state = (int)State, decoded = Decoded };
        }

        public void Restore(CallSaveData data)
        {
            State = data != null && Enum.IsDefined(typeof(CallState), data.state)
                ? (CallState)data.state
                : CallState.Broken;
            Decoded = data != null ? Mathf.Clamp(data.decoded, 0, Total) : 0;
        }

        public void Dispose()
        {
            foreach (IDisposable token in _tokens)
            {
                token.Dispose();
            }
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            switch (reached.Milestone)
            {
                case OpeningMilestone.FirstCharge:
                    Advance(CallState.Locked, reached.Restored);
                    DecodeAtLeast(_settings.DecodedAtFirstCharge, reached.Restored);
                    break;
                case OpeningMilestone.NightReady:
                    Advance(CallState.Clear, reached.Restored);
                    break;
                case OpeningMilestone.CallAnswered:
                    Advance(CallState.Answered, reached.Restored);
                    DecodeAtLeast(_settings.DecodedAtFirstCharge + _settings.DecodedAtAnswer, reached.Restored);
                    break;
            }
        }

        /// <summary>After the answer, each dock decodes one more character until the identity reads in full.</summary>
        private void OnDockChanged(RoverDockChanged dock)
        {
            if (dock.Docked && State == CallState.Answered && Decoded < Total)
            {
                Decoded++;
                _events.Publish(new CallDecoded(Decoded, Total, false));
            }
        }

        private void Advance(CallState state, bool restored)
        {
            if (state <= State)
            {
                return;
            }

            State = state;
            _events.Publish(new CallStateChanged(State, restored));
        }

        private void DecodeAtLeast(int count, bool restored)
        {
            int floor = Math.Min(count, Total);
            if (floor <= Decoded)
            {
                return;
            }

            Decoded = floor;
            _events.Publish(new CallDecoded(Decoded, Total, restored));
        }
    }
}
