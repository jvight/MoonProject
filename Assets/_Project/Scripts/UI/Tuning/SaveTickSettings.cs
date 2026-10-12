using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The save tick: a tiny "07" with a check mark that shows for a moment when the game saves.</summary>
    [Serializable]
    public sealed class SaveTickSettings
    {
        [Tooltip("The sign easing in and out (soft, quick in, slow out).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.25f, 0.7f, 6f, 0.96f, 1.2f, 0.8f);

        [Tooltip("Seconds the sign rests fully visible.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _holdSeconds = 1f;

        public RevealSettings Reveal => _reveal;

        public float HoldSeconds => _holdSeconds;
    }
}
