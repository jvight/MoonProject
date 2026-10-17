using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The title screen's procedural music variants (built from <c>tools/music/title.json</c> by the
    /// <c>Audio/TitleMusic</c> builder) and the data-driven choice of which variant plays on the title screen,
    /// together with its fade-in (~2 s) and fade-out (0.8 s) durations.
    /// </summary>
    public sealed class TitleMusicLibrary : ScriptableObject
    {
        public const string DefaultTrackId = "title_a_turning_moon";

        [Tooltip("The title theme variants rendered by tools/music/build_music.py --title.")]
        [SerializeField] private RadioTrack[] _tracks = Array.Empty<RadioTrack>();

        [Tooltip("ID of the title theme variant that plays on the title screen (defaults to Variant A).")]
        [SerializeField] private string _selectedId = DefaultTrackId;

        [Tooltip("Seconds for the title theme to fade in when the title screen appears.")]
        [Min(0.01f)] [SerializeField] private float _fadeInSeconds = 2f;

        [Tooltip("Seconds for the title theme to fade out on Continue or New Game.")]
        [Min(0.01f)] [SerializeField] private float _fadeOutSeconds = 0.8f;

        [Tooltip("Gain applied on top of the Music bus (tracks are mastered to -20 LUFS).")]
        [Range(0f, 1f)] [SerializeField] private float _volume = 1f;

        public int Count => _tracks != null ? _tracks.Length : 0;
        public string SelectedId => _selectedId;
        public float FadeInSeconds => _fadeInSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
        public float Volume => _volume;

        public RadioTrack GetTrack(int index)
        {
            return _tracks[index];
        }

        public bool TryGetTrack(string id, out RadioTrack track)
        {
            if (_tracks != null && !string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < _tracks.Length; i++)
                {
                    RadioTrack candidate = _tracks[i];
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        track = candidate;
                        return true;
                    }
                }
            }

            track = null;
            return false;
        }

        /// <summary>The variant selected by <see cref="SelectedId"/>, or the first track if the ID is not found.</summary>
        public RadioTrack SelectedTrack
        {
            get
            {
                if (TryGetTrack(_selectedId, out RadioTrack chosen))
                {
                    return chosen;
                }

                return _tracks != null && _tracks.Length > 0 ? _tracks[0] : null;
            }
        }

        public void SelectVariant(string id)
        {
            _selectedId = id ?? string.Empty;
        }

        public void Populate(RadioTrack[] tracks, string selectedId = DefaultTrackId, float fadeInSeconds = 2f,
            float fadeOutSeconds = 0.8f, float volume = 1f)
        {
            _tracks = tracks ?? Array.Empty<RadioTrack>();
            _selectedId = string.IsNullOrEmpty(selectedId) ? DefaultTrackId : selectedId;
            _fadeInSeconds = Mathf.Max(0.01f, fadeInSeconds);
            _fadeOutSeconds = Mathf.Max(0.01f, fadeOutSeconds);
            _volume = Mathf.Clamp01(volume);
        }

        /// <summary>Returns the first structural problem found, or null when the library is ready to play.</summary>
        public string FindProblem()
        {
            if (_tracks == null || _tracks.Length == 0)
            {
                return "title music library is empty";
            }

            for (int i = 0; i < _tracks.Length; i++)
            {
                RadioTrack track = _tracks[i];
                if (track == null || string.IsNullOrEmpty(track.Id) || track.Clip == null)
                {
                    return $"title track {i} ('{track?.Id}') has no id or clip";
                }
            }

            if (!TryGetTrack(_selectedId, out _))
            {
                return $"selected title track '{_selectedId}' is not in the library";
            }

            return null;
        }
    }
}
