using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Builds <see cref="UiAssetPaths.RoverDiagram"/> by reading the equipment socket transforms from the generated
    /// 07 v3 prefab (<see cref="UiAssetPaths.RoverModelPrefab"/>; socket names in
    /// <c>tools/art/qa/contracts/rover.json</c>) and the 25 SVG icons from <see cref="UiAssetPaths.IconsFolder"/>.
    /// Fails loudly if the prefab, any socket transform, or any icon is missing.
    /// </summary>
    public static class RoverDiagramBuilder
    {
        [MoonBuilder("UI/Rover Diagram", UiTuningBuilder.BuilderOrder + 3)]
        public static void Build()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiAssetPaths.RoverModelPrefab);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"UI/Rover Diagram: missing rover model prefab at '{UiAssetPaths.RoverModelPrefab}' (run Art/R1/Rover Model first).");
            }

            Transform root = prefab.transform;
            Transform[] nodes = prefab.GetComponentsInChildren<Transform>(true);
            RoverSocket[] values = (RoverSocket[])Enum.GetValues(typeof(RoverSocket));
            if (values.Length != RoverDiagramData.SocketCount)
            {
                throw new InvalidOperationException(
                    $"UI/Rover Diagram: expected {RoverDiagramData.SocketCount} RoverSocket values, found {values.Length}.");
            }

            var sockets = new Vector3[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                RoverSocket socket = values[i];
                string primaryName = socket + "Socket";
                RequireNode(nodes, primaryName, "RoverSocket." + socket);
                string anchorName = RoverDiagramData.SocketNodeName(socket);
                Transform anchor = RequireNode(nodes, anchorName, "RoverSocket." + socket);
                sockets[i] = root.InverseTransformPoint(anchor.position);
            }

            var wheels = new Vector3[RoverDiagramData.WheelCount];
            for (int i = 0; i < RoverDiagramData.WheelCount; i++)
            {
                string wheelName = RoverDiagramData.WheelNodeName(i);
                Transform wheel = RequireNode(nodes, wheelName, wheelName);
                wheels[i] = root.InverseTransformPoint(wheel.position);
            }

            var data = ScriptableObject.CreateInstance<RoverDiagramData>();
            data.SetSockets(sockets);
            data.SetWheels(wheels);
            data.SetIcons(UiIconBuilder.ParseAllIcons());
            string problem = data.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"UI/Rover Diagram: RoverDiagramData {problem}.");
            }

            GeneratedAssets.CreateOrReplace(data, UiAssetPaths.RoverDiagram);
        }

        private static Transform RequireNode(Transform[] nodes, string name, string role)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && string.Equals(nodes[i].name, name, StringComparison.Ordinal))
                {
                    return nodes[i];
                }
            }

            throw new InvalidOperationException(
                $"UI/Rover Diagram: '{UiAssetPaths.RoverModelPrefab}' has no transform named '{name}' for {role}.");
        }
    }
}
