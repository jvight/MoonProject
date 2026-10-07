using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One radio track: a base track of the <see cref="RadioPlaylist"/> (tools/music/playlist.json) or a cassette's
    /// track in the <see cref="RadioTapeLibrary"/> (tools/music/tapes.json, <see cref="TapeId"/> set).
    /// </summary>
    [Serializable]
    public sealed class RadioTrack
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _title = string.Empty;
        [SerializeField] private AudioClip _clip;
        [Min(0f)] [SerializeField] private float _bpm;

        [Tooltip("Cassette id this track comes from; empty for the base tracks.")]
        [SerializeField] private string _tapeId = string.Empty;

        internal RadioTrack(string id, string title, AudioClip clip, float bpm, string tapeId = "")
        {
            _id = id;
            _title = title;
            _clip = clip;
            _bpm = bpm;
            _tapeId = tapeId ?? string.Empty;
        }

        /// <summary>Track id: its title is localized under <c>track.&lt;id&gt;.title</c>.</summary>
        public string Id => _id;

        public string Title => _title;

        public AudioClip Clip => _clip;

        public float Bpm => _bpm;

        public string TapeId => _tapeId;
    }
}
