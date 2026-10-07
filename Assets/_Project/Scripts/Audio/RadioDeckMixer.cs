using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's music decks as levels only (no Unity): changing track or station fades the live deck out and,
    /// after a short delay, starts the incoming music on the quietest deck while it fades in, with a swell of static
    /// over the change. Three decks with independent levels mean a fast turn across several detents finds a silent
    /// deck; if every deck is still ringing the start waits for one to fall silent, so nothing is ever cut
    /// mid-sound. A newer change simply replaces a start still pending.
    /// </summary>
    public sealed class RadioDeckMixer
    {
        public const int DeckCount = 3;

        private readonly float[] _level = new float[DeckCount];
        private readonly float[] _target = new float[DeckCount];
        private readonly float[] _rate = new float[DeckCount];
        private bool _pending;
        private float _pendingDelay;
        private float _fadeIn;
        private float _swellTime;
        private float _swellDuration;

        /// <summary>The deck the current music plays on; -1 when nothing does.</summary>
        public int Live { get; private set; } = -1;

        /// <summary>True while a start is waiting for its moment in a change.</summary>
        public bool StartPending => _pending;

        /// <summary>0 at both ends, 1 in the middle of the latest change (extra static, the dial between
        /// stations).</summary>
        public float Swell => _swellDuration > 0f && _swellTime < _swellDuration
            ? Mathf.Sin(Mathf.PI * _swellTime / _swellDuration)
            : 0f;

        public float Level(int deck)
        {
            return _level[deck];
        }

        /// <summary>Begins a change: the live deck fades out over <paramref name="fadeOut"/> seconds; when
        /// <paramref name="startIncoming"/>, music starts after <paramref name="delay"/> and fades in over
        /// <paramref name="fadeIn"/>. The static swells over <paramref name="swell"/> seconds (0 = none).</summary>
        public void Change(bool startIncoming, float fadeOut, float delay, float fadeIn, float swell)
        {
            if (Live >= 0)
            {
                FadeTo(Live, 0f, fadeOut);
                Live = -1;
            }

            _pending = startIncoming;
            _pendingDelay = Mathf.Max(0f, delay);
            _fadeIn = fadeIn;
            if (swell > 0f)
            {
                _swellDuration = swell;
                _swellTime = 0f;
            }
        }

        /// <summary>Starts music at once on a silent deck, fading in over <paramref name="fadeIn"/> (wake-up,
        /// a silent state change).</summary>
        public int StartNow(float fadeIn)
        {
            _pending = false;
            int deck = Quietest();
            _level[deck] = 0f;
            FadeTo(deck, 1f, fadeIn);
            Live = deck;
            return deck;
        }

        /// <summary>
        /// Advances the fades. Returns true when the pending start fires; <paramref name="startDeck"/> is the deck
        /// to start the incoming music on (it is now live, rising from silence).
        /// </summary>
        public bool Step(float deltaTime, out int startDeck)
        {
            float dt = Mathf.Max(0f, deltaTime);

            // Chosen before this step's fades: a deck is free only once it was already silent, never mid-release.
            int silent = FindSilent();
            for (int i = 0; i < DeckCount; i++)
            {
                _level[i] = Mathf.MoveTowards(_level[i], _target[i], _rate[i] * dt);
            }

            _swellTime += dt;
            startDeck = -1;
            if (!_pending)
            {
                return false;
            }

            _pendingDelay -= dt;
            if (_pendingDelay > 0f || silent < 0)
            {
                return false;
            }

            _pending = false;
            FadeTo(silent, 1f, _fadeIn);
            Live = silent;
            startDeck = silent;
            return true;
        }

        /// <summary>True once deck <paramref name="deck"/> is silent and not live (its source may stop).</summary>
        public bool IsIdle(int deck)
        {
            return deck != Live && _level[deck] <= 0f && _target[deck] <= 0f;
        }

        private void FadeTo(int deck, float target, float seconds)
        {
            _target[deck] = target;
            if (seconds > 0f)
            {
                _rate[deck] = 1f / seconds;
                return;
            }

            _rate[deck] = 0f;
            _level[deck] = target;
        }

        private int FindSilent()
        {
            for (int i = 0; i < DeckCount; i++)
            {
                if (IsIdle(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private int Quietest()
        {
            int best = 0;
            for (int i = 1; i < DeckCount; i++)
            {
                if (i != Live && (_level[i] < _level[best] || best == Live))
                {
                    best = i;
                }
            }

            return best;
        }
    }
}
