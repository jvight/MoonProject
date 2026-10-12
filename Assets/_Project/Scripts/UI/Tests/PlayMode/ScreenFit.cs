using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>One screen shape the UI must lay out for: a name for the capture files and a pixel size.</summary>
    internal readonly struct ScreenShape
    {
        public ScreenShape(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name { get; }

        public int Width { get; }

        public int Height { get; }
    }

    /// <summary>
    /// The screen shapes the UI is proven on (docs/features/M3-24 part A): 16:9, 16:10, 21:9, 32:9 and 4:3, from
    /// 1280x720 to 3840x2160, plus the 2000x837 window the owner's 21:9 screenshot came from.
    /// </summary>
    internal static class ScreenShapes
    {
        public static readonly ScreenShape[] All =
        {
            new ScreenShape("16x9_720p", 1280, 720),
            new ScreenShape("16x9_4k", 3840, 2160),
            new ScreenShape("16x10", 1920, 1200),
            new ScreenShape("21x9", 2560, 1080),
            new ScreenShape("21x9_owner", 2000, 837),
            new ScreenShape("32x9", 3840, 1080),
            new ScreenShape("4x3", 1600, 1200),
        };
    }

    /// <summary>
    /// Reads where the shown UI actually sits on the panel: nothing may leave the screen and the panels that share a
    /// screen may not overlap. Hidden elements (display none, hidden, faded out) are skipped, as are the soft shadows,
    /// which spread past their element by design.
    /// </summary>
    internal static class ScreenFit
    {
        private const float Epsilon = 1.5f;
        private const float MinimumOpacity = 0.05f;
        private const string ShadowClass = "soft-shadow";

        /// <summary>
        /// Runs <paramref name="check"/> on two frames in a row and adds only the problems both frames found: an
        /// element that turns on during a frame has no layout until the next one, and that is not a fit problem.
        /// </summary>
        public static IEnumerator Probe(Action<List<string>> check, List<string> problems)
        {
            var first = new List<string>();
            check(first);
            yield return null;
            var second = new List<string>();
            check(second);
            foreach (string problem in second)
            {
                if (first.Contains(problem))
                {
                    problems.Add(problem);
                }
            }
        }

        public static bool IsShown(VisualElement element)
        {
            for (VisualElement e = element; e != null; e = e.parent)
            {
                IResolvedStyle style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden ||
                    style.opacity < MinimumOpacity)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Adds a problem for every shown element whose box leaves <paramref name="root"/>'s box.</summary>
        public static void CheckOnScreen(VisualElement root, string shot, List<string> problems)
        {
            Rect screen = root.worldBound;
            Visit(root, screen, shot, problems);
        }

        /// <summary>Adds a problem when both elements are shown and their boxes overlap.</summary>
        public static void CheckApart(VisualElement first, VisualElement second, string shot, List<string> problems)
        {
            if (!IsShown(first) || !IsShown(second))
            {
                return;
            }

            Rect a = first.worldBound;
            Rect b = second.worldBound;
            if (a.Overlaps(b))
            {
                problems.Add($"{shot}: '{first.name}' {Describe(a)} overlaps '{second.name}' {Describe(b)}");
            }
        }

        private static void Visit(VisualElement element, Rect screen, string shot, List<string> problems)
        {
            if (!IsShown(element))
            {
                return;
            }

            Rect box = element.worldBound;
            bool measured = box.width > 0f && box.height > 0f && !element.ClassListContains(ShadowClass);
            if (measured && !Contains(screen, box))
            {
                string label = string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name;
                problems.Add($"{shot}: '{label}' {Describe(box)} leaves the screen {Describe(screen)}");
            }

            foreach (VisualElement child in element.Children())
            {
                Visit(child, screen, shot, problems);
            }
        }

        private static bool Contains(Rect screen, Rect box)
        {
            return box.xMin >= screen.xMin - Epsilon && box.yMin >= screen.yMin - Epsilon &&
                   box.xMax <= screen.xMax + Epsilon && box.yMax <= screen.yMax + Epsilon;
        }

        private static string Describe(Rect rect)
        {
            return $"({rect.xMin:0},{rect.yMin:0} to {rect.xMax:0},{rect.yMax:0})";
        }
    }
}
