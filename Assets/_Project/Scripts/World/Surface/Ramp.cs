using UnityEngine;

namespace MoonProject.World
{
    /// <summary>A resolved play ramp in world space (see <see cref="RampPlacement"/> for the designer inputs).</summary>
    public readonly struct Ramp
    {
        public Ramp(Vector2 crest, Vector2 direction, float riseLength, float fallLength, float halfWidth, float height)
        {
            Crest = crest;
            Direction = direction;
            RiseLength = riseLength;
            FallLength = fallLength;
            HalfWidth = halfWidth;
            Height = height;
        }

        /// <summary>World XZ of the crest centre.</summary>
        public Vector2 Crest { get; }

        /// <summary>Unit XZ driving direction up the ramp.</summary>
        public Vector2 Direction { get; }

        public float RiseLength { get; }

        public float FallLength { get; }

        public float HalfWidth { get; }

        public float Height { get; }

        /// <summary>Radius of a circle around the crest that contains the whole ramp.</summary>
        public float BoundingRadius
        {
            get
            {
                float length = RiseLength > FallLength ? RiseLength : FallLength;
                return Mathf.Sqrt(length * length + HalfWidth * HalfWidth);
            }
        }
    }
}
