using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// A complete gameplay stack built in test code (opening-v2): a flat world with the opening's anchors and the
    /// Kestrel's site anchor, a fake 07, test content (a stand-in Kestrel wreck and the material bundles, stand-in R1
    /// crates and ruin pieces, the base structures; default tuning; SoftGlow materials) and the real gameplay
    /// components, wired the way the scene contributor wires them and booted through GameBootstrap with a private
    /// save slot.
    /// </summary>
    public sealed class GameplayFixture : IDisposable
    {
        public const string ShaderPath = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string GlintShaderPath = "Assets/_Project/Shaders/Gameplay/Glint.shader";

        public static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));

        private readonly List<Object> _created = new List<Object>();
        private readonly InputActionAsset _controls;

        private GameplayFixture(InputActionAsset controls, string saveSlot)
        {
            _controls = controls;
            SaveSlot = saveSlot;
        }

        public string SaveSlot { get; }

        public FlatWorldSystem World { get; private set; }

        public GameplayFakeRover Rover { get; private set; }

        public GameplaySystem Gameplay { get; private set; }

        public GameBootstrap Bootstrap { get; private set; }

        public EventRecorder Events { get; private set; }

        public GlintTuning GlintTuning { get; private set; }

        public SalvageTuning SalvageTuning { get; private set; }

        /// <summary>The opening's anchors, reserve, cargo points and crates, as the tuning builder makes it.</summary>
        public OpeningTuning OpeningTuning { get; private set; }

        /// <summary>The ruins' sets and the scan verb (0.5.5 §1), as the tuning builder makes it.</summary>
        public ScanTuning ScanTuning { get; private set; }

        public CargoTuning CargoTuning { get; private set; }

        public BaseTuning BaseTuning { get; private set; }

        public BuildingTuning BuildingTuning { get; private set; }

        public StructureCatalog StructureCatalog { get; private set; }

        /// <summary>
        /// Builds and boots everything; 07 starts at the wake post facing +Z. A new game (no save yet) starts with an
        /// empty hold unless <paramref name="spares"/> keeps 07's starting spares (Metal 2, Wiring 1) in it, and with
        /// the cutter's igniter fitted unless <paramref name="startKit"/> keeps the opening's faulted cutter
        /// (opening-v2 §10): most sessions are about what the cutter does, not how it is fixed.
        /// </summary>
        public static GameplayFixture Boot(InputActionAsset controls, string saveSlot = null, bool spares = false,
            bool startKit = false)
        {
            var fixture = new GameplayFixture(controls, saveSlot ?? BootstrapHarness.NewTestSlot());
            fixture.Build(spares, startKit);
            return fixture;
        }

        /// <summary>The salvage site named <paramref name="name"/> ("kestrel").</summary>
        public SalvageSite FindSite(string name)
        {
            SalvageSite site = Gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + name);
            if (site == null)
            {
                throw new ArgumentException($"No salvage site '{name}'.", nameof(name));
            }

            return site;
        }

        /// <summary>Puts materials into 07's hold.</summary>
        public void GiveMaterials(int metal, int wiring, int optics)
        {
            Give(SalvageMaterial.Metal, metal);
            Give(SalvageMaterial.Wiring, wiring);
            Give(SalvageMaterial.Optics, optics);
        }

        /// <summary>
        /// Saves a capture of the rover camera to Logs/gameplay-captures/<paramref name="name"/>.png.
        /// </summary>
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

        private void Give(SalvageMaterial material, int units)
        {
            if (units > 0)
            {
                Gameplay.Materials.Collect(material, units);
            }
        }

        private void Build(bool spares, bool startKit)
        {
            World = Track(FlatWorldSystem.Create());
            Rover = Track(GameplayFakeRover.Create(Vector3.zero, 0f));
            GlintTuning = Asset<GlintTuning>();
            BaseTuning = Asset<BaseTuning>();
            ScanTuning = Asset<ScanTuning>();
            OpeningTuning = Asset<OpeningTuning>();
            CargoTuning = Asset<CargoTuning>();

            var visuals = Asset<GameplayVisuals>();
            Shader shader = LoadShader(ShaderPath);
            visuals.Populate(GlowMaterial(shader, GlowRole.WeldBeam), GlowMaterial(shader, GlowRole.Flash),
                GlowMaterial(shader, GlowRole.PieceHalo), Track(GlintMaterials.CreatePart(LoadShader(GlintShaderPath))),
                GlowMaterial(shader, GlowRole.Spark));
            visuals.PopulateFeel(GlowMaterial(shader, GlowRole.Spark), GlowMaterial(shader, GlowRole.WeldBeam),
                GlowMaterial(shader, GlowRole.Flash), GlowMaterial(shader, GlowRole.PieceHalo),
                Asset<FeelVfxTuning>());
            visuals.PopulateHologram(GlowMaterial(shader, GlowRole.WeldBeam), GlowMaterial(shader, GlowRole.Flash));
            visuals.PopulateContainers(new[]
            {
                CrateTemplate(ContainerKinds.CrateS, 0.6f), CrateTemplate(ContainerKinds.CrateM, 0.9f),
                CrateTemplate(ContainerKinds.CrateL, 1.2f),
            });

            var root = new GameObject("[Gameplay]");
            root.SetActive(false);
            Gameplay = root.AddComponent<GameplaySystem>();
            var salvage = Child<SalvageField>(root, "Salvage");
            var battery = Child<BatteryField>(root, "Battery");
            var build = Child<BuildField>(root, "Build");
            BuildStructures(build);
            BuildSalvage(salvage);
            battery.Wire(Asset<BatteryTuning>());
            var materials = Asset<MaterialCatalog>();
            materials.Populate(new[]
            {
                new MaterialEntry(SalvageMaterial.Metal, "metal", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Wiring, "wiring", MaterialFamily.Salvage, 4, true),
                new MaterialEntry(SalvageMaterial.Optics, "optics", MaterialFamily.Salvage, 4, true),
            });
            Gameplay.Wire(visuals, GlintTuning, salvage);
            Gameplay.WireBattery(battery);
            Gameplay.WireCargo(materials, CargoTuning);
            Gameplay.WireCutter(Asset<CutterTuning>());
            Gameplay.WireScans(ScanTuning);
            Gameplay.WireBuilding(build);
            Gameplay.WireRuins(Ruins(root.transform));
            Gameplay.WireOpening(OpeningTuning, BaseTuning);
            root.SetActive(true);

            Bootstrap = BootstrapHarness.Create(_controls, SaveSlot, World, Rover, Gameplay);
            bool newGame = Gameplay.Materials != null &&
                           Bootstrap.Context.Get<ISaveService>().LoadResult == SaveLoadResult.NoSave;
            if (newGame && !spares && !Gameplay.Materials.TrySpend(CargoTuning.StarterItems))
            {
                throw new InvalidOperationException("A new game's hold should carry 07's spares.");
            }

            if (newGame && !startKit)
            {
                Gameplay.StartKit.Restore(new StartKitSaveData { igniterFitted = true });
            }

            Events = new EventRecorder(Bootstrap.Context.Events);
        }

        /// <summary>
        /// The Kestrel as a stand-in wreck with the site contract's nodes (a skeleton and one small piece of each
        /// material, cut points toward 07's approach) and the three material bundles, as in the content builder.
        /// </summary>
        private void BuildSalvage(SalvageField field)
        {
            SalvageTuning = Asset<SalvageTuning>();
            GameObject model = SiteModel("kestrel");
            var catalog = Asset<SalvageCatalog>();
            catalog.Populate(new[]
                {
                    new SalvageSiteEntry(WorldAnchorIds.SitePrefix + "kestrel", model, 1,
                        SalvageCutLines.Across(model), SmallPieces(model)),
                }, Template("Material_Metal", Vector3.one * 0.35f), Template("Material_Wiring", Vector3.one * 0.35f),
                Template("Material_Optics", Vector3.one * 0.35f));
            field.Wire(SalvageTuning, catalog);
        }

        /// <summary>Every piece of a stand-in wreck is small (one short segment, the small yield).</summary>
        private static PieceSizeEntry[] SmallPieces(GameObject model)
        {
            var sizes = new List<PieceSize>();
            foreach (Transform node in model.transform)
            {
                if (SalvagePieceName.TryParsePiece(node.name, out _, out _, out _))
                {
                    sizes.Add(PieceSize.Small);
                }
            }

            return SalvageSizes.Deal(model, sizes);
        }

        /// <summary>
        /// A stand-in wreck in its anchor's frame (+Z the way 07 arrives): a skeleton wall behind, one small piece of
        /// each material in front of it, every cut point on the face toward 07's approach, and a drag piece (it cuts
        /// like any other in the opening).
        /// </summary>
        private GameObject SiteModel(string name)
        {
            var root = new GameObject("Site_" + name);
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            Named(Block(root.transform, new Vector3(0f, 1f, 4.5f), new Vector3(6f, 2f, 1f)), "Skeleton");
            Piece(root.transform, "Salvage_0_Metal", new Vector3(-3.5f, 0.4f, 0.5f), Vector3.one * 0.8f);
            Piece(root.transform, "Salvage_1_Wiring", new Vector3(3.5f, 0.4f, 0.5f), Vector3.one * 0.8f);
            Piece(root.transform, "Salvage_2_Optics", new Vector3(-2.5f, 0.4f, 3f), Vector3.one * 0.8f);
            Piece(root.transform, "Salvage_3_Metal_Drag", new Vector3(2.5f, 0.3f, 3f), new Vector3(1.4f, 0.4f, 1.4f));
            return root;
        }

        private static void Piece(Transform site, string name, Vector3 position, Vector3 size)
        {
            Transform piece = Named(Block(site, position, size), name);
            Node("CutPoint", piece, new Vector3(0f, 0f, -0.5f)).localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        /// <summary>
        /// Stand-ins for the Lumen ruin pieces World stands under [World]/Ruins, at the ruins anchor: each a block with
        /// its Scan_1..N targets (as ScanTuning's ruin sets) above it, the workshop and the scatter with none, and the
        /// habitat's airlock panel (its story scan).
        /// </summary>
        private Transform[] Ruins(Transform parent)
        {
            (string Id, Vector3 Offset, int Targets)[] pieces =
            {
                ("HabitatModules", new Vector3(-6f, 0f, 2f), 2), ("KenjiWorkshop", new Vector3(7f, 0f, 1f), 0),
                ("FallenSolarArray", new Vector3(2f, 0f, -12f), 3), ("RadioTower", new Vector3(2f, 0f, 14f), 2),
                ("ScatterSet", new Vector3(-6f, 0f, -12f), 0),
            };
            var ruins = new Transform[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                Transform piece = Node(pieces[i].Id, parent, new Vector3(95f, 0f, 185f) + pieces[i].Offset);
                Named(Block(piece, new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 2.4f)), "Body");
                if (pieces[i].Id == "HabitatModules")
                {
                    Named(Block(piece, new Vector3(-1.6f, 1.1f, -1.25f), new Vector3(0.5f, 0.6f, 0.05f)),
                        "AirlockPanel");
                }

                for (int t = 1; t <= pieces[i].Targets; t++)
                {
                    Node("Scan_" + t, piece, new Vector3(-1.2f + t * 0.8f, 1.3f, -1.3f));
                }

                ruins[i] = piece;
            }

            return ruins;
        }

        private void BuildStructures(BuildField field)
        {
            BuildingTuning = Asset<BuildingTuning>();
            StructureCatalog = Asset<StructureCatalog>();
            var ports = new[] { new StructurePort(new Vector3(0f, 0.2f, -1.1f), Vector3.back) };
            StructureCatalog.Populate(new[]
            {
                StructureDef(StructureIds.ChargingPad, new Vector3(2.2f, 0.4f, 2.2f), 12f, 0.25f, 0f, 120f, 0f, 0f,
                    new Recipe(4, 2, 0), 3f, ports, false),
                StructureDef(StructureIds.SolarArray, new Vector3(2.0f, 1.8f, 2.0f), 18f, 0.25f, 180f, 0f, 0f, 14f,
                    new Recipe(3, 2, 1), 3f, ports, false),
                StructureDef(StructureIds.BatteryBank, new Vector3(1.4f, 1.2f, 1.4f), 15f, 0.25f, 0f, 0f, 360f, 0f,
                    new Recipe(3, 3, 0), 2.5f, ports, true),
                StructureDef(StructureIds.PowerPylon, new Vector3(0.8f, 2.4f, 0.8f), 22f, 0.2f, 0f, 15f, 0f, 16f,
                    new Recipe(2, 1, 0), 1.5f, ports, false),
                StructureDef(StructureIds.AntennaMast, new Vector3(1.6f, 3.5f, 1.6f), 15f, 0.3f, 0f, 90f, 0f, 0f,
                    new Recipe(4, 3, 2), 4.5f, ports, true),
            });
            field.Wire(StructureCatalog, BuildingTuning);
        }

        private StructureDefinition StructureDef(string id, Vector3 footprint, float maxSlope, float clearance,
            float produceW, float consumeW, float storeWh, float radius, Recipe cost, float seconds,
            StructurePort[] ports, bool artPending)
        {
            var def = Asset<StructureDefinition>();
            GameObject prefab = artPending ? null : StructureTemplate(id, footprint);
            def.Populate(id, StructureIds.NameKey(id), footprint, maxSlope, clearance, produceW, consumeW, storeWh,
                radius, cost, seconds, ports, prefab, artPending);
            return def;
        }

        private GameObject StructureTemplate(string id, Vector3 footprint)
        {
            var root = new GameObject("Structure_" + id);
            root.layer = Layers.Prop;
            root.transform.position = new Vector3(0f, -500f, 0f);
            _created.Add(root);
            var stages = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float sliceH = footprint.y * 0.25f;
                Transform stage = Named(Block(root.transform, new Vector3(0f, sliceH * (i + 0.5f), 0f),
                    new Vector3(footprint.x, sliceH, footprint.z)), "Stage_" + (i + 1));
                stage.gameObject.layer = Layers.Prop;
                stages[i] = stage;
            }

            if (id == StructureIds.ChargingPad)
            {
                // The R1 pad's dock-lock bars, one per corner (PadLock raises them as 07 charges).
                for (int i = 0; i < 4; i++)
                {
                    Named(Block(root.transform, new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0.4f, i < 2 ? -0.9f : 0.9f),
                        new Vector3(0.1f, 0.8f, 0.1f)), "TotemBar_" + (i + 1));
                }
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, footprint.y * 0.5f, 0f);
            collider.size = footprint;
            var view = root.AddComponent<StructureView>();
            view.Populate(stages, new[] { stages[3].GetComponent<Renderer>() }, new Collider[] { collider });
            return root;
        }

        /// <summary>
        /// A stand-in for one of Art's R1 crates (opening-v2 §4): a body with its <c>Lid</c> hinged at the back,
        /// holding <c>CutPoint</c> and the glowing <c>Seal</c>, and the <c>ItemSocket</c> inside.
        /// </summary>
        private GameObject CrateTemplate(string name, float width)
        {
            GameObject container = Template(name, new Vector3(width, width * 0.66f, width * 0.78f));
            var lid = new GameObject(SealedContainer.LidPath);
            lid.transform.SetParent(container.transform, false);
            lid.transform.localPosition = new Vector3(0f, width * 0.66f, -width * 0.39f);
            var cut = new GameObject(SealedContainer.CutPointName);
            cut.transform.SetParent(lid.transform, false);
            cut.transform.localPosition = new Vector3(0f, 0f, width * 0.78f);
            Transform seal = Block(lid.transform, new Vector3(0f, 0f, width * 0.78f),
                new Vector3(width * 0.9f, 0.04f, 0.04f));
            seal.name = SealedContainer.SealName;
            var socket = new GameObject(SealedContainer.ItemSocketPath);
            socket.transform.SetParent(container.transform, false);
            socket.transform.localPosition = new Vector3(0f, width * 0.28f, 0f);
            return container;
        }

        private static Transform Named(Transform node, string name)
        {
            node.name = name;
            return node;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        private static Transform Block(Transform parent, Vector3 localPosition, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = size;
            return block.transform;
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
