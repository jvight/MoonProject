using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    public sealed class FaceScreenMapperTests
    {
        [Test]
        public void TryUvToPanel_CenterMapsToPanelCenter_AndMirrorsHorizontalAndFlipsVertical()
        {
            Vector2 size = FaceScreenMapper.DefaultPanelSize;
            Assert.IsTrue(FaceScreenMapper.TryUvToPanel(new Vector2(0.5f, 0.5f), 0.12f, size, out Vector2 center));
            Assert.AreEqual(576f, center.x, 0.01f);
            Assert.AreEqual(384f, center.y, 0.01f);

            // Viewer's left side of the Screen quad has meshUv.x = 0.75 -> panel x = 0.25 * 1152 = 288.
            // Upper half of the Screen quad has meshUv.y = 0.75 -> top-origin panel y = (1 - 0.75) * 768 = 192.
            Assert.IsTrue(FaceScreenMapper.TryUvToPanel(new Vector2(0.75f, 0.75f), 0f, size, out Vector2 topLeftQuad));
            Assert.AreEqual(288f, topLeftQuad.x, 0.01f);
            Assert.AreEqual(192f, topLeftQuad.y, 0.01f);
        }

        [Test]
        public void TryUvToPanel_AppliesBarrelCurvature_AndRejectsWarpedCornersOutsideBezel()
        {
            Vector2 size = FaceScreenMapper.DefaultPanelSize;
            Assert.IsTrue(FaceScreenMapper.TryUvToPanel(new Vector2(0.75f, 0.75f), 0f, size, out Vector2 flat));
            Assert.IsTrue(FaceScreenMapper.TryUvToPanel(new Vector2(0.75f, 0.75f), 0.12f, size, out Vector2 curved));
            Assert.Less(curved.x, flat.x, "barrel warp pushes top-left quadrant further from center");
            Assert.Less(curved.y, flat.y);

            // Extreme corner (0.99, 0.99) warps past [0..1]^2 under curvature = 0.12.
            Assert.IsFalse(FaceScreenMapper.TryUvToPanel(new Vector2(0.99f, 0.99f), 0.12f, size, out Vector2 outside));
            Assert.IsTrue(float.IsNaN(outside.x));
            Assert.IsTrue(float.IsNaN(outside.y));
        }

        [Test]
        public void ScreenToPanel_ReturnsPanelPointWhenOpenAndHit_AndNaNWhenClosedOrMissed()
        {
            var cameraHost = new GameObject("TestCamera");
            var target = new RenderTexture(1920, 1080, 16);
            try
            {
                var camera = cameraHost.AddComponent<Camera>();
                camera.targetTexture = target;
                camera.fieldOfView = 26.86f;
                cameraHost.transform.position = new Vector3(0f, 0f, -0.67f);
                cameraHost.transform.rotation = Quaternion.identity;

                var fakeScreen = new FakeFaceScreen { Curvature = 0.12f, IsOpen = false };
                Vector2 closed = FaceScreenMapper.ScreenToPanel(new Vector2(960f, 540f), camera, fakeScreen,
                    FaceScreenMapper.DefaultPanelSize, 1080f);
                Assert.IsTrue(float.IsNaN(closed.x));

                fakeScreen.IsOpen = true;
                fakeScreen.Hit = true;
                fakeScreen.HitUv = new Vector2(0.5f, 0.5f);
                Vector2 centerHit = FaceScreenMapper.ScreenToPanel(new Vector2(960f, 540f), camera, fakeScreen,
                    FaceScreenMapper.DefaultPanelSize, 1080f);
                Assert.AreEqual(576f, centerHit.x, 0.01f);
                Assert.AreEqual(384f, centerHit.y, 0.01f);

                fakeScreen.Hit = false;
                Vector2 miss = FaceScreenMapper.ScreenToPanel(new Vector2(10f, 10f), camera, fakeScreen,
                    FaceScreenMapper.DefaultPanelSize, 1080f);
                Assert.IsTrue(float.IsNaN(miss.x));
            }
            finally
            {
                Object.DestroyImmediate(cameraHost);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void GeneratedFacePanelAndRenderTexture_MatchFaceAspect1152x768()
        {
            var faceTarget = AssetDatabase.LoadAssetAtPath<RenderTexture>(UiAssetPaths.FaceRenderTexture);
            var facePanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiAssetPaths.FacePanelSettings);
            Assert.IsNotNull(faceTarget, $"{UiAssetPaths.FaceRenderTexture} is missing: run the UI/Theme builder");
            Assert.IsNotNull(facePanel, $"{UiAssetPaths.FacePanelSettings} is missing: run the UI/Theme builder");
            Assert.AreEqual(1152, faceTarget.width);
            Assert.AreEqual(768, faceTarget.height);
            Assert.AreEqual(new Vector2Int(1152, 768), facePanel.referenceResolution);
            Assert.AreSame(faceTarget, facePanel.targetTexture);
            Assert.AreEqual(1f, facePanel.sortingOrder, 1e-4f, "Face panel must sort above the main HUD panel.");
        }

        [Test]
        public void TryPanelToUv_InvertsTryUvToPanelAcrossHoldAndOrbitalSocketCoordinates()
        {
            Vector2 size = FaceScreenMapper.DefaultPanelSize;
            const float curvature = 0.08f;
            Vector2[] targets =
            {
                FaceInventoryView.GetHoldCellReferenceRect(0).center,
                FaceInventoryView.GetHoldCellReferenceRect(7).center,
                FaceInventoryView.GetOrbitalSocketReferenceCenter(FaceInventoryView.OrbitalIndexOf(RoverSocket.Wing)),
                FaceInventoryView.GetOrbitalSocketReferenceCenter(FaceInventoryView.OrbitalIndexOf(RoverSocket.Belly)),
                FaceInventoryView.GetOrbitalSocketReferenceCenter(FaceInventoryView.OrbitalIndexOf(RoverSocket.Deck)),
            };

            foreach (Vector2 target in targets)
            {
                Assert.IsTrue(FaceScreenMapper.TryPanelToUv(target, curvature, size, out Vector2 uv),
                    $"TryPanelToUv must succeed for {target}");
                Assert.IsTrue(FaceScreenMapper.TryUvToPanel(uv, curvature, size, out Vector2 roundTrip),
                    $"TryUvToPanel must succeed for inverted UV {uv}");
                Assert.AreEqual(target.x, roundTrip.x, 0.05f);
                Assert.AreEqual(target.y, roundTrip.y, 0.05f);
            }
        }

        private sealed class FakeFaceScreen : IRoverFaceScreen
        {
            public float Curvature { get; set; }
            public bool IsOpen { get; set; }
            public bool Hit { get; set; }
            public Vector2 HitUv { get; set; }

            public void SetScreenOpen(bool open, Texture contentTexture)
            {
                IsOpen = open;
            }

            public bool TryRaycastScreenUv(Ray ray, out Vector2 meshUv)
            {
                meshUv = Hit ? HitUv : Vector2.zero;
                return Hit;
            }
        }
    }
}
