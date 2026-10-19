using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Builds <see cref="UiAssetPaths.MakerLinework"/> from Art's Kessler–Arai CRT linework
    /// (<see cref="KesslerAraiCrtLinework.DefaultAssetPath"/>): every stroke's points in the 0..1 square, whether it
    /// closes, and the line's width. 07's face traces it as it boots. It runs before UI/Tuning, which points the boot
    /// face's tuning at it.
    /// </summary>
    public static class MakerLineworkBuilder
    {
        [MoonBuilder("UI/MakerLinework", UiTuningBuilder.BuilderOrder - 1)]
        public static void Build()
        {
            KesslerAraiCrtLinework source =
                KesslerAraiCrtLinework.FromJson(File.ReadAllText(KesslerAraiCrtLinework.DefaultAssetPath));
            var points = new List<Vector2>();
            var starts = new int[source.strokes.Count];
            var closed = new bool[source.strokes.Count];
            for (int i = 0; i < source.strokes.Count; i++)
            {
                starts[i] = points.Count;
                closed[i] = source.strokes[i].closed;
                points.AddRange(source.strokes[i].points);
            }

            var linework = ScriptableObject.CreateInstance<MakerLinework>();
            linework.Set(points.ToArray(), starts, closed, source.strokeWidthNormalized);
            string problem = linework.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{KesslerAraiCrtLinework.DefaultAssetPath}: the mark {problem}.");
            }

            GeneratedAssets.CreateOrReplace(linework, UiAssetPaths.MakerLinework);
        }
    }
}
