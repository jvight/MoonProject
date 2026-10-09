using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Kenji's Rover Bay's choices on the station panel (docs/features/M3-11): one row per piece of kit still to craft,
    /// with its name ("upgrade.&lt;id&gt;.name"), its recipe in material icons (the ones 07 is short of dimmed) and
    /// what it does in one line ("upgrade.&lt;id&gt;.&lt;level&gt;.effect", cut short with an ellipsis). The picked
    /// row is lit and shows its whole line. Rows are made when the list first needs them and then reused; text is
    /// written only when the list, the pick, the stock or the language changes.
    /// </summary>
    internal sealed class BayList
    {
        public const string RowClass = "bay-row";
        public const string SelectedClass = "bay-row--selected";
        public const string LineClass = "bay-row__line";
        public const string LampClass = "bay-row__lamp";
        public const string NameClass = "bay-row__name";
        public const string RecipeClass = "bay-row__recipe";
        public const string EffectClass = "bay-row__effect";

        private readonly VisualElement _container;
        private readonly ILocalization _localization;
        private readonly IntText _numbers;
        private readonly List<Row> _pool = new List<Row>();
        private int _count;
        private int _version = -1;
        private int _selected = -1;
        private int _metal = -1;
        private int _wiring = -1;
        private int _optics = -1;
        private bool _stale = true;

        public BayList(VisualElement container, ILocalization localization, IntText numbers)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        }

        /// <summary>Rows showing a choice.</summary>
        public int RowCount => _count;

        /// <summary>The name on row <paramref name="row"/> (tests and captures).</summary>
        public string RowName(int row)
        {
            return _pool[row].Name.text;
        }

        /// <summary>The effect line on row <paramref name="row"/> (tests and captures).</summary>
        public string RowEffect(int row)
        {
            return _pool[row].Effect.text;
        }

        /// <summary>The recipe on row <paramref name="row"/> (tests and captures).</summary>
        public MaterialSlots RowRecipe(int row)
        {
            return _pool[row].Slots;
        }

        /// <summary>The element of row <paramref name="row"/> (tests and captures).</summary>
        public VisualElement RowRoot(int row)
        {
            return _pool[row].Root;
        }

        /// <summary>Shows <paramref name="choices"/> against <paramref name="stock"/> (cheap when unchanged).</summary>
        public void Show(BayChoice choices, IMaterialStock stock)
        {
            if (_stale || choices.Version != _version)
            {
                Fill(choices);
            }

            if (choices.Selected != _selected)
            {
                Select(choices.Selected);
            }

            int metal = stock.Metal;
            int wiring = stock.Wiring;
            int optics = stock.Optics;
            if (metal == _metal && wiring == _wiring && optics == _optics)
            {
                return;
            }

            for (int i = 0; i < _count; i++)
            {
                Row row = _pool[i];
                row.Slots.ShowRecipe(row.Cost, new RecipeStatus(row.Cost, metal, wiring, optics));
            }

            _metal = metal;
            _wiring = wiring;
            _optics = optics;
        }

        /// <summary>A new language: the next <see cref="Show"/> re-reads the words.</summary>
        public void Relocalize()
        {
            _stale = true;
        }

        private void Fill(BayChoice choices)
        {
            int count = choices.Count;
            while (_pool.Count < count)
            {
                _pool.Add(NewRow());
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                Row row = _pool[i];
                bool used = i < count;
                row.Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                row.Root.RemoveFromClassList(SelectedClass);
                if (!used)
                {
                    continue;
                }

                UpgradeDefinition upgrade = choices.At(i);
                row.Name.text = _localization.Get(UiKeys.UpgradeName(upgrade.Id));
                row.Effect.text = _localization.Get(UiKeys.UpgradeEffect(upgrade.Id, choices.NextLevelAt(i)));
                row.Cost = choices.CostAt(i);
            }

            _count = count;
            _version = choices.Version;
            _selected = -1;
            _metal = -1;
            _stale = false;
        }

        private void Select(int row)
        {
            if (_selected >= 0 && _selected < _count)
            {
                _pool[_selected].Root.RemoveFromClassList(SelectedClass);
            }

            if (row >= 0 && row < _count)
            {
                _pool[row].Root.AddToClassList(SelectedClass);
            }

            _selected = row;
        }

        private Row NewRow()
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList(LineClass);
            var lamp = new VisualElement { pickingMode = PickingMode.Ignore };
            lamp.AddToClassList(LampClass);
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList(NameClass);
            var recipe = new VisualElement { pickingMode = PickingMode.Ignore };
            recipe.AddToClassList("recipe");
            recipe.AddToClassList(RecipeClass);
            var effect = new Label { pickingMode = PickingMode.Ignore };
            effect.AddToClassList(EffectClass);
            line.Add(lamp);
            line.Add(name);
            line.Add(recipe);
            root.Add(line);
            root.Add(effect);
            _container.Add(root);
            return new Row(root, name, effect, new MaterialSlots(recipe, _numbers, false));
        }

        private sealed class Row
        {
            public Row(VisualElement root, Label name, Label effect, MaterialSlots slots)
            {
                Root = root;
                Name = name;
                Effect = effect;
                Slots = slots;
            }

            public VisualElement Root { get; }

            public Label Name { get; }

            public Label Effect { get; }

            public MaterialSlots Slots { get; }

            public Recipe Cost { get; set; }
        }
    }
}
