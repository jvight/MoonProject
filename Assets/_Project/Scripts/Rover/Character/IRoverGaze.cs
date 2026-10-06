using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Points 07's head at things it interacts with. Registered in the GameContext by <see cref="RoverBodyLanguage"/>.
    /// Gameplay sets a target per priority (update it every frame for moving targets) and clears it with null.
    /// </summary>
    public interface IRoverGaze
    {
        /// <summary>Looks at <paramref name="worldPoint"/> at this priority; null clears that priority.</summary>
        void SetGazeTarget(GazePriority priority, Vector3? worldPoint);

        /// <summary>A small, eased perk-up (head lift, brighter eye, antenna wiggle). Strength 0..1.</summary>
        void PerkUp(float strength);
    }
}
