using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// A recipe in materials (docs/features/M3-13), the way the tower panel, Kenji's Rover Bay and the relay price tag
    /// show it: the icon and number of each material it uses, the ones 07 has enough of reading normally and the short
    /// ones dimmed, with one quiet need line ("ui.recipe.need") naming the first short material
    /// (<see cref="RecipeStatus"/>). No other words. Each need line is formatted once per language and then reused, so
    /// picking between recipes (Kenji's Rover Bay) or a changing stock writes text without allocating.
    /// </summary>
    internal sealed class RecipeView
    {
        private readonly MaterialSlots _slots;
        private readonly Label _need;
        private readonly ILocalization _localization;
        private readonly Dictionary<int, string> _needLines = new Dictionary<int, string>();
        private Recipe _shownRecipe;
        private int _shownMetal;
        private int _shownWiring;
        private int _shownOptics;
        private bool _shownNeed = true;
        private bool _stale = true;

        /// <param name="items">The container the material slots are built in.</param>
        /// <param name="need">The quiet need line (left out of the layout while nothing is short).</param>
        public RecipeView(VisualElement items, Label need, ILocalization localization, IntText numbers)
        {
            _slots = new MaterialSlots(items, numbers, false);
            _need = need ?? throw new ArgumentNullException(nameof(need));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            SetNeedShown(false);
        }

        /// <summary>True when the stock last shown covers the recipe last shown.</summary>
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
            bool needed = status.TryGetNeed(out SalvageMaterial lacking, out int missing);
            if (needed)
            {
                _need.text = NeedLine(lacking, missing);
            }

            SetNeedShown(needed);
            _shownRecipe = recipe;
            _shownMetal = metal;
            _shownWiring = wiring;
            _shownOptics = optics;
            _stale = false;
        }

        /// <summary>A new language: the next <see cref="Show"/> re-reads the words.</summary>
        public void Relocalize()
        {
            _needLines.Clear();
            _stale = true;
        }

        private string NeedLine(SalvageMaterial material, int missing)
        {
            int key = missing * Materials.Count + (int)material;
            if (!_needLines.TryGetValue(key, out string line))
            {
                line = string.Format(_localization.Get(UiKeys.RecipeNeed), missing,
                    _localization.Get(UiKeys.MaterialName(material)));
                _needLines.Add(key, line);
            }

            return line;
        }

        private void SetNeedShown(bool shown)
        {
            if (shown != _shownNeed)
            {
                _need.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                _shownNeed = shown;
            }
        }

        private static bool Same(Recipe a, Recipe b)
        {
            return a.Metal == b.Metal && a.Wiring == b.Wiring && a.Optics == b.Optics;
        }
    }
}
