using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>One available action: what, where in the world (for a diegetic or projected prompt) and whether it
    /// would happen right now if the player acted.</summary>
    public readonly struct InteractionHint
    {
        public InteractionHint(InteractionKind kind, Vector3 position, bool ready, string nounKey = null,
            int setStep = 0, int setCount = 0, float holdProgress = 0f)
            : this(kind, position, ready, position, false, nounKey, setStep, setCount, holdProgress)
        {
        }

        /// <param name="target">Where the action happens, when its prompt stands elsewhere.</param>
        /// <param name="onRover">The prompt stands on 07 because <paramref name="target"/> is off screen.</param>
        public InteractionHint(InteractionKind kind, Vector3 position, bool ready, Vector3 target, bool onRover,
            string nounKey = null, int setStep = 0, int setCount = 0, float holdProgress = 0f)
        {
            Target = target;
            OnRover = onRover;
            HoldProgress = Mathf.Clamp01(holdProgress);
            Kind = kind;
            Position = position;
            Ready = ready;
            NounKey = nounKey;
            SetStep = setStep;
            SetCount = setCount;
        }

        public InteractionKind Kind { get; }

        /// <summary>Where the prompt stands: the action's place, or 07 while that place is off screen.</summary>
        public Vector3 Position { get; }

        /// <summary>Where the action happens (the apron, the hitch): <see cref="Position"/> unless it is on 07.</summary>
        public Vector3 Target { get; }

        /// <summary>
        /// The action's place is off screen, so its first-use prompt stands on 07 (and 07 looks toward
        /// <see cref="Target"/>).
        /// </summary>
        public bool OnRover { get; }

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

        /// <summary>
        /// The verb is a hold (scan, cut, weld, fold down), not a press (pick up, Build, Tab): its prompt's keycap carries a ring
        /// that fills with <see cref="HoldProgress"/>.
        /// </summary>
        public bool IsHold => Kind != InteractionKind.None && Kind != InteractionKind.PickUp &&
                              Kind != InteractionKind.OpenBuild && Kind != InteractionKind.FitPart &&
                              Kind != InteractionKind.Rack &&
                              Kind != InteractionKind.Tether;

        /// <summary>How far the hold has come while the key is down (0..1); 0 at rest.</summary>
        public float HoldProgress { get; }

        public static InteractionHint None => new InteractionHint(InteractionKind.None, Vector3.zero, false);
    }
}
