using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The memory card a relic tells once it rests on the museum shelf: read at a calm pace, never in the way.
    /// </summary>
    [Serializable]
    public sealed class MemoryCardSettings
    {
        [Tooltip("The card easing in and out (slow: it is a moment).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(1f, 0.9f, 16f, 0.97f, 0.9f, 0.75f);

        [Tooltip("Seconds after the relic settles on the shelf before the card begins to appear.")]
        [Range(0f, 5f)]
        [SerializeField] private float _appearDelay = 1.1f;

        [Tooltip("Seconds of reading time before counting the text.")]
        [Range(0f, 10f)]
        [SerializeField] private float _readBaseSeconds = 4f;

        [Tooltip("Extra reading seconds per character of the name and memory text (a calm reading pace).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _readSecondsPerCharacter = 0.06f;

        [Tooltip("Shortest time the card stays fully visible.")]
        [Range(1f, 20f)]
        [SerializeField] private float _minReadSeconds = 6f;

        [Tooltip("Longest time the card stays fully visible.")]
        [Range(2f, 40f)]
        [SerializeField] private float _maxReadSeconds = 16f;

        public RevealSettings Reveal => _reveal;

        public float AppearDelay => _appearDelay;

        public float ReadBaseSeconds => _readBaseSeconds;

        public float ReadSecondsPerCharacter => _readSecondsPerCharacter;

        public float MinReadSeconds => _minReadSeconds;

        public float MaxReadSeconds => _maxReadSeconds;

        /// <summary>Seconds a card with <paramref name="characters"/> characters stays fully visible.</summary>
        public float ReadSeconds(int characters)
        {
            return Mathf.Clamp(_readBaseSeconds + characters * _readSecondsPerCharacter, _minReadSeconds,
                Mathf.Max(_minReadSeconds, _maxReadSeconds));
        }
    }
}
