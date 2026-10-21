using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Core.Input;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// A complete, playable 07 built in test code only: the real Rover components and default tuning on a minimal
    /// primitive stand-in for the art box's RoverModel (same node names and contract dimensions), plus the real camera
    /// rig. A <see cref="ScriptedDrive"/> holds the wheel, and the camera listens on its own Cinemachine channel, so
    /// devices and cameras left by other tests cannot interfere. Nothing here ships: the game's prefab always wraps the
    /// art model.
    /// </summary>
    public sealed class TestRover : IDisposable
    {
        private const float WheelRadius = 0.35f;
        private const float HalfTrack = 0.61f;
        private const float SuspZ = 0.31f;
        private const float WheelOffsetZ = 0.22f;
        private const float AxleSpacing = SuspZ + WheelOffsetZ;

        /// <summary>The battery's flank lamps stand-in spacing along the stripe.</summary>
        private const float FlankLampSpacing = 0.06f;

        /// <summary>The real headlamp glass material as the stand-in body's second slot, as on the built Body mesh.</summary>
        private const string LampGlassMaterialPath = "Assets/_Project/Generated/Art/R1/Materials/R1_LampAmber.mat";
        private const int LampGlassSlot = 1;

        /// <summary>The charge-up's cell stand-ins on the deck: their offset, height and spacing.</summary>
        private const float CellX = 0.2f;
        private const float CellY = 0.62f;
        private const float CellSpacing = 0.2f;

        /// <summary>The cargo hatch stand-in on the rear face and its crate window's slots.</summary>
        private const float HatchY = 0.65f;
        private const float HatchZ = -0.7f;
        private const int HatchCrateCount = 8;

        /// <summary>Cinemachine channel for rover tests: their brains and cameras only see each other.</summary>
        private const OutputChannels TestChannel = OutputChannels.Channel15;

        private readonly GameObject _cameraRoot;
        private readonly InputActionAsset _actions;
        private readonly InputReader _input;
        private readonly PhysicsMaterial _sphereMaterial;
        private readonly Mesh _screenMesh;
        private readonly ScriptableObject[] _tunings;

        private TestRover(GameObject root, GameObject cameraRoot, InputActionAsset actions, InputReader input,
            PhysicsMaterial sphereMaterial, Mesh screenMesh, ScriptableObject[] tunings)
        {
            Root = root;
            _cameraRoot = cameraRoot;
            _actions = actions;
            _input = input;
            _sphereMaterial = sphereMaterial;
            _screenMesh = screenMesh;
            _tunings = tunings;
        }

        /// <summary>The scripted stick holding 07's wheel (x = steer, y = throttle).</summary>
        public ScriptedDrive Drive { get; } = new ScriptedDrive();

        public GameObject Root { get; }

        public GameContext Context { get; private set; }

        public RoverController Controller { get; private set; }

        public RoverVisualRig Rig { get; private set; }

        public RoverBodyLanguage BodyLanguage { get; private set; }

        public RoverCameraRig CameraRig { get; private set; }

        public Camera Camera { get; private set; }

        public Transform Chassis { get; private set; }

        public Transform Neck { get; private set; }

        public Transform NeckLift { get; private set; }

        public Transform Head { get; private set; }

        public Transform SolarWing { get; private set; }

        public Light EyeLight { get; private set; }

        public RoverLampMotes LampMotes { get; private set; }

        public Light Headlamp { get; private set; }

        /// <summary>The road light's dimmer (docked, crawling).</summary>
        public RoverHeadlamp RoadLight { get; private set; }

        /// <summary>Tilly and Bell, dormant until a test wakes them.</summary>
        public TestFriendRoster Friends { get; } = new TestFriendRoster();

        /// <summary>The cutter's state for the charge-up's brace and warm window (an <see cref="ICutter"/> double).</summary>
        public TestCutter Cutter { get; } = new TestCutter();

        /// <summary>The world's anchors: only micro.06, where 07's elder sits (far away unless a test places it).</summary>
        public TestAnchors Anchors { get; } = new TestAnchors();

        /// <summary>The in-memory save 07's own section registers with.</summary>
        public TestSave Save { get; } = new TestSave();

        /// <summary>The world's landmarks (a test may move the base to bring 07 home).</summary>
        public TestWorldLayout Layout { get; } = new TestWorldLayout();

        public RoverTuning Tuning => (RoverTuning)_tunings[0];

        public RoverRigTuning RigTuning => (RoverRigTuning)_tunings[1];

        public RoverFxTuning FxTuning => (RoverFxTuning)_tunings[2];

        public RoverCameraTuning CameraTuning => (RoverCameraTuning)_tunings[3];

        public RoverCharacterTuning CharacterTuning => (RoverCharacterTuning)_tunings[4];

        public FaceTuning FaceTuning => (FaceTuning)_tunings[5];

        public FaceDirectorTuning FaceDirectorTuning => (FaceDirectorTuning)_tunings[6];

        public FaceDisplay FaceDisplay { get; private set; }

        public RoverFaceDirector FaceDirector { get; private set; }

        /// <summary>
        /// Builds, wires and initialises a rover at <paramref name="position"/>, yaw in degrees. It starts awake
        /// unless <paramref name="asleep"/> asks for the game's first-boot wake-up. <paramref name="wideShotDelay"/>
        /// (seconds) shortens the rest before the wide shot opens, so sessions need not wait the shipped delay.
        /// </summary>
        /// <param name="prepare">Sets what a loaded game had (anchors, saved sections) before 07 initialises.</param>
        public static TestRover Spawn(TestWorld world, Vector3 position, float yaw, bool asleep = false,
            float wideShotDelay = 0f, Action<TestRover> prepare = null)
        {
            var tuning = ScriptableObject.CreateInstance<RoverTuning>();
            var rigTuning = ScriptableObject.CreateInstance<RoverRigTuning>();
            var fxTuning = ScriptableObject.CreateInstance<RoverFxTuning>();
            var cameraTuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            var characterTuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            var faceTuning = ScriptableObject.CreateInstance<FaceTuning>();
            var faceDirectorTuning = ScriptableObject.CreateInstance<FaceDirectorTuning>();
            var tunings = new ScriptableObject[]
            {
                tuning, rigTuning, fxTuning, cameraTuning, characterTuning, faceTuning, faceDirectorTuning
            };
            var character = new SerializedObject(characterTuning);
            character.FindProperty("_sleepOnBoot").boolValue = asleep;
            character.ApplyModifiedPropertiesWithoutUndo();
            if (wideShotDelay > 0f)
            {
                var cameraSettings = new SerializedObject(cameraTuning);
                cameraSettings.FindProperty("_wideShot._delay").floatValue = wideShotDelay;
                cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            var root = new GameObject("TestRover");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var controller = root.AddComponent<RoverController>();
            var body = root.AddComponent<RoverBodyLanguage>();

            var sphere = new GameObject("PhysicsSphere") { layer = Layers.Rover };
            sphere.transform.SetParent(root.transform, false);
            var rigidbody = sphere.AddComponent<Rigidbody>();
            var collider = sphere.AddComponent<SphereCollider>();
            var sphereMaterial = new PhysicsMaterial("Frictionless")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            collider.sharedMaterial = sphereMaterial;

            Transform visual = Node("Visual", root.transform, Vector3.zero);
            var rig = visual.gameObject.AddComponent<RoverVisualRig>();
            Transform chassis = Node("Chassis", visual, Vector3.zero);
            Transform model = Node("RoverModel", chassis, Vector3.zero);
            Material material = world.Material;

            Transform bodyNode = Node(RoverModelNodes.Body, model, Vector3.zero);
            var bodyMesh = Shape(PrimitiveType.Cube, bodyNode, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 0.55f, 1.9f),
                material).GetComponent<MeshRenderer>();
            var lampGlass = AssetDatabase.LoadAssetAtPath<Material>(LampGlassMaterialPath);
            if (lampGlass == null)
            {
                throw new InvalidOperationException($"TestRover: no headlamp glass material at {LampGlassMaterialPath}.");
            }

            bodyMesh.sharedMaterials = new[] { material, lampGlass };
            var wheels = new Object[RoverModelNodes.WheelCount];
            var arms = new Object[RoverModelNodes.WheelCount];
            var shocks = new Object[RoverModelNodes.WheelCount];
            for (int i = 0; i < wheels.Length; i++)
            {
                bool left = i % 2 == 0;
                bool front = RoverModelNodes.WheelRow(i) == 0;
                float suspX = left ? -0.42f : 0.42f;
                float z = front ? SuspZ : -SuspZ;
                Transform susp = Node(RoverModelNodes.Susp(i), model, new Vector3(suspX, 0.26f, z));
                Transform arm = Node(RoverModelNodes.Arm(i), susp, Vector3.zero);
                Transform shock = Node(RoverModelNodes.Shock(i), susp, new Vector3(left ? -0.06f : 0.06f, 0.14f, 0f));
                Transform wheel = Node(RoverModelNodes.Wheel(i), susp,
                    new Vector3((left ? -HalfTrack : HalfTrack) - suspX, WheelRadius - 0.26f,
                        front ? WheelOffsetZ : -WheelOffsetZ));
                Transform tyre = Shape(PrimitiveType.Cylinder, wheel, Vector3.zero,
                    new Vector3(2f * WheelRadius, 0.1f, 2f * WheelRadius), material);
                tyre.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheels[i] = wheel;
                arms[i] = arm;
                shocks[i] = shock;
            }

            Transform neck = Node(RoverModelNodes.Neck, model, new Vector3(0f, 0.63f, 0.22f));
            Transform neckLift = Node(RoverModelNodes.NeckLift, neck, new Vector3(0f, 0.22f, 0.06f));
            Transform head = Node(RoverModelNodes.Head, neckLift, new Vector3(0f, 0.22f, 0.04f));
            Transform screen = Shape(PrimitiveType.Quad, head, new Vector3(0f, 0f, 0.20f),
                new Vector3(0.32f, 0.20f, 1f), material);
            screen.name = RoverModelNodes.Screen;
            Transform rearVentStrip = Shape(PrimitiveType.Cube, head, new Vector3(0f, 0f, -0.18f),
                new Vector3(0.24f, 0.04f, 0.02f), material);
            rearVentStrip.name = RoverModelNodes.RearVentStrip;
            Transform eye = Shape(PrimitiveType.Sphere, head, new Vector3(0.20f, 0.02f, 0.16f), Vector3.one * 0.22f,
                material);
            eye.name = RoverModelNodes.Eye;
            Transform eyeOrigin = Node(RoverModelNodes.EyeOrigin, eye, new Vector3(0f, 0f, 0.08f));
            Transform cutterTurret = Node(RoverModelNodes.CutterTurret, bodyNode, new Vector3(0.18f, 0.42f, 0.54f));
            Transform cutterHead = Node(RoverModelNodes.CutterHead, cutterTurret, new Vector3(0f, 0f, 0.07f));
            Transform cutterOrigin = Node(RoverModelNodes.CutterOrigin, cutterHead, new Vector3(0f, 0f, 0.11f));
            Transform digScoop = Node(RoverModelNodes.DigScoop, bodyNode, new Vector3(0f, 0.24f, 0.52f));
            Transform scoopArms = Node(RoverModelNodes.ScoopArms, digScoop, new Vector3(0f, 0f, 0.04f));
            Transform scoopBucket = Node(RoverModelNodes.ScoopBucket, scoopArms, new Vector3(0f, -0.02f, 0.14f));
            Transform scoopOrigin = Node(RoverModelNodes.ScoopOrigin, scoopBucket, new Vector3(0f, -0.02f, 0.10f));
            Transform winchSocket = Node(RoverModelNodes.WinchSocket, bodyNode, new Vector3(-0.18f, 0.34f, 0.55f));
            Transform winchSpool = Node(RoverModelNodes.WinchSpool, winchSocket, new Vector3(0f, 0f, 0.03f));
            Transform towHook = Node(RoverModelNodes.TowHook, winchSocket, new Vector3(0f, -0.01f, 0.08f));
            Transform winchOrigin = Node(RoverModelNodes.WinchOrigin, towHook, new Vector3(0f, 0f, 0.05f));
            Transform wing = Node(RoverModelNodes.SolarWing, model, new Vector3(0f, 1.08f, -0.5f));
            Transform antenna = Node(RoverModelNodes.Antenna, model, new Vector3(0.45f, 1.05f, -0.75f));
            Transform tip = Shape(PrimitiveType.Sphere, antenna, new Vector3(0f, 0.9f, 0f), Vector3.one * 0.06f,
                material);
            tip.name = RoverModelNodes.AntennaTip;
            Transform antennaSegment = Node(RoverModelNodes.AntennaSegment2, antenna, new Vector3(0f, 0.5f, 0f));
            Transform igniter = Node(RoverModelNodes.CutterIgniter, cutterTurret, new Vector3(0f, 0.04f, 0.02f));
            Transform lampSocket = Node(RoverModelNodes.HeadlampSocket, model, new Vector3(0f, 0.55f, 1f));
            Transform browSocket = Node(RoverModelNodes.BrowSocket, head, new Vector3(0f, 0.14f, 0.19f));
            Transform headlampPod = Node("Kit_SSR3Headlamp", browSocket, Vector3.zero);
            Transform housingLeft = Shape(PrimitiveType.Cube, headlampPod, new Vector3(-0.454f, -0.13f, -0.07f),
                new Vector3(0.11f, 0.18f, 0.36f), material);
            housingLeft.name = RoverModelNodes.HeadlampHousingLeft;
            Transform lensLeft = Shape(PrimitiveType.Sphere, housingLeft, new Vector3(0f, 0f, 0.18f),
                Vector3.one * 0.12f, lampGlass);
            lensLeft.name = RoverModelNodes.HeadlampLensLeft;
            Transform housingRight = Shape(PrimitiveType.Cube, headlampPod, new Vector3(0.454f, -0.13f, -0.07f),
                new Vector3(0.11f, 0.18f, 0.36f), material);
            housingRight.name = RoverModelNodes.HeadlampHousingRight;
            Transform lensRight = Shape(PrimitiveType.Sphere, housingRight, new Vector3(0f, 0f, 0.18f),
                Vector3.one * 0.12f, lampGlass);
            lensRight.name = RoverModelNodes.HeadlampLensRight;
            Transform podSocketLeft = Node(RoverModelNodes.PodSocketLeft, model, new Vector3(-0.62f, 0.58f, -0.05f));
            Transform cellPodLeft = Node("Kit_CellPod", podSocketLeft, Vector3.zero);
            cellPodLeft.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Transform podSocketRight = Node(RoverModelNodes.PodSocketRight, model, new Vector3(0.62f, 0.58f, -0.05f));
            Transform cellPodRight = Node("Kit_CellPod", podSocketRight, Vector3.zero);
            var cellPodBars = new Object[RoverModelNodes.CellPodBarCount * 2];
            for (int i = 0; i < RoverModelNodes.CellPodBarCount; i++)
            {
                Transform barLeft = Shape(PrimitiveType.Cube, cellPodLeft, new Vector3(0.186f, -0.02f + i * 0.075f, 0f),
                    new Vector3(0.014f, 0.028f, 0.44f), lampGlass);
                barLeft.name = RoverModelNodes.CellPodBar(i);
                cellPodBars[i] = barLeft.GetComponent<MeshRenderer>();
                Transform barRight = Shape(PrimitiveType.Cube, cellPodRight, new Vector3(0.186f, -0.02f + i * 0.075f, 0f),
                    new Vector3(0.014f, 0.028f, 0.44f), lampGlass);
                barRight.name = RoverModelNodes.CellPodBar(i);
                cellPodBars[RoverModelNodes.CellPodBarCount + i] = barRight.GetComponent<MeshRenderer>();
            }

            Transform cargo = Node(RoverModelNodes.CargoSocket, model, new Vector3(0f, 1.05f, -0.4f));
            Transform dustLeft = Node(RoverModelNodes.DustSocketLeft, model, new Vector3(-HalfTrack, 0f, -AxleSpacing));
            Transform dustRight =
                Node(RoverModelNodes.DustSocketRight, model, new Vector3(HalfTrack, 0f, -AxleSpacing));

            var headlamp = Node("Headlamp", lampSocket, Vector3.zero).gameObject.AddComponent<Light>();
            headlamp.transform.localRotation = Quaternion.Euler(rigTuning.HeadlampDownTilt, 0f, 0f);
            var eyeLight = Node("EyeGlow", eye, Vector3.zero).gameObject.AddComponent<Light>();

            var flankLamps = new Transform[RoverModelNodes.FlankLampCount];
            var flankGlasses = new Renderer[RoverModelNodes.FlankLampCount];
            for (int i = 0; i < flankLamps.Length; i++)
            {
                var at = new Vector3(0.55f, 1.08f, -0.05f - i * FlankLampSpacing);
                flankLamps[i] = Node(RoverModelNodes.FlankLamp(i), bodyNode, at);
                Transform glass = Shape(PrimitiveType.Cube, flankLamps[i], Vector3.zero, new Vector3(0.04f, 0.01f, 0.05f),
                    material);
                glass.name = RoverModelNodes.FlankGlass(i);
                flankGlasses[i] = glass.GetComponent<MeshRenderer>();
            }

            var flank = visual.gameObject.AddComponent<RoverFlankLamps>();
            var towRope = Node("TowRope", visual, Vector3.zero).gameObject.AddComponent<LineRenderer>();
            var towClamp = Node("TowClamp", visual, Vector3.zero).gameObject.AddComponent<LineRenderer>();
            towRope.sharedMaterial = material;
            towClamp.sharedMaterial = material;
            var towCable = visual.gameObject.AddComponent<RoverTowCable>();
            Assign(towCable, ("_tuning", rigTuning), ("_rope", towRope), ("_hook", towClamp), ("_winch", winchOrigin));
            var roadLight = visual.gameObject.AddComponent<RoverHeadlamp>();
            var growth = visual.gameObject.AddComponent<RoverGrowth>();
            Assign(growth, ("_tuning", rigTuning), ("_antennaSegment", antennaSegment), ("_igniter", igniter),
                ("_turret", cutterTurret), ("_headlamp", roadLight), ("_headlampPod", headlampPod),
                ("_headlampHousingLeft", housingLeft), ("_headlampHousingRight", housingRight),
                ("_cellPodLeft", cellPodLeft), ("_cellPodRight", cellPodRight));
            AssignArray(growth, "_cellPodBars", cellPodBars);
            var moteSystem = Particles("LampMotes", headlamp.transform, material);
            var lampMotes = moteSystem.gameObject.AddComponent<RoverLampMotes>();
            var nozzleDevice = visual.gameObject.AddComponent<CutterNozzleDevice>();
            var nozzleBlades = new Transform[RoverModelNodes.NozzleBladeCount];
            for (int i = 0; i < nozzleBlades.Length; i++)
            {
                Transform blade = Shape(PrimitiveType.Cube, cutterOrigin, Vector3.zero,
                    new Vector3(0.028f, 0.016f, 0.004f), material);
                blade.name = RoverModelNodes.NozzleBlade(i);
                nozzleBlades[i] = blade;
            }

            Transform nozzleCore = Shape(PrimitiveType.Quad, cutterOrigin, Vector3.zero, Vector3.one * 0.012f, material);
            nozzleCore.name = RoverModelNodes.NozzleHotCore;
            var nozzleSpillLight = Node(RoverModelNodes.NozzleSpillLight, cutterOrigin, Vector3.zero).gameObject
                .AddComponent<Light>();
            nozzleSpillLight.shadows = LightShadows.None;

            Transform fxHost = Node("WheelFx", root.transform, Vector3.zero);
            var fx = fxHost.gameObject.AddComponent<RoverWheelFx>();

            Assign(rig, ("_tuning", rigTuning), ("_chassis", chassis), ("_antenna", antenna));
            Assign(roadLight, ("_tuning", rigTuning), ("_headlamp", headlamp), ("_lampBody", bodyMesh));
            AssignArray(roadLight, "_podLenses",
                new Object[] { lensLeft.GetComponent<MeshRenderer>(), lensRight.GetComponent<MeshRenderer>() });
            SetInt(roadLight, "_lampGlassSlot", LampGlassSlot);
            AssignArray(rig, "_wheels", wheels);
            AssignArray(rig, "_arms", arms);
            AssignArray(rig, "_shocks", shocks);
            Assign(nozzleDevice, ("_tuning", fxTuning), ("_core", nozzleCore),
                ("_coreRenderer", nozzleCore.GetComponent<MeshRenderer>()), ("_spillLight", nozzleSpillLight));
            AssignArray(nozzleDevice, "_blades", nozzleBlades);
            Assign(fx, ("_tuning", fxTuning),
                ("_frontSocketLeft", (Transform)wheels[0]),
                ("_frontSocketRight", (Transform)wheels[1]),
                ("_dustSocketLeft", dustLeft),
                ("_dustSocketRight", dustRight),
                ("_trackFrontLeft", Track("TrackFrontLeft", fxHost, material)),
                ("_trackFrontRight", Track("TrackFrontRight", fxHost, material)),
                ("_trackLeft", Track("TrackLeft", fxHost, material)),
                ("_trackRight", Track("TrackRight", fxHost, material)),
                ("_dustLeft", Particles("DustLeft", fxHost, material)),
                ("_dustRight", Particles("DustRight", fxHost, material)),
                ("_landingDust", Particles("LandingDust", fxHost, material)));
            Assign(lampMotes, ("_tuning", fxTuning), ("_motes", moteSystem), ("_headlamp", headlamp));
            Assign(flank, ("_tuning", rigTuning));
            var portraitCamera = Node("PortraitCamera", visual, Vector3.zero).gameObject.AddComponent<Camera>();
            portraitCamera.enabled = false;
            var portrait = visual.gameObject.AddComponent<RoverPortrait>();
            var portraitKey = Node("PortraitKey", visual, Vector3.zero).gameObject.AddComponent<Light>();
            var portraitRim = Node("PortraitRim", visual, Vector3.zero).gameObject.AddComponent<Light>();
            Assign(portrait, ("_tuning", rigTuning), ("_camera", portraitCamera), ("_key", portraitKey),
                ("_rim", portraitRim));
            Assign(controller, ("_portrait", portrait));
            AssignArray(flank, "_lamps", flankLamps);
            AssignArray(flank, "_glasses", flankGlasses);
            Assign(controller, ("_tuning", tuning), ("_body", rigidbody), ("_sphere", collider), ("_visualRig", rig),
                ("_wheelFx", fx), ("_lampMotes", lampMotes), ("_headlamp", roadLight),
                ("_flankLamps", flank), ("_growth", growth), ("_towCable", towCable), ("_nozzleDevice", nozzleDevice),
                ("_tetherOrigin", eyeOrigin), ("_cutterOrigin", cutterOrigin), ("_scanOrigin", eyeOrigin),
                ("_winchOrigin", winchOrigin), ("_scoopOrigin", scoopOrigin), ("_cargoSocket", cargo));
            Transform hatch = Node(RoverModelNodes.CargoHatch, model, new Vector3(0f, HatchY, HatchZ));
            Node(RoverModelNodes.UpperCanopy, hatch, Vector3.zero);
            Node(RoverModelNodes.LowerChute, hatch, new Vector3(0f, -0.3f, 0f));
            var crates = new Object[HatchCrateCount];
            for (int i = 0; i < crates.Length; i++)
            {
                crates[i] = Node(RoverModelNodes.HatchCrate(i), hatch, Vector3.zero).gameObject;
            }

            Assign(controller, ("_cargoHatch", hatch));
            AssignArray(controller, "_hatchCrates", crates);
            var conduits = new Object[]
            {
                Shape(PrimitiveType.Cube, model, new Vector3(0f, CellY, 0f), new Vector3(0.04f, 0.02f, 0.8f), material)
                    .GetComponent<MeshRenderer>(),
                Shape(PrimitiveType.Cube, neck, Vector3.zero, new Vector3(0.02f, 0.3f, 0.02f), material)
                    .GetComponent<MeshRenderer>(),
            };
            var sleeves = new Object[2 * RoverModelNodes.ChargeStages];
            for (int i = 0; i < sleeves.Length; i++)
            {
                int stage = i / 2 + 1;
                bool left = i % 2 == 0;
                Transform stageCell = Node(RoverModelNodes.StageCell(stage, left), model,
                    new Vector3(left ? -CellX : CellX, CellY, (stage - 2) * CellSpacing));
                Transform sleeve = Shape(PrimitiveType.Cylinder, stageCell, Vector3.zero, Vector3.one * 0.06f, material);
                sleeve.name = RoverModelNodes.StageGlow(stage, left);
                sleeves[i] = sleeve.GetComponent<MeshRenderer>();
            }

            Transform lockGlow = Shape(PrimitiveType.Cylinder, eye, Vector3.zero, new Vector3(1.1f, 0.05f, 1.1f),
                material);
            lockGlow.name = RoverModelNodes.LockGlow;
            Transform lens = Node(RoverModelNodes.LensSocket, eye, Vector3.zero);
            Transform lensGlass = Shape(PrimitiveType.Sphere, lens, Vector3.zero, Vector3.one * 0.5f, material);
            lensGlass.name = "Glass";
            AssignArray(controller, "_conduitGlows", conduits);
            AssignArray(controller, "_stageSleeves", sleeves);
            Assign(controller, ("_lockGlow", lockGlow.GetComponent<MeshRenderer>()), ("_focusLens", lens.gameObject),
                ("_focusLensGlass", lensGlass.GetComponent<MeshRenderer>()));
            var screenMesh = new Mesh
            {
                name = "TestScreen",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            screenMesh.RecalculateNormals();
            screenMesh.RecalculateBounds();
            screen.GetComponent<MeshFilter>().sharedMesh = screenMesh;
            var screenCollider = screen.gameObject.AddComponent<MeshCollider>();
            screenCollider.sharedMesh = screenMesh;
            var faceDisplay = screen.gameObject.AddComponent<FaceDisplay>();
            Assign(faceDisplay, ("_tuning", faceTuning), ("_screenRenderer", screen.GetComponent<MeshRenderer>()));
            var faceDirector = screen.gameObject.AddComponent<RoverFaceDirector>();
            Assign(faceDirector,
                ("_tuning", faceDirectorTuning),
                ("_display", faceDisplay),
                ("_head", head),
                ("_rearVentStrip", rearVentStrip),
                ("_rearVentRenderer", rearVentStrip.GetComponent<MeshRenderer>()));

            Assign(body, ("_tuning", characterTuning), ("_rover", controller), ("_rig", rig), ("_neck", neck),
                ("_neckLift", neckLift), ("_head", head), ("_solarWing", wing),
                ("_cutterTurret", cutterTurret), ("_cutterHead", cutterHead),
                ("_scoopArms", scoopArms), ("_scoopBucket", scoopBucket), ("_winchSpool", winchSpool),
                ("_eyeRenderer", eye.GetComponent<MeshRenderer>()),
                ("_antennaTipRenderer", tip.GetComponent<MeshRenderer>()),
                ("_eyeLight", eyeLight),
                ("_scanFan", Shape(PrimitiveType.Cube, eye, Vector3.forward, new Vector3(0.01f, 0.01f, 1f), material)
                    .GetComponent<MeshRenderer>()),
                ("_faceDirector", faceDirector));
            Assign(controller, ("_bodyLanguage", body), ("_screenCollider", screenCollider));

            GameObject cameraRoot = BuildCamera(cameraTuning, out RoverCameraRig cameraRig, out Camera camera);

            InputActionAsset actions = TestControls.Create();
            var input = new InputReader(actions);
            input.Enable();
            cameraRig.CursorLockOverride = true;
            var rover = new TestRover(root, cameraRoot, actions, input, sphereMaterial, screenMesh, tunings)
            {
                Controller = controller,
                Rig = rig,
                BodyLanguage = body,
                FaceDisplay = faceDisplay,
                FaceDirector = faceDirector,
                CameraRig = cameraRig,
                Camera = camera,
                Chassis = chassis,
                Neck = neck,
                NeckLift = neckLift,
                Head = head,
                SolarWing = wing,
                EyeLight = eyeLight,
                LampMotes = lampMotes,
                Headlamp = headlamp,
                RoadLight = roadLight,
            };

            var context = new GameContext(new EventBus(), input);
            context.Register(world.Terrain);
            context.Register<IWorldLayout>(rover.Layout);
            context.Register<IFriendRoster>(rover.Friends);
            context.Register<ICutter>(rover.Cutter);
            context.Register<IWorldAnchors>(rover.Anchors);
            context.Register<ISaveService>(rover.Save);
            rover.Context = context;
            prepare?.Invoke(rover);
            controller.Initialize(context);
            controller.SetDriveSource(rover.Drive);
            body.Initialize(context);
            cameraRig.Initialize(context);
            return rover;
        }

        /// <summary>Puts the Cinemachine brains and cameras of a rig on the rover test channel.</summary>
        public static void IsolateCamera(GameObject cameraRig)
        {
            foreach (CinemachineBrain brain in cameraRig.GetComponentsInChildren<CinemachineBrain>(true))
            {
                brain.ChannelMask = TestChannel;
            }

            foreach (CinemachineCamera virtualCamera in cameraRig.GetComponentsInChildren<CinemachineCamera>(true))
            {
                virtualCamera.OutputChannel = TestChannel;
            }
        }

        private static GameObject BuildCamera(RoverCameraTuning tuning, out RoverCameraRig rig, out Camera camera)
        {
            var root = new GameObject("TestCameraRig");
            rig = root.AddComponent<RoverCameraRig>();
            Transform target = Node("FollowTarget", root.transform, Vector3.zero);
            Transform cameraHost = Node("MainCamera", root.transform, Vector3.zero);
            camera = cameraHost.gameObject.AddComponent<Camera>();
            cameraHost.gameObject.AddComponent<CinemachineBrain>();
            Transform drone = Node("DroneCamera", root.transform, Vector3.zero);
            var virtualCamera = drone.gameObject.AddComponent<CinemachineCamera>();
            var orbit = drone.gameObject.AddComponent<CinemachineOrbitalFollow>();
            var composer = drone.gameObject.AddComponent<CinemachineRotationComposer>();
            var bump = drone.gameObject.AddComponent<RoverCameraBump>();
            var decollider = drone.gameObject.AddComponent<CinemachineDecollider>();
            var deoccluder = drone.gameObject.AddComponent<CinemachineDeoccluder>();
            Assign(rig, ("_tuning", tuning), ("_target", target), ("_viewCamera", camera), ("_camera", virtualCamera),
                ("_orbit", orbit),
                ("_composer", composer), ("_decollider", decollider), ("_deoccluder", deoccluder), ("_bump", bump));
            IsolateCamera(root);
            return root;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        private static Transform Shape(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = scale;
            return shape.transform;
        }

        private static RoverTrackRenderer Track(string name, Transform parent, Material material)
        {
            Transform track = Node(name, parent, Vector3.zero);
            track.gameObject.AddComponent<MeshFilter>();
            track.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return track.gameObject.AddComponent<RoverTrackRenderer>();
        }

        private static ParticleSystem Particles(string name, Transform parent, Material material)
        {
            var system = Node(name, parent, Vector3.zero).gameObject.AddComponent<ParticleSystem>();
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.MainModule main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            return system;
        }

        private static void Assign(Object target, params (string Field, Object Value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, Object value) in fields)
            {
                SerializedProperty property = serialized.FindProperty(field)
                    ?? throw new InvalidOperationException($"{target.GetType().Name} has no field {field}.");
                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"{target.GetType().Name} has no field {field}.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Destroys everything immediately, so the next test never meets this rover or its camera.</summary>
        public void Dispose()
        {
            Controller.SetDriveSource(null);
            _input.Dispose();
            Object.DestroyImmediate(Root);
            Object.DestroyImmediate(_cameraRoot);
            Object.DestroyImmediate(_sphereMaterial);
            Object.DestroyImmediate(_screenMesh);
            Object.DestroyImmediate(_actions);
            foreach (ScriptableObject tuning in _tunings)
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
