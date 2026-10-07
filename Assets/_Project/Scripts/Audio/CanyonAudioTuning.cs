using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How Whispering Canyon sounds (feel pillar 6: far from home, wide, airy, sparse): where "inside" begins along
    /// the World's canyon anchors, the whisper and chasm-trough beds, the basin bed and radio thinning inside (applied
    /// by the soundscape's mix) and a gentle echo on 07's own sounds. Created by the Audio/Tuning builder; runtime code
    /// only reads it.
    /// </summary>
    public sealed class CanyonAudioTuning : ScriptableObject
    {
        [Header("Where the canyon is (from the World's anchors)")]
        [Tooltip("Metres either side of the anchor corridor that count fully as inside (the canyon is 12-25 m wide; " +
                 "alcoves sit off to the side).")]
        [Range(4f, 40f)] [SerializeField] private float _halfWidth = 16f;

        [Tooltip("Metres beyond the half width over which 'inside' eases to nothing (no edge you can hear).")]
        [Range(1f, 40f)] [SerializeField] private float _edge = 12f;

        [Tooltip("Metres from the mouth (or the exit) over which the canyon fades in as 07 drives in.")]
        [Range(1f, 80f)] [SerializeField] private float _entry = 30f;

        [Tooltip("Seconds (time constant) for the canyon's sound to follow 07 in and out: slow enough to never jump.")]
        [Range(0.1f, 10f)] [SerializeField] private float _ease = 2.5f;

        [Header("Chasm trough")]
        [Tooltip("Metres below the lip-to-landing line before the trough's darker bed starts.")]
        [Range(0f, 20f)] [SerializeField] private float _troughDepthStart = 2f;

        [Tooltip("Metres further down over which the trough bed comes fully in.")]
        [Range(0.5f, 30f)] [SerializeField] private float _troughDepthRange = 6f;

        [Header("Beds")]
        [Tooltip("Whisper bed level deep inside (scales the cue's own volume).")]
        [Range(0f, 1f)] [SerializeField] private float _whisperGain = 1f;

        [Tooltip("Trough bed level at the bottom of the chasm (the whisper gives way to it).")]
        [Range(0f, 1f)] [SerializeField] private float _troughGain = 1f;

        [Tooltip("The basin's ambience bed level deep inside: the open crater recedes, the canyon's air takes over.")]
        [Range(0f, 1f)] [SerializeField] private float _basinBedInside = 0.55f;

        [Header("Radio inside (thinner, far from home)")]
        [Tooltip("Music level deep inside the canyon (on top of the signal's own distance falloff).")]
        [Range(0f, 1f)] [SerializeField] private float _radioMusicInside = 0.6f;

        [Tooltip("Low-pass (Hz) the radio thins to deep inside: a small, distant set.")]
        [Range(500f, 22000f)] [SerializeField] private float _radioCutoffInside = 2400f;

        [Header("Echo on 07's sounds (3D sources; 2D radio, UI and beds bypass it)")]
        [Tooltip("Reverb room level deep inside (dB; it fades with 'inside', to off outside).")]
        [Range(-30f, 0f)] [SerializeField] private float _roomDb = -9f;

        [Tooltip("High-frequency room level (dB): rock walls give back a darker echo.")]
        [Range(-30f, 0f)] [SerializeField] private float _roomHfDb = -8f;

        [Tooltip("Reverb decay time (s).")]
        [Range(0.1f, 10f)] [SerializeField] private float _decayTime = 2.6f;

        [Tooltip("High-frequency decay ratio (below 1: the highs die first).")]
        [Range(0.1f, 2f)] [SerializeField] private float _decayHfRatio = 0.5f;

        [Tooltip("Early reflection level (dB): the first slap back off the canyon walls.")]
        [Range(-30f, 10f)] [SerializeField] private float _reflectionsDb = -12f;

        [Tooltip("Seconds before the first reflection (the walls are ~10 m away).")]
        [Range(0f, 0.3f)] [SerializeField] private float _reflectionsDelay = 0.06f;

        [Tooltip("Late reverb level (dB) relative to the room.")]
        [Range(-30f, 20f)] [SerializeField] private float _reverbDb = -4f;

        [Tooltip("Seconds between the first reflection and the late reverb.")]
        [Range(0f, 0.1f)] [SerializeField] private float _reverbDelay = 0.08f;

        [Tooltip("Echo density smearing (%): lower keeps the canyon's echo a little discrete.")]
        [Range(0f, 100f)] [SerializeField] private float _diffusion = 60f;

        [Tooltip("Modal density (%).")]
        [Range(0f, 100f)] [SerializeField] private float _density = 70f;

        public float HalfWidth => _halfWidth;
        public float Edge => _edge;
        public float Entry => _entry;
        public float Ease => _ease;
        public float TroughDepthStart => _troughDepthStart;
        public float TroughDepthRange => _troughDepthRange;
        public float WhisperGain => _whisperGain;
        public float TroughGain => _troughGain;
        public float BasinBedInside => _basinBedInside;
        public float RadioMusicInside => _radioMusicInside;
        public float RadioCutoffInside => _radioCutoffInside;
        public float RoomDb => _roomDb;
        public float RoomHfDb => _roomHfDb;
        public float DecayTime => _decayTime;
        public float DecayHfRatio => _decayHfRatio;
        public float ReflectionsDb => _reflectionsDb;
        public float ReflectionsDelay => _reflectionsDelay;
        public float ReverbDb => _reverbDb;
        public float ReverbDelay => _reverbDelay;
        public float Diffusion => _diffusion;
        public float Density => _density;
    }
}
