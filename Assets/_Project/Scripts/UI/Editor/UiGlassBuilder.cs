using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Renders 07's Variant C ("Terminal hai màu") CRT surfaces as textures, since UI Toolkit has no gradients,
    /// scanline shaders, or box shadows: a dark warm phosphor surface with a crisp amber bezel, subtle corner bracket
    /// accents, faint inner phosphor bloom, and subtle low-opacity horizontal scanlines in the unstretched zones.
    /// Drawn at twice the size the USS shows them (<c>-unity-slice-scale: 0.5</c>), so the corners stay crisp at 4K
    /// and at 150% text.
    /// </summary>
    internal static class UiGlassBuilder
    {
        public const string BuilderPath = "UI/Glass";
        public const int BuilderOrder = 604;

        // Everything in pixels of the 2x texture.
        private const int Glow = 6;
        private const float Rim = 3f;
        private const float InnerGlow = 20f;
        private const float SliceBand = 48f;

        // Variant C ("Terminal hai màu") dark warm CRT surfaces + amber bezel tokens.
        // High opacity (~0.96) keeps phosphor text comfortably above WCAG AA 4.5:1 even over the brightest daytime dune.
        private static readonly Color GlassTop = Rgba(28, 19, 10, 0.96f);
        private static readonly Color GlassBottom = Rgba(14, 9, 5, 0.97f);
        private static readonly Color RimColor = Rgba(255, 181, 71, 0.86f);
        private static readonly Color CornerAccent = Rgba(255, 214, 128, 0.96f);
        private static readonly Color RimGlow = Rgba(255, 181, 71, 0.32f);
        private static readonly Color Inner = Rgba(255, 181, 71, 0.12f);
        private static readonly Color ScanlineLight = Rgba(255, 194, 92, 0.045f);
        private static readonly Color ScanlineDark = Rgba(6, 4, 2, 0.14f);
        private static readonly Color BubbleLight = Rgba(36, 24, 12, 0.96f);
        private static readonly Color BubbleDark = Rgba(16, 10, 5, 0.97f);
        private static readonly Color DialLight = Rgba(32, 22, 11, 0.96f);
        private static readonly Color DialDark = Rgba(14, 9, 5, 0.97f);

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            GeneratedAssets.EnsureFolder(UiAssetPaths.GlassFolder);
            Write(UiAssetPaths.GlassPanel, Rounded(160, 160, 18f, false, GlassTop, GlassBottom, true));
            Write(UiAssetPaths.GlassPill, Rounded(200, 160, 22f, false, GlassTop, GlassBottom, true));
            Write(UiAssetPaths.GlassBubble, Rounded(176, 176, 24f, true, BubbleLight, BubbleDark, false));
            Write(UiAssetPaths.GlassDial, Rounded(256, 256, 36f, true, DialLight, DialDark, false));
            Debug.Log($"{BuilderPath}: wrote the CRT panel, pill, bubble and dial under {UiAssetPaths.GlassFolder}");
        }

        /// <summary>
        /// A rounded CRT bezel box inset by the glow margin, with subtle horizontal scanlines in the unstretched zones
        /// and brighter corner bracket ticks along the bezel.
        /// </summary>
        private static Texture2D Rounded(
            int width,
            int height,
            float radius,
            bool radial,
            Color light,
            Color dark,
            bool slicedVertically)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var half = new Vector2(width * 0.5f - Glow, height * 0.5f - Glow);
            var centre = new Vector2(width * 0.5f, height * 0.5f);
            var lightPoint = new Vector2(Glow + 0.35f * half.x * 2f, height - Glow - 0.3f * half.y * 2f);
            float reach = Mathf.Max(half.x, half.y) * 2f;
            for (int y = 0; y < height; y++)
            {
                float scanWeight = ScanlineWeight(y, height, slicedVertically);
                bool evenPair = ((y / 2) & 1) == 0;
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = RoundedBox(p - centre, half, radius);
                    Color c = Color.clear;
                    if (d > 0f)
                    {
                        float t = Mathf.Clamp01(1f - d / Glow);
                        c = WithAlpha(RimGlow, RimGlow.a * t * t);
                    }

                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage > 0f)
                    {
                        float blend = radial
                            ? Mathf.Clamp01(Vector2.Distance(p, lightPoint) / reach)
                            : Mathf.Clamp01(Linear(p, width, height));
                        Color fill = Color.Lerp(light, dark, blend);
                        c = Over(c, WithAlpha(fill, fill.a * coverage));

                        if (scanWeight > 0f && d < -Rim)
                        {
                            Color scan = evenPair ? ScanlineLight : ScanlineDark;
                            c = Over(c, WithAlpha(scan, scan.a * scanWeight * coverage));
                        }

                        float inner = Mathf.Exp(Mathf.Min(0f, d) / InnerGlow);
                        c = Over(c, WithAlpha(Inner, Inner.a * inner * coverage));
                    }

                    float rim = Mathf.Clamp01(0.5f - (Mathf.Abs(d + Rim * 0.5f) - Rim * 0.5f));
                    if (rim > 0f)
                    {
                        bool inCornerBracket = (x < 34 || x >= width - 34) && (y < 34 || y >= height - 34);
                        Color rimCol = inCornerBracket ? CornerAccent : RimColor;
                        c = Over(c, WithAlpha(rimCol, rimCol.a * rim));
                    }

                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float ScanlineWeight(int y, int height, bool slicedVertically)
        {
            if (!slicedVertically)
            {
                return 1f;
            }

            float edgeDist = Mathf.Min(y, height - 1 - y);
            return Mathf.Clamp01((SliceBand - edgeDist) / 10f);
        }

        /// <summary>0 at the top-left, 1 at the bottom-right, along the mockup's 160-degree gradient.</summary>
        private static float Linear(Vector2 p, int width, int height)
        {
            var direction = new Vector2(Mathf.Sin(20f * Mathf.Deg2Rad), -Mathf.Cos(20f * Mathf.Deg2Rad));
            var from = new Vector2(0f, height);
            float length = Mathf.Abs(width * direction.x) + Mathf.Abs(height * direction.y);
            return Vector2.Dot(p - from, direction) / length;
        }

        /// <summary>Signed distance to a rounded box of half size <paramref name="half"/> (negative inside).</summary>
        private static float RoundedBox(Vector2 p, Vector2 half, float radius)
        {
            float r = Mathf.Min(radius, Mathf.Min(half.x, half.y));
            var q = new Vector2(Mathf.Abs(p.x) - half.x + r, Mathf.Abs(p.y) - half.y + r);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        /// <summary>Straight-alpha "source over destination".</summary>
        private static Color Over(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0f)
            {
                return Color.clear;
            }

            Color c = (src * src.a + dst * dst.a * (1f - src.a)) / a;
            c.a = a;
            return c;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Rgba(int r, int g, int b, float a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        private static void Write(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
