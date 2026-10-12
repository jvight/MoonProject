using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The player's preferences as the pause menu edits them, applied straight to the services that own them
    /// (<see cref="IAudioSettings"/>, <see cref="ILookSettings"/>, <see cref="IFeelSettings"/>,
    /// <see cref="ILocalization"/>) and persisted by the
    /// UI's "ui.settings" save section. Slider positions are whole steps, so a gamepad reaches any value in a few
    /// presses; the Gentle camera adds one step below the lowest, standing for its lower look floor.
    /// </summary>
    internal sealed class PlayerSettings
    {
        private readonly IAudioSettings _audio;
        private readonly ILookSettings _look;
        private readonly IFeelSettings _feel;
        private readonly ILocalization _localization;
        private readonly PauseSettings _settings;
        private bool _guidance = true;
        private readonly string[] _textLabels;
        private int _textSize;

        public PlayerSettings(IAudioSettings audio, ILookSettings look, IFeelSettings feel,
            ILocalization localization, PauseSettings settings)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _look = look ?? throw new ArgumentNullException(nameof(look));
            _feel = feel ?? throw new ArgumentNullException(nameof(feel));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _textLabels = new string[settings.TextScales.Length];
            for (int i = 0; i < _textLabels.Length; i++)
            {
                _textLabels[i] = Mathf.RoundToInt(settings.TextScales[i] * 100f) + "%";
            }
        }

        /// <summary>True when something changed since the last <see cref="MarkSaved"/>.</summary>
        public bool IsDirty { get; private set; }

        public int VolumeSteps => _settings.VolumeSteps;

        public int LookStepCount => LookStepOf(_settings.LookMax);

        /// <summary>The slider's lowest step now: one lower while the Gentle camera is on.</summary>
        public int MinLookStep => _look.GentleCamera ? GentleLookStep : UsualMinLookStep;

        /// <summary>The step below the usual lowest, which stands for the Gentle camera's look floor.</summary>
        public int GentleLookStep => UsualMinLookStep - 1;

        private int UsualMinLookStep => LookStepOf(_settings.LookMin);

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
            float sensitivity = _look.Sensitivity;
            bool gentleFloor = sensitivity < 0.5f * (_settings.GentleLookMin + _settings.LookMin);
            return Mathf.Clamp(gentleFloor ? GentleLookStep : LookStepOf(sensitivity), MinLookStep, LookStepCount);
        }

        /// <summary>The look multiplier a slider step stands for.</summary>
        public float LookValue(int step)
        {
            return step < UsualMinLookStep ? _settings.GentleLookMin : step * _settings.LookStep;
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

        /// <summary>Gamepad rumble is on (the default).</summary>
        public bool Rumble
        {
            get => _feel.RumbleEnabled;
            set
            {
                _feel.RumbleEnabled = value;
                IsDirty = true;
            }
        }

        /// <summary>The rumble strength's slider step: 0 is none, <see cref="VolumeSteps"/> is as tuned.</summary>
        public int GetRumbleStep()
        {
            return Mathf.RoundToInt(_feel.RumbleStrength * _settings.VolumeSteps);
        }

        public void SetRumbleStep(int step)
        {
            _feel.RumbleStrength = Mathf.Clamp(step, 0, _settings.VolumeSteps) / (float)_settings.VolumeSteps;
            IsDirty = true;
        }

        public bool GentleCamera
        {
            get => _look.GentleCamera;
            set
            {
                _look.GentleCamera = value;
                IsDirty = true;
            }
        }

        /// <summary>The soft "next" line is wanted (on unless the player switched it off).</summary>
        public bool Guidance
        {
            get => _guidance;
            set
            {
                _guidance = value;
                IsDirty = true;
            }
        }

        /// <summary>The text size step: 0 is 100%, 1 is 125%, 2 is 150%.</summary>
        public int TextSize => _textSize;

        /// <summary>The text size as the Settings row words it ("125%").</summary>
        public string TextSizeLabel => _textLabels[_textSize];

        /// <summary>The text size as a multiple of the normal size.</summary>
        public float TextScale => _settings.TextScales[_textSize];

        /// <summary>Steps to the next text size, the last wrapping round to the first.</summary>
        public void NextTextSize()
        {
            _textSize = (_textSize + 1) % _settings.TextScales.Length;
            IsDirty = true;
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

        /// <summary>The first boot's language step: <paramref name="language"/> is one of the tables' codes.</summary>
        public void ChooseLanguage(string language)
        {
            _localization.SetLanguage(language);
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
                gentleCamera = _look.GentleCamera,
                rumbleOff = !_feel.RumbleEnabled,
                rumbleStrength = _feel.RumbleStrength,
                guidanceOff = !_guidance,
                textScale = TextScale,
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
            _look.GentleCamera = data.gentleCamera;
            _look.Sensitivity = data.lookSensitivity;
            _look.InvertY = data.invertY;
            _feel.RumbleEnabled = !data.rumbleOff;
            _feel.RumbleStrength = Volume(data.rumbleStrength);
            _guidance = !data.guidanceOff;
            _textSize = NearestTextSize(data.textScale);
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

        private int NearestTextSize(float saved)
        {
            int nearest = 0;
            float[] scales = _settings.TextScales;
            for (int i = 1; i < scales.Length; i++)
            {
                if (Mathf.Abs(scales[i] - saved) < Mathf.Abs(scales[nearest] - saved))
                {
                    nearest = i;
                }
            }

            return nearest;
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
