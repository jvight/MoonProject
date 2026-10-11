using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Core.Map;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;
using MoonProject.UI.Map;

namespace MoonProject.UI.Editor.Map
{
    /// <summary>
    /// Gives the <see cref="UISystem"/> the crew's map (docs/features/M3-07): World's traced <see cref="MapSheet"/> and
    /// the map view's tuning, created once with the code defaults and never overwritten.
    /// </summary>
    public sealed class MapUiContributor : ISceneContributor
    {
        /// <summary>Written by World's map sheet builder (World/Map Sheet).</summary>
        public const string SheetPath = "Assets/_Project/Generated/World/Map/MapSheet.asset";

        public const string TuningPath = UiAssetPaths.TuningFolder + "/MapViewTuning.asset";

        public int Order => 610;

        [MoonBuilder("UI/Map Tuning", UiTuningBuilder.BuilderOrder + 1)]
        public static void BuildTuning()
        {
            EnsureTuning();
        }

        public void Contribute(SceneBuildContext context)
        {
            UISystem ui = null;
            foreach (MonoBehaviour system in context.Systems)
            {
                ui = system as UISystem ?? ui;
            }

            if (ui == null)
            {
                throw new InvalidOperationException("The UI contributor added no UISystem.");
            }

            var sheet = context.LoadAsset<MapSheet>(SheetPath);
            string problem = sheet.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"{SheetPath}: {problem}");
            }

            ui.WireMap(sheet, EnsureTuning());
            EditorUtility.SetDirty(ui);
        }

        private static MapViewTuning EnsureTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<MapViewTuning>(TuningPath);
            return tuning != null ? tuning
                : GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<MapViewTuning>(), TuningPath);
        }
    }
}
