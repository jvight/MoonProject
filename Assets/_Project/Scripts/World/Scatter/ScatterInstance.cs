using UnityEngine;

namespace MoonProject.World
{
    /// <summary>One placed rock: where it sits, how it is turned and sized, and which shape it uses.</summary>
    public readonly struct ScatterInstance
    {
        public ScatterInstance(ScatterKind kind, Vector3 position, Vector3 normal, float yawDegrees, float size,
            int variant)
        {
            Kind = kind;
            Position = position;
            Normal = normal;
            YawDegrees = yawDegrees;
            Size = size;
            Variant = variant;
        }

        public ScatterKind Kind { get; }

        /// <summary>Ground contact point (on the analytic surface).</summary>
        public Vector3 Position { get; }

        /// <summary>Surface normal at <see cref="Position"/>.</summary>
        public Vector3 Normal { get; }

        public float YawDegrees { get; }

        /// <summary>Horizontal extent in metres (RockGenerator's size).</summary>
        public float Size { get; }

        /// <summary>Seeded non-negative shape pick; the builder uses it modulo its number of source rocks.</summary>
        public int Variant { get; }
    }
}
