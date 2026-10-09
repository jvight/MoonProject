using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Finds the first difference between two GameObject hierarchies as Unity serializes them: every serialized
    /// property of every GameObject and component, all the way down, nested prefab contents included (and which source
    /// object each nested object stands for). A reference to an object inside its own hierarchy is compared by where
    /// that object sits (child indices and component slot); any other reference (an asset) must be the same object.
    /// </summary>
    public static class HierarchyComparison
    {
        /// <summary>
        /// Prefab bookkeeping that tells a saved prefab from a fresh build by construction, not by content: the
        /// PrefabInstance record a nested object belongs to, and the prefab asset an object is stored in.
        /// </summary>
        private static readonly string[] PrefabBookkeeping = { "m_PrefabInstance", "m_PrefabAsset" };

        /// <summary>
        /// Where and how <paramref name="a"/> and <paramref name="b"/> first differ
        /// ("Rover/Visual/Chassis: Light.m_Range"), or null when they serialize alike.
        /// </summary>
        public static string FirstDifference(GameObject a, GameObject b)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            return Node(a.transform, b.transform, a.transform, b.transform);
        }

        private static string Node(Transform a, Transform b, Transform rootA, Transform rootB)
        {
            string difference = Properties(a.gameObject, b.gameObject, rootA, rootB);
            if (difference != null)
            {
                return $"{Where(a, rootA)}: GameObject.{difference}";
            }

            Component[] componentsA = a.GetComponents<Component>();
            Component[] componentsB = b.GetComponents<Component>();
            if (componentsA.Length != componentsB.Length)
            {
                return $"{Where(a, rootA)}: {componentsA.Length} vs {componentsB.Length} components";
            }

            for (int i = 0; i < componentsA.Length; i++)
            {
                if (componentsA[i] == null || componentsB[i] == null)
                {
                    return $"{Where(a, rootA)}: component {i} is a missing script";
                }

                Type type = componentsA[i].GetType();
                if (componentsB[i].GetType() != type)
                {
                    return $"{Where(a, rootA)}: component {i} is a {type.Name} vs a {componentsB[i].GetType().Name}";
                }

                difference = Properties(componentsA[i], componentsB[i], rootA, rootB);
                if (difference != null)
                {
                    return $"{Where(a, rootA)}: {type.Name}.{difference}";
                }
            }

            if (a.childCount != b.childCount)
            {
                return $"{Where(a, rootA)}: {a.childCount} vs {b.childCount} children";
            }

            for (int i = 0; i < a.childCount; i++)
            {
                difference = Node(a.GetChild(i), b.GetChild(i), rootA, rootB);
                if (difference != null)
                {
                    return difference;
                }
            }

            return null;
        }

        /// <summary>The path of the first serialized property that differs, or null.</summary>
        private static string Properties(Object a, Object b, Transform rootA, Transform rootB)
        {
            using (var serializedA = new SerializedObject(a))
            using (var serializedB = new SerializedObject(b))
            {
                SerializedProperty propertyA = serializedA.GetIterator();
                SerializedProperty propertyB = serializedB.GetIterator();
                bool moreA = propertyA.Next(true);
                bool moreB = propertyB.Next(true);
                while (moreA && moreB)
                {
                    string path = propertyA.propertyPath;
                    if (path != propertyB.propertyPath)
                    {
                        return $"{path} vs {propertyB.propertyPath}";
                    }

                    bool enterChildren = false;
                    if (propertyA.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (Array.IndexOf(PrefabBookkeeping, path) < 0 && !SameReference(
                                propertyA.objectReferenceValue, propertyB.objectReferenceValue, rootA, rootB))
                        {
                            return path;
                        }
                    }
                    else if (propertyA.hasChildren)
                    {
                        enterChildren = true;
                    }
                    else if (!SerializedProperty.DataEquals(propertyA, propertyB))
                    {
                        return path;
                    }

                    moreA = propertyA.Next(enterChildren);
                    moreB = propertyB.Next(enterChildren);
                }

                return moreA == moreB ? null : "(property count)";
            }
        }

        private static bool SameReference(Object a, Object b, Transform rootA, Transform rootB)
        {
            Transform ownerA = Owner(a);
            Transform ownerB = Owner(b);
            bool insideA = ownerA != null && ownerA.IsChildOf(rootA);
            bool insideB = ownerB != null && ownerB.IsChildOf(rootB);
            if (!insideA && !insideB)
            {
                return a == b;
            }

            return insideA && insideB && SamePlace(ownerA, rootA, ownerB, rootB) && Slot(a) == Slot(b);
        }

        private static Transform Owner(Object reference)
        {
            if (reference == null)
            {
                return null;
            }

            if (reference is GameObject gameObject)
            {
                return gameObject.transform;
            }

            return reference is Component component ? component.transform : null;
        }

        /// <summary>Whether a sits under <paramref name="rootA"/> where b sits under rootB.</summary>
        private static bool SamePlace(Transform a, Transform rootA, Transform b, Transform rootB)
        {
            while (a != rootA && b != rootB)
            {
                if (a.GetSiblingIndex() != b.GetSiblingIndex())
                {
                    return false;
                }

                a = a.parent;
                b = b.parent;
            }

            return a == rootA && b == rootB;
        }

        /// <summary>-1 for a GameObject, else the component's index on its GameObject.</summary>
        private static int Slot(Object reference)
        {
            return reference is Component component
                ? Array.IndexOf(component.GetComponents<Component>(), component)
                : -1;
        }

        private static string Where(Transform node, Transform root)
        {
            var path = new StringBuilder(node.name);
            for (Transform current = node; current != root; current = current.parent)
            {
                path.Insert(0, '/').Insert(0, current.parent.name);
            }

            return path.ToString();
        }
    }
}
