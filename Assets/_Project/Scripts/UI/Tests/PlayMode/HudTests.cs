using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.Testing;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The driving HUD end to end: prompts appear only near usable things and only a few times, the pickup feed only
    /// on change, the hold and the device sockets on 07's face, and the low-battery line on the shared line.
    /// </summary>
    public sealed class HudTests : InputTestFixture
    {
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

        [UnityTest]
        public IEnumerator Prompt_AppearsNearAUsableThing_NamesItsKey_AndBowsOutWhenUsed()
        {
            InputSystem.AddDevice<Keyboard>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot();
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 12f), true);
            yield return null;
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompts before 07 wakes");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Build)), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text, "keyboard glyph from the real binding");

            yield return Tap(gamepad.buttonSouth);
            yield return Seconds(0.1f);
            Assert.AreEqual(InputDeviceKind.Gamepad, _rig.Bootstrap.Context.Input.ActiveDevice);
            Assert.AreEqual("X", _rig.Ui.Layout.PromptGlyphLabel.text, "the glyph follows the device");
            Assert.IsTrue(_rig.Ui.Layout.PromptGlyph.ClassListContains(GlyphView.PadClass));

            Events.Publish(new StructurePlaced(1, StructureIds.ChargingPad, Vector3.zero, 0f));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "doing it ends the prompt");
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Build));
            Assert.AreEqual(1, _rig.Ui.Ledger.Shown(InteractionKind.Build));
        }

        [UnityTest]
        public IEnumerator Prompt_HidesBehindTheCamera_AndWhilePaused()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(2f, 0f, -10f), true);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "never pinned to the screen edge for something behind you");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1f);
            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "the pause menu has the stage alone");
        }

        [UnityTest]
        public IEnumerator CargoScreen_OpensOnTab_ShowsTheHoldAndTheDevices_AndClosesOnTab_WithoutPausing()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(5, new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 2, 4), new MaterialSlot(SalvageMaterial.Ilmenite, 3, 6));
            _rig.Fakes.Hold.AddPart("part.igniter");
            _rig.Fakes.Hold.WaitingBundles = 3;
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            CargoScreen cargo = _rig.Ui.Cargo;
            Assert.IsTrue(cargo.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "the game does not pause");
            Assert.AreEqual(8, cargo.HoldCellCount, "a cell for every unlocked slot of 07's hold");
            Assert.AreEqual(16, cargo.View.LockedCellCount, "16 locked cells complete the 4x6 grid");
            Assert.AreEqual(Text(UiKeys.PickupName("part.igniter")), cargo.HoldPartText(3),
                "the carried story part shows in its hold slot with its localized name");
            Assert.IsFalse(cargo.HoldCell(3).ClassListContains(CargoScreen.EmptyClass));
            Assert.IsTrue(cargo.HoldCell(8).ClassListContains(CargoScreen.LockedClass));
            StringAssert.Contains("3", cargo.WaitingText);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            Assert.IsFalse(cargo.IsOpen, "Tab again closes it");

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.4f);
            Assert.IsTrue(cargo.IsOpen);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(0.4f);
            Assert.IsFalse(cargo.IsOpen, "Esc closes it too");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "and does not open the pause menu");
        }

        [UnityTest]
        public IEnumerator CargoScreen_FirstTab_BootsOnce_AndAsksForTheHatchAndTheChirp()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(6, new MaterialSlot(SalvageMaterial.Metal, 2, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 1, 4));
            int started = 0;
            int lit = 0;
            int arrived = 0;
            float hatch = -1f;
            ChirpMood? chirp = null;
            Events.Subscribe<HoldBootStarted>(_ => started++);
            Events.Subscribe<HoldSlotLit>(_ => lit++);
            Events.Subscribe<HoldItemArrived>(_ => arrived++);
            Events.Subscribe<RoverHatchCycleRequested>(request => hatch = request.OpenSeconds);
            Events.Subscribe<RoverChirpRequested>(request => chirp = request.Mood);
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.15f);
            CargoScreen cargo = _rig.Ui.Cargo;
            Assert.IsTrue(cargo.IsBooting, "the first press boots the hold");
            Assert.AreEqual(1, started);
            Assert.Greater(hatch, 1.2f, "07's hatch is asked to stay open for the whole boot");
            yield return Seconds(2f);
            Assert.IsFalse(cargo.IsBooting);
            Assert.AreEqual(8, lit, "every slot outline lit, once");
            Assert.AreEqual(2, arrived, "both starter items slid in");
            Assert.AreEqual(ChirpMood.Affirm, chirp, "07 chirps when the boot is done");

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.5f);
            Assert.IsFalse(cargo.IsBooting, "never again");
            Assert.AreEqual(1, started);
        }

        [UnityTest]
        public IEnumerator SevenTab_ShowsTheFourDevices_AndTheCutterTakesTheSpareIgniterOnlyOnThePad()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(8);
            _rig.Fakes.Hold.AddPart("part.igniter");
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            CargoScreen cargo = _rig.Ui.Cargo;
            FaceInventoryView view = cargo.View;
            Assert.AreEqual(8, cargo.HoldCellCount, "Tab opens one panel with both the hold grid and 07's devices");
            Vector2 fieldPanelSize = cargo.PanelRect.size;
            Assert.AreEqual(1.5f, fieldPanelSize.x / fieldPanelSize.y, 0.02f, "1.5:1 aspect ratio");
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Assert.AreEqual(DisplayStyle.Flex, view.DeviceElement(device).resolvedStyle.display,
                    $"device {device} is shown in the same panel");
            }

            Assert.IsTrue(view.DeviceElement(FaceDevices.Scan).ClassListContains(CargoScreen.FittedClass));
            Assert.IsFalse(view.DeviceElement(FaceDevices.Cutter).ClassListContains(CargoScreen.FittedClass));
            Assert.AreEqual(Text("ui.system.state.fault_igniter"), view.DeviceStateText(FaceDevices.Cutter));
            Assert.AreEqual(Text("ui.system.state.empty"), view.DeviceStateText(FaceDevices.SocketOne));

            Submit(view.DeviceElement(FaceDevices.Cutter));
            yield return Seconds(0.2f);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits, "off the pad the cutter takes nothing");
            Assert.IsFalse(view.IsIgniterOffered);

            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.3f);
            Assert.IsTrue(view.IsIgniterOffered, "on the pad with the igniter in the hold the cutter offers it");
            view.FocusDevice(FaceDevices.Cutter);
            yield return null;
            Assert.AreEqual(Text("ui.device.cutter.fit"), view.TooltipDescText);

            Submit(view.DeviceElement(FaceDevices.Cutter));
            yield return Seconds(0.3f);
            Assert.AreEqual(1, _rig.Fakes.Systems.Fits, "one confirm fits the igniter");
            Assert.AreEqual(Text("ui.system.state.ok"), view.DeviceStateText(FaceDevices.Cutter));
            Assert.IsFalse(view.IsIgniterOffered);
            Assert.IsTrue(cargo.IsOpen, "and the screen stays open");
        }

        [UnityTest]
        public IEnumerator SevenTab_InEnglishAndVietnamese_At1080pAnd720p_TiersBadgesAndDevicesFitWithoutOverlap()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot, 1920, 1080);
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.2f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.5f);

            FaceInventoryView view = _rig.Ui.Cargo.View;
            int[] tiers = { 8, 16, 24 };
            foreach (int tier in tiers)
            {
                _rig.Fakes.Hold.Set(tier - 3,
                    new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                    new MaterialSlot(SalvageMaterial.Wiring, 1, 4),
                    new MaterialSlot(SalvageMaterial.Ilmenite, 12, 12));
                _rig.Ui.Cargo.MarkDirty();
                yield return null;

                Assert.AreEqual(tier, view.UnlockedCellCount);
                Assert.AreEqual(24 - tier, view.LockedCellCount);
                Assert.AreEqual(DisplayStyle.Flex, view.HoldBadge(0).resolvedStyle.display, "stack >= 2 shows badge");
                Assert.AreEqual("4", view.HoldBadge(0).text);
                Assert.AreEqual(DisplayStyle.None, view.HoldBadge(1).resolvedStyle.display, "stack == 1 hides badge");
                Assert.AreEqual("12", view.HoldBadge(2).text);
            }

            (int w, int h)[] sizes = { (1920, 1080), (1280, 720) };
            string[] languages = { "en", "vi" };
            foreach (string lang in languages)
            {
                _rig.Ui.Localization.SetLanguage(lang);
                foreach ((int w, int h) in sizes)
                {
                    _rig.Reframe(w, h);
                    yield return null;
                    yield return null;

                    Rect panel = _rig.Ui.Cargo.PanelRect;
                    Assert.AreEqual(1.5f, panel.width / panel.height, 0.02f, $"{lang} {w}x{h}: 1.5:1 panel");
                    for (int i = 0; i < FaceDevices.Count; i++)
                    {
                        VisualElement bubble = view.DeviceElement(i);
                        Rect box = bubble.worldBound;
                        Assert.GreaterOrEqual(box.xMin, panel.xMin + panel.width * 0.4f,
                            $"{lang} {w}x{h}: device {i} stays in right pane");
                        Assert.LessOrEqual(box.xMax, panel.xMax + 1f);
                        Assert.GreaterOrEqual(box.yMin, panel.yMin - 1f);
                        Assert.LessOrEqual(box.yMax, panel.yMax + 1f);
                        for (int j = i + 1; j < FaceDevices.Count; j++)
                        {
                            Assert.IsFalse(box.Overlaps(view.DeviceElement(j).worldBound),
                                $"{lang} {w}x{h}: device {i} bubble overlaps device {j}");
                        }

                        Rect state = view.DeviceStateLabel(i).worldBound;
                        Assert.GreaterOrEqual(state.xMin, panel.xMin, $"{lang} {w}x{h}: device {i} state fits the panel");
                        Assert.LessOrEqual(state.xMax, panel.xMax + 1f, $"{lang} {w}x{h}: device {i} state fits the panel");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator UnlockCard_SaysWhichBlueprintWasLearned_AndLeavesByItself()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.3f);
            Events.Publish(new StructureLearned(StructureIds.ChargingPad));
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.UnlockCard.IsWaiting || _rig.Ui.UnlockCard.IsVisible);
            float limit = Time.realtimeSinceStartup + 20f;
            while (!_rig.Ui.UnlockCard.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.UnlockCard.IsVisible, "the card shows once the screen is clear");
            Assert.AreEqual(StructureIds.ChargingPad, _rig.Ui.UnlockCard.Shown);
            Assert.AreEqual(string.Format(Text(UiKeys.BlueprintLearned), Text(StructureIds.NameKey(StructureIds.ChargingPad))),
                _rig.Ui.Layout.UnlockCard.Q<Label>("unlock-card-verb").text);
            yield return Seconds(_rig.Tuning.UnlockCard.HoldSeconds + _rig.Tuning.UnlockCard.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(_rig.Ui.UnlockCard.IsVisible, "and leaves by itself");
        }

        [UnityTest]
        public IEnumerator PickupFeed_ShowsAMaterialBeyondTheFirstThree()
        {
            Boot();
            yield return Seconds(0.3f);
            _rig.Fakes.SetUnits(SalvageMaterial.Ilmenite, 3);
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Pickups.IsShowing(SalvageMaterial.Ilmenite));
            Assert.AreEqual(Text(UiKeys.MaterialName(SalvageMaterial.Ilmenite)),
                _rig.Ui.Pickups.NameText(SalvageMaterial.Ilmenite));
        }

        [UnityTest]
        public IEnumerator BuildPrompt_SaysBuild_ForAShelfSocket()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 4f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(Text("hint.build"), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text);
        }

        [UnityTest]
        public IEnumerator PickupFeed_SlidesInOnAGain_RollsThePlusAndTheTotal_StacksTheMaterials_AndLeaves()
        {
            Boot();
            PickupFeed feed = _rig.Ui.Pickups;
            _rig.Fakes.SetMaterials(5, 2, 1);
            yield return Seconds(0.3f);
            Assert.IsFalse(feed.IsVisible, "the stock a save loads with shows nothing");

            _rig.Fakes.SetMaterials(5, 6, 1);
            yield return Seconds(0.15f);
            Assert.IsTrue(feed.IsShowing(SalvageMaterial.Wiring));
            Assert.IsFalse(feed.IsShowing(SalvageMaterial.Metal), "only the material that grew has a row");
            Assert.Less(feed.Shown(SalvageMaterial.Wiring), 6, "it counts rather than jumps");
            yield return Seconds(_rig.Tuning.PickupFeed.MaxCountSeconds + 0.3f);
            Assert.AreEqual("4", feed.GainText(SalvageMaterial.Wiring), "four more than the row opened on");
            Assert.AreEqual("6", feed.TotalText(SalvageMaterial.Wiring), "and the new total");
            Assert.AreEqual(Text(UiKeys.MaterialName(SalvageMaterial.Wiring)), feed.NameText(SalvageMaterial.Wiring));

            _rig.Fakes.SetMaterials(5, 8, 1);
            yield return Seconds(_rig.Tuning.PickupFeed.MaxCountSeconds + 0.3f);
            Assert.AreEqual("6", feed.GainText(SalvageMaterial.Wiring), "more of it adds to the row that is up");
            Assert.AreEqual("8", feed.TotalText(SalvageMaterial.Wiring));

            _rig.Fakes.SetMaterials(6, 8, 2);
            yield return Seconds(0.3f);
            Assert.IsTrue(feed.IsShowing(SalvageMaterial.Metal) && feed.IsShowing(SalvageMaterial.Wiring) &&
                          feed.IsShowing(SalvageMaterial.Optics), "the three materials stack, three rows at most");

            yield return Seconds(_rig.Tuning.PickupFeed.MaxCountSeconds + _rig.Tuning.PickupFeed.LingerSeconds +
                                 _rig.Tuning.PickupFeed.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(feed.IsVisible, "then the rows leave the screen to the moon");
        }

        [UnityTest]
        public IEnumerator Salvage_CutPromptThenARing_ThatKeepsItsProgress()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            var cut = new Vector3(0.5f, 0.6f, 6f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, cut, true);
            _rig.Fakes.HasTarget = true;
            _rig.Fakes.CutPoint = cut;
            _rig.Fakes.Material = SalvageMaterial.Optics;
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "aimed at a piece: the cut is taught");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Salvage)), _rig.Ui.Layout.PromptWord.text);
            Assert.IsFalse(_rig.Ui.SalvageRing.IsVisible, "no ring before there is a cut to show");

            _rig.Fakes.IsCutting = true;
            _rig.Fakes.CutProgress = 0.3f;
            yield return Seconds(0.8f);
            Assert.IsTrue(_rig.Ui.SalvageRing.IsVisible);
            Assert.AreEqual(0.3f, _rig.Ui.SalvageRing.Progress, 1e-3f);
            Assert.AreEqual(SalvageMaterial.Optics, _rig.Ui.SalvageRing.ShownMaterial, "it says what the piece yields");
            Assert.AreEqual(0, _rig.Ui.Ledger.Used(InteractionKind.Salvage), "the verb is learned when a piece comes free");
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "the prompt bows out once the beam cuts");

            _rig.Fakes.IsCutting = false;
            _rig.Fakes.CutProgress = 0.55f;
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.SalvageRing.IsVisible, "let go halfway: the ring keeps the progress");
            Assert.AreEqual(0.55f, _rig.Ui.SalvageRing.Progress, 1e-3f);

            _rig.Fakes.HasTarget = false;
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.SalvageRing.IsVisible, "aimed away: it eases off");
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        /// <summary>The tables' own words for <paramref name="key"/>, so a test never guesses copy.</summary>
        private string Text(string key)
        {
            return _rig.Ui.Localization.Get(key);
        }

        [UnityTest]
        public IEnumerator TheLowBatteryLine_IsTheHintOnTheSharedLine()
        {
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Charge = 1.2f;
            yield return Seconds(1f);
            Events.Publish(new TickerLine("battery.low.lander"));
            yield return Seconds(2f);
            Assert.IsTrue(_rig.Ui.Objective.ShowsBattery, "it shows on the one soft line");
            Assert.AreEqual(Text("battery.low.lander"), _rig.Ui.Layout.NextLineText.text);

            _rig.Fakes.IsCharging = true;
            yield return Seconds(2.5f);
            Assert.IsFalse(_rig.Ui.Objective.IsVisible, "a charger ends it");
        }

        [UnityTest]
        public IEnumerator NoFrame_ShowsTwoGuidanceElements()
        {
            Boot();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new TickerLine("battery.low.lander"));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 10f), true);
            int shownFrames = 0;
            float until = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < until)
            {
                int up = (_rig.Ui.Prompt.IsVisible ? 1 : 0) + (_rig.Ui.LookUp.IsVisible ? 1 : 0) +
                         (_rig.Ui.Objective.IsVisible ? 1 : 0);
                shownFrames += up > 0 ? 1 : 0;
                Assert.LessOrEqual(up, 1, "one guidance element at a time");
                yield return null;
            }

            Assert.Greater(shownFrames, 0, "the scene did guide, or the test proves nothing");
        }

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
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
