using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
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
    /// and the databank; and the call on the face at rest (broken, nothing decoded) and after the first charge (locked,
    /// its first letter in the accent), and at night clear and then answered. Writes Logs/captures/face_a/.
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

        // The wake's parts as rover now times them (PostSettings), and the strip's beat.
        private const float WakePowerOnSeconds = 1.2f;
        private const float WakeLogoSeconds = 5f;
        private const float WakeLoadSeconds = 2.4f;
        private const float StripEverySeconds = 0.25f;
        private const int StripColumns = 6;
        private const int TileWidth = 384;
        private const int TileHeight = 256;

        // From in front of 07 and a little to its side, so the CRT face reads (as the face-inventory captures).
        private const float FrontBearing = 162f;

        // The sky's clock in the earthlit night (SkyPhase.Earthlit).
        private const float NightClockSeconds = 4800f;

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

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator LookA_TheCallOnTheRealFace_BrokenAtRest_ThenLockedWithItsFirstLetter()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return _scene.Boot();
            _target = new RenderTexture(Width, Height, 24);
            _scene.View.targetTexture = _target;
            CallDisplayView call = _scene.Ui.CallView;
            // The still call is the quietest guidance: with no objective line wanting the slot, it shows at rest.
            _scene.Context.Events.Publish(new NextStepChanged(string.Empty));
            float deadline = Time.time + OpenTimeout;
            while (!call.IsVisible)
            {
                Assert.Less(Time.time, deadline, "standing still, 07's face shows the call");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(1f);
            Assert.AreEqual("??? ??", call.Letters, "on the reserve nothing has decoded");
            Capture("call_broken");
            CaptureFace("call_broken_panel");

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            yield return RealRoverScene.WaitSeconds(1f);
            Assert.IsTrue(call.IsVisible);
            Assert.AreEqual("A?? ??", call.Letters, "the first charge decodes one letter");
            Assert.AreEqual(1, call.AccentLetters, "the one accent");
            Capture("call_locked");
            CaptureFace("call_locked_panel");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator LookA_TheCallAtNight_ClearThenAnswered_OnTheRealFace()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return _scene.Boot();
            _target = new RenderTexture(Width, Height, 24);
            _scene.View.targetTexture = _target;
            SetNight();
            _scene.Context.Events.Publish(new NextStepChanged(string.Empty));
            _scene.CameraRig.Orbit.AdoptBearing(_scene.Rover.Heading + FrontBearing);
            CallDisplayView call = _scene.Ui.CallView;

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            yield return UntilCall(call, CallState.Clear, "A?? ??");
            Capture("call_clear_night");
            CaptureFace("call_clear_panel");

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.CallAnswered, false));
            yield return UntilCall(call, CallState.Answered, "ARK ??");
            Assert.AreEqual(3, call.AccentLetters, "the answer decodes two more letters into the accent");
            Capture("call_answered_night");
            CaptureFace("call_answered_panel");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheWakeOn07sFace_AsAStrip_EveryQuarterSecond()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return _scene.Boot();
            var face = _scene.Ui.FaceTexture as RenderTexture;
            Assert.IsNotNull(face, "the UI draws 07's face into a render texture");
            var tiles = new List<Texture2D>();
            float[] parts = { WakePowerOnSeconds, WakeLogoSeconds, WakeLoadSeconds };
            float clock = 0f;
            float nextShot = 0f;
            for (int part = 0; part < parts.Length; part++)
            {
                if (part == 0)
                {
                    _scene.Context.Events.Publish(new CrtPowerOn(parts[part]));
                }
                else if (part == 1)
                {
                    _scene.Context.Events.Publish(new BootLogoBegan(parts[part]));
                }
                else
                {
                    _scene.Context.Events.Publish(new BootLoadBegan(parts[part]));
                }

                float end = clock + parts[part];
                while (clock < end - 1e-4f)
                {
                    yield return null;
                    clock += FrameTime;
                    if (clock + 1e-4f >= nextShot)
                    {
                        tiles.Add(Tile(face));
                        nextShot += StripEverySeconds;
                    }
                }
            }

            Texture2D strip = Grid(tiles);
            try
            {
                FrameCapture.WritePng(strip, Path.Combine(Folder, "wake_face_strip.png"));
            }
            finally
            {
                Object.DestroyImmediate(strip);
                foreach (Texture2D tile in tiles)
                {
                    Object.DestroyImmediate(tile);
                }
            }

            Assert.AreEqual(Mathf.CeilToInt((WakePowerOnSeconds + WakeLogoSeconds + WakeLoadSeconds) /
                                            StripEverySeconds), tiles.Count, 1, "a frame every quarter second");
        }

        /// <summary>The face panel now, small, as one tile of the strip.</summary>
        private static Texture2D Tile(RenderTexture face)
        {
            RenderTexture small = RenderTexture.GetTemporary(TileWidth, TileHeight, 0);
            RenderTexture previous = RenderTexture.active;
            var tile = new Texture2D(TileWidth, TileHeight, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(face, small);
                RenderTexture.active = small;
                tile.ReadPixels(new Rect(0f, 0f, TileWidth, TileHeight), 0, 0);
                tile.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(small);
            }

            return tile;
        }

        /// <summary>The tiles in reading order, <see cref="StripColumns"/> to a row, on the face's dark.</summary>
        private static Texture2D Grid(List<Texture2D> tiles)
        {
            int rows = (tiles.Count + StripColumns - 1) / StripColumns;
            var grid = new Texture2D(StripColumns * TileWidth, rows * TileHeight, TextureFormat.RGBA32, false);
            var dark = new Color[grid.width * grid.height];
            for (int i = 0; i < dark.Length; i++)
            {
                dark[i] = Color.black;
            }

            grid.SetPixels(dark);
            for (int i = 0; i < tiles.Count; i++)
            {
                int column = i % StripColumns;
                int row = rows - 1 - i / StripColumns;
                Color[] pixels = tiles[i].GetPixels();
                for (int p = 0; p < pixels.Length; p++)
                {
                    // The panel draws on a clear background: over black, as the CRT shows it.
                    pixels[p] = new Color(pixels[p].r * pixels[p].a, pixels[p].g * pixels[p].a,
                        pixels[p].b * pixels[p].a, 1f);
                }

                grid.SetPixels(column * TileWidth, row * TileHeight, TileWidth, TileHeight, pixels);
            }

            grid.Apply();
            return grid;
        }

        /// <summary>The sky's clock set to the earthlit night (the sky exposes no setter on its Core interface).</summary>
        private void SetNight()
        {
            ISunlight sun = _scene.Context.Get<ISunlight>();
            MethodInfo setClockTime = sun.GetType().GetMethod("SetClockTime", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(setClockTime, "the sky can be set to a time of day");
            setClockTime.Invoke(sun, new object[] { NightClockSeconds, false, true });
        }

        /// <summary>Waits until the face shows the call in <paramref name="state"/> with <paramref name="letters"/>.</summary>
        private IEnumerator UntilCall(CallDisplayView call, CallState state, string letters)
        {
            float deadline = Time.time + OpenTimeout;
            while (!call.IsVisible || _scene.Ui.Call.State != state || call.Letters != letters)
            {
                Assert.Less(Time.time, deadline, $"the face shows the call {state} with {letters}");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(1f);
            Assert.IsTrue(call.IsVisible, "and holds it");
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

        /// <summary>The face panel itself, as the UI hands it to 07's CRT (before the glass and its shader).</summary>
        private void CaptureFace(string name)
        {
            var face = _scene.Ui.FaceTexture as RenderTexture;
            Assert.IsNotNull(face, "the UI draws 07's face into a render texture");
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(face.width, face.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = face;
                image.ReadPixels(new Rect(0f, 0f, face.width, face.height), 0, 0);
                image.Apply();
                FrameCapture.WritePng(image, Path.Combine(Folder, name + ".png"));
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
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
