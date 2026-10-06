namespace MoonProject.UI
{
    /// <summary>The UI's save sections. Never rename a key: it identifies the data in existing saves.</summary>
    internal static class UiSaveKeys
    {
        /// <summary>Volumes, look speed, invert look and language (<see cref="SettingsSaveData"/>).</summary>
        public const string Settings = "ui.settings";
        public const int SettingsVersion = 1;

        /// <summary>Which context prompts have been taught (<see cref="PromptsSaveData"/>).</summary>
        public const string Prompts = "ui.prompts";
        public const int PromptsVersion = 1;
    }
}
