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
    /// register their Core contracts (incl. ILookSettings), no rover light casts shadows, and 07 drives (scripted
    /// stick, no devices).
    /// Under a single directional
    /// light at the world's Earthlight angle it captures frames with and without the head, to tell a second light's
    /// shadow apart from the head's own shadow.
    /// </summary>
    public sealed class RoverPrefabTests : InputTestFixture
    {
        private const string RoverPrefab = "Assets/_Project/Generated/Rover/Rover.prefab";
        private const string CameraRigPrefab = "Assets/_Project/Generated/Rover/RoverCameraRig.prefab";
        private const float EarthlightElevation = 24f;
        private const float EarthlightBearing = 265f;

        /// <summary>Design ruling 8: the opening shot pitches down at most ~6 degrees.</summary>
        private const float MaxOpeningPitch = 6f;

        private static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-metrics"));

        private readonly ScriptedDrive _drive = new ScriptedDrive();
        private LunarTestPhysics _physics;
        private InputActionAsset _actions;
        private InputReader _input;
        private TestWorld _world;
        private GameObject _rover;
        private GameObject _cameraRig;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _actions = TestControls.Create();
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(_rover);
            Object.DestroyImmediate(_cameraRig);
            _input?.Dispose();
            _world?.Dispose();
            _world = null;
            Object.DestroyImmediate(_actions);
            _physics.Dispose();
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

            _rover = Object.Instantiate(roverAsset, TestWorld.Point(0f, -300f), Quaternion.identity);
            _cameraRig = Object.Instantiate(cameraAsset);
            TestRover.IsolateCamera(_cameraRig);
            foreach (Light light in _rover.GetComponentsInChildren<Light>(true))
            {
                Assert.AreEqual(LightShadows.None, light.shadows,
                    $"Rover light '{light.name}' casts shadows; only the world light may.");
            }

            _input = new InputReader(_actions);
            _input.Enable();
            var context = new GameContext(new EventBus(), _input);
            context.Register(_world.Terrain);
            context.Register<IWorldLayout>(new TestWorldLayout());
            var controller = _rover.GetComponent<RoverController>();
            controller.Initialize(context);
            controller.SetDriveSource(_drive);
            _rover.GetComponent<RoverBodyLanguage>().Initialize(context);
            var cameraRig = _cameraRig.GetComponent<RoverCameraRig>();
            cameraRig.Initialize(context);

            Assert.IsTrue(controller.enabled && cameraRig.enabled, "Wiring validation failed (see the log).");
            Assert.AreSame(controller, context.Get<IRoverState>());
            Assert.AreSame(controller, context.Get<IRoverRig>());
            Assert.IsNotNull(context.Get<IRoverRig>().TetherOrigin);
            Assert.AreSame(cameraRig.Camera, context.Get<IViewCamera>().Camera);
            Assert.AreSame(cameraRig, context.Get<ILookSettings>());

            yield return null;
            yield return null;
            Camera camera = cameraRig.Camera;
            float openingPitch = Mathf.Asin(-camera.transform.forward.y) * Mathf.Rad2Deg;
            Vector3 roverOnScreen = camera.WorldToViewportPoint(controller.Position);
            Debug.Log($"[rover-opening] camera pitch {openingPitch:0.0} deg down, 07 at viewport {roverOnScreen}");
            Assert.LessOrEqual(openingPitch, MaxOpeningPitch, "Opening shot looks across the basin (ruling 8).");
            Assert.That(roverOnScreen.x, Is.InRange(0.3f, 0.7f), "07 is in the opening frame.");
            Assert.That(roverOnScreen.y, Is.InRange(0.1f, 0.5f), "07 sits in the lower half of the opening frame.");
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "09-prefab-opening-shot.png"));

            yield return Settle(1.5f);
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

            _drive.Drive = new Vector2(0.3f, 1f);
            yield return Settle(4f);
            Assert.Greater(controller.Speed, 4f, "The built rover drives.");
            FrameCapture.SavePng(camera, 960, 540, Path.Combine(OutputFolder, "13-prefab-driving.png"));
            _drive.Drive = Vector2.zero;
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
    }
}
