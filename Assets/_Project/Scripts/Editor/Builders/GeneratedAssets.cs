using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// Idempotent asset writes for builders. Re-running a builder must update assets in place so their GUIDs, and
    /// therefore every prefab/scene reference to them, survive. All paths are project-relative ("Assets/...").
    /// </summary>
    public static class GeneratedAssets
    {
        /// <summary>Root of all builder output; each domain writes below Generated/&lt;Domain&gt;/.</summary>
        public const string Root = "Assets/_Project/Generated";

        /// <summary>Creates every missing folder of <paramref name="folder"/> (e.g. "Assets/_Project/Generated/Art").</summary>
        public static void EnsureFolder(string folder)
        {
            folder = Normalize(folder);
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent))
            {
                throw new ArgumentException($"'{folder}' is not below Assets/.", nameof(folder));
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// Stores <paramref name="asset"/> (a new, in-memory object such as a Mesh or Material) at
        /// <paramref name="path"/>. If an asset of the same type already exists there, its contents are replaced in
        /// place and the existing object is returned (same GUID); otherwise the new asset is created and returned.
        /// </summary>
        public static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            path = Normalize(path);
            EnsureFolder(Path.GetDirectoryName(path));
            asset.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    throw new InvalidOperationException(
                        $"{path} holds a {AssetDatabase.GetMainAssetTypeAtPath(path).Name}, not a {typeof(T).Name}.");
                }

                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            existing.name = asset.name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }

        /// <summary>
        /// Saves <paramref name="root"/> (a scene object you built) as the prefab at <paramref name="path"/>, keeping
        /// the prefab's GUID when it already exists, then destroys <paramref name="root"/>. Returns the prefab asset.
        /// </summary>
        public static GameObject SavePrefab(GameObject root, string path)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            path = Normalize(path);
            EnsureFolder(Path.GetDirectoryName(path));
            root.name = Path.GetFileNameWithoutExtension(path);
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || prefab == null)
                {
                    throw new InvalidOperationException($"Saving prefab {path} failed.");
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Writes raw file bytes (PNG, WAV, ...) at <paramref name="path"/> and imports it, but only when the content
        /// changed, so re-running a builder does not trigger needless reimports. Returns true if the file changed.
        /// </summary>
        public static bool WriteFileIfChanged(string path, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            path = Normalize(path);
            EnsureFolder(Path.GetDirectoryName(path));
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath) && ContentEquals(File.ReadAllBytes(fullPath), bytes))
            {
                return false;
            }

            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static bool ContentEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is empty.", nameof(path));
            }

            path = path.Replace('\\', '/').TrimEnd('/');
            if (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException($"'{path}' must be a project path starting with Assets/.", nameof(path));
            }

            return path;
        }
    }
}
