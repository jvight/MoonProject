using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Live state of one friend machine, owned by the Gameplay domain and read every frame by Audio, UI and Rover.
    /// </summary>
    public interface IFriendState
    {
        /// <summary>Stable friend id, e.g. "tilly".</summary>
        string Id { get; }

        /// <summary>Interpolated visual position (voices and prompts follow it).</summary>
        Vector3 Position { get; }

        FriendActivity Activity { get; }

        /// <summary>0..1 rotor/motor effort: 0 = stopped, ~0.4 hovering, 1 = dashing.</summary>
        float RotorSpeed { get; }

        /// <summary>0..1 while <see cref="FriendActivity.Repairing"/>; otherwise 0 (not yet repaired) or 1.</summary>
        float RepairProgress { get; }
    }
}
