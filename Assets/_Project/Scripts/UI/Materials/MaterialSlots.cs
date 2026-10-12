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
    /// Wiring, Optics order. The materials chip, the pause summary and every recipe use it. A material can be left out
    /// of the layout, shown dimmed (short), and given a glow behind its icon. Writes styles and text only when they
    /// change. A recipe's slots can also read "have / need" (a short material gets a hollow pip), and carry the
    /// material's name (docs/design/first-hour-v2 6.5).
    /// </summary>
    internal sealed class MaterialSlots
    {
        public const string SlotClass = "material-slot";
        public const string ShortClass = "material-slot--short";
        public const string IconClass = "material-icon";
        public const string GlowClass = "material-slot__glow";
        public const string CountClass = "material-slot__count";
        public const string WordClass = "material-slot__word";
        public const string PipClass = "material-slot__pip";
        private const string HaveNeedSeparator = " / ";

        private const float WriteEpsilon = 1e-3f;
        private const int HaveCap = 999;

        private readonly IntText _numbers;
        private readonly ILocalization _words;
        private readonly bool _haveNeed;
        private readonly Dictionary<int, string> _haveNeedTexts = new Dictionary<int, string>();
        private readonly Slot[] _slots = new Slot[Materials.Count];

        /// <param name="container">Where the three slots go (emptied first).</param>
        /// <param name="numbers">Cached number strings.</param>
        /// <param name="withGlow">True: each icon gets a soft halo behind it for <see cref="SetGlow"/>.</param>
        /// <param name="words">When given, each slot also names its material.</param>
        /// <param name="haveNeed">True: a recipe's count reads "have / need" and a short one gets a hollow pip.</param>
        public MaterialSlots(VisualElement container, IntText numbers, bool withGlow, ILocalization words = null,
            bool haveNeed = false)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _words = words;
            _haveNeed = haveNeed;
            container.Clear();
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                string suffix = Materials.ClassSuffix(material);
                var root = new VisualElement { pickingMode = PickingMode.Ignore, name = SlotClass + "-" + suffix };
                root.AddToClassList(SlotClass);
                root.AddToClassList(SlotClass + "--" + suffix);
                VisualElement glow = null;
                if (withGlow)
                {
                    glow = new VisualElement { pickingMode = PickingMode.Ignore };
                    glow.AddToClassList(GlowClass);
                    glow.style.opacity = 0f;
                    new ShadowPainter(glow);
                    root.Add(glow);
                }

                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(IconClass);
                icon.AddToClassList(IconClass + "--" + suffix);
                var count = new Label { pickingMode = PickingMode.Ignore };
                count.AddToClassList(CountClass);
                root.Add(icon);
                Label word = null;
                if (words != null)
                {
                    word = new Label { pickingMode = PickingMode.Ignore };
                    word.AddToClassList(WordClass);
                    root.Add(word);
                }

                root.Add(count);
                if (haveNeed)
                {
                    var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                    pip.AddToClassList(PipClass);
                    root.Add(pip);
                }

                container.Add(root);
                new MaterialIconPainter(icon, material);
                _slots[i] = new Slot(root, glow, icon, count, word);
                SetCount(material, 0);
            }

            Relocalize();
        }

        /// <summary>Writes the materials' names again (a new language).</summary>
        public void Relocalize()
        {
            if (_words == null)
            {
                return;
            }

            for (int i = 0; i < Materials.Count; i++)
            {
                _slots[i].Word.text = _words.Get(UiKeys.MaterialName(Materials.At(i)));
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

        /// <summary>Reads "have / need" on <paramref name="material"/>'s slot; each pair is formatted once.</summary>
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
                text = string.Concat(_numbers.Get(have), HaveNeedSeparator, _numbers.Get(need));
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

        /// <summary>
        /// Swells <paramref name="material"/>'s icon and its halo by <paramref name="glow"/> (0..1), and the icon a
        /// little more by <paramref name="tick"/> (0..1, the pulse of the unit that just rolled).
        /// </summary>
        public void SetGlow(SalvageMaterial material, float glow, float tick, float scale, float tickScale,
            float opacity)
        {
            Slot slot = _slots[(int)material];
            if (slot.Glow == null ||
                (Mathf.Abs(glow - slot.WrittenGlow) < WriteEpsilon && Mathf.Abs(tick - slot.WrittenTick) < WriteEpsilon))
            {
                return;
            }

            float grown = 1f + glow * scale + tick * tickScale;
            slot.Icon.style.scale = new StyleScale(new Scale(new Vector2(grown, grown)));
            slot.Glow.style.opacity = glow * opacity;
            slot.WrittenGlow = glow;
            slot.WrittenTick = tick;
        }

        private sealed class Slot
        {
            public Slot(VisualElement root, VisualElement glow, VisualElement icon, Label count, Label word)
            {
                Root = root;
                Glow = glow;
                Icon = icon;
                Count = count;
                Word = word;
            }

            public VisualElement Root { get; }

            public VisualElement Glow { get; }

            public VisualElement Icon { get; }

            public Label Count { get; }

            public Label Word { get; }

            public int WrittenPair { get; set; } = -1;

            public int Written { get; set; } = -1;

            public bool Shown { get; set; } = true;

            public bool Short { get; set; }

            public float WrittenGlow { get; set; } = -1f;

            public float WrittenTick { get; set; } = -1f;
        }
    }
}
