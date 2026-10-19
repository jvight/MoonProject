using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The owner could not fit the spare igniter in a real play (2026-10-19): the opening stalled at B6. With 07 on its
    /// built pad and the igniter in the hold, with real pointer events (the Input System's mouse: move, press, move,
    /// release) on 07's curved CRT, at 1920x1080 and 2560x1440 (not only the 21:9 of our captures): a drag onto the
    /// cutter socket fits it, a click on the igniter and then on the socket fits it, Enter or E on the selected
    /// igniter fits it, and a drop anywhere on the right-hand device panel goes to the cutter. The hold screen opens
    /// with the igniter selected and says how.
    /// </summary>
    public sealed class FaceIgniterPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float BootSettleSeconds = 3f;
        private const float DockSeconds = 4f;
        private const float FitTimeout = 2f;
        private const int DragSteps = 10;

        /// <summary>The shapes a player may have: 16:9 at two sizes (the captures are 21:9).</summary>
        private static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(1920, 1080), new Vector2Int(2560, 1440),
        };

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Camera _view;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private RenderTexture _target;
        private GameplaySystem _gameplay;
        private FaceInventoryView _face;
        private IRoverFaceScreen _screen;
        private int _igniterCell;

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
        public IEnumerator TheMouseDragsTheIgniterOntoTheCutter_At1920x1080()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            yield return Drag(Center(HoldCell()), Center(FaceLayout.DeviceHitRect(FaceDevices.Cutter)));
            yield return AssertFitted("the drag fits the igniter at 1920x1080");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheMouseDragsTheIgniterOntoTheCutter_At2560x1440()
        {
            yield return OpenWithTheIgniter(Shapes[1]);
            yield return Drag(Center(HoldCell()), Center(FaceLayout.DeviceHitRect(FaceDevices.Cutter)));
            yield return AssertFitted("the drag fits the igniter at 2560x1440");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ADragRightAsTheScreenOpens_FitsToo()
        {
            yield return OpenWithTheIgniter(Shapes[0], QuickSeconds);
            yield return Drag(Center(HoldCell()), Center(FaceLayout.DeviceHitRect(FaceDevices.Cutter)));
            yield return AssertFitted("a player who drags the moment the screen is open still fits the igniter");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AClickOnTheIgniter_ThenAClickOnTheCutter_FitsIt()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            yield return Click(Center(HoldCell()));
            Assert.AreEqual(_igniterCell, _face.FocusedHoldCell, "a click selects the igniter in the hold");
            Assert.IsFalse(_gameplay.StartKit.IgniterFitted, "a click on the igniter alone fits nothing");
            yield return Click(Center(FaceLayout.DeviceHitRect(FaceDevices.Cutter)));
            yield return AssertFitted("the click on the cutter fits the selected igniter");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AClickOnTheIgniter_ThenEnterOrE_FitsIt()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            yield return Click(Center(HoldCell()));
            Assert.AreEqual(_igniterCell, _face.FocusedHoldCell, "a click selects the igniter in the hold");
            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            yield return AssertFitted("E on the selected igniter fits it");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ADropAnywhereOnTheDevicePanel_GoesToTheCutter()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            Vector2 anywhere = Center(FaceLayout.DeviceHitRect(FaceDevices.Scan));
            yield return Drag(Center(HoldCell()), anywhere);
            yield return AssertFitted("a drop on another device of the panel goes to the cutter");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheScreenOpensWithTheIgniterSelected_AndTheHintSaysHow()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            Assert.AreEqual(_igniterCell, _face.FocusedHoldCell, "the screen opens with the igniter selected");
            StringAssert.Contains("E", _face.TooltipDescText, "and its hint says to drag it or press E");
            Assert.IsTrue(_face.IsIgniterOffered, "the cutter offers it");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheIgniterIsAFieldSwap_ItFitsWithNoPadAnywhere()
        {
            yield return OpenWithTheIgniter(Shapes[0], BootSettleSeconds, pad: false);
            Assert.IsFalse(_gameplay.PadLock.OnPad, "07 stands nowhere near a pad");
            Assert.AreEqual(_igniterCell, _face.FocusedHoldCell, "the screen opens with the igniter selected");
            yield return Drag(Center(HoldCell()), Center(FaceLayout.DeviceHitRect(FaceDevices.Cutter)));
            yield return AssertFitted("with no pad at all the igniter fits");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheCutterSocket_IsFullyOnScreen_AndClearOfTheHudsText_At16x9()
        {
            yield return OpenWithTheIgniter(Shapes[0]);
            foreach (Vector2Int shape in Shapes)
            {
                _view.targetTexture = null;
                _target.Release();
                Object.DestroyImmediate(_target);
                _target = new RenderTexture(shape.x, shape.y, 24);
                _view.targetTexture = _target;
                yield return RealRoverScene.WaitSeconds(0.3f);

                Rect frame = FaceLayout.DeviceHitRect(FaceDevices.Cutter);
                Vector2[] corners =
                {
                    new Vector2(frame.xMin, frame.yMin), new Vector2(frame.xMax, frame.yMin),
                    new Vector2(frame.xMin, frame.yMax), new Vector2(frame.xMax, frame.yMax),
                };
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (Vector2 corner in corners)
                {
                    Assert.IsTrue(ToScreen(corner, out Vector2 screen), $"{shape}: the cutter's corner {corner} is on the glass");
                    Assert.That(screen.x, Is.InRange(0f, shape.x), $"{shape}: the cutter is within the screen's width");
                    Assert.That(screen.y, Is.InRange(0f, shape.y), $"{shape}: the cutter is within the screen's height");
                    minX = Mathf.Min(minX, screen.x);
                    maxX = Mathf.Max(maxX, screen.x);
                    minY = Mathf.Min(minY, screen.y);
                    maxY = Mathf.Max(maxY, screen.y);
                }

                // The HUD's text, as shares of its panel, against the cutter's, as shares of the frame (screen
                // pixels have a bottom-left origin; a panel's top-left).
                var cutter = new Rect(minX / shape.x, 1f - maxY / shape.y, (maxX - minX) / shape.x,
                    (maxY - minY) / shape.y);
                foreach (UIDocument document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                {
                    if (document.panelSettings == null || document.panelSettings.targetTexture != null ||
                        document.rootVisualElement == null || document.rootVisualElement.panel == null)
                    {
                        continue;
                    }

                    Rect panel = document.rootVisualElement.worldBound;
                    foreach (Label label in document.rootVisualElement.Query<Label>().ToList())
                    {
                        if (string.IsNullOrEmpty(label.text) || label.resolvedStyle.display == DisplayStyle.None ||
                            label.resolvedStyle.opacity < 0.05f || !label.visible || label.worldBound.width <= 1f)
                        {
                            continue;
                        }

                        Rect share = new Rect(label.worldBound.x / panel.width, label.worldBound.y / panel.height,
                            label.worldBound.width / panel.width, label.worldBound.height / panel.height);
                        Assert.IsFalse(share.Overlaps(cutter),
                            $"{shape}: the HUD text '{label.text}' ({share}) lies over the cutter socket ({cutter})");
                    }
                }
            }
        }

        /// <summary>The scene booted, the pad built, 07 parked on it with the igniter in the hold, the face open.</summary>
        private IEnumerator OpenWithTheIgniter(Vector2Int shape, float settleSeconds = BootSettleSeconds,
            bool pad = true)
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            _view = _scene.View;
            _gameplay = Object.FindFirstObjectByType<GameplaySystem>();
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _face = _scene.Ui.Cargo.View;
            _screen = _scene.Context.Get<IRoverFaceScreen>();

            var terrain = _scene.Context.Get<ITerrainQuery>();
            Assert.IsTrue(_scene.Context.Get<IWorldAnchors>().TryGet("place.apron", out MoonProject.Core.WorldAnchor apron),
                "the World has the apron");
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            Assert.IsTrue(_gameplay.Materials.Hold.TryAddPart(_gameplay.StartKit.IgniterPartId),
                "the igniter goes into the hold");
            if (pad)
            {
                _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                    new StructurePose(centre, PadYaw), 1f);
                _scene.Context.Get<IRoverPlacement>().PlaceAt(centre,
                    Quaternion.LookRotation(Vector3.forward, Vector3.up));
                yield return RealRoverScene.WaitSeconds(DockSeconds);
                Assert.IsTrue(_gameplay.PadLock.OnPad, "07 stands on its pad");
            }

            Assert.IsTrue(_gameplay.StartKit.CanFitIgniter, "the igniter in the hold can be fitted");

            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!_screen.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the inventory on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(settleSeconds);
            _igniterCell = FindIgniterCell();
            _view.targetTexture = null;
            _target = new RenderTexture(shape.x, shape.y, 24);
            _view.targetTexture = _target;
            yield return RealRoverScene.WaitSeconds(0.3f);
        }

        private const float PadYaw = 180f;

        /// <summary>Seconds a hurried player waits after the screen opens before acting.</summary>
        private const float QuickSeconds = 0.2f;

        private int FindIgniterCell()
        {
            for (int i = 0; i < _face.UnlockedCellCount; i++)
            {
                if (_face.HoldKind(i) == HoldCellKind.StoryPart)
                {
                    return i;
                }
            }

            Assert.Fail("the igniter is in the hold's cells");
            return -1;
        }

        private Rect HoldCell()
        {
            return FaceInventoryView.GetHoldCellReferenceRect(_igniterCell);
        }

        private static Vector2 Center(Rect rect)
        {
            return rect.center;
        }

        private bool ToScreen(Vector2 panelPoint, out Vector2 screen)
        {
            return FaceScreenMapper.TryPanelToScreen(panelPoint, _view, _screen, FaceScreenMapper.DefaultPanelSize,
                out screen);
        }

        private IEnumerator Drag(Vector2 from, Vector2 to)
        {
            Assert.IsTrue(ToScreen(from, out Vector2 start), $"the igniter's cell {from} is on screen");
            Assert.IsTrue(ToScreen(to, out Vector2 end), $"the drop point {to} is on screen");
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

            yield return null;
            Release(_mouse.leftButton);
            yield return null;
        }

        private IEnumerator Click(Vector2 at)
        {
            Assert.IsTrue(ToScreen(at, out Vector2 screen), $"{at} is on screen");
            Set(_mouse.position, screen);
            yield return null;
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            Release(_mouse.leftButton);
            yield return null;
            yield return null;
        }

        private IEnumerator AssertFitted(string why)
        {
            float deadline = Time.time + FitTimeout;
            while (!_gameplay.StartKit.IgniterFitted && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_gameplay.StartKit.IgniterFitted,
                $"{why} (drag phase {_face.DragPhase}, cell {_igniterCell}, can fit {_gameplay.StartKit.CanFitIgniter})");
        }
    }
}
