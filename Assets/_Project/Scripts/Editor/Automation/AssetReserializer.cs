using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Re-writes assets through Unity's serializer so fields added to a ScriptableObject since the asset was last
    /// saved are written out with the values they load at today. Only the selection is plain logic; the write is
    /// <see cref="AssetDatabase.ForceReserializeAssets(IEnumerable{string}, ForceReserializeAssetsOptions)"/>.
    /// </summary>
    public static class AssetReserializer
    {
        private const string ProjectRoot = "Assets/_Project/";

        /// <summary>
        /// The paths under Assets/_Project/ that match <paramref name="pattern"/> (case-insensitive regex), sorted.
        /// An empty pattern is refused: reserializing the whole project in one go is never what a caller means.
        /// </summary>
        public static IReadOnlyList<string> Select(IEnumerable<string> assetPaths, string pattern)
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException("A path filter is required.", nameof(pattern));
            }

            var filter = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var selected = new List<string>();
            foreach (string path in assetPaths)
            {
                if (path.StartsWith(ProjectRoot, StringComparison.Ordinal) && filter.IsMatch(path))
                {
                    selected.Add(path);
                }
            }

            selected.Sort(StringComparer.Ordinal);
            return selected;
        }

        /// <summary>Reserializes the selected assets; returns how many were written.</summary>
        public static int Reserialize(IReadOnlyList<string> paths)
        {
            AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
            AssetDatabase.SaveAssets();
            return paths.Count;
        }
    }
}
