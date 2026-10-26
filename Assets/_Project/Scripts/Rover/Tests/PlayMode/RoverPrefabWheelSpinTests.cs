using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The real built Rover.prefab's wheels turn with the ground (owner playtest f3d1ff265: "wheels spin much faster
    /// than 07 travels"): each wheel mesh's own radius, measured in the world, times its spin rate equals the travel its
    /// hub makes along its own heading, driven straight at three speeds, in reverse and round both ways. The numbers are logged.
    /// </summary>
    public sealed class RoverPrefabWheelSpinTests : InputTestFixture
    {
        private const string RoverPrefab = "Assets/_Project/Generated/Rover/Rover.prefab";
        private const string CameraRigPrefab = "Assets/_Project/Generated/Rover/RoverCameraRig.prefab";
        private const float SettleSeconds = 3f;
        private const float MeasureSeconds = 2f;
        private const float OpeningSeconds = 4f;

        /// <summary>The rim speed may differ from its hub's path speed by at most this share.</summary>
        private const float Tolerance = 0.03f;

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

        private static float SignedSpinDegrees(Quaternion previous, Quaternion current)
        {
            Quaternion delta = Quaternion.Inverse(previous) * current;
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return axis.x >= 0f ? angle : -angle;
        }

        private static Transform FindNode(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            return null;
        }

        [UnityTest]
        public IEnumerator TheBuiltPrefabsWheels_KeepPaceWithTheGround_AtThreeSpeeds()
        {
            _world = new TestWorld();
            var roverAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefab);
            var cameraAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefab);
            Assert.IsNotNull(roverAsset, $"{RoverPrefab} missing: run the Rover builders.");
            _rover = Object.Instantiate(roverAsset, TestWorld.Point(0f, -300f), Quaternion.identity);
            _cameraRig = Object.Instantiate(cameraAsset);
            TestRover.IsolateCamera(_cameraRig);
            _input = new InputReader(_actions);
            _input.Enable();
            var context = new GameContext(new EventBus(), _input);
            context.Register(_world.Terrain);
            context.Register<IWorldLayout>(new TestWorldLayout());
            context.Register<IFriendRoster>(new TestFriendRoster());
            context.Register<ICutter>(new TestCutter());
            context.Register<IWorldAnchors>(new TestAnchors());
            context.Register<ISaveService>(new TestSave());
            var controller = _rover.GetComponent<RoverController>();
            controller.Initialize(context);
            controller.SetDriveSource(_drive);
            _rover.GetComponent<RoverBodyLanguage>().Initialize(context);
            _cameraRig.GetComponent<RoverCameraRig>().Initialize(context);
            Assert.IsTrue(controller.enabled, "Wiring validation failed (see the log).");

            float opened = Time.time + OpeningSeconds;
            while (Time.time < opened)
            {
                yield return null;
            }

            var wheels = new Transform[RoverModelNodes.WheelCount];
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i] = FindNode(_rover.transform, RoverModelNodes.Wheel(i));
                Assert.IsNotNull(wheels[i], RoverModelNodes.Wheel(i));
                Debug.Log($"[rover-wheelspin] wheel {i}: visual radius {WheelKinematics.Radius(wheels[i]):0.000} m, "
                    + $"lossy scale {wheels[i].lossyScale}");
            }

            var visual = new float[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                visual[i] = WheelKinematics.Radius(wheels[i]);
            }

            RoverRigTuning rigTuning = controller.GetComponentInChildren<RoverVisualRig>(true).Tuning;
            var cases = new (string name, Vector2 stick)[]
            {
                ("straight 0.3", new Vector2(0f, 0.3f)),
                ("straight 0.6", new Vector2(0f, 0.6f)),
                ("straight 1.0", new Vector2(0f, 1f)),
                ("reverse 0.6", new Vector2(0f, -0.6f)),
                ("arc right 0.8", new Vector2(0.6f, 0.8f)),
                ("arc left 0.8", new Vector2(-0.6f, 0.8f)),
            };
            foreach ((string name, Vector2 stick) in cases)
            {
                _drive.Drive = stick;
                float settled = Time.time + SettleSeconds;
                while (Time.time < settled)
                {
                    yield return null;
                }

                var last = new Quaternion[wheels.Length];
                var lastHub = new Vector3[wheels.Length];
                var spin = new float[wheels.Length];
                var drawn = new float[wheels.Length];
                var rolled = new float[wheels.Length];
                for (int i = 0; i < wheels.Length; i++)
                {
                    last[i] = wheels[i].localRotation;
                    lastHub[i] = wheels[i].position;
                }

                float elapsed = 0f;
                while (elapsed < MeasureSeconds)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        spin[i] += SignedSpinDegrees(last[i], wheels[i].localRotation) * Mathf.Deg2Rad * visual[i];
                        // A wheel pays its hub's travel along its own heading, which is square to its axle (spin
                        // turns about the axle, so it cannot move it).
                        Vector3 heading = Vector3.Cross(wheels[i].right, controller.Rotation * Vector3.up).normalized;
                        float step = Vector3.Dot(wheels[i].position - lastHub[i], heading);
                        rolled[i] += step;
                        // The drawn turn is the true one eased under the soft ceiling, frame by frame.
                        float trueRate = step / visual[i] * Mathf.Rad2Deg / Time.deltaTime;
                        drawn[i] += WheelKinematics.SoftCap(trueRate, rigTuning.WheelSpinKnee, rigTuning.WheelSpinCeiling) * Time.deltaTime * Mathf.Deg2Rad * visual[i];
                        last[i] = wheels[i].localRotation;
                        lastHub[i] = wheels[i].position;
                    }
                }

                var line = new System.Text.StringBuilder($"[rover-wheelspin] {name}:");
                for (int i = 0; i < wheels.Length; i++)
                {
                    line.Append($" w{i} hub {rolled[i] / elapsed:0.00} rim {spin[i] / elapsed:0.00}");
                }

                Debug.Log(line.ToString());
                for (int i = 0; i < wheels.Length; i++)
                {
                    Assert.AreEqual(drawn[i] / elapsed, spin[i] / elapsed, Mathf.Abs(drawn[i] / elapsed) * Tolerance + 0.02f,
                        $"{name}: wheel {i}'s rim turns the hub's travel along its own heading, under the soft ceiling.");
                }
            }

            _drive.Drive = Vector2.zero;
        }
    }
}
