using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws a <see cref="UiIconShape"/> into a <see cref="Painter2D"/> at any target pixel size and state tint
    /// (<see cref="UiIconState.Lit"/>, <see cref="UiIconState.Faint"/>, or <see cref="UiIconState.Cyan"/>).
    /// </summary>
    public enum UiIconState
    {
        Lit = 0,
        Faint = 1,
        Cyan = 2,
    }

    /// <summary>
    /// Renders parsed 64x64 vector icons with <see cref="Painter2D"/> using the two-colour amber CRT palette.
    /// </summary>
    public static class UiIconPainter
    {
        public static readonly Color32 LitAmber = new Color32(255, 206, 116, 255);
        public static readonly Color32 FaintAmber = new Color32(224, 176, 94, 225);
        public static readonly Color32 CyanTint = new Color32(95, 243, 255, 255);
        private static readonly Color32 IconInteriorFill = new Color32(19, 18, 16, 255);

        public static Color32 ColorFor(UiIconState state)
        {
            switch (state)
            {
                case UiIconState.Faint:
                    return FaintAmber;
                case UiIconState.Cyan:
                    return CyanTint;
                default:
                    return LitAmber;
            }
        }

        public static void Draw(Painter2D painter, in UiIconShape icon, Vector2 center, float size, UiIconState state)
        {
            Draw(painter, in icon, center, size, ColorFor(state));
        }

        public static void Draw(Painter2D painter, in UiIconShape icon, Vector2 center, float size, Color32 tint)
        {
            float scale = size / UiIconShape.ViewBoxSize;
            Vector2 origin = new Vector2(center.x - size * 0.5f, center.y - size * 0.5f);
            Color32 detailStroke = new Color32(tint.r, tint.g, tint.b, (byte)Mathf.RoundToInt(tint.a * 0.92f));

            if (!icon.Fill.IsEmpty)
            {
                icon.Fill.Draw(painter, origin, scale, detailStroke, IconInteriorFill);
            }

            if (!icon.Primary.IsEmpty)
            {
                icon.Primary.Draw(painter, origin, scale, tint, default);
            }

            if (!icon.Detail.IsEmpty)
            {
                icon.Detail.Draw(painter, origin, scale, detailStroke, default);
            }
        }
    }
}
