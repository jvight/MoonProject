using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The player's chase camera while 07 tows the habitat's power module in the real Main scene: it rises and draws
    /// back so 07 is seen over the load, never sits inside it, eases in at the hook and out at the release without a
    /// jump, in a straight drive and in a turn. Captures of the real view in Logs/captures/tow-camera/.
    /// </summary>
    public sealed class TowCameraPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const string LoadId = "habitat.power_module";
        private const string HitchNode = "TowHitch";
        private const float HookGap = 2.4f;
        private const float EyeToHead = 1.1f;
        private const float MostStepMetres = 0.45f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "tow-camera"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Mouse _mouse;
        private Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator WhileTowing_TheChaseCameraSees07OverTheLoad_StraightAndInATurn_AndEasesInAndOut()
        {
            Directory.CreateDirectory(CaptureFolder);
            _mouse = InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            ITowLine tow = _scene.Context.Get<ITowLine>();
            RoverController rover = _scene.Rover;
            Camera view = _scene.View;
            Assert.IsTrue(tow.TryFindLoad(LoadId, out Transform module));
            Transform hitch = Find(module, HitchNode);
            Renderer[] parts = module.GetComponentsInChildren<Renderer>();

            Vector3 front = hitch.position - module.position;
            front.y = 0f;
            front = front.normalized;
            Vector3 stand = hitch.position + front * HookGap;
            stand.y = hitch.position.y;
            _scene.Context.Get<IRoverPlacement>().PlaceAt(stand, Quaternion.LookRotation(front, Vector3.up));
            _scene.CameraRig.Snap();
            yield return RealRoverScene.WaitSeconds(1.2f);
            Vector3 before = view.transform.position;
            float before07 = Vector3.Distance(before, rover.Position);

            Press(_mouse.rightButton);
            yield return null;
            Release(_mouse.rightButton);
            yield return null;
            Assert.IsTrue(tow.IsTowing, "07 hooked the load");

            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            Vector3 last = view.transform.position;
            float biggestStep = 0f;
            float until = Time.time + 1.6f;
            while (Time.time < until)
            {
                yield return null;
                Vector3 now = view.transform.position;
                biggestStep = Mathf.Max(biggestStep, Vector3.Distance(now, last) - rover.Speed * FrameTime);
                last = now;
            }

            Assert.Less(biggestStep, MostStepMetres, "the camera eases up and back: it never jumps at the hook");
            yield return RealRoverScene.WaitSeconds(1.5f);
            Assert.Greater(Vector3.Distance(view.transform.position, rover.Position), before07 + 0.5f,
                "it has drawn back from 07");
            AssertSees07(view, rover, parts, "driving straight");
            Capture(view, "tow-cam-straight");

            Set(_gamepad.leftStick, new Vector2(0.85f, 1f));
            yield return RealRoverScene.WaitSeconds(1.2f);
            AssertSees07(view, rover, parts, "turning");
            Capture(view, "tow-cam-turning-1");
            yield return RealRoverScene.WaitSeconds(1.8f);
            AssertSees07(view, rover, parts, "still turning");
            Capture(view, "tow-cam-turning-2");

            Set(_gamepad.leftStick, Vector2.zero);
            Press(_mouse.rightButton);
            yield return null;
            Release(_mouse.rightButton);
            yield return RealRoverScene.WaitSeconds(0.2f);
            Assert.IsFalse(tow.IsTowing, "07 let the load go");
            Vector3 lastOut = view.transform.position;
            float stepOut = 0f;
            until = Time.time + 1.4f;
            while (Time.time < until)
            {
                yield return null;
                Vector3 now = view.transform.position;
                stepOut = Mathf.Max(stepOut, Vector3.Distance(now, lastOut));
                lastOut = now;
            }

            Assert.Less(stepOut, MostStepMetres, "and it eases back down: no jump at the release");
        }

        /// <summary>The eye is outside the load and the line from it to 07's head never crosses the load.</summary>
        private static void AssertSees07(Camera view, RoverController rover, Renderer[] load, string when)
        {
            Vector3 eye = view.transform.position;
            Vector3 head = rover.Position + Vector3.up * EyeToHead;
            Vector3 line = head - eye;
            float length = line.magnitude;
            var ray = new Ray(eye, line / length);
            foreach (Renderer part in load)
            {
                Bounds bounds = part.bounds;
                Assert.IsFalse(bounds.Contains(eye), $"{when}: the camera is inside the load ({part.name})");
                if (bounds.IntersectRay(ray, out float distance))
                {
                    Assert.Greater(distance, length - 0.3f, $"{when}: the load ({part.name}) hides 07 from the camera");
                }
            }
        }

        private static void Capture(Camera view, string name)
        {
            FrameCapture.SavePng(view, 1920, 1080, Path.Combine(CaptureFolder, name + ".png"));
        }

        private static Transform Find(Transform root, string name)
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
    }
}
