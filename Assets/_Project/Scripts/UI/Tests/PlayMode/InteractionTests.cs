using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The interaction pass end to end with real Input System devices (owner feedback 2026-10-18): the cursor shows on
    /// every screen the mouse can use and hides while driving, the mouse drags the spare igniter onto the cutter on 07's
    /// screen (fitting it on the pad, easing it home with a reason off it), and the Build key hands the screen over to
    /// build mode.
    /// </summary>
    public sealed class InteractionTests : InputTestFixture
    {
        private const float Settle = 0.6f;

        private string _slot;
        private InputActionAsset _controls;
        private UiTestRig _rig;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
        }

        public override void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            UiTestRig.DeleteSlotFiles(_slot);
            Object.Destroy(_controls);
            base.TearDown();
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        [UnityTest]
        public IEnumerator TheCursor_ShowsOnTheTitleThePauseMenuAndTheFaceScreen_AndHidesWhileDriving()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            _rig = UiTestRig.Boot(_controls, _slot);
            yield return null;

            Events.Publish(new TitleShown(true));
            yield return Seconds(0.3f);
            AssertCursorFree("the title");

            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = _rig.Ui.Layout.ResumeButton;
                _rig.Ui.Layout.ResumeButton.SendEvent(submit);
            }

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(Settle);
            AssertCursorCaptured("driving");

            yield return Tap(keyboard.escapeKey);
            AssertCursorFree("the pause menu");
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(Settle);
            AssertCursorCaptured("driving again");

            _rig.Ui.Ledger.RecordHoldBooted();
            yield return Tap(keyboard.tabKey);
            AssertCursorFree("07's face screen");
            Assert.AreEqual(CursorLockMode.Confined, _rig.Ui.Cursor.LockState, "kept inside the window for a drag");
            yield return Tap(keyboard.tabKey);
            AssertCursorCaptured("driving after the face screen");
        }

        [UnityTest]
        public IEnumerator DraggingTheIgniter_OffThePad_EasesItHomeWithAReason_AndOnThePad_FitsIt()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(7, new MaterialSlot(SalvageMaterial.Metal, 3, 4));
            _rig.Fakes.Hold.AddPart(DropRule.SpareIgniterPart);
            var cues = new List<UiCueKind>();
            Events.Subscribe<UiCue>(cue => cues.Add(cue.Kind));
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            FaceInventoryView view = _rig.Ui.Cargo.View;
            Assert.AreEqual(HoldCellKind.StoryPart, view.HoldKind(1), "the igniter sits in the second slot");

            yield return Drag(mouse, CellOnScreen(1), DeviceOnScreen(FaceDevices.Cutter), view);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits, "off the pad the cutter does not take it");
            Assert.AreEqual(HoldDragPhase.Returning, view.DragPhase, "the icon eases home");
            CollectionAssert.Contains(cues, UiCueKind.PartPick, "the drag lifts it with a tick");
            CollectionAssert.Contains(cues, UiCueKind.PartRefuse, "and the drop is refused quietly");
            Assert.AreEqual(1, view.FocusedHoldCell, "the igniter stays selected");
            Assert.AreEqual(Text(UiKeys.CargoDragNotDocked), view.TooltipDescText, "the info box says why");
            yield return Seconds(_rig.Tuning.Cargo.DragReturnSeconds + 0.1f);
            Assert.AreEqual(HoldDragPhase.Idle, view.DragPhase, "home");

            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.2f);
            cues.Clear();
            yield return Drag(mouse, CellOnScreen(1), DeviceOnScreen(FaceDevices.Cutter), view);
            Assert.AreEqual(1, _rig.Fakes.Systems.Fits, "on the pad the cutter takes the dropped igniter");
            CollectionAssert.Contains(cues, UiCueKind.PartDrop);
            Assert.AreEqual(FaceDevices.Cutter, view.FocusedDeviceIndex, "the cutter is selected, now fitted");
            Assert.AreEqual(Text("ui.device.state.ok"), view.DeviceStateText(FaceDevices.Cutter));
        }

        /// <summary>
        /// Where a part can go reads at a glance (owner, 2026-10-19): at rest the cutter that can take the igniter now
        /// wears the accent brackets and the igniter's cell says where it goes; selected from the keys the igniter
        /// lights the cutter; dragged with the real mouse it lights the cutter, thickens it under the pointer with
        /// "Release to fit" and dims every other device; off the pad the cutter stays quiet and says why.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBays_SayWhereAPartGoes_AtRest_FromTheKeys_AndUnderTheMouse()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(7, new MaterialSlot(SalvageMaterial.Metal, 3, 4));
            _rig.Fakes.Hold.AddPart(DropRule.SpareIgniterPart);
            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            FaceInventoryView view = _rig.Ui.Cargo.View;
            string cutter = Text(FaceDevices.NameKey(FaceDevices.Cutter));

            view.ClearFocus();
            yield return null;
            Assert.AreEqual(BayLook.Ready, view.BayLookOf(FaceDevices.Cutter), "the cutter can take the igniter now");
            Assert.AreEqual(BayLook.Rest, view.BayLookOf(FaceDevices.SocketOne), "an empty socket is a quiet bay");
            Assert.AreEqual(BayLook.None, view.BayLookOf(FaceDevices.Scan), "the working lens takes nothing");
            StringAssert.EndsWith(cutter, view.HoldTagText(1), "the igniter's cell says where it goes");
            Assert.IsEmpty(view.HoldTagText(0), "metal has no device to go to");
            Assert.AreEqual(Text("ui.device.state.socket_empty"), view.DeviceStateText(FaceDevices.SocketOne));

            view.FocusHoldCell(1);
            yield return null;
            Assert.AreEqual(BayLook.Valid, view.BayLookOf(FaceDevices.Cutter), "selected, the igniter lights the cutter");
            Assert.AreEqual(BayLook.Rest, view.BayLookOf(FaceDevices.SocketOne), "and dims nothing from the keys");
            Assert.AreEqual(cutter, view.UseText, "the info box says where it goes");
            view.FocusHoldCell(0);
            yield return null;
            Assert.AreEqual(Text(UiKeys.FaceBuild), view.UseText, "metal goes into builds");

            yield return DragTo(mouse, CellOnScreen(1), DeviceOnScreen(FaceDevices.Cutter), view);
            Assert.AreEqual(BayLook.Hover, view.BayLookOf(FaceDevices.Cutter), "over the cutter it thickens");
            Assert.AreEqual(BayLook.Invalid, view.BayLookOf(FaceDevices.SocketOne), "the sockets dim");
            Assert.AreEqual(BayLook.Invalid, view.BayLookOf(FaceDevices.Scan), "and so does the lens");
            Assert.AreEqual(Text(UiKeys.CargoReleaseFit), view.ReleaseText);
            Assert.Less(view.DeviceElement(FaceDevices.SocketOne).resolvedStyle.opacity, 0.5f, "dimmed to about 40%");

            yield return MoveTo(mouse, DeviceOnScreen(FaceDevices.SocketOne));
            Assert.AreEqual(BayLook.Valid, view.BayLookOf(FaceDevices.Cutter), "away from it the cutter still lights");
            Assert.IsEmpty(view.ReleaseText, "and nothing says release over a socket");
            Release(mouse.leftButton);
            yield return Seconds(_rig.Tuning.Cargo.DragReturnSeconds + 0.1f);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits, "a socket does not take the igniter");

            _rig.Fakes.Systems.CanFitIgniter = false;
            yield return Seconds(0.2f);
            yield return DragTo(mouse, CellOnScreen(1), DeviceOnScreen(FaceDevices.Cutter), view);
            Assert.AreEqual(BayLook.Blocked, view.BayLookOf(FaceDevices.Cutter), "off the pad it stays quiet, undimmed");
            Assert.AreEqual(Text(UiKeys.CargoDragNotDocked), view.ReleaseText, "and says why beside the part");
            Release(mouse.leftButton);
            yield return Seconds(_rig.Tuning.Cargo.DragReturnSeconds + 0.1f);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits);
        }

        [UnityTest]
        public IEnumerator DraggingAMaterial_GoesNowhere_AndAClickStillSelects()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(7, new MaterialSlot(SalvageMaterial.Metal, 3, 4));
            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            FaceInventoryView view = _rig.Ui.Cargo.View;

            Set(mouse.position, CellOnScreen(3));
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;
            Assert.AreEqual(3, view.FocusedHoldCell, "a click selects");
            Assert.AreEqual(HoldDragPhase.Idle, view.DragPhase, "a click is no drag");

            yield return Drag(mouse, CellOnScreen(0), DeviceOnScreen(FaceDevices.Cutter), view);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits, "metal fits no device");
            Assert.AreEqual(Text(UiKeys.CargoDragNowhere), view.TooltipDescText);
            Assert.AreEqual(HoldDragPhase.Returning, view.DragPhase);
        }

        [UnityTest]
        public IEnumerator TheBuildKey_OnTheFaceScreen_ClosesIt()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.3f);
            Assert.IsTrue(_rig.Ui.Cargo.IsOpen);

            yield return Tap(keyboard.bKey);
            Assert.IsFalse(_rig.Ui.Cargo.IsOpen, "B closes the screen for build mode");
            Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the rover controls are back");
            AssertCursorCaptured("no build field here, so back to driving");
        }

        /// <summary>
        /// The drag on 07's screen as the owner will see it, over the face's own dark: <c>face-drag-mid.png</c> (the
        /// igniter's icon over the cutter, its slot dimmed), <c>face-drag-return.png</c> (easing home off the pad, the
        /// info box saying why) and <c>face-drag-fit.png</c> (on the pad, easing into the cutter). PNGs land in
        /// Logs/captures.
        /// </summary>
        [UnityTest]
        [Explicit("Writes the drag captures; run with --filter FaceDragCaptures.")]
        public IEnumerator FaceDragCaptures()
        {
            const int width = 1600;
            const int height = 900;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot, width, height);
            _rig.Ui.Ledger.RecordHoldBooted();
            _rig.Ui.Localization.SetLanguage("vi");
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(7, new MaterialSlot(SalvageMaterial.Metal, 3, 4));
            _rig.Fakes.Hold.AddPart(DropRule.SpareIgniterPart);
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            FaceInventoryView view = _rig.Ui.Cargo.View;
            float scale = _rig.Ui.Cargo.PanelRect.width / FaceInventoryView.ReferenceWidth;
            Vector2 from = FaceInventoryView.GetHoldCellReferenceRect(1).center * scale;
            Vector2 to = FaceDevices.CenterOf(FaceDevices.Cutter) * scale;

            yield return null;
            view.PointerDownAtPanel(from);
            for (int i = 1; i <= 10; i++)
            {
                yield return null;
                view.PointerMoveAtPanel(Vector2.Lerp(from, to, i / 10f));
            }

            yield return Seconds(0.4f);
            Assert.AreEqual(HoldDragPhase.Dragging, view.DragPhase);
            yield return CaptureFrame("face-drag-mid");

            view.PointerUpAtPanel(to);
            yield return Seconds(_rig.Tuning.Cargo.DragReturnSeconds * 0.4f);
            Assert.AreEqual(HoldDragPhase.Returning, view.DragPhase);
            yield return CaptureFrame("face-drag-return");
            yield return Seconds(_rig.Tuning.Cargo.DragReturnSeconds);
            yield return CaptureFrame("face-drag-reason");

            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.3f);
            view.PointerDownAtPanel(from);
            for (int i = 1; i <= 10; i++)
            {
                yield return null;
                view.PointerMoveAtPanel(Vector2.Lerp(from, to, i / 10f));
            }

            yield return Seconds(0.3f);
            view.PointerUpAtPanel(to);
            yield return Seconds(_rig.Tuning.Cargo.DragFitSeconds * 0.4f);
            Assert.AreEqual(HoldDragPhase.Fitting, view.DragPhase);
            yield return CaptureFrame("face-drag-fit");
            yield return Seconds(0.6f);
            yield return CaptureFrame("face-drag-fitted");
        }

        /// <summary>The UI frame over the face's own dark, as a PNG in Logs/captures.</summary>
        private IEnumerator CaptureFrame(string name)
        {
            yield return null;
            RenderTexture frame = _rig.Frame;
            var image = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = frame;
            image.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0, false);
            image.Apply(false);
            RenderTexture.active = previous;
            Color32 backdrop = _rig.Tuning.Cargo.PlateBackground;
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 ui = pixels[i];
                int keep = 255 - ui.a;
                pixels[i] = new Color32((byte)Mathf.Min(255, ui.r + backdrop.r * keep / 255),
                    (byte)Mathf.Min(255, ui.g + backdrop.g * keep / 255),
                    (byte)Mathf.Min(255, ui.b + backdrop.b * keep / 255), 255);
            }

            image.SetPixels32(pixels);
            image.Apply(false);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
            Directory.CreateDirectory(folder);
            FrameCapture.WritePng(image, Path.Combine(folder, name + ".png"));
            Object.Destroy(image);
        }

        /// <summary>
        /// The cursor the UI asks for. A batch-mode player has no window, so Unity keeps the OS cursor unlocked whatever
        /// is asked; the policy is what the game writes to it.
        /// </summary>
        private void AssertCursorFree(string where)
        {
            Assert.IsTrue(_rig.Ui.Cursor.IsVisible, where + ": the cursor shows");
            Assert.AreNotEqual(CursorLockMode.Locked, _rig.Ui.Cursor.LockState, where + ": the cursor is free");
        }

        private void AssertCursorCaptured(string where)
        {
            Assert.IsFalse(_rig.Ui.Cursor.IsVisible, where + ": the cursor hides");
            Assert.AreEqual(CursorLockMode.Locked, _rig.Ui.Cursor.LockState, where + ": the mouse turns the camera");
        }

        /// <summary>A real mouse drag: down on <paramref name="from"/>, a few steps across, up on <paramref name="to"/>.</summary>
        private IEnumerator Drag(Mouse mouse, Vector2 from, Vector2 to, FaceInventoryView view)
        {
            const int steps = 8;
            Set(mouse.position, from);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            for (int i = 1; i <= steps; i++)
            {
                Set(mouse.position, Vector2.Lerp(from, to, i / (float)steps));
                yield return null;
            }

            Assert.AreEqual(HoldDragPhase.Dragging, view.DragPhase, "the part is being dragged");
            yield return Seconds(0.2f);
            Release(mouse.leftButton);
            yield return null;
        }

        /// <summary>Presses on <paramref name="from"/> and drags to <paramref name="to"/>, still holding the button.</summary>
        private IEnumerator DragTo(Mouse mouse, Vector2 from, Vector2 to, FaceInventoryView view)
        {
            const int steps = 8;
            Set(mouse.position, from);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            for (int i = 1; i <= steps; i++)
            {
                Set(mouse.position, Vector2.Lerp(from, to, i / (float)steps));
                yield return null;
            }

            Assert.AreEqual(HoldDragPhase.Dragging, view.DragPhase, "the part is being dragged");
            yield return Seconds(0.2f);
        }

        private IEnumerator MoveTo(Mouse mouse, Vector2 to)
        {
            Set(mouse.position, to);
            yield return Seconds(0.2f);
        }

        private Vector2 CellOnScreen(int cell)
        {
            return ReferenceOnScreen(FaceInventoryView.GetHoldCellReferenceRect(cell).center);
        }

        private Vector2 DeviceOnScreen(int device)
        {
            return ReferenceOnScreen(FaceDevices.CenterOf(device));
        }

        /// <summary>A point of the 1152x768 face frame in Input System screen pixels (bottom-left origin).</summary>
        private Vector2 ReferenceOnScreen(Vector2 reference)
        {
            Rect panel = _rig.Ui.Cargo.PanelRect;
            Vector2 panelPoint = panel.position + reference * (panel.width / FaceInventoryView.ReferenceWidth);
            float scale = Screen.width / _rig.Ui.Layout.Root.layout.width;
            return new Vector2(panelPoint.x * scale, Screen.height - panelPoint.y * scale);
        }

        private string Text(string key)
        {
            return _rig.Ui.Localization.Get(key);
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }
    }
}
