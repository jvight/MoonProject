using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Save section "ui.settings", version 1: the player's volumes (linear 0..1), camera look preferences and language
    /// (JsonUtility DTO; field names are the JSON keys).
    /// </summary>
    [Serializable]
    public sealed class SettingsSaveData
    {
        public float master = 1f;
        public float music = 1f;
        public float sfx = 1f;
        public float ambience = 1f;
        public float lookSensitivity = 1f;
        public bool invertY;

        /// <summary>The Gentle camera (off in saves written before it existed).</summary>
        public bool gentleCamera;

        /// <summary>The Guidance setting switched off (stored inverted so older saves keep it on).</summary>
        public bool guidanceOff;

        /// <summary>Gamepad rumble switched off (stored inverted so older saves keep it on).</summary>
        public bool rumbleOff;

        /// <summary>The rumble strength multiplier, 0..1; older saves read 1.</summary>
        public float rumbleStrength = 1f;

        /// <summary>The text size as a multiple of the normal size (1, 1.25, 1.5); older saves read 1.</summary>
        public float textScale = 1f;

        /// <summary>Language code ("en", "vi"); empty in saves written before a language was chosen.</summary>
        public string language = string.Empty;
    }
}
