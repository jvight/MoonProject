using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// A recipe in materials (docs/features/M3-13), the way the tower panel, Kenji's Rover Bay and the relay price tag
    /// show it: the icon, name and "need (have N)" of each material it uses, the ones 07 has enough of reading
    /// normally with a check and the short ones dimmed (<see cref="RecipeStatus"/>). A component the build still
    /// waits for (the Tower Coil) is one more row under them, "Tower Coil  1 (have 0)", with where to find it in small
    /// type under it (docs/design/0.5.5-owner-feedback.md §4.2): the rows say what is missing, so no sentence does.
    /// Row texts are formatted once per language and then reused.
    /// </summary>
    internal sealed class RecipeView
    {
        private const string PartRowClass = "recipe__part";
        private const string PartNounPrefix = "pickup.";

        private readonly MaterialSlots _slots;
        private readonly ILocalization _localization;
        private readonly VisualElement _part;
        private readonly Label _partName;
        private readonly Label _partCount;
        private readonly Label _partSource;
        private Recipe _shownRecipe;
        private int _shownMetal;
        private int _shownWiring;
        private int _shownOptics;
        private string _shownPart;
        private bool _stale = true;

        /// <param name="items">The container the material slots (and the component's row) are built in.</param>
        public RecipeView(VisualElement items, ILocalization localization, IntText numbers)
        {
            _slots = new MaterialSlots(items, numbers, localization, true);
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _part = new VisualElement { pickingMode = PickingMode.Ignore };
            _part.AddToClassList(PartRowClass);
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList(PartRowClass + "-row");
            _partName = new Label { pickingMode = PickingMode.Ignore };
            _partName.AddToClassList(PartRowClass + "-name");
            _partCount = new Label { pickingMode = PickingMode.Ignore };
            _partCount.AddToClassList(PartRowClass + "-count");
            _partSource = new Label { pickingMode = PickingMode.Ignore };
            _partSource.AddToClassList(PartRowClass + "-source");
            row.Add(_partName);
            row.Add(_partCount);
            _part.Add(row);
            _part.Add(_partSource);
            _part.style.display = DisplayStyle.None;
            items.Add(_part);
        }

        /// <summary>True when the stock last shown covers every material of the recipe last shown.</summary>
        public bool IsAffordable { get; private set; }

        /// <summary>True when the materials are covered and no component is still missing.</summary>
        public bool IsComplete => IsAffordable && _shownPart == null;

        /// <summary>The slots (tests and captures).</summary>
        public MaterialSlots Slots => _slots;

        /// <summary>The component's row: its name and its source line (tests and captures).</summary>
        public string PartText => _part.style.display == DisplayStyle.None ? string.Empty
            : _partName.text + " | " + _partCount.text + " | " + _partSource.text;

        /// <summary>
        /// Shows <paramref name="recipe"/> against <paramref name="stock"/> (cheap when unchanged), and under it the
        /// component <paramref name="partId"/> the build still waits for (Art's model id), or none.
        /// </summary>
        public void Show(Recipe recipe, IMaterialStock stock, string partId = null)
        {
            int metal = stock.Metal;
            int wiring = stock.Wiring;
            int optics = stock.Optics;
            if (!_stale && Same(recipe, _shownRecipe) && metal == _shownMetal && wiring == _shownWiring &&
                optics == _shownOptics && ReferenceEquals(partId, _shownPart))
            {
                return;
            }

            var status = new RecipeStatus(recipe, metal, wiring, optics);
            _slots.ShowRecipe(recipe, status);
            IsAffordable = status.IsAffordable;
            if (!ReferenceEquals(partId, _shownPart) || _stale)
            {
                WritePart(partId);
            }

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

        private void WritePart(string partId)
        {
            _shownPart = partId;
            bool shown = partId != null;
            _part.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown)
            {
                return;
            }

            _partName.text = _localization.Get(PartNounPrefix + partId.ToLowerInvariant());
            _partCount.text = string.Format(_localization.Get(UiKeys.RecipeRow), 1, 0);
            _partSource.text = _localization.Get(UiKeys.ComponentNeeded(partId));
        }

        private static bool Same(Recipe a, Recipe b)
        {
            return a.Metal == b.Metal && a.Wiring == b.Wiring && a.Optics == b.Optics;
        }
    }
}
