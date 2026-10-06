using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The UI booted through GameBootstrap the way UISceneContributor wires Main.unity (UIDocument on the generated
    /// PanelSettings, GameUI.uxml, the real tuning, catalog and string tables), behind <see cref="FakeGameServices"/>.
    /// The tuning is a copy with a short title and no prompt start delay, so tests reach the interesting states
    /// quickly. Editor-only (assets come through the AssetDatabase).
    /// </summary>
    internal sealed class UiTestRig : IDisposable
    {
        public const string PanelSettingsPath = "Assets/_Project/Generated/UI/LofiLunarPanel.asset";
        public const string UxmlPath = "Assets/_Project/UI/GameUI.uxml";
        public const string TuningPath = "Assets/_Project/Data/Tuning/UI/UiTuning.asset";
        public const string CatalogPath = "Assets/_Project/Data/Content/RelicCatalog.asset";
        public const string UpgradePath = "Assets/_Project/Data/Content/Upgrades/Upgrade_radio_tower.asset";
        public const string EnglishPath = "Assets/_Project/Data/Localization/en.json";
        public const string VietnamesePath = "Assets/_Project/Data/Localization/vi.json";

        private readonly GameObject _camera;
        private readonly GameObject _services;
        private readonly GameObject _ui;

        private UiTestRig(GameObject camera, GameObject services, GameObject ui, UiTuning tuning,
            GameBootstrap bootstrap, string saveSlot)
        {
            _camera = camera;
            _services = services;
            _ui = ui;
            Tuning = tuning;
            Bootstrap = bootstrap;
            SaveSlot = saveSlot;
            Fakes = services.GetComponent<FakeGameServices>();
            Ui = ui.GetComponent<UISystem>();
        }

        public GameBootstrap Bootstrap { get; }

        public UISystem Ui { get; }

        public FakeGameServices Fakes { get; }

        public UiTuning Tuning { get; }

        public string SaveSlot { get; }

        public Camera Camera => _camera.GetComponent<Camera>();

        public static UiTestRig Boot(InputActionAsset controls, string saveSlot)
        {
#if UNITY_EDITOR
            var camera = new GameObject("TestViewCamera");
            camera.AddComponent<Camera>();

            var services = new GameObject("FakeGameServices");
            var fakes = services.AddComponent<FakeGameServices>();
            fakes.Camera = camera.GetComponent<Camera>();
            fakes.Upgrade = Load<UpgradeDefinition>(UpgradePath);

            UiTuning tuning = QuickTuning();
            var ui = new GameObject("GameUI");
            ui.SetActive(false);
            var document = ui.AddComponent<UIDocument>();
            document.panelSettings = Load<PanelSettings>(PanelSettingsPath);
            document.visualTreeAsset = Load<VisualTreeAsset>(UxmlPath);
            var system = ui.AddComponent<UISystem>();
            system.Wire(document, tuning, Load<RelicCatalog>(CatalogPath),
                new[] { Load<TextAsset>(EnglishPath), Load<TextAsset>(VietnamesePath) });
            system.QuitAction = () => { };
            ui.SetActive(true);

            GameBootstrap bootstrap = BootstrapHarness.Create(controls, saveSlot, fakes, system);
            return new UiTestRig(camera, services, ui, tuning, bootstrap, saveSlot);
#else
            throw new NotSupportedException("UiTestRig loads assets through the editor's AssetDatabase.");
#endif
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_ui);
            Object.DestroyImmediate(Bootstrap.gameObject);
            Object.DestroyImmediate(_services);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(Tuning);
            Time.timeScale = 1f;
        }

#if UNITY_EDITOR
        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"{path} is missing: run the UI and Gameplay builders.");
            }

            return asset;
        }

        private static UiTuning QuickTuning()
        {
            UiTuning tuning = Object.Instantiate(Load<UiTuning>(TuningPath));
            var serialized = new SerializedObject(tuning);
            serialized.FindProperty("_title._delay").floatValue = 0f;
            serialized.FindProperty("_title._hold").floatValue = 0.1f;
            serialized.FindProperty("_title._reveal._fadeIn").floatValue = 0.1f;
            serialized.FindProperty("_title._reveal._fadeOut").floatValue = 0.1f;
            serialized.FindProperty("_prompts._startDelay").floatValue = 0f;
            serialized.FindProperty("_memoryCard._appearDelay").floatValue = 0.1f;
            serialized.FindProperty("_memoryCard._reveal._fadeIn").floatValue = 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tuning;
        }
#endif
    }
}
