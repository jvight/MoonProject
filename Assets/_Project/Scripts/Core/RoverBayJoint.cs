namespace MoonProject.Core
{
    /// <summary>
    /// The joints of one of the Rover Bay's gantry arms, root to tip (docs/ARCHITECTURE.md, M3-14): the shoulder
    /// turns about the vertical (Yaw), then pitches (Upper), the elbow pitches (Lower), the wrist pitches (Tip).
    /// </summary>
    public enum RoverBayJoint
    {
        Upper = 0,
        Lower = 1,
        Tip = 2,
        SparkSocket = 3,
        Yaw = 4,
    }
}
