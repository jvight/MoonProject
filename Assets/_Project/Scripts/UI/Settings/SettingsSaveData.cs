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

        /// <summary>Language code ("en", "vi"); empty in saves written before a language was chosen.</summary>
        public string language = string.Empty;
    }
}
