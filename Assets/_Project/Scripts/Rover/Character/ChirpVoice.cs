using System;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Whether 07 may chirp now (docs/design/feel-pass.md §7.2): one chirp at a time, a global cooldown after any
    /// chirp, and a cooldown per mood (shorter for Affirm). Plain logic; times are the caller's clock in seconds.
    /// </summary>
    public sealed class ChirpVoice
    {
        private const int MoodCount = 8;

        private readonly ChirpSettings _settings;
        private readonly float[] _lastByMood = new float[MoodCount];
        private float _last = float.NegativeInfinity;
        private float _soundingUntil = float.NegativeInfinity;

        public ChirpVoice(ChirpSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            for (int i = 0; i < MoodCount; i++)
            {
                _lastByMood[i] = float.NegativeInfinity;
            }
        }

        /// <summary>Whether a chirp is sounding at <paramref name="now"/>.</summary>
        public bool IsSounding(float now)
        {
            return now < _soundingUntil;
        }

        /// <summary>True (and the chirp is booked) when <paramref name="mood"/> may sound at <paramref
        /// name="now"/>.</summary>
        public bool TryChirp(ChirpMood mood, float now)
        {
            int index = (int)mood;
            if (index < 0 || index >= MoodCount || IsSounding(now) || now - _last < _settings.GlobalCooldown)
            {
                return false;
            }

            float cooldown = mood == ChirpMood.Affirm ? _settings.AffirmCooldown : _settings.MoodCooldown;
            if (now - _lastByMood[index] < cooldown)
            {
                return false;
            }

            _last = now;
            _lastByMood[index] = now;
            _soundingUntil = now + _settings.PhraseSeconds;
            return true;
        }

        /// <summary>A syllable started: the chirp sounds at least until it ends (the last one ends the
        /// phrase).</summary>
        public void Syllable(int index, int count, float seconds, float now)
        {
            float end = now + seconds;
            _soundingUntil = index >= count - 1 ? end : Math.Max(_soundingUntil, end);
        }
    }
}
