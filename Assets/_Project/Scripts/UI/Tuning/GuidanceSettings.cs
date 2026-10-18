using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The gentle guidance (docs/design/opening-v2.md §3, §10): the objective line, 07's readout cards and the Sun's
    /// arc.
    /// </summary>
    [Serializable]
    public sealed class GuidanceSettings
    {
        [Tooltip("The objective line easing in and out.")]
        [SerializeField] private RevealSettings _line = new RevealSettings(0.9f, 0.5f, 6f, 1f, 1.4f, 0.6f);

        [Tooltip("Seconds the low-battery hint rests on screen before it goes (it also goes when a charger starts).")]
        [Range(2f, 20f)]
        [SerializeField] private float _lowBatteryHoldSeconds = 8f;

        [Tooltip("Speed (m/s) under which 07 counts as still.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stillSpeed = 0.4f;

        [Tooltip("Seconds the objective line keeps its place while a prompt that takes over from it fades in: the two " +
                 "cross-fade, so the screen is never blank in the hand-over.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _handOverSeconds = 0.3f;

        [Tooltip("Seconds after a step begins before its objective line may show (docs/design/opening-v2.md §3).")]
        [Range(0f, 10f)]
        [SerializeField] private float _objectiveDelaySeconds = 1.5f;

        [Tooltip("Seconds 07 must have been still before the objective line shows.")]
        [Range(0f, 5f)]
        [SerializeField] private float _objectiveStillSeconds = 1f;

        [Tooltip("Seconds the objective line rests bright before it dims to a small line.")]
        [Range(1f, 30f)]
        [SerializeField] private float _objectiveRestSeconds = 8f;

        [Tooltip("Seconds 07 must be still before a dim objective line may brighten again.")]
        [Range(1f, 30f)]
        [SerializeField] private float _objectiveRebrightStillSeconds = 4f;

        [Tooltip("Seconds without progress before a dim objective line may brighten again.")]
        [Range(10f, 600f)]
        [SerializeField] private float _objectiveIdleSeconds = 90f;

        [Tooltip("07's readout cards (the boot diagnostics, the protocol line, the lander's reserve, a scanned " +
                 "panel's text) easing in and out.")]
        [SerializeField] private RevealSettings _readout = new RevealSettings(0.8f, 1.1f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Seconds the boot diagnostics rest fully visible: ten lines to read.")]
        [Range(2f, 20f)]
        [SerializeField] private float _bootHoldSeconds = 8f;

        [Tooltip("Seconds a one- or two-line readout card (the protocol line, the reserve, one system) rests.")]
        [Range(1f, 12f)]
        [SerializeField] private float _statusHoldSeconds = 4f;

        [Tooltip("Seconds a scanned panel's text rests: the player stops to read it (opening-v2 section 7).")]
        [Range(3f, 20f)]
        [SerializeField] private float _storyHoldSeconds = 9f;

        [Tooltip("Seconds a line stays lit after its system changed or refused.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _flashSeconds = 1.2f;

        [Tooltip("The Sun's arc easing in and out.")]
        [SerializeField] private RevealSettings _sunArc = new RevealSettings(1.2f, 1.5f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Elevation (degrees) at which the Sun's arc reads full: the highest the low circling Sun climbs " +
                 "(World's sky cycle).")]
        [Range(5f, 60f)]
        [SerializeField] private float _sunArcFullElevation = 17.3f;

        public RevealSettings Line => _line;

        public float LowBatteryHoldSeconds => _lowBatteryHoldSeconds;

        public float StillSpeed => _stillSpeed;

        public float HandOverSeconds => _handOverSeconds;

        public float ObjectiveDelaySeconds => _objectiveDelaySeconds;

        public float ObjectiveStillSeconds => _objectiveStillSeconds;

        public float ObjectiveRestSeconds => _objectiveRestSeconds;

        public float ObjectiveRebrightStillSeconds => _objectiveRebrightStillSeconds;

        public float ObjectiveIdleSeconds => _objectiveIdleSeconds;

        public RevealSettings Readout => _readout;

        public float BootHoldSeconds => _bootHoldSeconds;

        public float StatusHoldSeconds => _statusHoldSeconds;

        public float StoryHoldSeconds => _storyHoldSeconds;

        public float FlashSeconds => _flashSeconds;

        public RevealSettings SunArc => _sunArc;

        public float SunArcFullElevation => _sunArcFullElevation;
    }
}
