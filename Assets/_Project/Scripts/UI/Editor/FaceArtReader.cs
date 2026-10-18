using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Reads the face's art from <see cref="UiAssetPaths.FaceArtFolder"/> for <see cref="RoverDiagramBuilder"/>: 07's
    /// three-quarter line drawing and where its lines sit in the image, the leader end point of each device from
    /// <c>rover_07_anchors.json</c>, and the face icons (<see cref="RoverDiagramData.FaceIconIds"/>) parsed from their
    /// 256-unit SVGs into the 64-unit <see cref="UiIconShape"/> the painter draws. Anything missing or malformed throws.
    /// </summary>
    internal static class FaceArtReader
    {
        private const string SourceViewBox = "0 0 256 256";
        private const float SourceViewSize = 256f;

        // Argon's icons stroke their outlines at 5.2 units and their details at 3.8 (of 256): the thicker are primary.
        private const float PrimaryStrokeMin = 4.5f;

        // A pixel whose alpha is above this belongs to the drawing's lines (the rest of the image is clear).
        private const byte ContentAlpha = 8;

        private const float FullCircle = 360f;

        // An arc's values are its centre x, y and radius (lengths, which scale), then two angles and a flag.
        private const int ArcLengthValues = 3;

        /// <summary>The line drawing (<see cref="UiAssetPaths.FaceArt"/>), imported by Unity.</summary>
        public static Texture2D LoadArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<Texture2D>(UiAssetPaths.FaceArt);
            if (art == null)
            {
                throw new InvalidOperationException($"Face art: no texture at '{UiAssetPaths.FaceArt}'.");
            }

            return art;
        }

        /// <summary>Where the drawing's lines sit in its image, as a UV rect with a top-left origin.</summary>
        public static Rect ReadContent()
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!image.LoadImage(File.ReadAllBytes(UiAssetPaths.FaceArt)))
                {
                    throw new InvalidOperationException($"Face art: '{UiAssetPaths.FaceArt}' is not a readable PNG.");
                }

                return ContentOf(image.GetPixels32(), image.width, image.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        /// <summary>The leader end point of every device (<see cref="FaceDevices"/> order) on the drawing, as UV.</summary>
        public static Vector2[] ReadAnchors()
        {
            if (!File.Exists(UiAssetPaths.FaceAnchors))
            {
                throw new InvalidOperationException($"Face art: no anchors at '{UiAssetPaths.FaceAnchors}'.");
            }

            AnchorSet set = JsonUtility.FromJson<AnchorFile>(File.ReadAllText(UiAssetPaths.FaceAnchors)).rover_07_line_3q;
            var anchors = new Vector2[FaceDevices.Count];
            anchors[FaceDevices.Scan] = Require(set.lens, "lens");
            anchors[FaceDevices.Cutter] = Require(set.cutter_nozzle, "cutter_nozzle");
            anchors[FaceDevices.SocketOne] = Require(set.socket_1, "socket_1");
            anchors[FaceDevices.SocketTwo] = Require(set.socket_2, "socket_2");
            return anchors;
        }

        /// <summary>Parses every icon of <see cref="RoverDiagramData.FaceIconIds"/>, in that order.</summary>
        public static UiIconShape[] ParseIcons()
        {
            var icons = new UiIconShape[RoverDiagramData.FaceIconIds.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = ParseIcon(RoverDiagramData.FaceIconIds[i]);
            }

            return icons;
        }

        /// <summary>The bounds of the pixels drawn in a bottom-up pixel array, as a UV rect with a top-left origin.</summary>
        internal static Rect ContentOf(Color32[] pixels, int width, int height)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= ContentAlpha)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < 0)
            {
                throw new InvalidOperationException("Face art: the drawing is empty.");
            }

            float top = height - 1 - maxY;
            return new Rect(minX / (float)width, top / height, (maxX - minX + 1f) / width, (maxY - minY + 1f) / height);
        }

        private static Vector2 Require(Anchor anchor, string name)
        {
            if (anchor.u <= 0f && anchor.v <= 0f)
            {
                throw new InvalidOperationException($"Face art: '{UiAssetPaths.FaceAnchors}' has no '{name}' anchor.");
            }

            return new Vector2(anchor.u, anchor.v);
        }

        private static UiIconShape ParseIcon(string id)
        {
            string path = UiAssetPaths.FaceArtFolder + "/" + id + ".svg";
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Face art: no icon at '{path}'.");
            }

            XElement svg = XDocument.Load(path).Root ?? throw new InvalidOperationException($"{path} has no root <svg>.");
            string viewBox = (string)svg.Attribute("viewBox");
            if (!string.Equals(viewBox, SourceViewBox, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{path} must have viewBox='{SourceViewBox}', found '{viewBox}'.");
            }

            var primary = new IconLayer();
            var detail = new IconLayer();
            foreach (XElement element in svg.Descendants())
            {
                string kind = element.Name.LocalName;
                if (kind != "path" && kind != "circle")
                {
                    continue;
                }

                string fill = (string)element.Attribute("fill") ?? "none";
                if (!string.Equals(fill, "none", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{path}: only stroked shapes are drawn, found fill '{fill}'.");
                }

                float stroke = Number(element, "stroke-width", path);
                IconLayer layer = stroke >= PrimaryStrokeMin ? primary : detail;
                layer.Stroke = stroke;
                if (kind == "path")
                {
                    SvgPathParser.Parse((string)element.Attribute("d"), layer.Commands, layer.Values);
                }
                else
                {
                    AddCircle(layer, Number(element, "cx", path), Number(element, "cy", path),
                        Number(element, "r", path));
                }
            }

            float scale = UiIconShape.ViewBoxSize / SourceViewSize;
            var empty = new UiIconPath(Array.Empty<WordmarkCommand>(), Array.Empty<float>(), 0f, false);
            return new UiIconShape(id, empty, primary.ToPath(scale), detail.ToPath(scale));
        }

        private static void AddCircle(IconLayer layer, float cx, float cy, float r)
        {
            layer.Commands.Add(WordmarkCommand.MoveTo);
            layer.Values.Add(cx + r);
            layer.Values.Add(cy);
            layer.Commands.Add(WordmarkCommand.ArcTo);
            layer.Values.Add(cx);
            layer.Values.Add(cy);
            layer.Values.Add(r);
            layer.Values.Add(0f);
            layer.Values.Add(FullCircle);
            layer.Values.Add(1f);
        }

        private static float Number(XElement element, string attribute, string path)
        {
            string text = (string)element.Attribute(attribute);
            if (text == null || !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw new InvalidOperationException($"{path}: <{element.Name.LocalName}> has no number '{attribute}'.");
            }

            return value;
        }

        /// <summary>One stroke weight of an icon while it is parsed, in the source's 256 units.</summary>
        private sealed class IconLayer
        {
            public readonly List<WordmarkCommand> Commands = new List<WordmarkCommand>();
            public readonly List<float> Values = new List<float>();
            public float Stroke;

            /// <summary>The layer scaled into the painter's units: points and radii scale, angles and flags do not.</summary>
            public UiIconPath ToPath(float scale)
            {
                var values = new float[Values.Count];
                int v = 0;
                for (int i = 0; i < Commands.Count; i++)
                {
                    int count = WordmarkShape.ValueCount(Commands[i]);
                    int scaled = Commands[i] == WordmarkCommand.ArcTo ? ArcLengthValues : count;
                    for (int k = 0; k < count; k++)
                    {
                        values[v + k] = k < scaled ? Values[v + k] * scale : Values[v + k];
                    }

                    v += count;
                }

                return new UiIconPath(Commands.ToArray(), values, Stroke * scale, false);
            }
        }

        // JsonUtility DTOs: the field names are the JSON keys of rover_07_anchors.json. JsonUtility fills a missing
        // object with defaults, so an absent anchor reads as (0, 0).
        [Serializable]
        private sealed class AnchorFile
        {
            public AnchorSet rover_07_line_3q;
        }

        [Serializable]
        private sealed class AnchorSet
        {
            public Anchor lens;
            public Anchor cutter_nozzle;
            public Anchor socket_1;
            public Anchor socket_2;
        }

        [Serializable]
        private sealed class Anchor
        {
            public float u;
            public float v;
        }
    }
}
