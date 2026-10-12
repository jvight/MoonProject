using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's machine voice (docs/design/feel-pass.md section 7.2): per <see cref="ChirpMood"/>, the variants' syllable
    /// data, built from tools/audio/chirp_syllables.json by the Audio/Chirp Bank builder. The clips themselves are the
    /// AudioLibrary cues <c>chirp_&lt;mood&gt;</c>, one clip per variant in the same order.
    /// </summary>
    public sealed class ChirpBank : ScriptableObject
    {
        /// <summary>Moods there are (<see cref="ChirpMood"/> values 0 .. MoodCount-1).</summary>
        public const int MoodCount = 8;

        [SerializeField] private Mood[] _moods = Array.Empty<Mood>();

        public int VariantCount(ChirpMood mood) => _moods[(int)mood].Variants.Length;

        public ChirpVariant GetVariant(ChirpMood mood, int variant) => _moods[(int)mood].Variants[variant];

        /// <summary>The AudioLibrary cue id playing <paramref name="mood"/>.</summary>
        public string CueId(ChirpMood mood) => _moods[(int)mood].CueId;

        /// <summary>First data problem, or null when every mood has variants with valid syllables.</summary>
        public string FindProblem()
        {
            if (_moods.Length != MoodCount)
            {
                return $"the chirp bank has {_moods.Length} moods, {MoodCount} expected";
            }

            for (int m = 0; m < _moods.Length; m++)
            {
                Mood mood = _moods[m];
                if (mood == null || string.IsNullOrEmpty(mood.CueId) || mood.Variants.Length == 0)
                {
                    return $"chirp mood {(ChirpMood)m} has no cue or no variants";
                }

                for (int v = 0; v < mood.Variants.Length; v++)
                {
                    ChirpVariant variant = mood.Variants[v];
                    string problem = variant == null ? "missing" : variant.FindProblem();
                    if (problem != null)
                    {
                        return $"chirp {(ChirpMood)m} variant {v}: {problem}";
                    }
                }
            }

            return null;
        }

        internal void Populate(Mood[] moods)
        {
            _moods = moods ?? throw new ArgumentNullException(nameof(moods));
        }

        /// <summary>One mood's cue and variants.</summary>
        [Serializable]
        internal sealed class Mood
        {
            [SerializeField] private string _cueId;
            [SerializeField] private ChirpVariant[] _variants = Array.Empty<ChirpVariant>();

            public Mood(string cueId, ChirpVariant[] variants)
            {
                _cueId = cueId;
                _variants = variants ?? throw new ArgumentNullException(nameof(variants));
            }

            public string CueId => _cueId;

            public ChirpVariant[] Variants => _variants;
        }
    }
}
