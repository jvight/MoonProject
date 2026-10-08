using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One available action: what, where in the world (for a diegetic or projected prompt) and whether it
    /// would happen right now if the player acted.</summary>
    public readonly struct InteractionHint
    {
        public InteractionHint(InteractionKind kind, Vector3 position, bool ready)
        {
            Kind = kind;
            Position = position;
            Ready = ready;
        }

        public InteractionKind Kind { get; }

        public Vector3 Position { get; }

        /// <summary>
        /// False when the action is there but waiting (sonar cooling down, not enough materials for the next level).
        /// </summary>
        public bool Ready { get; }

        public static InteractionHint None => new InteractionHint(InteractionKind.None, Vector3.zero, false);
    }
}
