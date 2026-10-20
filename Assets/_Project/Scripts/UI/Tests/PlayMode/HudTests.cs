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
            Assert.AreEqual(Text("ui.device.state.cutter_needs"), view.DeviceStateText(FaceDevices.Cutter));
            Assert.AreEqual(Text("ui.device.state.socket_empty"), view.DeviceStateText(FaceDevices.SocketOne));

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
            Assert.AreEqual(Text("ui.device.state.ok"), view.DeviceStateText(FaceDevices.Cutter));
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
        public IEnumerator SevenTab_AFullHold_TurnsItsPage_ByTheArrows_AndByAClickOnThePageCount()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordHoldBooted();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(24, new MaterialSlot(SalvageMaterial.Metal, 4, 4));
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            FaceInventoryView view = _rig.Ui.Cargo.View;
            float scale = _rig.Ui.Cargo.PanelRect.width / FaceInventoryView.ReferenceWidth;
            Assert.AreEqual(0, view.HoldPage, "it opens on the first page");
            StringAssert.EndsWith("1/2", view.PageText);

            view.FocusHoldCell(FaceLayout.PageSize - 1);
            view.Navigate(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(FaceLayout.PageSize - 1 + FaceLayout.Columns, view.FocusedHoldCell);
            Assert.AreEqual(1, view.HoldPage, "down past the last row turns the page");
            StringAssert.EndsWith("2/2", view.PageText);
            Assert.AreEqual(DisplayStyle.None, view.HoldCell(0).resolvedStyle.display, "the first page steps aside");

            yield return null;
            view.PointerDownAtPanel(FaceLayout.PageRect.center * scale);
            Assert.AreEqual(0, view.HoldPage, "a click on the page count turns back");
            Assert.AreEqual(0, view.FocusedHoldCell, "and selects the page's first slot");

            view.FocusHoldCell(1);
            yield return null;
            view.PointerMoveAtPanel(FaceInventoryView.GetHoldCellReferenceRect(FaceLayout.PageSize).center * scale);
            Assert.AreEqual(0, view.FocusedHoldCell,
                "the pointer answers the slot shown there, never the one of the page not shown");
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
                        Assert.GreaterOrEqual(box.xMin, panel.xMin - 1f, $"{lang} {w}x{h}: device {i} stays on the panel");
                        Assert.LessOrEqual(box.xMax, panel.xMax + 1f);
                        Assert.GreaterOrEqual(box.yMin, panel.yMin - 1f);
                        Assert.LessOrEqual(box.yMax, panel.yMax + 1f);
                        for (int j = i + 1; j < FaceDevices.Count; j++)
                        {
                            Assert.IsFalse(box.Overlaps(view.DeviceElement(j).worldBound),
                                $"{lang} {w}x{h}: device {i} frame overlaps device {j}");
                        }

                        Rect state = view.DeviceStateLabel(i).worldBound;
                        Assert.GreaterOrEqual(state.xMin, box.xMin, $"{lang} {w}x{h}: device {i} state stays in its frame");
                        Assert.LessOrEqual(state.xMax, box.xMax + 1f, $"{lang} {w}x{h}: device {i} state stays in its frame");
                        Assert.LessOrEqual(state.yMax, box.yMax + 1f, $"{lang} {w}x{h}: device {i} state stays in its frame");
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
        public IEnumerator PickupFeed_SaysWhatASourcesPickupIsFor_AndACutPieceSaysNothing()
        {
            Boot();
            _rig.Bootstrap.Context.Register<ISupplyPurposes>(new OneSource("supply.lander.open",
                StructureIds.ChargingPad));
            PickupFeed feed = _rig.Ui.Pickups;
            _rig.Fakes.SetMaterials(0, 0, 0);
            yield return Seconds(0.3f);

            Events.Publish(new MaterialSalvaged(SalvageMaterial.Metal, 4, Vector3.zero, "supply.lander.open", 0,
                MaterialSource.Crate));
            _rig.Fakes.SetMaterials(4, 0, 0);
            yield return Seconds(0.3f);
            Assert.AreEqual("· " + Text(UiKeys.PickupPurpose(StructureIds.ChargingPad)),
                feed.PurposeText(SalvageMaterial.Metal), "opening-v3 E3: Metal 4 · for the charging pad");

            Events.Publish(new MaterialSalvaged(SalvageMaterial.Wiring, 2, Vector3.zero, "site.kestrel", 0));
            _rig.Fakes.SetMaterials(4, 2, 0);
            yield return Seconds(0.3f);
            Assert.IsTrue(feed.IsShowing(SalvageMaterial.Wiring));
            Assert.IsEmpty(feed.PurposeText(SalvageMaterial.Wiring), "a piece cut from a wreck names no purpose");
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
        public IEnumerator AfterTheChaseFrame_TheControlsCard_ThenTheFirstObjectiveLine_EachTheOnlyGuidanceElement()
        {
            const float WatchSeconds = 6f;
            const float CardSeconds = 5f;
            Boot();
            _rig.Tune("_controlsCard._maxSeconds", CardSeconds);
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.ControlsCardView.IsVisible, "the controls card comes at the hand-over");
            float limit = Time.realtimeSinceStartup + CardSeconds + 8f;
            while (!_rig.Ui.Objective.ShowsObjective && Time.realtimeSinceStartup < limit)
            {
                Assert.IsFalse(_rig.Ui.Readout.IsVisible, "no boot card: the diagnostics were on 07's face");
                if (_rig.Ui.ControlsCardView.IsVisible)
                {
                    Assert.IsFalse(_rig.Ui.Objective.IsVisible, "the objective line waits for the card");
                    Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompt beside the card");
                    Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "no chip beside the card");
                }

                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the first objective line comes after the card");
            Assert.IsFalse(_rig.Ui.ControlsCardView.IsVisible, "the card has gone");
            Assert.AreEqual(Text("next.v2.drive_lander"), _rig.Ui.Layout.NextLineText.text);
            float until = Time.realtimeSinceStartup + WatchSeconds;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.IsFalse(_rig.Ui.Readout.IsVisible, "no readout card beside it");
                Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompt beside it");
                Assert.IsFalse(_rig.Ui.LookUp.ShowsPrompt, "no look or drive chip beside it");
                Assert.IsFalse(_rig.Ui.UnlockCard.IsVisible, "no card beside it");
                Assert.IsFalse(_rig.Ui.ControlsCardView.IsVisible, "the controls card does not come back");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheFaceBoot_TracesTheMakersMark_LoadsThenGivesWayToTheLines_CueingEachStrokeStepAndLine()
        {
            Boot();
            yield return null;
            int strokes = 0;
            int steps = 0;
            int typed = 0;
            var completed = new System.Collections.Generic.List<int>();
            Events.Subscribe<LogoStrokeDrawn>(_ => strokes++);
            Events.Subscribe<LoadStep>(_ => steps++);
            Events.Subscribe<BootTextTyped>(_ => typed++);
            Events.Subscribe<BootLineCompleted>(line => completed.Add(line.LineIndex));
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new CrtPowerOn(0.6f));
            yield return Seconds(0.7f);
            Assert.IsTrue(_rig.Ui.BootFace.IsShowing, "the tube woke");
            Events.Publish(new BootLogoBegan(2f));
            yield return Seconds(1f);
            Assert.AreEqual(DisplayStyle.Flex, _rig.Ui.BootFaceView.Root.resolvedStyle.display, "the mark is on the face");
            Assert.Greater(strokes, 0, "the beam is tracing");
            yield return Seconds(1.1f);
            Assert.AreEqual(Text(UiKeys.BootMaker), _rig.Ui.BootFaceView.Wordmark.text, "the wordmark has typed");
            Events.Publish(new BootLoadBegan(0.8f));
            yield return Seconds(0.9f);
            Assert.AreEqual(Text(UiKeys.BootLoading), _rig.Ui.BootFaceView.LoadText.text);
            Assert.IsTrue(_rig.Ui.BootFaceView.Step(4).ClassListContains(BootFaceView.StepFilledClass), "loaded");
            Events.Publish(new BootCheckStarted(BootCheckStep.Carrier));
            yield return Seconds(0.8f);
            Events.Publish(new BootCheckCompleted(BootCheckStep.Carrier, BootCheckResult.Info));
            yield return Seconds(0.2f);

            Assert.IsFalse(_rig.Ui.BootFace.IsShowing, "the lines have the face");
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.BootFaceView.Root.resolvedStyle.display);
            Assert.AreEqual(_rig.Tuning.BootFace.Linework.StrokeCount, strokes, "each stroke cued once");
            Assert.AreEqual(_rig.Tuning.BootFace.LoadDwells.Count, steps, "each load step cued once");
            Assert.Greater(typed, 10, "typing cues for the wordmark, the load line and the first line");
            CollectionAssert.AreEqual(new[] { BootFace.WordmarkLine, BootFace.LoadLine, BootFace.FirstPostLine },
                completed, "each line done once, in order");
        }

        /// <summary>Share (%) of the rig's HUD height left below the log's column, so it is about a face tall.</summary>
        private const float FaceFreeShare = 45f;

        [UnityTest]
        public IEnumerator TheSelfTestLog_ScrollsUpOnceItFillsTheFace_KeepingTheNewestLineInView()
        {
            Boot();
            yield return null;
            // The rig has no face panel, so the log sits on the tall HUD: a face-sized column makes it fill as on 07.
            _rig.Ui.PostView.Root.style.bottom = Length.Percent(FaceFreeShare);
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            for (int i = 0; i <= (int)BootCheckStep.Begin; i++)
            {
                var step = (BootCheckStep)i;
                Events.Publish(new BootCheckStarted(step));
                yield return Seconds(0.25f);
                Events.Publish(new BootCheckCompleted(step, BootCheckResult.Ok));
                yield return Seconds(0.15f);
            }

            PostView view = _rig.Ui.PostView;
            Assert.Greater(view.ScrolledBy, 0f, "fourteen lines and two blocks overfill the face: the log scrolled up");
            Rect last = view.Line((int)BootCheckStep.Begin).worldBound;
            Rect room = view.Root.worldBound;
            Assert.LessOrEqual(last.yMax, room.yMax + 1f, "the newest line is in view");
            Assert.Greater(last.yMin, room.yMin, "not scrolled past it");
        }

        [UnityTest]
        public IEnumerator TheControlsCard_ShowsOnceAfterTheOpening_GoesOnceThePlayerDroveAndLooked_AndNotOnAReload()
        {
            InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Boot();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.ControlsCardView.IsVisible, "not during the wake");
            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.ControlsCardView.IsVisible, "at the hand-over");
            Assert.AreEqual(Text(UiKeys.KeyDrive), _rig.Ui.Layout.ControlsDriveGlyphLabel.text);
            Assert.AreEqual(Text(UiKeys.Drive), _rig.Ui.Layout.ControlsDriveWord.text);
            Assert.AreEqual(Text(UiKeys.MouseLook), _rig.Ui.Layout.ControlsLookGlyphLabel.text);
            Assert.AreEqual(Text(UiKeys.KeyTab), _rig.Ui.Layout.ControlsFaceGlyphLabel.text);
            Assert.AreEqual("L", _rig.Ui.Layout.ControlsLightsGlyphLabel.text, "the lights key's real binding");
            Assert.AreEqual(Text(UiKeys.ControlsLights), _rig.Ui.Layout.ControlsLightsWord.text);
            Rect card = _rig.Ui.Layout.ControlsCard.worldBound;
            Rect root = _rig.Ui.Layout.Root.worldBound;
            Assert.Less(card.center.x, root.center.x, "at the left");
            Assert.Greater(card.center.y, root.center.y, "at the bottom");

            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return Tap(gamepad.buttonSouth);
            yield return Seconds(0.1f);
            Assert.AreEqual(Text(UiKeys.PadDrive), _rig.Ui.Layout.ControlsDriveGlyphLabel.text, "the pad's caps");
            Assert.AreEqual(Text(UiKeys.PadView), _rig.Ui.Layout.ControlsFaceGlyphLabel.text);
            Assert.AreEqual(Text("glyph.gamepad.leftstickpress"), _rig.Ui.Layout.ControlsLightsGlyphLabel.text);

            _rig.Fakes.Speed = 3f;
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.ControlsCard.WantsShown, "driving alone leaves the look still to learn");
            float lookUntil = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < lookUntil)
            {
                Set(mouse.delta, new Vector2(12f, 0f), queueEventOnly: true);
                yield return null;
            }

            _rig.Fakes.Speed = 0f;
            Assert.IsFalse(_rig.Ui.ControlsCard.WantsShown, "driven and looked: its job is done");
            yield return Seconds(_rig.Tuning.ControlsCard.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(_rig.Ui.ControlsCardView.IsVisible, "it has faded out");

            _rig.Dispose();
            Boot();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.IsFalse(_rig.Ui.ControlsCardView.IsVisible, "a reloaded save plays the short wake: no card");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Readout_ACutterChangeGetsItsOwnCard_AndOnlyAMilestoneReachedNowShowsTheProtocolLine()
        {
            Boot();
            _rig.Tune("_guidance._statusHoldSeconds", 1.5f);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            ReadoutView readout = _rig.Ui.Readout;

            Events.Publish(new CutterRefused(Vector3.zero));
            yield return null;
            Assert.AreEqual(ReadoutKind.System, readout.Showing, "a refused cut shows the cutter's line");
            Assert.IsTrue(readout.IsFlashing(0), "and flashes it");
            Assert.AreEqual(1, readout.LineCount);
            StringAssert.StartsWith(Text("ui.system.cutter"), readout.LineText(0));
            StringAssert.EndsWith(Text("ui.system.state.fault_igniter"), readout.LineText(0));

            yield return Seconds(_rig.Tuning.Guidance.StatusHoldSeconds + _rig.Tuning.Guidance.Readout.FadeOut + 0.6f);
            Assert.IsFalse(readout.IsVisible);
            _rig.Fakes.Systems.Set(RoverSystem.Cutter, RoverSystemState.Ok);
            Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, false));
            yield return Seconds(_rig.Tuning.Guidance.Readout.FadeIn + 0.4f);
            Assert.AreEqual(ReadoutKind.System, readout.Showing, "the mended cutter gets its own card");
            StringAssert.EndsWith(Text("ui.system.state.ok"), readout.LineText(0), "the line reads OK");

            yield return Seconds(_rig.Tuning.Guidance.StatusHoldSeconds + _rig.Tuning.Guidance.Readout.FadeOut + 0.6f);
            Assert.IsFalse(readout.IsVisible);
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
        public IEnumerator NoFrame_ShowsTwoGuidanceElements_SaveTheBriefCrossFadeFromTheLineToAPrompt()
        {
            Boot();
            _rig.Tune("_guidance._statusHoldSeconds", 2f);
            _rig.Tune("_controlsCard._maxSeconds", 5f);
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle));
            Events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            Events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, false));
            Events.Publish(new TickerLine("battery.low.lander"));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 10f), true);
            int shownFrames = 0;
            bool readoutSeen = false;
            float crossFade = 0f;
            float crossFadeLimit = _rig.Tuning.Guidance.HandOverSeconds + _rig.Tuning.Guidance.Line.FadeOut + 0.2f;
            float until = Time.realtimeSinceStartup + 18f;
            float last = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < until)
            {
                float now = Time.realtimeSinceStartup;
                readoutSeen |= _rig.Ui.Readout.IsVisible;
                int up = (_rig.Ui.Prompt.IsVisible ? 1 : 0) + (_rig.Ui.LookUp.ShowsPrompt ? 1 : 0) +
                         (_rig.Ui.Readout.IsVisible ? 1 : 0) + (_rig.Ui.UnlockCard.IsVisible ? 1 : 0) +
                         (_rig.Ui.Objective.IsVisible ? 1 : 0) + (_rig.Ui.ControlsCardView.IsVisible ? 1 : 0);
                shownFrames += up > 0 ? 1 : 0;
                bool lineToPrompt = up == 2 && _rig.Ui.Prompt.IsVisible && _rig.Ui.Objective.ShowsObjective;
                crossFade = lineToPrompt ? crossFade + (now - last) : 0f;
                last = now;
                Assert.LessOrEqual(crossFade, crossFadeLimit, "the line and the prompt cross-fade only briefly");
                Assert.LessOrEqual(lineToPrompt ? 1 : up, 1, "one guidance element at a time: prompt " +
                    _rig.Ui.Prompt.IsVisible + ", look-up " + _rig.Ui.LookUp.ShowsPrompt + ", readout " +
                    _rig.Ui.Readout.IsVisible + ", unlock " + _rig.Ui.UnlockCard.IsVisible + ", objective " +
                    _rig.Ui.Objective.IsVisible + ", controls " + _rig.Ui.ControlsCardView.IsVisible);
                yield return null;
            }

            Assert.IsTrue(readoutSeen, "the protocol card came, or the test proves nothing");
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
            Events.Publish(new FragmentScanned("lander.fragment.1", "lander", 0, 2, false, 0));
            limit = Time.realtimeSinceStartup + 4f;
            while (_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "the first scan retires the chip");
            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the line stays until the step is done");
        }

        [UnityTest]
        public IEnumerator TheVerbChip_StandingStillInReachOfTheSecondFragment_ShowsWithinHalfASecond()
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

            Events.Publish(new ScanStarted("lander.fragment.1", Vector3.zero));
            Events.Publish(new FragmentScanned("lander.fragment.1", "lander", 0, 2, false, 0));
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Scan);
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "the first fragment is scanned and nothing else is in reach");
            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the line is still up: one fragment is left");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, new Vector3(0f, 0f, 6f), true);
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.LookUp.IsVisible, "07 stands in reach of the second fragment: its key shows");
            Assert.AreEqual(Text("ui.verb.scan"), _rig.Ui.Layout.LookHintWord.text);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "the learned verb's prompt stays retired: one element");

            _rig.Fakes.PrimaryHint = InteractionHint.None;
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "out of reach, the chip bows out again");
        }

        [UnityTest]
        public IEnumerator AtACrateSeal_WithAScanTargetAlsoInReach_TheChipReadsECut_AndHoldsItWhileEIsHeld()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Salvage);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.scan_lander"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.AreEqual(Text(UiKeys.VerbScan), _rig.Ui.Layout.LookHintWord.text, "the scan step's own key first");
            var seal = new Vector3(0.4f, 0.5f, 5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, true);
            yield return UntilWord(UiKeys.VerbCut);
            Assert.IsTrue(_rig.Ui.LookUp.IsVisible);
            Assert.AreEqual(Text(UiKeys.VerbCut), _rig.Ui.Layout.LookHintWord.text,
                "the crate is the candidate: the chip names what E does there");

            _rig.Fakes.IsCutting = true;
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, new Vector3(-0.6f, 0.4f, 5f), true);
            float until = Time.realtimeSinceStartup + 1.5f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.AreEqual(Text(UiKeys.VerbCut), _rig.Ui.Layout.LookHintWord.text,
                    "while E cuts, the chip stays Cut even as the scan target slides in front");
                yield return null;
            }

            _rig.Fakes.IsCutting = false;
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            yield return UntilWord(UiKeys.VerbScan);
            Assert.AreEqual(Text(UiKeys.VerbScan), _rig.Ui.Layout.LookHintWord.text,
                "nothing in reach: the step's own key again");
        }

        [UnityTest]
        public IEnumerator OnTheCutStep_AfterAStoryScan_OrAScanWhoseEndWasLost_AReadyCutReadsECut(
            [Values(true, false)] bool storyEnd)
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Salvage);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new ScanStarted("airlock_panel", Vector3.zero));
            if (storyEnd)
            {
                Events.Publish(new StoryScanned("airlock_panel", "story.airlock_panel", Vector3.zero));
            }

            yield return null;
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            var seal = new Vector3(0.4f, 0.5f, 5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, true);
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.LookUp.IsVisible, "the cut step's chip shows at the seal");
            Assert.AreEqual(Text(UiKeys.VerbCut), _rig.Ui.Layout.LookHintWord.text,
                "E is not held and the seal is ready: the chip reads Cut, never a scan that is over");
        }

        [UnityTest]
        public IEnumerator TheFailedReply_PutsTxNoRangeOn07sFace_AsTheOneElement_ForAboutThreeSeconds()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown);

            Events.Publish(new CallReplyFailed());
            float limit = Time.realtimeSinceStartup + 4f;
            while (!_rig.Ui.FaceNotice.IsShown && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.FaceNotice.IsShown, "07's face shows its line");
            Assert.AreEqual(Text(UiKeys.FaceTxNoRange), _rig.Ui.FaceNotice.Text);
            Assert.IsFalse(_rig.Ui.Objective.ShowsObjective, "it is the one element: the objective line is gone");
            float shownAt = Time.realtimeSinceStartup;
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "and every chip has faded away");
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
            while (_rig.Ui.FaceNotice.IsShown && Time.realtimeSinceStartup < shownAt + 6f)
            {
                yield return null;
            }

            float shownFor = Time.realtimeSinceStartup - shownAt;
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown, "the line lets go");
            Assert.That(shownFor, Is.InRange(_rig.Tuning.Guidance.ReplyFaceSeconds - 0.4f,
                _rig.Tuning.Guidance.ReplyFaceSeconds + 0.6f), "after about three seconds");
        }

        [UnityTest]
        public IEnumerator AimingAtARackOutline_NamesThePartAndWhereItWasSeen_OnAChipWithNoKey()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Rack, new Vector3(1.9f, 0.7f, 4f), true,
                "rack.headlamp");
            float limit = Time.realtimeSinceStartup + 4f;
            while (!_rig.Ui.Prompt.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "the chip names the outline 07 looks at");
            Assert.AreEqual(Text("rack.headlamp"), _rig.Ui.Layout.PromptWord.text, "the part and where 07 saw it");
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.PromptGlyph.style.display.value, "no key to press");
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.PromptHoldRing.style.display.value);
            Assert.IsFalse(_rig.Ui.Objective.ShowsObjective, "it is the one guidance element");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 4f), true);
            limit = Time.realtimeSinceStartup + 6f;
            while (_rig.Ui.Layout.PromptWord.text != Text(UiKeys.Hint(InteractionKind.Build)) &&
                   Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Build)), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual(DisplayStyle.Flex, _rig.Ui.Layout.PromptGlyph.style.display.value,
                "a verb's chip has its key again");
        }

        [UnityTest]
        public IEnumerator ALoadInHookReach_TeachesTheTetherKeyOnce_AtItsHitch()
        {
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            var hitch = new Vector3(1f, 0.5f, 4f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, hitch, true);
            float limit = Time.realtimeSinceStartup + 4f;
            while (!_rig.Ui.Prompt.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "the chip shows at the load's hitch");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Tether)), _rig.Ui.Layout.PromptWord.text, "Hook cable");
            Assert.AreEqual(Text("glyph.mouse.rightbutton"), _rig.Ui.Layout.PromptGlyphLabel.text, "the tether key");
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.PromptHoldRing.style.display.value, "a press, not a hold");

            Events.Publish(new TowAttached("habitat.power_module", hitch, 1f));
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "hooking a load ends the chip");

            _rig.Fakes.PrimaryHint = InteractionHint.None;
            yield return Seconds(0.5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, hitch + Vector3.right * 20f, true);
            float until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until)
            {
                Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "it teaches once: never again, even at another load");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheCall_ShowsOn07sFace_AfterTwoSecondsStill_AndForThreeOnAChange_NeverBesideAChip()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Fakes.Speed = 3f;
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new CallStateChanged(CallState.Broken, true));
            Events.Publish(new CallDecoded(0, 5, true));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.CallView.IsVisible, "a replayed call shows nothing while 07 drives");

            _rig.Fakes.Speed = 0f;
            float stoppedAt = Time.realtimeSinceStartup;
            float limit = stoppedAt + 4f;
            while (!_rig.Ui.CallView.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            float still = _rig.Tuning.CallDisplay.StillSeconds;
            Assert.IsTrue(_rig.Ui.CallView.IsVisible, "standing still, 07's face shows the call");
            Assert.That(Time.realtimeSinceStartup - stoppedAt, Is.InRange(still - 0.2f, still + 0.6f), "after 2 s");
            Assert.AreEqual("??? ??", _rig.Ui.CallView.Letters, "nothing has decoded yet");
            Assert.AreEqual(0, _rig.Ui.CallView.AccentLetters);

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 4f), true);
            yield return OneElement(3f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "a chip takes the slot from the still call");
            Assert.IsFalse(_rig.Ui.CallView.IsVisible);

            _rig.Fakes.Speed = 3f;
            Events.Publish(new CallStateChanged(CallState.Locked, false));
            Events.Publish(new CallDecoded(1, 5, false));
            limit = Time.realtimeSinceStartup + 3f;
            while (!_rig.Ui.CallView.IsVisible && Time.realtimeSinceStartup < limit)
            {
                Assert.IsFalse(_rig.Ui.CallView.IsVisible && _rig.Ui.Prompt.IsVisible, "never beside the chip");
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.CallView.IsVisible, "a change takes the face even while 07 drives");
            Assert.AreEqual("A?? ??", _rig.Ui.CallView.Letters);
            Assert.AreEqual(1, _rig.Ui.CallView.AccentLetters, "the decoded letter is the one accent");
            float shownAt = Time.realtimeSinceStartup;
            while (_rig.Ui.CallView.IsVisible && Time.realtimeSinceStartup < shownAt + 6f)
            {
                Assert.IsFalse(_rig.Ui.Prompt.IsVisible || _rig.Ui.LookUp.IsVisible, "never beside a chip");
                yield return null;
            }

            float change = _rig.Tuning.CallDisplay.ChangeSeconds;
            Assert.That(Time.realtimeSinceStartup - shownAt, Is.InRange(change - 0.2f, change + 0.8f),
                "for about three seconds, then it lets go");
            yield return OneElement(2f);
        }

        /// <summary>Waits <paramref name="seconds"/>, failing on any frame where the call shares the screen.</summary>
        private IEnumerator OneElement(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                bool chip = _rig.Ui.Prompt.IsVisible || _rig.Ui.LookUp.IsVisible;
                Assert.IsFalse(_rig.Ui.CallView.IsVisible && chip, "the call and a chip on screen together");
                Assert.IsFalse(_rig.Ui.CallView.IsVisible && _rig.Ui.Objective.ShowsObjective,
                    "the call and the objective line on screen together");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheAnswer_PutsTxReadyThenTheAckRouteOn07sFace_EachAsTheOneElement()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown);

            Events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Ready, true));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown, "a loaded game's ready phase shows nothing");
            Events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Ready, false));
            yield return WaitForFaceNotice(2f);
            Assert.IsTrue(_rig.Ui.FaceNotice.IsShown, "the answer becomes possible: 07's face says so");
            Assert.AreEqual(Text(UiKeys.FaceTxReady), _rig.Ui.FaceNotice.Text);
            float readyFor = 0f;
            float since = Time.realtimeSinceStartup;
            while (_rig.Ui.FaceNotice.IsShown && Time.realtimeSinceStartup < since + 8f)
            {
                yield return null;
            }

            readyFor = Time.realtimeSinceStartup - since;
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown);
            Assert.That(readyFor, Is.InRange(_rig.Tuning.Guidance.TxReadySeconds - 0.5f,
                _rig.Tuning.Guidance.TxReadySeconds + 0.8f));

            Events.Publish(new TransmitPrompted());
            yield return WaitForFaceNotice(2f);
            Assert.AreEqual(Text(UiKeys.FaceTxReady), _rig.Ui.FaceNotice.Text, "stepping up to the mast: again");
            yield return Seconds(_rig.Tuning.Guidance.TxPromptSeconds + 1.2f);
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown);

            Events.Publish(new RelayAcked(Vector3.zero));
            yield return WaitForFaceNotice(2f);
            Assert.AreEqual(Text(UiKeys.FaceAck), _rig.Ui.FaceNotice.Text, "relay.1 answered: the route is read out");
            Assert.IsFalse(_rig.Ui.Objective.ShowsObjective, "the one element");
        }

        private IEnumerator WaitForFaceNotice(float seconds)
        {
            float limit = Time.realtimeSinceStartup + seconds;
            while (!_rig.Ui.FaceNotice.IsShown && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AHeldEWithNoLineOfSight_DimsAndShakesTheChip_And07sFaceSaysWhy_WithNoNewPanel()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Salvage);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            var seal = new Vector3(0.4f, 0.5f, 5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, true);
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.LookUp.IsVisible, "the cut step's chip is up at the seal");
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown);

            Events.Publish(new InteractionBlocked(InteractionKind.Salvage, seal, BlockReason.NoLineOfSight));
            yield return null;
            yield return null;
            Assert.IsTrue(_rig.Ui.FaceNotice.IsShown, "07's face answers the key");
            Assert.AreEqual(Text(UiKeys.FaceNoLine), _rig.Ui.FaceNotice.Text);
            Assert.Less(_rig.Ui.Layout.LookHintWord.resolvedStyle.opacity, 0.99f, "the chip dims");
            bool moved = false;
            float until = Time.realtimeSinceStartup + _rig.Tuning.Guidance.BlockedChipSeconds * 0.5f;
            while (Time.realtimeSinceStartup < until)
            {
                moved |= System.Math.Abs(_rig.Ui.Layout.LookHint.style.translate.value.x.value) > 0.01f;
                Assert.IsFalse(_rig.Ui.Readout.IsVisible, "no readout card for it");
                Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompt for it");
                yield return null;
            }

            Assert.IsTrue(moved, "the chip shakes a little");
            yield return Seconds(_rig.Tuning.Guidance.BlockedChipSeconds + _rig.Tuning.Guidance.BlockedFaceSeconds);
            Assert.IsFalse(_rig.Ui.FaceNotice.IsShown, "the face lets go");
            Assert.AreEqual(0f, _rig.Ui.Layout.LookHint.style.translate.value.x.value, "the chip is back in place");
            Assert.AreEqual(1f, _rig.Ui.Layout.LookHintWord.style.opacity.value, "at full");
            Assert.AreEqual(Text(UiKeys.VerbCut), _rig.Ui.Layout.LookHintWord.text, "still naming the cut");
        }

        [UnityTest]
        public IEnumerator OnTheCutStep_TheLinesChipNamesWhatEDoesWhere07Stands_ScanByAFragment_CutAtTheSeal()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Scan);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new ScanStarted("airlock_panel", Vector3.zero));
            Events.Publish(new StoryScanned("airlock_panel", "story.airlock_panel", Vector3.zero));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, new Vector3(-1f, 0.3f, 4f), true);
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.AreEqual(Text("next.v2.cut_optics"), _rig.Ui.Layout.NextLineText.text, "the cut line is up");
            Assert.AreEqual(Text(UiKeys.VerbScan), _rig.Ui.Layout.LookHintWord.text,
                "by the airlock an unscanned fragment is in reach: E would scan it, and the chip says so");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, new Vector3(0.4f, 0.5f, 5f), true);
            yield return UntilWord(UiKeys.VerbCut);
            Assert.IsTrue(_rig.Ui.LookUp.IsVisible);
            Assert.AreEqual(Text(UiKeys.VerbCut), _rig.Ui.Layout.LookHintWord.text,
                "at the seal the same chip, with the same line, names the cut on its first use");
            Assert.AreEqual(Text("next.v2.cut_optics"), _rig.Ui.Layout.NextLineText.text);
        }

        [UnityTest]
        public IEnumerator EachPieceOfAScanSet_ShowsItsBlueprintsProgressBriefly_OverTheLine_LearnedOnTheLast()
        {
            Boot();
            _rig.Tune("_guidance._statusHoldSeconds", 1.5f);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.drive_lander"));
            ReadoutView readout = _rig.Ui.Readout;
            float wait = _rig.Tuning.Guidance.Readout.FadeIn + 0.4f;
            float gone = _rig.Tuning.Guidance.StatusHoldSeconds + _rig.Tuning.Guidance.Readout.FadeOut + 0.6f;

            Events.Publish(new FragmentScanned("array.1", "solar_array", 0, 3, false, 0));
            yield return Seconds(wait);
            Assert.AreEqual(ReadoutKind.ScanSet, readout.Showing, "the first piece says how far the blueprint is");
            Assert.AreEqual(1, readout.LineCount, "in one short line");
            Assert.AreEqual(string.Format(Text(ReadoutText.ScanSetKey), Text("structure.solar_array.name"), 1, 3),
                readout.LineText(0));
            Assert.IsFalse(_rig.Ui.Objective.IsVisible, "one guidance element: the card stands over the line");
            yield return Seconds(gone);
            Assert.IsFalse(readout.IsVisible, "and yields by itself");

            Events.Publish(new FragmentScanned("array.3", "solar_array", 2, 3, false, 0));
            yield return Seconds(wait);
            Assert.AreEqual(string.Format(Text(ReadoutText.ScanSetLearnedKey), Text("structure.solar_array.name"), 3, 3),
                readout.LineText(0), "the last piece: learned");
            yield return Seconds(gone);

            Events.Publish(new FragmentScanned("array.1", "solar_array", 0, 3, true, 4));
            Events.Publish(new FragmentScanned("pad.1", "charging_pad", 0, 1, false, 0));
            yield return Seconds(wait);
            Assert.IsFalse(readout.IsVisible, "a known blueprint (it pays metal) and a one-piece set need no count");
        }

        [UnityTest]
        public IEnumerator AimingAtAnUnlearnedSetPiece_TheChipReadsEScanWithItsPlaceInTheSet()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            _rig.Ui.Ledger.RecordUsed(InteractionKind.Scan);
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            var piece = new Vector3(-1f, 0.3f, 4f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, piece, true, null, 1, 3);
            Events.Publish(new NextStepChanged("next.v2.scan_lander"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.LookUp.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.AreEqual(Text(UiKeys.VerbScan) + " 1/3", _rig.Ui.Layout.LookHintWord.text,
                "the first piece of three");
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, piece, true, null, 2, 3);
            yield return null;
            yield return null;
            Assert.AreEqual(Text(UiKeys.VerbScan) + " 2/3", _rig.Ui.Layout.LookHintWord.text, "the next piece");
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Scan, piece, true);
            yield return null;
            yield return null;
            Assert.AreEqual(Text(UiKeys.VerbScan), _rig.Ui.Layout.LookHintWord.text,
                "a known set or a lone target: no count");
        }

        [UnityTest]
        public IEnumerator AfterTheB4Collect_TheBuildPadLineShowsWithinTheNormalDwell()
        {
            const float LineWithin = 10f;
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.scan_lander"));
            yield return Seconds(1f);
            Events.Publish(new ScanStarted("lander.cargo.1", Vector3.zero));
            Events.Publish(new FragmentScanned("lander.cargo.1", "charging_pad", 0, 2, false, 0));
            yield return Seconds(0.5f);
            Events.Publish(new ScanStarted("lander.cargo.2", Vector3.zero));
            Events.Publish(new FragmentScanned("lander.cargo.2", "charging_pad", 1, 2, false, 0));
            Events.Publish(new NextStepChanged("next.v2.collect_parts"));
            yield return Seconds(1f);
            _rig.Fakes.Hold.Set(8, new MaterialSlot(SalvageMaterial.Metal, 4, 4));
            _rig.Fakes.Hold.AddPart("part.igniter");
            Events.Publish(new CargoChanged(2, 8));
            Events.Publish(new NextStepChanged("next.v2.build_pad"));
            string expected = Text("next.v2.build_pad");
            float until = Time.realtimeSinceStartup + LineWithin;
            while (!(_rig.Ui.Objective.ShowsObjective && _rig.Ui.Layout.NextLineText.text == expected) &&
                   Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective,
                "the build-pad line comes back after the collect: the set's card and the Tab chip each teach briefly " +
                $"(slot {_rig.Ui.Slot.Granted}, readout {_rig.Ui.Readout.Showing}, " +
                $"chip '{_rig.Ui.Layout.LookHintWord.text}')");
            Assert.AreEqual(expected, _rig.Ui.Layout.NextLineText.text);
        }

        [UnityTest]
        public IEnumerator AnUnlearnedReadyVerb_TakesTheSlotFromTheObjectiveLine_WhenSevenNosesIntoACrate()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.Objective.ShowsObjective && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "07 stands still: the step's line is up");
            var seal = new Vector3(0.4f, 0.5f, 5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, true);
            _rig.Fakes.HasTarget = true;
            _rig.Fakes.CutPoint = seal;
            _rig.Fakes.Material = SalvageMaterial.Optics;
            limit = Time.realtimeSinceStartup + 3f;
            while (!_rig.Ui.Prompt.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "nosed in at the seal, the cut is taught within 3 s");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Salvage)), _rig.Ui.Layout.PromptWord.text);
        }

        [UnityTest]
        public IEnumerator AnUnlearnedCut_With07ArrivingToFaceTheSeal_ShowsECutWithinTheNormalDwell(
            [Values(true, false)] bool ready)
        {
            const float ArrivalSeconds = 2f;
            const float Reveal = 0.5f;
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Speed = 3f;
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            yield return Seconds(ArrivalSeconds);

            // 07 stops 4.5 m from the seal and faces it: the cut is the step's verb. Ready, its prompt teaches; not
            // ready yet (the beam still busy), the step's own chip has to say it.
            var seal = new Vector3(0.4f, 0.5f, 4.5f);
            _rig.Fakes.Speed = 0f;
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, ready);
            _rig.Fakes.HasTarget = true;
            _rig.Fakes.CutPoint = seal;
            _rig.Fakes.Material = SalvageMaterial.Optics;
            float dwell = _rig.Tuning.Prompts.Find(InteractionKind.Salvage).DwellSeconds;
            float arrived = Time.realtimeSinceStartup;
            float shownAfter = -1f;
            while (Time.realtimeSinceStartup - arrived < 4f && shownAfter < 0f)
            {
                yield return null;
                bool prompt = _rig.Ui.Prompt.IsVisible
                    && _rig.Ui.Layout.PromptWord.text == Text(UiKeys.Hint(InteractionKind.Salvage));
                bool chip = _rig.Ui.LookUp.IsVisible && _rig.Ui.Layout.LookHintWord.text == Text("ui.verb.cut");
                if (prompt || chip)
                {
                    shownAfter = Time.realtimeSinceStartup - arrived;
                }
            }

            Assert.GreaterOrEqual(shownAfter, 0f, "E Cut never showed at the seal");
            Assert.LessOrEqual(shownAfter, dwell + Reveal, $"E Cut showed only after {shownAfter:0.00} s at the seal");

            // 07 turns away: once the prompt has gone it holds no stale words.
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            float left = Time.realtimeSinceStartup;
            while (_rig.Ui.Prompt.IsVisible && Time.realtimeSinceStartup - left < 4f)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(string.Empty, _rig.Ui.Layout.PromptWord.text, "a hidden prompt holds no words");
        }

        [UnityTest]
        public IEnumerator TheHandOverFromTheLineToAnUnlearnedVerb_IsNeverBlank_TheLineGoesOnlyOnceThePromptIsComing()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Ui.Ledger.RecordLookTaught();
            _rig.Ui.Ledger.RecordDriveTaught();
            _rig.Ui.Ledger.RecordHoldChipShown();
            yield return null;
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            Events.Publish(new NextStepChanged("next.v2.cut_optics"));
            float limit = Time.realtimeSinceStartup + 12f;
            while (!_rig.Ui.Objective.ShowsObjective && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Objective.ShowsObjective, "the step's line is up");
            var seal = new Vector3(0.4f, 0.5f, 5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, seal, true);
            _rig.Fakes.HasTarget = true;
            _rig.Fakes.CutPoint = seal;
            _rig.Fakes.Material = SalvageMaterial.Optics;
            float lineAtPrompt = -1f;
            limit = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < limit && !(lineAtPrompt >= 0f && !_rig.Ui.Objective.ShowsObjective))
            {
                yield return null;
                Assert.IsTrue(_rig.Ui.Objective.ShowsObjective || _rig.Ui.Prompt.IsVisible,
                    "one of the two is always on screen through the hand-over");
                if (lineAtPrompt < 0f && _rig.Ui.Prompt.IsVisible)
                {
                    lineAtPrompt = _rig.Ui.Layout.NextLine.resolvedStyle.opacity;
                }
            }

            Assert.GreaterOrEqual(lineAtPrompt, 0f, "the cut prompt came, or the test proves nothing");
            Assert.Greater(lineAtPrompt, 0.9f, "the line had not begun to fade when the prompt began to fade in");
            Assert.IsFalse(_rig.Ui.Objective.ShowsObjective, "then the line gave the screen to the prompt");
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
        }

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
        }

        /// <summary>Waits (2 s at most) for the chip to fade out its old word and show <paramref name="key"/>.</summary>
        private IEnumerator UntilWord(string key)
        {
            string word = Text(key);
            float limit = Time.realtimeSinceStartup + 2f;
            while ((_rig.Ui.Layout.LookHintWord.text != word || !_rig.Ui.LookUp.IsVisible) &&
                   Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }
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

        /// <summary>One source of the opening and the structure it is for.</summary>
        private sealed class OneSource : ISupplyPurposes
        {
            private readonly string _sourceId;
            private readonly string _structureId;

            public OneSource(string sourceId, string structureId)
            {
                _sourceId = sourceId;
                _structureId = structureId;
            }

            public bool TryPurposeOf(string sourceId, out string structureId)
            {
                bool known = sourceId == _sourceId;
                structureId = known ? _structureId : null;
                return known;
            }
        }
    }
}
