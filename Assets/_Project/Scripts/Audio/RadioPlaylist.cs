using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio station's tracks, generated from the music box's tools/music/playlist.json by the
    /// Audio/Radio Playlist builder.
    /// </summary>
    public sealed class RadioPlaylist : ScriptableObject
    {
        [SerializeField] private RadioTrack[] _tracks = Array.Empty<RadioTrack>();

        public int Count => _tracks.Length;

        public RadioTrack GetTrack(int index)
        {
            return _tracks[index];
        }

        /// <summary>First wiring problem, or null when every track is playable.</summary>
        public string FindProblem()
        {
            if (_tracks.Length == 0)
            {
                return "the playlist is empty (render the music, then run MoonProject/Build/Audio/Radio Playlist)";
            }

            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i] == null || _tracks[i].Clip == null)
                {
                    return $"track {i} has no clip";
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
