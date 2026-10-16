using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Puts 07 and its camera into Main.unity. The rover stands on its dock on the base pad, at the spawn position and
    /// facing the spawn yaw from <see cref="RoverTuning"/> (the camera starts behind it, so that yaw frames the first
    /// shot); its height
    /// is taken from the World's <c>ITerrainQuery</c> when it initialises, so it always spawns resting on the ground
    /// (no drop, no landing event, no guessing the pad height at build time).
    /// Systems, in initialisation order (after World): RoverController (registers IRoverState and IRoverRig),
    /// RoverBodyLanguage (needs IWorldLayout), RoverCameraRig (needs IRoverState, registers IViewCamera).
    /// The rover prefab comes from the Rover/Rover builder; a missing or stale prefab fails the scene build.
    /// </summary>
    public sealed class RoverSceneContributor : ISceneContributor
    {
        public int Order => 300;

        public void Contribute(SceneBuildContext context)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverPrefab);
            string problem = PrefabProblem(prefab);
            if (problem != null)
            {
                Debug.LogError($"{RoverAssetPaths.RoverPrefab}: {problem}; run the Rover/Rover builder " +
                               "(MoonProject/Build/Build All) before the Main Scene.", prefab);
                return;
            }

            Transform parent = context.RoverRoot.transform;
            var tuning = context.LoadAsset<RoverTuning>(RoverAssetPaths.RoverTuning);
            GameObject rover = context.InstantiatePrefab(prefab, parent, Vector3.zero, Quaternion.identity);
            Vector2 spawn = tuning.Spawn.Position;
            rover.transform.localPosition = new Vector3(spawn.x, 0f, spawn.y);
            rover.transform.localRotation = Quaternion.Euler(0f, tuning.Spawn.Yaw, 0f);
            GameObject cameraRig = context.InstantiatePrefab(RoverAssetPaths.CameraRigPrefab, parent);

            context.AddSystem(Require<RoverController>(rover));
            context.AddSystem(Require<RoverBodyLanguage>(rover));
            context.AddSystem(Require<RoverCameraRig>(cameraRig));
        }

        /// <summary>
        /// What the rover prefab lacks for the scene: the prefab itself, its <see cref="RoverController"/>, a cutter
        /// origin, or a tether origin of its own; null when it has everything.
        /// </summary>
        private static string PrefabProblem(GameObject prefab)
        {
            if (prefab == null)
            {
                return "the rover prefab is missing";
            }

            if (!prefab.TryGetComponent(out RoverController controller))
            {
                return $"the prefab has no {nameof(RoverController)}";
            }

            if (controller.CutterOrigin == null)
            {
                return $"{nameof(RoverController)}.{nameof(RoverController.CutterOrigin)} is missing";
            }

            return controller.TetherOrigin == controller.CutterOrigin
                ? $"{nameof(RoverController)}.{nameof(RoverController.TetherOrigin)} is the " +
                  $"{nameof(RoverController.CutterOrigin)}, not its own node"
                : null;
        }

        private static T Require<T>(GameObject host) where T : Component
        {
            if (!host.TryGetComponent(out T component))
            {
                throw new InvalidOperationException($"{host.name} has no {typeof(T).Name}; rebuild Rover prefabs.");
            }

            return component;
        }
    }
}
