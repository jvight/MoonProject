using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Something a spotter friend noticed: what kind, which one, and where.</summary>
    public readonly struct SpotTarget
    {
        public SpotTarget(SpotKind kind, int index, int part, Vector3 position)
        {
            Kind = kind;
            Index = index;
            Part = part;
            Position = position;
        }

        public SpotKind Kind { get; }

        /// <summary>Salvage site index, or friend index (for a part).</summary>
        public int Index { get; }

        /// <summary>Part index for a friend part, else -1.</summary>
        public int Part { get; }

        public Vector3 Position { get; }
    }
}
