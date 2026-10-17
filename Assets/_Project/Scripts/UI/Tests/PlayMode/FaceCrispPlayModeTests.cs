using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// PlayMode visual verification for FACE-CRISP: renders known white-on-black text content through Rover 07's
    /// curved CRT face material (<c>R1_CrtFace.mat</c>, <c>MoonProject/R1/CrtFace</c>) at the inspect framing
    /// (0.62 m, 1920x1080) and measures the ghost metric (image energy correlated with itself shifted by 4-12 px
    /// relative to 0 px).
    /// </summary>
    public sealed class FaceCrispPlayModeTests
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const float InspectDistanceM = 0.62f;
        private const int MinGhostShiftPx = 4;
        private const int MaxGhostShiftPx = 12;

        /// <summary>
        /// Maximum allowed normalized ghost correlation energy of the text strip shifted by 4-12 px into the halo band
        /// relative to 0 px energy. On <c>main</c>, the discrete 4-tap halo produces 0.1302 at 8 px shift (FAILS).
        /// </summary>
        private const float MaxAllowedGhostRatio = 0.050f;

        /// <summary>
        /// Maximum allowed vertical high-pass edge correlation across shifts of 4-12 px relative to 0 px edge energy.
        /// On <c>main</c>, the discrete 4-tap halo produces 0.2442 at 8 px shift (FAILS).
        /// </summary>
        private const float MaxAllowedEdgeGhostRatio = 0.160f;

        private const string RoverPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";
        private const string ScreenMaterialPath = "Assets/_Project/Generated/Art/R1/Materials/R1_CrtFace.mat";
        private const string FacePanelPath = "Assets/_Project/Generated/UI/LofiLunarFacePanel.asset";
        private const string FaceTargetPath = "Assets/_Project/Generated/UI/LofiLunarFaceTarget.asset";
        private const string FontPath = "Assets/_Project/Generated/UI/Fonts/ChakraPetch-SemiBold.ttf";

        private static readonly int ContentTexId = Shader.PropertyToID("_ContentTex");
        private static readonly int FaceCrtStateId = Shader.PropertyToID("_FaceCrtState");
        private static readonly int FacePhasesId = Shader.PropertyToID("_FacePhases");
        private static readonly int FaceContentStateId = Shader.PropertyToID("_FaceContentState");

        private GameObject _roverInstance;
        private GameObject _cameraObject;
        private GameObject _uiHost;
        private PanelSettings _panelCopy;
        private Material _materialCopy;

        [TearDown]
        public void TearDown()
        {
            if (_uiHost != null)
            {
                Object.DestroyImmediate(_uiHost);
                _uiHost = null;
            }

            if (_panelCopy != null)
            {
                _panelCopy.targetTexture = null;
                Object.DestroyImmediate(_panelCopy);
                _panelCopy = null;
            }

            if (_materialCopy != null)
            {
                Object.DestroyImmediate(_materialCopy);
                _materialCopy = null;
            }

            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
                _cameraObject = null;
            }

            if (_roverInstance != null)
            {
                Object.DestroyImmediate(_roverInstance);
                _roverInstance = null;
            }
        }

        [UnityTest]
        public IEnumerator FaceScreen_WhiteTextLineOnBlack_GhostEnergyStaysUnderThreshold()
        {
#if UNITY_EDITOR
            var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefabPath);
            var screenMatAsset = AssetDatabase.LoadAssetAtPath<Material>(ScreenMaterialPath);
            var facePanelAsset = AssetDatabase.LoadAssetAtPath<PanelSettings>(FacePanelPath);
            var faceTarget = AssetDatabase.LoadAssetAtPath<RenderTexture>(FaceTargetPath);
            var fontAsset = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

            Assert.IsNotNull(roverPrefab, $"Missing Rover prefab at {RoverPrefabPath}.");
            Assert.IsNotNull(screenMatAsset, $"Missing CRT screen material at {ScreenMaterialPath}.");
            Assert.IsNotNull(facePanelAsset, $"Missing FacePanelSettings at {FacePanelPath}.");
            Assert.IsNotNull(faceTarget, $"Missing FaceTarget RenderTexture at {FaceTargetPath}.");

            _roverInstance = Object.Instantiate(roverPrefab, Vector3.zero, Quaternion.identity);
            Transform screenNode = FindChildRecursive(_roverInstance.transform, "Screen");
            Assert.IsNotNull(screenNode, "Rover prefab must contain child 'Screen'.");

            var screenRenderer = screenNode.GetComponent<Renderer>();
            Assert.IsNotNull(screenRenderer, "'Screen' must have a MeshRenderer.");

            _materialCopy = new Material(screenMatAsset);
            screenRenderer.sharedMaterial = _materialCopy;

            var block = new MaterialPropertyBlock();
            screenRenderer.GetPropertyBlock(block);
            block.SetTexture(ContentTexId, faceTarget);
            block.SetVector(FaceCrtStateId, new Vector4(1f, 1f, 0f, 0f));
            block.SetVector(FacePhasesId, new Vector4(0f, 0f, 0.35f, 0.35f));
            block.SetVector(FaceContentStateId, new Vector4(1f, 0f, 0f, 0f));
            screenRenderer.SetPropertyBlock(block);

            // Position camera at inspect framing (0.62 m in front of Screen, filling ~80% of the 1080p frame).
            Bounds screenBounds = screenRenderer.bounds;
            Vector3 screenCenter = screenBounds.center;
            Vector3 outward = _roverInstance.transform.forward;
            Vector3 camPos = screenCenter + outward * InspectDistanceM;
            Quaternion camRot = Quaternion.LookRotation(screenCenter - camPos, _roverInstance.transform.up);
            float screenHeight = Mathf.Max(0.20f, screenBounds.size.y);
            float inspectFov = 2f * Mathf.Atan((screenHeight / 0.80f) / (2f * InspectDistanceM)) * Mathf.Rad2Deg;

            _cameraObject = new GameObject("FaceCrispInspectCamera");
            var camera = _cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = inspectFov;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 20f;
            var camData = camera.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            _cameraObject.transform.SetPositionAndRotation(camPos, camRot);

            _panelCopy = Object.Instantiate(facePanelAsset);
            _panelCopy.targetTexture = faceTarget;
            _panelCopy.clearColor = true;
            _panelCopy.colorClearValue = Color.black;

            _uiHost = new GameObject("FaceCrispTestUI");
            _uiHost.SetActive(false);
            var doc = _uiHost.AddComponent<UIDocument>();
            doc.panelSettings = _panelCopy;
            _uiHost.SetActive(true);

            VisualElement root = doc.rootVisualElement;
            root.style.width = 1152;
            root.style.height = 768;
            root.style.backgroundColor = Color.black;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;

            yield return null;
            yield return null;
            yield return null;

            // 1. Render blank black screen to record the static CRT phosphor/scanline floor.
            Texture2D blankTex = FrameCapture.Render(camera, CaptureWidth, CaptureHeight);

            // 2. Add a crisp white text line on black at inspect framing.
            var label = new Label("Đèn pha ấm — 07 HOLD");
            label.style.color = Color.white;
            label.style.fontSize = 20;
            if (fontAsset != null)
            {
                label.style.unityFont = fontAsset;
                label.style.unityFontDefinition = FontDefinition.FromFont(fontAsset);
            }

            root.Add(label);

            yield return null;
            yield return null;
            yield return null;

            Texture2D textTex = FrameCapture.Render(camera, CaptureWidth, CaptureHeight);

            string capturesDir = Path.GetFullPath("Logs/captures");
            Directory.CreateDirectory(capturesDir);
            FrameCapture.WritePng(textTex, Path.Combine(capturesDir, "face_crisp_ghost_test_after.png"));

            try
            {
                GhostMeasurement measurement = MeasureGhostEnergy(blankTex, textTex);
                Debug.Log(
                    $"[face-crisp-ghost] haloBandRatio={measurement.HaloBandEnergyRatio:F4} at shift={measurement.PeakHaloBandShiftPx}px, " +
                    $"edgeYRatio={measurement.MaxEdgeGhostRatio:F4} at shift={measurement.PeakEdgeShiftPx}px " +
                    $"(rawYMax={measurement.MaxRawShiftYRatio:F4} at dy={measurement.PeakRawShiftYPx}px, " +
                    $"rawXMax={measurement.MaxRawShiftXRatio:F4} at dx={measurement.PeakRawShiftXPx}px)");

                Assert.Less(
                    measurement.HaloBandEnergyRatio,
                    MaxAllowedGhostRatio,
                    $"CRT face halo-band ghost energy ratio ({measurement.HaloBandEnergyRatio:F4} at {measurement.PeakHaloBandShiftPx}px shift) " +
                    $"must stay under {MaxAllowedGhostRatio:F4} (main FAILS at 0.1302).");

                Assert.Less(
                    measurement.MaxEdgeGhostRatio,
                    MaxAllowedEdgeGhostRatio,
                    $"CRT face vertical edge ghost correlation ({measurement.MaxEdgeGhostRatio:F4} at {measurement.PeakEdgeShiftPx}px shift) " +
                    $"must stay under {MaxAllowedEdgeGhostRatio:F4} (main FAILS at 0.2442).");
            }
            finally
            {
                Object.DestroyImmediate(blankTex);
                Object.DestroyImmediate(textTex);
            }
