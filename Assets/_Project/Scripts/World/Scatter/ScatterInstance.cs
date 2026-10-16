using UnityEngine;

namespace MoonProject.World
{
    /// <summary>One placed rock: where it sits, how it is turned, scaled, and buried, and which shape it uses.</summary>
    public readonly struct ScatterInstance
    {
        public ScatterInstance(
            ScatterKind kind,
            Vector3 position,
            Vector3 normal,
            float yawDegrees,
            float size,
            int variant,
            Vector3 scaleAspect = default,
            float burialDepth = 0f)
        {
            Kind = kind;
            Position = position;
            Normal = normal;
            YawDegrees = yawDegrees;
            Size = size;
            Variant = variant;
            ScaleAspect = scaleAspect.sqrMagnitude > 1e-4f ? scaleAspect : Vector3.one;
            BurialDepth = burialDepth;
        }

        public ScatterKind Kind { get; }

        /// <summary>Ground contact point (on the analytic surface).</summary>
        public Vector3 Position { get; }

        /// <summary>Surface normal at <see cref="Position"/>.</summary>
        public Vector3 Normal { get; }

        public float YawDegrees { get; }

        /// <summary>Horizontal extent in metres.</summary>
        public float Size { get; }

        /// <summary>Seeded non-negative shape pick; the builder uses it modulo its number of source rocks.</summary>
        public int Variant { get; }

        /// <summary>Per-instance anisotropic 3D scale multiplier (with max horizontal axis = 1).</summary>
        public Vector3 ScaleAspect { get; }

        /// <summary>Additional per-instance burial depth in metres along the surface normal.</summary>
        public float BurialDepth { get; }
    }
}
