using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Serializable representation of the Kessler-Arai B mark monoline CRT boot linework
    /// (Assets/_Project/Art/R1/Brands/kessler_arai_crt_linework.json), stored as ordered polylines in
    /// normalized 0..1 screen space so 07's CRT face can draw it stroke by stroke at boot.
    /// </summary>
    [Serializable]
    public sealed class KesslerAraiCrtLinework
    {
        public const string DefaultAssetPath = "Assets/_Project/Art/R1/Brands/kessler_arai_crt_linework.json";
        public const int MaxStrokeBudget = 12;

        public string brand = "kessler_arai";
        public string direction = "B";
        public string name = "Kessler-Arai CRT Boot Monoline Mark";
        public string coordinateSpace = "0..1 normalized (origin top-left, x right, y down)";
        public float strokeWidthNormalized = 0.0085f;
        public int strokeCount;
        public List<Stroke> strokes = new List<Stroke>();

        [Serializable]
        public sealed class Stroke
        {
            public string id;
            public string label;
            public bool closed;
            public List<Vector2> points = new List<Vector2>();
        }

        /// <summary>
        /// Parses the JSON text of kessler_arai_crt_linework.json into a <see cref="KesslerAraiCrtLinework"/> instance.
        /// Supports both <c>[[x, y], ...]</c> coordinate arrays and <c>[{"x": x, "y": y}]</c> objects.
        /// </summary>
        public static KesslerAraiCrtLinework FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("CRT linework JSON cannot be null or empty.", nameof(json));

            var result = JsonUtility.FromJson<KesslerAraiCrtLinework>(json);
            if (result == null)
                throw new InvalidOperationException("Failed to parse CRT linework JSON header.");

            result.strokes = ParseStrokesFromRawJson(json);
            result.strokeCount = result.strokes.Count;
            return result;
        }

        private static List<Stroke> ParseStrokesFromRawJson(string json)
        {
            var list = new List<Stroke>();
            int strokesIdx = json.IndexOf("\"strokes\"", StringComparison.Ordinal);
            if (strokesIdx < 0)
                return list;

            int arrStart = json.IndexOf('[', strokesIdx);
            if (arrStart < 0)
                return list;

            int pos = arrStart + 1;
            while (pos < json.Length)
            {
                int objStart = json.IndexOf('{', pos);
                if (objStart < 0)
                    break;

                int depth = 0;
                int objEnd = -1;
                for (int i = objStart; i < json.Length; i++)
                {
                    char c = json[i];
                    if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            objEnd = i;
                            break;
                        }
                    }
                }

                if (objEnd < 0)
                    break;

                string strokeJson = json.Substring(objStart, objEnd - objStart + 1);
                var strokeHeader = JsonUtility.FromJson<Stroke>(strokeJson);
                if (strokeHeader != null)
                {
                    strokeHeader.points = ParsePointPairs(strokeJson);
                    list.Add(strokeHeader);
                }

                pos = objEnd + 1;
            }

            return list;
        }

        private static List<Vector2> ParsePointPairs(string strokeJson)
        {
            var pts = new List<Vector2>();
            int ptsKey = strokeJson.IndexOf("\"points\"", StringComparison.Ordinal);
            if (ptsKey < 0)
                return pts;

            int outerOpen = strokeJson.IndexOf('[', ptsKey);
            if (outerOpen < 0)
                return pts;

            int i = outerOpen + 1;
            while (i < strokeJson.Length)
            {
                char c = strokeJson[i];
                if (c == ']')
                    break;

                if (c == '[')
                {
                    int close = strokeJson.IndexOf(']', i + 1);
                    if (close < 0)
                        break;

                    string pair = strokeJson.Substring(i + 1, close - i - 1);
                    string[] parts = pair.Split(',');
                    if (parts.Length >= 2 &&
                        float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        pts.Add(new Vector2(x, y));
                    }

                    i = close + 1;
                }
                else
                {
                    i++;
                }
            }

            return pts;
        }
    }
}
