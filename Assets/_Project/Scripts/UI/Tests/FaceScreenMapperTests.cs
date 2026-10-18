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
            Assert.AreEqual(UiThemeBuilder.FaceReferenceResolution.x * UiThemeBuilder.FaceRenderScale, faceTarget.width);
            Assert.AreEqual(UiThemeBuilder.FaceReferenceResolution.y * UiThemeBuilder.FaceRenderScale, faceTarget.height);
            Assert.IsTrue(faceTarget.useMipMap, "Face target RenderTexture must enable mipmaps for smooth CRT phosphor glow.");
            Assert.IsTrue(faceTarget.autoGenerateMips, "Face target RenderTexture must auto-generate mipmaps.");
            Assert.AreEqual(FilterMode.Trilinear, faceTarget.filterMode, "Face target RenderTexture must use Trilinear filtering.");
            Assert.AreEqual(new Vector2Int(1152, 768), facePanel.referenceResolution);
            Assert.AreSame(faceTarget, facePanel.targetTexture);
            Assert.AreEqual(1f, facePanel.sortingOrder, 1e-4f, "Face panel must sort above the main HUD panel.");
        }

        [Test]
        public void TryPanelToUv_InvertsTryUvToPanelAcrossHoldAndDeviceCoordinates()
        {
            Vector2 size = FaceScreenMapper.DefaultPanelSize;
            const float curvature = 0.08f;
            Vector2[] targets =
            {
                FaceInventoryView.GetHoldCellReferenceRect(0).center,
                FaceInventoryView.GetHoldCellReferenceRect(7).center,
                FaceDevices.CenterOf(FaceDevices.Scan),
                FaceDevices.CenterOf(FaceDevices.Cutter),
                FaceDevices.CenterOf(FaceDevices.SocketTwo),
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

        [Test]
        public void GlassSafeArea_ContainsAllInventoryElementsAndMarginsAreSymmetric_AndCameraFramingIsSquareAcrossAspects()
        {
            Vector2 panelSize = FaceScreenMapper.DefaultPanelSize;
            float minMargin = FaceScreenMapper.GlassSafeMarginPx;

            float contentMinX = float.MaxValue;
            float contentMaxX = float.MinValue;
            float contentMinY = float.MaxValue;
            float contentMaxY = float.MinValue;

            // 1. All 24 Hold grid cells lie inside the rounded-rectangle glass mask with margin >= M.
            for (int i = 0; i < FaceInventoryView.MaxHoldCells; i++)
            {
                Rect cell = FaceInventoryView.GetHoldCellReferenceRect(i);
                float dist = FaceScreenMapper.SignedDistanceInsideGlass(cell, panelSize);
                Assert.GreaterOrEqual(dist, minMargin,
                    $"Hold cell {i} ({cell}) must stay >= {minMargin}px inside the rounded CRT glass (actual {dist:F2}px).");
                contentMinX = Mathf.Min(contentMinX, cell.xMin);
                contentMaxX = Mathf.Max(contentMaxX, cell.xMax);
            }

            // 2. All four device bubbles lie inside the rounded-rectangle glass mask with margin >= M.
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                Vector2 center = FaceDevices.CenterOf(i);
                float r = FaceInventoryView.BubbleRadius;
                float dist = FaceScreenMapper.SignedDistanceInsideGlass(center, r, panelSize);
                Assert.GreaterOrEqual(dist, minMargin,
                    $"Device {i} at {center} must stay >= {minMargin}px inside the rounded CRT glass (actual {dist:F2}px).");
                contentMinX = Mathf.Min(contentMinX, center.x - r);
                contentMaxX = Mathf.Max(contentMaxX, center.x + r);
            }

            // 3. Headers, header rules, divider, and both left/right tooltip boxes lie inside the glass safe area.
            var leftHeaderRule = new Rect(72f, 42f, 376f, 22f);
            var rightHeaderRule = new Rect(FaceInventoryView.RightPaneX, 42f, 568f, 22f);
            var leftTooltip = new Rect(72f, 664f, 376f, 62f);
            var rightTooltip = new Rect(512f, 664f, 568f, 62f);

            Assert.GreaterOrEqual(FaceScreenMapper.SignedDistanceInsideGlass(leftHeaderRule, panelSize), minMargin,
                "Left header and rule must lie inside the glass safe area.");
            Assert.GreaterOrEqual(FaceScreenMapper.SignedDistanceInsideGlass(rightHeaderRule, panelSize), minMargin,
                "Right header and rule must lie inside the glass safe area.");
            Assert.GreaterOrEqual(FaceScreenMapper.SignedDistanceInsideGlass(leftTooltip, panelSize), minMargin,
                "Left hold tooltip must lie inside the glass safe area.");
            Assert.GreaterOrEqual(FaceScreenMapper.SignedDistanceInsideGlass(rightTooltip, panelSize), minMargin,
                "Right socket tooltip must lie inside the glass safe area.");

            contentMinY = leftHeaderRule.yMin;
            contentMaxY = leftTooltip.yMax;

            // 4. Left/right and top/bottom margins of the content bounding box differ by < 10%.
            float leftMargin = contentMinX;
            float rightMargin = panelSize.x - contentMaxX;
            float topMargin = contentMinY;
            float bottomMargin = panelSize.y - contentMaxY;

            Assert.GreaterOrEqual(leftMargin, minMargin);
            Assert.GreaterOrEqual(rightMargin, minMargin);
            Assert.GreaterOrEqual(topMargin, minMargin);
            Assert.GreaterOrEqual(bottomMargin, minMargin);

            float lrDiffRatio = Mathf.Abs(leftMargin - rightMargin) / Mathf.Max(leftMargin, rightMargin);
            float tbDiffRatio = Mathf.Abs(topMargin - bottomMargin) / Mathf.Max(topMargin, bottomMargin);
            Assert.Less(lrDiffRatio, 0.10f,
                $"Left ({leftMargin:F1}px) and right ({rightMargin:F1}px) margins must differ by < 10% (actual {lrDiffRatio * 100f:F1}%).");
            Assert.Less(tbDiffRatio, 0.10f,
                $"Top ({topMargin:F1}px) and bottom ({bottomMargin:F1}px) margins must differ by < 10% (actual {tbDiffRatio * 100f:F1}%).");
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
