namespace MoonProject.Rover
{
    /// <summary>
    /// How far 07 joins the player's look up at the sky (0..1, eased), registered by the camera rig and read by 07's
    /// body language to lift its head, open its wing a little and soften its eye.
    /// </summary>
    public interface ISkyGaze
    {
        float SkyLift { get; }
    }
}
