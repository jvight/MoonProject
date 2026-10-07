using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The dial readout: a small label near the bottom centre naming the station Bell's dial was just turned to.
    /// </summary>
    [Serializable]
    public sealed class DialReadoutSettings
    {
        [Tooltip("The readout easing in and out (quick in: it answers the player's own turn of the dial).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.3f, 0.8f, 8f, 0.96f, 1.8f, 0.7f);

        [Tooltip("Seconds the readout rests fully visible after the last turn of the dial.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _holdSeconds = 2.4f;

        public RevealSettings Reveal => _reveal;

        public float HoldSeconds => _holdSeconds;
    }
}
