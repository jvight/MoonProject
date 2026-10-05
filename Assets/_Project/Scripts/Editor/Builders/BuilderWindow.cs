using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// <c>MoonProject/Builders</c>: every registered <see cref="MoonBuilderAttribute"/> builder grouped by its first
    /// path segment, with a Run button, its order and declaring method, and the result and duration of its last run
    /// from this window. Also owns the <c>MoonProject/Build/Build All</c> menu item.
    /// </summary>
    public sealed class BuilderWindow : EditorWindow
    {
        public const string BuildMenuRoot = "MoonProject/Build/";

        private const string UngroupedName = "Project";
        private static readonly Color FailedColor = new Color(1f, 0.45f, 0.45f);
        private static readonly Color SucceededColor = new Color(0.55f, 0.9f, 0.6f);

        private readonly Dictionary<string, BuilderResult> _lastResults =
            new Dictionary<string, BuilderResult>(StringComparer.OrdinalIgnoreCase);

        [MenuItem("MoonProject/Builders", priority = 0)]
        private static void Open()
        {
            GetWindow<BuilderWindow>("Moon Builders").Show();
        }

        [MenuItem(BuildMenuRoot + "Build All", priority = 0)]
        private static void BuildAll()
        {
            RunFromMenu(string.Empty);
        }

        /// <summary>Menu entry point: asks to save modified scenes if needed, then runs the matching builders.</summary>
        public static void RunFromMenu(string pattern)
        {
            IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Filter(BuilderRegistry.Discover(out _), pattern);
            if (BuilderRegistry.ConfirmInteractiveRun(builders))
            {
                BuilderRegistry.DiscoverAndRun(pattern);
            }
        }

        private void CreateGUI()
        {
            Rebuild();
        }

        private void OnFocus()
        {
            if (rootVisualElement.childCount > 0)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Discover(out IReadOnlyList<string> problems);

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            toolbar.Add(new Button(() => Run(builders)) { text = $"Build All ({builders.Count})" });
            toolbar.Add(new Button(Rebuild) { text = "Refresh" });
            root.Add(toolbar);

            foreach (string problem in problems)
            {
                root.Add(new HelpBox(problem, HelpBoxMessageType.Error));
            }

            if (builders.Count == 0)
            {
                root.Add(new HelpBox("No [MoonBuilder] methods found in the loaded editor assemblies.",
                    HelpBoxMessageType.Info));
                return;
            }

            var scroll = new ScrollView();
            root.Add(scroll);
            Foldout group = null;
            string groupName = null;
            foreach (BuilderInfo builder in builders)
            {
                string name = GroupOf(builder.Path);
                if (group == null || name != groupName)
                {
                    group = FindOrAddGroup(scroll, name);
                    groupName = name;
                }

                group.Add(Row(builder));
            }
        }

        private static Foldout FindOrAddGroup(VisualElement scroll, string name)
        {
            foreach (VisualElement child in scroll.Children())
            {
                if (child is Foldout existing && existing.text == name)
                {
                    return existing;
                }
            }

            var group = new Foldout { text = name, value = true };
            scroll.Add(group);
            return group;
        }

        private VisualElement Row(BuilderInfo builder)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var run = new Button(() => Run(new[] { builder })) { text = "Run", style = { width = 48 } };
            row.Add(run);
            row.Add(new Label(builder.Path)
            {
                tooltip = $"{builder.MethodName} (order {builder.Order})",
                style = { width = 220, unityFontStyleAndWeight = FontStyle.Bold },
            });
            row.Add(new Label($"#{builder.Order}") { style = { width = 50 } });

            var status = new Label { style = { flexGrow = 1 } };
            if (_lastResults.TryGetValue(builder.Path, out BuilderResult result))
            {
                status.text = result.Succeeded
                    ? $"OK  {result.Seconds:0.00}s"
                    : $"FAILED  {result.Seconds:0.00}s  {result.ErrorCount} error(s): {FirstLine(result.FirstError)}";
                status.tooltip = result.FirstError;
                status.style.color = result.Succeeded ? SucceededColor : FailedColor;
            }
            else
            {
                status.text = "not run in this session";
            }

            row.Add(status);
            return row;
        }

        private void Run(IReadOnlyList<BuilderInfo> builders)
        {
            if (builders.Count == 0)
            {
                return;
            }

            var pattern = new List<string>(builders.Count);
            foreach (BuilderInfo builder in builders)
            {
                pattern.Add(Regex.Escape(builder.Path));
            }

            IReadOnlyList<BuilderInfo> current = BuilderRegistry.Filter(
                BuilderRegistry.Discover(out IReadOnlyList<string> problems), $"^({string.Join("|", pattern)})$");
            foreach (string problem in problems)
            {
                Debug.LogError(problem);
            }

            if (!BuilderRegistry.ConfirmInteractiveRun(current))
            {
                return;
            }

            foreach (BuilderResult result in BuilderRegistry.Run(current))
            {
                _lastResults[result.Path] = result;
            }

            Rebuild();
        }

        private static string GroupOf(string path)
        {
            int slash = path.IndexOf('/');
            return slash > 0 ? path.Substring(0, slash) : UngroupedName;
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int newline = text.IndexOf('\n');
            return newline < 0 ? text : text.Substring(0, newline);
        }
    }
}
