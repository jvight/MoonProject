using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The call display on 07's CRT face (docs/design/opening-v3-plan.md 2.4): when it shows, how it eases and how its
    /// waveform moves in each of the call's states.
    /// </summary>
    [Serializable]
    public sealed class CallDisplaySettings
    {
        [Tooltip("Seconds 07 must stand still before its face shows the call.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _stillSeconds = 2f;

        [Tooltip("Seconds the face shows the call after its state changes or a letter decodes (counted on screen).")]
        [Range(1f, 6f)]
        [SerializeField] private float _changeSeconds = 3f;

        [Tooltip("Seconds the display eases in or out.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _fadeSeconds = 0.4f;

        [Tooltip("Seconds per unit of the call sign (a short pulse is one unit, the long one three).")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _unitSeconds = 0.16f;

        [Tooltip("Units of the call sign across the waveform's width (one whole call is 13).")]
        [Range(6f, 52f)]
        [SerializeField] private float _windowUnits = 13f;

        [Tooltip("Carrier waves drawn inside one unit of a pulse.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _carrierCycles = 1.5f;

        [Tooltip("Points along the waveform's line.")]
        [Range(32, 320)]
        [SerializeField] private int _samples = 260;

        [Tooltip("Static on a broken call, as a share of a pulse's height.")]
        [Range(0f, 1f)]
        [SerializeField] private float _brokenStatic = 0.45f;

        [Tooltip("Share of a broken call's units that drop out.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _brokenDropout = 0.4f;

        [Tooltip("Static on a locked call, as a share of a pulse's height (a clear or answered call has none).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lockedStatic = 0.06f;

        [Tooltip("Times a second the static redraws: slow enough to stay calm.")]
        [Range(2f, 30f)]
        [SerializeField] private float _staticRate = 9f;

        public float StillSeconds => _stillSeconds;

        public float ChangeSeconds => _changeSeconds;

        public float FadeSeconds => _fadeSeconds;

        public float UnitSeconds => _unitSeconds;

        public float WindowUnits => _windowUnits;

        public float CarrierCycles => _carrierCycles;

        public int Samples => _samples;

        public float BrokenStatic => _brokenStatic;

        public float BrokenDropout => _brokenDropout;

        public float LockedStatic => _lockedStatic;

        public float StaticRate => _staticRate;
    }
}
