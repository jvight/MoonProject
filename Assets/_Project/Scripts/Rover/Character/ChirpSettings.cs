using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// When 07 speaks machine and how its body moves with the chirp (docs/design/feel-pass.md §7.2), plus the full
    /// dock's sigh (§7.1).
    /// </summary>
    [Serializable]
    public sealed class ChirpSettings
    {
        [Header("When 07 chirps")]
        [Tooltip("Seconds after any chirp before 07 chirps again.")]
        [Range(0f, 30f)]
        [SerializeField] private float _globalCooldown = 6f;

        [Tooltip("Seconds before the same mood is chirped again.")]
        [Range(0f, 120f)]
        [SerializeField] private float _moodCooldown = 30f;

        [Tooltip("Seconds before Affirm is chirped again (it answers frequent small successes).")]
        [Range(0f, 30f)]
        [SerializeField] private float _affirmCooldown = 3f;

        [Tooltip("Seconds a requested chirp counts as sounding until its syllables say otherwise (one at a time).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _phraseSeconds = 1.2f;

        [Tooltip("Battery share at or under which 07 arriving at the dock chirps Relieved instead of Affirm.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _relievedBelow = 0.15f;

        [Header("Body with each syllable")]
        [Tooltip("Eye glow pulse per syllable (0.25 = +25%).")]
        [Range(0f, 1f)]
        [SerializeField] private float _eyePulse = 0.25f;

        [Tooltip("Seconds the eye pulse lasts.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _eyePulseSeconds = 0.08f;

        [Tooltip("Antenna flick per syllable (deg/s kick).")]
        [Range(0f, 300f)]
        [SerializeField] private float _antennaFlick = 60f;

        [Header("Mood poses")]
        [Tooltip("Curious: the head tilts sideways (deg).")]
        [Range(0f, 20f)]
        [SerializeField] private float _curiousTilt = 8f;

        [Tooltip("Worried: the head dips (deg).")]
        [Range(0f, 15f)]
        [SerializeField] private float _worriedDip = 4f;

        [Tooltip("Tired: the head lowers slowly (deg).")]
        [Range(0f, 20f)]
        [SerializeField] private float _tiredLower = 7f;

        [Tooltip("Happy: a small bounce per syllable (upward chassis kick, m/s).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _happyBounce = 0.12f;

        [Tooltip("Half-life (s) of a pose easing in and out.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _poseHalfLife = 0.15f;

        [Tooltip("Half-life (s) of Tired's slow lowering.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _tiredHalfLife = 0.8f;

        [Tooltip("Seconds a pose holds after the phrase's last syllable before easing back.")]
        [Range(0f, 3f)]
        [SerializeField] private float _poseHold = 0.6f;

        [Header("The full dock's sigh")]
        [Tooltip("The wing opens this far (deg) as the battery fills at the dock.")]
        [Range(0f, 30f)]
        [SerializeField] private float _sighWing = 10f;

        [Tooltip("The eye brightens by this share at the sigh's peak.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sighEye = 0.35f;

        [Tooltip("Half-life (s) of the sigh settling back.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _sighHalfLife = 0.9f;

        public float GlobalCooldown => _globalCooldown;

        public float MoodCooldown => _moodCooldown;

        public float AffirmCooldown => _affirmCooldown;

        public float PhraseSeconds => _phraseSeconds;

        public float RelievedBelow => _relievedBelow;

        public float EyePulse => _eyePulse;

        public float EyePulseSeconds => _eyePulseSeconds;

        public float AntennaFlick => _antennaFlick;

        public float CuriousTilt => _curiousTilt;

        public float WorriedDip => _worriedDip;

        public float TiredLower => _tiredLower;

        public float HappyBounce => _happyBounce;

        public float PoseHalfLife => _poseHalfLife;

        public float TiredHalfLife => _tiredHalfLife;

        public float PoseHold => _poseHold;

        public float SighWing => _sighWing;

        public float SighEye => _sighEye;

        public float SighHalfLife => _sighHalfLife;
    }
}
