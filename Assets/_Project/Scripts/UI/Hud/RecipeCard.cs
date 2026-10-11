using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The recipe card (docs/features/M3-24 B): while 07's view rests on the radio tower's port hopper or a sheet on
    /// Kenji's board (<see cref="IRecipeLook"/>), the next level's name and title, what it needs and what 07 has of each
    /// material (the short ones dimmed, with the quiet need line), the component the tower still waits for, and where
    /// it is built: the tower's pad, or the bay's turntable for a decoded blueprint. Never while parked at a station
    /// (its own panel shows then). Words are written only when the recipe or the language changes.
    /// </summary>
    internal sealed class RecipeCard
    {
        private const string TowerWhereKey = "ui.recipecard.tower";
        private const string BlueprintWhereKey = "ui.recipecard.blueprint";
        private const string NeedsKey = "ui.recipecard.needs";
        private const string HaveKey = "ui.recipecard.have";

        private readonly ILocalization _localization;
        private readonly IUpgradeShop _shop;
        private readonly IMaterialStock _stock;
        private readonly IRecipeLook _look;
        private readonly Reveal _reveal;
        private readonly Label _title;
        private readonly Label _level;
        private readonly Label _needsWord;
        private readonly Label _haveWord;
        private readonly Label _part;
        private readonly Label _where;
        private readonly RecipeView _needs;
        private readonly MaterialSlots _have;
        private string _shownId;
        private int _shownLevel = -1;
        private string _shownPart;
        private bool _shownSheet;

        /// <param name="hud">The HUD holding the card (recipe-card).</param>
        public RecipeCard(VisualElement hud, RevealSettings reveal, ILocalization localization, IntText numbers,
            IUpgradeShop shop, IMaterialStock stock, IRecipeLook look)
        {
            if (hud == null)
            {
                throw new ArgumentNullException(nameof(hud));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _look = look ?? throw new ArgumentNullException(nameof(look));
            VisualElement root = Require<VisualElement>(hud, "recipe-card");
            _reveal = new Reveal(root, reveal ?? throw new ArgumentNullException(nameof(reveal)));
            _reveal.Snap(false);
            new ShadowPainter(Require<VisualElement>(root, "recipe-card-shadow"));
            _title = Require<Label>(root, "recipe-card-title");
            _level = Require<Label>(root, "recipe-card-level");
            _needsWord = Require<Label>(root, "recipe-card-needs-word");
            _haveWord = Require<Label>(root, "recipe-card-have-word");
            _part = Require<Label>(root, "recipe-card-part");
            _where = Require<Label>(root, "recipe-card-where");
            _needs = new RecipeView(Require<VisualElement>(root, "recipe-card-needs"),
                Require<Label>(root, "recipe-card-short"), localization, numbers);
            _have = new MaterialSlots(Require<VisualElement>(root, "recipe-card-have"), numbers, false);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The upgrade whose card is up (or easing), or null (tests and captures).</summary>
        public string Shown => _shownId;

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="open">False while something else holds the screen.</param>
        public void Tick(float deltaTime, bool open)
        {
            string looked = open && !_shop.IsAtStation ? _look.Looked : null;
            if (looked != null && _shop.TryGetOffer(looked, out UpgradeOffer offer) && !offer.IsMaxed)
            {
                Write(looked, offer, _look.IsSheet);
                _reveal.Show();
            }
            else
            {
                _reveal.Hide();
            }

            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden && !_reveal.Target)
            {
                _shownId = null;
            }
        }

        public void Relocalize()
        {
            _needsWord.text = _localization.Get(NeedsKey);
            _haveWord.text = _localization.Get(HaveKey);
            _needs.Relocalize();
            _shownId = null;
            _shownLevel = -1;
            _shownPart = null;
        }

        private void Write(string id, UpgradeOffer offer, bool sheet)
        {
            int level = offer.CurrentLevel + 1;
            if (!ReferenceEquals(id, _shownId) || level != _shownLevel || sheet != _shownSheet)
            {
                _shownId = id;
                _shownLevel = level;
                _shownSheet = sheet;
                _shownPart = null;
                _title.text = _localization.Get(UiKeys.UpgradeName(id));
                _level.text = _localization.Get(UiKeys.UpgradeTitle(id, level));
                _where.text = _localization.Get(sheet ? BlueprintWhereKey : TowerWhereKey);
                WritePart(null);
            }

            string part = _shop.ComponentNeeded(id);
            if (!ReferenceEquals(part, _shownPart))
            {
                WritePart(part);
            }

            Recipe recipe = offer.NextCost;
            _needs.Show(recipe, _stock);
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                int need = recipe.Of(material);
                int have = _stock.Of(material);
                _have.SetShown(material, need > 0);
                _have.SetCount(material, have);
                _have.SetShort(material, have < need);
            }
        }

        private void WritePart(string part)
        {
            _shownPart = part;
            _part.style.display = part != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (part != null)
            {
                _part.text = _localization.Get(UiKeys.ComponentNeeded(part));
            }
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}
