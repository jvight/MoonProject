using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Build mode with a real mouse and keyboard in the built Main scene (owner feedback 2026-10-18): B opens the card
    /// row with the cursor free, the pointer over a card selects it and a click picks it, which captures the cursor
    /// for aiming and hands the screen to the one placing line; Esc leaves build mode without pausing the game.
    /// </summary>
    public sealed class BuildInteractionTests : InputTestFixture
    {
        private const float Settle = 0.6f;

        private BuildScene _scene;

        public override void TearDown()
        {
            _scene?.TearDown();
            _scene = null;
            base.TearDown();
        }

        [UnityTest]
        [Timeout(180000)]
        [PrebuildSetup(typeof(BuildModeCaptureTests.BuildCaptureScene))]
        [PostBuildCleanup(typeof(BuildModeCaptureTests.BuildCaptureScene))]
        public IEnumerator TheMouse_HoversAndPicksACard_ThenTheLineSaysHowToPlace_AndEscLeavesWithoutPausing()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            _scene = new BuildScene();
            yield return _scene.Boot();
            _scene.Unlock(new[] { StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank },
                16, 8, 8);
            UISystem ui = _scene.Ui;
            BuildField build = _scene.Build;
            Set(mouse.position, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            yield return Tap(keyboard.bKey);
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Menu, build.State, "B opens the build menu");
            Assert.AreEqual(3, ui.BuildStrip.CardCount, "one card per learned structure");
            Assert.AreEqual(CursorMode.Screen, ui.Cursor.Mode, "the cursor is free over the cards");
            Assert.IsTrue(ui.Cursor.IsVisible);
            Assert.AreEqual(0, build.Controller.SelectedIndex);

            Vector2 card = CardOnScreen(ui, 2);
            for (int i = 1; i <= 6; i++)
            {
                Set(mouse.position, Vector2.Lerp(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), card, i / 6f));
                yield return null;
            }

            Assert.AreEqual(2, build.Controller.SelectedIndex, "the pointer over a card selects it");
            Assert.IsTrue(ui.BuildStrip.Card(2).ClassListContains(BuildStripView.SelectedCardClass));

            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Placing, build.State, "a click picks the card");
            Assert.AreEqual(StructureIds.BatteryBank, build.Controller.SelectedStructure.Id);
            Assert.AreEqual(CursorMode.Drive, ui.Cursor.Mode, "placing aims with the camera: the cursor is captured");
            Assert.AreEqual(BuildStep.Place, ui.BuildStrip.Step);
            Assert.IsTrue(ui.BuildStrip.IsReticleVisible, "the centre ring shows where the ghost follows the aim");
            Assert.IsFalse(ui.BuildStrip.IsVisible, "the cards leave for placing");
            Assert.IsTrue(ui.BuildStrip.IsCostShown, "the ghost carries its cost rows (opening-v3 E7)");
            Assert.IsTrue(ui.Objective.ShowsBuildStep, "the one placing line takes the guidance slot");
            StringAssert.Contains("E", ui.Layout.NextLineText.text, "it names the real place key");

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Closed, build.State, "Esc leaves build mode");
            Assert.IsFalse(ui.Pause.IsOpen, "and does not also pause the game");
            Assert.AreEqual(CursorMode.Drive, ui.Cursor.Mode);
            Assert.IsFalse(ui.Objective.ShowsBuildStep, "the placing line has gone");
            Assert.AreEqual(string.Empty, ui.Layout.NextLineText.text, "and holds no stale words once hidden");
        }

        [UnityTest]
        [Timeout(180000)]
        [PrebuildSetup(typeof(BuildModeCaptureTests.BuildCaptureScene))]
        [PostBuildCleanup(typeof(BuildModeCaptureTests.BuildCaptureScene))]
        public IEnumerator Tab_InBuildMode_LeavesItCleanlyAndOpens07sScreen_OneContextAtATime()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _scene = new BuildScene();
            yield return _scene.Boot();
            _scene.Unlock(new[] { StructureIds.ChargingPad }, 16, 8, 8);
            UISystem ui = _scene.Ui;
            BuildField build = _scene.Build;

            yield return Tap(keyboard.bKey);
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Menu, build.State);
            Assert.IsTrue(build.Controller.PickStructure(StructureIds.ChargingPad));
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Placing, build.State, "a structure is picked and its ghost shows");

            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            Assert.AreEqual(BuildModeState.Closed, build.State, "Tab leaves build mode");
            Assert.IsTrue(ui.Cargo.IsOpen, "and opens 07's screen");
            Assert.IsNull(build.GhostView, "the ghost is cancelled");
            Assert.IsFalse(ui.BuildStrip.IsReticleVisible, "and the centre ring goes with it");
            Assert.IsFalse(ui.Pause.IsOpen);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(Settle);
            Assert.IsFalse(ui.Cargo.IsOpen, "Tab again closes the screen");
            Assert.AreEqual(BuildModeState.Closed, build.State, "and build mode stays closed");
        }

        /// <summary>Card <paramref name="index"/>'s centre in Input System screen pixels (bottom-left origin).</summary>
        private static Vector2 CardOnScreen(UISystem ui, int index)
        {
            VisualElement root = ui.Layout.Root;
            Vector2 center = ui.BuildStrip.Card(index).worldBound.center;
            float scale = Screen.width / root.layout.width;
            return new Vector2(center.x * scale, Screen.height - center.y * scale);
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }
    }
}