#else
            Assert.Ignore("FaceCrispPlayModeTests loads generated assets via AssetDatabase in the Editor.");
            yield break;
#endif
        }

        private static GhostMeasurement MeasureGhostEnergy(Texture2D blankTex, Texture2D textTex)
        {
            int width = textTex.width;
            int height = textTex.height;
            Color32[] blankPixels = blankTex.GetPixels32();
            Color32[] textPixels = textTex.GetPixels32();

            // Compute excess luminance above the blank CRT screen in the central ROI containing the text line.
            int xMin = width / 4;
            int xMax = (width * 3) / 4;
            int yMin = height / 3;
            int yMax = (height * 2) / 3;
            int roiW = xMax - xMin;
            int roiH = yMax - yMin;

            var delta = new float[roiW * roiH];
            var rowEnergy = new float[roiH];
            for (int y = 0; y < roiH; y++)
            {
                int srcY = yMin + y;
                float rowSum = 0f;
                for (int x = 0; x < roiW; x++)
                {
                    int idx = srcY * width + (xMin + x);
                    float lumText = Luminance(textPixels[idx]);
                    float lumBlank = Luminance(blankPixels[idx]);
                    float diff = Mathf.Max(0f, lumText - lumBlank);
                    delta[y * roiW + x] = diff;
                    rowSum += diff;
                }

                rowEnergy[y] = rowSum;
            }

            // Find the vertical core bounds of the primary text line (rows exceeding 25% of peak row energy).
            float peakRow = 0f;
            int peakY = roiH / 2;
            for (int y = 0; y < roiH; y++)
            {
                if (rowEnergy[y] > peakRow)
                {
                    peakRow = rowEnergy[y];
                    peakY = y;
                }
            }

            int coreTop = peakY;
            int coreBottom = peakY;
            float coreThreshold = peakRow * 0.25f;
            for (int y = 0; y < roiH; y++)
            {
                if (rowEnergy[y] >= coreThreshold)
                {
                    if (y < coreTop)
                    {
                        coreTop = y;
                    }

                    if (y > coreBottom)
                    {
                        coreBottom = y;
                    }
                }
            }

            // Compute vertical high-pass / edge profile (captures horizontal strokes & glyph top/bottom edges)
            // and correlate across vertical shifts d in [MinGhostShiftPx..MaxGhostShiftPx].
            var edgeY = new float[roiW * roiH];
            double energy0EdgeY = 0.0;
            for (int y = 1; y < roiH - 1; y++)
            {
                for (int x = 0; x < roiW; x++)
                {
                    float hp = Mathf.Max(0f, delta[y * roiW + x] - 0.5f * (delta[(y - 1) * roiW + x] + delta[(y + 1) * roiW + x]));
                    edgeY[y * roiW + x] = hp;
                    energy0EdgeY += (double)hp * hp;
                }
            }

            double energy0Raw = 0.0;
            for (int i = 0; i < delta.Length; i++)
            {
                energy0Raw += (double)delta[i] * delta[i];
            }

            float maxEdgeGhostRatio = 0f;
            int peakEdgeShiftPx = MinGhostShiftPx;
            float maxRawYRatio = 0f;
            int peakRawYPx = MinGhostShiftPx;
            float maxRawXRatio = 0f;
            int peakRawXPx = MinGhostShiftPx;

            for (int d = MinGhostShiftPx; d <= MaxGhostShiftPx; d++)
            {
                double corrEdgeY = 0.0;
                double corrRawY = 0.0;
                for (int y = 0; y + d < roiH; y++)
                {
                    int row0 = y * roiW;
                    int rowD = (y + d) * roiW;
                    for (int x = 0; x < roiW; x++)
                    {
                        corrEdgeY += (double)edgeY[row0 + x] * edgeY[rowD + x];
                        corrRawY += (double)delta[row0 + x] * delta[rowD + x];
                    }
                }

                float ratioEdge = energy0EdgeY > 1e-9 ? (float)(corrEdgeY / energy0EdgeY) : 0f;
                float ratioRawY = energy0Raw > 1e-9 ? (float)(corrRawY / energy0Raw) : 0f;
                Debug.Log($"[face-crisp-shift] d={d}px edgeYRatio={ratioEdge:F4} rawYRatio={ratioRawY:F4}");

                if (ratioEdge > maxEdgeGhostRatio)
                {
                    maxEdgeGhostRatio = ratioEdge;
                    peakEdgeShiftPx = d;
                }

                if (ratioRawY > maxRawYRatio)
                {
                    maxRawYRatio = ratioRawY;
                    peakRawYPx = d;
                }

                double corrRawX = 0.0;
                for (int y = 0; y < roiH; y++)
                {
                    int row = y * roiW;
                    for (int x = 0; x + d < roiW; x++)
                    {
                        corrRawX += (double)delta[row + x] * delta[row + x + d];
                    }
                }

                float ratioRawX = energy0Raw > 1e-9 ? (float)(corrRawX / energy0Raw) : 0f;
                if (ratioRawX > maxRawXRatio)
                {
                    maxRawXRatio = ratioRawX;
                    peakRawXPx = d;
                }
            }

            // Measure correlation of the core text strip shifted into the 4-12 px halo bands above and below the text line,
            // relative to 0 px energy.
            double haloBandCorr = 0.0;
            int peakHaloBandShiftPx = MinGhostShiftPx;
            for (int d = MinGhostShiftPx; d <= MaxGhostShiftPx; d++)
            {
                double corrUp = 0.0;
                double corrDown = 0.0;
                for (int y = coreTop; y <= coreBottom; y++)
                {
                    int rowCore = y * roiW;
                    int yUp = y - d;
                    int yDown = y + d;
                    if (yUp >= 0 && yUp < coreTop)
                    {
                        int rowUp = yUp * roiW;
                        for (int x = 0; x < roiW; x++)
                        {
                            corrUp += (double)delta[rowCore + x] * delta[rowUp + x];
                        }
                    }

                    if (yDown > coreBottom && yDown < roiH)
                    {
                        int rowDown = yDown * roiW;
                        for (int x = 0; x < roiW; x++)
                        {
                            corrDown += (double)delta[rowCore + x] * delta[rowDown + x];
                        }
                    }
                }

                double bandRatio = energy0Raw > 1e-9 ? (corrUp + corrDown) / energy0Raw : 0.0;
                if (bandRatio > haloBandCorr)
                {
                    haloBandCorr = bandRatio;
                    peakHaloBandShiftPx = d;
                }
            }

            return new GhostMeasurement(
                maxEdgeGhostRatio,
                peakEdgeShiftPx,
                maxRawYRatio,
                peakRawYPx,
                maxRawXRatio,
                peakRawXPx,
                (float)haloBandCorr,
                peakHaloBandShiftPx);
        }

        private static float Luminance(Color32 c)
        {
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform hit = FindChildRecursive(parent.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private readonly struct GhostMeasurement
        {
            public readonly float MaxEdgeGhostRatio;
            public readonly int PeakEdgeShiftPx;
            public readonly float MaxRawShiftYRatio;
            public readonly int PeakRawShiftYPx;
            public readonly float MaxRawShiftXRatio;
            public readonly int PeakRawShiftXPx;
            public readonly float HaloBandEnergyRatio;
            public readonly int PeakHaloBandShiftPx;

            public GhostMeasurement(
                float maxEdgeGhostRatio,
                int peakEdgeShiftPx,
                float maxRawShiftYRatio,
                int peakRawShiftYPx,
                float maxRawShiftXRatio,
                int peakRawShiftXPx,
                float haloBandEnergyRatio,
                int peakHaloBandShiftPx)
            {
                MaxEdgeGhostRatio = maxEdgeGhostRatio;
                PeakEdgeShiftPx = peakEdgeShiftPx;
                MaxRawShiftYRatio = maxRawShiftYRatio;
                PeakRawShiftYPx = peakRawShiftYPx;
                MaxRawShiftXRatio = maxRawShiftXRatio;
                PeakRawShiftXPx = peakRawShiftXPx;
                HaloBandEnergyRatio = haloBandEnergyRatio;
                PeakHaloBandShiftPx = peakHaloBandShiftPx;
            }
        }
    }
}
