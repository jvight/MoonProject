using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using MoonProject.Core;
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
        private const float HalfTrack = 0.7f;
        private const float AxleSpacing = 0.8f;

        /// <summary>HoverCoils contract dimensions (art's CoilSocket height, spring spacing and length).</summary>
        private const float CoilSocketHeight = 0.28f;
        private const float CoilHalfTrack = 0.19f;
        private const float CoilHalfBase = 0.3f;
        private const float CoilLength = 0.1f;

        /// <summary>Kit stand-in dimensions (art's lamp bar, capacitor drums and rack, and their sockets).</summary>
        private const float LampSpacing = 0.27f;
        private const float DrumSocketX = 0.47f;
        private const float DrumSocketY = 0.87f;
        private const float DrumSocketZ = -0.12f;
        private const float SeatHeight = 0.045f;
        private const float SeatBack = -0.26f;

        /// <summary>Cinemachine channel for rover tests: their brains and cameras only see each other.</summary>
        private const OutputChannels TestChannel = OutputChannels.Channel15;

        private readonly GameObject _cameraRoot;
        private readonly InputActionAsset _actions;
        private readonly InputReader _input;
        private readonly PhysicsMaterial _sphereMaterial;
        private readonly ScriptableObject[] _tunings;

        private TestRover(GameObject root, GameObject cameraRoot, InputActionAsset actions, InputReader input,
            PhysicsMaterial sphereMaterial, ScriptableObject[] tunings)
        {
            Root = root;
            _cameraRoot = cameraRoot;
            _actions = actions;
            _input = input;
            _sphereMaterial = sphereMaterial;
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

        public Transform Head { get; private set; }

        public Transform SolarWing { get; private set; }

        public Transform Eyelid { get; private set; }

        public Light EyeLight { get; private set; }

        public RoverHoverCoils HoverCoils { get; private set; }

        public RoverLampMotes LampMotes { get; private set; }

        public Light Headlamp { get; private set; }

        /// <summary>The stand-in HoverCoils mount under CoilSocket.</summary>
        public Transform CoilMount { get; private set; }

        public Transform[] Coils { get; private set; }

        public Renderer[] CoilGlows { get; private set; }

        public Light CoilLight { get; private set; }

        public RoverKit Kit { get; private set; }

        /// <summary>The stand-in Kit_LampBar, capacitor drums (L, R), Kit_CargoRack and its RelicSeat.</summary>
        public Transform LampBar { get; private set; }

        public Transform[] Drums { get; private set; }

        public Renderer[] DrumGlows { get; private set; }

        public Renderer[] Lamps { get; private set; }

        public Transform CargoRack { get; private set; }

        public Transform RelicSeat { get; private set; }

        /// <summary>The gift nodes: SolarWing/CellFilled, Body/Decal07Fresh, Antenna/Pennant.</summary>
        public Transform CellFilled { get; private set; }

        public Transform FreshSerial { get; private set; }

        public Transform Pennant { get; private set; }

        /// <summary>Tilly and Bell, dormant until a test wakes them.</summary>
        public TestFriendRoster Friends { get; } = new TestFriendRoster();

        /// <summary>The world's landmarks (a test may move the base to bring 07 home).</summary>
        public TestWorldLayout Layout { get; } = new TestWorldLayout();

        public RoverTuning Tuning => (RoverTuning)_tunings[0];

        public RoverRigTuning RigTuning => (RoverRigTuning)_tunings[1];

        public RoverFxTuning FxTuning => (RoverFxTuning)_tunings[2];

        public RoverCameraTuning CameraTuning => (RoverCameraTuning)_tunings[3];

        public RoverCharacterTuning CharacterTuning => (RoverCharacterTuning)_tunings[4];

        /// <summary>
        /// Builds, wires and initialises a rover at <paramref name="position"/>, yaw in degrees. It starts awake
        /// unless <paramref name="asleep"/> asks for the game's first-boot wake-up. <paramref name="wideShotDelay"/>
        /// (seconds) shortens the rest before the wide shot opens, so sessions need not wait the shipped delay.
        /// </summary>
        public static TestRover Spawn(TestWorld world, Vector3 position, float yaw, bool asleep = false,
            float wideShotDelay = 0f)
        {
            var tuning = ScriptableObject.CreateInstance<RoverTuning>();
            var rigTuning = ScriptableObject.CreateInstance<RoverRigTuning>();
            var fxTuning = ScriptableObject.CreateInstance<RoverFxTuning>();
            var cameraTuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            var characterTuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            var tunings = new ScriptableObject[] { tuning, rigTuning, fxTuning, cameraTuning, characterTuning };
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
            Shape(PrimitiveType.Cube, bodyNode, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 0.55f, 1.9f), material);
            var wheels = new Object[RoverModelNodes.WheelCount];
            for (int i = 0; i < wheels.Length; i++)
            {
                float x = i % 2 == 0 ? -HalfTrack : HalfTrack;
                float z = AxleSpacing * (1 - RoverModelNodes.WheelRow(i));
                Transform wheel = Node(RoverModelNodes.Wheel(i), model, new Vector3(x, WheelRadius, z));
                Transform tyre = Shape(PrimitiveType.Cylinder, wheel, Vector3.zero,
                    new Vector3(2f * WheelRadius, 0.1f, 2f * WheelRadius), material);
                tyre.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheels[i] = wheel;
            }

            Transform bogieLeft = Node(RoverModelNodes.BogieLeft, model, new Vector3(-0.82f, 0.45f, 0.2f));
            Transform bogieRight = Node(RoverModelNodes.BogieRight, model, new Vector3(0.82f, 0.45f, 0.2f));
            Transform neck = Node(RoverModelNodes.Neck, model, new Vector3(0f, 1.05f, 0.55f));
            Transform head = Node(RoverModelNodes.Head, neck, new Vector3(0f, 0.4f, 0f));
            Transform eye = Shape(PrimitiveType.Sphere, head, new Vector3(0f, 0f, 0.18f), Vector3.one * 0.22f,
                material);
            eye.name = RoverModelNodes.Eye;
            Transform tetherOrigin = Node(RoverModelNodes.TetherOrigin, eye, Vector3.zero);
            Transform eyelid = Node(RoverModelNodes.Eyelid, head, new Vector3(0f, 0f, 0.15f));
            Transform wing = Node(RoverModelNodes.SolarWing, model, new Vector3(0f, 1.08f, -0.5f));
            Transform antenna = Node(RoverModelNodes.Antenna, model, new Vector3(0.45f, 1.05f, -0.75f));
            Transform tip = Shape(PrimitiveType.Sphere, antenna, new Vector3(0f, 0.9f, 0f), Vector3.one * 0.06f,
                material);
            tip.name = RoverModelNodes.AntennaTip;
            Transform lampSocket = Node(RoverModelNodes.HeadlampSocket, model, new Vector3(0f, 0.55f, 1f));
            Transform cargo = Node(RoverModelNodes.CargoSocket, model, new Vector3(0f, 1.05f, -0.4f));
            Transform dustLeft = Node(RoverModelNodes.DustSocketLeft, model, new Vector3(-HalfTrack, 0f, -AxleSpacing));
            Transform dustRight =
                Node(RoverModelNodes.DustSocketRight, model, new Vector3(HalfTrack, 0f, -AxleSpacing));

            var headlamp = Node("Headlamp", lampSocket, Vector3.zero).gameObject.AddComponent<Light>();
            var eyeLight = Node("EyeGlow", eye, Vector3.zero).gameObject.AddComponent<Light>();

            Transform coilSocket = Node(RoverModelNodes.CoilSocket, model, Vector3.up * CoilSocketHeight);
            Transform coilMount = Node("HoverCoils", coilSocket, Vector3.zero);
            var coils = new Transform[RoverModelNodes.CoilCount];
            var coilGlows = new Renderer[RoverModelNodes.CoilCount];
            for (int i = 0; i < coils.Length; i++)
            {
                var top = new Vector3(i % 2 == 0 ? -CoilHalfTrack : CoilHalfTrack, 0f,
                    i < 2 ? CoilHalfBase : -CoilHalfBase);
                coils[i] = Node(RoverModelNodes.Coil(i), coilMount, top);
                Shape(PrimitiveType.Cylinder, coils[i], Vector3.down * (CoilLength * 0.5f),
                    new Vector3(0.12f, CoilLength * 0.5f, 0.12f), material);
                Transform ring = Shape(PrimitiveType.Sphere, coils[i], Vector3.down * CoilLength,
                    new Vector3(0.15f, 0.02f, 0.15f), material);
                ring.name = RoverModelNodes.CoilGlow(i);
                coilGlows[i] = ring.GetComponent<MeshRenderer>();
            }

            var coilLight = Node("CoilGlow", coilSocket, Vector3.zero).gameObject.AddComponent<Light>();

            Transform lampBar = Node("Kit_LampBar", lampSocket, Vector3.zero);
            Shape(PrimitiveType.Cube, lampBar, new Vector3(0f, 0.06f, 0.03f), new Vector3(0.9f, 0.08f, 0.06f),
                material);
            var lamps = new Renderer[RoverModelNodes.KitLampCount];
            for (int i = 0; i < lamps.Length; i++)
            {
                var at = new Vector3((i - 1) * LampSpacing, 0.06f, 0.07f);
                Transform glass = Shape(PrimitiveType.Sphere, lampBar, at, Vector3.one * 0.1f, material);
                glass.name = RoverModelNodes.KitLamp(i);
                lamps[i] = glass.GetComponent<MeshRenderer>();
            }

            var drums = new Transform[2];
            var drumGlows = new Renderer[2];
            for (int i = 0; i < drums.Length; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Transform socket = Node(i == 0 ? RoverModelNodes.DrumSocketLeft : RoverModelNodes.DrumSocketRight,
                    model, new Vector3(side * DrumSocketX, DrumSocketY, DrumSocketZ));
                socket.localRotation = Quaternion.Euler(0f, i == 0 ? 180f : 0f, 0f);
                drums[i] = Node("Kit_CapacitorDrum", socket, Vector3.zero);
                Transform band = Shape(PrimitiveType.Cylinder, drums[i], new Vector3(0.08f, 0f, 0f),
                    new Vector3(0.16f, 0.1f, 0.16f), material);
                band.name = RoverModelNodes.DrumGlow;
                drumGlows[i] = band.GetComponent<MeshRenderer>();
            }

            Transform rack = Node("Kit_CargoRack", cargo, Vector3.zero);
            Shape(PrimitiveType.Cube, rack, new Vector3(0f, 0.02f, SeatBack), new Vector3(0.7f, 0.04f, 0.5f), material);
            Transform seat = Node(RoverModelNodes.RelicSeat, rack, new Vector3(0f, SeatHeight, SeatBack));
            Transform cell = Shape(PrimitiveType.Cube, wing, new Vector3(0.1f, 0.02f, -0.1f),
                new Vector3(0.15f, 0.01f, 0.15f), material);
            cell.name = RoverModelNodes.CellFilled;
            Transform serial = Shape(PrimitiveType.Cube, bodyNode, new Vector3(0.66f, 0.8f, 0.17f),
                new Vector3(0.01f, 0.08f, 0.21f), material);
            serial.name = RoverModelNodes.Decal07Fresh;
            Transform pennant = Shape(PrimitiveType.Cube, antenna, new Vector3(0f, 0.6f, -0.05f),
                new Vector3(0.01f, 0.06f, 0.1f), material);
            pennant.name = RoverModelNodes.Pennant;
            var kit = visual.gameObject.AddComponent<RoverKit>();
            var moteSystem = Particles("LampMotes", headlamp.transform, material);
            var lampMotes = moteSystem.gameObject.AddComponent<RoverLampMotes>();
            var hoverCoils = visual.gameObject.AddComponent<RoverHoverCoils>();

            Transform fxHost = Node("WheelFx", root.transform, Vector3.zero);
            var fx = fxHost.gameObject.AddComponent<RoverWheelFx>();

            Assign(rig, ("_tuning", rigTuning), ("_chassis", chassis), ("_bogieLeft", bogieLeft),
                ("_bogieRight", bogieRight), ("_antenna", antenna));
            Assign(kit, ("_tuning", rigTuning), ("_headlamp", headlamp), ("_lampBar", lampBar), ("_cargoRack", rack),
                ("_relicSeat", seat), ("_solarCell", cell), ("_freshSerial", serial), ("_pennant", pennant));
            AssignArray(kit, "_lamps", lamps);
            AssignArray(kit, "_drums", drums);
            AssignArray(kit, "_drumGlows", drumGlows);
            AssignArray(rig, "_wheels", wheels);
            Assign(hoverCoils, ("_tuning", rigTuning), ("_mount", coilMount), ("_light", coilLight));
            AssignArray(hoverCoils, "_coils", coils);
            AssignArray(hoverCoils, "_glows", coilGlows);
            Assign(fx, ("_tuning", fxTuning), ("_dustSocketLeft", dustLeft), ("_dustSocketRight", dustRight),
                ("_trackLeft", Track("TrackLeft", fxHost, material)),
                ("_trackRight", Track("TrackRight", fxHost, material)),
                ("_dustLeft", Particles("DustLeft", fxHost, material)),
                ("_dustRight", Particles("DustRight", fxHost, material)),
                ("_landingDust", Particles("LandingDust", fxHost, material)));
            Assign(lampMotes, ("_tuning", fxTuning), ("_motes", moteSystem), ("_headlamp", headlamp));
            Assign(controller, ("_tuning", tuning), ("_body", rigidbody), ("_sphere", collider), ("_visualRig", rig),
                ("_wheelFx", fx), ("_hoverCoils", hoverCoils), ("_lampMotes", lampMotes), ("_kit", kit),
                ("_tetherOrigin", tetherOrigin), ("_cargoSocket", cargo));
            Assign(body, ("_tuning", characterTuning), ("_rover", controller), ("_rig", rig), ("_neck", neck),
                ("_head", head), ("_eyelid", eyelid), ("_solarWing", wing),
                ("_eyeRenderer", eye.GetComponent<MeshRenderer>()),
                ("_antennaTipRenderer", tip.GetComponent<MeshRenderer>()),
                ("_eyeLight", eyeLight));

            GameObject cameraRoot = BuildCamera(cameraTuning, out RoverCameraRig cameraRig, out Camera camera);

            InputActionAsset actions = TestControls.Create();
            var input = new InputReader(actions);
            input.Enable();
            var rover = new TestRover(root, cameraRoot, actions, input, sphereMaterial, tunings)
            {
                Controller = controller,
                Rig = rig,
                BodyLanguage = body,
                CameraRig = cameraRig,
                Camera = camera,
                Chassis = chassis,
                Neck = neck,
                Head = head,
                SolarWing = wing,
                Eyelid = eyelid,
                EyeLight = eyeLight,
                HoverCoils = hoverCoils,
                LampMotes = lampMotes,
                Headlamp = headlamp,
                CoilMount = coilMount,
                Coils = coils,
                CoilGlows = coilGlows,
                CoilLight = coilLight,
                Kit = kit,
                LampBar = lampBar,
                Lamps = lamps,
                Drums = drums,
                DrumGlows = drumGlows,
                CargoRack = rack,
                RelicSeat = seat,
                CellFilled = cell,
                FreshSerial = serial,
                Pennant = pennant,
            };

            var context = new GameContext(new EventBus(), input);
            context.Register(world.Terrain);
            context.Register<IWorldLayout>(rover.Layout);
            context.Register<IFriendRoster>(rover.Friends);
            rover.Context = context;
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
            Object.DestroyImmediate(_actions);
            foreach (ScriptableObject tuning in _tunings)
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
