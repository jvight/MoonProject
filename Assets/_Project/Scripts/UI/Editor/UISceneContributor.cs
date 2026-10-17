using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Editor.SceneBuild;
using MoonProject.Gameplay;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// The UI domain's part of Main.unity: under [UI], one GameUI object with the UIDocument (GameUI.uxml on the
    /// Lunarion PanelSettings) and the <see cref="UISystem"/> (initialised last), wired to the UI tuning and the
    /// string tables. Fails loudly when an asset is missing.
    /// </summary>
    public sealed class UISceneContributor : ISceneContributor
    {
        public int Order => 600;

        public void Contribute(SceneBuildContext context)
        {
            var tuning = context.LoadAsset<UiTuning>(UiAssetPaths.Tuning);
            var panel = context.LoadAsset<PanelSettings>(UiAssetPaths.PanelSettings);
            var facePanel = context.LoadAsset<PanelSettings>(UiAssetPaths.FacePanelSettings);
            var layout = context.LoadAsset<VisualTreeAsset>(UiAssetPaths.Uxml);
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

            GameObject faceHost = context.CreateChild("FaceUI", context.UIRoot.transform);
            var faceDocument = faceHost.AddComponent<UIDocument>();
            faceDocument.panelSettings = facePanel;

            var system = host.AddComponent<UISystem>();
            system.Wire(document, tuning, tables, context.LoadAsset<WordmarkShape>(UiAssetPaths.Wordmark),
                context.LoadAsset<RoverDiagramData>(UiAssetPaths.RoverDiagram),
                context.LoadAsset<TextAsset>(UiAssetPaths.Credits), faceDocument);
            var structureIcons = new Texture2D[StructureIds.All.Length];
            for (int i = 0; i < StructureIds.All.Length; i++)
            {
                structureIcons[i] = context.LoadAsset<Texture2D>(UiAssetPaths.StructureIcon(StructureIds.All[i]));
            }

            system.WireStructureIcons(structureIcons);
            context.AddSystem(system);
        }
    }
}
