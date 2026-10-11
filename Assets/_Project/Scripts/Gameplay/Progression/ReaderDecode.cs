using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The lab's reader as plain logic (docs/features/M3-19 §5): 07's beam feeds a blueprint tube into the slot; once
    /// it is in, the reader has taken it and decodes it, a moment rather than a timer. One tube at a time.
    /// </summary>
    public sealed class ReaderDecode
    {
        private readonly float _feedSeconds;
        private readonly float _decodeSeconds;
        private float _elapsed;

        /// <param name="feedSeconds">Seconds the beam takes to feed the tube in.</param>
        /// <param name="decodeSeconds">Seconds the reader takes to decode it.</param>
        public ReaderDecode(float feedSeconds, float decodeSeconds)
        {
            _feedSeconds = Math.Max(0f, feedSeconds);
            _decodeSeconds = Math.Max(0f, decodeSeconds);
        }

        public ReaderPhase Phase { get; private set; }

        /// <summary>The kit of the tube being fed or decoded, or the last one decoded.</summary>
        public string Kit { get; private set; }

        /// <summary>How far the decode has come, 0..1 (0 unless decoding).</summary>
        public float DecodeProgress => Phase == ReaderPhase.Decoding && _decodeSeconds > 0f
            ? Math.Min(1f, _elapsed / _decodeSeconds)
            : 0f;

        /// <summary>The beam starts feeding the tube of <paramref name="kit"/> in.</summary>
        public void Feed(string kit)
        {
            Start(ReaderPhase.Feeding, kit);
        }

        /// <summary>A tube saved in the reader decodes again from the start (a loaded game).</summary>
        public void Resume(string kit)
        {
            Start(ReaderPhase.Decoding, kit);
        }

        public ReaderStep Step(float deltaTime)
        {
            if (Phase == ReaderPhase.Idle)
            {
                return ReaderStep.None;
            }

            _elapsed += deltaTime;
            if (Phase == ReaderPhase.Feeding)
            {
                if (_elapsed < _feedSeconds)
                {
                    return ReaderStep.None;
                }

                Phase = ReaderPhase.Decoding;
                _elapsed = 0f;
                return ReaderStep.Taken;
            }

            if (_elapsed < _decodeSeconds)
            {
                return ReaderStep.None;
            }

            Phase = ReaderPhase.Idle;
            _elapsed = 0f;
            return ReaderStep.Decoded;
        }

        private void Start(ReaderPhase phase, string kit)
        {
            if (string.IsNullOrEmpty(kit))
            {
                throw new ArgumentException("The reader needs a tube's kit.", nameof(kit));
            }

            if (Phase != ReaderPhase.Idle)
            {
                throw new InvalidOperationException($"The reader is busy with '{Kit}'.");
            }

            Phase = phase;
            Kit = kit;
            _elapsed = 0f;
        }
    }
}
