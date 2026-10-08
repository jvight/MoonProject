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

        private MaterialsChipSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new MaterialsChipSettings();
        }

        [Test]
        public void ALoadedStock_IsTakenAsItIs_WithoutAGlow()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(5, 3, 1);
            Assert.AreEqual(5, tally.Shown(SalvageMaterial.Metal));
            Assert.AreEqual(3, tally.Shown(SalvageMaterial.Wiring));
            Assert.AreEqual(1, tally.Shown(SalvageMaterial.Optics));
            Assert.IsTrue(tally.IsSettled);
            Assert.IsFalse(tally.IsGlowing);
        }

        [Test]
        public void AGain_CountsUpEased_AndOnlyThatMaterialGlows_ThenSettles()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(5, 3, 1);
            Assert.IsTrue(tally.Apply(5, 9, 1));
            tally.Step(Frame);
            Assert.Less(tally.Shown(SalvageMaterial.Wiring), 9, "it counts rather than jumps");
            Assert.Greater(tally.Glow(SalvageMaterial.Wiring), 0f, "the wiring that grew glows");
            Assert.AreEqual(0f, tally.Glow(SalvageMaterial.Metal), "the others stay calm");
            Assert.AreEqual(0f, tally.Glow(SalvageMaterial.Optics));

            float peak = 0f;
            for (float t = 0f; t < _settings.PulseSeconds + 0.5f; t += Frame)
            {
                tally.Step(Frame);
                peak = Mathf.Max(peak, tally.Glow(SalvageMaterial.Wiring));
            }

            Assert.Greater(peak, 0.95f, "a soft swell to full");
            Assert.AreEqual(9, tally.Shown(SalvageMaterial.Wiring));
            Assert.AreEqual(0f, tally.Glow(SalvageMaterial.Wiring), "and a settle back to calm");
            Assert.IsTrue(tally.IsSettled);
            Assert.IsFalse(tally.IsGlowing);
        }

        [Test]
        public void ASpend_CountsDown_WithoutAGlow()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(10, 6, 2);
            Assert.IsTrue(tally.Apply(0, 0, 0));
            for (float t = 0f; t < _settings.MaxCountSeconds + 0.2f; t += Frame)
            {
                tally.Step(Frame);
                Assert.IsFalse(tally.IsGlowing, "crafting spends quietly");
            }

            Assert.AreEqual(0, tally.Shown(SalvageMaterial.Metal));
        }

        [Test]
        public void TheSameAmounts_AreNoChange()
        {
            var tally = new MaterialTally(_settings);
            tally.Snap(2, 2, 2);
            Assert.IsFalse(tally.Apply(2, 2, 2));
            Assert.IsFalse(tally.IsGlowing);
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
        public void TheRecipeView_DimsTheShort_AndWritesOneQuietNeedLine_InTheCurrentLanguage()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            LocalizationService localization = TestTables.Localization(new EventBus(),
                UiKeys.RecipeNeed, "{0} more {1} to salvage", "Cần trục vớt thêm {0} {1}",
                UiKeys.MaterialName(SalvageMaterial.Metal), "metal", "kim loại",
                UiKeys.MaterialName(SalvageMaterial.Wiring), "wiring", "dây điện",
                UiKeys.MaterialName(SalvageMaterial.Optics), "optics", "quang học");
            var layout = new UiLayout(uxml.Instantiate());
            var view = new RecipeView(layout.TowerRecipe, layout.TowerNeed, localization, new IntText(100));
            var stock = new Stock { Metal = 2, Wiring = 9 };

            view.Show(new Recipe(6, 4, 0), stock);
            MaterialSlots slots = view.Slots;
            Assert.AreEqual("6", slots.Text(SalvageMaterial.Metal));
            Assert.AreEqual("4", slots.Text(SalvageMaterial.Wiring));
            Assert.IsTrue(slots.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.IsFalse(slots.Root(SalvageMaterial.Wiring).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, slots.Root(SalvageMaterial.Optics).style.display.value);
            Assert.AreEqual("4 more metal to salvage", layout.TowerNeed.text);
            Assert.AreEqual(DisplayStyle.Flex, layout.TowerNeed.style.display.value);
            Assert.IsFalse(view.IsAffordable);

            localization.SetLanguage(TestTables.Vietnamese);
            view.Relocalize();
            view.Show(new Recipe(6, 4, 0), stock);
            Assert.AreEqual("Cần trục vớt thêm 4 kim loại", layout.TowerNeed.text);

            stock.Metal = 6;
            view.Show(new Recipe(6, 4, 0), stock);
            Assert.IsFalse(slots.Root(SalvageMaterial.Metal).ClassListContains(MaterialSlots.ShortClass));
            Assert.AreEqual(DisplayStyle.None, layout.TowerNeed.style.display.value, "covered: the line goes");
            Assert.IsTrue(view.IsAffordable);
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
                return Materials.Of(this, material);
            }

            public bool Has(Recipe recipe)
            {
                return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
            }
        }
    }
}
