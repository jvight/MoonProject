using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Kenji's bench's choosing (docs/features/M3-11): the list keeps the station's order and leaves out what is
    /// bought, the pick stays put or moves to the piece that took its place, a tap steps without stirring the hold
    /// ring, the Winch steps once per notch (never while towing or holding), and the rows dim what 07 is short of.
    /// </summary>
    public sealed class BenchTests
    {
        private const float Frame = 1f / 60f;
        private const int ControlBytes = 1024;

        private TestUpgrades _shop;
        private TowerPanelSettings _settings;
        private UpgradeDefinition _cradle;
        private UpgradeDefinition _headlamp;
        private UpgradeDefinition _coils;
        private UpgradeDefinition _hover;

        [SetUp]
        public void SetUp()
        {
            _shop = new TestUpgrades();
            _settings = new TowerPanelSettings();
            _hover = _shop.Kit("rover.hover_jump", new Recipe(2, 1, 0));
            _cradle = _shop.Kit("rover.cargo_cradle", new Recipe(3, 2, 0));
            _headlamp = _shop.Kit("rover.warm_headlamp", new Recipe(1, 2, 2));
            _coils = _shop.Kit("rover.boost_coils", new Recipe(4, 3, 1));
            _shop.Catalog = new[] { _hover, _cradle, _headlamp, _coils };
        }

        [TearDown]
        public void TearDown()
        {
            _shop.Dispose();
        }

        [Test]
        public void TheChoices_AreWhatIsLeft_InTheStationsOwnOrder()
        {
            _shop.Purchase(_hover.Id);
            var choices = new BenchChoice();
            Assert.IsTrue(choices.Refresh(_shop));
            Assert.AreEqual(3, choices.Count, "bought kit is left out");
            Assert.AreSame(_cradle, choices.At(0));
            Assert.AreSame(_headlamp, choices.At(1));
            Assert.AreSame(_coils, choices.At(2));
            Assert.AreEqual(0, choices.Selected, "the first one left is picked to begin with");
            Assert.AreEqual(1, choices.NextLevelAt(1));
            Assert.AreEqual(2, choices.CostAt(1).Optics);
        }

        [Test]
        public void ThePick_StaysOnItsPiece_AndMovesToTheOneThatTookItsPlace()
        {
            var choices = new BenchChoice();
            choices.Refresh(_shop);
            Assert.IsTrue(choices.Step(1));
            Assert.AreSame(_cradle, choices.Current);

            _shop.Purchase(_hover.Id);
            Assert.IsTrue(choices.Refresh(_shop));
            Assert.AreSame(_cradle, choices.Current, "a piece leaving the list does not move the pick");
            Assert.AreEqual(0, choices.Selected);

            _shop.Purchase(_cradle.Id);
            choices.Refresh(_shop);
            Assert.AreSame(_headlamp, choices.Current, "the picked piece was crafted: the next one takes its place");

            choices.Step(-1);
            Assert.AreSame(_coils, choices.Current, "stepping back from the first wraps to the last");
            _shop.Purchase(_coils.Id);
            choices.Refresh(_shop);
            Assert.AreSame(_headlamp, choices.Current, "the last one crafted: the pick falls back to what is left");
            Assert.IsFalse(choices.Step(1), "one choice is no choice");
        }

        [Test]
        public void Stepping_WrapsBothWays()
        {
            var choices = new BenchChoice();
            choices.Refresh(_shop);
            choices.Step(1);
            choices.Step(1);
            choices.Step(1);
            Assert.AreSame(_coils, choices.Current);
            choices.Step(1);
            Assert.AreSame(_hover, choices.Current, "past the last comes the first");
            choices.Step(-1);
            Assert.AreSame(_coils, choices.Current);
        }

        [Test]
        public void Refresh_ReportsOnlyChanges_AndAllocatesNothing()
        {
            var choices = new BenchChoice();
            Assert.IsTrue(choices.Refresh(_shop));
            int version = choices.Version;
            Assert.IsFalse(choices.Refresh(_shop));
            Assert.AreEqual(version, choices.Version);
            choices.Step(1);

            Assert.That(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    choices.Refresh(_shop);
                    choices.Step(1);
                }
            }, Is.Not.AllocatingGCMemory(), "choosing allocates nothing");
            byte[] proof = null;
            Assert.That(() => { proof = new byte[ControlBytes]; },
                Is.AllocatingGCMemory(), "the allocation check sees allocations");
            Assert.IsNotNull(proof);
        }

        [Test]
        public void Clear_ForgetsThePick_SoTheNextVisitStartsAtTheFirst()
        {
            var choices = new BenchChoice();
            choices.Refresh(_shop);
            choices.Step(2);
            choices.Clear();
            Assert.AreEqual(0, choices.Count);
            Assert.IsNull(choices.Current);
            choices.Refresh(_shop);
            Assert.AreSame(_hover, choices.Current);

            _shop.Catalog = Array.Empty<UpgradeDefinition>();
            Assert.IsTrue(choices.Refresh(_shop), "07 left the pad");
            Assert.AreEqual(-1, choices.Selected);
        }

        [Test]
        public void ATap_StepsToTheNext_WithoutStirringTheRing()
        {
            var pick = new BenchPick(_settings);
            Assert.AreEqual(0, pick.Step(true, false, 0f, false, Frame));
            int frames = (int)(_settings.TapSeconds / Frame) - 2;
            for (int i = 0; i < frames; i++)
            {
                Assert.AreEqual(0, pick.Step(true, true, 0f, false, Frame));
                Assert.IsFalse(pick.HoldHeld, "the ring never sees a tap");
            }

            Assert.AreEqual(1, pick.Step(true, false, 0f, false, Frame), "let go quickly: the next choice");
            Assert.AreEqual(0, pick.Step(true, false, 0f, false, Frame));
        }

        [Test]
        public void AHold_ReachesTheRing_AfterTheTapWindow_AndIsNoTap()
        {
            var pick = new BenchPick(_settings);
            pick.Step(true, false, 0f, false, Frame);
            pick.Step(true, true, 0f, false, Frame);
            Assert.IsFalse(pick.HoldHeld);
            int frames = (int)(_settings.TapSeconds / Frame) + 2;
            for (int i = 0; i < frames; i++)
            {
                pick.Step(true, true, 0f, false, Frame);
            }

            Assert.IsTrue(pick.HoldHeld, "held past the tap window: the ring fills");
            Assert.AreEqual(0, pick.Step(true, false, 0f, false, Frame), "letting go of a hold picks nothing");
            Assert.IsFalse(pick.HoldHeld);
        }

        [Test]
        public void APressFromBeforeTheChoicesOpened_IsNeitherTapNorHold()
        {
            var pick = new BenchPick(_settings);
            pick.Step(false, true, 0f, false, Frame);
            Assert.IsTrue(pick.HoldHeld, "the ring sees it held, so it never arms");
            Assert.AreEqual(0, pick.Step(true, true, 0f, false, Frame));
            Assert.IsTrue(pick.HoldHeld);
            Assert.AreEqual(0, pick.Step(true, false, 0f, false, Frame), "arrived with it down: not a tap");
            Assert.AreEqual(0, pick.Step(false, true, 0f, false, Frame));
            Assert.AreEqual(0, pick.Step(false, false, 0f, false, Frame), "no tap while the choices are closed");
        }

        [Test]
        public void TheWinch_StepsOncePerNotch_UpForThePrevious_ButNotWhileTowingOrHolding()
        {
            var pick = new BenchPick(_settings);
            Assert.AreEqual(-1, pick.Step(true, false, 1f, false, Frame), "wheel or d-pad up: the previous one");
            Assert.AreEqual(0, pick.Step(true, false, 1f, false, Frame), "a held d-pad steps once");
            Assert.AreEqual(0, pick.Step(true, false, 0f, false, Frame));
            Assert.AreEqual(1, pick.Step(true, false, -1f, false, Frame), "down: the next one");
            pick.Step(true, false, 0f, false, Frame);
            Assert.AreEqual(0, pick.Step(true, false, _settings.WinchStep * 0.5f, false, Frame), "drift is ignored");
            pick.Step(true, false, 0f, false, Frame);
            Assert.AreEqual(0, pick.Step(true, false, 1f, true, Frame), "towing: the Winch reels instead");
            pick.Step(true, false, 0f, false, Frame);
            pick.Step(true, true, 0f, false, Frame);
            Assert.AreEqual(0, pick.Step(true, true, -1f, false, Frame), "never under a held button");
            pick.Step(true, false, 0f, false, Frame);
            Assert.AreEqual(0, pick.Step(false, false, 1f, false, Frame), "nothing while the choices are closed");
        }

        [Test]
        public void TheRows_NameEachPiece_DimWhatIsShort_AndLightThePick()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            LocalizationService localization = TestTables.Localization(new EventBus(),
                UiKeys.UpgradeName(_hover.Id), "Hover-Jump", "Nhảy lơ lửng",
                UiKeys.UpgradeEffect(_hover.Id, 1), "Hop over rocks.", "Nhảy qua đá.",
                UiKeys.UpgradeName(_cradle.Id), "Cargo Cradle", "Giỏ hàng",
                UiKeys.UpgradeEffect(_cradle.Id, 1), "A basket on 07's back.", "Một chiếc giỏ sau lưng 07.",
                UiKeys.UpgradeName(_headlamp.Id), "Warm Headlamp", "Đèn ấm",
                UiKeys.UpgradeEffect(_headlamp.Id, 1), "Light in the dark.", "Ánh sáng trong bóng tối.",
                UiKeys.UpgradeName(_coils.Id), "Boost Coils", "Cuộn tăng tốc",
                UiKeys.UpgradeEffect(_coils.Id, 1), "Swift on the flats.", "Nhanh trên đất bằng.");
            var layout = new UiLayout(uxml.Instantiate());
            var list = new BenchList(layout.TowerChoices, localization, new IntText(100));
            _shop.Purchase(_hover.Id);
            _shop.Metal = 3;
            _shop.Wiring = 2;
            _shop.Optics = 0;
            var choices = new BenchChoice();
            choices.Refresh(_shop);
            choices.Step(1);

            list.Show(choices, _shop);
            Assert.AreEqual(3, list.RowCount);
            Assert.AreEqual("Cargo Cradle", list.RowName(0));
            Assert.AreEqual("Light in the dark.", list.RowEffect(1));
            Assert.IsTrue(list.RowRoot(1).ClassListContains(BenchList.SelectedClass), "the pick is lit");
            Assert.IsFalse(list.RowRoot(0).ClassListContains(BenchList.SelectedClass));
            MaterialSlots cradle = list.RowRecipe(0);
            Assert.AreEqual("3", cradle.Text(SalvageMaterial.Metal));
            Assert.IsFalse(cradle.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass),
                "enough metal for the cradle");
            Assert.AreEqual(DisplayStyle.None, cradle.Root(SalvageMaterial.Optics).style.display.value);
            MaterialSlots headlamp = list.RowRecipe(1);
            Assert.IsTrue(headlamp.Root(SalvageMaterial.Optics).ClassListContains(MaterialSlots.ShortClass),
                "no optics yet: the headlamp's optics read dimmed");
            Assert.IsFalse(headlamp.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));

            _shop.Optics = 2;
            list.Show(choices, _shop);
            Assert.IsFalse(headlamp.Root(SalvageMaterial.Optics).ClassListContains(MaterialSlots.ShortClass),
                "the stock grew: the row brightens");

            choices.Step(1);
            list.Show(choices, _shop);
            Assert.IsTrue(list.RowRoot(2).ClassListContains(BenchList.SelectedClass));
            Assert.IsFalse(list.RowRoot(1).ClassListContains(BenchList.SelectedClass), "one lit row at a time");

            localization.SetLanguage(TestTables.Vietnamese);
            list.Relocalize();
            list.Show(choices, _shop);
            Assert.AreEqual("Giỏ hàng", list.RowName(0), "a new language re-reads the rows");
            Assert.IsTrue(list.RowRoot(2).ClassListContains(BenchList.SelectedClass), "and keeps the pick lit");
        }
    }
}
