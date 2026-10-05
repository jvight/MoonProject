namespace MoonProject.Audio
{
    /// <summary>
    /// Volume buses (no AudioMixer asset): the effective volume of a sound is Master x its bus. Music carries the
    /// radio (tracks, static, tuning swish), Sfx every one-shot and rover/tether loop, Ambience the lunar bed.
    /// </summary>
    public enum AudioBus
    {
        Master = 0,
        Music = 1,
        Sfx = 2,
        Ambience = 3,
    }
}
