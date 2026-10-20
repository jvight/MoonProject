using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// A real tow in the real Main scene (opening v3 3.2b): 07 hooks the habitat's power module with its tether key,
    /// drives off with it, and the cable is seen from the side and from behind; the drum turns as the line pays out and
    /// the load arrives (or is let go) without a glow on the rope. Captures in Logs/captures/tow/.
    /// </summary>
    public sealed class TowCaptureTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const string LoadId = "habitat.power_module";
        private const string HitchNode = "TowHitch";
        private const float HookGap = 2.4f;
        private const float DriveSeconds = 7f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "tow"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Mouse _mouse;
        private Gamepad _gamepad;
        private GameObject _lens;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_lens != null)
            {
                Object.Destroy(_lens);
            }

            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ATow_ShowsTheRopeAndTheTurningDrum_FromTheSideAndFromBehind()
        {
            Directory.CreateDirectory(CaptureFolder);
            _mouse = InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            ITowLine tow = _scene.Context.Get<ITowLine>();
            RoverController rover = _scene.Rover;
            Assert.IsTrue(tow.TryFindLoad(LoadId, out Transform module), "the habitat's power module stands");
            Transform hitch = Find(module, HitchNode);
            Assert.IsNotNull(hitch, "the module has its tow hitch node");

            Vector3 front = hitch.position - module.position;
            front.y = 0f;
            front = front.normalized;
            Vector3 stand = hitch.position + front * HookGap;
            stand.y = hitch.position.y;
            _scene.Context.Get<IRoverPlacement>().PlaceAt(stand, Quaternion.LookRotation(front, Vector3.up));
            yield return RealRoverScene.WaitSeconds(1.2f);
            float gap = Vector3.Distance(rover.WinchOrigin.position, hitch.position);
            Assert.LessOrEqual(gap, 3f, $"07's winch is within the hook's reach of the hitch ({gap:F1} m)");

            Press(_mouse.rightButton);
            yield return null;
            Release(_mouse.rightButton);
            yield return RealRoverScene.WaitSeconds(0.3f);
            Assert.IsTrue(tow.IsTowing, "the tether key hooks the load");
            RoverTowCable cable = rover.TowCable;
            yield return RealRoverScene.WaitSeconds(0.2f);
            Assert.IsTrue(cable.IsVisible, "the rope shows");
            Assert.AreEqual(0f, Vector3.Distance(cable.HookPoint, tow.LoadHitch), 0.05f, "the clamp is on the hitch");
            float spoolAtHook = cable.SpoolDegrees;
            Capture("tow-hooked-side", rover, module, true);
            Capture("tow-hooked-behind", rover, module, false);

            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            float until = Time.time + DriveSeconds;
            float strainMax = 0f;
            bool shotTaut = false;
            while (Time.time < until)
            {
                yield return null;
                strainMax = Mathf.Max(strainMax, cable.Strain01);
                if (!shotTaut && cable.Extension >= tow.Length * 0.98f)
                {
                    shotTaut = true;
                    Capture("tow-taut-side", rover, module, true);
                    Capture("tow-taut-behind", rover, module, false);
                }
            }

            Assert.IsTrue(tow.IsTowing, "07 drives off with the load");
            Assert.Greater(cable.SpoolDegrees, spoolAtHook + 30f, "the drum turned as the rope paid out");
            Assert.IsTrue(shotTaut, "the rope came taut as 07 pulled away");
            Capture("tow-driving-side", rover, module, true);
            Capture("tow-driving-behind", rover, module, false);

            Set(_gamepad.leftStick, Vector2.zero);
            Press(_mouse.rightButton);
            yield return null;
            Release(_mouse.rightButton);
            yield return RealRoverScene.WaitSeconds(0.4f);
            Assert.IsFalse(tow.IsTowing, "the key lets it go");
            yield return RealRoverScene.WaitSeconds(1.8f);
            Assert.IsFalse(cable.IsVisible, "the rope winds back in and is gone");
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

        /// <summary>A shot of the tow: <paramref name="side"/> from 07's right, else from behind the load.</summary>
        private void Capture(string name, RoverController rover, Transform load, bool side)
        {
            if (_lens == null)
            {
                _lens = new GameObject("TowLens");
                Camera camera = _lens.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 45f;
                camera.nearClipPlane = 0.1f;
            }

            Vector3 roverAt = rover.Position;
            Vector3 loadAt = load.position;
            Vector3 middle = (roverAt + loadAt) * 0.5f + Vector3.up * 0.6f;
            Vector3 along = roverAt - loadAt;
            along.y = 0f;
            along = along.sqrMagnitude > 1e-4f ? along.normalized : rover.transform.forward;
            Vector3 across = Vector3.Cross(Vector3.up, along);
            float span = Vector3.Distance(roverAt, loadAt);
            if (side)
            {
                _lens.transform.position = middle + across * (span * 0.5f + 5f) + Vector3.up * 1.2f;
            }
            else
            {
                _lens.transform.position = loadAt - along * 5.5f + Vector3.up * 1.8f;
            }

            _lens.transform.rotation = Quaternion.LookRotation(middle - _lens.transform.position, Vector3.up);
            FrameCapture.SavePng(_lens.GetComponent<Camera>(), 1280, 720, Path.Combine(CaptureFolder, name + ".png"));
        }
    }
}
