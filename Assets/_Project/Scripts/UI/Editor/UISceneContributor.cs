using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Editor.SceneBuild;
using MoonProject.Gameplay;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// The UI domain's part of Main.unity: under [UI], one GameUI object with the UIDocument (GameUI.uxml on the
    /// Lofi Lunar PanelSettings) and the <see cref="UISystem"/> (initialised last), wired to the UI tuning, the relic
    /// catalog and the string tables. Fails loudly when an asset is missing.
    /// </summary>
    public sealed class UISceneContributor : ISceneContributor
    {
        public int Order => 600;

        public void Contribute(SceneBuildContext context)
        {
            var tuning = context.LoadAsset<UiTuning>(UiAssetPaths.Tuning);
            var panel = context.LoadAsset<PanelSettings>(UiAssetPaths.PanelSettings);
            var layout = context.LoadAsset<VisualTreeAsset>(UiAssetPaths.Uxml);
            var catalog = context.LoadAsset<RelicCatalog>(UiAssetPaths.RelicCatalog);
            TextAsset[] tables = StringTableAssets.Load();
            string problem = tuning.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"UI scene build: UiTuning {problem}.");
            }

            GameObject host = context.CreateChild("GameUI", context.UIRoot.transform);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = layout;
            var system = host.AddComponent<UISystem>();
            system.Wire(document, tuning, catalog, tables);
            context.AddSystem(system);
        }
    }
}
