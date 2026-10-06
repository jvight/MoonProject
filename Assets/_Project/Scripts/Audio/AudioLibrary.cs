using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Every sound cue of the game, generated from tools/audio/sfx_manifest.json by the Audio/Library builder.
    /// Systems resolve ids to <see cref="CueHandle"/>s once at initialisation; playback then indexes an array.
    /// </summary>
    public sealed class AudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioCue[] _cues = Array.Empty<AudioCue>();

        private Dictionary<string, int> _index;

        public int CueCount => _cues.Length;

        public AudioCue GetCue(CueHandle handle)
        {
            if (!handle.IsValid || handle.Index >= _cues.Length)
            {
                throw new ArgumentException("Invalid cue handle.", nameof(handle));
            }

            return _cues[handle.Index];
        }

        public AudioCue GetCueAt(int index)
        {
            return _cues[index];
        }

        /// <summary>Looks up <paramref name="id"/>; allocates the index on first use only.</summary>
        public bool TryResolve(string id, out CueHandle handle)
        {
            if (_index == null)
            {
                _index = new Dictionary<string, int>(_cues.Length, StringComparer.Ordinal);
                for (int i = 0; i < _cues.Length; i++)
                {
                    if (_cues[i] != null && !string.IsNullOrEmpty(_cues[i].Id))
                    {
                        _index[_cues[i].Id] = i;
                    }
                }
            }

            if (id != null && _index.TryGetValue(id, out int found))
            {
                handle = new CueHandle(found);
                return true;
            }

            handle = default;
            return false;
        }

        /// <summary>First wiring problem (missing clip, duplicate id...), or null when every cue is playable.</summary>
        public string FindProblem()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _cues.Length; i++)
            {
                if (_cues[i] == null)
                {
                    return $"cue slot {i} is empty";
                }

                string problem = _cues[i].FindProblem();
                if (problem != null)
                {
                    return problem;
                }

                if (!seen.Add(_cues[i].Id))
                {
                    return $"cue id '{_cues[i].Id}' is duplicated";
                }
            }

            return null;
        }

        internal void Populate(AudioCue[] cues)
        {
            _cues = cues ?? throw new ArgumentNullException(nameof(cues));
            _index = null;
        }

        private void OnValidate()
        {
            _index = null;
            string problem = FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(AudioLibrary)} '{name}': {problem}. Re-run MoonProject/Build/Audio/Library.",
                    this);
            }
        }
    }
}
