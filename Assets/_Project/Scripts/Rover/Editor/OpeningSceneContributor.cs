using System;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Puts 07's opening into Main.unity (docs/design/cutscenes.md §1): the lens camera (a Cinemachine camera with no
    /// body or aim, placed each frame by the opening) and the <see cref="OpeningDirector"/>, wired to the rig, 07's
    /// body language and the two gaze targets of the built home: the lander and the tower's pilot (The Peak and
    /// Earth come from IWorldLayout). Runs after the World's home wiring (order 650), which makes the pilot, and after Rover's own systems,
    /// so the director initialises last of the three.
    /// </summary>
    public sealed class OpeningSceneContributor : ISceneContributor
    {
        private const string LanderName = "Lander";
        private const string PilotName = "TowerPilot";

        public int Order => 700;

        public void Contribute(SceneBuildContext context)
        {
            RoverCameraRig rig = null;
            RoverBodyLanguage body = null;
            RoverController controller = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                rig = system as RoverCameraRig ?? rig;
                controller = system as RoverController ?? controller;
                body = system as RoverBodyLanguage ?? body;
            }

            if (rig == null || body == null || controller == null)
            {
                throw new InvalidOperationException("The Rover contributor added no rover, camera rig or body language.");
            }

            Transform rover = context.RoverRoot.transform;
            GameObject lensHost = context.CreateChild("Lens Camera", rover);
            var lensCamera = lensHost.AddComponent<CinemachineCamera>();
            lensCamera.enabled = false;

            var director = context.CreateChild("Opening", rover).AddComponent<OpeningDirector>();
            var serialized = new SerializedObject(director);
            Assign(serialized, "_tuning", context.LoadAsset<RoverCameraTuning>(RoverAssetPaths.CameraTuning));
            Assign(serialized, "_rig", rig);
            Assign(serialized, "_controller", controller);
            Assign(serialized, "_body", body);
            Assign(serialized, "_lensCamera", lensCamera);
            Assign(serialized, "_lander", Find(context.GameplayRoot.transform, LanderName));
            Assign(serialized, "_pilot", Find(context.GameplayRoot.transform, PilotName));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(director);
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
                $"No '{name}' under {root.name}: the opening's gaze needs it; rebuild Gameplay's base and the World.");
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(OpeningDirector)} has no serialized field '{field}'; update {nameof(OpeningSceneContributor)}.");
            }

            property.objectReferenceValue = value;
        }
    }
}
