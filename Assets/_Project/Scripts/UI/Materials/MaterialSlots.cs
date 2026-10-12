using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// One row of the three materials, built into a container: per material a painted icon and a number, in Metal,
    /// Wiring, Optics order. The pause summary and every recipe use it. A material can be left out of the layout and
    /// shown dimmed (short). Writes styles and text only when they change. A recipe's slots can also read "need (have N)" (docs/design/writing-style.md rule 7: the need first,
    /// the stock after; a covered material gets a painted check, a short one reads dim), and carry the material's name
    /// (docs/design/first-hour-v2 6.5).
    /// </summary>
    internal sealed class MaterialSlots
    {
        public const string SlotClass = "material-slot";
        public const string ShortClass = "material-slot--short";
        public const string IconClass = "material-icon";
        public const string CountClass = "material-slot__count";
        public const string WordClass = "material-slot__word";
        public const string CheckClass = "material-slot__check";

        private const int HaveCap = 999;

        private readonly IntText _numbers;
        private readonly ILocalization _words;
        private readonly bool _names;
        private readonly bool _haveNeed;
        private readonly Dictionary<int, string> _haveNeedTexts = new Dictionary<int, string>();
        private readonly Slot[] _slots = new Slot[Materials.Count];

        /// <param name="container">Where the three slots go (emptied first).</param>
        /// <param name="numbers">Cached number strings.</param>
        /// <param name="words">The words (material names, the row's "need (have N)" format); required for a recipe.</param>
        /// <param name="haveNeed">True: a recipe's count reads "need (have N)" and a covered one gets a check.</param>
        /// <param name="names">True (with <paramref name="words"/>): each slot also names its material.</param>
        public MaterialSlots(VisualElement container, IntText numbers, ILocalization words = null,
            bool haveNeed = false, bool names = true)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            if (haveNeed && words == null)
            {
                throw new ArgumentNullException(nameof(words), "A recipe row needs the words for its format.");
            }

            _words = words;
            _names = words != null && names;
            _haveNeed = haveNeed;
            container.Clear();
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                string suffix = Materials.ClassSuffix(material);
                var root = new VisualElement { pickingMode = PickingMode.Ignore, name = SlotClass + "-" + suffix };
                root.AddToClassList(SlotClass);
                root.AddToClassList(SlotClass + "--" + suffix);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(IconClass);
                icon.AddToClassList(IconClass + "--" + suffix);
                var count = new Label { pickingMode = PickingMode.Ignore };
                count.AddToClassList(CountClass);
                root.Add(icon);
                Label word = null;
                if (_names)
                {
                    word = new Label { pickingMode = PickingMode.Ignore };
                    word.AddToClassList(WordClass);
                    root.Add(word);
                }

                root.Add(count);
                if (haveNeed)
                {
                    var check = new VisualElement { pickingMode = PickingMode.Ignore };
                    check.AddToClassList(CheckClass);
                    new CheckPainter(check);
                    root.Add(check);
                }

                container.Add(root);
                new MaterialIconPainter(icon, material);
                _slots[i] = new Slot(root, count, word);
                SetCount(material, 0);
            }

            Relocalize();
        }

        /// <summary>Writes the materials' names again (a new language).</summary>
        public void Relocalize()
        {
            _haveNeedTexts.Clear();
            for (int i = 0; i < Materials.Count; i++)
            {
                _slots[i].WrittenPair = -1;
                if (_names)
                {
                    _slots[i].Word.text = _words.Get(UiKeys.MaterialName(Materials.At(i)));
                }
            }
        }

        /// <summary>The number on <paramref name="material"/>'s slot (tests and captures).</summary>
        public string Text(SalvageMaterial material)
        {
            return _slots[(int)material].Count.text;
        }

        /// <summary>The name written on <paramref name="material"/>'s slot (tests and captures).</summary>
        public string Word(SalvageMaterial material)
        {
            return _slots[(int)material].Word.text;
        }

        /// <summary>The slot of <paramref name="material"/> (tests and captures).</summary>
        public VisualElement Root(SalvageMaterial material)
        {
            return _slots[(int)material].Root;
        }

        public void SetCount(SalvageMaterial material, int value)
        {
            Slot slot = _slots[(int)material];
            if (value != slot.Written)
            {
                slot.Count.text = _numbers.Get(value);
                slot.Written = value;
            }
        }

        /// <summary>Reads "need (have N)" on <paramref name="material"/>'s slot; each pair is formatted once.</summary>
        private void SetHaveNeed(SalvageMaterial material, int have, int need)
        {
            Slot slot = _slots[(int)material];
            int key = Mathf.Clamp(have, 0, HaveCap) * (HaveCap + 1) + Mathf.Clamp(need, 0, HaveCap);
            if (key == slot.WrittenPair)
            {
                return;
            }

            if (!_haveNeedTexts.TryGetValue(key, out string text))
            {
                text = string.Format(_words.Get(UiKeys.RecipeRow), _numbers.Get(need), _numbers.Get(have));
                _haveNeedTexts.Add(key, text);
            }

            slot.Count.text = text;
            slot.WrittenPair = key;
        }

        /// <summary>Takes <paramref name="material"/>'s slot out of the layout (a recipe not using it).</summary>
        public void SetShown(SalvageMaterial material, bool shown)
        {
            Slot slot = _slots[(int)material];
            if (shown != slot.Shown)
            {
                slot.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                slot.Shown = shown;
            }
        }

        /// <summary>Dims <paramref name="material"/>'s slot (07 does not have enough of it yet).</summary>
        public void SetShort(SalvageMaterial material, bool isShort)
        {
            Slot slot = _slots[(int)material];
            if (isShort != slot.Short)
            {
                slot.Root.EnableInClassList(ShortClass, isShort);
                slot.Short = isShort;
            }
        }

        /// <summary>
        /// Shows <paramref name="recipe"/>: only the materials it uses, each with its amount, the ones
        /// <paramref name="status"/> is short of dimmed.
        /// </summary>
        public void ShowRecipe(Recipe recipe, RecipeStatus status)
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                SetShown(material, status.Uses(material));
                if (_haveNeed)
                {
                    SetHaveNeed(material, status.Have(material), recipe.Of(material));
                }
                else
                {
                    SetCount(material, recipe.Of(material));
                }

                SetShort(material, status.IsShort(material));
            }
        }

        private sealed class Slot
        {
            public Slot(VisualElement root, Label count, Label word)
            {
                Root = root;
                Count = count;
                Word = word;
            }

            public VisualElement Root { get; }

            public Label Count { get; }

            public Label Word { get; }

            public int WrittenPair { get; set; } = -1;

            public int Written { get; set; } = -1;

            public bool Shown { get; set; } = true;

            public bool Short { get; set; }
        }
    }
}
