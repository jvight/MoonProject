using System;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Puts 07 and its camera into Main.unity. The rover stands on the base pad at the world origin facing +Z; its
    /// height is taken from the World's <c>ITerrainQuery</c> when it initialises, so it always spawns resting on the
    /// ground (no drop, no landing event, no guessing the pad height at build time).
    /// Systems, in initialisation order (after World): RoverController (registers IRoverState),
    /// RoverBodyLanguage (needs IWorldLayout, registers IRoverGaze), RoverCameraRig (needs IRoverState).
    /// </summary>
    public sealed class RoverSceneContributor : ISceneContributor
    {
        public int Order => 300;

        public void Contribute(SceneBuildContext context)
        {
            Transform parent = context.RoverRoot.transform;
            GameObject rover = context.InstantiatePrefab(RoverAssetPaths.RoverPrefab, parent);
            GameObject cameraRig = context.InstantiatePrefab(RoverAssetPaths.CameraRigPrefab, parent);

            context.AddSystem(Require<RoverController>(rover));
            context.AddSystem(Require<RoverBodyLanguage>(rover));
            context.AddSystem(Require<RoverCameraRig>(cameraRig));
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
