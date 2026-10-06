using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// The string tables in Assets/_Project/Data/Localization (one &lt;code&gt;.json per language), in the order the UI
    /// wants them: the primary language (en) first, then the others by code. Adding a language is adding a file.
    /// </summary>
    internal static class StringTableAssets
    {
        public const string Extension = ".json";

        /// <summary>Every table, primary first; throws when the primary table is missing (fail fast).</summary>
        public static TextAsset[] Load()
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { UiAssetPaths.LocalizationFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(Extension, StringComparison.Ordinal))
                {
                    paths.Add(path);
                }
            }

            string primary = UiAssetPaths.StringTable(UiAssetPaths.PrimaryLanguage);
            if (!paths.Remove(primary))
            {
                throw new InvalidOperationException($"The primary string table {primary} is missing.");
            }

            paths.Sort(StringComparer.Ordinal);
            paths.Insert(0, primary);
            var tables = new TextAsset[paths.Count];
            for (int i = 0; i < paths.Count; i++)
            {
                tables[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);
                if (tables[i] == null)
                {
                    throw new InvalidOperationException($"{paths[i]} is not a text asset.");
                }
            }

            return tables;
        }

        /// <summary>Language code of a table file ("vi" for .../vi.json).</summary>
        public static string LanguageOf(TextAsset table)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(table));
        }
    }
}
