using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the 0.5.2 feel beats sound (docs/design/feel-pass.md): the cut, the dock's lock, 07's chirps and the first
    /// hour's small moments. Levels are shares of each cue's own level (the cues carry the loudness table).
    /// Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class FeelAudioTuning : ScriptableObject
    {
        [Header("The cut (feel-pass section 3.1)")]
        [Tooltip("Charge at least this long (s) plays the fresh four-note arpeggio; shorter ones (a resume) the " +
            "two-note.")]
        [Range(0.1f, 1f)] [SerializeField] private float _freshChargeSeconds = 0.45f;

        [Tooltip("Level of the sizzle through 07's body at full cut progress.")]
        [Range(0f, 1f)] [SerializeField] private float _sizzleMax = 0.8f;

        [Tooltip("Level of the sizzle right after ignition (progress 0).")]
        [Range(0f, 1f)] [SerializeField] private float _sizzleMin = 0.3f;

        [Tooltip("Time constant (s) of the sizzle easing to its level, and away when the beam stops.")]
        [Min(0.01f)] [SerializeField] private float _sizzleEase = 0.15f;

        [Header("07 speaks machine (feel-pass section 7.2)")]
        [Tooltip("Level of 07's chirps (mezzo-piano).")]
        [Range(0f, 1f)] [SerializeField] private float _chirpVolume = 0.8f;

        [Tooltip("Share of the chirp level while the radio plays music: the chirps sit under the radio.")]
        [Range(0f, 1f)] [SerializeField] private float _chirpUnderRadio = 0.7f;

        [Tooltip("Seconds a chirp asked for during a voice line (or another chirp) waits before it is dropped.")]
        [Min(0f)] [SerializeField] private float _chirpHoldSeconds = 3f;

        [Header("The first hour's small moments (polish-pass section 1.1)")]
        [Tooltip("Shortest time (s) between two save ticks: one tick per burst of saves.")]
        [Min(0f)] [SerializeField] private float _saveTickInterval = 3f;

        [Tooltip("Time constant (s) of the drums' boost whine easing in and out.")]
        [Min(0.01f)] [SerializeField] private float _boostEase = 0.12f;

        [Tooltip("Level of the drums' boost whine.")]
        [Range(0f, 1f)] [SerializeField] private float _boostVolume = 0.7f;

        public float FreshChargeSeconds => _freshChargeSeconds;
        public float SizzleMax => _sizzleMax;
        public float SizzleMin => Mathf.Min(_sizzleMin, _sizzleMax);
        public float SizzleEase => _sizzleEase;
        public float ChirpVolume => _chirpVolume;
        public float ChirpUnderRadio => _chirpUnderRadio;
        public float ChirpHoldSeconds => _chirpHoldSeconds;
        public float SaveTickInterval => _saveTickInterval;
        public float BoostEase => _boostEase;
        public float BoostVolume => _boostVolume;
    }
}
