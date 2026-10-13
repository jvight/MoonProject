using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The gentle guidance (docs/features/M3-16, M3-24 B): the soft "next" line, the place cards and the opening's
    /// protocol readout.
    /// </summary>
    [Serializable]
    public sealed class GuidanceSettings
    {
        [Tooltip("The next line easing in when 07 has been still a moment, and out when it drives.")]
        [SerializeField] private RevealSettings _line = new RevealSettings(0.9f, 0.5f, 6f, 1f, 1.4f, 0.6f);

        [Tooltip("Seconds on the same recovery-target need without progress before the next line offers its nudge "
            + "(P8: patience first; a player who is finding their own way never sees it). Teaching steps 0-5 show "
            + "their line as soon as 07 is still (M3-24 B).")]
        [Range(0f, 600f)]
        [SerializeField] private float _stepPatienceSeconds = 120f;

        [Tooltip("Seconds the low-battery hint rests on screen before it goes (it also goes when a charger starts).")]
        [Range(2f, 20f)]
        [SerializeField] private float _lowBatteryHoldSeconds = 8f;

        [Tooltip("Seconds on a teaching step without progress before its next line shows (the owner: guidance only " +
                 "when it is needed).")]
        [Range(0f, 120f)]
        [SerializeField] private float _teachingPatienceSeconds = 20f;

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

        [Tooltip("Seconds the radio tower's card waits after 07 first comes near, while World's slow light runs down "
            + "each dark cable to what the tower will power (M3-24 B); its line then names the job.")]
        [Range(0f, 10f)]
        [SerializeField] private float _towerCardDelay = 4f;

        [Tooltip("The recipe card easing in while 07 looks at the tower's hopper or a board sheet, and out after.")]
        [SerializeField] private RevealSettings _recipe = new RevealSettings(0.45f, 0.6f, 8f, 0.96f, 1.4f, 0.7f);

        [Tooltip("The recovery protocol's readout in 07's lens at the end of the full opening, easing in and out.")]
        [SerializeField] private RevealSettings _protocol = new RevealSettings(0.8f, 1.1f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Seconds the protocol readout rests fully visible (about 4 s on screen with its fades, M3-24 B).")]
        [Range(1f, 10f)]
        [SerializeField] private float _protocolHoldSeconds = 2.8f;

        public RevealSettings Line => _line;

        public float StepPatienceSeconds => _stepPatienceSeconds;

        public float LowBatteryHoldSeconds => _lowBatteryHoldSeconds;

        public float TeachingPatienceSeconds => _teachingPatienceSeconds;

        public float StillSpeed => _stillSpeed;

        public float StillSeconds => _stillSeconds;

        public RevealSettings Place => _place;

        public float PlaceHoldSeconds => _placeHoldSeconds;

        public float TowerCardDelay => _towerCardDelay;

        public RevealSettings Recipe => _recipe;

        public RevealSettings Protocol => _protocol;

        public float ProtocolHoldSeconds => _protocolHoldSeconds;
    }
}
