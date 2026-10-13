using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// What 07's hold had no room for, held where it was (docs/features/M3-25 §2.3, 0.5.5 §2: a piece on its wreck, a
    /// remainder in its crate's mouth), for the survey chart's pencil crates: one per held stack until it is taken or
    /// Tilly brings it home.
    /// Registered in the GameContext by Gameplay. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IWaitingBundles
    {
        int Count { get; }

        /// <summary>Where bundle <paramref name="index"/> waits (world, over the ground).</summary>
        Vector3 Position(int index);
    }
}
