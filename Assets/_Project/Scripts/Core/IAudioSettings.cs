namespace MoonProject.Core
{
    /// <summary>
    /// Player-facing volume control, registered in the <see cref="GameContext"/> by the Audio domain so UI can drive
    /// it without referencing Audio.
    /// </summary>
    public interface IAudioSettings
    {
        /// <summary>Linear volume 0..1.</summary>
        float GetVolume(AudioBus bus);

        /// <summary>Sets a linear volume 0..1 (clamped); takes effect immediately.</summary>
        void SetVolume(AudioBus bus, float volume);
    }
}
