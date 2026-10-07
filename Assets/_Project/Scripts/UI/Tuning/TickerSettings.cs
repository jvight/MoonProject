using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The radio ticker: one soft line along the bottom of the HUD, one line at a time, resting long enough to read at
    /// a calm pace, with a breath of quiet between lines.
    /// </summary>
    [Serializable]
    public sealed class TickerSettings
    {
        [Tooltip("A line easing in and out (slow: it drifts in like a voice on the radio).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.9f, 1.1f, 6f, 1f, 1f, 1f);

        [Tooltip("Seconds a line rests fully visible before counting its characters.")]
        [Range(0f, 10f)]
        [SerializeField] private float _holdBaseSeconds = 2.5f;

        [Tooltip("Extra resting seconds per character of the line (a calm reading pace).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _holdSecondsPerCharacter = 0.055f;

        [Tooltip("Shortest time a line rests fully visible.")]
        [Range(1f, 20f)]
        [SerializeField] private float _minHoldSeconds = 4f;

        [Tooltip("Longest time a line rests fully visible.")]
        [Range(2f, 30f)]
        [SerializeField] private float _maxHoldSeconds = 9f;

        [Tooltip("Seconds of quiet before a line starts: after the previous line, and after whatever made it wait (a "
            + "card, a dig, a prompt) has gone.")]
        [Range(0f, 10f)]
        [SerializeField] private float _gapSeconds = 1.5f;

        [Tooltip("Lines that may wait their turn. One more drops the oldest waiting line: a radio ticker carries the "
            + "latest news.")]
        [Range(1, 20)]
        [SerializeField] private int _capacity = 5;

        [Tooltip("Times a line may make way (for a card, a dig, a prompt, ...) and still come back. Interrupted once "
            + "more, it eases away for good, so no line keeps returning.")]
        [Range(0, 5)]
        [SerializeField] private int _maxYields = 1;

        [Tooltip("Seconds of one slow breath of the amber on-air lamp beside the line.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lampBreathSeconds = 3.2f;

        [Tooltip("Opacity of the on-air lamp at the bottom of its breath (1 at the top).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lampMinOpacity = 0.45f;

        public RevealSettings Reveal => _reveal;

        public float HoldBaseSeconds => _holdBaseSeconds;

        public float HoldSecondsPerCharacter => _holdSecondsPerCharacter;

        public float MinHoldSeconds => _minHoldSeconds;

        public float MaxHoldSeconds => _maxHoldSeconds;

        public float GapSeconds => _gapSeconds;

        public int Capacity => _capacity;

        public int MaxYields => _maxYields;

        public float LampBreathSeconds => _lampBreathSeconds;

        public float LampMinOpacity => _lampMinOpacity;

        /// <summary>Seconds a line of <paramref name="characters"/> characters rests fully visible.</summary>
        public float HoldSeconds(int characters)
        {
            return Mathf.Clamp(_holdBaseSeconds + characters * _holdSecondsPerCharacter, _minHoldSeconds,
                Mathf.Max(_minHoldSeconds, _maxHoldSeconds));
        }
    }
}
