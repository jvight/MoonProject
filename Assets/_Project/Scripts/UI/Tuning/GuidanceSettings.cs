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
        [SerializeField] private RevealSettings _line = new RevealSettings(0.25f, 0.25f, 6f, 1f, 1.4f, 0.6f);

        [Tooltip("Seconds the low-battery hint rests on screen before it goes (it also goes when a charger starts).")]
        [Range(2f, 20f)]
        [SerializeField] private float _lowBatteryHoldSeconds = 8f;

        [Tooltip("Speed (m/s) under which 07 counts as still.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stillSpeed = 0.4f;

        [Tooltip("07's readout cards (the boot diagnostics, the protocol line, the lander's reserve, a scanned " +
                 "panel's text) easing in and out.")]
        [SerializeField] private RevealSettings _readout = new RevealSettings(0.8f, 1.1f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Seconds a one- or two-line readout card (the protocol line, the reserve, one system) rests.")]
        [Range(1f, 12f)]
        [SerializeField] private float _statusHoldSeconds = 4f;

        [Tooltip("Seconds a scanned panel's text rests: the player stops to read it (opening-v2 section 7).")]
        [Range(3f, 20f)]
        [SerializeField] private float _storyHoldSeconds = 9f;

        [Tooltip("Seconds a scan set's progress card rests (\"Solar Array · blueprint 1/3\"): a glance, so the " +
                 "objective line is back soon.")]
        [Range(1f, 8f)]
        [SerializeField] private float _scanSetHoldSeconds = 2.5f;

        [Tooltip("Seconds a line stays lit after its system changed or refused.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _flashSeconds = 1.2f;

        [Tooltip("Seconds the verb chip stays dimmed and shakes after a held E was blocked (no line of sight), " +
                 "easing back to full.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _blockedChipSeconds = 0.9f;

        [Tooltip("Opacity the verb chip dims to the moment a held E is blocked.")]
        [Range(0f, 1f)]
        [SerializeField] private float _blockedChipOpacity = 0.45f;

        [Tooltip("Pixels of the chip's sideways shake when a held E is blocked (fades out): small and soft.")]
        [Range(0f, 20f)]
        [SerializeField] private float _blockedShakePixels = 5f;

        [Tooltip("Shakes per second of that chip shake.")]
        [Range(1f, 12f)]
        [SerializeField] private float _blockedShakeHertz = 5f;

        [Tooltip("Seconds 07's face says why a held E did nothing (\"no line of sight\").")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _blockedFaceSeconds = 1.8f;

        [Tooltip("Seconds 07's face shows the failed reply's line (TX · NO RANGE) as the one element on screen.")]
        [Range(1f, 6f)]
        [SerializeField] private float _replyFaceSeconds = 3f;

        [Tooltip("Seconds 07's face shows TX · READY when the answer first becomes possible.")]
        [Range(1f, 10f)]
        [SerializeField] private float _txReadySeconds = 5f;

        [Tooltip("Seconds 07's face shows TX · READY again when it steps up to the mast.")]
        [Range(1f, 6f)]
        [SerializeField] private float _txPromptSeconds = 3f;

        [Tooltip("Seconds 07's face shows the answer's ACK line (the route read out), the one element on screen. Short, so " +
                 "the next line with its drive key follows within the first-time player's few seconds.")]
        [Range(2f, 12f)]
        [SerializeField] private float _ackSeconds = 3f;

        [Tooltip("The Sun's arc easing in and out.")]
        [SerializeField] private RevealSettings _sunArc = new RevealSettings(1.2f, 1.5f, 6f, 0.97f, 1f, 0.8f);

        [Tooltip("Elevation (degrees) at which the Sun's arc reads full: the highest the low circling Sun climbs " +
                 "(World's sky cycle).")]
        [Range(5f, 60f)]
        [SerializeField] private float _sunArcFullElevation = 17.3f;

        public RevealSettings Line => _line;

        public float LowBatteryHoldSeconds => _lowBatteryHoldSeconds;

        public float StillSpeed => _stillSpeed;

        public RevealSettings Readout => _readout;

        public float StatusHoldSeconds => _statusHoldSeconds;

        public float StoryHoldSeconds => _storyHoldSeconds;

        public float ScanSetHoldSeconds => _scanSetHoldSeconds;

        public float FlashSeconds => _flashSeconds;

        public float BlockedChipSeconds => _blockedChipSeconds;

        public float BlockedChipOpacity => _blockedChipOpacity;

        public float BlockedShakePixels => _blockedShakePixels;

        public float BlockedShakeHertz => _blockedShakeHertz;

        public float BlockedFaceSeconds => _blockedFaceSeconds;

        public float ReplyFaceSeconds => _replyFaceSeconds;

        public float TxReadySeconds => _txReadySeconds;

        public float TxPromptSeconds => _txPromptSeconds;

        public float AckSeconds => _ackSeconds;

        public RevealSettings SunArc => _sunArc;

        public float SunArcFullElevation => _sunArcFullElevation;
    }
}
