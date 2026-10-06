using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The "Lofi Lunar" wordmark that drifts in low on screen while 07 wakes up.</summary>
    [Serializable]
    public sealed class TitleSettings
    {
        [Tooltip("The wordmark easing in and out (slow and soft).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(2.2f, 2.8f, 14f, 0.985f, 0.6f, 0.8f);

        [Tooltip("Seconds after 07 starts waking before the wordmark begins to appear.")]
        [Range(0f, 6f)]
        [SerializeField] private float _delay = 1.6f;

        [Tooltip("Seconds the wordmark rests fully visible before it fades away.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _hold = 3.8f;

        public RevealSettings Reveal => _reveal;

        public float Delay => _delay;

        public float Hold => _hold;
    }
}
