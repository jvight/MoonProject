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
        public IEnumerator SevenTab_HoverArrowsAndClicks_SelectVisibly_WithATick_AndTheCutterExplainsItsFault()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(8, new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 2, 4));
            var cues = new System.Collections.Generic.List<UiCueKind>();
            Events.Subscribe<UiCue>(cue => cues.Add(cue.Kind));
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            FaceInventoryView view = _rig.Ui.Cargo.View;
            float scale = _rig.Ui.Cargo.PanelRect.width / FaceInventoryView.ReferenceWidth;
            Assert.AreEqual(0, view.FocusedHoldCell, "it opens on the first thing in the hold");
            StringAssert.Contains(Text("material.metal"), view.TooltipNameText, "and the info box names it");

            cues.Clear();
            view.Navigate(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(1, view.FocusedHoldCell, "an arrow moves the selection");
            Assert.AreEqual(Text("material.wiring"), view.TooltipNameText, "the info box follows it");
            CollectionAssert.Contains(cues, UiCueKind.FocusMove, "with a soft tick");

            cues.Clear();
            Vector2 emptyCell = FaceInventoryView.GetHoldCellReferenceRect(5).center * scale;
            view.PointerMoveAtPanel(emptyCell);
            Assert.AreEqual(5, view.FocusedHoldCell, "the hover selects the empty slot under the pointer");
            Assert.AreEqual(Text(UiKeys.CargoEmptyName), view.TooltipNameText, "an empty slot says so");
            CollectionAssert.Contains(cues, UiCueKind.FocusMove);

            cues.Clear();
            yield return null;
            view.PointerDownAtPanel(FaceInventoryView.GetHoldCellReferenceRect(2).center * scale);
            Assert.AreEqual(2, view.FocusedHoldCell, "a click selects");
            CollectionAssert.Contains(cues, UiCueKind.Confirm, "and answers with a tick");

            cues.Clear();
            yield return null;
            view.PointerDownAtPanel(FaceDevices.CenterOf(FaceDevices.Cutter) * scale);
            Assert.AreEqual(FaceDevices.Cutter, view.FocusedDeviceIndex);
            Assert.AreEqual(Text("ui.device.cutter.fault"), view.TooltipDescText, "the cutter's fault is explained");
            CollectionAssert.Contains(cues, UiCueKind.Confirm);
            Assert.AreEqual(0, _rig.Fakes.Systems.Fits, "off the pad a click only explains");

            _rig.Fakes.Systems.CanFitIgniter = true;
            yield return Seconds(0.3f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.5f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.5f);
            Assert.AreEqual(FaceDevices.Cutter, view.FocusedDeviceIndex, "it opens on the cutter while it offers the igniter");
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
        public IEnumerator Readout_BootDiagnosticsFirst_TheCutterFlashes_AndOnlyAMilestoneReachedNowShowsTheProtocolLine()
        {
            Boot();
            _rig.Tune("_guidance._bootHoldSeconds", 1.5f);
            _rig.Tune("_guidance._statusHoldSeconds", 1.5f);
            int cutterLine = 1 + (int)RoverSystem.Cutter;
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle, false));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.GazeEarth, false));
            yield return Seconds(_rig.Tuning.Guidance.Readout.FadeIn + 0.4f);
            ReadoutView readout = _rig.Ui.Readout;
            Assert.IsTrue(readout.IsVisible);
            Assert.AreEqual(ReadoutKind.Boot, readout.Showing, "07's diagnostics show when its lens opens");
            Assert.AreEqual(11, readout.LineCount, "the unit, eight systems, the battery and the protocol line");
            Assert.AreEqual(Text(ReadoutText.UnitKey), readout.LineText(0));
            StringAssert.StartsWith(Text("ui.system.cutter"), readout.LineText(cutterLine));
            StringAssert.EndsWith(Text("ui.system.state.fault_igniter"), readout.LineText(cutterLine));
            Assert.AreEqual(Text("ui.readout.power_off"), readout.LineText(10));
            Assert.IsFalse(_rig.Ui.Objective.IsVisible, "the objective line waits for the diagnostics");

            Events.Publish(new CutterRefused(Vector3.zero));
            yield return null;
            Assert.IsTrue(readout.IsFlashing(cutterLine), "a refused cut flashes the cutter's line");

            _rig.Fakes.Systems.Set(RoverSystem.Cutter, RoverSystemState.Ok);
            Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, false));
            yield return null;
            StringAssert.EndsWith(Text("ui.system.state.ok"), readout.LineText(cutterLine), "the line turns OK");

            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame, false));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.Objective.ShowsObjective && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the first objective line follows the diagnostics");
            Assert.IsFalse(readout.IsVisible);
            Assert.AreEqual(Text("next.v2.drive_lander"), _rig.Ui.Layout.NextLineText.text);

            Events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, false));
            yield return Seconds(_rig.Tuning.Guidance.Readout.FadeIn + 0.6f);
            Assert.AreEqual(ReadoutKind.Protocol, readout.Showing, "a milestone reached now shows its line");
            Assert.AreEqual(Text("ui.readout.power_on"), readout.LineText(0));
            Assert.IsFalse(_rig.Ui.Objective.IsVisible, "over the objective line");

            yield return Seconds(_rig.Tuning.Guidance.StatusHoldSeconds + _rig.Tuning.Guidance.Readout.FadeOut + 0.6f);
            Assert.IsFalse(readout.IsVisible);
            Events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, true));
            yield return Seconds(1f);
            Assert.IsFalse(readout.IsVisible, "a replayed milestone sets the line without showing it");
            Assert.AreEqual(ProtocolLine.LinkSignalWest, _rig.Ui.OpeningProtocol.Line);
        }

        [UnityTest]
        public IEnumerator Readout_TheLandersReserveAndAScannedPanel_ShowOnceInTheirOwnWords_AndALoadedGameSaysNothing()
        {
            Boot();
            _rig.Tune("_guidance._statusHoldSeconds", 1.5f);
            _rig.Tune("_guidance._storyHoldSeconds", 1.5f);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new LanderReserveChanged(0.5f, true));
            Events.Publish(new LanderReserveChanged(0f, true));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Readout.IsVisible, "a loaded game's reserve is stated without a card");

            Events.Publish(new LanderReserveChanged(0.4f, false));
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Readout.IsVisible, "the reserve is read only once it has given out");
            Events.Publish(new LanderReserveChanged(0f, false));
            yield return Seconds(_rig.Tuning.Guidance.Readout.FadeIn + 0.6f);
            Assert.AreEqual(ReadoutKind.Reserve, _rig.Ui.Readout.Showing);
            Assert.AreEqual(string.Format(Text("ui.readout.reserve"), 0), _rig.Ui.Readout.LineText(0));

            Events.Publish(new StoryScanned("airlock_panel", "story.airlock_panel", Vector3.zero));
            yield return Seconds(_rig.Tuning.Guidance.Readout.FadeOut + _rig.Tuning.Guidance.Readout.FadeIn + 0.8f);
            Assert.AreEqual(ReadoutKind.Story, _rig.Ui.Readout.Showing, "the scanned panel's text follows");
            Assert.AreEqual(2, _rig.Ui.Readout.LineCount);
            Assert.AreEqual(Text("story.airlock_panel").Split('\n')[1], _rig.Ui.Readout.LineText(1));
        }

        [UnityTest]
        public IEnumerator NoFrame_ShowsTwoGuidanceElements()
        {
            Boot();
            _rig.Tune("_guidance._bootHoldSeconds", 2f);
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle, false));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.GazeEarth, false));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame, false));
            Events.Publish(new TickerLine("battery.low.lander"));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 10f), true);
            int shownFrames = 0;
            bool readoutSeen = false;
            float until = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < until)
            {
                readoutSeen |= _rig.Ui.Readout.IsVisible;
                int up = (_rig.Ui.Prompt.IsVisible ? 1 : 0) + (_rig.Ui.LookUp.ShowsPrompt ? 1 : 0) +
                         (_rig.Ui.Readout.IsVisible ? 1 : 0) + (_rig.Ui.UnlockCard.IsVisible ? 1 : 0) +
                         (_rig.Ui.Objective.IsVisible ? 1 : 0);
                shownFrames += up > 0 ? 1 : 0;
                Assert.LessOrEqual(up, 1, "one guidance element at a time: prompt " + _rig.Ui.Prompt.IsVisible + ", look-up " +
                    _rig.Ui.LookUp.ShowsPrompt + ", readout " + _rig.Ui.Readout.IsVisible + ", unlock " +
                    _rig.Ui.UnlockCard.IsVisible + ", objective " + _rig.Ui.Objective.IsVisible);
                yield return null;
            }

            Assert.IsTrue(readoutSeen, "the boot diagnostics came first, or the test proves nothing");
            Assert.Greater(shownFrames, 0, "the scene did guide, or the test proves nothing");
        }

        [UnityTest]
        public IEnumerator TheVerbChip_BelongsToTheObjective_ShowsItsRealKey_AndRetiresOnTheFirstUse()
        {
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.scan_lander"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the line is up");
            Assert.IsTrue(_rig.Ui.LookUp.IsVisible, "and its chip with it");
            Assert.IsFalse(_rig.Ui.LookUp.ShowsPrompt, "the chip is the objective's, not a prompt of its own");
            Assert.AreEqual(Text("ui.verb.scan"), _rig.Ui.Layout.LookHintWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.LookHintGlyphLabel.text, "the real key of the scan beam");
            Assert.AreEqual(GuidanceKind.Objective, _rig.Ui.Slot.Granted, "one guidance element holds the slot");

            Events.Publish(new ScanStarted("lander.fragment.1", Vector3.zero));
            limit = Time.realtimeSinceStartup + 4f;
            while (_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "the first scan retires the chip");
            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the line stays until the step is done");
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
