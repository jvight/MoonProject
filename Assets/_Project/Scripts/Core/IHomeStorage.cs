namespace MoonProject.Core
{
    /// <summary>
    /// Home's supply shelves (docs/features/M3-25 §3), registered in the GameContext by Gameplay. Its slots are every
    /// built shelf's slots, in shelf order. Overflow crates from migrated saves count in
    /// <see cref="IMaterialSlots.Units"/> but hold no slot, and leave no room until spent.
    /// </summary>
    public interface IHomeStorage : IMaterialSlots
    {
        /// <summary>Shelves and free sockets, built or not.</summary>
        int ShelfCount { get; }

        HomeShelf GetShelf(int index);

        /// <summary>Units kept in overflow crates beside the shelves (migration only; never deleted).</summary>
        int OverflowUnits(SalvageMaterial material);
    }
}
