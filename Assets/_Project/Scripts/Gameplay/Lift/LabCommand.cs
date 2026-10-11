namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// What the player asks of the lab's turntable (docs/features/M3-19 §5) with the drive stick: left and right turn
    /// to the next station, forward looks at it, back comes away from a look or brings the platform down.
    /// </summary>
    public enum LabCommand
    {
        None = 0,
        TurnLeft = 1,
        TurnRight = 2,
        Look = 3,
        Down = 4,
    }
}
