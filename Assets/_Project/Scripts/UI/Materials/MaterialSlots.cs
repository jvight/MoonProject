using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// One row of the three materials, built into a container: per material a painted icon and a number, in Metal,
    /// Wiring, Optics order. The materials chip, the pause summary and every recipe use it. A material can be left out
    /// of the layout, shown dimmed (short), and given a glow behind its icon. Writes styles and text only when they
    /// change.
    /// </summary>
    internal sealed class MaterialSlots
    {
        public const string SlotClass = "material-slot";
        public const string ShortClass = "material-slot--short";
        public const string IconClass = "material-icon";
        public const string GlowClass = "material-slot__glow";
        public const string CountClass = "material-slot__count";

        private const float WriteEpsilon = 1e-3f;

        private readonly IntText _numbers;
        private readonly Slot[] _slots = new Slot[Materials.Count];

        /// <param name="container">Where the three slots go (emptied first).</param>
        /// <param name="numbers">Cached number strings.</param>
        /// <param name="withGlow">True: each icon gets a soft halo behind it for <see cref="SetGlow"/>.</param>
        public MaterialSlots(VisualElement container, IntText numbers, bool withGlow)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
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
                root.Add(count);
                container.Add(root);
                new MaterialIconPainter(icon, material);
                _slots[i] = new Slot(root, glow, icon, count);
                SetCount(material, 0);
            }
        }

        /// <summary>The number on <paramref name="material"/>'s slot (tests and captures).</summary>
        public string Text(SalvageMaterial material)
        {
            return _slots[(int)material].Count.text;
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

        /// <summary>Swells <paramref name="material"/>'s icon and its halo by <paramref name="glow"/> (0..1).</summary>
        public void SetGlow(SalvageMaterial material, float glow, float scale, float opacity)
        {
            Slot slot = _slots[(int)material];
            if (slot.Glow == null || Mathf.Abs(glow - slot.WrittenGlow) < WriteEpsilon)
            {
                return;
            }

            float grown = 1f + glow * scale;
            slot.Icon.style.scale = new StyleScale(new Scale(new Vector2(grown, grown)));
            slot.Glow.style.opacity = glow * opacity;
            slot.WrittenGlow = glow;
        }

        private sealed class Slot
        {
            public Slot(VisualElement root, VisualElement glow, VisualElement icon, Label count)
            {
                Root = root;
                Glow = glow;
                Icon = icon;
                Count = count;
            }

            public VisualElement Root { get; }

            public VisualElement Glow { get; }

            public VisualElement Icon { get; }

            public Label Count { get; }

            public int Written { get; set; } = -1;

            public bool Shown { get; set; } = true;

            public bool Short { get; set; }

            public float WrittenGlow { get; set; } = -1f;
        }
    }
}
