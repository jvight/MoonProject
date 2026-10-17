using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Tuning for the LETTER CARD (`#letter-card`): Variant C terminal + telex/paper presentation for long story-v4
    /// letters (`letter.mast_1`…`10`, `letter.after_1`…`10`, `ticker.ending.answer`) with a gentle typewriter reveal
    /// and a calm hold after completion.
    /// </summary>
    [Serializable]
    public sealed class LetterCardSettings
    {
        [Tooltip("The letter card easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.35f, 0.75f, 10f, 0.98f, 1.1f, 0.75f);

        [Tooltip("Seconds of quiet before the letter card appears once the stage is clear.")]
        [Range(0f, 5f)]
        [SerializeField] private float _appearDelay = 0.4f;

        [Tooltip("Characters revealed per second during the typewriter print pass.")]
        [Range(10f, 160f)]
        [SerializeField] private float _charactersPerSecond = 42f;

        [Tooltip("Minimum seconds between published UiTypewriterTick(UiTypeKind.Card) audio cues.")]
        [Range(0.02f, 0.25f)]
        [SerializeField] private float _tickIntervalSeconds = 0.055f;

        [Tooltip("Seconds the letter card rests after the typewriter reveal completes before gently fading out.")]
        [Range(2f, 30f)]
        [SerializeField] private float _holdAfterRevealSeconds = 8f;

        [Tooltip("Minimum total seconds (reveal + hold) the letter card stays on screen unless dismissed.")]
        [Range(3f, 40f)]
        [SerializeField] private float _minHoldSeconds = 10f;

        [Tooltip("Extra seconds a voiced letter stays visible after its recording finishes.")]
        [Min(0f)]
        [SerializeField] private float _voiceTailSeconds = 1.5f;

        public RevealSettings Reveal => _reveal.FadeIn > 0.01f ? _reveal : new RevealSettings(0.35f, 0.75f, 10f, 0.98f, 1.1f, 0.75f);

        public float AppearDelay => _appearDelay >= 0f ? _appearDelay : 0.4f;

        public float CharactersPerSecond => _charactersPerSecond > 0.01f ? _charactersPerSecond : 42f;

        public float TickIntervalSeconds => _tickIntervalSeconds > 0.001f ? _tickIntervalSeconds : 0.055f;

        public float HoldAfterRevealSeconds => _holdAfterRevealSeconds > 0.1f ? _holdAfterRevealSeconds : 8f;

        public float MinHoldSeconds => _minHoldSeconds > 0.1f ? _minHoldSeconds : 10f;

        public float VoiceTailSeconds => _voiceTailSeconds >= 0f ? _voiceTailSeconds : 1.5f;

        public float TotalHoldSeconds(int characterCount)
        {
            float revealTime = characterCount / CharactersPerSecond;
            return Mathf.Max(MinHoldSeconds, revealTime + HoldAfterRevealSeconds);
        }

        public string Validate()
        {
            if (_charactersPerSecond <= 0f)
            {
                return "letter card _charactersPerSecond must be > 0";
            }

            if (_tickIntervalSeconds <= 0f)
            {
                return "letter card _tickIntervalSeconds must be > 0";
            }

            if (_minHoldSeconds <= 0f)
            {
                return "letter card _minHoldSeconds must be > 0";
            }

            return null;
        }
    }
}
