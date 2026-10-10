namespace MoonProject.Core
{
    /// <summary>
    /// How lit each of home's warm points is right now (docs/features/M3-16), registered in the
    /// <see cref="GameContext"/> by the World domain from <see cref="IBasePower"/>. Whoever drives a lamp (the base's
    /// glow, a station's lights) multiplies its linear glow and light intensity by <see cref="Level"/>.
    /// Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IHomeLights
    {
        /// <summary>0 (dark) to 1 (fully lit), eased while a stage wakes.</summary>
        float Level(HomeLight light);
    }
}
