using System;
using UnityEngine;
using MoonProject.Editor.SceneBuild;
using MoonProject.Gameplay.Map;

namespace MoonProject.Gameplay.Editor.Map
{
    /// <summary>
    /// The crew's map in <c>Main.unity</c> (docs/features/M3-07): a "CrewMap" object under the gameplay root holding
    /// <see cref="MapSystem"/>, added to the systems right after <see cref="GameplaySystem"/> (whose parts it reads) and
    /// before UI (which draws the map). Its tuning is the committed <see cref="GameplayAssetPaths.MapTuning"/>, which
    /// Gameplay/Tuning creates with the code defaults only when it is missing.
    /// </summary>
    public sealed class MapSceneContributor : ISceneContributor
    {
        public int Order => 520;

        public void Contribute(SceneBuildContext context)
        {
            GameplaySystem gameplay = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                gameplay = system as GameplaySystem ?? gameplay;
            }

            if (gameplay == null)
            {
                throw new InvalidOperationException("The Gameplay contributor added no GameplaySystem.");
            }

            GameObject host = context.CreateChild("CrewMap", context.GameplayRoot.transform);
            var map = host.AddComponent<MapSystem>();
            map.Wire(gameplay, context.LoadAsset<MapTuning>(GameplayAssetPaths.MapTuning));
            context.AddSystem(map);
        }
    }
}
