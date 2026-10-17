using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One radio track: a track of the <see cref="RadioPlaylist"/> (tools/music/playlist.json) or a title theme of the
    /// <see cref="TitleMusicLibrary"/> (tools/music/title.json).
    /// </summary>
    [Serializable]
    public sealed class RadioTrack
    {
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _title = string.Empty;
        [SerializeField] private AudioClip _clip;
        [Min(0f)] [SerializeField] private float _bpm;

        internal RadioTrack(string id, string title, AudioClip clip, float bpm)
        {
            _id = id;
            _title = title;
            _clip = clip;
            _bpm = bpm;
        }

        /// <summary>Track id: its title is localized under <c>track.&lt;id&gt;.title</c>.</summary>
        public string Id => _id;

        public string Title => _title;

        public AudioClip Clip => _clip;

        public float Bpm => _bpm;
    }
}
