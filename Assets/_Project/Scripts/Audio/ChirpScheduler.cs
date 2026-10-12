using System;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// When 07's chirps may sound (pure, EditMode-tested; feel-pass section 7.2). One chirp at a time; never over a
    /// voice line or a letter: a request made while one plays (or while a chirp still speaks) waits up to the hold
    /// time, then is dropped; a newer request replaces a waiting one. Variants are picked at random, never the same
    /// one twice in a row for a mood. 07's character (Rover) owns the cooldowns and which mood to ask for.
    /// </summary>
    public sealed class ChirpScheduler
    {
        private readonly AudioRandom _random;
        private readonly int[] _lastVariant = new int[ChirpBank.MoodCount];
        private bool _waiting;
        private ChirpMood _mood;
        private float _waited;

        public ChirpScheduler(AudioRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            for (int i = 0; i < _lastVariant.Length; i++)
            {
                _lastVariant[i] = -1;
            }
        }

        /// <summary>True while a request waits for the voice (or the chirp before it) to finish.</summary>
        public bool IsWaiting => _waiting;

        /// <summary>Requests that ran out of hold time.</summary>
        public int Dropped { get; private set; }

        public void Request(ChirpMood mood)
        {
            _waiting = true;
            _mood = mood;
            _waited = 0f;
        }

        /// <summary>
        /// Advances the wait. Returns true with the mood to speak now when nothing else is speaking; drops the request
        /// once it has waited longer than <paramref name="holdSeconds"/>.
        /// </summary>
        public bool Step(float deltaTime, bool busy, float holdSeconds, out ChirpMood mood)
        {
            mood = _mood;
            if (!_waiting)
            {
                return false;
            }

            if (!busy)
            {
                _waiting = false;
                return true;
            }

            _waited += deltaTime;
            if (_waited > holdSeconds)
            {
                _waiting = false;
                Dropped++;
            }

            return false;
        }

        /// <summary>A variant of <paramref name="mood"/> (of <paramref name="count"/>), never the last again.</summary>
        public int PickVariant(ChirpMood mood, int count)
        {
            int index = (int)mood;
            int variant = _random.PickAvoiding(count, _lastVariant[index]);
            _lastVariant[index] = variant;
            return variant;
        }
    }
}
