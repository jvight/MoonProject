using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Gameplay;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Look A on 07's real face at 1920x1080, with 07 parked on its built pad, the spare igniter and some metal in the
    /// hold and the real mouse: the hold as it opens (the igniter selected, the cutter lit, the igniter's tag), the
    /// same at rest with nothing selected (the cutter's accent brackets), the igniter dragged over the cutter
    /// ("Release to fit", the other devices dimmed), metal dragged over a socket (it fits nowhere: every device dims)
    /// and the databank. Writes Logs/captures/face_a/.
    /// </summary>
    public sealed class RoverFaceLookCaptures : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 3f;
        private const float DockSeconds = 4f;
        private const float FlipTimeout = 3f;
        private const float PadYaw = 180f;
        private const int DragSteps = 10;
        private const int MetalUnits = 5;
        private const int Width = 1920;
        private const int Height = 1080;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "face_a"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
        private Mouse _mouse;
        private RenderTexture _target;
        private FaceInventoryView _face;
        private IRoverFaceScreen _screen;

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
        public IEnumerator LookA_TheBaysSayWhereAPartGoes_OnTheRealFace()
        {
            Directory.CreateDirectory(Folder);
            yield return OpenOnThePad();
            int igniter = FindCell(HoldCellKind.StoryPart);
            int metal = FindCell(HoldCellKind.Material);

            Assert.AreEqual(igniter, _face.FocusedHoldCell, "the hold opens on the igniter");
            Assert.AreEqual(BayLook.Valid, _face.BayLookOf(FaceDevices.Cutter), "and the cutter lights for it");
            Assert.IsNotEmpty(_face.HoldTagText(igniter), "the igniter's cell says where it goes");
            Capture("hold_open_igniter");

            _face.ClearFocus();
            yield return RealRoverScene.WaitSeconds(0.5f);
            Assert.AreEqual(BayLook.Ready, _face.BayLookOf(FaceDevices.Cutter), "nothing selected: accent brackets");
            Assert.AreEqual(BayLook.Rest, _face.BayLookOf(FaceDevices.SocketOne));
            Capture("hold_rest");

            yield return DragTo(FaceLayout.HoldCellRect(igniter).center,
                FaceLayout.DeviceFrame(FaceDevices.Cutter).center);
            Assert.AreEqual(BayLook.Hover, _face.BayLookOf(FaceDevices.Cutter), "over the cutter it thickens");
            Assert.AreEqual(BayLook.Invalid, _face.BayLookOf(FaceDevices.SocketOne), "the sockets dim");
            Assert.IsNotEmpty(_face.ReleaseText, "and the part says to release it");
            Capture("drag_igniter_over_cutter");
            yield return MoveTo(FaceLayout.HoldCellRect(igniter).center);
            Release(_mouse.leftButton);
            yield return RealRoverScene.WaitSeconds(1f);

            yield return DragTo(FaceLayout.HoldCellRect(metal).center,
                FaceLayout.DeviceFrame(FaceDevices.SocketOne).center);
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Assert.AreEqual(BayLook.Invalid, _face.BayLookOf(device), $"metal fits no device ({device})");
            }

            Assert.IsEmpty(_face.ReleaseText, "nothing says release");
            Capture("drag_metal_over_socket");
            yield return MoveTo(FaceLayout.HoldCellRect(metal).center);
            Release(_mouse.leftButton);
            yield return RealRoverScene.WaitSeconds(1f);

            Press(_keyboard.qKey);
            yield return null;
            Release(_keyboard.qKey);
            float flip = Time.time + FlipTimeout;
            while (_scene.Ui.Pages.Page != FacePage.Databank || _scene.Ui.Pages.IsFlipping)
            {
                Assert.Less(Time.time, flip, "Q flips to the databank");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            Assert.IsNotEmpty(_scene.Ui.Databank.View.NameText, "the entry's name heads its words");
            Capture("databank");
        }

        /// <summary>The pad built, 07 parked on it with the igniter and metal in the hold, the face open at 1080p.</summary>
        private IEnumerator OpenOnThePad()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            GameplaySystem gameplay = Object.FindFirstObjectByType<GameplaySystem>();
            Assert.IsNotNull(gameplay, "the scene has the gameplay system");
            _face = _scene.Ui.Cargo.View;
            _screen = _scene.Context.Get<IRoverFaceScreen>();

            var terrain = _scene.Context.Get<ITerrainQuery>();
            Assert.IsTrue(_scene.Context.Get<IWorldAnchors>().TryGet("place.apron", out MoonProject.Core.WorldAnchor apron),
                "the World has the apron");
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            gameplay.PlacedBase.PlaceUnchecked(gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                new StructurePose(centre, PadYaw), 1f);
            Assert.IsTrue(gameplay.Materials.Hold.TryAddPart(gameplay.StartKit.IgniterPartId),
                "the igniter goes into the hold");
            gameplay.Materials.Hold.Add(SalvageMaterial.Metal, MetalUnits);
            _scene.Context.Get<IRoverPlacement>().PlaceAt(centre, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return RealRoverScene.WaitSeconds(DockSeconds);
            Assert.IsTrue(gameplay.StartKit.CanFitIgniter, "07 stands on its pad with the igniter in the hold");

            _target = new RenderTexture(Width, Height, 24);
            _scene.View.targetTexture = _target;
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!_screen.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the hold on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(SettleSeconds);
        }

        private int FindCell(HoldCellKind kind)
        {
            for (int i = 0; i < _face.UnlockedCellCount; i++)
            {
                if (_face.HoldKind(i) == kind)
                {
                    return i;
                }
            }

            Assert.Fail($"the hold has a {kind}");
            return -1;
        }

        private Vector2 ToScreen(Vector2 panelPoint)
        {
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(panelPoint, _scene.View, _screen,
                FaceScreenMapper.DefaultPanelSize, out Vector2 screen), $"{panelPoint} is on the glass");
            return screen;
        }

        /// <summary>Presses on <paramref name="from"/> and drags to <paramref name="to"/> (panel points), held.</summary>
        private IEnumerator DragTo(Vector2 from, Vector2 to)
        {
            Vector2 start = ToScreen(from);
            Vector2 end = ToScreen(to);
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

            Assert.AreEqual(HoldDragPhase.Dragging, _face.DragPhase, "the part is being dragged");
            yield return RealRoverScene.WaitSeconds(0.6f);
        }

        private IEnumerator MoveTo(Vector2 panelPoint)
        {
            Set(_mouse.position, ToScreen(panelPoint));
            yield return RealRoverScene.WaitSeconds(0.2f);
        }

        private void Capture(string name)
        {
            Texture2D frame = FrameCapture.Render(_scene.View, Width, Height);
            try
            {
                FrameCapture.WritePng(frame, Path.Combine(Folder, name + "_1920x1080.png"));
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }
    }
}
