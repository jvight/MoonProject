namespace MoonProject.Rover
{
    /// <summary>
    /// Which candidate camera placement <see cref="FaceInspectComposer"/> chose when probing for terrain or rock
    /// occlusion in front of 07's CRT screen (probed in strict order: Default, Left, Right, Closer).
    /// </summary>
    public enum FaceInspectProbeChoice
    {
        Default = 0,
        Left = 1,
        Right = 2,
        Closer = 3,
    }
}
