using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The chase camera while 07 tows a load (owner build 2c1ea201d: it jerks back and forth when driving). A weaving
    /// 10 s drive with a load dragged behind at the physics rate, as a tow's body is; each frame's camera yaw and
    /// position are written to Logs/tow-camera.csv, and the camera's frame-to-frame yaw change must stay smooth: no
    /// reversals of its swing faster than the drive itself turns, and no frame's change differing sharply from its
    /// neighbours.
    /// </summary>
    public sealed class RoverTowCameraTests
    {
        private const string TowId = "test-crate";
        private const float LoadBehindMetres = 3.2f;
        private const float FollowPerStep = 0.25f;
        private const float SettleSeconds = 1.5f;

        /// <summary>The frame step the drive is recorded at: a player's 60 fps, not the editor's uncapped speed.</summary>
        private const float FrameStep = 1f / 60f;
        private const float DriveSeconds = 10f;
        private const float Throttle = 0.7f;
        private const float WeaveRadiansPerSecond = 0.9f;
        private const float WeaveSteer = 0.8f;
        private const float MinSwingDegrees = 0.02f;

        /// <summary>Most the yaw's per-frame change may differ from the average of the frames round it (deg).</summary>
        private const float MaxJerkDegrees = 0.25f;

        /// <summary>Most direction reversals of the yaw's swing a second (the weave itself reverses twice a period).</summary>
        private const float MaxReversalsPerSecond = 1.6f;

        private sealed class DraggedLoad : MonoBehaviour
        {
            public Transform Winch;
            public Transform Body;

            private void FixedUpdate()
            {
                Vector3 behind = Winch.position - Body.forward * LoadBehindMetres;
                behind.y = transform.position.y;
                transform.position = Vector3.Lerp(transform.position, behind, FollowPerStep);
                Vector3 flat = Winch.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 1e-4f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), FollowPerStep);
                }
            }
        }

        private sealed class FakeTowLine : ITowLine
        {
            public Transform Load { get; set; }

            public bool IsTowing => Load != null;

            public Vector3 LoadHitch => Load.position;

            public float Length => 6f;

            public float Strain01 => 0f;

            public bool TryFindLoad(string towId, out Transform root)
            {
                root = Load;
                return Load != null;
            }
        }

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private FakeTowLine _line;
        private GameObject _load;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameStep;
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _line = new FakeTowLine();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f, false,
                rover => rover.Context.Register<ITowLine>(_line));
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _rover?.Dispose();
            if (_load != null)
            {
                Object.Destroy(_load);
            }

            _world.Dispose();
            _physics.Dispose();
        }

        private static float Heading(Vector3 forward)
        {
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator DrivingWithALoad_TheChaseCameraSwingsSmoothly()
        {
            yield return DriveAndMeasure(true, "tow-camera.csv");
        }

        [UnityTest]
        public IEnumerator DrivingWithoutALoad_TheChaseCameraSwingsSmoothly()
        {
            yield return DriveAndMeasure(false, "tow-camera-control.csv");
        }

        private IEnumerator DriveAndMeasure(bool withLoad, string csvName)
        {
            yield return new WaitForSeconds(SettleSeconds);
            if (withLoad)
            {
                AttachLoad();
            }

            yield return new WaitForSeconds(SettleSeconds);
            yield return Drive(csvName, withLoad);
        }

        private void AttachLoad()
        {
            _load = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(_load.GetComponent<Collider>());
            _load.transform.localScale = new Vector3(1.6f, 1.1f, 1.6f);
            Vector3 start = _rover.Controller.WinchOrigin.position - _rover.Controller.transform.forward * LoadBehindMetres;
            _load.transform.position = start;
            var dragged = _load.AddComponent<DraggedLoad>();
            dragged.Winch = _rover.Controller.WinchOrigin;
            dragged.Body = _rover.Chassis;
            _line.Load = _load.transform;
            _rover.Context.Events.Publish(new TowAttached(TowId, start, 80f));
        }

        private IEnumerator Drive(string csvName, bool withLoad)
        {
            Transform camera = _rover.Camera.transform;
            var yaws = new List<float>();
            var csv = new StringBuilder("t,yaw,x,y,z,roverYaw\n");
            float began = Time.time;
            while (Time.time - began < DriveSeconds)
            {
                float t = Time.time - began;
                _rover.Drive.Drive = new Vector2(Mathf.Sin(t * WeaveRadiansPerSecond) * WeaveSteer, Throttle);
                yield return null;
                float yaw = Heading(camera.forward);
                yaws.Add(yaw);
                csv.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0000},{1:0.000},{2:0.000},{3:0.000},{4:0.000},{5:0.000}\n",
                    Time.time - began, yaw, camera.position.x, camera.position.y, camera.position.z,
                    Heading(_rover.Chassis.forward));
            }

            _rover.Drive.Drive = Vector2.zero;
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", csvName));
            File.WriteAllText(path, csv.ToString());

            var deltas = new List<float>();
            for (int i = 1; i < yaws.Count; i++)
            {
                deltas.Add(Mathf.DeltaAngle(yaws[i - 1], yaws[i]));
            }

            float worstJerk = 0f;
            int reversals = 0;
            float lastSwing = 0f;
            for (int i = 1; i < deltas.Count - 1; i++)
            {
                float around = 0.5f * (deltas[i - 1] + deltas[i + 1]);
                worstJerk = Mathf.Max(worstJerk, Mathf.Abs(deltas[i] - around));
                if (Mathf.Abs(deltas[i]) > MinSwingDegrees)
                {
                    float swing = Mathf.Sign(deltas[i]);
                    reversals += lastSwing != 0f && swing != lastSwing ? 1 : 0;
                    lastSwing = swing;
                }
            }

            float perSecond = reversals / DriveSeconds;
            Debug.Log($"[rover-towcam] load {withLoad}: {yaws.Count} frames: worst yaw jerk {worstJerk:0.000} deg, {reversals} reversals " +
                $"({perSecond:0.00}/s); csv {path}");
            Assert.LessOrEqual(worstJerk, MaxJerkDegrees, "the camera's yaw never kicks from one frame to the next");
            Assert.LessOrEqual(perSecond, MaxReversalsPerSecond, "the camera's swing does not flip back and forth");
        }
    }
}
