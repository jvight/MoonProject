using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The face inventory's pointer on 07's curved CRT at every screen shape (ultrawide 3440x1440 included): a screen
    /// point maps into the face panel and back onto the same pixel, and a hold part dragged by the real mouse sits
    /// exactly under the cursor. Writes Logs/captures/inv_drag/ with a cross at the cursor.
    /// </summary>
    public sealed class RoverFaceDragPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float BootSettleSeconds = 4f;
        private const float RoundTripPixels = 1.5f;
        private const float UnderCursorPixels = 3f;
        private const int DragSteps = 8;
        private const int CrossHalf = 18;

        private static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(3440, 1440), new Vector2Int(1920, 1080), new Vector2Int(2560, 1600), new Vector2Int(1280, 1024),
        };

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "inv_drag"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Camera _view;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private RenderTexture _target;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_view != null)
            {
                _view.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
                _target = null;
            }

            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator EveryScreenShape_MapsTheCursorOntoTheFaceAndBack_AndTheDraggedPartSitsUnderIt()
        {
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            _view = _scene.View;
            ICargoHold hold = _scene.Context.Get<ICargoHold>();
            MethodInfo add = hold.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(add, "the hold takes material");
            add.Invoke(hold, new object[] { SalvageMaterial.Metal, 5 });
            FaceInventoryView view = _scene.Ui.Cargo.View;
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();

            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!face.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the inventory on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(BootSettleSeconds);
            var failures = new List<string>();
            foreach (Vector2Int shape in Shapes)
            {
                yield return UseShape(shape);
                CheckRoundTrip(face, shape, failures);
                yield return CheckDrag(view, face, shape, failures);
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private IEnumerator UseShape(Vector2Int shape)
        {
            _view.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

            _target = new RenderTexture(shape.x, shape.y, 24);
            _view.targetTexture = _target;
            yield return RealRoverScene.WaitSeconds(0.3f);
        }

        /// <summary>Points across the glass (inside the rounded safe area) map back onto themselves.</summary>
        private void CheckRoundTrip(IRoverFaceScreen face, Vector2Int shape, List<string> failures)
        {
            Vector2 panel = FaceScreenMapper.DefaultPanelSize;
            Rect safe = FaceScreenMapper.GetGlassSafeRect(panel);
            for (int ix = 0; ix <= 4; ix++)
            {
                for (int iy = 0; iy <= 3; iy++)
                {
                    var point = new Vector2(safe.x + safe.width * ix / 4f, safe.y + safe.height * iy / 3f);
                    if (FaceScreenMapper.SignedDistanceInsideGlass(point, panel) < FaceScreenMapper.GlassSafeMarginPx)
                    {
                        continue;
                    }

                    if (!FaceScreenMapper.TryPanelToScreen(point, _view, face, panel, out Vector2 screen))
                    {
                        failures.Add($"{shape}: panel {point} has no screen point");
                        continue;
                    }

                    var topLeft = new Vector2(screen.x, _view.pixelHeight - screen.y);
                    Vector2 back = FaceScreenMapper.ScreenToPanel(topLeft, _view, face, panel);
                    if (float.IsNaN(back.x) || (back - point).magnitude > RoundTripPixels)
                    {
                        failures.Add($"{shape}: panel {point} -> screen {screen} -> panel {back}");
                    }
                }
            }
        }

        /// <summary>Drags hold cell 0's part with the mouse to cell 6 and measures the part against the cursor.</summary>
        private IEnumerator CheckDrag(FaceInventoryView view, IRoverFaceScreen face, Vector2Int shape,
            List<string> failures)
        {
            Vector2 panel = FaceScreenMapper.DefaultPanelSize;
            Vector2 from = FaceInventoryView.GetHoldCellReferenceRect(0).center;
            Vector2 to = FaceInventoryView.GetHoldCellReferenceRect(6).center + new Vector2(30f, 20f);
            if (!FaceScreenMapper.TryPanelToScreen(from, _view, face, panel, out Vector2 start) ||
                !FaceScreenMapper.TryPanelToScreen(to, _view, face, panel, out Vector2 end))
            {
                failures.Add($"{shape}: the hold cells are not on screen");
                yield break;
            }

            Set(_mouse.position, start);
            yield return null;
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            for (int i = 1; i <= DragSteps; i++)
            {
                Set(_mouse.position, Vector2.Lerp(start, end, i / (float)DragSteps));
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(0.4f);
            if (view.DragPhase != HoldDragPhase.Dragging)
            {
                failures.Add($"{shape}: the press on cell 0 did not become a drag ({view.DragPhase})");
            }
            else if (!FaceScreenMapper.TryPanelToScreen(view.DragGhost, _view, face, panel, out Vector2 ghost))
            {
                failures.Add($"{shape}: the dragged part at {view.DragGhost} is off the glass");
            }
            else
            {
                float apart = (ghost - end).magnitude;
                if (apart > UnderCursorPixels)
                {
                    failures.Add($"{shape}: the dragged part is {apart:F1} px from the cursor (part {ghost}, " +
                                 $"cursor {end})");
                }

                if (shape.x == Shapes[0].x && shape.y == Shapes[0].y)
                {
                    Capture($"drag_{shape.x}x{shape.y}", end);
                }
            }

            Release(_mouse.leftButton);
            float settle = Time.time + 2f;
            while (view.DragPhase != HoldDragPhase.Idle && Time.time < settle)
            {
                yield return null;
            }
        }

        /// <summary>The frame at the target's size with a cross where the cursor is (the OS cursor is not drawn).</summary>
        private void Capture(string name, Vector2 cursor)
        {
            Texture2D image = FrameCapture.Render(_view, _target.width, _target.height);
            try
            {
                int cx = Mathf.RoundToInt(cursor.x);
                int cy = Mathf.RoundToInt(cursor.y);
                for (int d = -CrossHalf; d <= CrossHalf; d++)
                {
                    for (int w = -1; w <= 1; w++)
                    {
                        Plot(image, cx + d, cy + w);
                        Plot(image, cx + w, cy + d);
                    }
                }

                image.Apply();
                FrameCapture.WritePng(image, Path.Combine(CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        private static void Plot(Texture2D image, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < image.width && y < image.height)
            {
                image.SetPixel(x, y, Color.magenta);
            }
        }
    }
}
