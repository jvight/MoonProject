using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How salvage sounds (M3-13): the cutting beam (its fades, how its singing edge climbs the pentatonic, its
    /// levels), the break-off and the salvage melody. Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class SalvageAudioTuning : ScriptableObject
    {
        [Header("The cutting beam")]
        [Tooltip("Seconds for the beam's grind and tone to come in as the cut starts.")]
        [Range(0.01f, 2f)] [SerializeField] private float _cutFadeIn = 0.25f;

        [Tooltip("Seconds for the beam to fade softly when the player lets go before the piece is free.")]
        [Range(0.05f, 3f)] [SerializeField] private float _releaseFade = 0.6f;

        [Tooltip("Seconds for the beam to fall away as the piece breaks loose (the break-off takes over).")]
        [Range(0.01f, 1f)] [SerializeField] private float _breakFade = 0.12f;

        [Tooltip("Seconds of cutting per step up the pentatonic (a cut takes 2-4 s: a few steps).")]
        [Range(0.1f, 3f)] [SerializeField] private float _stepSeconds = 0.6f;

        [Tooltip("Highest step of the climb (0 = D, 1 = E, 2 = F#, 3 = A, 4 = B, 5 = the D above).")]
        [Range(0, 5)] [SerializeField] private int _topStep = 4;

        [Tooltip("Seconds (time constant) for the tone to glide onto each new note.")]
        [Range(0.005f, 0.5f)] [SerializeField] private float _glide = 0.06f;

        [Tooltip("How much the grind's pitch lifts over the climb (1 = none): the cut tightens as it goes.")]
        [Range(1f, 1.5f)] [SerializeField] private float _textureRise = 1.12f;

        [Tooltip("Volume scale of the material's grind, crackle or shimmer.")]
        [Range(0f, 1f)] [SerializeField] private float _textureVolume = 0.85f;

        [Tooltip("Volume scale of the beam's singing tone.")]
        [Range(0f, 1f)] [SerializeField] private float _toneVolume = 0.6f;

        [Header("Salvage melody")]
        [Tooltip("Long chains weave over this many of the highest chime notes once the climb reaches the top.")]
        [Range(2, 8)] [SerializeField] private int _melodyTopWindow = 4;

        public float CutFadeIn => _cutFadeIn;
        public float ReleaseFade => _releaseFade;
        public float BreakFade => _breakFade;
        public float StepSeconds => _stepSeconds;
        public int TopStep => _topStep;
        public float Glide => _glide;
        public float TextureRise => _textureRise;
        public float TextureVolume => _textureVolume;
        public float ToneVolume => _toneVolume;
        public int MelodyTopWindow => _melodyTopWindow;
    }
}
