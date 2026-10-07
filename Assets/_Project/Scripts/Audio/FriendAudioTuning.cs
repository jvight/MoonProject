using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How friend machines sound: rotor hum from their effort, the repair stitching rising over the repair, the boot
    /// and how often they chirp on their own. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class FriendAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Rotor")]
        [Tooltip("Rotor loop pitch at the lightest effort (just lifting off).")]
        [Range(0.25f, 2f)] [SerializeField] private float _rotorIdlePitch = 0.85f;

        [Tooltip("Rotor loop pitch when dashing (RotorSpeed 1).")]
        [Range(0.5f, 2f)] [SerializeField] private float _rotorDashPitch = 1.45f;

        [Tooltip("Rotor volume scale at the lightest effort (silent at RotorSpeed 0).")]
        [Range(0f, 1f)] [SerializeField] private float _rotorLowVolume = 0.5f;

        [Tooltip("Rotor volume scale when dashing.")]
        [Range(0f, 1f)] [SerializeField] private float _rotorHighVolume = 1f;

        [Tooltip("Seconds (time constant) for the rotors to spin up: pitch and volume ease in.")]
        [Range(0.01f, 3f)] [SerializeField] private float _rotorSpinUpTime = 0.6f;

        [Tooltip("Seconds (time constant) for the rotors to wind down.")]
        [Range(0.01f, 3f)] [SerializeField] private float _rotorSpinDownTime = 0.4f;

        [Header("Repair")]
        [Tooltip("Seconds for the stitching texture to fade in when the repair starts.")]
        [Min(0f)] [SerializeField] private float _stitchFadeIn = 0.5f;

        [Tooltip("Seconds for the stitching texture to fade out when the repair ends.")]
        [Min(0f)] [SerializeField] private float _stitchFadeOut = 0.6f;

        [Tooltip("Stitching volume scale as it begins (it swells to 1 over the rise time).")]
        [Range(0f, 1f)] [SerializeField] private float _stitchStartGain = 0.55f;

        [Tooltip("Semitones the stitching rises; stay in key from D: 2, 4, 7, 9 or 12.")]
        [Range(0f, 12f)] [SerializeField] private float _stitchRiseSemitones = 7f;

        [Tooltip("Seconds over which the stitching rises; match the friends' stitch time (Tilly's repairDuration " +
                 "is 3.5 s). It holds at the top if the stitching lasts longer.")]
        [Min(0.1f)] [SerializeField] private float _stitchRiseTime = 3.5f;

        [Header("Walking and dozing (friends with step / doze cues, e.g. Bell)")]
        [Tooltip("Metres of walking per foot tap.")]
        [Range(0.1f, 2f)] [SerializeField] private float _stepStride = 0.45f;

        [Tooltip("A move longer than this in one frame (metres) is a placement, not walking: no steps.")]
        [Min(0.5f)] [SerializeField] private float _stepTeleportDistance = 3f;

        [Tooltip("Seconds for the doze hum to fade in once napping starts.")]
        [Min(0.01f)] [SerializeField] private float _dozeFadeIn = 2.5f;

        [Tooltip("Seconds for the doze hum to fade out on waking.")]
        [Min(0.01f)] [SerializeField] private float _dozeFadeOut = 0.8f;

        [Header("Ambient chirps (seconds between, random in range)")]
        [Tooltip("Shortest gap between curious chirps while following or spotting.")]
        [Min(0.5f)] [SerializeField] private float _followingChirpMin = 9f;

        [Tooltip("Longest gap between curious chirps while following or spotting.")]
        [Min(0.5f)] [SerializeField] private float _followingChirpMax = 22f;

        [Tooltip("Shortest gap between happy chirps at home.")]
        [Min(0.5f)] [SerializeField] private float _homeChirpMin = 12f;

        [Tooltip("Longest gap between happy chirps at home.")]
        [Min(0.5f)] [SerializeField] private float _homeChirpMax = 30f;

        [Tooltip("Shortest gap between sleepy coos while napping.")]
        [Min(0.5f)] [SerializeField] private float _nappingChirpMin = 6f;

        [Tooltip("Longest gap between sleepy coos while napping.")]
        [Min(0.5f)] [SerializeField] private float _nappingChirpMax = 15f;

        public float RotorIdlePitch => _rotorIdlePitch;
        public float RotorDashPitch => _rotorDashPitch;
        public float RotorLowVolume => _rotorLowVolume;
        public float RotorHighVolume => _rotorHighVolume;
        public float RotorSpinUpTime => _rotorSpinUpTime;
        public float RotorSpinDownTime => _rotorSpinDownTime;
        public float StitchFadeIn => _stitchFadeIn;
        public float StitchFadeOut => _stitchFadeOut;
        public float StitchStartGain => _stitchStartGain;
        public float StitchRiseSemitones => _stitchRiseSemitones;
        public float StitchRiseTime => _stitchRiseTime;
        public float StepStride => _stepStride;
        public float StepTeleportDistance => _stepTeleportDistance;
        public float DozeFadeIn => _dozeFadeIn;
        public float DozeFadeOut => _dozeFadeOut;
        public float FollowingChirpMin => _followingChirpMin;
        public float FollowingChirpMax => Mathf.Max(_followingChirpMax, _followingChirpMin + MinSpan);
        public float HomeChirpMin => _homeChirpMin;
        public float HomeChirpMax => Mathf.Max(_homeChirpMax, _homeChirpMin + MinSpan);
        public float NappingChirpMin => _nappingChirpMin;
        public float NappingChirpMax => Mathf.Max(_nappingChirpMax, _nappingChirpMin + MinSpan);
    }
}
