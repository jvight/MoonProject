using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Renders the "Moonlight Glass" surfaces (the owner's pick, Subnautica's grammar in moonlight) as textures, since
    /// UI Toolkit has no gradients or box shadows: a rounded blue glass gradient with a thin bright rim, a faint inner
    /// glow and a soft outer glow. Drawn at twice the size the USS shows them (<c>-unity-slice-scale: 0.5</c>), so the
    /// corners stay crisp at 4K and at 150% text. The panel and pill are 9-sliced; the bubble and dial are round.
    /// </summary>
    internal static class UiGlassBuilder
    {
        public const string BuilderPath = "UI/Glass";
        public const int BuilderOrder = 604;

        // Everything in pixels of the 2x texture.
        private const int Glow = 12;
        private const float Rim = 3f;
        private const float InnerGlow = 26f;

        // Moonlight Glass tokens (Logs/ui-ref/moonlight-glass-mockup.html, .v1).
        // Deeper and nearly opaque, so white text keeps 7:1 even over the brightest dune (the legibility capture).
        private static readonly Color GlassTop = Rgba(26, 46, 118, 0.93f);
        private static readonly Color GlassBottom = Rgba(12, 21, 62, 0.95f);
        private static readonly Color RimColor = Rgba(150, 196, 255, 0.85f);
        private static readonly Color RimGlow = Rgba(110, 170, 255, 0.45f);
        private static readonly Color Inner = Rgba(140, 190, 255, 0.18f);
        private static readonly Color BubbleLight = Rgba(58, 92, 180, 0.94f);
        private static readonly Color BubbleDark = Rgba(22, 38, 98, 0.94f);
        private static readonly Color DialLight = Rgba(44, 72, 156, 0.94f);
        private static readonly Color DialDark = Rgba(14, 24, 66, 0.95f);

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            GeneratedAssets.EnsureFolder(UiAssetPaths.GlassFolder);
            Write(UiAssetPaths.GlassPanel, Rounded(160, 160, 44f, false, GlassTop, GlassBottom));
            Write(UiAssetPaths.GlassPill, Rounded(200, 160, 60f, false, GlassTop, GlassBottom));
            Write(UiAssetPaths.GlassBubble, Rounded(176, 176, 68f, true, BubbleLight, BubbleDark));
            Write(UiAssetPaths.GlassDial, Rounded(256, 256, 108f, true, DialLight, DialDark));
            Debug.Log($"{BuilderPath}: wrote the glass panel, pill, bubble and dial under {UiAssetPaths.GlassFolder}");
        }

        /// <summary>
        /// A rounded box (a circle when the radius is half the inner size) inset by the glow margin. Linear fills run
        /// from top-left light to bottom-right dark (the mockup's 160 degrees); radial fills light from 35% / 30%.
        /// </summary>
        private static Texture2D Rounded(int width, int height, float radius, bool radial, Color light, Color dark)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var half = new Vector2(width * 0.5f - Glow, height * 0.5f - Glow);
            var centre = new Vector2(width * 0.5f, height * 0.5f);
            var lightPoint = new Vector2(Glow + 0.35f * half.x * 2f, height - Glow - 0.3f * half.y * 2f);
            float reach = Mathf.Max(half.x, half.y) * 2f;
            for (int y = 0; y < height; y++)
            {
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
                        float inner = Mathf.Exp(Mathf.Min(0f, d) / InnerGlow);
                        c = Over(c, WithAlpha(Inner, Inner.a * inner * coverage));
                    }

                    float rim = Mathf.Clamp01(0.5f - (Mathf.Abs(d + Rim * 0.5f) - Rim * 0.5f));
                    c = Over(c, WithAlpha(RimColor, RimColor.a * rim));
                    pixels[y * width + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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
