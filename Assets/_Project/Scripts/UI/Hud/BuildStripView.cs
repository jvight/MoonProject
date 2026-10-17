using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI
{
    /// <summary>
    /// The build strip (0.5.7 slice 6, BUILD-MODE) and the world-anchored placement reason chip. While 07 is in
    /// <see cref="BuildModeState.Menu"/> or <see cref="BuildModeState.Placing"/>, a calm Moonlight Glass strip at the
    /// bottom of the HUD lists the structures 07 has learned with their 256 px icons, localized names, description,
    /// material cost rows (<see cref="RecipeView"/>), the share of 07's battery the weld takes
    /// (<see cref="IWeldEnergy"/>), and input hints. When the placement ghost sits on an invalid
    /// spot in <see cref="BuildModeState.Placing"/>, a one-line reason chip floats above the ghost via
    /// <see cref="WorldAnchor"/>. Zero per-frame GC allocations.
    /// </summary>
    internal sealed class BuildStripView
    {
        public const string ShortStripClass = "build-strip--short";
        public const string CardClass = "build-card";
        public const string SelectedCardClass = "build-card--selected";
        public const string ShortCardClass = "build-card--short";
        public const string CardIconClass = "build-card__icon";
        public const string CardNameClass = "build-card__name";
        public const string CardCostClass = "build-card__cost";
        public const string CardEnergyClass = "build-card__energy";

        private const float ReasonAnchorHeightOffset = 0.35f;

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly IntText _numbers;
        private readonly IMaterialStock _stock;
        private readonly BuildField _buildField;
        private readonly GlyphLabels _glyphs;
        private readonly IWeldEnergy _weldEnergy;
        private readonly string[] _energyTexts = new string[WeldEnergyShare.MaxPercent + 1];
        private readonly GlyphView _primaryGlyph;
        private readonly GlyphView _secondaryGlyph;
        private readonly Reveal _stripReveal;
        private readonly Reveal _reasonReveal;
        private readonly WorldAnchor _reasonAnchor;
        private readonly RecipeView _recipe;
        private readonly Dictionary<string, Texture2D> _icons =
            new Dictionary<string, Texture2D>(StructureIds.Count, StringComparer.Ordinal);
        private readonly List<CardSlot> _cards = new List<CardSlot>(StructureIds.Count);

        private int _shownAvailableCount = -1;
        private int _shownSelectedIndex = -1;
        private string _shownSelectedId;
        private int _shownMetal = -1;
        private int _shownWiring = -1;
        private int _shownOptics = -1;
        private BuildModeState _shownState = BuildModeState.Closed;
        private bool _shownSnapped;
        private InputDeviceKind _shownDevice = (InputDeviceKind)(-1);
        private PlacementReason _shownReason = PlacementReason.Valid;
        private bool _stale = true;

        public BuildStripView(
            UiLayout layout,
            RevealSettings stripReveal,
            PromptSettings promptSettings,
            ILocalization localization,
            IntText numbers,
            IMaterialStock stock,
            IViewCamera view,
            BuildField buildField = null,
            Texture2D[] structureIcons = null,
            GlyphLabels glyphs = null,
            IWeldEnergy weldEnergy = null)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (stripReveal == null)
            {
                throw new ArgumentNullException(nameof(stripReveal));
            }

            if (promptSettings == null)
            {
                throw new ArgumentNullException(nameof(promptSettings));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _buildField = buildField;
            _glyphs = glyphs;
            _weldEnergy = weldEnergy;
            _primaryGlyph = new GlyphView(layout.BuildStripGlyphPrimary, layout.BuildStripGlyphPrimaryLabel);
            _secondaryGlyph = new GlyphView(layout.BuildStripGlyphSecondary, layout.BuildStripGlyphSecondaryLabel);

            _stripReveal = new Reveal(layout.BuildStrip, stripReveal);
            _stripReveal.Snap(false);

            _reasonReveal = new Reveal(layout.BuildReason, promptSettings.Reveal);
            _reasonReveal.Snap(false);

            _reasonAnchor = new WorldAnchor(
                layout.BuildReasonAnchor,
                view ?? throw new ArgumentNullException(nameof(view)),
                promptSettings.ScreenMargin,
                promptSettings.FollowHalfLife,
                true);

            _recipe = new RecipeView(layout.BuildStripRecipe, localization, numbers, haveNeed: false, names: false);
            new ShadowPainter(layout.BuildStripShadow);
            new ShadowPainter(layout.BuildReasonShadow);

            if (structureIcons != null)
            {
                for (int i = 0; i < structureIcons.Length; i++)
                {
                    Texture2D icon = structureIcons[i];
                    if (icon != null && !string.IsNullOrEmpty(icon.name))
                    {
                        _icons[icon.name] = icon;
                    }
                }
            }
        }

        /// <summary>True while the build strip is visible or easing.</summary>
        public bool IsVisible => !_stripReveal.IsHidden;

        /// <summary>True while the build mode is in Menu or Placing state.</summary>
        public bool IsOpen =>
            _buildField != null &&
            (_buildField.State == BuildModeState.Menu || _buildField.State == BuildModeState.Placing);

        /// <summary>True while the world-anchored placement reason chip is visible.</summary>
        public bool IsReasonVisible => !_reasonReveal.IsHidden;

        /// <summary>The recipe cost view on the strip (for tests and captures).</summary>
        public RecipeView Recipe => _recipe;

        /// <summary>The energy words on card <paramref name="index"/> (tests and captures).</summary>
        public string EnergyText(int index) => _cards[index].Energy.text;

        /// <summary>Number of structure cards currently displayed in the strip.</summary>
        public int CardCount => _shownAvailableCount > 0 ? _shownAvailableCount : 0;

        public void Tick(
            float deltaTime,
            bool gateOpen,
            Vector2 panelSize,
            InputDeviceKind device = InputDeviceKind.KeyboardMouse)
        {
            BuildModeController controller = _buildField != null ? _buildField.Controller : null;
            BuildModeState state = controller != null ? controller.State : BuildModeState.Closed;

            bool wantsStrip = gateOpen &&
                              controller != null &&
                              state == BuildModeState.Menu &&
                              controller.AvailableStructures.Count > 0;
            if (wantsStrip)
            {
                RefreshStrip(controller, state, device);
            }

            _stripReveal.Set(wantsStrip);
            _stripReveal.Tick(deltaTime);

            bool wantsReason = gateOpen &&
                               controller != null &&
                               state == BuildModeState.Placing &&
                               controller.HasCandidatePoint &&
                               controller.CandidateReason != PlacementReason.Valid;
            bool onScreen = false;
            if (wantsReason)
            {
                RefreshReason(controller.CandidateReason);
                Vector3 anchorWorld = controller.CandidatePose.Position - Vector3.up * ReasonAnchorHeightOffset;
                onScreen = _reasonAnchor.Track(anchorWorld, panelSize, deltaTime, _reasonReveal.IsHidden);
            }

            _reasonReveal.Set(wantsReason && onScreen);
            _reasonReveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _recipe.Relocalize();
            Array.Clear(_energyTexts, 0, _energyTexts.Length);
            _stale = true;
            _shownReason = PlacementReason.Valid;
        }

        private void RefreshStrip(BuildModeController controller, BuildModeState state, InputDeviceKind device)
        {
            IReadOnlyList<StructureDefinition> available = controller.AvailableStructures;
            int count = available.Count;
            int selectedIndex = controller.SelectedIndex;
            StructureDefinition selected = controller.SelectedStructure;
            int metal = _stock.Metal;
            int wiring = _stock.Wiring;
            int optics = _stock.Optics;
            bool snapped = controller.CandidateSnapped;

            bool cardsChanged = _stale || count != _shownAvailableCount;
            bool stockChanged = metal != _shownMetal || wiring != _shownWiring || optics != _shownOptics;
            bool selectionChanged = selectedIndex != _shownSelectedIndex ||
                                    (selected != null && !ReferenceEquals(selected.Id, _shownSelectedId));
            bool modeChanged = state != _shownState || snapped != _shownSnapped;
            bool deviceChanged = device != _shownDevice;
            bool energyChanged = !cardsChanged && EnergyChanged(available, count);

            if (!cardsChanged && !stockChanged && !selectionChanged && !modeChanged && !deviceChanged &&
                !energyChanged)
            {
                return;
            }

            EnsureCardPool(count);

            for (int i = 0; i < _cards.Count; i++)
            {
                CardSlot slot = _cards[i];
                bool active = i < count;
                slot.Root.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                if (!active)
                {
                    continue;
                }

                StructureDefinition def = available[i];
                if (cardsChanged || !ReferenceEquals(slot.StructureId, def.Id))
                {
                    slot.StructureId = def.Id;
                    slot.Name.text = _localization.Get(UiKeys.StructureName(def.Id));
                    Texture2D icon = ResolveIcon(def.Id);
                    slot.Icon.style.backgroundImage = icon != null
                        ? new StyleBackground(icon)
                        : new StyleBackground(StyleKeyword.None);
                }

                var cardStatus = new RecipeStatus(def.BuildCost, metal, wiring, optics);
                slot.CostSlots.ShowRecipe(def.BuildCost, cardStatus);
                WriteEnergy(slot, EnergyPercentOf(def), cardsChanged);
                slot.Root.EnableInClassList(SelectedCardClass, i == selectedIndex);
                slot.Root.EnableInClassList(ShortCardClass, !cardStatus.IsAffordable);
            }

            if (_stale)
            {
                _layout.BuildStripTitle.text = _localization.Get(UiKeys.BuildStripTitle);
            }

            if (selected != null)
            {
                if (_stale || selectionChanged)
                {
                    _layout.BuildStripName.text = _localization.Get(UiKeys.StructureName(selected.Id));
                    _layout.BuildStripDesc.text = _localization.Get(UiKeys.StructureDescription(selected.Id));
                }

                _recipe.Show(selected.BuildCost, _stock);
                bool affordable = _recipe.IsAffordable;
                _layout.BuildStripStatus.text = _localization.Get(
                    affordable ? UiKeys.BuildCostAffordable : UiKeys.BuildCostShort);
                _layout.BuildStrip.EnableInClassList(ShortStripClass, !affordable);
            }

            if (_stale || modeChanged || deviceChanged)
            {
                string excavateGlyph = _glyphs != null
                    ? _glyphs.For(RoverAction.Excavate, device)
                    : (device == InputDeviceKind.Gamepad ? "X" : "E");
                string winchGlyph = _glyphs != null
                    ? _glyphs.For(RoverAction.Winch, device)
                    : (device == InputDeviceKind.Gamepad ? "D-Pad" : "Scroll");
                if (state == BuildModeState.Menu)
                {
                    _primaryGlyph.Set(excavateGlyph, device);
                    _secondaryGlyph.Set(winchGlyph, device);
                    _layout.BuildStripHintPrimary.text = _localization.Get(UiKeys.BuildHintPlace);
                    _layout.BuildStripHintSecondary.text = _localization.Get(UiKeys.BuildHintRotate);
                }
                else
                {
                    _primaryGlyph.Set(winchGlyph, device);
                    _secondaryGlyph.Set(excavateGlyph, device);
                    _layout.BuildStripHintPrimary.text = _localization.Get(UiKeys.BuildHintRotate);
                    _layout.BuildStripHintSecondary.text = _localization.Get(
                        snapped ? UiKeys.BuildHintSnap : UiKeys.BuildHintPlace);
                }
            }

            _shownAvailableCount = count;
            _shownSelectedIndex = selectedIndex;
            _shownSelectedId = selected != null ? selected.Id : null;
            _shownMetal = metal;
            _shownWiring = wiring;
            _shownOptics = optics;
            _shownState = state;
            _shownSnapped = snapped;
            _shownDevice = device;
            _stale = false;
        }

        private int EnergyPercentOf(StructureDefinition definition)
        {
            return _weldEnergy != null ? WeldEnergyShare.Percent(_weldEnergy.WeldShare01(definition)) : -1;
        }

        private bool EnergyChanged(IReadOnlyList<StructureDefinition> available, int count)
        {
            if (_weldEnergy == null)
            {
                return false;
            }

            for (int i = 0; i < count && i < _cards.Count; i++)
            {
                if (_cards[i].EnergyPercent != EnergyPercentOf(available[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void WriteEnergy(CardSlot slot, int percent, bool rewrite)
        {
            if (percent == slot.EnergyPercent && !rewrite)
            {
                return;
            }

            slot.EnergyPercent = percent;
            slot.Energy.style.display = percent >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (percent >= 0)
            {
                slot.Energy.text = _energyTexts[percent] ?? (_energyTexts[percent] = string.Format(
                    _localization.Get(UiKeys.BuildEnergyCard), _numbers.Get(percent) + "%"));
            }
        }

        private void RefreshReason(PlacementReason reason)
        {
            if (!_stale && reason == _shownReason)
            {
                return;
            }

            string key = UiKeys.PlacementReasonKey(reason);
            _layout.BuildReasonText.text = key != null ? _localization.Get(key) : string.Empty;
            _shownReason = reason;
        }

        private void EnsureCardPool(int count)
        {
            while (_cards.Count < count)
            {
                var root = new VisualElement { pickingMode = PickingMode.Ignore };
                root.AddToClassList(CardClass);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(CardIconClass);
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList(CardNameClass);
                var costRow = new VisualElement { pickingMode = PickingMode.Ignore };
                costRow.AddToClassList(CardCostClass);
                var costSlots = new MaterialSlots(costRow, _numbers, haveNeed: false, names: false);
                var energy = new Label { pickingMode = PickingMode.Ignore };
                energy.AddToClassList(CardEnergyClass);
                root.Add(icon);
                root.Add(name);
                root.Add(costRow);
                root.Add(energy);
                _layout.BuildStripCards.Add(root);
                _cards.Add(new CardSlot(root, icon, name, costSlots, energy));
            }
        }

        private Texture2D ResolveIcon(string structureId)
        {
            if (string.IsNullOrEmpty(structureId))
            {
                return null;
            }

            if (_icons.TryGetValue(structureId, out Texture2D cached))
            {
                return cached;
            }

#if UNITY_EDITOR
            string path = "Assets/_Project/Generated/UI/StructureIcons/" + structureId + ".png";
            var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (loaded != null)
            {
                _icons[structureId] = loaded;
                return loaded;
            }
#endif
            return null;
        }

        private sealed class CardSlot
        {
            public CardSlot(VisualElement root, VisualElement icon, Label name, MaterialSlots costSlots, Label energy)
            {
                Root = root;
                Icon = icon;
                Name = name;
                CostSlots = costSlots;
                Energy = energy;
            }

            public VisualElement Root { get; }

            public VisualElement Icon { get; }

            public Label Name { get; }

            public MaterialSlots CostSlots { get; }

            /// <summary>The share of 07's battery the weld takes, in words.</summary>
            public Label Energy { get; }

            public string StructureId { get; set; }

            /// <summary>The whole percent last written on <see cref="Energy"/>; -1 when it shows none.</summary>
            public int EnergyPercent { get; set; } = int.MinValue;
        }
    }
}
