using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Read-only tether state for the UI reticle, registered in the GameContext. Poll it each frame; it never
    /// allocates.
    /// </summary>
    public interface ITetherAim
    {
        TetherAimState State { get; }

        /// <summary>World position of what is hovered or towed (valid unless <see cref="State"/> is Idle).</summary>
        Vector3 TargetPosition { get; }

        /// <summary>0..1: how close the tether is to letting go softly.</summary>
        float Strain { get; }

        /// <summary>Current tether length (m) while towing.</summary>
        float Length { get; }
    }
}
