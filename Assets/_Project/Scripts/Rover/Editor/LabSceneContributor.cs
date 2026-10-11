using System;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using MoonProject.Art.Editor;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Puts the crew's lab camera into Main.unity (docs/features/M3-19 §6): a Cinemachine camera with no body or aim,
    /// placed each frame by the <see cref="LabCamera"/>, wired to the chase rig, 07 and the built lander's belly lift
    /// and lab nodes (docs/ARCHITECTURE.md "Contract: the lab"). Runs after Gameplay's base (order 500) and the opening
    /// (700), so the lander stands and the lab camera initialises last.
    /// </summary>
    public sealed class LabSceneContributor : ISceneContributor
    {
        public int Order => 710;

        public void Contribute(SceneBuildContext context)
        {
            RoverCameraRig rig = null;
            RoverController rover = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                rig = system as RoverCameraRig ?? rig;
                rover = system as RoverController ?? rover;
            }

            if (rig == null || rover == null)
            {
                throw new InvalidOperationException("The Rover contributor added no rover or camera rig.");
            }

            Transform lander = Find(context.GameplayRoot.transform, BaseModelBuilder.LanderName);
            Transform shaft = Child(lander, BaseModelBuilder.BellyLiftName);
            Transform lab = Child(lander, BaseModelBuilder.LabName);
            var stations = new Transform[Enum.GetValues(typeof(LabStation)).Length];
            for (int i = 0; i < stations.Length; i++)
            {
                stations[i] = Child(lab, BaseModelBuilder.StationPrefix + (LabStation)i);
            }

            GameObject host = context.CreateChild("Lab Camera", context.RoverRoot.transform);
            var labCamera = host.AddComponent<CinemachineCamera>();
            labCamera.enabled = false;
            var view = context.CreateChild("Lab View", context.RoverRoot.transform).AddComponent<LabCamera>();
            var serialized = new SerializedObject(view);
            Assign(serialized, "_tuning", context.LoadAsset<RoverCameraTuning>(RoverAssetPaths.CameraTuning));
            Assign(serialized, "_rig", rig);
            Assign(serialized, "_rover", rover);
            Assign(serialized, "_camera", labCamera);
            Assign(serialized, "_platform", Child(shaft, BaseModelBuilder.PlatformName));
            Assign(serialized, "_shaft", shaft);
            Assign(serialized, "_ringRail", Child(lab, BaseModelBuilder.RingRailName));
            SerializedProperty frames = Property(serialized, "_stations");
            frames.arraySize = stations.Length;
            for (int i = 0; i < stations.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }

            Property(serialized, "_ringRadius").floatValue = BaseModelBuilder.LabRingRadius;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(view);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            throw new InvalidOperationException(
                $"No '{name}' under {root.name}: the lab camera needs it; rebuild Gameplay's base.");
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                throw new InvalidOperationException(
                    $"No '{name}' under {parent.name}: the lab camera needs it (lab contract, docs/ARCHITECTURE.md).");
            }

            return child;
        }

        private static SerializedProperty Property(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(LabCamera)} has no serialized field '{field}'; update {nameof(LabSceneContributor)}.");
            }

            return property;
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            Property(serialized, field).objectReferenceValue = value;
        }
    }
}
