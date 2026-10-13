namespace MoonProject.Core
{
    /// <summary>
    /// The docked transfer view's one move (0.5.5 §3): a stack between 07's hold (<see cref="ICargoHold"/>) and
    /// home's shelves (<see cref="IHomeStorage"/>), registered in the GameContext by Gameplay. Valid only while 07
    /// rests on the dock (<see cref="IDockState"/>) and the dock's own unload has finished (it runs first, announced by
    /// <see cref="Events.CargoUnloaded"/>). A move publishes <see cref="Events.CargoChanged"/> and
    /// <see cref="Events.HomeStorageChanged"/>; the totals never change.
    /// </summary>
    public interface IStackTransfer
    {
        /// <summary>
        /// Moves one stack of <paramref name="material"/> (up to its stack size, as much as the other side has room
        /// for) to home (<paramref name="toHome"/>) or aboard 07. False, with <paramref name="moved"/> 0, when 07 is
        /// not docked, the unload is still running, the source holds none or the destination has no room.
        /// </summary>
        bool TryMove(SalvageMaterial material, bool toHome, out int moved);
    }
}
