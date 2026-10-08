using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The quiet title naming a piece of kit as it settles onto 07 (docs/features/M3-11).</summary>
    [Serializable]
    public sealed class KitTitleSettings
    {
        [Tooltip("The name easing in and out (slow and soft, rising a little).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.9f, 1.4f, 12f, 0.94f, 1f, 0.7f);

        [Tooltip("Seconds after the piece settles before its name begins to appear: the clunk lands first.")]
        [Range(0f, 3f)]
        [SerializeField] private float _delay = 0.35f;

        [Tooltip("Seconds the name rests fully visible before it fades away.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _holdSeconds = 2.6f;

        public RevealSettings Reveal => _reveal;

        public float Delay => _delay;

        public float HoldSeconds => _holdSeconds;
    }
}
