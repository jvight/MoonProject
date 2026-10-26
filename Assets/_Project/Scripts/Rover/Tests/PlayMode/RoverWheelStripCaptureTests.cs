using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// What the player sees of the wheels (owner playtest 00a309e9f: they spin far too fast): eight consecutive 60 fps
    /// frames of the wheel nearest the chase camera on the real rover, at about a metre a second and at full speed,
    /// written as strips to Logs/captures/wheels. <c>MOON_WHEEL_TAG</c> names the strips and <c>MOON_WHEEL_CAP</c>
    /// overrides the wheel spin's soft ceiling, so a before (the cap lifted) and an after can be compared.
    /// </summary>
    public sealed class RoverWheelStripCaptureTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const int StripFrames = 8;
        private const int TilePixels = 360;
        private const float SettleSeconds = 4f;
        private const float SideDegrees = 90f;

        private readonly RealRoverScene _scene = new RealRoverScene();
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

        private Transform NearestWheel()
        {
            Transform nearest = null;
            float best = float.MaxValue;
            foreach (Transform node in _scene.Rover.GetComponentsInChildren<Transform>(true))
            {
                for (int w = 0; w < RoverModelNodes.WheelCount; w++)
                {
                    if (node.name != RoverModelNodes.Wheel(w))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(node.position, _scene.View.transform.position);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = node;
                    }
                }
            }

            return nearest;
        }

        private IEnumerator Strip(string name, float stick)
        {
            Set(_gamepad.leftStick, new Vector2(0f, stick));
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            // The view the player turns to when they want to look at the wheels: square to the side of 07.
            _scene.CameraRig.Orbit.AdoptBearing(_scene.Rover.Heading + SideDegrees);
            yield return RealRoverScene.WaitSeconds(1.5f);
            Transform wheel = NearestWheel();
            var strip = new Texture2D(TilePixels * StripFrames, TilePixels, TextureFormat.RGB24, false);
            for (int i = 0; i < StripFrames; i++)
            {
                yield return null;
                Texture2D frame = FrameCapture.Render(_scene.View, 1920, 1080);
                Vector3 at = _scene.View.WorldToViewportPoint(wheel.position);
                int x = Mathf.Clamp(Mathf.RoundToInt(at.x * frame.width) - TilePixels / 2, 0, frame.width - TilePixels);
                int y = Mathf.Clamp(Mathf.RoundToInt(at.y * frame.height) - TilePixels / 2, 0, frame.height - TilePixels);
                strip.SetPixels(i * TilePixels, 0, TilePixels, TilePixels, frame.GetPixels(x, y, TilePixels, TilePixels));
                Object.DestroyImmediate(frame);
            }

            Set(_gamepad.leftStick, Vector2.zero);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "wheels"));
            Directory.CreateDirectory(folder);
            FrameCapture.WritePng(strip, Path.Combine(folder, name + ".png"));
            Object.DestroyImmediate(strip);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator CaptureStrips_TheRealWheelFrameByFrame_AtAboutOneMetrePerSecondAndFullSpeed()
        {
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            string tag = System.Environment.GetEnvironmentVariable("MOON_WHEEL_TAG") ?? "after";
#if UNITY_EDITOR
            string cap = System.Environment.GetEnvironmentVariable("MOON_WHEEL_CAP");
            if (!string.IsNullOrEmpty(cap))
            {
                var tuning = new UnityEditor.SerializedObject(_scene.Rover.GetComponentInChildren<RoverVisualRig>(true).Tuning);
                tuning.FindProperty("_wheelSpinSoftCap").floatValue =
                    float.Parse(cap, System.Globalization.CultureInfo.InvariantCulture);
                tuning.ApplyModifiedPropertiesWithoutUndo();
            }
#endif
            yield return Strip("wheel_" + tag + "_1mps", 0.13f);
            yield return Strip("wheel_" + tag + "_full", 1f);
        }
    }
}
