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
        [Tooltip("The beam's deeper undertone at cutter tiers 1, 2 and 3, as a share of its level (tier 0 has none: " +
            "the singing tone alone).")]
        [SerializeField] private Vector3 _undertoneByTier = new Vector3(0.35f, 0.6f, 0.9f);

        [Tooltip("The cutter tier from which the charge run gets its sustained low D (M3-26 section 5).")]
        [Range(0, 3)] [SerializeField] private int _lowDFromTier = 3;

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
        [Tooltip("Shortest time (s) between two stock count ticks: a big haul rolls on without a rattle (its last " +
            "unit always ticks).")]
        [Min(0f)] [SerializeField] private float _countTickInterval = 0.07f;

        [Tooltip("Shortest time (s) between two save ticks: one tick per burst of saves.")]
        [Min(0f)] [SerializeField] private float _saveTickInterval = 3f;

        [Tooltip("Time constant (s) of the drums' boost whine easing in and out.")]
        [Min(0.01f)] [SerializeField] private float _boostEase = 0.12f;

        [Tooltip("Level of the drums' boost whine.")]
        [Range(0f, 1f)] [SerializeField] private float _boostVolume = 0.7f;

        public int LowDFromTier => _lowDFromTier;

        /// <summary>The undertone's level at cutter tier <paramref name="tier"/> (0 at tier 0).</summary>
        public float UndertoneAt(int tier)
        {
            switch (tier)
            {
                case 0:
                    return 0f;
                case 1:
                    return Mathf.Clamp01(_undertoneByTier.x);
                case 2:
                    return Mathf.Clamp01(_undertoneByTier.y);
                default:
                    return Mathf.Clamp01(_undertoneByTier.z);
            }
        }

        public float SizzleMax => _sizzleMax;
        public float SizzleMin => Mathf.Min(_sizzleMin, _sizzleMax);
        public float SizzleEase => _sizzleEase;
        public float ChirpVolume => _chirpVolume;
        public float ChirpUnderRadio => _chirpUnderRadio;
        public float ChirpHoldSeconds => _chirpHoldSeconds;
        public float CountTickInterval => _countTickInterval;
        public float SaveTickInterval => _saveTickInterval;
        public float BoostEase => _boostEase;
        public float BoostVolume => _boostVolume;
    }
}
