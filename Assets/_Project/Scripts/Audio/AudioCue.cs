using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// One sound cue of the <see cref="AudioLibrary"/>: its variant clips, bus, 2D/3D, looping, and the random
    /// volume/pitch ranges applied per play (tonal cues keep pitch 1 so they stay in key).
    /// </summary>
    [Serializable]
    public sealed class AudioCue
    {
        [Tooltip("Stable id from tools/audio/sfx_manifest.json.")]
        [SerializeField] private string _id = string.Empty;

        [Tooltip("Variants; a random one plays each time (never the same one twice in a row).")]
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();

        [SerializeField] private AudioBus _bus = AudioBus.Sfx;

        [Tooltip("3D cues play at a world position; 2D cues play flat (UI, radio, ambience).")]
        [SerializeField] private bool _spatial;

        [SerializeField] private bool _loop;

        [Range(0f, 1f)] [SerializeField] private float _volumeMin = 1f;
        [Range(0f, 1f)] [SerializeField] private float _volumeMax = 1f;
        [Range(0.25f, 4f)] [SerializeField] private float _pitchMin = 1f;
        [Range(0.25f, 4f)] [SerializeField] private float _pitchMax = 1f;

        internal AudioCue(string id, AudioClip[] clips, AudioBus bus, bool spatial, bool loop, float volumeMin,
            float volumeMax, float pitchMin, float pitchMax)
        {
            _id = id;
            _clips = clips;
            _bus = bus;
            _spatial = spatial;
            _loop = loop;
            _volumeMin = volumeMin;
            _volumeMax = volumeMax;
            _pitchMin = pitchMin;
            _pitchMax = pitchMax;
        }

        public string Id => _id;

        public int ClipCount => _clips.Length;

        public AudioBus Bus => _bus;

        public bool Spatial => _spatial;

        public bool Loop => _loop;

        public float VolumeMin => _volumeMin;

        public float VolumeMax => _volumeMax;

        public float PitchMin => _pitchMin;

        public float PitchMax => _pitchMax;

        public AudioClip GetClip(int index)
        {
            return _clips[index];
        }

        /// <summary>First wiring problem of this cue, or null when it is playable.</summary>
        public string FindProblem()
        {
            if (string.IsNullOrEmpty(_id))
            {
                return "a cue has no id";
            }

            if (_clips == null || _clips.Length == 0)
            {
                return $"cue '{_id}' has no clips";
            }

            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] == null)
                {
                    return $"cue '{_id}' clip {i} is missing";
                }
            }

            if (_volumeMin > _volumeMax || _pitchMin > _pitchMax)
            {
                return $"cue '{_id}' has an inverted volume or pitch range";
            }

            return null;
        }
    }
}
