using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Builds <see cref="UiAssetPaths.Wordmark"/> from the approved SVG (<see cref="UiAssetPaths.WordmarkSource"/>,
    /// docs/design/trial-blender-svg.md, v2): the viewBox, the group's round-join stroke width and every path except the
    /// paper rectangle. Colours are not taken from the SVG; the title's USS paints it.
    /// </summary>
    public static class WordmarkBuilder
    {
        private const string SvgNamespace = "http://www.w3.org/2000/svg";

        [MoonBuilder("UI/Wordmark", UiTuningBuilder.BuilderOrder + 2)]
        public static void Build()
        {
            var document = new XmlDocument();
            document.Load(UiAssetPaths.WordmarkSource);
            var shape = ScriptableObject.CreateInstance<WordmarkShape>();
            Fill(shape, document);
            string problem = shape.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{UiAssetPaths.WordmarkSource}: the wordmark {problem}.");
            }

            GeneratedAssets.CreateOrReplace(shape, UiAssetPaths.Wordmark);
        }

        private static void Fill(WordmarkShape shape, XmlDocument document)
        {
            var names = new XmlNamespaceManager(document.NameTable);
            names.AddNamespace("svg", SvgNamespace);
            XmlElement root = document.DocumentElement
                ?? throw new FormatException("The wordmark SVG is empty.");
            string[] box = root.GetAttribute("viewBox").Split(' ');
            if (box.Length != 4)
            {
                throw new FormatException("The wordmark SVG needs a viewBox.");
            }

            var group = (XmlElement)root.SelectSingleNode("svg:g", names)
                ?? throw new FormatException("The wordmark SVG needs one group of paths.");
            var commands = new List<WordmarkCommand>();
            var values = new List<float>();
            var starts = new List<int>();
            var evenOdd = new List<bool>();
            foreach (XmlNode node in group.SelectNodes("svg:path", names))
            {
                var path = (XmlElement)node;
                starts.Add(commands.Count);
                evenOdd.Add(path.GetAttribute("fill-rule") == "evenodd");
                SvgPathParser.Parse(path.GetAttribute("d"), commands, values);
            }

            shape.Set(new Vector2(Number(box[2]), Number(box[3])), Number(group.GetAttribute("stroke-width")),
                commands.ToArray(), values.ToArray(), starts.ToArray(), evenOdd.ToArray());
        }

        private static float Number(string text)
        {
            return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
