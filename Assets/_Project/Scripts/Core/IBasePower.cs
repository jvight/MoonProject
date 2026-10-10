namespace MoonProject.Core
{
    /// <summary>
    /// The base's power stage (docs/features/M3-16), registered in the <see cref="GameContext"/> by the Gameplay domain
    /// and announced with <see cref="Events.BasePowerChanged"/>. World lights home, Art's dormant pieces wake, Audio
    /// plays the wake stinger and UI offers what just woke. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IBasePower
    {
        BasePowerStage Stage { get; }

        /// <summary>True once power has reached <paramref name="stage"/>.</summary>
        bool HasReached(BasePowerStage stage);
    }
}
