using System;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// How a primitive picks a <see cref="PaletteSwatch"/> for each of its faces. Converts implicitly from a single
    /// swatch, so <c>builder.Box(at, size, PaletteSwatch.Cream)</c> paints the whole box cream.
    /// </summary>
    public readonly struct Paint
    {
        private Paint(PaletteSwatch main, PaletteSwatch alt, PaintRule rule, Vector3 direction, float minDot)
        {
            Main = main;
            Alt = alt;
            Rule = rule;
            Direction = direction;
            MinDot = minDot;
        }

        public PaletteSwatch Main { get; }

        public PaletteSwatch Alt { get; }

        public PaintRule Rule { get; }

        /// <summary>Unit direction tested by <see cref="PaintRule.Facing"/>, in the output mesh's space.</summary>
        public Vector3 Direction { get; }

        /// <summary>A face uses <see cref="Main"/> when dot(faceNormal, Direction) is at least this value.</summary>
        public float MinDot { get; }

        public static Paint Solid(PaletteSwatch swatch)
        {
            return new Paint(swatch, swatch, PaintRule.Solid, Vector3.up, 0f);
        }

        /// <summary>Side faces get <paramref name="sides"/>, cap faces get <paramref name="caps"/>.</summary>
        public static Paint WithCaps(PaletteSwatch sides, PaletteSwatch caps)
        {
            return new Paint(sides, caps, PaintRule.Caps, Vector3.up, 0f);
        }

        /// <summary>
        /// Faces whose final normal satisfies dot(normal, direction) &gt;= minDot get <paramref name="facing"/>, the
        /// rest get <paramref name="other"/>. Evaluated after placement and displacement (e.g. two-tone rocks).
        /// </summary>
        public static Paint Facing(PaletteSwatch facing, PaletteSwatch other, Vector3 direction, float minDot)
        {
            if (direction.sqrMagnitude < 1e-12f)
            {
                throw new ArgumentException("Facing paint needs a non-zero direction.", nameof(direction));
            }

            return new Paint(facing, other, PaintRule.Facing, direction.normalized, minDot);
        }

        public static implicit operator Paint(PaletteSwatch swatch)
        {
            return Solid(swatch);
        }

        internal PaletteSwatch Resolve(bool isCap, Vector3 normal)
        {
            switch (Rule)
            {
                case PaintRule.Caps:
                    return isCap ? Alt : Main;
                case PaintRule.Facing:
                    return Vector3.Dot(normal, Direction) >= MinDot ? Main : Alt;
                default:
                    return Main;
            }
        }
    }
}
