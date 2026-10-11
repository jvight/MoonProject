using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Every voiced radio line (Ro's letters and recordings, the automated station), generated from
    /// tools/voice/voice_manifest.json by the Audio/Voice Lines builder. Voices are English; the ticker's localized
    /// line is the subtitle in every language.
    /// </summary>
    public sealed class VoiceLibrary : ScriptableObject
    {
        [SerializeField] private VoiceLine[] _lines = Array.Empty<VoiceLine>();

        public int Count => _lines.Length;

        public VoiceLine GetLine(int index)
        {
            return _lines[index];
        }

        /// <summary>The recording for ticker line <paramref name="key"/>, or false when the line has none.</summary>
        public bool TryGet(string key, out VoiceLine line)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                if (string.Equals(_lines[i].Key, key, StringComparison.Ordinal))
                {
                    line = _lines[i];
                    return true;
                }
            }

            line = null;
            return false;
        }

        /// <summary>First wiring problem (missing clip, missing or duplicate key), or null when playable.</summary>
        public string FindProblem()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _lines.Length; i++)
            {
                VoiceLine line = _lines[i];
                if (line == null || line.Clip == null)
                {
                    return $"voice line {i} has no clip";
                }

                if (string.IsNullOrEmpty(line.Key) || !seen.Add(line.Key))
                {
                    return $"voice line {i} has a missing or duplicate key '{line.Key}'";
                }
            }

            return null;
        }

        internal void Populate(VoiceLine[] lines)
        {
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }
    }
}
