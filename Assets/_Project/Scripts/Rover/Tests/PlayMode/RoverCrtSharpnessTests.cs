using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// How sharp 07's CRT face UI reads on screen (owner: "it all looks smeared"): at 1920x1080, 2560x1440 and
    /// 3440x1440 it measures how many UI-texture pixels land on one screen pixel, an edge sharpness score and the WCAG
    /// contrast of the foot row's words and of a hold cell's name, for the face as shipped and with each CRT effect
    /// turned off in turn, and saves crops of the hold and of a databank card. Writes Logs/captures/crt_crisp/.
    /// </summary>
    public sealed class RoverCrtSharpnessTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 4f;
        private const float FlipTimeout = 3f;
        private const float ContrastFloor = 0.05f;

        // The safe rect's corners sit on the glass's rounded edge; a step toward the centre keeps them on it.
        private const float CornerInset = 0.06f;
        private static readonly int ContentGlowId = Shader.PropertyToID("_ContentGlowIntensity");
        private static readonly int ScanlineDepthId = Shader.PropertyToID("_ScanlineDepth");

        private static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3440, 1440),
        };

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "crt_crisp"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private readonly StringBuilder _report = new StringBuilder();
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
        public IEnumerator TheFaceUi_SharpnessAndContrast_AtEveryScreenSize_WithEachCrtEffectInTurn()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();
            yield return Tap(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!face.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the hold on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Renderer screen = _scene.Body.FaceDirector.Display.ScreenRenderer;
            Material shipped = screen.sharedMaterial;
            var probe = new Material(shipped);
            screen.sharedMaterial = probe;
            var cameraData = _scene.View.GetComponent<UniversalAdditionalCameraData>();
            Texture content = _scene.Body.FaceDirector.Display.ContentTexture;
            _report.AppendLine($"# CRT face sharpness (content texture {content.width}x{content.height}, " +
                               $"filter {content.filterMode}, mips {(content is RenderTexture rt && rt.useMipMap)})");
            _report.AppendLine();
            _report.AppendLine("| screen | variant | texels/px | sharpness | foot row WCAG | cell name WCAG |");
            _report.AppendLine("|---|---|---|---|---|---|");
            try
            {
                foreach (Vector2Int shape in Shapes)
                {
                    yield return UseShape(shape);
                    yield return Measure(face, shape, "shipped", content);
                    cameraData.renderPostProcessing = false;
                    yield return Measure(face, shape, "no post (grain, bloom, grading)", content);
                    cameraData.renderPostProcessing = true;
                    probe.SetFloat(ContentGlowId, 0f);
                    yield return Measure(face, shape, "no phosphor halo", content);
                    probe.SetFloat(ContentGlowId, shipped.GetFloat(ContentGlowId));
                    probe.SetFloat(ScanlineDepthId, 0f);
                    yield return Measure(face, shape, "no scanlines", content);
                    probe.SetFloat(ScanlineDepthId, shipped.GetFloat(ScanlineDepthId));
                    SaveFace(face, $"hold_{shape.x}x{shape.y}");
                }

                yield return Tap(_keyboard.qKey);
                float flip = Time.time + FlipTimeout;
                while (_scene.Ui.Pages.Page != FacePage.Databank || _scene.Ui.Pages.IsFlipping)
                {
                    Assert.Less(Time.time, flip, "Q flips to the databank");
                    yield return null;
                }

                yield return RealRoverScene.WaitSeconds(1f);
                foreach (Vector2Int shape in Shapes)
                {
                    yield return UseShape(shape);
                    SaveFace(face, $"databank_{shape.x}x{shape.y}");
                }
            }
            finally
            {
                screen.sharedMaterial = shipped;
                Object.DestroyImmediate(probe);
                cameraData.renderPostProcessing = true;
            }

            File.WriteAllText(Path.Combine(Folder, "report.md"), _report.ToString());
            Debug.Log(_report.ToString());
        }

        private IEnumerator UseShape(Vector2Int shape)
        {
            _scene.View.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            _target = new RenderTexture(shape.x, shape.y, 24);
            _scene.View.targetTexture = _target;
            yield return RealRoverScene.WaitSeconds(0.4f);
        }

        private IEnumerator Measure(IRoverFaceScreen face, Vector2Int shape, string variant, Texture content)
        {
            yield return null;
            yield return null;
            Texture2D frame = FrameCapture.Render(_scene.View, shape.x, shape.y);
            try
            {
                RectInt foot = ScreenRect(face, new Rect(FaceLayout.Margin, FaceLayout.FooterY,
                    FaceLayout.RightEdge - FaceLayout.Margin, FaceLayout.FooterHeight));
                RectInt cell = ScreenRect(face, FaceLayout.HoldNameRect(0));
                Rect safe = FaceScreenMapper.GetGlassSafeRect(FaceScreenMapper.DefaultPanelSize);
                float texels = content.width / FaceInventoryView.ReferenceWidth * safe.width /
                               Mathf.Max(1, ScreenRect(face, safe).width);
                _report.AppendLine($"| {shape.x}x{shape.y} | {variant} | {texels:F2} | {Sharpness(frame, foot):F3} | " +
                                   $"{Contrast(frame, foot):F1}:1 | {Contrast(frame, cell):F1}:1 |");
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }

        /// <summary>A panel rect's bounding box on screen (the face is curved; its corners bound it).</summary>
        private RectInt ScreenRect(IRoverFaceScreen face, Rect panel)
        {
            var points = new List<Vector2>();
            foreach (Vector2 corner in new[]
                     {
                         new Vector2(panel.xMin, panel.yMin), new Vector2(panel.xMax, panel.yMin),
                         new Vector2(panel.xMin, panel.yMax), new Vector2(panel.xMax, panel.yMax),
                     })
            {
                Vector2 inside = Vector2.Lerp(corner, panel.center, CornerInset);
                if (FaceScreenMapper.TryPanelToScreen(inside, _scene.View, face, FaceScreenMapper.DefaultPanelSize,
                        out Vector2 screen))
                {
                    points.Add(screen);
                }
            }

            Assert.IsNotEmpty(points, $"the panel rect {panel} is on the glass");
            Vector2 min = points[0];
            Vector2 max = points[0];
            foreach (Vector2 p in points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return new RectInt(Mathf.FloorToInt(min.x), Mathf.FloorToInt(min.y), Mathf.CeilToInt(max.x - min.x),
                Mathf.CeilToInt(max.y - min.y));
        }

        /// <summary>Mean horizontal luminance step over the region's contrast: higher reads sharper.</summary>
        private static float Sharpness(Texture2D frame, RectInt rect)
        {
            float sum = 0f;
            int count = 0;
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax - 1; x++)
                {
                    sum += Mathf.Abs(Luma(frame, x + 1, y) - Luma(frame, x, y));
                    count++;
                }
            }

            Percentiles(frame, rect, out float dark, out float bright);
            return count == 0 ? 0f : sum / count / Mathf.Max(0.01f, bright - dark);
        }

        /// <summary>WCAG contrast of the region's brightest strokes (95th percentile) against its background (5th).</summary>
        private static float Contrast(Texture2D frame, RectInt rect)
        {
            Percentiles(frame, rect, out float dark, out float bright);
            return (bright + ContrastFloor) / (dark + ContrastFloor);
        }

        private static void Percentiles(Texture2D frame, RectInt rect, out float dark, out float bright)
        {
            var values = new List<float>(rect.width * rect.height);
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    values.Add(Luma(frame, x, y));
                }
            }

            values.Sort();
            dark = values.Count == 0 ? 0f : values[(int)(values.Count * 0.05f)];
            bright = values.Count == 0 ? 0f : values[(int)(values.Count * 0.95f)];
        }

        /// <summary>Relative luminance (WCAG) of a pixel.</summary>
        private static float Luma(Texture2D frame, int x, int y)
        {
            Color c = frame.GetPixel(x, y).linear;
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private void SaveFace(IRoverFaceScreen face, string name)
        {
            RectInt rect = ScreenRect(face, FaceScreenMapper.GetGlassSafeRect(FaceScreenMapper.DefaultPanelSize));
            Texture2D frame = FrameCapture.Render(_scene.View, _target.width, _target.height);
            var crop = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            try
            {
                crop.SetPixels(frame.GetPixels(rect.x, rect.y, rect.width, rect.height));
                crop.Apply();
                FrameCapture.WritePng(crop, Path.Combine(Folder, name + ".png"));
            }
            finally
            {
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(crop);
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
