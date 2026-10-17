using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// A recipe in materials (docs/features/M3-13), the way the build menu shows it: the icon, name and "need (have N)"
    /// of each material it uses, the ones 07 has enough of reading normally with a check and the short ones dimmed
    /// (<see cref="RecipeStatus"/>). Row texts are formatted once per language and then reused.
    /// </summary>
    internal sealed class RecipeView
    {
        private readonly MaterialSlots _slots;
        private Recipe _shownRecipe;
        private int _shownMetal;
        private int _shownWiring;
        private int _shownOptics;
        private bool _stale = true;

        /// <param name="items">The container the material slots are built in.</param>
        public RecipeView(
            VisualElement items,
            ILocalization localization,
            IntText numbers,
            bool haveNeed = true,
            bool names = true)
        {
            _slots = new MaterialSlots(items, numbers, localization, haveNeed, names);
        }

        /// <summary>True when the stock last shown covers every material of the recipe last shown.</summary>
        public bool IsAffordable { get; private set; }

        /// <summary>The slots (tests and captures).</summary>
        public MaterialSlots Slots => _slots;

        /// <summary>Shows <paramref name="recipe"/> against <paramref name="stock"/> (cheap when unchanged).</summary>
        public void Show(Recipe recipe, IMaterialStock stock)
        {
            int metal = stock.Metal;
            int wiring = stock.Wiring;
            int optics = stock.Optics;
            if (!_stale && Same(recipe, _shownRecipe) && metal == _shownMetal && wiring == _shownWiring &&
                optics == _shownOptics)
            {
                return;
            }

            var status = new RecipeStatus(recipe, metal, wiring, optics);
            _slots.ShowRecipe(recipe, status);
            IsAffordable = status.IsAffordable;
            _shownRecipe = recipe;
            _shownMetal = metal;
            _shownWiring = wiring;
            _shownOptics = optics;
            _stale = false;
        }

        /// <summary>A new language: the next <see cref="Show"/> re-reads the words.</summary>
        public void Relocalize()
        {
            _slots.Relocalize();
            _stale = true;
        }

        private static bool Same(Recipe a, Recipe b)
        {
            return a.Metal == b.Metal && a.Wiring == b.Wiring && a.Optics == b.Optics;
        }
    }
}
