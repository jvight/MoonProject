using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Builder helpers: assigns private [SerializeField]s by name (failing loudly on typos) and locates RoverModel
    /// contract nodes by name (failing loudly when the art contract is not met).
    /// </summary>
    public static class BuildWiring
    {
        /// <summary>Sets object reference fields on <paramref name="target"/>: pairs of (field name, value).</summary>
        public static void Assign(Object target, params (string Field, Object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
            {
                Property(serialized, field).objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sets an object reference array field on <paramref name="target"/>.</summary>
        public static void AssignArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = Property(serialized, field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Property(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{field}'.");
            }

            return property;
        }

        /// <summary>The descendant of <paramref name="root"/> named <paramref name="name"/>; throws if absent.</summary>
        public static Transform Node(Transform root, string name)
        {
            Transform found = Search(root, name);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"RoverModel has no node '{name}' (rig contract, docs/ARCHITECTURE.md); "
                    + "ask the art box to add it.");
            }

            return found;
        }

        /// <summary>The <typeparamref name="T"/> on contract node <paramref name="name"/>; throws if absent.</summary>
        public static T NodeComponent<T>(Transform root, string name) where T : Component
        {
            Transform node = Node(root, name);
            if (!node.TryGetComponent(out T component))
            {
                throw new InvalidOperationException($"RoverModel node '{name}' has no {typeof(T).Name}.");
            }

            return component;
        }

        private static Transform Search(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                Transform deeper = Search(child, name);
                if (deeper != null)
                {
                    return deeper;
                }
            }

            return null;
        }

        /// <summary>Loads a required asset; throws with the builder to run when it is missing.</summary>
        public static T Require<T>(string path, string producedBy) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing {typeof(T).Name} at {path} (produced by {producedBy}).");
            }

            return asset;
        }
    }
}
