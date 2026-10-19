using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's one failed reply to the call (docs/design/opening-v3-purpose.md R7, EditMode-tested): once the base is
    /// night-ready and the call has cleared, after dark, as soon as 07 stands still or sits docked for a while it tries
    /// to answer and cannot, <see cref="CallReplyFailed"/>. It happens once in a game and is saved: a loaded game that
    /// already tried says nothing, and one that reaches the moment loaded fires it as soon as 07 is still. A call
    /// that is already answered never fails a reply.
    /// </summary>
    public sealed class CallReply
    {
        private readonly EventBus _events;
        private readonly ICallSource _call;
        private readonly CallReplySettings _settings;
        private float _still;

        public CallReply(EventBus events, ICallSource call, CallReplySettings settings)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _call = call ?? throw new ArgumentNullException(nameof(call));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The reply has been tried (or is moot): it does not come again.</summary>
        public bool Tried { get; private set; }

        /// <param name="nightFallen">The first night has fallen over the night-ready base.</param>
        /// <param name="speed">07's ground speed (m/s).</param>
        public void Step(float deltaTime, bool nightFallen, float speed)
        {
            if (Tried)
            {
                return;
            }

            if (_call.State >= CallState.Answered)
            {
                Tried = true;
                return;
            }

            bool ready = _call.State == CallState.Clear && nightFallen;
            _still = ready && speed <= _settings.StillSpeed ? _still + deltaTime : 0f;
            if (_still >= _settings.StillSeconds)
            {
                Tried = true;
                _events.Publish(new CallReplyFailed());
            }
        }

        public void Capture(CallSaveData data)
        {
            data.replyTried = Tried;
        }

        public void Restore(CallSaveData data)
        {
            Tried = data != null && data.replyTried;
            _still = 0f;
        }
    }
}
