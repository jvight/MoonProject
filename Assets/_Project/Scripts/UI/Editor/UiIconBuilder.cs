using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Builds <see cref="UiAssetPaths.Icons"/> from the 30 production SVGs in <see cref="UiAssetPaths.IconsFolder"/>
    /// (<c>item_*.svg</c> and <c>kit_*.svg</c>). Every path is parsed at build time via <see cref="SvgPathParser"/>.
    /// </summary>
    public static class UiIconBuilder
    {
        [MoonBuilder("UI/Icons", 12)]
        public static void Build()
        {
            UiIconShape[] shapes = ParseAllIcons();
            var set = ScriptableObject.CreateInstance<UiIconSet>();
            set.Set(shapes);
            string problem = set.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"UI/Icons: {problem}.");
            }

            GeneratedAssets.CreateOrReplace(set, UiAssetPaths.Icons);
        }

        /// <summary>Parses all 30 SVG files in <see cref="UiAssetPaths.IconsFolder"/> in deterministic sorted order.</summary>
        public static UiIconShape[] ParseAllIcons()
        {
            if (!Directory.Exists(UiAssetPaths.IconsFolder))
            {
                throw new InvalidOperationException($"UI/Icons: folder '{UiAssetPaths.IconsFolder}' does not exist.");
            }

            string[] files = Directory.GetFiles(UiAssetPaths.IconsFolder, "*.svg", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);
            if (files.Length != UiIconSet.ExpectedIconCount)
            {
                throw new InvalidOperationException(
                    $"UI/Icons: expected {UiIconSet.ExpectedIconCount} SVGs in '{UiAssetPaths.IconsFolder}', found {files.Length}.");
            }

            var shapes = new UiIconShape[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                shapes[i] = ParseSvgFile(files[i]);
            }

            return shapes;
        }

        private static UiIconShape ParseSvgFile(string path)
        {
            string id = Path.GetFileNameWithoutExtension(path);
            XDocument document = XDocument.Load(path);
            XElement svg = document.Root ?? throw new InvalidOperationException($"{path} has no root <svg>.");
            string viewBox = (string)svg.Attribute("viewBox");
            if (!string.Equals(viewBox, "0 0 64 64", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{path} must have viewBox='0 0 64 64', found '{viewBox}'.");
            }

            var fillCommands = new List<WordmarkCommand>();
            var fillValues = new List<float>();
            float fillStroke = 1.5f;

            var primaryCommands = new List<WordmarkCommand>();
            var primaryValues = new List<float>();
            float primaryStroke = 2.2f;

            var detailCommands = new List<WordmarkCommand>();
            var detailValues = new List<float>();
            float detailStroke = 1.5f;

            foreach (XElement pathElement in svg.Descendants())
            {
                if (!string.Equals(pathElement.Name.LocalName, "path", StringComparison.Ordinal))
                {
                    continue;
                }

                string d = (string)pathElement.Attribute("d");
                if (string.IsNullOrWhiteSpace(d))
                {
                    continue;
                }

                string fillAttr = (string)pathElement.Attribute("fill") ?? "none";
                string strokeWidthAttr = (string)pathElement.Attribute("stroke-width") ?? "2.2";
                float strokeWidth = float.Parse(strokeWidthAttr, CultureInfo.InvariantCulture);

                if (string.Equals(fillAttr, "currentColor", StringComparison.Ordinal))
                {
                    SvgPathParser.Parse(d, fillCommands, fillValues);
                    fillStroke = strokeWidth;
                }
                else if (strokeWidth >= 2.0f)
                {
                    SvgPathParser.Parse(d, primaryCommands, primaryValues);
                    primaryStroke = strokeWidth;
                }
                else
                {
                    SvgPathParser.Parse(d, detailCommands, detailValues);
                    detailStroke = strokeWidth;
                }
            }

            var fillPath = new UiIconPath(fillCommands.ToArray(), fillValues.ToArray(), fillStroke, true);
            var primaryPath = new UiIconPath(primaryCommands.ToArray(), primaryValues.ToArray(), primaryStroke, false);
            var detailPath = new UiIconPath(detailCommands.ToArray(), detailValues.ToArray(), detailStroke, false);
            return new UiIconShape(id, fillPath, primaryPath, detailPath);
        }
    }
}
