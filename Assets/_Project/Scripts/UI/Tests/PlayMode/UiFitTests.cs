using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The UI fits every screen shape (docs/features/M3-24 part A): at 16:9, 16:10, 21:9, 32:9 and 4:3, from 1280x720
    /// to 3840x2160, the title, its settings, the HUD, the pause menu, its settings and the lab's screen leave nothing
    /// off the screen and no two panels over each other, and the title's wordmark keeps its place against the picture.
    /// </summary>
    public sealed class UiFitTests : InputTestFixture
    {
        private const float Settle = 0.4f;
        private const float PlaceTolerance = 0.01f;
        private const float WideAspect = 16f / 9f;
        private const float NeverIdleSeconds = 1e6f;
        private const int BigTextPresses = 2;

        private readonly List<string> _problems = new List<string>();
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
            _problems.Clear();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator EveryScreenShape_KeepsEveryShownPanelOnScreen_AndApart()
        {
            yield return Fit(0);
        }

        [UnityTest]
        public IEnumerator EveryScreenShape_AtOneAndAHalfTimesTheText_StillFits()
        {
            yield return Fit(BigTextPresses);
        }

        private IEnumerator Fit(int textSizePresses)
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            ScreenShape first = ScreenShapes.All[0];
            _rig = UiTestRig.Boot(_controls, _slot, first.Width, first.Height);
#if UNITY_EDITOR
            var serialized = new SerializedObject(_rig.Tuning);
            serialized.FindProperty("_title._idleSeconds").floatValue = NeverIdleSeconds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            UiLayout layout = _rig.Ui.Layout;
            EventBus events = _rig.Bootstrap.Context.Events;
            yield return null;

            events.Publish(new TitleShown(true));
            yield return Seconds(1f);
            if (textSizePresses > 0)
            {
                Submit(layout.SettingsButton);
                yield return Seconds(0.6f);
                for (int i = 0; i < textSizePresses; i++)
                {
                    Submit(layout.TextSizeButton);
                    yield return null;
                }

                Submit(layout.SettingsBackButton);
                yield return Seconds(0.8f);
            }

            var wordmarkPlaces = new List<float>();
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "title",
                    (label, found) => ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseMain, label, found));
                if ((float)shape.Width / shape.Height >= WideAspect - PlaceTolerance)
                {
                    wordmarkPlaces.Add(WordmarkPlace(layout));
                }
            }

            foreach (float place in wordmarkPlaces)
            {
                Assert.AreEqual(wordmarkPlaces[0], place, PlaceTolerance,
                    "the wordmark keeps its place against the picture on every shape at 16:9 or wider");
            }

            Submit(layout.SettingsButton);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "title settings", (label, found) =>
                {
                    ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, label, found);
                    ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseSettings, label, found);
                });
            }

            Submit(layout.SettingsBackButton);
            yield return Seconds(1f);
            Submit(layout.ResumeButton);
            events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Fade = 0f;
            yield return Seconds(1f);

            _rig.Fakes.Cells = 4;
            _rig.Fakes.Charge = 2f;
            events.Publish(new BatteryArcRevealed(false));
            events.Publish(new NextStepChanged("next.tower"));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, new Vector3(0f, 0f, 12f), true);
            events.Publish(new TickerLine(UiTestRig.HomeLine));
            int gain = 0;
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                gain++;
                _rig.Fakes.SetMaterials(gain, gain, gain);
                yield return Seconds(Settle);
                yield return Check(shape, "hud", (label, found) =>
                {
                    ScreenFit.CheckApart(layout.BatteryDial, layout.NextLine, label, found);
                    ScreenFit.CheckApart(layout.HomeMeter, layout.NextLine, label, found);
                    ScreenFit.CheckApart(layout.AbilitySlots, layout.MaterialsChip, label, found);
                    ScreenFit.CheckApart(layout.NextLine, layout.Ticker, label, found);
                    ScreenFit.CheckApart(layout.Ticker, layout.MaterialsChip, label, found);
                });
            }

            _rig.Fakes.PrimaryHint = InteractionHint.None;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.AtStation = true;
            yield return Seconds(2f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "tower card", null);
            }

            _rig.Fakes.AtStation = false;
            yield return Seconds(1f);
            events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 4));
            yield return Seconds(2f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "memory card", null);
            }

            _rig.Ui.Card.Dismiss();
            yield return Seconds(1.5f);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "pause", null);
            }

            Submit(layout.SettingsButton);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "pause settings",
                    (label, found) => ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, label, found));
            }

            Submit(layout.SettingsBackButton);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1f);

            events.Publish(new LabEntered(LabStation.Board));
            events.Publish(new PlaceFirstApproached("place.lab"));
            events.Publish(new LabStationLooked(LabStation.Board, true));
            yield return Seconds(2f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "lab", null);
            }

            Assert.IsEmpty(_problems, "the UI fits every screen shape at " + (textSizePresses > 0 ? "150" : "100") +
                                      "% text:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// Where the wordmark's centre sits, in screen heights from the screen's centre: the picture is the same
        /// height on every shape, so a wordmark that keeps its place keeps this number.
        /// </summary>
        private static float WordmarkPlace(UiLayout layout)
        {
            Rect screen = layout.Root.worldBound;
            Rect wordmark = layout.TitleWordmark.worldBound;
            return (wordmark.center.x - screen.center.x) / screen.height;
        }

        private IEnumerator Reframe(ScreenShape shape)
        {
            _rig.Reframe(shape.Width, shape.Height);
            yield return Seconds(Settle);
        }

        private IEnumerator Check(ScreenShape shape, string state, Action<string, List<string>> pairs)
        {
            string label = Label(shape, state);
            yield return ScreenFit.Probe(found =>
            {
                ScreenFit.CheckOnScreen(_rig.Ui.Layout.Root, label, found);
                pairs?.Invoke(label, found);
            }, _problems);
        }

        private static string Label(ScreenShape shape, string state)
        {
            return shape.Name + " " + state;
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private static void Submit(VisualElement target)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = target;
                target.SendEvent(submit);
            }
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
