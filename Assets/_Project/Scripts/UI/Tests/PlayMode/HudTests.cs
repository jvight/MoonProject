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
    /// The driving HUD end to end: prompts appear only near usable things and only a few times, the reticle only while
    /// aiming, the materials chip only on change, the memory card after a deposit, and the tower panel buys only on a
    /// deliberate hold.
    /// </summary>
    public sealed class HudTests : InputTestFixture
    {
        /// <summary>About when the tower's new section starts rising after a purchase (the port moment).</summary>
        private const float StitchDelay = 1.3f;

        private const float ShortCueWait = 1.5f;

        /// <summary>The upper third of the screen: the zone of place cards and kit titles (first-hour-v2 6.3).</summary>
        private const float UpperThird = 1f / 3f;

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
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, new Vector3(0f, 0f, 12f), true);
            yield return null;
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "no prompts before 07 wakes");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual("Dig", _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text, "keyboard glyph from the real binding");

            yield return Tap(gamepad.buttonSouth);
            yield return Seconds(0.1f);
            Assert.AreEqual(InputDeviceKind.Gamepad, _rig.Bootstrap.Context.Input.ActiveDevice);
            Assert.AreEqual("X", _rig.Ui.Layout.PromptGlyphLabel.text, "the glyph follows the device");
            Assert.IsTrue(_rig.Ui.Layout.PromptGlyph.ClassListContains(GlyphView.PadClass));

            Events.Publish(new ExcavationStarted(new Vector3(0f, 0f, 12f)));
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "doing it ends the prompt");
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Excavate));
            Assert.AreEqual(1, _rig.Ui.Ledger.Shown(InteractionKind.Excavate));
        }

        [UnityTest]
        public IEnumerator Prompt_HidesBehindTheCamera_AndWhilePaused()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, -10f), true);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "never pinned to the screen edge for something behind you");

            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tether, new Vector3(2f, 0f, 10f), true);
            yield return Seconds(1f);
            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "the pause menu has the stage alone");
        }

        [UnityTest]
        public IEnumerator CargoScreen_OpensOnTab_ShowsTheHoldAndHome_AndClosesOnTab_WithoutPausing()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(5, new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 2, 4), new MaterialSlot(SalvageMaterial.Ilmenite, 3, 6));
            _rig.Fakes.Hold.WaitingBundles = 3;
            yield return Seconds(0.3f);

            yield return Tap(keyboard.tabKey);
            yield return Seconds(0.6f);
            CargoScreen cargo = _rig.Ui.Cargo;
            Assert.IsTrue(cargo.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "the game does not pause");
            Assert.AreEqual(8, cargo.HoldCellCount, "a cell for every slot of 07's hold");
            StringAssert.Contains("3", cargo.WaitingText);
            Assert.AreEqual(string.Format(Text(UiKeys.CargoSummary), 3, 8), cargo.SummaryText);

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
        public IEnumerator UnlockCard_SaysWhatAFittedKitLetsSevenDo_AfterTheKitTitle()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.3f);
            Events.Publish(new RoverKitFitted(RoverKitPiece.HoverCoils, false, "rover.hover_jump"));
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.UnlockCard.IsWaiting || _rig.Ui.UnlockCard.IsVisible);
            float limit = Time.realtimeSinceStartup + 20f;
            while (!_rig.Ui.UnlockCard.IsVisible && Time.realtimeSinceStartup < limit)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Ui.UnlockCard.IsVisible, "the card follows the kit's title");
            Assert.AreEqual(RoverAbility.HoverJump, _rig.Ui.UnlockCard.Shown);
            Assert.AreEqual(Text("ui.can.hover_jump.verb"), _rig.Ui.Layout.UnlockCard.Q<Label>("unlock-card-verb").text);
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
        public IEnumerator CrateNote_ShowsThePackingNote_RestsToReadIt_AndLeavesByItself()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.3f);
            Events.Publish(new CrewCrateOpened("site.depot", "kenji", SalvageMaterial.Metal, 3, Vector3.zero));
            yield return Seconds(_rig.Tuning.CrateNote.Reveal.FadeIn + 0.5f);
            CrateNote note = _rig.Ui.CrateNote;
            Assert.IsTrue(note.IsVisible);
            Assert.AreEqual(Text("crate.depot.note"), note.Text, "the note as Kenji wrote it");
            Assert.AreEqual(1f, Time.timeScale, "it never pauses the game");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen);
            yield return Seconds(_rig.Tuning.CrateNote.HoldSeconds(note.Text.Length) +
                                 _rig.Tuning.CrateNote.Reveal.FadeOut + 0.5f);
            Assert.IsFalse(note.IsVisible, "it closes itself");
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
        public IEnumerator PickUpPrompt_NamesThePart_AfterTheVerb()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            const string noun = "pickup.part_towercoil";
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.PickUp, new Vector3(0f, 0f, 3f), true, noun);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(Text("hint.pickup") + " " + Text(noun), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text);
        }

        [UnityTest]
        public IEnumerator Reticle_ShowsOnlyWhileAiming_AndBrightensOnAHoveredTarget()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "no permanent crosshair");

            _rig.Fakes.TetherState = TetherAimState.Asleep;
            Press(mouse.rightButton);
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "while the tether sleeps there is nothing to aim");
            _rig.Fakes.TetherState = TetherAimState.Idle;
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Reticle.IsVisible, "holding the tether with nothing in the cone");
            Assert.IsFalse(_rig.Ui.Reticle.IsHighlighted);

            _rig.Fakes.TetherState = TetherAimState.Hovering;
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Reticle.IsHighlighted, "a hovered relic brightens it");
            Assert.Greater(_rig.Ui.Layout.ReticleHover.resolvedStyle.scale.value.x, 1.1f, "and it gently opens");

            _rig.Fakes.TetherState = TetherAimState.Towing;
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "towing: the beam says it all");

            Release(mouse.rightButton);
            _rig.Fakes.TetherState = TetherAimState.Idle;
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible);
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
        public IEnumerator MemoryCard_TellsTheRelicsMemory_AndEscapeClosesItWithoutPausing()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 2));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Card.IsVisible);
            Assert.AreEqual("rubber_duck", _rig.Ui.Card.Current);
            Assert.AreEqual("Senior Debugging Consultant", _rig.Ui.Layout.MemoryCardName.text);
            StringAssert.StartsWith("Kenji explained", _rig.Ui.Layout.MemoryCardText.text);
            Assert.AreEqual(string.Format(Text(UiKeys.CardCaption), 2, 6), _rig.Ui.Layout.MemoryCardCaption.text);
            Assert.AreEqual("Esc", _rig.Ui.Layout.MemoryCardGlyphLabel.text);
            Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the card never blocks driving");

            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Card.IsVisible, "Esc closes the topmost thing first");
            Assert.IsFalse(_rig.Ui.Pause.IsOpen, "and does not also open the pause menu");
        }

        [UnityTest]
        public IEnumerator TowerPanel_SaysWhichPartTheTowerWaitsFor()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(3, 1, 0);
            _rig.Fakes.AwaitingPart = true;
            _rig.Fakes.AtStation = true;
            yield return Seconds(2f);
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.TowerConfirm.resolvedStyle.display,
                "the action waits until everything is in hand");
            Assert.AreEqual(Text("pickup.part_towercoil") + " | " + string.Format(Text(UiKeys.RecipeRow), 1, 0) + " | " +
                            Text(UiKeys.ComponentNeeded("Part_TowerCoil")), _rig.Ui.Tower.Recipe.PartText,
                "the missing coil is one row, with where to find it");
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds + 0.5f);
            Release(keyboard.eKey);
            yield return null;
            Assert.AreEqual(0, _rig.Fakes.Purchases, "nothing is bought without the coil");
            Assert.IsFalse(_rig.Ui.Tower.IsWorking);

            _rig.Fakes.AwaitingPart = false;
            yield return Seconds(0.3f);
            Assert.AreEqual(string.Empty, _rig.Ui.Tower.Recipe.PartText);
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds + 0.5f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "once the coil is in, the same hold wakes the tower");
        }

        [UnityTest]
        public IEnumerator AtTheTower_TheTabChipComesFirst_ThenTheCardFollows()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(6, new MaterialSlot(SalvageMaterial.Metal, 2, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 1, 4));
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(2, 1, 0);
            _rig.Fakes.AtStation = true;
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.LookUp.IsVisible, "the one Tab chip, first");
            Assert.AreEqual(Text(UiKeys.HintHold), _rig.Ui.Layout.LookHintWord.text);
            Assert.IsFalse(_rig.Ui.Tower.IsVisible, "the card and the chip never show together");

            yield return Tap(keyboard.tabKey);
            yield return Seconds(2.5f);
            yield return Tap(keyboard.tabKey);
            yield return Seconds(2f);
            Assert.IsTrue(_rig.Ui.Tower.IsVisible, "once the hold has been opened the card follows");
            Assert.IsFalse(_rig.Ui.LookUp.IsVisible, "and the chip does not come back");
        }

        [UnityTest]
        public IEnumerator TheDarkTower_NeverPutsWordsOnTheTicker_OtherSleepersDo()
        {
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            Events.Publish(new StationNotYet(UiKeys.NotYetTower, Vector3.zero, 10f));
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the tower answers with its lamp and a tone, not a line");

            Events.Publish(new StationNotYet("notyet.bay", Vector3.zero, 10f));
            yield return Seconds(2f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "another sleeper still says what wakes it");
        }

        [UnityTest]
        public IEnumerator NoFrame_ShowsTwoGuidanceElements()
        {
            Boot();
            yield return null;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(3, 1, 0);
            _rig.Fakes.AtStation = true;
            int shownFrames = 0;
            float until = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < until)
            {
                int up = (_rig.Ui.Prompt.IsVisible ? 1 : 0) + (_rig.Ui.Tower.IsVisible ? 1 : 0) +
                         (_rig.Ui.RecipeCard.IsVisible ? 1 : 0) + (_rig.Ui.PlaceCard.IsVisible ? 1 : 0) +
                         (_rig.Ui.NextLine.IsVisible ? 1 : 0);
                shownFrames += up > 0 ? 1 : 0;
                Assert.LessOrEqual(up, 1, "one guidance element at a time");
                yield return null;
            }

            Assert.Greater(shownFrames, 0, "the scene did guide, or the test proves nothing");
        }

        [UnityTest]
        public IEnumerator TowerPanel_BuysOnlyOnADeliberateHold()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(3, 1, 0);
            Press(keyboard.eKey);
            _rig.Fakes.AtStation = true;
            yield return Seconds(2f);
            Assert.IsTrue(_rig.Ui.Tower.IsVisible);
            Assert.AreEqual(0, _rig.Fakes.Purchases, "arriving with the button already held buys nothing");
            Assert.AreEqual(Text(UiKeys.UpgradeTitle("radio_tower", 1)), _rig.Ui.Layout.TowerTitle.text);
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.TowerLevel.resolvedStyle.display,
                "the title says which level comes next, so no counter");
            MaterialSlots recipe = _rig.Ui.Tower.Recipe.Slots;
            Assert.AreEqual(string.Format(Text(UiKeys.RecipeRow), 2, _rig.Fakes.Metal),
                recipe.Text(SalvageMaterial.Metal), "the need first, then what 07 has");
            Assert.AreEqual(string.Format(Text(UiKeys.RecipeRow), 1, _rig.Fakes.Wiring),
                recipe.Text(SalvageMaterial.Wiring));
            Assert.AreEqual(DisplayStyle.None, recipe.Root(SalvageMaterial.Optics).resolvedStyle.display,
                "a material the recipe does not use is left out");
            Assert.IsFalse(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(string.Empty, _rig.Ui.Tower.Recipe.PartText, "and no component is missing");

            Release(keyboard.eKey);
            yield return Seconds(0.2f);
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds * 0.5f);
            Assert.AreEqual(0, _rig.Fakes.Purchases, "not before the ring is full");
            Assert.Greater(_rig.Ui.Tower.HoldProgress, 0.2f);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds);
            Assert.AreEqual(1, _rig.Fakes.Purchases);
            Assert.IsTrue(_rig.Ui.Tower.IsWorking, "the station works; the ring rests full");
            Assert.AreEqual(1f, _rig.Ui.Tower.HoldProgress, 1e-3f);
            Assert.AreEqual(string.Empty, _rig.Ui.Layout.TowerHoldWord.text, "and nothing is said yet");
            yield return Seconds(StitchDelay);
            Assert.IsTrue(_rig.Ui.Tower.IsWorking, "no line at the purchase itself");
            Events.Publish(new StationCued(StationCue.FeedStarted, _rig.Fakes.Upgrade.Id, Vector3.zero));
            Events.Publish(new StationCued(StationCue.Fed, _rig.Fakes.Upgrade.Id, Vector3.zero));
            yield return null;
            Assert.IsTrue(_rig.Ui.Tower.IsWorking, "feeding is not yet the tower's moment");
            Events.Publish(new StationCued(StationCue.StitchStarted, _rig.Fakes.Upgrade.Id, Vector3.zero));
            yield return null;
            Assert.IsTrue(_rig.Ui.Tower.IsCelebrating, "the beam stitches the new section up: the line comes");
            Assert.AreEqual(string.Empty, _rig.Ui.Layout.TowerHoldWord.text, "the tower says nothing: the world shows it");
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "one purchase per hold");
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.AreEqual(Text(UiKeys.UpgradeTitle("radio_tower", 2)), _rig.Ui.Layout.TowerTitle.text,
                "the next level is offered");
            Assert.AreEqual(string.Empty, _rig.Ui.Layout.TowerHoldWord.text, "the hold is the ring, never a word");
            Assert.IsTrue(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass),
                "what 07 is short of reads dimmed");
            Assert.IsTrue(recipe.Root(SalvageMaterial.Optics).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.TowerConfirm.resolvedStyle.display,
                "the hold waits until the recipe is covered");

            _rig.Fakes.AtStation = false;
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Tower.IsVisible);
        }

        [UnityTest]
        public IEnumerator ACrewLogAndATape_EachGetTheirCard_OneAfterTheOther()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new CrewLogFound(UiTestRig.FirstLog, Vector3.zero));
            Events.Publish(new CassetteCollected(UiTestRig.FirstTape, Vector3.zero, 1, 3));
            yield return Seconds(1f);
            Assert.AreEqual(UiTestRig.FirstLog, _rig.Ui.Card.Current);
            Assert.AreEqual(Text(UiKeys.CrewLog(UiTestRig.FirstLog)), _rig.Ui.Layout.MemoryCardText.text);
            Assert.IsTrue(_rig.Ui.Layout.MemoryCard.ClassListContains(MemoryCard.UntitledClass));

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(_rig.Tuning.MemoryCard.Reveal.FadeOut + 0.6f);
            Assert.AreEqual(UiTestRig.FirstTape, _rig.Ui.Card.Current, "the liner card waited its turn");
            Assert.AreEqual(Text(UiKeys.CassetteTitle(UiTestRig.FirstTape)), _rig.Ui.Layout.MemoryCardName.text);
            Assert.AreEqual(string.Format(Text(UiKeys.TapeCount), 1, 3), _rig.Ui.Layout.MemoryCardCount.text);
            Assert.IsTrue(_rig.Ui.Layout.MemoryCard.ClassListContains(MemoryCard.LinerClass));
        }

        [UnityTest]
        public IEnumerator UpgradePanel_IsDressedForItsStation()
        {
            Boot();
            yield return null;
            _rig.Fakes.SetMaterials(50, 50, 50);
            _rig.Fakes.AtStation = true;
            yield return Seconds(1f);
            UiLayout layout = _rig.Ui.Layout;
            Assert.AreEqual(Text(UiKeys.StationName(UpgradeStationKind.RadioTower)), layout.TowerName.text);
            Assert.AreEqual(DisplayStyle.None, layout.TowerLevel.resolvedStyle.display, "the tower does not count its levels");
            Assert.IsTrue(layout.TowerPanel.ClassListContains(TowerPanel.StationClass(UpgradeStationKind.RadioTower)));

            _rig.Fakes.AtStation = false;
            yield return Seconds(1f);
            _rig.Fakes.Upgrade = UiTestRig.HoverJumpUpgrade();
            _rig.Fakes.AtStation = true;
            yield return Seconds(1f);
            Assert.AreEqual(Text(UiKeys.StationName(UpgradeStationKind.Workshop)), layout.TowerName.text);
            Assert.AreEqual(Text(UiKeys.UpgradeName(_rig.Fakes.Upgrade.Id)), layout.TowerLevel.text,
                "a single-level offer names the ability instead of counting to one");
            Assert.AreEqual(Text(UiKeys.UpgradeTitle(_rig.Fakes.Upgrade.Id, 1)), layout.TowerTitle.text);
            Assert.IsTrue(layout.TowerPanel.ClassListContains(TowerPanel.StationClass(UpgradeStationKind.Workshop)));
            Assert.IsFalse(layout.TowerPanel.ClassListContains(
                TowerPanel.StationClass(UpgradeStationKind.RadioTower)), "one station's look at a time");
        }

        [UnityTest]
        public IEnumerator Bay_ListsWhatIsLeft_TapAndWinchPick_AndTheHoldCraftsThePickedOne()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Boot();
            yield return null;
            UpgradeDefinition[] bay = _rig.TestBay(new Recipe(2, 1, 0), new Recipe(3, 2, 0), new Recipe(1, 2, 2));
            UpgradeDefinition hover = bay[0];
            UpgradeDefinition cradle = bay[1];
            UpgradeDefinition headlamp = bay[2];
            _rig.Fakes.Bay = bay;
            _rig.Fakes.SetMaterials(4, 3, 0);
            _rig.Fakes.AtStation = true;
            yield return Seconds(1.5f);
            UiLayout layout = _rig.Ui.Layout;
            TowerPanel panel = _rig.Ui.Tower;
            Assert.IsTrue(panel.IsVisible && panel.IsChoosing, "three pieces to craft: the bay lists them");
            Assert.AreEqual(3, panel.List.RowCount);
            Assert.AreEqual(Text(UiKeys.UpgradeName(hover.Id)), panel.List.RowName(0));
            Assert.AreEqual(Text(UiKeys.UpgradeEffect(headlamp.Id, 1)), panel.List.RowEffect(2));
            Assert.IsTrue(panel.List.RowRoot(0).ClassListContains(BayList.SelectedClass), "the first is picked");
            Assert.IsTrue(panel.List.RowRecipe(2).Root(SalvageMaterial.Optics).ClassListContains(
                MaterialSlots.ShortClass), "no optics yet: the headlamp's optics read dimmed");
            Assert.AreEqual(DisplayStyle.None, layout.TowerTitle.resolvedStyle.display, "no single offer's title");
            Assert.AreEqual(DisplayStyle.Flex, layout.TowerPick.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, layout.TowerConfirm.resolvedStyle.display, "the hover-jump is covered");

            yield return Tap(keyboard.eKey);
            yield return Seconds(0.1f);
            Assert.AreSame(cradle, panel.Choices.Current, "a tap picks the next");
            Assert.IsTrue(panel.List.RowRoot(1).ClassListContains(BayList.SelectedClass));
            Assert.AreEqual(0f, panel.HoldProgress, "a tap never stirs the ring");
            Assert.AreEqual(0, _rig.Fakes.Purchases);

            yield return Tap(gamepad.dpad.up);
            yield return Seconds(0.1f);
            Assert.AreSame(hover, panel.Choices.Current, "d-pad up: the previous one");
            Assert.IsTrue(layout.TowerPickGlyph.ClassListContains(GlyphView.PadClass), "the hint follows the device");
            _rig.Fakes.TetherState = TetherAimState.Towing;
            yield return Tap(gamepad.dpad.down);
            yield return Seconds(0.1f);
            Assert.AreSame(hover, panel.Choices.Current, "towing: the Winch reels, it does not pick");
            _rig.Fakes.TetherState = TetherAimState.Idle;
            yield return Tap(gamepad.dpad.down);
            yield return Seconds(0.1f);
            Assert.AreSame(cradle, panel.Choices.Current);

            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.TapSeconds + _rig.Tuning.TowerPanel.HoldSeconds + 0.3f);
            Assert.AreEqual(1, _rig.Fakes.Purchases);
            Assert.AreEqual(1, _rig.Fakes.LevelOf(cradle.Id), "the hold crafts the picked piece");
            Assert.AreEqual(0, _rig.Fakes.LevelOf(hover.Id), "not the first on the list");
            Assert.IsTrue(panel.IsWorking, "the bay feeds and fits; nothing is said yet");
            Assert.AreEqual(string.Empty, layout.TowerHoldWord.text);
            Events.Publish(new RoverKitFitted(RoverKitPiece.SolarCell, true, string.Empty));
            Events.Publish(new RoverKitInstalling(RoverKitPiece.CargoRack, false));
            yield return Seconds(StitchDelay);
            Assert.IsTrue(panel.IsWorking, "a gift or the install starting is not the piece set on");
            Events.Publish(new RoverKitFitted(RoverKitPiece.CargoRack, false, cradle.Id));
            yield return null;
            Assert.IsTrue(panel.IsCelebrating, "the arms set the piece on: the bay's line comes");
            Assert.AreEqual(Text(UiKeys.BayFitted), layout.TowerHoldWord.text);
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "one craft per hold");
            Assert.AreEqual(2, panel.List.RowCount, "the crafted piece leaves the list");
            Assert.AreSame(headlamp, panel.Choices.Current, "and the next takes its place under the pick");
            Assert.AreEqual(DisplayStyle.None, layout.TowerConfirm.resolvedStyle.display, "short: no hold");

            _rig.Fakes.SetMaterials(9, 9, 9);
            yield return Tap(keyboard.eKey);
            yield return Seconds(0.1f);
            Assert.AreSame(hover, panel.Choices.Current, "past the last comes the first");
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.TapSeconds + _rig.Tuning.TowerPanel.HoldSeconds + 0.3f);
            Assert.AreEqual(1, _rig.Fakes.LevelOf(hover.Id));
            Events.Publish(new RoverKitFitted(RoverKitPiece.HoverCoils, false, hover.Id));
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.IsFalse(panel.IsChoosing, "the last piece gets the single offer");
            Assert.AreEqual(Text(UiKeys.UpgradeTitle(headlamp.Id, 1)), layout.TowerTitle.text);
            Assert.AreEqual(DisplayStyle.Flex, layout.TowerTitle.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.TowerChoices.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, layout.TowerPick.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator AStationThatNeverShowsItsWork_IsReported_AndThePanelMovesOn()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            _rig.Tune("_towerPanel._cueWaitSeconds", ShortCueWait);
            yield return null;
            _rig.Fakes.Upgrade = _rig.TestTower();
            _rig.Fakes.SetMaterials(20, 20, 20);
            _rig.Fakes.AtStation = true;
            yield return Seconds(1.5f);
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds + 0.2f);
            Release(keyboard.eKey);
            Assert.IsTrue(_rig.Ui.Tower.IsWorking);
            LogAssert.Expect(LogType.Error, new Regex("never showed its work"));
            yield return Seconds(ShortCueWait + 0.5f);
            Assert.IsFalse(_rig.Ui.Tower.IsWorking, "the panel does not wait forever");
            Assert.AreEqual(string.Empty, _rig.Ui.Layout.TowerHoldWord.text, "it offers the next level");
        }

        [UnityTest]
        public IEnumerator KitTitle_NamesTheCraftedPiece_ThenTheGift_HighAndClearOf07()
        {
            Boot();
            _rig.Tune("_kitTitle._holdSeconds", 0.6f);
            yield return null;
            const string headlamp = "rover.warm_headlamp";
            KitTitleSettings settings = _rig.Tuning.KitTitle;
            Events.Publish(new RoverKitInstalling(RoverKitPiece.LampBar, false));
            yield return Seconds(settings.Delay + settings.Reveal.FadeIn);
            Assert.IsFalse(_rig.Ui.KitTitle.IsVisible, "installing, not yet settled: no name before the piece lands");

            Events.Publish(new RoverKitFitted(RoverKitPiece.LampBar, false, headlamp));
            Events.Publish(new RoverKitInstalling(RoverKitPiece.SolarCell, true));
            Events.Publish(new RoverKitFitted(RoverKitPiece.SolarCell, true, string.Empty));
            yield return Seconds(settings.Delay + settings.Reveal.FadeIn + 0.2f);
            Assert.IsTrue(_rig.Ui.KitTitle.IsVisible);
            Assert.AreEqual(Text(UiKeys.UpgradeName(headlamp)), _rig.Ui.Layout.KitTitleName.text,
                "named after the upgrade the fitted piece came from");
            Rect screen = _rig.Ui.Layout.Root.worldBound;
            Assert.Less(_rig.Ui.Layout.KitTitle.worldBound.yMax, screen.yMin + screen.height * UpperThird,
                "in the upper third, clear of 07 in the middle of the install view");

            yield return Seconds(settings.HoldSeconds + settings.Reveal.FadeOut + settings.Delay +
                                 settings.Reveal.FadeIn + 0.4f);
            Assert.IsTrue(UiKeys.TryGetGiftName(RoverKitPiece.SolarCell, out string gift));
            Assert.AreEqual(gift, _rig.Ui.KitTitle.Current, "the gift's title waited its turn");
            Assert.AreEqual(Text(gift), _rig.Ui.Layout.KitTitleName.text);

            LogAssert.Expect(LogType.Error, new Regex("CargoRack settled onto 07"));
            Events.Publish(new RoverKitFitted(RoverKitPiece.CargoRack, false, string.Empty));
        }

        [UnityTest]
        public IEnumerator RelayTag_ShowsTheRecipe_DimmedWhereShort_AndRestsOnTheRestorePrompt()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.NextCost = new Recipe(6, 4, 0);
            _rig.Fakes.SetMaterials(2, 4, 0);
            var socket = new Vector3(0f, 0f, 8f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Restore, socket, false);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.RelayTag.IsVisible, "holding the part at the mast: its recipe shows");
            MaterialSlots recipe = _rig.Ui.RelayTag.Recipe.Slots;
            Assert.AreEqual(string.Format(Text(UiKeys.RecipeRow), 6, 2), recipe.Text(SalvageMaterial.Metal),
                "the need first, then what 07 has");
            Assert.IsTrue(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass),
                "short of metal: it reads dimmed");
            Assert.IsFalse(recipe.Root(SalvageMaterial.Wiring).ClassListContains(MaterialSlots.ShortClass),
                "the wiring 07 has enough of reads normally");
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "and nothing nags");

            _rig.Fakes.SetMaterials(8, 5, 0);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Restore, socket, true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Restore)), _rig.Ui.Layout.PromptWord.text);
            Assert.IsFalse(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.LessOrEqual(_rig.Ui.Layout.RelayTag.worldBound.yMax, _rig.Ui.Layout.Prompt.worldBound.yMin + 1f,
                "the tag rests on top of the prompt, never over it");

            _rig.Fakes.RestoreHold = 0.5f;
            yield return null;
            Assert.AreEqual(0.5f, _rig.Ui.RelayTag.Hold, 1e-3f, "the ring fills as Interact is held");

            Events.Publish(new RelayRestored("relay.0", socket, 1, 4, "home", 0f));
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            _rig.Fakes.RestoreHold = 0f;
            yield return Seconds(1f);
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Restore));
            Assert.IsFalse(_rig.Ui.RelayTag.IsVisible);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
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

        [UnityTest]
        public IEnumerator Stow_PromptOverTheRelic_BowsOutOnceItRidesInTheRack()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            var relic = new Vector3(-1f, 0.4f, 7f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Stow, relic, true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "an empty cradle and a highlighted relic: stowing is taught");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Stow)), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual(relic + Vector3.up * _rig.Tuning.Prompts.Find(InteractionKind.Stow).LiftMetres,
                _rig.Ui.Director.WorldPoint, "it floats over the relic");

            Events.Publish(new RelicStowed("relic.test", new Vector3(0f, 1f, -1f)));
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            yield return Seconds(1f);
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Stow), "a relic in the rack is doing it");
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible);
        }

        [UnityTest]
        public IEnumerator Site_NamesItselfTheFirstTimeItAnswers_Once()
        {
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(0.5f);
            Events.Publish(new SiteAnswered("site.depot", new Vector3(0f, 0f, 40f), 40f, true));
            yield return Seconds(_rig.Tuning.Salvage.SiteName.FadeIn + 0.2f);
            Assert.IsTrue(_rig.Ui.SiteName.IsVisible);
            Assert.AreEqual(Text(UiKeys.SiteName("site.depot")), _rig.Ui.Layout.SiteNameText.text);
            Events.Publish(new SiteAnswered("site.kestrel", new Vector3(30f, 0f, 60f), 70f, false));
            Assert.AreEqual("site.depot", _rig.Ui.SiteName.Current, "one name at a time");

            yield return Seconds(_rig.Tuning.Salvage.SiteNameHoldSeconds + _rig.Tuning.Salvage.SiteName.FadeOut + 0.5f);
            Assert.IsFalse(_rig.Ui.SiteName.IsVisible);
            Events.Publish(new SiteAnswered("site.depot", new Vector3(0f, 0f, 40f), 40f, true));
            yield return Seconds(0.3f);
            Assert.IsFalse(_rig.Ui.SiteName.IsVisible, "a site is named once");
            Events.Publish(new SiteAnswered("site.kestrel", new Vector3(30f, 0f, 60f), 70f, false));
            yield return Seconds(0.3f);
            Assert.AreEqual("site.kestrel", _rig.Ui.SiteName.Current, "the one that waited gets its turn");
        }

        [UnityTest]
        public IEnumerator Hop_PromptThenList_ThenASoftFade_NeverOverlapping()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.SetHopChoices(UiTestRig.HomeNode, UiTestRig.SecondRelayNode);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Hop, new Vector3(0f, 0f, 6f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "parked on a lit pad: the hop is taught");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Hop)), _rig.Ui.Layout.PromptWord.text);

            Assert.IsTrue(_rig.Fakes.Open());
            Events.Publish(new TickerLine(UiTestRig.HomeLine));
            bool shared = false;
            for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                yield return null;
                shared |= _rig.Ui.HopList.IsVisible && (_rig.Ui.Prompt.IsVisible || _rig.Ui.Ticker.IsVisible);
            }

            Assert.IsFalse(shared, "the prompt fades before the list eases in, and the ticker waits");
            Assert.IsTrue(_rig.Ui.HopList.IsVisible);
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Hop), "opening the list is doing it");
            Assert.AreEqual(Text(UiTestRig.HomeNode), _rig.Ui.HopList.RowName(0));
            Assert.AreEqual(Text(UiTestRig.SecondRelayNode), _rig.Ui.HopList.RowName(1));
            _rig.Fakes.ConfirmHold = 0.5f;
            yield return null;
            Assert.AreEqual(0.5f, _rig.Ui.HopList.RowHold(0), 1e-3f);

            Assert.IsTrue(_rig.Fakes.Confirm());
            _rig.Fakes.ConfirmHold = 0f;
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            float previous = 0f;
            float largestStep = 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                _rig.Fakes.Fade = UiEase.InOutSine(t);
                yield return null;
                largestStep = Mathf.Max(largestStep, _rig.Ui.HopFade.Level - previous);
                previous = _rig.Ui.HopFade.Level;
            }

            Assert.IsTrue(_rig.Ui.HopFade.IsVisible, "the screen eases to a soft dark");
            Assert.Less(largestStep, 0.2f, "never a hard cut");
            Assert.IsFalse(_rig.Ui.HopList.IsVisible, "the list bowed out");
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "nothing speaks in the dark");

            _rig.Fakes.Phase = RadioHopPhase.Arriving;
            _rig.Fakes.Fade = 0f;
            yield return Seconds(0.5f);
            _rig.Fakes.Phase = RadioHopPhase.Closed;
            yield return Seconds(_rig.Tuning.Ticker.GapSeconds + _rig.Tuning.Ticker.Reveal.FadeIn + 0.5f);
            Assert.IsFalse(_rig.Ui.HopFade.IsVisible, "the view is clear again");
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "and the waiting line has its turn");
        }

        [UnityTest]
        public IEnumerator TunePrompt_TeachesBellsDial_MakesWayForTheReadout_AndRetiresAfterOneTurn()
        {
            InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.DialUnlocked = true;
            yield return Seconds(0.5f);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Tune, new Vector3(0f, 0f, 8f), true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible, "parked in front of Bell at home: the dial is taught");
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Tune)), _rig.Ui.Layout.PromptWord.text);
            Assert.AreEqual("E", _rig.Ui.Layout.PromptGlyphLabel.text, "Interact turns the dial");

            _rig.Fakes.Tune(RadioChannel.QuietHours, string.Empty);
            bool shared = false;
            bool answered = false;
            for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime)
            {
                yield return null;
                shared |= _rig.Ui.Prompt.IsVisible && _rig.Ui.Dial.IsVisible;
                answered |= _rig.Ui.Dial.IsVisible;
            }

            Assert.IsFalse(shared, "the prompt fades before the readout eases in: never both on screen");
            Assert.IsTrue(answered, "the turn is answered");
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Tune));
            Assert.IsFalse(_rig.Ui.Ledger.ShouldTeach(InteractionKind.Tune), "one turn and the dial is known");

            yield return Seconds(_rig.Tuning.DialReadout.HoldSeconds + _rig.Tuning.DialReadout.Reveal.FadeOut + 2f);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "still parked at the dial, but never taught again");
        }

        [UnityTest]
        public IEnumerator DialReadout_AnswersATurnOfTheDial_NeverTheLoad()
        {
            Boot();
            _rig.Fakes.DialUnlocked = true;
            _rig.Fakes.Tune(RadioChannel.TapeDeck, UiTestRig.SecondTape);
            yield return Seconds(1f);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "a program loaded with the save is not a turn of the dial");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            _rig.Fakes.AddTape(UiTestRig.FirstTape);
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "a collected tape is not a turn either");

            _rig.Fakes.Tune(RadioChannel.QuietHours, UiTestRig.SecondTape);
            yield return Seconds(0.5f);
            Assert.IsTrue(_rig.Ui.Dial.IsVisible);
            Assert.AreEqual(Text(UiKeys.RadioChannelName(RadioChannel.QuietHours)), _rig.Ui.Layout.DialStation.text);
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.DialTape.resolvedStyle.display);

            _rig.Fakes.Tune(RadioChannel.TapeDeck, UiTestRig.FirstTape);
            Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return Seconds(0.5f);
            Assert.AreEqual(Text(UiKeys.RadioChannelName(RadioChannel.TapeDeck)), _rig.Ui.Layout.DialStation.text,
                "turning again rewrites it in place");
            Assert.AreEqual(Text(UiKeys.CassetteTitle(UiTestRig.FirstTape)), _rig.Ui.Layout.DialTape.text);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the ticker waits for the readout");

            yield return Seconds(_rig.Tuning.DialReadout.HoldSeconds + _rig.Tuning.DialReadout.Reveal.FadeOut);
            Assert.IsFalse(_rig.Ui.Dial.IsVisible, "it fades after a moment");
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "then the ticker has its turn");
        }

        [UnityTest]
        public IEnumerator Ticker_SpeaksOnceAwake_MakesWayForACard_AndComesBack()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return null;
            Events.Publish(new TickerLine(UiTestRig.SignalLine));
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the radio waits for 07 to wake");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "then the line drifts in");
            Assert.AreEqual(Text(UiTestRig.SignalLine), _rig.Ui.Layout.TickerText.text);
            Assert.IsTrue(_rig.Bootstrap.Context.Input.Enabled, "the ticker never blocks driving");

            Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 2));
            Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return Seconds(0.6f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "a card is coming: the ticker makes way");
            yield return Seconds(0.6f);
            Assert.IsTrue(_rig.Ui.Card.IsVisible);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "never under a card");

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Card.IsVisible);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "the line comes back after the card");
            Assert.AreEqual(Text(UiTestRig.SignalLine), _rig.Ui.Layout.TickerText.text,
                "the interrupted line first, then the next");
            Assert.AreEqual(1, _rig.Ui.TickerLines.Waiting);
        }

        [UnityTest]
        public IEnumerator Ticker_WaitsDuringADig_AndWhilePaused()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Boot();
            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);
            Events.Publish(new ExcavationStarted(new Vector3(0f, 0f, 12f)));
            Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return Seconds(2f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "never during a dig");

            Events.Publish(new ExcavationStopped(new Vector3(0f, 0f, 12f), true));
            yield return Seconds(1f);
            Assert.IsTrue(_rig.Ui.Ticker.IsShown);

            yield return Tap(keyboard.escapeKey);
            yield return Seconds(_rig.Ui.TickerLines.HoldSeconds + 1f);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(0.6f);
            Assert.IsTrue(_rig.Ui.Ticker.IsShown, "the pause held the line where it was");
        }

        private EventBus Events => _rig.Bootstrap.Context.Events;

        /// <summary>The tables' own words for <paramref name="key"/>, so a test never guesses copy.</summary>
        private string Text(string key)
        {
            return _rig.Ui.Localization.Get(key);
        }

        private void Boot()
        {
            _rig = UiTestRig.Boot(_controls, _slot);
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
