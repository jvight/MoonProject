using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The gentle guidance (docs/features/M3-16): the soft "next" line and the place cards.</summary>
    [Serializable]
    public sealed class GuidanceSettings
    {
        [Tooltip("The next line easing in when 07 has been still a moment, and out when it drives.")]
        [SerializeField] private RevealSettings _line = new RevealSettings(0.9f, 0.5f, 6f, 1f, 1.4f, 0.6f);

        [Tooltip("Seconds on the same step without progress before the next line offers its nudge (P8: patience "
            + "first; a player who is finding their own way never sees it).")]
        [Range(0f, 600f)]
        [SerializeField] private float _stepPatienceSeconds = 120f;

        [Tooltip("Speed (m/s) under which 07 counts as still.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stillSpeed = 0.4f;

        [Tooltip("Seconds 07 must stay still before the next line shows.")]
        [Range(0f, 5f)]
        [SerializeField] private float _stillSeconds = 1.2f;

        [Tooltip("A place card (the place's name and its one job) easing in and out.")]
        [SerializeField] private RevealSettings _place = new RevealSettings(1f, 1.4f, 12f, 0.94f, 1f, 0.7f);

        [Tooltip("Seconds a place card rests fully visible.")]
        [Range(1f, 10f)]
        [SerializeField] private float _placeHoldSeconds = 4f;

        public RevealSettings Line => _line;

        public float StepPatienceSeconds => _stepPatienceSeconds;

        public float StillSpeed => _stillSpeed;

        public float StillSeconds => _stillSeconds;

        public RevealSettings Place => _place;

        public float PlaceHoldSeconds => _placeHoldSeconds;
    }
}
