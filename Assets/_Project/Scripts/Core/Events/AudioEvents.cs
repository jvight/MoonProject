namespace MoonProject.Core.Events
{
    /// <summary>
    /// A voiced recording started on the radio (Ro's letters, the automated station): the ticker line with the same
    /// <see cref="Key"/> is its subtitle and stays up at least <see cref="Seconds"/>.
    /// </summary>
    public readonly struct VoiceLineStarted
    {
        public VoiceLineStarted(string key, float seconds)
        {
            Key = key;
            Seconds = seconds;
        }

        /// <summary>The ticker line's localization key.</summary>
        public string Key { get; }

        /// <summary>Length of the recording in seconds.</summary>
        public float Seconds { get; }
    }
}
