using NUnit.Framework;
using UnityEngine;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The interaction pass's pure rules: the cursor shows on every screen the mouse can click and hides while the mouse
    /// turns the camera; build mode's guidance line names one step at a time; the menu's keys name real controls; a
    /// dragged part fits only where it belongs and otherwise goes home with a reason.
    /// </summary>
    public sealed class InteractionRulesTests
    {
        [Test]
        public void TheCursor_ShowsOnEveryClickableScreen_AndHidesWhileTheMouseTurnsTheCamera()
        {
            Assert.AreEqual(CursorMode.Drive, CursorRule.Decide(false, false, BuildModeState.Closed), "driving");
            Assert.AreEqual(CursorMode.Menu, CursorRule.Decide(true, false, BuildModeState.Closed), "title / pause");
            Assert.AreEqual(CursorMode.Screen, CursorRule.Decide(false, true, BuildModeState.Closed), "face screen");
            Assert.AreEqual(CursorMode.Screen, CursorRule.Decide(false, false, BuildModeState.Menu), "build cards");
            Assert.AreEqual(CursorMode.Drive, CursorRule.Decide(false, false, BuildModeState.Placing),
                "placing aims with the camera");
            Assert.AreEqual(CursorMode.Drive, CursorRule.Decide(false, false, BuildModeState.Welding),
                "welding aims with the camera");
            Assert.AreEqual(CursorMode.Menu, CursorRule.Decide(true, false, BuildModeState.Placing),
                "the pause menu over build mode frees the cursor");

            Assert.IsFalse(CursorRule.IsFree(CursorMode.Drive));
            Assert.AreEqual(CursorLockMode.Locked, CursorRule.LockOf(CursorMode.Drive));
            Assert.IsTrue(CursorRule.IsFree(CursorMode.Menu));
            Assert.AreEqual(CursorLockMode.None, CursorRule.LockOf(CursorMode.Menu));
            Assert.IsTrue(CursorRule.IsFree(CursorMode.Screen));
            Assert.AreEqual(CursorLockMode.Confined, CursorRule.LockOf(CursorMode.Screen));
        }

        [Test]
        public void TheCursorPolicy_WritesTheModeItIsGiven()
        {
            CursorLockMode lockState = Cursor.lockState;
            bool visible = Cursor.visible;
            try
            {
                var policy = new CursorPolicy();
                policy.Set(CursorMode.Screen);
                Assert.IsTrue(policy.IsVisible);
                Assert.IsTrue(policy.InMenu);
                Assert.AreEqual(CursorLockMode.Confined, policy.LockState);

                policy.Set(CursorMode.Drive);
                Assert.IsFalse(policy.IsVisible);
                Assert.IsFalse(policy.InMenu);
                Assert.AreEqual(CursorLockMode.Locked, policy.LockState);

                policy.Release();
                Assert.AreEqual(CursorMode.Menu, policy.Mode);
            }
            finally
            {
                Cursor.lockState = lockState;
                Cursor.visible = visible;
            }
        }

        [Test]
        public void BuildMode_NamesOneStepAtATime()
        {
            Assert.AreEqual(BuildStep.None, BuildSteps.Of(BuildModeState.Closed, 0));
            Assert.AreEqual(BuildStep.None, BuildSteps.Of(BuildModeState.Menu, 0), "the cards carry their own keys");
            Assert.AreEqual(BuildStep.Place, BuildSteps.Of(BuildModeState.Placing, 0));
            Assert.AreEqual(BuildStep.Weld, BuildSteps.Of(BuildModeState.Welding, 3));
            Assert.AreEqual(BuildStep.None, BuildSteps.Of(BuildModeState.Welding, 0), "no weld to explain");

            Assert.AreEqual(UiKeys.BuildStepPlace, BuildSteps.KeyOf(BuildStep.Place));
            Assert.AreEqual(UiKeys.BuildStepWeld, BuildSteps.KeyOf(BuildStep.Weld));
            Assert.IsNull(BuildSteps.KeyOf(BuildStep.None));
            Assert.IsTrue(BuildSteps.ShowsProgress(BuildStep.Weld));
            Assert.IsFalse(BuildSteps.ShowsProgress(BuildStep.Place));
        }

        [Test]
        public void TheBuildMenusKeys_DifferPerDevice_AndPerHint()
        {
            foreach (InputDeviceKind device in new[] { InputDeviceKind.KeyboardMouse, InputDeviceKind.Gamepad })
            {
                Assert.AreNotEqual(BuildKeyHints.CapKey(BuildHint.Pick, device),
                    BuildKeyHints.CapKey(BuildHint.Leave, device));
            }

            Assert.AreEqual(UiKeys.KeyMouseOrE, BuildKeyHints.CapKey(BuildHint.Pick, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual(UiKeys.KeyBOrEscape, BuildKeyHints.CapKey(BuildHint.Leave, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual(UiKeys.BuildHintPick, BuildKeyHints.WordKey(BuildHint.Pick));
            Assert.AreEqual(UiKeys.BuildHintLeave, BuildKeyHints.WordKey(BuildHint.Leave));
        }

        [Test]
        public void TheIgniter_FitsTheCutter_OnlyOnThePad_AndEverythingElseGoesHomeWithAReason()
        {
            const string igniter = DropRule.SpareIgniterPart;
            Assert.AreEqual(DropOutcome.Fit, DropRule.Decide(igniter, FaceDevices.Cutter, true));
            Assert.AreEqual(DropOutcome.NotDocked, DropRule.Decide(igniter, FaceDevices.Cutter, false));
            Assert.AreEqual(DropOutcome.NotDocked, DropRule.Decide(igniter, -1, false));
            Assert.AreEqual(DropOutcome.ToCutter, DropRule.Decide(igniter, FaceDevices.Scan, true));
            Assert.AreEqual(DropOutcome.ToCutter, DropRule.Decide(igniter, -1, true));
            Assert.AreEqual(DropOutcome.Fit, DropRule.Decide(igniter, FaceDevices.Scan, true, onDevicePanel: true),
                "a drop anywhere on the device panel goes to the cutter");
            Assert.AreEqual(DropOutcome.Fit, DropRule.Decide(igniter, -1, true, onDevicePanel: true));
            Assert.AreEqual(DropOutcome.NotDocked, DropRule.Decide(igniter, FaceDevices.Scan, false, onDevicePanel: true),
                "the panel takes nothing while the cutter will not");
            Assert.AreEqual(DropOutcome.Nowhere, DropRule.Decide("part.icecore", FaceDevices.Cutter, true));
            Assert.AreEqual(DropOutcome.Nowhere, DropRule.Decide(null, FaceDevices.Cutter, true), "a material");

            Assert.IsNull(DropRule.ReasonKey(DropOutcome.Fit));
            Assert.AreEqual(UiKeys.CargoDragNotDocked, DropRule.ReasonKey(DropOutcome.NotDocked));
            Assert.AreEqual(UiKeys.CargoDragToCutter, DropRule.ReasonKey(DropOutcome.ToCutter));
            Assert.AreEqual(UiKeys.CargoDragNowhere, DropRule.ReasonKey(DropOutcome.Nowhere));
        }

        [Test]
        public void APress_StaysAClick_UntilThePointerMovesPastTheThreshold()
        {
            var settings = new CargoScreenSettings();
            var drag = new HoldDrag(settings);
            drag.Press(2, new Vector2(100f, 100f), new Vector2(110f, 105f));
            Assert.AreEqual(HoldDragPhase.Pressed, drag.Phase);
            Assert.IsFalse(drag.Move(new Vector2(100f + settings.DragThreshold * 0.5f, 100f)), "a wobble is no drag");
            Assert.IsFalse(drag.ShowsGhost);
            Assert.IsFalse(drag.Release(new Vector2(101f, 100f)), "a click needs no drop");
            Assert.AreEqual(HoldDragPhase.Idle, drag.Phase);
        }

        [Test]
        public void TheDraggedIcon_FollowsSoftly_KeepsItsGrip_AndEasesHomeWhenRefused()
        {
            var settings = new CargoScreenSettings();
            var drag = new HoldDrag(settings);
            var home = new Vector2(110f, 105f);
            drag.Press(1, new Vector2(100f, 100f), home);
            Assert.IsTrue(drag.Move(new Vector2(300f, 200f)), "past the threshold it is a drag");
            Assert.AreEqual(HoldDragPhase.Dragging, drag.Phase);

            drag.Step(1f / 60f);
            Vector2 target = new Vector2(310f, 205f);
            Assert.Greater(Vector2.Distance(drag.Ghost, home), 0f, "the icon starts to follow");
            Assert.Greater(Vector2.Distance(drag.Ghost, target), 1f, "eased, not snapped");
            for (int i = 0; i < 120; i++)
            {
                drag.Step(1f / 60f);
            }

            Assert.AreEqual(target.x, drag.Ghost.x, 0.5f, "it keeps the spot where it was grabbed");
            Assert.AreEqual(target.y, drag.Ghost.y, 0.5f);

            Assert.IsTrue(drag.Release(new Vector2(300f, 200f)));
            drag.Return();
            Assert.AreEqual(HoldDragPhase.Returning, drag.Phase);
            drag.Step(settings.DragReturnSeconds * 0.5f);
            Vector2 halfway = drag.Ghost;
            Assert.Greater(Vector2.Distance(halfway, home), 1f, "on its way home");
            Assert.Greater(Vector2.Distance(halfway, target), 1f);
            drag.Step(settings.DragReturnSeconds);
            Assert.AreEqual(HoldDragPhase.Idle, drag.Phase, "home, and done");
            Assert.AreEqual(home, drag.Ghost);
        }

        [Test]
        public void AFittedIcon_EasesIntoTheDevice_AndFades()
        {
            var settings = new CargoScreenSettings();
            var drag = new HoldDrag(settings);
            drag.Press(0, Vector2.zero, Vector2.zero);
            drag.Move(new Vector2(500f, 300f));
            drag.Step(1f);
            Assert.IsTrue(drag.Release(new Vector2(500f, 300f)));
            var device = new Vector2(1012f, 214f);
            drag.Fit(device);
            Assert.AreEqual(1f, drag.GhostAlpha, 0.001f);
            drag.Step(settings.DragFitSeconds * 0.5f);
            Assert.Less(drag.GhostAlpha, 1f, "fading into the device");
            Assert.Greater(drag.GhostAlpha, 0f);
            drag.Step(settings.DragFitSeconds);
            Assert.AreEqual(HoldDragPhase.Idle, drag.Phase);
        }
    }
}
