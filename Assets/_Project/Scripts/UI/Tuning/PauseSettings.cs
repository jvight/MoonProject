using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The pause menu: how game time eases to a stop and back, the menu's motion and the settings ranges.
    /// </summary>
    [Serializable]
    public sealed class PauseSettings
    {
        [Tooltip("The menu panel easing in and out.")]
        [SerializeField] private RevealSettings _menu = new RevealSettings(0.2f, 0.3f, 12f, 0.96f, 1.6f, 0.65f);

        [Tooltip("The settings panel easing in beside the menu.")]
        [SerializeField] private RevealSettings _settingsPanel =
            new RevealSettings(0.3f, 0.25f, 10f, 0.97f, 1.8f, 0.7f);

        [Tooltip("A page of the menu (main buttons, quit question) crossfading with the other.")]
        [SerializeField] private RevealSettings _page = new RevealSettings(0.18f, 0.14f, 6f, 1f, 2f, 1f);

        [Tooltip("The soft indigo veil dimming the world behind the menu.")]
        [SerializeField] private RevealSettings _veil = new RevealSettings(0.25f, 0.2f, 0f, 1f, 1f, 1f);

        [Tooltip("The driving HUD (prompts, reticle, chip, cards) fading away while paused and back after.")]
        [SerializeField] private RevealSettings _hud = new RevealSettings(0.25f, 0.4f, 0f, 1f, 1.6f, 1f);

        [Tooltip("Real seconds over which game time eases to a stop when the menu opens.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float _freezeSeconds = 0.3f;

        [Tooltip("Real seconds over which game time eases back to full speed after resuming.")]
        [Range(0f, 2f)]
        [SerializeField] private float _resumeSeconds = 0.45f;

        [Tooltip("Steps on each volume slider (10 = 10 % per press of a direction).")]
        [Range(4, 20)]
        [SerializeField] private int _volumeSteps = 10;

        [Tooltip("Look speed per step of the slider (multiplier on the tuned camera sensitivity).")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _lookStep = 0.25f;

        [Tooltip("Lowest look speed multiplier offered.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _lookMin = 0.25f;

        [Tooltip("Look speed multiplier offered one step below the lowest while the Gentle camera is on.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _gentleLookMin = 0.15f;

        [Tooltip("Highest look speed multiplier offered.")]
        [Range(1f, 5f)]
        [SerializeField] private float _lookMax = 3f;

        [Tooltip("The text sizes the Settings row steps through, as multiples of the normal size (100, 125, 150 %). " +
                 "The whole UI scales with it; layouts wrap and never truncate.")]
        [SerializeField] private float[] _textScales = { 1f, 1.25f, 1.5f };

        public RevealSettings Menu => _menu;

        public RevealSettings SettingsPanel => _settingsPanel;

        public RevealSettings Page => _page;

        public RevealSettings Veil => _veil;

        public RevealSettings Hud => _hud;

        public float FreezeSeconds => _freezeSeconds;

        public float ResumeSeconds => _resumeSeconds;

        public int VolumeSteps => _volumeSteps;

        public float LookStep => _lookStep;

        public float LookMin => _lookMin;

        public float GentleLookMin => Mathf.Min(_gentleLookMin, _lookMin);

        public float LookMax => _lookMax;

        public float[] TextScales => _textScales;

        /// <summary>Null when usable, else the first problem.</summary>
        public string Validate()
        {
            if (_textScales == null || _textScales.Length == 0)
            {
                return "no text sizes are offered";
            }

            foreach (float scale in _textScales)
            {
                if (scale < 1f)
                {
                    return "a text size is below the normal size";
                }
            }

            return null;
        }
    }
}
