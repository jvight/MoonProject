using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Authoritative tuning asset for 07's expressive CRT face (0.5.7 slice 9), containing the 9 feelings from
    /// <c>face_prototype.html</c> plus easing, blink, saccade, boot, gaze, phosphor trail, and low-power timings.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Rover/Face Tuning", fileName = "FaceTuning")]
    public sealed class FaceTuning : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Tuning/FaceTuning.asset";

        [Header("Feelings (face_prototype.html)")]
        [Tooltip("Per-feeling eye geometry, gaze offset, symbol, and asymmetry parameters.")]
        [SerializeField] private FaceFeelingSpec[] _feelings = CreateDefaultFeelings();

        [Header("Asymmetric Curious Overrides")]
        [Tooltip("Left eye scale multiplier when a feeling has Asymmetric enabled (curious).")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float _asymmetricLeftScale = 1.12f;

        [Tooltip("Right eye scale multiplier when a feeling has Asymmetric enabled (curious).")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float _asymmetricRightScale = 0.90f;

        [Tooltip("Right eye top-lid cut override when Asymmetric is enabled (curious).")]
        [Range(0f, 1f)]
        [SerializeField] private float _asymmetricRightCut = 0.42f;

        [Tooltip("Right eye top-lid tilt override (before mirror sign) when Asymmetric is enabled (curious).")]
        [Range(-1.2f, 1.2f)]
        [SerializeField] private float _asymmetricRightTilt = 0.15f;

        [Header("Placement & Gaze Limits")]
        [Tooltip("Horizontal eye separation from screen center in normalized screen width (prototype sep = 0.19).")]
        [Range(0.10f, 0.35f)]
        [SerializeField] private float _eyeSeparation = 0.19f;

        [Tooltip("Maximum horizontal look offset contributed by external LookAt targets.")]
        [Range(0.01f, 0.20f)]
        [SerializeField] private float _lookMaxOffsetX = 0.10f;

        [Tooltip("Maximum vertical look offset contributed by external LookAt targets.")]
        [Range(0.01f, 0.20f)]
        [SerializeField] private float _lookMaxOffsetY = 0.07f;

        [Header("Easing & Phosphor Trail")]
        [Tooltip("Exponential easing rate for eye parameters (prototype k = 1 - exp(-dt * 7)).")]
        [Min(0.5f)]
        [SerializeField] private float _easeRate = 7.0f;

        [Tooltip("Exponential easing rate for corner/status symbol alpha (prototype k = 1 - exp(-dt * 5)).")]
        [Min(0.5f)]
        [SerializeField] private float _symbolEaseRate = 5.0f;

        [Tooltip("Exponential decay rate for the previous-frame phosphor trail ghost.")]
        [Min(1.0f)]
        [SerializeField] private float _trailEaseRate = 18.0f;

        [Tooltip("Opacity of the trailing phosphor ghost layer.")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _trailAlpha = 0.28f;

        [Header("Blink Schedule")]
        [Tooltip("Minimum interval in seconds between spontaneous blinks while awake (prototype 2.2 s).")]
        [Min(0.2f)]
        [SerializeField] private float _blinkMinInterval = 2.2f;

        [Tooltip("Maximum interval in seconds between spontaneous blinks while awake (prototype 5.7 s).")]
        [Min(0.5f)]
        [SerializeField] private float _blinkMaxInterval = 5.7f;

        [Tooltip("Total duration of a single blink close-and-reopen cycle in seconds (prototype 0.16 s).")]
        [Range(0.04f, 0.50f)]
        [SerializeField] private float _blinkDuration = 0.16f;

        [Header("Saccades")]
        [Tooltip("Minimum interval in seconds between micro-saccade gaze shifts while awake (prototype 1.2 s).")]
        [Min(0.2f)]
        [SerializeField] private float _saccadeMinInterval = 1.2f;

        [Tooltip("Maximum interval in seconds between micro-saccade gaze shifts while awake (prototype 3.6 s).")]
        [Min(0.5f)]
        [SerializeField] private float _saccadeMaxInterval = 3.6f;

        [Tooltip("Half-range of horizontal micro-saccade offset (prototype +/-0.04).")]
        [Range(0f, 0.10f)]
        [SerializeField] private float _saccadeRangeX = 0.04f;

        [Tooltip("Half-range of vertical micro-saccade offset (prototype +/-0.02).")]
        [Range(0f, 0.10f)]
        [SerializeField] private float _saccadeRangeY = 0.02f;

        [Header("Boot Sequence")]
        [Tooltip("Rate at which normalized boot progress advances per second (prototype 0.9 / s).")]
        [Min(0.1f)]
        [SerializeField] private float _bootRate = 0.90f;

        [Tooltip("Normalized boot progress threshold where the horizontal power-on line finishes and eyes begin opening (prototype 0.45).")]
        [Range(0.10f, 0.80f)]
        [SerializeField] private float _bootLineEnd = 0.45f;

        [Tooltip("Normalized boot progress span over which the eyes open vertically after the power-on line (prototype 0.30).")]
        [Range(0.05f, 0.55f)]
        [SerializeField] private float _bootOpenSpan = 0.30f;

        [Header("CRT Brightness, Scanlines & Low Power")]
        [Tooltip("Brightness multiplier when 07 is asleep (prototype 0.70).")]
        [Range(0.2f, 1.0f)]
        [SerializeField] private float _sleepBrightness = 0.70f;

        [Tooltip("Brightness multiplier at full low-power state (SPEC.md §3.1.1: 55% luminance).")]
        [Range(0.2f, 1.0f)]
        [SerializeField] private float _lowPowerMinBrightness = 0.55f;

        [Tooltip("Vertical-hold UV jitter amplitude at full low-power state (SPEC.md §3.1.1: +/-0.004 UV).")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _lowPowerJitterAmplitude = 0.004f;

        [Tooltip("Vertical-hold jitter frequency in Hz at low power (SPEC.md §3.1.1: 2 Hz).")]
        [Range(0.5f, 10.0f)]
        [SerializeField] private float _lowPowerJitterFrequency = 2.0f;

        [Tooltip("Scanline phase scroll speed in cycles per second.")]
        [Range(0f, 10.0f)]
        [SerializeField] private float _scanlineSpeed = 2.5f;

        [Tooltip("Slow rolling CRT refresh band speed in cycles per second (prototype 0.12 / s).")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float _rollingBandSpeed = 0.12f;

        [Header("Screen Content Mode (FACE-INV)")]
        [Tooltip("Lead-in duration in seconds while 07 brakes and lifts its head before collapsing the CRT eyes into a horizontal line (0.18 s).")]
        [Range(0.00f, 0.40f)]
        [SerializeField] private float _screenLeadInSeconds = 0.18f;

        [Tooltip("Duration in seconds for 07's eyes to collapse into a horizontal phosphor line when opening the screen UI (0.16 s).")]
        [Range(0.04f, 0.50f)]
        [SerializeField] private float _screenCollapseSeconds = 0.16f;

        [Tooltip("Hold duration in seconds for the bright horizontal CRT carrier line as the camera arrives at 07's face (0.16 s).")]
        [Range(0.00f, 0.30f)]
        [SerializeField] private float _screenLineHoldSeconds = 0.16f;

        [Tooltip("Duration in seconds for the horizontal phosphor line to expand into the screen UI content when opening (0.22 s).")]
        [Range(0.04f, 0.60f)]
        [SerializeField] private float _screenExpandSeconds = 0.22f;

        [Tooltip("Duration in seconds for the screen UI content to collapse back to a horizontal phosphor line when closing (0.14 s).")]
        [Range(0.04f, 0.50f)]
        [SerializeField] private float _screenCloseCollapseSeconds = 0.14f;

        [Tooltip("Duration in seconds for the horizontal phosphor line to expand back into 07's eyes when closing (0.16 s).")]
        [Range(0.04f, 0.50f)]
        [SerializeField] private float _screenCloseExpandSeconds = 0.16f;

        [Tooltip("Hold duration in seconds for the small happy blink (^ ^) after closing the screen UI.")]
        [Range(0.05f, 1.00f)]
        [SerializeField] private float _screenCloseHappyHoldSeconds = 0.35f;

        public IReadOnlyList<FaceFeelingSpec> Feelings => _feelings;
        public float AsymmetricLeftScale => _asymmetricLeftScale;
        public float AsymmetricRightScale => _asymmetricRightScale;
        public float AsymmetricRightCut => _asymmetricRightCut;
        public float AsymmetricRightTilt => _asymmetricRightTilt;
        public float EyeSeparation => _eyeSeparation;
        public float LookMaxOffsetX => _lookMaxOffsetX;
        public float LookMaxOffsetY => _lookMaxOffsetY;
        public float EaseRate => _easeRate;
        public float SymbolEaseRate => _symbolEaseRate;
        public float TrailEaseRate => _trailEaseRate;
        public float TrailAlpha => _trailAlpha;
        public float BlinkMinInterval => _blinkMinInterval;
        public float BlinkMaxInterval => _blinkMaxInterval;
        public float BlinkDuration => _blinkDuration;
        public float SaccadeMinInterval => _saccadeMinInterval;
        public float SaccadeMaxInterval => _saccadeMaxInterval;
        public float SaccadeRangeX => _saccadeRangeX;
        public float SaccadeRangeY => _saccadeRangeY;
        public float BootRate => _bootRate;
        public float BootLineEnd => _bootLineEnd;
        public float BootOpenSpan => _bootOpenSpan;
        public float SleepBrightness => _sleepBrightness;
        public float LowPowerMinBrightness => _lowPowerMinBrightness;
        public float LowPowerJitterAmplitude => _lowPowerJitterAmplitude;
        public float LowPowerJitterFrequency => _lowPowerJitterFrequency;
        public float ScanlineSpeed => _scanlineSpeed;
        public float RollingBandSpeed => _rollingBandSpeed;
        public float ScreenLeadInSeconds => _screenLeadInSeconds;
        public float ScreenCollapseSeconds => _screenCollapseSeconds;
        public float ScreenLineHoldSeconds => _screenLineHoldSeconds;
        public float ScreenExpandSeconds => _screenExpandSeconds;
        public float ScreenCloseCollapseSeconds => _screenCloseCollapseSeconds;
        public float ScreenCloseExpandSeconds => _screenCloseExpandSeconds;
        public float ScreenCloseHappyHoldSeconds => _screenCloseHappyHoldSeconds;

        /// <summary>Returns the authored <see cref="FaceFeelingSpec"/> for <paramref name="feeling"/>.</summary>
        public FaceFeelingSpec GetFeelingSpec(FaceFeeling feeling)
        {
            if (_feelings != null)
            {
                for (int i = 0; i < _feelings.Length; i++)
                {
                    if (_feelings[i].Feeling == feeling)
                    {
                        return _feelings[i];
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(feeling), feeling, $"Missing FaceFeelingSpec for '{feeling}'.");
        }

        private static FaceFeelingSpec[] CreateDefaultFeelings()
        {
            return new[]
            {
                new FaceFeelingSpec(
                    FaceFeeling.Calm,
                    0.17f, 0.34f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Curious,
                    0.17f, 0.34f, 1.00f, 0.20f, 0.10f, 0.00f, 0.00f, 0.00f, 0.00f, 0.06f, -0.04f,
                    FaceSymbol.Question, true),
                new FaceFeelingSpec(
                    FaceFeeling.Happy,
                    0.19f, 0.30f, 1.00f, 0.00f, 0.00f, 0.00f, 1.00f, 0.00f, 0.00f, 0.00f, -0.02f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Proud,
                    0.19f, 0.34f, 1.00f, -0.12f, 0.12f, 0.35f, 0.40f, 0.00f, 0.00f, 0.00f, -0.06f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Worried,
                    0.16f, 0.32f, 0.90f, -0.62f, 0.36f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.02f,
                    FaceSymbol.Battery, false),
                new FaceFeelingSpec(
                    FaceFeeling.Tired,
                    0.18f, 0.32f, 1.00f, 0.00f, 0.52f, 0.10f, 0.00f, 0.00f, 0.00f, 0.00f, 0.07f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Sad,
                    0.16f, 0.30f, 0.85f, -0.72f, 0.44f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.09f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Startled,
                    0.20f, 0.36f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 1.00f, 0.00f, 0.00f, -0.02f,
                    FaceSymbol.None, false),
                new FaceFeelingSpec(
                    FaceFeeling.Asleep,
                    0.18f, 0.30f, 1.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 1.00f, 0.00f, 0.05f,
                    FaceSymbol.Zzz, false)
            };
        }
    }
}
