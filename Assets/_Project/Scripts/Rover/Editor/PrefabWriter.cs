using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Saves builder-made prefabs only when they changed. Unity keeps file IDs when it overwrites a prefab by matching
    /// the new objects to the old ones, but it cannot tell two nested instances of the same prefab apart (07's two
    /// capacitor drums): the second one gets a fresh PrefabInstance ID on every save. Leaving an unchanged prefab
    /// untouched keeps every re-run byte-identical.
    /// </summary>
    public static class PrefabWriter
    {
        /// <summary>
        /// As <see cref="GeneratedAssets.SavePrefab"/> (names <paramref name="root"/> after the file, destroys it and
        /// returns the prefab asset), but leaves the file untouched when the saved prefab already serializes exactly
        /// like <paramref name="root"/> (see <see cref="HierarchyComparison"/>).
        /// </summary>
        public static GameObject SaveIfChanged(GameObject root, string path)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            root.name = Path.GetFileNameWithoutExtension(path);
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved == null)
            {
                return GeneratedAssets.SavePrefab(root, path);
            }

            string difference = HierarchyComparison.FirstDifference(saved, root);
            if (difference == null)
            {
                Object.DestroyImmediate(root);
                return saved;
            }

            Debug.Log($"{path} changed ({difference}); saving it.");
            return GeneratedAssets.SavePrefab(root, path);
        }
    }
}
