using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The page change on 07's real face, frame by frame: Q from the hold to the databank and back, a 1920x1080 frame
    /// every 33 ms from the key press until the page has settled, cropped to the glass. Writes
    /// Logs/captures/page_flip/ (to_databank_NN.png, to_hold_NN.png) for a frame strip.
    /// </summary>
    public sealed class RoverFacePageStrip : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const int FramesPerShot = 2;
        private const int Shots = 18;
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 3f;
        private const int Width = 1920;
        private const int Height = 1080;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "page_flip"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
        private RenderTexture _target;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_scene.View != null)
            {
                _scene.View.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator QFlipsTheFacePage_BothWays_FrameByFrame()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();
            _target = new RenderTexture(Width, Height, 24);
            _scene.View.targetTexture = _target;
            yield return Tap(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!face.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the hold on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            yield return Strip("to_databank", FacePage.Databank);
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            yield return Strip("to_hold", FacePage.Hold);
        }

        private IEnumerator Strip(string name, FacePage to)
        {
            Press(_keyboard.qKey);
            for (int shot = 0; shot < Shots; shot++)
            {
                Save($"{name}_{shot:00}");
                for (int f = 0; f < FramesPerShot; f++)
                {
                    yield return null;
                    if (shot == 0 && f == 0)
                    {
                        Release(_keyboard.qKey);
                    }
                }
            }

            Assert.AreEqual(to, _scene.Ui.Pages.Page, "the page changed");
            Assert.IsFalse(_scene.Ui.Pages.IsFlipping, "and settled inside the strip");
        }

        private void Save(string name)
        {
            Texture2D frame = FrameCapture.Render(_scene.View, Width, Height);
            try
            {
                FrameCapture.WritePng(frame, Path.Combine(Folder, name + ".png"));
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }

        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }
    }
}
