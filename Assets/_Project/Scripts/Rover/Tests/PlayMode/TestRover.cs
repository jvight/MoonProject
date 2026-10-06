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
    /// rig. Nothing here ships: the game's prefab always wraps the art model.
    /// </summary>
    public sealed class TestRover : IDisposable
    {
        private const float WheelRadius = 0.35f;
        private const float HalfTrack = 0.7f;
        private const float AxleSpacing = 0.8f;

        private readonly GameObject _cameraRoot;
        private readonly InputReader _input;
        private readonly ScriptableObject[] _tunings;

        private TestRover(GameObject root, GameObject cameraRoot, InputReader input, ScriptableObject[] tunings)
        {
            Root = root;
            _cameraRoot = cameraRoot;
            _input = input;
            _tunings = tunings;
        }

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

        public RoverTuning Tuning => (RoverTuning)_tunings[0];

        /// <summary>Builds, wires and initialises a rover at <paramref name="position"/>, yaw in degrees.</summary>
        public static TestRover Spawn(InputActionAsset actions, TestWorld world, Vector3 position, float yaw)
        {
            var tuning = ScriptableObject.CreateInstance<RoverTuning>();
            var rigTuning = ScriptableObject.CreateInstance<RoverRigTuning>();
            var fxTuning = ScriptableObject.CreateInstance<RoverFxTuning>();
            var cameraTuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            var characterTuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            var tunings = new ScriptableObject[] { tuning, rigTuning, fxTuning, cameraTuning, characterTuning };

            var root = new GameObject("TestRover");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var controller = root.AddComponent<RoverController>();
            var body = root.AddComponent<RoverBodyLanguage>();

            var sphere = new GameObject("PhysicsSphere") { layer = Layers.Rover };
            sphere.transform.SetParent(root.transform, false);
            var rigidbody = sphere.AddComponent<Rigidbody>();
            var collider = sphere.AddComponent<SphereCollider>();
            collider.sharedMaterial = new PhysicsMaterial("Frictionless")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

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

            Transform fxHost = Node("WheelFx", root.transform, Vector3.zero);
            var fx = fxHost.gameObject.AddComponent<RoverWheelFx>();

            Assign(rig, ("_tuning", rigTuning), ("_chassis", chassis), ("_bogieLeft", bogieLeft),
                ("_bogieRight", bogieRight), ("_antenna", antenna), ("_headlamp", headlamp));
            AssignArray(rig, "_wheels", wheels);
            Assign(fx, ("_tuning", fxTuning), ("_dustSocketLeft", dustLeft), ("_dustSocketRight", dustRight),
                ("_trackLeft", Track("TrackLeft", fxHost, material)),
                ("_trackRight", Track("TrackRight", fxHost, material)),
                ("_dustLeft", Particles("DustLeft", fxHost, material)),
                ("_dustRight", Particles("DustRight", fxHost, material)),
                ("_landingDust", Particles("LandingDust", fxHost, material)));
            Assign(controller, ("_tuning", tuning), ("_body", rigidbody), ("_sphere", collider), ("_visualRig", rig),
                ("_wheelFx", fx), ("_tetherOrigin", tetherOrigin), ("_cargoSocket", cargo));
            Assign(body, ("_tuning", characterTuning), ("_rover", controller), ("_rig", rig), ("_neck", neck),
                ("_head", head), ("_eyelid", eyelid), ("_solarWing", wing),
                ("_eyeRenderer", eye.GetComponent<MeshRenderer>()),
                ("_antennaTipRenderer", tip.GetComponent<MeshRenderer>()),
                ("_eyeLight", eyeLight));

            GameObject cameraRoot = BuildCamera(cameraTuning, out RoverCameraRig cameraRig, out Camera camera);

            var input = new InputReader(actions);
            input.Enable();
            var rover = new TestRover(root, cameraRoot, input, tunings)
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
            };

            var context = new GameContext(new EventBus(), input);
            context.Register(world.Terrain);
            context.Register<IWorldLayout>(new TestWorldLayout());
            rover.Context = context;
            controller.Initialize(context);
            body.Initialize(context);
            cameraRig.Initialize(context);
            return rover;
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

        public void Dispose()
        {
            _input.Dispose();
            Object.Destroy(Root);
            Object.Destroy(_cameraRoot);
            foreach (ScriptableObject tuning in _tunings)
            {
                Object.Destroy(tuning);
            }
        }
    }
}
