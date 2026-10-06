using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// A complete gameplay stack built in test code: a flat world, a fake 07, test content (relic and scrap stand-in
    /// meshes in place of the Art prefabs, default tuning, SoftGlow materials) and the real gameplay components,
    /// wired the way the scene contributor wires them and booted through GameBootstrap with a private save slot.
    /// </summary>
    public sealed class GameplayFixture : IDisposable
    {
        public const string ShaderPath = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShaderPath = "Assets/_Project/Shaders/Gameplay/Glint.shader";

        public static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));

        private static readonly string[] RelicIds =
        {
            "cassette_player", "rubber_duck", "golden_record", "astronaut_boot", "teapot", "garden_gnome",
        };

        private static readonly RelicPlacementBand[] RelicBands =
        {
            RelicPlacementBand.Onboarding, RelicPlacementBand.Onboarding, RelicPlacementBand.RimView,
            RelicPlacementBand.Wanderer, RelicPlacementBand.Wanderer, RelicPlacementBand.Wanderer,
        };

        private static readonly float[] RelicMasses = { 6f, 3f, 5f, 9f, 7f, 14f };

        private readonly List<Object> _created = new List<Object>();
        private readonly InputActionAsset _controls;

        private GameplayFixture(InputActionAsset controls, string saveSlot)
        {
            _controls = controls;
            SaveSlot = saveSlot;
        }

        public string SaveSlot { get; }

        public FlatWorldSystem World { get; private set; }

        public FakeRoverSystem Rover { get; private set; }

        public GameplaySystem Gameplay { get; private set; }

        public GameBootstrap Bootstrap { get; private set; }

        public EventRecorder Events { get; private set; }

        public ScrapTuning ScrapTuning { get; private set; }

        public SonarTuning SonarTuning { get; private set; }

        public RelicTuning RelicTuning { get; private set; }

        public ExcavationTuning ExcavationTuning { get; private set; }

        public TetherTuning TetherTuning { get; private set; }

        /// <summary>Builds and boots everything; 07 starts at the base facing +Z.</summary>
        public static GameplayFixture Boot(InputActionAsset controls, string saveSlot = null)
        {
            var fixture = new GameplayFixture(controls, saveSlot ?? BootstrapHarness.NewTestSlot());
            fixture.Build();
            return fixture;
        }

        public Relic FindRelic(string id)
        {
            Relic relic = Gameplay.Relics.Find(id);
            if (relic == null)
            {
                throw new ArgumentException($"No relic '{id}'.", nameof(id));
            }

            return relic;
        }

        /// <summary>Saves a capture of the rover camera to Logs/gameplay-captures/<paramref name="name"/>.png.</summary>
        public void Capture(string name)
        {
            FrameCapture.SavePng(Rover.Camera, 960, 540, Path.Combine(CaptureFolder, name + ".png"));
        }

        /// <summary>Tears the stack down; <paramref name="keepSave"/> leaves the save file for a reboot.</summary>
        public void Dispose(bool keepSave)
        {
            Events?.Dispose();
            if (Bootstrap != null)
            {
                Object.DestroyImmediate(Gameplay.gameObject);
                Object.DestroyImmediate(Bootstrap.gameObject);
            }

            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            if (!keepSave)
            {
                BootstrapHarness.DeleteSaveFiles(SaveSlot);
            }
        }

        public void Dispose()
        {
            Dispose(false);
        }

        private void Build()
        {
            World = Track(FlatWorldSystem.Create());
            Rover = Track(FakeRoverSystem.Create(Vector3.zero, 0f));

            ScrapTuning = Asset<ScrapTuning>();
            SonarTuning = Asset<SonarTuning>();
            RelicTuning = Asset<RelicTuning>();
            ExcavationTuning = Asset<ExcavationTuning>();
            TetherTuning = Asset<TetherTuning>();
            var placement = Asset<RelicPlacementTuning>();

            var relicCatalog = Asset<RelicCatalog>();
            var definitions = new RelicDefinition[RelicIds.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i] = Asset<RelicDefinition>();
                definitions[i].Populate(RelicIds[i], "Test " + RelicIds[i], "A test memory of Earth.",
                    RelicMasses[i], Template("Relic_" + RelicIds[i], new Vector3(0.7f, 0.6f, 0.5f)), i,
                    RelicBands[i]);
            }

            relicCatalog.Populate(definitions);
            var scrapCatalog = Asset<ScrapCatalog>();
            scrapCatalog.Populate(new[]
            {
                new ScrapVariant(Template("Scrap_A", Vector3.one * 0.3f), 1, 3f),
                new ScrapVariant(Template("Scrap_B", Vector3.one * 0.45f), 2, 2f),
            });

            var visuals = Asset<GameplayVisuals>();
            Shader shader = LoadShader(ShaderPath);
            visuals.Populate(GlowMaterial(shader, GlowRole.SonarRing), GlowMaterial(shader, GlowRole.SiteRing),
                GlowMaterial(shader, GlowRole.SitePillar), GlowMaterial(shader, GlowRole.TractorBeam),
                GlowMaterial(shader, GlowRole.TetherBeam), GlowMaterial(shader, GlowRole.Flash),
                GlowMaterial(shader, GlowRole.RelicHalo), GlowMaterial(shader, GlowRole.Dust),
                GlowMaterial(shader, GlowRole.WarmRing), GlowMaterial(shader, GlowRole.WarmGlow),
                Track(GlintMaterials.Create(LoadShader(GlintShaderPath))));

            var root = new GameObject("[Gameplay]");
            root.SetActive(false);
            Gameplay = root.AddComponent<GameplaySystem>();
            var relics = Child<RelicField>(root, "Relics");
            var scrap = Child<ScrapField>(root, "Scrap");
            var sonar = Child<SonarSystem>(root, "Sonar");
            var excavation = Child<ExcavationSystem>(root, "Excavation");
            var tether = Child<TetherSystem>(root, "Tether");
            relics.Wire(relicCatalog, placement, RelicTuning);
            scrap.Wire(ScrapTuning, scrapCatalog);
            sonar.Wire(SonarTuning);
            excavation.Wire(ExcavationTuning);
            tether.Wire(TetherTuning);
            Gameplay.Wire(visuals, relics, scrap, sonar, excavation, tether);
            root.SetActive(true);

            Bootstrap = BootstrapHarness.Create(_controls, SaveSlot, World, Rover, Gameplay);
            Events = new EventRecorder(Bootstrap.Context.Events);
        }

        private GameObject Template(string name, Vector3 size)
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = name;
            Object.DestroyImmediate(template.GetComponent<Collider>());
            template.transform.position = new Vector3(0f, -500f, 0f);
            template.transform.localScale = size;
            var prefab = new GameObject(name);
            prefab.transform.position = new Vector3(0f, -500f, 0f);
            template.transform.SetParent(prefab.transform, true);
            _created.Add(prefab);
            return prefab;
        }

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        private Material GlowMaterial(Shader shader, GlowRole role)
        {
            return Track(GlowMaterials.Create(shader, role));
        }

        private T Asset<T>() where T : ScriptableObject
        {
            return Track(ScriptableObject.CreateInstance<T>());
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created is Component component ? component.gameObject : (Object)created);
            return created;
        }

        private static Shader LoadShader(string path)
        {
#if UNITY_EDITOR
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader missing at {path}.");
            }

            return shader;
#else
            throw new NotSupportedException("The fixture loads the shader through the editor's AssetDatabase.");
#endif
        }
    }
}
