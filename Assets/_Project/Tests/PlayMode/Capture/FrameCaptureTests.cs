using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode.Capture
{
    /// <summary>How PlayMode tests take screenshots. Needs a GPU: never run these with --nographics.</summary>
    public sealed class FrameCaptureTests
    {
        private GameObject _camera;
        private GameObject _cube;
        private GameObject _light;
        private string _pngPath;

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_camera);
            Object.Destroy(_cube);
            Object.Destroy(_light);
            if (_pngPath != null && File.Exists(_pngPath))
            {
                File.Delete(_pngPath);
            }
        }

        [UnityTest]
        public IEnumerator Render_LitObject_IsNotBlackAndWritesPng()
        {
            _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _light = new GameObject("Light");
            var light = _light.AddComponent<Light>();
            light.type = LightType.Directional;
            _light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            _camera = new GameObject("Camera");
            var camera = _camera.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            _camera.transform.position = new Vector3(0f, 1.5f, -3f);
            _camera.transform.LookAt(Vector3.zero);
            yield return null;

            Texture2D image = FrameCapture.Render(camera, 128, 128);
            float luminance = FrameCapture.MeanLuminance(image);
            _pngPath = Path.Combine(Path.GetTempPath(), "moon-framecapture-test.png");
            FrameCapture.WritePng(image, _pngPath);
            Object.Destroy(image);

            Assert.Greater(luminance, 0.02f, "the lit cube shows up against the black background");
            Assert.IsTrue(File.Exists(_pngPath));
        }
    }
}
