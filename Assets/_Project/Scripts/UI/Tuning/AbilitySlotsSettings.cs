using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The ability slots at the bottom centre: when the row shows, and how long a new ability wears its ring.</summary>
    [Serializable]
    public sealed class AbilitySlotsSettings
    {
        [Tooltip("The row easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.5f, 0.9f, 8f, 0.96f, 1.2f, 0.8f);

        [Tooltip("Seconds a newly earned ability wears the amber ring.")]
        [Range(1f, 30f)]
        [SerializeField] private float _earnedSeconds = 8f;

        [Tooltip("Metres per second under which 07 counts as still.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stillSpeed = 0.3f;

        [Tooltip("Seconds 07 rests before the row drifts into view (it is no permanent HUD).")]
        [Range(0f, 10f)]
        [SerializeField] private float _stillSeconds = 1.5f;

        public RevealSettings Reveal => _reveal;

        public float EarnedSeconds => _earnedSeconds;

        public float StillSpeed => _stillSpeed;

        public float StillSeconds => _stillSeconds;
    }
}
