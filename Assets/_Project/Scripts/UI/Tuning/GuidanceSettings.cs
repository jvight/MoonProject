using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The gentle guidance (docs/design/opening-v2.md §3): the objective line and the protocol
    /// readout.
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

        [Tooltip("The recovery protocol's readout in 07's lens at the end of the full opening, easing in and out.")]
        [SerializeField] private RevealSettings _protocol = new RevealSettings(0.8f, 1.1f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Seconds the protocol readout rests fully visible (about 4 s on screen with its fades, M3-24 B).")]
        [Range(1f, 10f)]
        [SerializeField] private float _protocolHoldSeconds = 2.8f;

        public RevealSettings Line => _line;

        public float LowBatteryHoldSeconds => _lowBatteryHoldSeconds;

        public float StillSpeed => _stillSpeed;

        public float ObjectiveDelaySeconds => _objectiveDelaySeconds;

        public float ObjectiveStillSeconds => _objectiveStillSeconds;

        public float ObjectiveRestSeconds => _objectiveRestSeconds;

        public float ObjectiveRebrightStillSeconds => _objectiveRebrightStillSeconds;

        public float ObjectiveIdleSeconds => _objectiveIdleSeconds;

        public RevealSettings Protocol => _protocol;

        public float ProtocolHoldSeconds => _protocolHoldSeconds;
    }
}
