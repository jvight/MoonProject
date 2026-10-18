using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A held E would start <see cref="Kind"/> on the target at <see cref="Point"/> now, but cannot, for
    /// <see cref="Reason"/>. Published once per hold, so the UI can answer the key instead of leaving it dead. It lives
    /// beside <see cref="InteractionKind"/> because Core cannot name the gameplay's verbs.
    /// </summary>
    public readonly struct InteractionBlocked
    {
        public InteractionBlocked(InteractionKind kind, Vector3 point, BlockReason reason)
        {
            Kind = kind;
            Point = point;
            Reason = reason;
        }

        public InteractionKind Kind { get; }

        public Vector3 Point { get; }

        public BlockReason Reason { get; }
    }
}
