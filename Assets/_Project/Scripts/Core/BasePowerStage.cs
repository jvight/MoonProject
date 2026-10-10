namespace MoonProject.Core
{
    /// <summary>
    /// How far power has come back through the base (docs/features/M3-16): each radio tower level wakes one more stage.
    /// A fresh game is <see cref="Asleep"/>: only 07's charging dock glows.
    /// </summary>
    public enum BasePowerStage
    {
        Asleep = 0,
        Home = 1,
        Bay = 2,
        Lift = 3,
    }
}
