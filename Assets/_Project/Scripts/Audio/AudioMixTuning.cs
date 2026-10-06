using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Mix-wide audio tuning: default bus volumes, one-shot voice pools and 3D rolloff, the landing thump response
    /// and the ambience bed. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class AudioMixTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Buses (defaults until the player changes them)")]
        [Tooltip("Master volume multiplied into every bus.")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;

        [Tooltip("Radio: music, static and the tuning swish.")]
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.8f;

        [Tooltip("One-shots and rover/tether loops.")]
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        [Tooltip("The lunar ambience bed.")]
        [Range(0f, 1f)] [SerializeField] private float _ambienceVolume = 0.8f;

        [Header("Voices")]
        [Tooltip("Pooled 3D one-shot voices; when all are busy the one finishing soonest is reused.")]
        [Range(4, 64)] [SerializeField] private int _spatialVoices = 16;

        [Tooltip("Pooled 2D (UI, stingers) one-shot voices.")]
        [Range(2, 32)] [SerializeField] private int _flatVoices = 8;

        [Header("3D rolloff")]
        [Tooltip("Curve used by every 3D voice and loop.")]
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;

        [Tooltip("Metres within which 3D sounds play at full volume (the camera sits ~8-12 m from the rover).")]
        [Min(0.1f)] [SerializeField] private float _minDistance = 6f;

        [Tooltip("Metres beyond which 3D sounds stop getting quieter.")]
        [Min(1f)] [SerializeField] private float _maxDistance = 120f;

        [Tooltip("Pitch shift from relative velocity. 0 keeps every tonal cue in key.")]
        [Range(0f, 1f)] [SerializeField] private float _dopplerLevel;

        [Tooltip("Stereo spread of 3D voices in degrees; a little width keeps close sounds soft.")]
        [Range(0f, 360f)] [SerializeField] private float _spread = 30f;

        [Header("Landing thump")]
        [Tooltip("Downward impact speed (m/s) below which a landing makes no thump.")]
        [Min(0f)] [SerializeField] private float _thumpMinImpact = 0.6f;

        [Tooltip("Impact speed (m/s) that gives the loudest, deepest thump (a ~5 m lunar drop is ~4 m/s).")]
        [Min(0.1f)] [SerializeField] private float _thumpFullImpact = 4f;

        [Tooltip("Volume scale of the softest audible thump.")]
        [Range(0f, 1f)] [SerializeField] private float _thumpSoftVolume = 0.3f;

        [Tooltip("Volume scale of a full-impact thump.")]
        [Range(0f, 1f)] [SerializeField] private float _thumpHardVolume = 1f;

        [Tooltip("Pitch scale of the softest thump (lighter = slightly higher).")]
        [Range(0.5f, 1.5f)] [SerializeField] private float _thumpSoftPitch = 1.05f;

        [Tooltip("Pitch scale of a full-impact thump (heavier = lower).")]
        [Range(0.5f, 1.5f)] [SerializeField] private float _thumpHardPitch = 0.9f;

        [Header("Ambience")]
        [Tooltip("Seconds the ambience bed takes to fade in at start.")]
        [Min(0f)] [SerializeField] private float _ambienceFadeIn = 4f;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public float AmbienceVolume => _ambienceVolume;
        public int SpatialVoices => _spatialVoices;
        public int FlatVoices => _flatVoices;
        public AudioRolloffMode RolloffMode => _rolloffMode;
        public float MinDistance => _minDistance;
        public float MaxDistance => Mathf.Max(_maxDistance, _minDistance + MinSpan);
        public float DopplerLevel => _dopplerLevel;
        public float Spread => _spread;
        public float ThumpMinImpact => _thumpMinImpact;
        public float ThumpFullImpact => Mathf.Max(_thumpFullImpact, _thumpMinImpact + MinSpan);
        public float ThumpSoftVolume => _thumpSoftVolume;
        public float ThumpHardVolume => _thumpHardVolume;
        public float ThumpSoftPitch => _thumpSoftPitch;
        public float ThumpHardPitch => _thumpHardPitch;
        public float AmbienceFadeIn => _ambienceFadeIn;
    }
}
