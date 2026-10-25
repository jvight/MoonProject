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
    /// The owner sees one broken frame when Tab closes 07's face: every frame the screen actually shows around the
    /// close (what is on the display at the end of each frame, UI and post-processing included) goes to
    /// Logs/captures/face_close/, one PNG a frame at a fixed 60 fps, for a script to find the odd one out.
    /// </summary>
    public sealed class FaceCloseFramesTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 2.5f;
        private const int FramesBefore = 6;
        private const int FramesAfter = 45;
        private const int Cycles = 2;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "face_close"));

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
        public IEnumerator EveryFrameAroundTheClose_IsRecordedAsItIsDisplayed()
        {
            if (Directory.Exists(Folder))
            {
                Directory.Delete(Folder, true);
            }

            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();
            yield return RealRoverScene.WaitSeconds(1f);
            for (int cycle = 0; cycle < Cycles; cycle++)
            {
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
                for (int i = 0; i < FramesBefore; i++)
                {
                    yield return Record(cycle, i - FramesBefore);
                }

                Press(_keyboard.tabKey);
                yield return Record(cycle, 0);
                Release(_keyboard.tabKey);
                for (int i = 1; i <= FramesAfter; i++)
                {
                    yield return Record(cycle, i);
                }

                Assert.IsFalse(face.IsOpen, "Tab closed the face");
                yield return RealRoverScene.WaitSeconds(SettleSeconds);
            }
        }

        private static IEnumerator Record(int cycle, int frame)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, $"c{cycle}_f{frame + 100:D3}.png"), shot.EncodeToPNG());
            Object.Destroy(shot);
        }
    }
}
