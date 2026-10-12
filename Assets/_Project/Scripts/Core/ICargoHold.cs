namespace MoonProject.Core
{
    /// <summary>
    /// 07's hold (docs/features/M3-25 §2), registered in the GameContext by Gameplay: what 07 carries, for the cargo
    /// screen, the visible stacks on 07's back and the suspension's settle. A bundle that does not fit waits in the
    /// world (<see cref="Events.CargoOverflowed"/>) until it is taken or Tilly fetches it.
    /// </summary>
    public interface ICargoHold : IMaterialSlots
    {
        /// <summary>Bundles waiting in the world because the hold was full.</summary>
        int WaitingBundles { get; }
    }
}
