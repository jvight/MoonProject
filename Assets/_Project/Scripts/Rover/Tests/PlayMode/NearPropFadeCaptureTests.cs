using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// A cut panel between the lens and 07 (the opening's B7c frame, the feel audit's camera score) thins away by
    /// dither as the lens nears it and is whole again once the camera has moved off. Before and after at 16:9 and 21:9 go
    /// to Logs/captures/near-fade/.
    /// </summary>
    public sealed class NearPropFadeCaptureTests
    {
        private const float FrameTime = 1f / 60f;
        private const float SettleSeconds = 1.2f;
        private const float PanelAhead = 1.9f;
        private const float PanelAside = 1.1f;
        private const float PanelBelow = 0.5f;
        private const float AwayMetres = 40f;
        private const float ThinnedBelow = 0.25f;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "near-fade"));

        private readonly RealRoverScene _scene = new RealRoverScene();

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator APanelAtTheLens_ThinsAway_AndComesBackWhenItIsLeftBehind()
        {
            Directory.CreateDirectory(Folder);
            yield return _scene.Boot();
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Camera lens = _scene.View;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.layer = Layers.Prop;
            panel.transform.localScale = new Vector3(3f, 0.08f, 1.8f);
            panel.transform.SetPositionAndRotation(
                lens.transform.position + lens.transform.forward * PanelAhead + lens.transform.right * PanelAside -
                lens.transform.up * PanelBelow,
                lens.transform.rotation * Quaternion.Euler(18f, 0f, 22f));
            var material = new Material(Shader.Find("MoonProject/R1/Lit"));
            material.SetColor("_BaseColor", new Color(0.35f, 0.42f, 0.55f));
            Renderer renderer = panel.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Physics.SyncTransforms();

            FrameCapture.SavePng(lens, 1920, 1080, Path.Combine(Folder, "panel-before-16x9.png"));
            FrameCapture.SavePng(lens, 3440, 1440, Path.Combine(Folder, "panel-before-21x9.png"));
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            FrameCapture.SavePng(lens, 1920, 1080, Path.Combine(Folder, "panel-after-16x9.png"));
            FrameCapture.SavePng(lens, 3440, 1440, Path.Combine(Folder, "panel-after-21x9.png"));
            Assert.Less(KeepOn(renderer), ThinnedBelow, "the panel at the lens has thinned away");

            panel.transform.position += Vector3.up * AwayMetres;
            Physics.SyncTransforms();
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Assert.AreEqual(1f, KeepOn(renderer), 1e-3f, "and it is whole again once the camera is off it");
            Object.Destroy(panel);
            Object.Destroy(material);
        }

        private static float KeepOn(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat("_NearKeep");
        }
    }
}
