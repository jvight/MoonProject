using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The player's preferences as the pause menu edits them, applied straight to the services that own them
    /// (<see cref="IAudioSettings"/>, <see cref="ILookSettings"/>, <see cref="ILocalization"/>) and persisted by the
    /// UI's "ui.settings" save section. Slider positions are whole steps, so a gamepad reaches any value in a few
    /// presses.
    /// </summary>
    internal sealed class PlayerSettings
    {
        private readonly IAudioSettings _audio;
        private readonly ILookSettings _look;
        private readonly ILocalization _localization;
        private readonly PauseSettings _settings;

        public PlayerSettings(IAudioSettings audio, ILookSettings look, ILocalization localization,
            PauseSettings settings)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _look = look ?? throw new ArgumentNullException(nameof(look));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True when something changed since the last <see cref="MarkSaved"/>.</summary>
        public bool IsDirty { get; private set; }

        public int VolumeSteps => _settings.VolumeSteps;

        public int LookStepCount => LookStepOf(_settings.LookMax);

        public int MinLookStep => LookStepOf(_settings.LookMin);

        public int GetVolumeStep(AudioBus bus)
        {
            return Mathf.RoundToInt(_audio.GetVolume(bus) * _settings.VolumeSteps);
        }

        public void SetVolumeStep(AudioBus bus, int step)
        {
            _audio.SetVolume(bus, Mathf.Clamp(step, 0, _settings.VolumeSteps) / (float)_settings.VolumeSteps);
            IsDirty = true;
        }

        public int GetLookStep()
        {
            return Mathf.Clamp(LookStepOf(_look.Sensitivity), MinLookStep, LookStepCount);
        }

        /// <summary>The look multiplier a slider step stands for.</summary>
        public float LookValue(int step)
        {
            return step * _settings.LookStep;
        }

        public void SetLookStep(int step)
        {
            _look.Sensitivity = LookValue(Mathf.Clamp(step, MinLookStep, LookStepCount));
            IsDirty = true;
        }

        public bool InvertY
        {
            get => _look.InvertY;
            set
            {
                _look.InvertY = value;
                IsDirty = true;
            }
        }

        public string Language => _localization.Language;

        /// <summary>
        /// Switches to the language after the current one (the selector cycles; English comes first).
        /// </summary>
        public void NextLanguage()
        {
            IReadOnlyList<string> languages = _localization.Languages;
            int index = 0;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] == _localization.Language)
                {
                    index = i;
                }
            }

            _localization.SetLanguage(languages[(index + 1) % languages.Count]);
            IsDirty = true;
        }

        public void MarkSaved()
        {
            IsDirty = false;
        }

        public SettingsSaveData Capture()
        {
            return new SettingsSaveData
            {
                master = _audio.GetVolume(AudioBus.Master),
                music = _audio.GetVolume(AudioBus.Music),
                sfx = _audio.GetVolume(AudioBus.Sfx),
                ambience = _audio.GetVolume(AudioBus.Ambience),
                lookSensitivity = _look.Sensitivity,
                invertY = _look.InvertY,
                language = _localization.Language,
            };
        }

        /// <summary>
        /// Applies saved preferences; out-of-range or corrupt numbers are clamped by the owning services.
        /// </summary>
        public void Restore(SettingsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _audio.SetVolume(AudioBus.Master, Volume(data.master));
            _audio.SetVolume(AudioBus.Music, Volume(data.music));
            _audio.SetVolume(AudioBus.Sfx, Volume(data.sfx));
            _audio.SetVolume(AudioBus.Ambience, Volume(data.ambience));
            _look.Sensitivity = data.lookSensitivity;
            _look.InvertY = data.invertY;
            RestoreLanguage(data.language);
            IsDirty = false;
        }

        private void RestoreLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return;
            }

            IReadOnlyList<string> languages = _localization.Languages;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] == language)
                {
                    _localization.SetLanguage(language);
                    return;
                }
            }

            Debug.LogWarning($"{nameof(PlayerSettings)}: the saved language '{language}' is not available; keeping " +
                             $"'{_localization.Language}'.");
        }

        private static float Volume(float saved)
        {
            return float.IsNaN(saved) ? 1f : Mathf.Clamp01(saved);
        }

        private int LookStepOf(float multiplier)
        {
            return Mathf.RoundToInt(multiplier / _settings.LookStep);
        }
    }
}
