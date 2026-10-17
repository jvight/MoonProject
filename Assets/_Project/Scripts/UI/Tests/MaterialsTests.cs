using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The materials UI's rules: the chip's counts ease and only the material that grew glows; a recipe shows only the
    /// materials it uses, dims the ones 07 is short of and names the first of them in one quiet need line.
    /// </summary>
    public sealed class MaterialsTests
    {
        private const float Frame = 1f / 60f;

        private PickupFeedSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new PickupFeedSettings();
        }

        [Test]
        public void ALoadedStock_IsTakenAsItIs_WithNothingToCount()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(5, 3, 1);
            Assert.AreEqual(5, tally.Shown(SalvageMaterial.Metal));
            Assert.AreEqual(3, tally.Shown(SalvageMaterial.Wiring));
            Assert.AreEqual(1, tally.Shown(SalvageMaterial.Optics));
            Assert.IsTrue(tally.IsCountSettled(SalvageMaterial.Metal));
            Assert.IsTrue(tally.IsCountSettled(SalvageMaterial.Wiring));
        }

        [Test]
        public void AGain_CountsUpOneUnitAtATime_AndOnlyThatMaterialCounts_ThenSettles()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(5, 3, 1);
            Assert.IsTrue(tally.Apply(5, 9, 1));
            tally.Step(Frame);
            Assert.Less(tally.Shown(SalvageMaterial.Wiring), 9, "it counts rather than jumps");
            Assert.IsFalse(tally.IsCountSettled(SalvageMaterial.Wiring));
            Assert.IsTrue(tally.IsCountSettled(SalvageMaterial.Metal), "the others stay where they were");
            Assert.IsTrue(tally.IsCountSettled(SalvageMaterial.Optics));

            for (float t = 0f; t < _settings.MaxCountSeconds + 0.5f; t += Frame)
            {
                tally.Step(Frame);
            }

            Assert.AreEqual(9, tally.Shown(SalvageMaterial.Wiring));
            Assert.IsTrue(tally.IsCountSettled(SalvageMaterial.Wiring));
        }

        [Test]
        public void ASpend_CountsDown_WithoutAPulse()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(10, 6, 2);
            Assert.IsTrue(tally.Apply(0, 0, 0));
            for (float t = 0f; t < _settings.MaxCountSeconds + 0.2f; t += Frame)
            {
                tally.Step(Frame);
                Assert.AreEqual(0f, tally.TickPulse(SalvageMaterial.Metal), "crafting spends quietly");
            }

            Assert.AreEqual(0, tally.Shown(SalvageMaterial.Metal));
        }

        [Test]
        public void TheSameAmounts_AreNoChange()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(2, 2, 2);
            Assert.IsFalse(tally.Apply(2, 2, 2));
            Assert.AreEqual(2, tally.Target(SalvageMaterial.Metal));
        }

        [Test]
        public void ARecipe_UsesOnlyItsMaterials_AndDimsWhatIsShort()
        {
            var status = new RecipeStatus(new Recipe(6, 4, 0), 2, 9, 0);
            Assert.IsTrue(status.Uses(SalvageMaterial.Metal));
            Assert.IsTrue(status.Uses(SalvageMaterial.Wiring));
            Assert.IsFalse(status.Uses(SalvageMaterial.Optics), "no optics in it: none shown");
            Assert.IsTrue(status.IsShort(SalvageMaterial.Metal));
            Assert.IsFalse(status.IsShort(SalvageMaterial.Wiring), "enough wiring reads normally");
            Assert.IsFalse(status.IsShort(SalvageMaterial.Optics), "an unused material is never short");
            Assert.IsFalse(status.IsAffordable);
        }

        [Test]
        public void TheNeedLine_NamesTheFirstShortMaterial_AndHowManyMore()
        {
            var status = new RecipeStatus(new Recipe(10, 6, 2), 4, 1, 0);
            Assert.IsTrue(status.TryGetNeed(out SalvageMaterial material, out int missing));
            Assert.AreEqual(SalvageMaterial.Metal, material, "metal first, then wiring, then optics");
            Assert.AreEqual(6, missing);

            status = new RecipeStatus(new Recipe(10, 6, 2), 10, 6, 1);
            Assert.IsTrue(status.TryGetNeed(out material, out missing));
            Assert.AreEqual(SalvageMaterial.Optics, material);
            Assert.AreEqual(1, missing);

            status = new RecipeStatus(new Recipe(10, 6, 2), 10, 6, 2);
            Assert.IsFalse(status.TryGetNeed(out _, out _), "covered: no need line");
            Assert.IsTrue(status.IsAffordable);
        }

        [Test]
        public void TheRecipeView_DimsTheShort_AndShowsAMissingComponentAsOneRow_InTheCurrentLanguage()
        {
            LocalizationService localization = TestTables.Localization(new EventBus(),
                UiKeys.RecipeRow, "{0} (have {1})", "{0} (có {1})",
                UiKeys.MaterialName(SalvageMaterial.Metal), "metal", "kim loại",
                UiKeys.MaterialName(SalvageMaterial.Wiring), "wiring", "dây điện",
                UiKeys.MaterialName(SalvageMaterial.Optics), "optics", "quang học",
                "pickup.part_towercoil", "Tower Coil", "cuộn dây tháp",
                UiKeys.ComponentNeeded("Part_TowerCoil"), "Coil crate · Supply Depot", "Thùng cuộn dây · Kho tiếp tế");
            var view = new RecipeView(new VisualElement(), localization, new IntText(100));
            var stock = new Stock { Metal = 2, Wiring = 9 };

            view.Show(new Recipe(6, 4, 0), stock);
            MaterialSlots slots = view.Slots;
            Assert.AreEqual("6 (have 2)", slots.Text(SalvageMaterial.Metal), "the need first, then what 07 has");
            Assert.AreEqual("4 (have 9)", slots.Text(SalvageMaterial.Wiring));
            Assert.AreEqual("metal", slots.Word(SalvageMaterial.Metal), "each row names its material");
            Assert.AreEqual("wiring", slots.Word(SalvageMaterial.Wiring));
            Assert.IsTrue(slots.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.IsFalse(slots.Root(SalvageMaterial.Wiring).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, slots.Root(SalvageMaterial.Optics).style.display.value);
            Assert.IsFalse(view.IsAffordable);
            Assert.AreEqual(string.Empty, view.PartText, "no component is waited for");

            view.Show(new Recipe(6, 4, 0), stock, "Part_TowerCoil");
            Assert.AreEqual("Tower Coil | 1 (have 0) | Coil crate · Supply Depot", view.PartText,
                "the missing component is one more row, with where to find it in small type");

            localization.SetLanguage(TestTables.Vietnamese);
            view.Relocalize();
            view.Show(new Recipe(6, 4, 0), stock, "Part_TowerCoil");
            Assert.AreEqual("cuộn dây tháp | 1 (có 0) | Thùng cuộn dây · Kho tiếp tế", view.PartText);

            stock.Metal = 6;
            view.Show(new Recipe(6, 4, 0), stock, "Part_TowerCoil");
            Assert.IsFalse(slots.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.IsTrue(view.IsAffordable);
            Assert.IsFalse(view.IsComplete, "the materials are in, the component is not");
            view.Show(new Recipe(6, 4, 0), stock);
            Assert.IsTrue(view.IsComplete);
            Assert.AreEqual(string.Empty, view.PartText);
        }

        /// <summary>A material stock the test sets by hand.</summary>
        private sealed class Stock : IMaterialStock
        {
            public int Metal { get; set; }

            public int Wiring { get; set; }

            public int Optics { get; set; }

            public int Total => Metal + Wiring + Optics;

            public int Of(SalvageMaterial material)
            {
                return material == SalvageMaterial.Metal ? Metal : material == SalvageMaterial.Wiring ? Wiring : Optics;
            }

            public bool Has(Recipe recipe)
            {
                return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
            }
        }
    }
}
