using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>One rendered chirp: where each syllable starts, how high it sits (0..1), how long it lasts.</summary>
    [Serializable]
    public sealed class ChirpVariant
    {
        [SerializeField] private float[] _onsets = Array.Empty<float>();
        [SerializeField] private float[] _pitches = Array.Empty<float>();
        [SerializeField] private float[] _lengths = Array.Empty<float>();

        public ChirpVariant(float[] onsets, float[] pitches, float[] lengths)
        {
            _onsets = onsets ?? throw new ArgumentNullException(nameof(onsets));
            _pitches = pitches ?? throw new ArgumentNullException(nameof(pitches));
            _lengths = lengths ?? throw new ArgumentNullException(nameof(lengths));
        }

        public int SyllableCount => _onsets.Length;

        /// <summary>Seconds from the chirp's start to the end of its last syllable.</summary>
        public float Seconds => _onsets.Length == 0 ? 0f : _onsets[_onsets.Length - 1] + _lengths[_lengths.Length - 1];

        public float Onset(int index) => _onsets[index];

        public float Pitch(int index) => _pitches[index];

        public float Length(int index) => _lengths[index];

        /// <summary>First data problem (mismatched arrays, onsets out of order, pitch outside 0..1), or null.</summary>
        public string FindProblem()
        {
            if (_onsets.Length == 0 || _pitches.Length != _onsets.Length || _lengths.Length != _onsets.Length)
            {
                return "syllable arrays are empty or of different lengths";
            }

            for (int i = 0; i < _onsets.Length; i++)
            {
                if (_lengths[i] <= 0f || _pitches[i] < 0f || _pitches[i] > 1f ||
                    (i > 0 && _onsets[i] < _onsets[i - 1] + _lengths[i - 1]))
                {
                    return $"syllable {i} overlaps, has no length or a pitch outside 0..1";
                }
            }

            return null;
        }
    }
}
