using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Gameplay;
using Object = UnityEngine.Object;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Rasterizes the 10 CRT line-art structure SVG icons (<c>Assets/_Project/UI/Icons/struct_&lt;id&gt;.svg</c>)
    /// via <see cref="SvgPathParser"/> into 256x256 white-on-transparent textures at
    /// <c>Assets/_Project/Generated/UI/StructureIcons/&lt;id&gt;.png</c> so the build strip can tint them in
    /// Variant C amber and cyan.
    /// </summary>
    internal static class StructureIconBuilder
    {
        public const string BuilderPath = "UI/StructureIcons";
        public const int BuilderOrder = 635;

        private const int IconSize = 256;
        private const float ViewBoxSize = 64f;
        private const string SvgNamespace = "http://www.w3.org/2000/svg";
        private const string SvgIconFolder = "Assets/_Project/UI/Icons/Structures";

        [MoonBuilder(BuilderPath, BuilderOrder)]
        internal static void Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StructureCatalog>(UiAssetPaths.StructureCatalog);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: {UiAssetPaths.StructureCatalog} is missing (run Gameplay/Content).");
            }

            string problem = catalog.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: {UiAssetPaths.StructureCatalog} is invalid: {problem}.");
            }

            GeneratedAssets.EnsureFolder(UiAssetPaths.StructureIconFolder);

            int renderedCount = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                StructureDefinition definition = catalog.StructureAt(i);
                if (definition == null || definition.ArtPending)
                {
                    continue;
                }

                string svgPath = $"{SvgIconFolder}/struct_{definition.Id}.svg";
                if (!File.Exists(svgPath))
                {
                    throw new FileNotFoundException(
                        $"{BuilderPath}: missing structure SVG icon at {svgPath}.",
                        svgPath);
                }

                Texture2D icon = RasterizeSvgIcon(svgPath);
                string iconPath = UiAssetPaths.StructureIcon(definition.Id);
                WriteIcon(iconPath, icon);
                renderedCount++;
            }

            Debug.Log($"{BuilderPath}: wrote {renderedCount} structure icon(s) under {UiAssetPaths.StructureIconFolder}");
        }

        internal static Texture2D RasterizeSvgIcon(string svgPath)
        {
            var doc = new XmlDocument();
            doc.Load(svgPath);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("svg", SvgNamespace);

            XmlElement root = doc.DocumentElement
                ?? throw new FormatException($"{svgPath}: empty SVG document.");
            XmlNodeList pathNodes = root.SelectNodes(".//svg:path", ns);
            if (pathNodes == null || pathNodes.Count == 0)
            {
                throw new FormatException($"{svgPath}: SVG contains no <path> elements.");
            }

            float scale = IconSize / ViewBoxSize;
            var alpha = new float[IconSize * IconSize];
            var commands = new List<WordmarkCommand>(64);
            var values = new List<float>(256);
            var subpaths = new List<List<Vector2>>(16);

            foreach (XmlNode node in pathNodes)
            {
                if (!(node is XmlElement pathEl))
                {
                    continue;
                }

                string d = pathEl.GetAttribute("d");
                if (string.IsNullOrWhiteSpace(d))
                {
                    continue;
                }

                float strokeWidth = ParseFloat(pathEl.GetAttribute("stroke-width"), 2.2f);
                bool isFill = string.Equals(pathEl.GetAttribute("fill"), "currentColor", StringComparison.Ordinal);

                commands.Clear();
                values.Clear();
                SvgPathParser.Parse(d, commands, values);

                SampleSubpaths(commands, values, scale, subpaths);
                float halfStroke = 0.5f * strokeWidth * scale;

                for (int s = 0; s < subpaths.Count; s++)
                {
                    List<Vector2> poly = subpaths[s];
                    if (poly.Count < 2)
                    {
                        continue;
                    }

                    RasterizeSubpath(poly, halfStroke, isFill, alpha);
                }
            }

            var pixels = new Color32[IconSize * IconSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha[i] * 255f), 0, 255);
                pixels[i] = new Color32(255, 255, 255, a);
            }

            var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void SampleSubpaths(
            List<WordmarkCommand> commands,
            List<float> values,
            float scale,
            List<List<Vector2>> subpaths)
        {
            subpaths.Clear();
            List<Vector2> currentPoly = null;
            Vector2 cur = Vector2.zero;
            Vector2 start = Vector2.zero;
            int idx = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                switch (commands[i])
                {
                    case WordmarkCommand.MoveTo:
                        if (currentPoly != null && currentPoly.Count >= 2)
                        {
                            subpaths.Add(currentPoly);
                        }

                        cur = start = ToPixel(values[idx], values[idx + 1], scale);
                        idx += 2;
                        currentPoly = new List<Vector2>(32) { cur };
                        break;

                    case WordmarkCommand.CubicTo:
                        Vector2 p1 = ToPixel(values[idx], values[idx + 1], scale);
                        Vector2 p2 = ToPixel(values[idx + 2], values[idx + 3], scale);
                        Vector2 p3 = ToPixel(values[idx + 4], values[idx + 5], scale);
                        idx += 6;
                        if (currentPoly == null)
                        {
                            currentPoly = new List<Vector2>(32) { cur };
                        }

                        if (Vector2.Distance(cur, p1) < 1e-2f && Vector2.Distance(p2, p3) < 1e-2f)
                        {
                            currentPoly.Add(p3);
                        }
                        else
                        {
                            const int steps = 16;
                            for (int step = 1; step <= steps; step++)
                            {
                                float t = step / (float)steps;
                                float mt = 1f - t;
                                Vector2 pt = mt * mt * mt * cur +
                                             3f * mt * mt * t * p1 +
                                             3f * mt * t * t * p2 +
                                             t * t * t * p3;
                                currentPoly.Add(pt);
                            }
                        }

                        cur = p3;
                        break;

                    case WordmarkCommand.ArcTo:
                        float cx = values[idx];
                        float cy = values[idx + 1];
                        float r = values[idx + 2];
                        float a0 = values[idx + 3];
                        float a1 = values[idx + 4];
                        idx += 6;
                        if (currentPoly == null)
                        {
                            currentPoly = new List<Vector2>(32) { cur };
                        }

                        int arcSteps = Mathf.Max(16, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 5f));
                        for (int step = 1; step <= arcSteps; step++)
                        {
                            float deg = Mathf.Lerp(a0, a1, step / (float)arcSteps);
                            float rad = deg * Mathf.Deg2Rad;
                            Vector2 pt = ToPixel(cx + r * Mathf.Cos(rad), cy + r * Mathf.Sin(rad), scale);
                            currentPoly.Add(pt);
                            cur = pt;
                        }

                        break;

                    case WordmarkCommand.Close:
                        if (currentPoly != null && currentPoly.Count > 0)
                        {
                            if (Vector2.Distance(currentPoly[currentPoly.Count - 1], start) > 1e-2f)
                            {
                                currentPoly.Add(start);
                            }

                            cur = start;
                        }

                        break;
                }
            }

            if (currentPoly != null && currentPoly.Count >= 2)
            {
                subpaths.Add(currentPoly);
            }
        }

        private static Vector2 ToPixel(float svgX, float svgY, float scale)
        {
            return new Vector2(svgX * scale, (ViewBoxSize - svgY) * scale);
        }

        private static void RasterizeSubpath(
            List<Vector2> poly,
            float halfStroke,
            bool isFill,
            float[] alpha)
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 v = poly[i];
                if (v.x < minX) minX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.x > maxX) maxX = v.x;
                if (v.y > maxY) maxY = v.y;
            }

            float pad = halfStroke + 1.5f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(minX - pad), 0, IconSize - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(minY - pad), 0, IconSize - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX + pad), 0, IconSize - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY + pad), 0, IconSize - 1);

            for (int py = y0; py <= y1; py++)
            {
                int rowOffset = py * IconSize;
                for (int px = x0; px <= x1; px++)
                {
                    var sample = new Vector2(px + 0.5f, py + 0.5f);
                    float bestDistSq = float.PositiveInfinity;
                    for (int i = 0; i < poly.Count - 1; i++)
                    {
                        float dSq = DistanceToSegmentSquared(sample, poly[i], poly[i + 1]);
                        if (dSq < bestDistSq)
                        {
                            bestDistSq = dSq;
                        }
                    }

                    float dist = Mathf.Sqrt(bestDistSq);
                    float a = Mathf.Clamp01(halfStroke + 0.5f - dist);
                    if (isFill && a < 1f && poly.Count >= 3 && PointInPolygon(sample, poly))
                    {
                        a = 1f;
                    }

                    if (a > alpha[rowOffset + px])
                    {
                        alpha[rowOffset + px] = a;
                    }
                }
            }
        }

        private static float DistanceToSegmentSquared(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq <= 1e-6f)
            {
                return (p - a).sqrMagnitude;
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 proj = a + t * ab;
            return (p - proj).sqrMagnitude;
        }

        private static bool PointInPolygon(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 pi = poly[i];
                Vector2 pj = poly[j];
                if (((pi.y > p.y) != (pj.y > p.y)) &&
                    (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float ParseFloat(string text, float fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        private static void WriteIcon(string path, Texture2D texture)
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
