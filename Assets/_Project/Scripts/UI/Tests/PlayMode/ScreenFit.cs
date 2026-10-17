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
            new ScreenShape("16x9_1080p", 1920, 1080),
            new ScreenShape("16x10", 1920, 1200),
            new ScreenShape("21x9", 2560, 1080),
            new ScreenShape("32x9", 3840, 1080),
            new ScreenShape("4x3", 1600, 1200),
            new ScreenShape("5x4", 1280, 1024),
            new ScreenShape("16x9_720p", 1280, 720),
            new ScreenShape("1_2x1", 1578, 1299),
            new ScreenShape("16x9_4k", 3840, 2160),
            new ScreenShape("21x9_owner", 2000, 837),
        };

        public static readonly ScreenShape[] TitleSheet =
        {
            new ScreenShape("16x9_1080p", 1920, 1080),
            new ScreenShape("16x10", 1920, 1200),
            new ScreenShape("21x9", 2560, 1080),
            new ScreenShape("32x9", 3840, 1080),
            new ScreenShape("4x3", 1600, 1200),
            new ScreenShape("5x4", 1280, 1024),
            new ScreenShape("16x9_720p", 1280, 720),
            new ScreenShape("1_2x1", 1578, 1299),
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
        private const float MinSettingRowFontSize = 20f;
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
            Visit(root, screen, shot, "the screen", problems);
        }

        /// <summary>
        /// Adds a problem for every shown title element whose box leaves the CRT bezel's inner screen area.
        /// </summary>
        public static void CheckInsideCrtFrame(UiLayout layout, TitleSettings title, string shot, List<string> problems)
        {
            Rect screen = layout.Root.worldBound;
            if (screen.width <= 0f || screen.height <= 0f)
            {
                return;
            }

            Vector2 refSize = title.StandaloneReferenceSize;
            Vector2 bezelInset = title.CrtBezelInset;
            float scale = Mathf.Min(screen.width / refSize.x, screen.height / refSize.y);
            float insetX = bezelInset.x * scale;
            float insetY = bezelInset.y * scale;
            Rect crtFrame = new Rect(
                screen.xMin + insetX,
                screen.yMin + insetY,
                screen.width - insetX * 2f,
                screen.height - insetY * 2f);

            Visit(layout.TitleScreen, crtFrame, shot, "the CRT frame", problems);
            Visit(layout.PauseMain, crtFrame, shot, "the CRT frame", problems);
            Visit(layout.PauseSettings, crtFrame, shot, "the CRT frame", problems);
            Visit(layout.TitleKeyHint, crtFrame, shot, "the CRT frame", problems);
        }

        /// <summary>
        /// Asserts that no shown left-column element (<c>#title-screen</c>, <c>#pause-main</c>, <c>#pause-settings</c>,
        /// <c>#title-key-hint</c>) intersects the painted Moon disc, rover silhouette, or rim landmarks.
        /// </summary>
        public static void CheckTitleArtApart(UiLayout layout, PauseMenu pause, string shot, List<string> problems)
        {
            var landmarks = new List<Rect>(8);
            pause.GetTitleArtBounds(out Rect moonDisc, out Rect rover, landmarks);

            VisualElement[] leftColumn =
            {
                layout.TitleScreen,
                layout.PauseMain,
                layout.PauseSettings,
                layout.TitleKeyHint,
            };

            for (int i = 0; i < leftColumn.Length; i++)
            {
                VisualElement element = leftColumn[i];
                if (element == null || !IsShown(element))
                {
                    continue;
                }

                Rect box = element.worldBound;
                if (box.width <= 0f || box.height <= 0f)
                {
                    continue;
                }

                if (moonDisc.width > 0f && box.Overlaps(moonDisc))
                {
                    problems.Add($"{shot}: '{element.name}' {Describe(box)} overlaps Moon disc {Describe(moonDisc)}");
                }

                if (rover.width > 0f && box.Overlaps(rover))
                {
                    problems.Add($"{shot}: '{element.name}' {Describe(box)} overlaps rover silhouette {Describe(rover)}");
                }

                for (int lm = 0; lm < landmarks.Count; lm++)
                {
                    Rect landmark = landmarks[lm];
                    if (landmark.width > 0f && box.Overlaps(landmark))
                    {
                        problems.Add(
                            $"{shot}: '{element.name}' {Describe(box)} overlaps landmark[{lm}] {Describe(landmark)}");
                    }
                }
            }
        }

        /// <summary>
        /// Checks that every settings tab fits on a single line without wrapping or clipping, and that every shown
        /// setting-row label reads at the menu size class.
        /// </summary>
        public static void CheckSettingsLegibility(UiLayout layout, string shot, List<string> problems)
        {
            if (!IsShown(layout.PauseSettings))
            {
                return;
            }

            Button[] tabs = { layout.TabSound, layout.TabCamera, layout.TabDisplay };
            for (int i = 0; i < tabs.Length; i++)
            {
                Button tab = tabs[i];
                if (!IsShown(tab))
                {
                    continue;
                }

                Vector2 singleLine = tab.MeasureTextSize(
                    tab.text,
                    0f,
                    VisualElement.MeasureMode.Undefined,
                    0f,
                    VisualElement.MeasureMode.Undefined);
                float availableWidth = tab.contentRect.width;
                if (singleLine.x > availableWidth + Epsilon)
                {
                    problems.Add(
                        $"{shot}: tab '{tab.name}' (\"{tab.text}\") requires {singleLine.x:0.0}px for one line but content width is {availableWidth:0.0}px");
                }
            }

            foreach (Label label in layout.PauseSettings.Query<Label>(className: "setting-row__label").Build())
            {
                if (!IsShown(label))
                {
                    continue;
                }

                float fontSize = label.resolvedStyle.fontSize;
                if (fontSize < MinSettingRowFontSize)
                {
                    problems.Add(
                        $"{shot}: setting row label \"{label.text}\" font-size is {fontSize:0.0}px (< {MinSettingRowFontSize:0.0}px menu size class)");
                }
            }
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

        /// <summary>Checks that shown direct children of <paramref name="container"/> do not overlap one another.</summary>
        public static void CheckChildrenApart(VisualElement container, string shot, List<string> problems)
        {
            if (container == null || !IsShown(container))
            {
                return;
            }

            var shown = new List<VisualElement>();
            foreach (VisualElement child in container.Children())
            {
                if (IsShown(child) && !child.ClassListContains(ShadowClass) &&
                    child.worldBound.width > 0f && child.worldBound.height > 0f)
                {
                    shown.Add(child);
                }
            }

            for (int i = 0; i < shown.Count; i++)
            {
                for (int j = i + 1; j < shown.Count; j++)
                {
                    CheckApart(shown[i], shown[j], shot, problems);
                }
            }
        }

        private static void Visit(VisualElement element, Rect bounds, string shot, string boundsName,
            List<string> problems)
        {
            if (element == null || !IsShown(element))
            {
                return;
            }

            Rect box = element.worldBound;
            bool measured = box.width > 0f && box.height > 0f && !element.ClassListContains(ShadowClass);
            if (measured && !Contains(bounds, box))
            {
                string label = string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name;
                problems.Add($"{shot}: '{label}' {Describe(box)} leaves {boundsName} {Describe(bounds)}");
            }

            foreach (VisualElement child in element.Children())
            {
                Visit(child, bounds, shot, boundsName, problems);
            }
        }

        private static bool Contains(Rect bounds, Rect box)
        {
            return box.xMin >= bounds.xMin - Epsilon && box.yMin >= bounds.yMin - Epsilon &&
                   box.xMax <= bounds.xMax + Epsilon && box.yMax <= bounds.yMax + Epsilon;
        }

        private static string Describe(Rect rect)
        {
            return $"({rect.xMin:0},{rect.yMin:0} to {rect.xMax:0},{rect.yMax:0})";
        }
    }
}
