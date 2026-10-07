using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Every cassette's radio track, keyed by cassette id, generated from the music box's tools/music/tapes.json by
    /// the Audio/Radio Tapes builder. Which tapes 07 owns comes from <see cref="MoonProject.Core.IRadioProgram"/>.
    /// </summary>
    public sealed class RadioTapeLibrary : ScriptableObject
    {
        [SerializeField] private RadioTrack[] _tracks = Array.Empty<RadioTrack>();

        public int Count => _tracks.Length;

        public RadioTrack GetTrack(int index)
        {
            return _tracks[index];
        }

        /// <summary>First wiring problem (missing clip, missing or duplicate tape id), or null when playable.</summary>
        public string FindProblem()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _tracks.Length; i++)
            {
                RadioTrack track = _tracks[i];
                if (track == null || track.Clip == null)
                {
                    return $"tape track {i} has no clip";
                }

                if (string.IsNullOrEmpty(track.TapeId) || !seen.Add(track.TapeId))
                {
                    return $"tape track '{track.Id}' has a missing or duplicate cassette id '{track.TapeId}'";
                }
            }

            return null;
        }

        internal void Populate(RadioTrack[] tracks)
        {
            _tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
        }
    }
}
