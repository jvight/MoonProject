using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Puts the relay pads' warm pools (<see cref="ChargerPools"/>, docs/features/M3-20) into Main.unity right after
    /// the World's own systems (order 215), so the world anchors it places its discs on are registered first.
    /// </summary>
    public sealed class ChargerPoolSceneContributor : ISceneContributor
    {
        public int Order => 215;

        public void Contribute(SceneBuildContext context)
        {
            var pools = context.CreateChild("Charger Pools", context.WorldRoot.transform).AddComponent<ChargerPools>();
            var serialized = new SerializedObject(pools);
            Assign(serialized, "_settings", context.LoadAsset<WorldSettings>(WorldPaths.Settings));
            Assign(serialized, "_discPrefab", context.LoadAsset<GameObject>(WorldPaths.LightPoolPrefab));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(pools);
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ChargerPools)} has no serialized field '{field}'; update " +
                    $"{nameof(ChargerPoolSceneContributor)}.");
            }

            property.objectReferenceValue = value;
        }
    }
}
