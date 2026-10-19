using System;
using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>What a readout card tells about.</summary>
    internal enum ReadoutKind
    {
        None = 0,

        /// <summary>One system's line (the cutter turning OK, or refusing a cut).</summary>
        System = 2,

        /// <summary>The protocol line (<c>POWER · ON</c>, <c>LINK · 0/1 · SIGNAL WEST</c>).</summary>
        Protocol = 3,

        /// <summary><c>LANDER RESERVE · 0%</c>.</summary>
        Reserve = 4,

        /// <summary>The text of a scanned story panel.</summary>
        Story = 5,

        /// <summary>
        /// A piece of a structure's scan set is done: its blueprint's progress (<c>Solar Array · blueprint 1/3</c>).
        /// </summary>
        ScanSet = 6,
    }

    /// <summary>One card the readout is asked to show.</summary>
    internal readonly struct ReadoutRequest
    {
        public ReadoutRequest(ReadoutKind kind, RoverSystem system = RoverSystem.Wheels, string textKey = null,
            int step = 0, int of = 0)
        {
            Kind = kind;
            System = system;
            TextKey = textKey ?? string.Empty;
            Step = step;
            Of = of;
        }

        public ReadoutKind Kind { get; }

        /// <summary>The system a <see cref="ReadoutKind.System"/> card tells about.</summary>
        public RoverSystem System { get; }

        /// <summary>
        /// The story text of a <see cref="ReadoutKind.Story"/> card, or the structure a
        /// <see cref="ReadoutKind.ScanSet"/> card's blueprint teaches.
        /// </summary>
        public string TextKey { get; }

        /// <summary>The pieces of a <see cref="ReadoutKind.ScanSet"/> scanned so far (1-based).</summary>
        public int Step { get; }

        /// <summary>The pieces in that set.</summary>
        public int Of { get; }

        /// <summary>
        /// True when <paramref name="other"/> tells about the same thing with newer words (a scan set's next piece):
        /// it takes this card's place instead of queueing after it.
        /// </summary>
        public bool Supersedes(ReadoutRequest other)
        {
            return Kind == ReadoutKind.ScanSet && other.Kind == ReadoutKind.ScanSet &&
                   string.Equals(TextKey, other.TextKey, StringComparison.Ordinal);
        }

        public bool Matches(ReadoutRequest other)
        {
            return Kind == other.Kind && System == other.System &&
                   string.Equals(TextKey, other.TextKey, StringComparison.Ordinal) &&
                   Step == other.Step && Of == other.Of;
        }
    }

    /// <summary>
    /// The order and timing of the readout's cards (pure, EditMode-tested). One card shows at a time; a card waits for
    /// the guidance slot, rests for its hold, fades, and then the next one comes, in the order asked. A card asked for
    /// again while it shows keeps the screen for a fresh hold instead of queueing twice, and a newer card about the
    /// same thing (a scan set's next piece) takes the older one's place, so cards never pile up between the
    /// objective's lines.
    /// </summary>
    internal sealed class ReadoutQueue
    {
        private enum Phase
        {
            Idle,
            Showing,
            Fading,
        }

        private readonly GuidanceSettings _settings;
        private readonly List<ReadoutRequest> _pending = new List<ReadoutRequest>(4);
        private Phase _phase;
        private float _hold;

        public ReadoutQueue(GuidanceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The card on the screen (showing or fading); <see cref="ReadoutKind.None"/> when idle.</summary>
        public ReadoutRequest Current { get; private set; }

        /// <summary>True from the moment a card is asked for until its fade is over: it wants the guidance slot.</summary>
        public bool Wants => _phase != Phase.Idle || _pending.Count > 0;

        /// <summary>True while the card rests on the screen (not yet fading).</summary>
        public bool IsShowing => _phase == Phase.Showing;

        /// <summary>
        /// Asks for a card. Returns true when it lands on the card already showing (the same card, or newer words about
        /// the same thing, now <see cref="Current"/>; its hold starts afresh), so the caller can rewrite or flash it.
        /// </summary>
        public bool Offer(ReadoutRequest request)
        {
            if (_phase == Phase.Showing && (Current.Matches(request) || Current.Supersedes(request)))
            {
                Current = request;
                _hold = HoldOf(request.Kind);
                return true;
            }

            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].Matches(request))
                {
                    return false;
                }

                if (_pending[i].Supersedes(request))
                {
                    _pending[i] = request;
                    return false;
                }
            }

            _pending.Add(request);

            return false;
        }

        /// <summary>
        /// Steps the timeline; true when a card started on this step (the caller writes its lines).
        /// </summary>
        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="granted">The guidance slot lets the readout show now.</param>
        /// <param name="hidden">The panel has finished fading out.</param>
        public bool Tick(float deltaTime, bool granted, bool hidden)
        {
            switch (_phase)
            {
                case Phase.Idle:
                    if (granted && _pending.Count > 0)
                    {
                        Current = _pending[0];
                        _pending.RemoveAt(0);
                        _hold = HoldOf(Current.Kind);
                        _phase = Phase.Showing;
                        return true;
                    }

                    break;
                case Phase.Showing:
                    if (granted)
                    {
                        _hold -= deltaTime;
                        if (_hold <= 0f)
                        {
                            _phase = Phase.Fading;
                        }
                    }

                    break;
                default:
                    if (hidden)
                    {
                        Current = default;
                        _phase = Phase.Idle;
                    }

                    break;
            }

            return false;
        }

        private float HoldOf(ReadoutKind kind)
        {
            switch (kind)
            {
                case ReadoutKind.Story:
                    return _settings.StoryHoldSeconds;
                case ReadoutKind.ScanSet:
                    return _settings.ScanSetHoldSeconds;
                default:
                    return _settings.StatusHoldSeconds;
            }
        }
    }
}
