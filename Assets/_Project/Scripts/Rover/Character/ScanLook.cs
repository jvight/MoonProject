using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's eye through a scan as plain logic (docs/design/0.5.5-owner-feedback.md §1.2): scanning is looking.
    /// <list type="bullet">
    /// <item><see cref="Begin"/>: the half-lidded eye wakes, its lid lifting fully, and a soft fan of light sweeps the
    /// fragment from the lens, growing a little brighter as the drawing nears complete.</item>
    /// <item><see cref="Quarter"/>: each quarter of progress pulses the eye once.</item>
    /// <item><see cref="LetGo"/>: the fan fades and the lid settles back (the drawn part stays drawn).</item>
    /// <item><see cref="Learned"/>: as the lines stream in, the lens flares once (100 ms), then everything settles.</item>
    /// </list>
    /// Zero allocations.
    /// </summary>
    public sealed class ScanLook
    {
        private const float HeadYawShare = 0.65f;
        private const float HeadPitchSweepDeg = 1.35f;
        private const float HeadNodDeg = 0.85f;
        private const float HeadQuarterTickDeg = 1.6f;
        private const float MinRemainingSeconds = 1e-3f;

        private readonly ScanLookSettings _settings;
        private float _open;
        private float _fan;
        private float _progress;
        private float _progressRate;
        private float _sweepTime;
        private float _pulseAge = float.PositiveInfinity;
        private float _flareAge = float.PositiveInfinity;
        private float _learnedAge = float.PositiveInfinity;

        public ScanLook(ScanLookSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True from <see cref="Begin"/> until <see cref="LetGo"/> or <see cref="Learned"/>.</summary>
        public bool IsScanning { get; private set; }

        /// <summary>Whether 07 still looks at the fragment: while scanning, and through the dissolve after it.</summary>
        public bool HoldsGaze => IsScanning || _learnedAge < _settings.DissolveSeconds;

        /// <summary>How far the lid has lifted toward fully open, 0 (its mood's pose) .. 1.</summary>
        public float Open => _open;

        /// <summary>The lens fan's brightness, 0..1 plus the swell toward the scan's end.</summary>
        public float Fan => _fan * (1f + _settings.FanSwell * _progress);

        /// <summary>The fan's sweep across the fragment now (deg, + to 07's right).</summary>
        public float Sweep =>
            _settings.SweepDegrees * Mathf.Sin(2f * Mathf.PI * _sweepTime / _settings.SweepSeconds);

        /// <summary>Small mechanical horizontal raster oscillation (deg) of 07's scanner head in step with the sweep.</summary>
        public float HeadSweepYaw => _fan * Sweep * HeadYawShare;

        /// <summary>Small mechanical vertical nod and raster tick (deg, + = pitch up) of 07's scanner head in step with the sweep.</summary>
        public float HeadSweepPitch =>
            _fan * (Mathf.Lerp(-HeadPitchSweepDeg, HeadPitchSweepDeg, _progress)
                + HeadNodDeg * Mathf.Sin(4f * Mathf.PI * _sweepTime / _settings.SweepSeconds)
                + Pulse * HeadQuarterTickDeg);

        /// <summary>The eye's glow multiplier: awake while scanning, a pulse per quarter, the flare at the end.</summary>
        public float EyeGlow => Mathf.Lerp(1f, _settings.EyeGlow, _open) + Pulse + Flare;

        public void Begin(float progress = 0f, float remainingSeconds = 0f)
        {
            IsScanning = true;
            _progress = Mathf.Clamp01(progress);
            _progressRate = remainingSeconds > MinRemainingSeconds
                ? (1f - _progress) / remainingSeconds
                : 0f;
            _flareAge = float.PositiveInfinity;
            _learnedAge = float.PositiveInfinity;
        }

        /// <param name="progress">How far the scan has come, 0..1 (gameplay's Act at each quarter).</param>
        public void Quarter(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            if (IsScanning)
            {
                _pulseAge = 0f;
            }
        }

        public void LetGo()
        {
            IsScanning = false;
        }

        public void Learned()
        {
            IsScanning = false;
            _progress = 0f;
            _progressRate = 0f;
            _flareAge = 0f;
            _learnedAge = 0f;
        }

        /// <summary>Back to the mood's pose at once (07 was placed).</summary>
        public void Reset()
        {
            IsScanning = false;
            _open = 0f;
            _fan = 0f;
            _progress = 0f;
            _progressRate = 0f;
            _sweepTime = 0f;
            _pulseAge = float.PositiveInfinity;
            _flareAge = float.PositiveInfinity;
            _learnedAge = float.PositiveInfinity;
        }

        public void Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            float target = IsScanning ? 1f : 0f;
            _open = Smoothing.Damp(_open, target, _settings.OpenHalfLife, step);
            _fan = Smoothing.Damp(_fan, target, _settings.FanHalfLife, step);
            if (IsScanning)
            {
                _progress = Mathf.Clamp01(_progress + _progressRate * step);
            }

            _sweepTime = Mathf.Repeat(_sweepTime + step, _settings.SweepSeconds);
            _pulseAge += step;
            _flareAge += step;
            _learnedAge += step;
        }

        private float Pulse => _pulseAge < _settings.PulseSeconds
            ? _settings.QuarterPulse * (1f - _pulseAge / _settings.PulseSeconds)
            : 0f;

        private float Flare => _flareAge < _settings.FlareSeconds
            ? _settings.Flare * (1f - Mathf.Abs(2f * _flareAge / _settings.FlareSeconds - 1f))
            : 0f;
    }
}
