using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core.Save;
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
        public const string WordmarkPath = "Assets/_Project/Generated/UI/Wordmark.asset";
        public const string TuningPath = "Assets/_Project/Data/Tuning/UI/UiTuning.asset";
        public const string CatalogPath = "Assets/_Project/Data/Content/RelicCatalog.asset";
        public const string UpgradePath = "Assets/_Project/Data/Content/Upgrades/Upgrade_radio_tower.asset";
        public const string HoverJumpUpgradePath =
            "Assets/_Project/Data/Content/Upgrades/Upgrade_rover_hover_jump.asset";
        public const string CradleUpgradePath =
            "Assets/_Project/Data/Content/Upgrades/Upgrade_rover_cargo_cradle.asset";
        public const string HeadlampUpgradePath =
            "Assets/_Project/Data/Content/Upgrades/Upgrade_rover_warm_headlamp.asset";
        public const string TillyPath = "Assets/_Project/Data/Content/Friends/Friend_tilly.asset";
        public const string CreditsPath = "Assets/_Project/UI/Credits/credits.json";
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
        private readonly CargoTuning _cargoTuning;
        private readonly List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();
        private PanelSettings _framedPanel;
        private RenderTexture _frame;

        private UiTestRig(GameObject camera, GameObject services, GameObject ui, UiTuning tuning,
            GameBootstrap bootstrap, string saveSlot, CargoTuning cargoTuning)
        {
            _cargoTuning = cargoTuning;
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

        /// <summary>The texture the UI renders into (a rig booted with a frame size), else null.</summary>
        public RenderTexture Frame => _frame;

        public Camera Camera => _camera.GetComponent<Camera>();

        /// <summary>
        /// Boots the UI. With a <paramref name="frameWidth"/> and <paramref name="frameHeight"/> it renders into a
        /// texture of that size instead of the screen, so a test can lay the UI out for any screen shape
        /// (<see cref="Reframe"/> changes it later).
        /// </summary>
        public static UiTestRig Boot(InputActionAsset controls, string saveSlot, int frameWidth = 0,
            int frameHeight = 0)
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
            PanelSettings framedPanel = null;
            RenderTexture frame = null;
            PanelSettings panel = Load<PanelSettings>(PanelSettingsPath);
            if (frameWidth > 0 && frameHeight > 0)
            {
                frame = NewFrame(frameWidth, frameHeight);
                framedPanel = Object.Instantiate(panel);
                framedPanel.targetTexture = frame;
                panel = framedPanel;
            }

            document.panelSettings = panel;
            document.visualTreeAsset = Load<VisualTreeAsset>(UxmlPath);
            var system = ui.AddComponent<UISystem>();
            CargoTuning cargoTuning = ScriptableObject.CreateInstance<CargoTuning>();
            system.Wire(document, tuning, Load<RelicCatalog>(CatalogPath),
                new[] { Load<TextAsset>(EnglishPath), Load<TextAsset>(VietnamesePath) },
                Load<WordmarkShape>(WordmarkPath), Load<TextAsset>(CreditsPath), cargoTuning);
            system.QuitAction = () => { };
            ui.SetActive(true);

            GameBootstrap bootstrap = BootstrapHarness.Create(controls, saveSlot, fakes, system);
            return new UiTestRig(camera, services, ui, tuning, bootstrap, saveSlot, cargoTuning)
            {
                _framedPanel = framedPanel,
                _frame = frame,
            };
#else
            throw new NotSupportedException("UiTestRig loads assets through the editor's AssetDatabase.");
#endif
        }

        /// <summary>Kenji's Rover Bay offer (Hover-Jump) in place of the radio tower's.</summary>
        public static UpgradeDefinition HoverJumpUpgrade()
        {
#if UNITY_EDITOR
            return Load<UpgradeDefinition>(HoverJumpUpgradePath);
#else
            throw new NotSupportedException("UiTestRig loads assets through the editor's AssetDatabase.");
#endif
        }

        /// <summary>The radio tower with three known recipes (2/1/0, 4/2/1, 6/4/2), whatever the content.</summary>
        public UpgradeDefinition TestTower()
        {
            return UpgradeCosting(UpgradePath, new Recipe(2, 1, 0), new Recipe(4, 2, 1), new Recipe(6, 4, 2));
        }

        /// <summary>
        /// Kenji's Rover Bay selling the Hover-Jump, the Cargo Cradle and the Warm Headlamp, in that order, at exactly
        /// these recipes (copies destroyed with the rig).
        /// </summary>
        public UpgradeDefinition[] TestBay(Recipe hoverJump, Recipe cargoCradle, Recipe warmHeadlamp)
        {
            return new[]
            {
                UpgradeCosting(HoverJumpUpgradePath, hoverJump),
                UpgradeCosting(CradleUpgradePath, cargoCradle),
                UpgradeCosting(HeadlampUpgradePath, warmHeadlamp),
            };
        }

        /// <summary>
        /// A copy of the upgrade at <paramref name="path"/> whose levels cost exactly <paramref name="recipes"/>, so a
        /// test reads the recipes it set whatever the content's tuning; destroyed with the rig.
        /// </summary>
        public UpgradeDefinition UpgradeCosting(string path, params Recipe[] recipes)
        {
            UpgradeDefinition copy = CopyCosting(path, recipes);
            _upgrades.Add(copy);
            return copy;
        }

        /// <summary>
        /// A copy of the upgrade at <paramref name="path"/> whose levels cost exactly <paramref name="recipes"/>; the
        /// caller destroys it.
        /// </summary>
        public static UpgradeDefinition CopyCosting(string path, params Recipe[] recipes)
        {
#if UNITY_EDITOR
            UpgradeDefinition copy = Object.Instantiate(Load<UpgradeDefinition>(path));
            var serialized = new SerializedObject(copy);
            SerializedProperty levels = serialized.FindProperty("_levels");
            if (levels.arraySize != recipes.Length)
            {
                throw new ArgumentException($"{path} has {levels.arraySize} levels, not {recipes.Length}.");
            }

            for (int i = 0; i < recipes.Length; i++)
            {
                SerializedProperty recipe = levels.GetArrayElementAtIndex(i).FindPropertyRelative("_recipe");
                recipe.FindPropertyRelative("_metal").intValue = recipes[i].Metal;
                recipe.FindPropertyRelative("_wiring").intValue = recipes[i].Wiring;
                recipe.FindPropertyRelative("_optics").intValue = recipes[i].Optics;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return copy;
#else
            throw new NotSupportedException("UiTestRig edits assets through the editor's SerializedObject.");
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

        /// <summary>
        /// Deletes a test slot's save and its settings file (the harness knows the save; the settings file is the
        /// UI's, beside it).
        /// </summary>
        public static void DeleteSlotFiles(string slot)
        {
            BootstrapHarness.DeleteSaveFiles(slot);
            var settings = new SettingsFile(SaveService.DefaultDirectory, slot);
            File.Delete(settings.FilePath);
            File.Delete(settings.BackupPath);
        }

        /// <summary>Lays the UI out for another screen shape. Only for a rig booted with a frame size.</summary>
        public void Reframe(int width, int height)
        {
            if (_framedPanel == null)
            {
                throw new InvalidOperationException("Reframe needs a rig booted with a frame size.");
            }

            RenderTexture previous = _frame;
            _frame = NewFrame(width, height);
            _framedPanel.targetTexture = _frame;
            ReleaseFrame(previous);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_ui);
            Object.DestroyImmediate(_framedPanel);
            ReleaseFrame(_frame);
            Object.DestroyImmediate(Bootstrap.gameObject);
            Object.DestroyImmediate(_services);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(Tuning);
            Object.DestroyImmediate(_cargoTuning);
            foreach (UpgradeDefinition upgrade in _upgrades)
            {
                Object.DestroyImmediate(upgrade);
            }

            Time.timeScale = 1f;
        }

        private static RenderTexture NewFrame(int width, int height)
        {
            var frame = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            frame.Create();
            return frame;
        }

        private static void ReleaseFrame(RenderTexture frame)
        {
            if (frame == null)
            {
                return;
            }

            frame.Release();
            Object.DestroyImmediate(frame);
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
            serialized.FindProperty("_title._reveal._fadeIn").floatValue = 0.1f;
            serialized.FindProperty("_title._reveal._fadeOut").floatValue = 0.1f;
            serialized.FindProperty("_title._curtain._fadeIn").floatValue = 0.1f;
            serialized.FindProperty("_title._curtain._fadeOut").floatValue = 0.1f;
            serialized.FindProperty("_credits._startDelaySeconds").floatValue = 0.2f;
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
