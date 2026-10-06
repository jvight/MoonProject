using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Hand-painted block letters for signs and stickers: simple polygons in a 0.14 x 0.2 cell (origin bottom-left,
    /// x right, y up), ready for <see cref="LowPolyMeshBuilder.Extrude"/>. Each call returns a fresh array. Letters
    /// with a hole (0, O, P, R) are solid outlines whose counter is painted over in the paper colour.
    /// </summary>
    internal static class Glyphs
    {
        public const float Advance = 0.18f;
        public const float CellHeight = 0.2f;

        /// <summary>Thickness of the paint, in cell units (so it scales with the letters).</summary>
        private const float InkDepth = 0.04f;
        private const float CounterThickness = 0.01f;

        /// <summary>
        /// Writes <paramref name="text"/> centred on <paramref name="frame"/>'s origin, readable by someone standing on
        /// the frame's +Z side (+Y up), letters <paramref name="height"/> tall. The ink sits on the frame's XY plane
        /// and stands out towards +Z. Spaces advance; a letter without a glyph throws.
        /// </summary>
        public static void Write(LowPolyMeshBuilder b, Matrix4x4 frame, string text, float height, PaletteSwatch ink,
            PaletteSwatch paper)
        {
            float scale = height / CellHeight;
            float width = ((text.Length - 1) * Advance + 0.14f) * scale;
            // The reader looks along -Z, so +X runs to their left: lay the line out in a frame turned to face them.
            Matrix4x4 reading = frame * Matrix4x4.Rotate(Place.Rotation(new Vector3(0f, 180f, 0f)));
            float lift = InkDepth * 0.5f * scale;
            for (int i = 0; i < text.Length; i++)
            {
                char letter = text[i];
                if (letter == ' ')
                {
                    continue;
                }

                var origin = new Vector3(-width * 0.5f + i * Advance * scale, -height * 0.5f, -lift);
                Matrix4x4 cell = reading * Place.At(origin, Vector3.zero, Vector3.one * scale);
                b.Extrude(cell, For(letter), InkDepth, ink);
                if (TryGetCounter(letter, out Rect counter))
                {
                    var centre = new Vector3(counter.center.x, counter.center.y, -(InkDepth + CounterThickness) * 0.5f);
                    b.Box(cell * Place.At(centre), new Vector3(counter.width, counter.height, CounterThickness), paper);
                }
            }
        }

        /// <summary>The outline of <paramref name="letter"/> (upper case, digits 0 and 7).</summary>
        public static Vector2[] For(char letter)
        {
            switch (letter)
            {
                case '0':
                case 'O':
                    return Zero();
                case '7':
                    return Seven();
                case 'C':
                    return C();
                case 'E':
                    return E();
                case 'H':
                    return H();
                case 'I':
                    return I();
                case 'M':
                    return M();
                case 'N':
                    return N();
                case 'P':
                    return P();
                case 'R':
                    return R();
                case 'S':
                    return S();
                case 'T':
                    return T();
                case 'W':
                    return W();
                default:
                    throw new System.ArgumentException($"No glyph for '{letter}'.", nameof(letter));
            }
        }

        /// <summary>The hole of a closed letter, in cell units, to paint over in the paper colour.</summary>
        public static bool TryGetCounter(char letter, out Rect counter)
        {
            switch (letter)
            {
                case '0':
                case 'O':
                    counter = new Rect(0.045f, 0.045f, 0.05f, 0.11f);
                    return true;
                case 'P':
                case 'R':
                    counter = new Rect(0.04f, 0.12f, 0.06f, 0.045f);
                    return true;
                default:
                    counter = default;
                    return false;
            }
        }

        public static Vector2[] Zero()
        {
            return new[]
            {
                new Vector2(0.03f, 0f), new Vector2(0.11f, 0f), new Vector2(0.14f, 0.03f), new Vector2(0.14f, 0.17f),
                new Vector2(0.11f, 0.2f), new Vector2(0.03f, 0.2f), new Vector2(0f, 0.17f), new Vector2(0f, 0.03f),
            };
        }

        public static Vector2[] Seven()
        {
            return new[]
            {
                new Vector2(0f, 0.2f), new Vector2(0.14f, 0.2f), new Vector2(0.14f, 0.165f), new Vector2(0.065f, 0f),
                new Vector2(0.022f, 0f), new Vector2(0.094f, 0.162f), new Vector2(0f, 0.162f),
            };
        }

        public static Vector2[] C()
        {
            return new[]
            {
                new Vector2(0.025f, 0f), new Vector2(0.13f, 0f), new Vector2(0.13f, 0.04f), new Vector2(0.04f, 0.04f),
                new Vector2(0.04f, 0.16f), new Vector2(0.13f, 0.16f), new Vector2(0.13f, 0.2f),
                new Vector2(0.025f, 0.2f), new Vector2(0f, 0.175f), new Vector2(0f, 0.025f),
            };
        }

        public static Vector2[] P()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.04f, 0.08f), new Vector2(0.115f, 0.08f),
                new Vector2(0.14f, 0.105f), new Vector2(0.14f, 0.175f), new Vector2(0.115f, 0.2f),
                new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] R()
        {
            return new[]
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0f), new Vector2(0.04f, 0.08f), new Vector2(0.065f, 0.08f),
                new Vector2(0.105f, 0f), new Vector2(0.148f, 0f), new Vector2(0.104f, 0.087f),
                new Vector2(0.14f, 0.11f), new Vector2(0.14f, 0.175f), new Vector2(0.115f, 0.2f), new Vector2(0f, 0.2f),
            };
        }

        public static Vector2[] T()
        {
            return new[]
            {
                new Vector2(0.05f, 0f), new Vector2(0.09f, 0f), new Vector2(0.09f, 0.16f), new Vector2(0.14f, 0.16f),
                new Vector2(0.14f, 0.2f), new Vector2(0f, 0.2f), new Vector2(0f, 0.16f), new Vector2(0.05f, 0.16f),
            };
        }

        public static Vector2[] W()
        {
            return new[]
            {
                new Vector2(0f, 0.2f), new Vector2(0.02f, 0f), new Vector2(0.05f, 0f), new Vector2(0.07f, 0.1f),
                new Vector2(0.09f, 0f), new Vector2(0.12f, 0f), new Vector2(0.14f, 0.2f), new Vector2(0.1f, 0.2f),
                new Vector2(0.09f, 0.08f), new Vector2(0.07f, 0.15f), new Vector2(0.05f, 0.08f),
                new Vector2(0.04f, 0.2f),
            };
        }

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
