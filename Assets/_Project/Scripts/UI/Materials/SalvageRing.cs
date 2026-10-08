using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The salvage hold ring (docs/features/M3-13): centred on the piece's cut point, it fills with the cut's progress
    /// (<see cref="ISalvageStatus.Progress"/>). Progress is kept when Interact lets go (ruling 4), so while 07 still
    /// aims at a half-cut piece the ring keeps showing how far it came. It appears once there is a cut to show
    /// (cutting, or progress kept) and eases away when the piece is no longer aimed at; the "E Cut" prompt teaches the
    /// start. Inside the ring, on a small glass disc, sits the icon of the material the piece folds into.
    /// </summary>
    internal sealed class SalvageRing
    {
        public const string IconClass = "salvage-ring__icon";

        private readonly ISalvageStatus _salvage;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly ProgressRingPainter _ring;
        private readonly VisualElement[] _icons = new VisualElement[Materials.Count];
        private int _shownMaterial = -1;

        public SalvageRing(UiLayout layout, SalvageSettings settings, PromptSettings anchoring, ISalvageStatus salvage,
            IViewCamera view)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (anchoring == null)
            {
                throw new ArgumentNullException(nameof(anchoring));
            }

            _salvage = salvage ?? throw new ArgumentNullException(nameof(salvage));
            _reveal = new Reveal(layout.SalvageRing, settings.Ring);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(layout.SalvageRingAnchor, view, anchoring.ScreenMargin, anchoring.FollowHalfLife,
                false);
            _ring = new ProgressRingPainter(layout.SalvageRing);
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(MaterialSlots.IconClass);
                icon.AddToClassList(MaterialSlots.IconClass + "--" + Materials.ClassSuffix(material));
                icon.AddToClassList(IconClass);
                icon.style.display = DisplayStyle.None;
                new MaterialIconPainter(icon, material);
                layout.SalvageRing.Add(icon);
                _icons[i] = icon;
            }
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The ring's fill (tests and captures).</summary>
        public float Progress => _ring.Progress;

        /// <summary>The material whose icon sits in the ring (tests and captures).</summary>
        public SalvageMaterial ShownMaterial => Materials.At(_shownMaterial < 0 ? 0 : _shownMaterial);

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="gateOpen">False while something else has the player's attention.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        public void Tick(float deltaTime, bool gateOpen, Vector2 panelSize)
        {
            bool aimed = _salvage.HasTarget;
            bool cut = aimed && (_salvage.IsCutting || _salvage.Progress > 0f);
            bool onScreen = cut && _anchor.Track(_salvage.CutPoint, panelSize, deltaTime, _reveal.IsHidden);
            if (aimed)
            {
                _ring.Progress = _salvage.Progress;
                ShowIcon((int)_salvage.Material);
            }

            _reveal.Set(gateOpen && cut && onScreen);
            _reveal.Tick(deltaTime);
        }

        private void ShowIcon(int material)
        {
            if (material == _shownMaterial)
            {
                return;
            }

            for (int i = 0; i < _icons.Length; i++)
            {
                _icons[i].style.display = i == material ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _shownMaterial = material;
        }
    }
}
