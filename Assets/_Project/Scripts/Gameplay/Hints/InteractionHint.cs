using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One available action: what, where in the world (for a diegetic or projected prompt) and whether it
    /// would happen right now if the player acted.</summary>
    public readonly struct InteractionHint
    {
        public InteractionHint(InteractionKind kind, Vector3 position, bool ready, string nounKey = null,
            int setStep = 0, int setCount = 0)
        {
            Kind = kind;
            Position = position;
            Ready = ready;
            NounKey = nounKey;
            SetStep = setStep;
            SetCount = setCount;
        }

        public InteractionKind Kind { get; }

        public Vector3 Position { get; }

        /// <summary>
        /// False when the action is there but waiting (sonar cooling down, not enough materials for the next level).
        /// </summary>
        public bool Ready { get; }

        /// <summary>The string key of what the verb acts on (the part's name for a pickup), or null.</summary>
        public string NounKey { get; }

        /// <summary>
        /// For a scan of one piece of an unlearned structure set: the piece this scan would make it (1-based), of
        /// <see cref="SetCount"/>; 0 otherwise.
        /// </summary>
        public int SetStep { get; }

        /// <summary>The pieces in that set (0 when the hint is not a set piece's scan).</summary>
        public int SetCount { get; }

        public static InteractionHint None => new InteractionHint(InteractionKind.None, Vector3.zero, false);
    }
}
