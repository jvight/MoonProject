using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The real built Rover.prefab (wrapping the art box's RoverModel) and camera rig: they initialise without errors,
    /// register their Core contracts, only the headlamp may cast shadows, and 07 drives. Under a single directional
    /// light at the world's Earthlight angle it captures frames with and without the head, to tell a second light's
    /// shadow apart from the head's own shadow.
    /// </summary>
    public sealed class RoverPrefabTests : InputTestFixture
    {
        private const string RoverPrefab = "Assets/_Project/Generated/Rover/Rover.prefab";
        private const string CameraRigPrefab = "Assets/_Project/Generated/Rover/RoverCameraRig.prefab";
        private const float EarthlightElevation = 24f;
        private const float EarthlightBearing = 265f;
        private const int MaxInputAttempts = 5;

        private static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-metrics"));

        private Gamepad _pad;
        private InputActionAsset _actions;
        private InputReader _input;
        private TestWorld _world;
        private GameObject _rover;
        private GameObject _cameraRig;
        private Vector3 _savedGravity;

        public override void Setup()
        {
            base.Setup();
            _pad = InputSystem.AddDevice<Gamepad>();
            _actions = TestControls.Create();
            _savedGravity = Physics.gravity;
            Physics.gravity = new Vector3(0f, -1.62f, 0f);
        }

        public override void TearDown()
        {
            Physics.gravity = _savedGravity;
            Object.Destroy(_rover);
            Object.Destroy(_cameraRig);
            _input?.Dispose();
            _world?.Dispose();
            Object.Destroy(_actions);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator BuiltPrefabs_InitialiseRegisterAndDrive()
        {
            _world = new TestWorld();
            Light sun = _world.Sun;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.LookRotation(-EarthlightSource());

            var roverAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefab);
            var cameraAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab);
            Assert.IsNotNull(roverAsset, $"{RoverPrefab} missing: run the Rover builders.");
            Assert.IsNotNull(cameraAsset, $"{CameraRigPrefab} missing: run the Rover builders.");

            _rover = Object.Instantiate(roverAsset, new Vector3(0f, 0f, -300f), Quaternion.identity);
            _cameraRig = Object.Instantiate(cameraAsset);
            foreach (Light light in _rover.GetComponentsInChildren<Light>(true))
            {
                bool headlamp = light.type == LightType.Spot;
                Assert.IsTrue(headlamp || light.shadows == LightShadows.None,
                    $"Rover light '{light.name}' casts shadows; only the world light and the headlamp may.");
            }

            _input = new InputReader(_actions);
            _input.Enable();
            var context = new GameContext(new EventBus(), _input);
            context.Register(_world.Terrain);
            context.Register<IWorldLayout>(new TestWorldLayout());
            var controller = _rover.GetComponent<RoverController>();
            controller.Initialize(context);
            _rover.GetComponent<RoverBodyLanguage>().Initialize(context);
            var cameraRig = _cameraRig.GetComponent<RoverCameraRig>();
            cameraRig.Initialize(context);

            Assert.IsTrue(controller.enabled && cameraRig.enabled, "Wiring validation failed (see the log).");
            Assert.AreSame(controller, context.Get<IRoverState>());
            Assert.AreSame(controller, context.Get<IRoverRig>());
            Assert.IsNotNull(context.Get<IRoverRig>().TetherOrigin);
            Assert.AreSame(cameraRig.Camera, context.Get<IViewCamera>().Camera);

            yield return Settle(1.5f);
            Camera camera = cameraRig.Camera;
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "10-prefab-sun-shadow.png"));
            SetHeadVisible(false);
            yield return null;
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "11-prefab-sun-shadow-no-head.png"));
            SetHeadVisible(true);
            foreach (Light light in _rover.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }

            yield return null;
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "12-prefab-no-rover-lights.png"));
            foreach (Light light in _rover.GetComponentsInChildren<Light>(true))
            {
                light.enabled = true;
            }

            yield return Drive(new Vector2(0.3f, 1f));
            yield return Settle(4f);
            Assert.Greater(controller.Speed, 4f, "The built rover drives.");
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "13-prefab-driving.png"));
            yield return Drive(Vector2.zero);
        }

        private static Vector3 EarthlightSource()
        {
            float elevation = EarthlightElevation * Mathf.Deg2Rad;
            float bearing = EarthlightBearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(bearing) * Mathf.Cos(elevation), Mathf.Sin(elevation),
                Mathf.Cos(bearing) * Mathf.Cos(elevation));
        }

        private void SetHeadVisible(bool visible)
        {
            Transform head = Find(_rover.transform, RoverModelNodes.Head);
            Assert.IsNotNull(head, "RoverModel has no Head.");
            foreach (Renderer renderer in head.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }
        }

        private static Transform Find(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = child.name == name ? child : Find(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static IEnumerator Settle(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IEnumerator Drive(Vector2 stick)
        {
            yield return null;
            Vector2 expected = Vector2.ClampMagnitude(stick, 1f);
            for (int attempt = 0; attempt < MaxInputAttempts; attempt++)
            {
                Set(_pad.leftStick, stick);
                yield return null;
                if ((_pad.leftStick.ReadValue() - expected).sqrMagnitude < 1e-4f)
                {
                    yield break;
                }
            }

            Assert.Fail($"The virtual gamepad never reported stick {stick}.");
        }
    }
}
