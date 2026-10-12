using UnityEngine;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// Bundles left waiting in the world because 07's hold was full (docs/features/M3-25 §2.3), for the survey chart's
    /// pencil crates: one per bundle from its <c>CargoOverflowed</c> until it is taken or Tilly brings it home.
    /// Registered in the GameContext by Gameplay. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IWaitingBundles
    {
        int Count { get; }

        /// <summary>Where bundle <paramref name="index"/> waits (world, over the ground).</summary>
        Vector3 Position(int index);
    }
}
