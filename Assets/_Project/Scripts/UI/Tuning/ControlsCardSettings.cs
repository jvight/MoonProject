using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The first controls card after the opening (<see cref="ControlsCard"/>): when it has done its job.</summary>
    [Serializable]
    public sealed class ControlsCardSettings
    {
        [Tooltip("The card easing in at the chase frame and out once its job is done: soft and slow.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.8f, 1.2f, 8f, 0.96f, 1.4f, 0.6f);

        [Tooltip("Metres the player has to drive (with looking around) before the card has done its job.")]
        [Range(1f, 20f)]
        [SerializeField] private float _driveMetres = 4f;

        [Tooltip("Seconds of look input (mouse or stick) that count as having looked around.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _lookSeconds = 0.6f;

        [Tooltip("Seconds the card stays on screen at most, whatever the player does.")]
        [Range(5f, 60f)]
        [SerializeField] private float _maxSeconds = 20f;

        [Tooltip("Seconds the card comes back on screen with a key it has just added (B Build, the hook cable).")]
        [Range(2f, 20f)]
        [SerializeField] private float _addedSeconds = 7f;

        public RevealSettings Reveal => _reveal;

        public float DriveMetres => _driveMetres;

        public float LookSeconds => _lookSeconds;

        public float MaxSeconds => _maxSeconds;

        public float AddedSeconds => _addedSeconds;
    }
}
