using System;
using UnityEngine;
using MoonProject.Core.Events;
using MoonProject.World;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Plain C# state machine for the Showcase time-of-day controls: T jumps through the 6 ordered sky phases
    /// (eclipse hold, release / dawn bead, sunrise, full sunlit, sunset, earthlit night), [ / ] scrub the clock, and
    /// P toggles 60x time speed.
    /// </summary>
    public sealed class ShowcasePhaseStepper
    {
        private readonly ShowcaseSkyPreset[] _presets;
        private readonly float _dayLengthSeconds;
        private readonly float _scrubSecondsPerSecond;
        private readonly float _fastTimeScale;
        private int _currentPresetIndex;
        private bool _isFastTime;

        public ShowcasePhaseStepper(
            ShowcaseSkyPreset[] presets,
            float dayLengthSeconds = 7200f,
            float scrubSecondsPerSecond = 600f,
            float fastTimeScale = 60f,
            int initialPresetIndex = 3)
        {
            if (presets == null || presets.Length == 0)
            {
                throw new ArgumentException("Presets array must not be null or empty.", nameof(presets));
            }

            _presets = presets;
            _dayLengthSeconds = Mathf.Max(60f, dayLengthSeconds);
            _scrubSecondsPerSecond = Mathf.Max(1f, scrubSecondsPerSecond);
            _fastTimeScale = Mathf.Max(1f, fastTimeScale);
            _currentPresetIndex = Mathf.Clamp(initialPresetIndex, 0, _presets.Length - 1);
            _isFastTime = false;
        }

        public int PresetCount => _presets.Length;
        public int CurrentPresetIndex => _currentPresetIndex;
        public ShowcaseSkyPreset CurrentPreset => _presets[_currentPresetIndex];
        public bool IsFastTime => _isFastTime;
        public float TimeScale => _isFastTime ? _fastTimeScale : 1f;

        /// <summary>Advances to the next sky phase preset in the cycle and returns it.</summary>
        public ShowcaseSkyPreset NextPreset()
        {
            _currentPresetIndex = (_currentPresetIndex + 1) % _presets.Length;
            return _presets[_currentPresetIndex];
        }

        /// <summary>Selects a specific preset by index and returns it.</summary>
        public ShowcaseSkyPreset SelectPreset(int index)
        {
            _currentPresetIndex = ((index % _presets.Length) + _presets.Length) % _presets.Length;
            return _presets[_currentPresetIndex];
        }

        /// <summary>Selects a specific preset by <see cref="ShowcaseSkyPresetId"/> and returns it.</summary>
        public ShowcaseSkyPreset SelectPreset(ShowcaseSkyPresetId id)
        {
            for (int i = 0; i < _presets.Length; i++)
            {
                if (_presets[i].PresetId == id)
                {
                    _currentPresetIndex = i;
                    return _presets[i];
                }
            }

            return _presets[_currentPresetIndex];
        }

        /// <summary>Toggles 60x fast-time speed (P key) and returns the new state.</summary>
        public bool ToggleFastTime()
        {
            _isFastTime = !_isFastTime;
            return _isFastTime;
        }

        public void SetFastTime(bool fast)
        {
            _isFastTime = fast;
        }

        /// <summary>
        /// Computes the new clock time in seconds when scrubbing with [ (-1) or ] (+1). Wraps cleanly around
        /// <see cref="_dayLengthSeconds"/>.
        /// </summary>
        public float ComputeScrubbedTime(float currentTimeSeconds, float scrubAxis, float deltaTime)
        {
            if (Mathf.Abs(scrubAxis) <= 1e-4f || deltaTime <= 0f)
            {
                return currentTimeSeconds;
            }

            float deltaClock = scrubAxis * _scrubSecondsPerSecond * deltaTime;
            float next = (currentTimeSeconds + deltaClock) % _dayLengthSeconds;
            return next < 0f ? next + _dayLengthSeconds : next;
        }

        /// <summary>Returns the Vietnamese display name for the live sky state.</summary>
        public static string FormatLivePhaseVi(SkyPhase phase, bool isHoldingEclipse, float eclipseFactor)
        {
            if (isHoldingEclipse)
            {
                return "Nhật thực toàn phần (Giữ)";
            }

            if (phase == SkyPhase.Eclipse || eclipseFactor > 0.01f)
            {
                return eclipseFactor >= 0.98f
                    ? "Nhật thực toàn phần"
                    : "Thoát nhật thực / Hạt quang";
            }

            switch (phase)
            {
                case SkyPhase.Sunrise:
                    return "Bình minh Mặt Trăng";
                case SkyPhase.Sunlit:
                    return "Ban ngày nắng gắt";
                case SkyPhase.Sunset:
                    return "Hoàng hôn Mặt Trăng";
                case SkyPhase.Earthlit:
                    return "Đêm ánh Địa Cầu";
                default:
                    return phase.ToString();
            }
        }

        /// <summary>Formats the lunar day clock (0..7200s) as a 24h-equivalent lunar cycle clock plus percentage.</summary>
        public string FormatClockReadout(float timeSeconds, bool isHoldingEclipse)
        {
            if (isHoldingEclipse)
            {
                return "00:00 (Giữ nhật thực)";
            }

            float wrapped = timeSeconds % _dayLengthSeconds;
            if (wrapped < 0f)
            {
                wrapped += _dayLengthSeconds;
            }

            int totalMinutes = Mathf.FloorToInt(wrapped / 60f);
            int seconds = Mathf.FloorToInt(wrapped % 60f);
            int percent = Mathf.RoundToInt((wrapped / _dayLengthSeconds) * 100f);
            return $"{totalMinutes:D2}:{seconds:D2} ({percent}%)";
        }
    }
}
