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
    /// PanelSettings, GameUI.uxml, the real tuning, catalog and string tables), behind
    /// <see cref="FakeGameServices"/>. The tuning is a copy with a short title, no prompt start delay and quick
    /// ticker fades, so tests reach the interesting states quickly; it also gains the prompt entries the UI/Tuning
    /// builder adds for interaction kinds newer than the asset. Editor-only (assets come through the AssetDatabase).
    /// </summary>
    internal sealed class UiTestRig : IDisposable
    {
        public const string PanelSettingsPath = "Assets/_Project/Generated/UI/LofiLunarPanel.asset";
        public const string UxmlPath = "Assets/_Project/UI/GameUI.uxml";
        public const string TuningPath = "Assets/_Project/Data/Tuning/UI/UiTuning.asset";
        public const string CatalogPath = "Assets/_Project/Data/Content/RelicCatalog.asset";
        public const string UpgradePath = "Assets/_Project/Data/Content/Upgrades/Upgrade_radio_tower.asset";
        public const string WorkbenchUpgradePath =
            "Assets/_Project/Data/Content/Upgrades/Upgrade_rover_hover_jump.asset";
        public const string TillyPath = "Assets/_Project/Data/Content/Friends/Friend_tilly.asset";
        public const string EnglishPath = "Assets/_Project/Data/Localization/en.json";
        public const string VietnamesePath = "Assets/_Project/Data/Localization/vi.json";

        /// <summary>Bell's signal ticker line; its {0} is a bearing.</summary>
        public const string SignalLine = "ticker.bell.signal";

        /// <summary>Bell's first homecoming ticker line (no argument).</summary>
        public const string HomeLine = "ticker.bell.home";

        /// <summary>M3-05 content with text in the shipped tables: Ro's first crew log and two of her tapes.</summary>
        public const string FirstLog = "ro_1";
        public const string FirstTape = "after_dark_1";
        public const string SecondTape = "dust_and_honey";

        /// <summary>Name keys of the radio-hop's nodes: home and the first two masts.</summary>
        public const string HomeNode = "hop.node.home";
        public const string FirstRelayNode = "hop.node.relay.0";
        public const string SecondRelayNode = "hop.node.relay.1";

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
            fakes.Friend = Load<FriendDefinition>(TillyPath);
            fakes.Position = new Vector3(0f, 0f, -500f);
            fakes.TillyStatus = new FriendStatus(FriendState.Dormant, 0, 3, false, false, new Vector3(0f, 0f, 500f));

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

        /// <summary>Kenji's workbench offer (Hover-Jump) in place of the radio tower's.</summary>
        public static UpgradeDefinition WorkbenchUpgrade()
        {
#if UNITY_EDITOR
            return Load<UpgradeDefinition>(WorkbenchUpgradePath);
#else
            throw new NotSupportedException("UiTestRig loads assets through the editor's AssetDatabase.");
#endif
        }

        /// <summary>Overrides one number of this rig's tuning copy (a serialized property path).</summary>
        public void Tune(string propertyPath, float value)
        {
#if UNITY_EDITOR
            var serialized = new SerializedObject(Tuning);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                throw new ArgumentException($"UiTuning has no '{propertyPath}'.", nameof(propertyPath));
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            throw new NotSupportedException("Tuning overrides go through the editor's SerializedObject.");
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
            tuning.Prompts.AddMissingDefaults();
            var serialized = new SerializedObject(tuning);
            serialized.FindProperty("_title._delay").floatValue = 0f;
            serialized.FindProperty("_title._hold").floatValue = 0.1f;
            serialized.FindProperty("_title._reveal._fadeIn").floatValue = 0.1f;
            serialized.FindProperty("_title._reveal._fadeOut").floatValue = 0.1f;
            serialized.FindProperty("_prompts._startDelay").floatValue = 0f;
            serialized.FindProperty("_memoryCard._appearDelay").floatValue = 0.1f;
            serialized.FindProperty("_memoryCard._reveal._fadeIn").floatValue = 0.2f;
            serialized.FindProperty("_ticker._gapSeconds").floatValue = 0.2f;
            serialized.FindProperty("_ticker._reveal._fadeIn").floatValue = 0.2f;
            serialized.FindProperty("_ticker._reveal._fadeOut").floatValue = 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tuning;
        }
#endif
    }
}
