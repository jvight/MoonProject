using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The card that says what a tower level gave (docs/design/feel-pass.md §8.2, the Reward beat).</summary>
    [Serializable]
    public sealed class RewardCardSettings
    {
        [Tooltip("The card easing in and out: slow and soft, rising a little.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.9f, 1.4f, 14f, 0.96f, 1f, 0.7f);

        [Tooltip("Seconds the card rests fully visible: long enough to read a title and three short lines.")]
        [Range(2f, 12f)]
        [SerializeField] private float _holdSeconds = 6f;

        public RevealSettings Reveal => _reveal;

        public float HoldSeconds => _holdSeconds;
    }
}
