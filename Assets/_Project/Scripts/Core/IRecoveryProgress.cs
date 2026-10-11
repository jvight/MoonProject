namespace MoonProject.Core
{
    /// <summary>
    /// Progress on the four main recovery targets (docs/features/M3-21), registered in the <see cref="GameContext"/> by
    /// the Gameplay domain and announced with <see cref="Events.RecoveryProgressChanged"/>. The recovery panel lights
    /// its lamps from it and the survey chart draws the Link's line. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IRecoveryProgress
    {
        /// <summary>Lamps lit in <paramref name="target"/>'s row.</summary>
        int LitLamps(RecoveryTarget target);

        /// <summary>Lamps in <paramref name="target"/>'s row in this version of the game.</summary>
        int Lamps(RecoveryTarget target);

        /// <summary>True once every lamp of <paramref name="target"/>'s row is lit.</summary>
        bool IsComplete(RecoveryTarget target);
    }
}
