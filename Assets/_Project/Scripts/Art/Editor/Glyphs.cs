using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Hand-painted block letters for signs and stickers: simple polygons in a 0.14 x 0.2 cell (origin bottom-left,
    /// x right, y up), ready for <see cref="LowPolyMeshBuilder.Extrude"/>. Each call returns a fresh array.
    /// </summary>
    internal static class Glyphs
    {
        public const float Advance = 0.18f;

        public static Vector2[] E()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.13f, 0f), new Vector2(0.13f, 0.04f), new Vector2(0.04f, 0.04f),
                new Vector2(0.04f, 0.08f), new Vector2(0.11f, 0.08f), new Vector2(0.11f, 0.12f),
                new Vector2(0.04f, 0.12f), new Vector2(0.04f, 0.16f), new Vector2(0.13f, 0.16f),
                new Vector2(0.13f, 0.2f), new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] H()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.04f, 0.08f), new Vector2(0.1f, 0.08f),
                new Vector2(0.1f, 0f), new Vector2(0.14f, 0f), new Vector2(0.14f, 0.2f), new Vector2(0.1f, 0.2f),
                new Vector2(0.1f, 0.12f), new Vector2(0.04f, 0.12f), new Vector2(0.04f, 0.2f), new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] I()
        {
            return new[]
            {
                new Vector2(0.05f, 0f), new Vector2(0.09f, 0f), new Vector2(0.09f, 0.2f), new Vector2(0.05f, 0.2f),
            };
        }

        public static Vector2[] M()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.04f, 0.12f), new Vector2(0.07f, 0.07f),
                new Vector2(0.1f, 0.12f), new Vector2(0.1f, 0f), new Vector2(0.14f, 0f), new Vector2(0.14f, 0.2f),
                new Vector2(0.1f, 0.2f), new Vector2(0.07f, 0.13f), new Vector2(0.04f, 0.2f), new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] N()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.04f, 0.12f), new Vector2(0.1f, 0f),
                new Vector2(0.14f, 0f), new Vector2(0.14f, 0.2f), new Vector2(0.1f, 0.2f), new Vector2(0.1f, 0.08f),
                new Vector2(0.04f, 0.2f), new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] S()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.13f, 0f), new Vector2(0.13f, 0.12f), new Vector2(0.04f, 0.12f),
                new Vector2(0.04f, 0.16f), new Vector2(0.13f, 0.16f), new Vector2(0.13f, 0.2f), new Vector2(0f, 0.2f),
                new Vector2(0f, 0.08f), new Vector2(0.09f, 0.08f), new Vector2(0.09f, 0.04f), new Vector2(0f, 0.04f),
            };
        }

        /// <summary>A five-pointed star of the given outer and inner radius, centred on the origin.</summary>
        public static Vector2[] Star(float outer, float inner)
        {
            var star = new Vector2[10];
            for (int i = 0; i < star.Length; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = i * Mathf.PI / 5f;
                star[i] = new Vector2(radius * Mathf.Sin(angle), radius * Mathf.Cos(angle));
            }

            return star;
        }
    }
}
