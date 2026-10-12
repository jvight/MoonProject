using UnityEngine;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>The WCAG contrast of text on its plate: (L1 + 0.05) / (L2 + 0.05) with sRGB relative luminance.</summary>
    internal static class Contrast
    {
        private const float Offset = 0.05f;
        private const float LinearThreshold = 0.04045f;
        private const float LinearScale = 12.92f;
        private const float Gamma = 2.4f;
        private const float GammaOffset = 0.055f;
        private const float GammaScale = 1.055f;
        private const float Red = 0.2126f;
        private const float Green = 0.7152f;
        private const float Blue = 0.0722f;

        /// <summary>The brightest dune of the Main scene: the worst backdrop a plate has to hold the text against.</summary>
        public static readonly Color32 BrightestDune = new Color32(0xA7, 0xA9, 0xCC, 0xFF);

        public static float Luminance(Color32 color)
        {
            return Red * Linear(color.r) + Green * Linear(color.g) + Blue * Linear(color.b);
        }

        public static float Ratio(Color32 text, Color32 background)
        {
            float a = Luminance(text);
            float b = Luminance(background);
            return (Mathf.Max(a, b) + Offset) / (Mathf.Min(a, b) + Offset);
        }

        /// <summary>UI Toolkit writes premultiplied colour: result = ui + backdrop x (1 - ui alpha).</summary>
        public static Color32 Over(Color32 ui, Color32 backdrop)
        {
            int keep = 255 - ui.a;
            return new Color32(
                (byte)Mathf.Min(255, ui.r + backdrop.r * keep / 255),
                (byte)Mathf.Min(255, ui.g + backdrop.g * keep / 255),
                (byte)Mathf.Min(255, ui.b + backdrop.b * keep / 255),
                255);
        }

        private static float Linear(byte channel)
        {
            float c = channel / 255f;
            return c <= LinearThreshold ? c / LinearScale : Mathf.Pow((c + GammaOffset) / GammaScale, Gamma);
        }
    }
}
