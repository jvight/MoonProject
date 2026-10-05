using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// The <c>MoonProject/Build</c> menu: a static "Build All" item plus one item per registered builder at
    /// <c>MoonProject/Build/&lt;path&gt;</c>. Per-builder items are added at runtime through Unity's internal
    /// <c>Menu.AddMenuItem</c> (the public MenuItem attribute cannot be generated per builder) and re-added whenever
    /// Unity rebuilds its menus. If a future Unity removes that internal API, one warning explains it and
    /// Build All plus the batch entry point keep working.
    /// </summary>
    [InitializeOnLoad]
    public static class BuilderMenu
    {
        public const string Root = "MoonProject/Build/";
        private const int BuildAllPriority = 0;
        private const int FirstBuilderPriority = 100;
        private const BindingFlags Internal = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo AddMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", Internal, null,
            new[] { typeof(string), typeof(string), typeof(bool), typeof(int), typeof(Action), typeof(Func<bool>) },
            null);

        private static readonly MethodInfo MenuItemExistsMethod =
            typeof(Menu).GetMethod("MenuItemExists", Internal, null, new[] { typeof(string) }, null);

        private static readonly MethodInfo UpdateAllMenusMethod =
            typeof(EditorUtility).GetMethod("Internal_UpdateAllMenus", Internal, null, Type.EmptyTypes, null);

        private static readonly EventInfo MenuChangedEvent =
            typeof(Menu).GetEvent("menuChanged", Internal | BindingFlags.Public);

        static BuilderMenu()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (!IsSupported)
            {
                Debug.LogWarning("MoonProject: this Unity version has no internal Menu.AddMenuItem; per-builder menu " +
                                 "items are unavailable. Use MoonProject/Build/Build All or the batch RunBuilders entry.");
                return;
            }

            if (MenuChangedEvent != null)
            {
                MenuChangedEvent.GetAddMethod(true)?.Invoke(null, new object[] { (Action)ScheduleRegistration });
            }

            ScheduleRegistration();
        }

        /// <summary>True when the per-builder menu items can be registered on this Unity version.</summary>
        public static bool IsSupported => AddMenuItemMethod != null && MenuItemExistsMethod != null;

        [MenuItem(Root + "Build All", priority = BuildAllPriority)]
        private static void BuildAll()
        {
            BuilderRegistry.DiscoverAndRun(string.Empty);
        }

        /// <summary>Adds missing per-builder items. Returns how many were added (0 if unsupported).</summary>
        public static int RegisterBuilderItems()
        {
            if (!IsSupported)
            {
                return 0;
            }

            IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Discover(out IReadOnlyList<string> problems);
            foreach (string problem in problems)
            {
                Debug.LogError(problem);
            }

            int added = 0;
            for (int i = 0; i < builders.Count; i++)
            {
                string menuPath = Root + builders[i].Path;
                if ((bool)MenuItemExistsMethod.Invoke(null, new object[] { menuPath }))
                {
                    continue;
                }

                string pattern = "^" + Regex.Escape(builders[i].Path) + "$";
                Action execute = () => BuilderRegistry.DiscoverAndRun(pattern);
                AddMenuItemMethod.Invoke(null, new object[] { menuPath, string.Empty, false,
                    FirstBuilderPriority + i, execute, null });
                added++;
            }

            if (added > 0)
            {
                UpdateAllMenusMethod?.Invoke(null, null);
            }

            return added;
        }

        private static void ScheduleRegistration()
        {
            EditorApplication.delayCall -= RegisterFromDelayCall;
            EditorApplication.delayCall += RegisterFromDelayCall;
        }

        private static void RegisterFromDelayCall()
        {
            RegisterBuilderItems();
        }
    }
}
