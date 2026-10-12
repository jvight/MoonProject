using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>One voiced recording: the ticker line it speaks (by localization key) and its clip.</summary>
    [Serializable]
    public sealed class VoiceLine
    {
        [SerializeField] private string _key;
        [SerializeField] private string _voice;
        [SerializeField] private AudioClip _clip;

        public VoiceLine(string key, string voice, AudioClip clip)
        {
            _key = key;
            _voice = voice;
            _clip = clip;
        }

        /// <summary>The ticker line's localization key; the ticker shows it as the subtitle.</summary>
        public string Key => _key;

        /// <summary>Who speaks: "ro" or "station" (tools/voice/voices.json).</summary>
        public string Voice => _voice;

        public AudioClip Clip => _clip;
    }
}
