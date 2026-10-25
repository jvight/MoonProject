using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Closing 07's face screen does not whirl the camera round 07 (owner: a broken frame on Tab): the camera stays on
    /// the face while the CRT powers off, the view dips to dark, the camera is back on the chase pose under the dark,
    /// and the view comes back up on the pose it had before the screen opened. The whole close is about a second.
    /// </summary>
    public sealed class FaceCloseDipPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 2.5f;
        private const float WatchSeconds = 3f;
        private const float DarkEnough = 0.9f;
        private const float BigYawJumpDegrees = 20f;
        private const float MostCloseSeconds = 1.2f;
        private const float SamePoseMetres = 0.25f;
        private const float SamePoseDegrees = 3f;

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;

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
        public IEnumerator TheCloseDipsToDark_PutsTheCameraBackUnderIt_AndComesBackOnTheSamePose()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Transform lens = _scene.View.transform;
            Vector3 beforePosition = lens.position;
            Quaternion beforeRotation = lens.rotation;

            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!face.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the hold on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);

            float start = Time.time;
            float lastYaw = lens.eulerAngles.y;
            float darkAtJump = -1f;
            float darkest = 0f;
            float clearedAt = -1f;
            float closeSeconds = 0f;
            while (Time.time - start < WatchSeconds)
            {
                yield return null;
                float yaw = lens.eulerAngles.y;
                float dark = _scene.CameraRig.FaceCloseDark;
                darkest = Mathf.Max(darkest, dark);
                if (Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) > BigYawJumpDegrees)
                {
                    darkAtJump = Mathf.Max(darkAtJump, dark);
                }

                lastYaw = yaw;
                if (darkest > 0.5f && dark <= 0.001f && clearedAt < 0f)
                {
                    clearedAt = Time.time - start;
                }
            }

            closeSeconds = clearedAt;
            Debug.Log($"[facedip] darkest {darkest:F2}, dark at the camera's jump {darkAtJump:F2}, clear again after " +
                      $"{closeSeconds:F2} s");
            Assert.GreaterOrEqual(darkest, DarkEnough, "the view went dark");
            Assert.GreaterOrEqual(darkAtJump, DarkEnough, "any big swing of the camera happened under the dark");
            Assert.Greater(closeSeconds, 0f, "the view came back");
            Assert.LessOrEqual(closeSeconds, MostCloseSeconds, "about a second, not a long orbit");
            Assert.LessOrEqual(Vector3.Distance(lens.position, beforePosition), SamePoseMetres,
                "back on the pose it had before the screen opened");
            Assert.LessOrEqual(Quaternion.Angle(lens.rotation, beforeRotation), SamePoseDegrees);
        }
    }
}
