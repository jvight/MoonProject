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
            BootstrapHarness.DeleteSaveFiles(_slot);
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
        public IEnumerator Reticle_ShowsOnlyWhileAiming_AndBrightensOnAHoveredTarget()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
            Boot();
            yield return Seconds(0.5f);
            Assert.IsFalse(_rig.Ui.Reticle.IsVisible, "no permanent crosshair");

            Press(mouse.rightButton);
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
        public IEnumerator MaterialsChip_DriftsInOnAGain_ThatMaterialGlows_CountsUp_AndLeaves()
        {
            Boot();
            _rig.Fakes.SetMaterials(5, 2, 1);
            yield return Seconds(0.3f);
            Assert.IsFalse(_rig.Ui.Chip.IsVisible, "the stock a save loads with shows nothing");
            Assert.AreEqual("5", _rig.Ui.Chip.Slots.Text(SalvageMaterial.Metal), "but is taken as it is");

            _rig.Fakes.SetMaterials(5, 6, 1);
            yield return Seconds(0.15f);
            Assert.IsTrue(_rig.Ui.Chip.IsVisible);
            Assert.Greater(_rig.Ui.Chip.Glow(SalvageMaterial.Wiring), 0f, "the wiring that grew glows");
            Assert.AreEqual(0f, _rig.Ui.Chip.Glow(SalvageMaterial.Metal), "the others stay calm");
            Assert.Less(_rig.Ui.Chip.Shown(SalvageMaterial.Wiring), 6, "it counts rather than jumps");
            yield return Seconds(_rig.Tuning.MaterialsChip.PulseSeconds + 0.3f);
            Assert.AreEqual("6", _rig.Ui.Chip.Slots.Text(SalvageMaterial.Wiring));
            Assert.AreEqual(0f, _rig.Ui.Chip.Glow(SalvageMaterial.Wiring), "the glow settles");

            yield return Seconds(_rig.Tuning.MaterialsChip.LingerSeconds +
                                 _rig.Tuning.MaterialsChip.Reveal.FadeOut + 0.3f);
            Assert.IsFalse(_rig.Ui.Chip.IsVisible, "then it leaves the screen to the moon");
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
            Assert.AreEqual("Memory 2 of 6", _rig.Ui.Layout.MemoryCardCaption.text);
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
            Assert.AreEqual("Wake the old mast", _rig.Ui.Layout.TowerTitle.text);
            Assert.AreEqual("Level 1 of 3", _rig.Ui.Layout.TowerLevel.text);
            MaterialSlots recipe = _rig.Ui.Tower.Recipe.Slots;
            Assert.AreEqual("2", recipe.Text(SalvageMaterial.Metal));
            Assert.AreEqual("1", recipe.Text(SalvageMaterial.Wiring));
            Assert.AreEqual(DisplayStyle.None, recipe.Root(SalvageMaterial.Optics).resolvedStyle.display,
                "a material the recipe does not use is left out");
            Assert.IsFalse(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.TowerNeed.resolvedStyle.display, "nothing is short");
            Assert.AreEqual("3", _rig.Ui.Chip.Slots.Text(SalvageMaterial.Metal), "the pinned chip shows the stock");
            Assert.IsTrue(_rig.Ui.Chip.IsVisible, "the stock stays in view at the pad");

            Release(keyboard.eKey);
            yield return Seconds(0.2f);
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds * 0.5f);
            Assert.AreEqual(0, _rig.Fakes.Purchases, "not before the ring is full");
            Assert.Greater(_rig.Ui.Tower.HoldProgress, 0.2f);
            yield return Seconds(_rig.Tuning.TowerPanel.HoldSeconds);
            Assert.AreEqual(1, _rig.Fakes.Purchases);
            Assert.IsTrue(_rig.Ui.Tower.IsCelebrating);
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "one purchase per hold");
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.AreEqual("Raise the mast", _rig.Ui.Layout.TowerTitle.text, "the next level is offered");
            Assert.IsTrue(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass),
                "what 07 is short of reads dimmed");
            Assert.IsTrue(recipe.Root(SalvageMaterial.Optics).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.Flex, _rig.Ui.Layout.TowerNeed.resolvedStyle.display,
                "and one quiet line says what to salvage");
            Assert.AreEqual(string.Format(Text(UiKeys.RecipeNeed), 3, Text(UiKeys.MaterialName(SalvageMaterial.Metal))),
                _rig.Ui.Layout.TowerNeed.text);
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
            Assert.AreEqual("Level 1 of 3", layout.TowerLevel.text, "the tower counts its levels");
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
            Assert.AreEqual(Text(UiKeys.BayPick), layout.TowerPickWord.text);
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
            Assert.IsTrue(panel.IsCelebrating);
            yield return Seconds(_rig.Tuning.TowerPanel.CelebrateSeconds + 0.5f);
            Release(keyboard.eKey);
            yield return Seconds(0.3f);
            Assert.AreEqual(1, _rig.Fakes.Purchases, "one craft per hold");
            Assert.AreEqual(2, panel.List.RowCount, "the crafted piece leaves the list");
            Assert.AreSame(headlamp, panel.Choices.Current, "and the next takes its place under the pick");
            Assert.AreEqual(DisplayStyle.None, layout.TowerConfirm.resolvedStyle.display, "short: no hold");
            Assert.AreEqual(DisplayStyle.Flex, layout.TowerNeed.resolvedStyle.display, "but one quiet need line");

            _rig.Fakes.SetMaterials(9, 9, 9);
            yield return Tap(keyboard.eKey);
            yield return Seconds(0.1f);
            Assert.AreSame(hover, panel.Choices.Current, "past the last comes the first");
            Press(keyboard.eKey);
            yield return Seconds(_rig.Tuning.TowerPanel.TapSeconds + _rig.Tuning.TowerPanel.HoldSeconds + 0.3f);
            Assert.AreEqual(1, _rig.Fakes.LevelOf(hover.Id));
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
        public IEnumerator KitTitle_NamesTheCraftedPiece_ThenTheGift_HighAndClearOf07()
        {
            Boot();
            _rig.Tune("_kitTitle._holdSeconds", 0.6f);
            yield return null;
            UpgradeDefinition[] bay = _rig.TestBay(new Recipe(2, 1, 0), new Recipe(3, 2, 0), new Recipe(1, 2, 2));
            _rig.Fakes.Bay = bay;
            KitTitleSettings settings = _rig.Tuning.KitTitle;
            Events.Publish(new UpgradePurchased(bay[2].Id, 1));
            Events.Publish(new RoverKitInstalling(RoverKitPiece.LampBar, false));
            yield return Seconds(settings.Delay + settings.Reveal.FadeIn);
            Assert.IsFalse(_rig.Ui.KitTitle.IsVisible, "bought but not yet settled: no name before the piece lands");

            Events.Publish(new RoverKitFitted(RoverKitPiece.LampBar, false, "rover.warm_headlamp"));
            Events.Publish(new RoverKitInstalling(RoverKitPiece.SolarCell, true));
            Events.Publish(new RoverKitFitted(RoverKitPiece.SolarCell, true, string.Empty));
            yield return Seconds(settings.Delay + settings.Reveal.FadeIn + 0.2f);
            Assert.IsTrue(_rig.Ui.KitTitle.IsVisible);
            Assert.AreEqual(Text(UiKeys.UpgradeName(bay[2].Id)), _rig.Ui.Layout.KitTitleName.text);
            Rect screen = _rig.Ui.Layout.Root.worldBound;
            Assert.Less(_rig.Ui.Layout.KitTitle.worldBound.yMax, screen.yMin + screen.height * 0.3f,
                "high on screen, clear of 07 in the middle of the install view");

            yield return Seconds(settings.HoldSeconds + settings.Reveal.FadeOut + settings.Delay +
                                 settings.Reveal.FadeIn + 0.4f);
            Assert.IsTrue(UiKeys.TryGetGiftName(RoverKitPiece.SolarCell, out string gift));
            Assert.AreEqual(gift, _rig.Ui.KitTitle.Current, "the gift's title waited its turn");
            Assert.AreEqual(Text(gift), _rig.Ui.Layout.KitTitleName.text);

            LogAssert.Expect(LogType.Error, new Regex("CargoRack settled onto 07"));
            Events.Publish(new RoverKitFitted(RoverKitPiece.CargoRack, false, "rover.cargo_cradle"));
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
            Assert.AreEqual("6", recipe.Text(SalvageMaterial.Metal));
            Assert.IsTrue(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass),
                "short of metal: it reads dimmed");
            Assert.IsFalse(recipe.Root(SalvageMaterial.Wiring).ClassListContains(MaterialSlots.ShortClass),
                "the wiring 07 has enough of reads normally");
            Assert.AreEqual(string.Format(Text(UiKeys.RecipeNeed), 4, Text(UiKeys.MaterialName(SalvageMaterial.Metal))),
                _rig.Ui.Layout.RelayTagNeed.text);
            Assert.IsFalse(_rig.Ui.Prompt.IsVisible, "and nothing nags");

            _rig.Fakes.SetMaterials(8, 5, 0);
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Restore, socket, true);
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);
            Assert.AreEqual(Text(UiKeys.Hint(InteractionKind.Restore)), _rig.Ui.Layout.PromptWord.text);
            Assert.IsFalse(recipe.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, _rig.Ui.Layout.RelayTagNeed.resolvedStyle.display);
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
            Assert.AreEqual(1, _rig.Ui.Ledger.Used(InteractionKind.Salvage), "cutting is doing it");
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
            Assert.AreEqual(Text(UiKeys.SiteName("site.depot")), _rig.Ui.Layout.SiteName.text);
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
            Events.Publish(new TickerLine(UiTestRig.SignalLine, "140"));
            yield return Seconds(1.5f);
            Assert.IsFalse(_rig.Ui.Ticker.IsVisible, "the radio waits for 07 to wake");

            Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1.5f);
            Assert.IsTrue(_rig.Ui.Ticker.IsVisible, "then the line drifts in");
            Assert.AreEqual(string.Format(Text(UiTestRig.SignalLine), "140"), _rig.Ui.Layout.TickerText.text);
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
            Assert.AreEqual(string.Format(Text(UiTestRig.SignalLine), "140"), _rig.Ui.Layout.TickerText.text,
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
