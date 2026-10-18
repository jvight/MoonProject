using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI
{
    /// <summary>
    /// Build mode on screen, one step at a time (0.5.7 slice 6, BUILD-MODE; owner feedback 2026-10-18).
    /// <list type="number">
    /// <item><description><see cref="BuildModeState.Menu"/>: a compact Moonlight Glass row of cards at the bottom of the
    /// HUD, one per structure 07 has learned, each with its icon, name, material cost and the share of 07's battery the
    /// weld takes (<see cref="IWeldEnergy"/>). The mouse hovers a card to select it and clicks to pick it; the keys
    /// under the row name the real controls of the active device (<see cref="BuildKeyHints"/>).</description></item>
    /// <item><description><see cref="BuildModeState.Placing"/>: the cards leave; <see cref="Step"/> and
    /// <see cref="StepText"/> give the guidance line its one instruction, and when the ghost sits on an invalid spot a
    /// one-line reason chip floats above it via <see cref="WorldAnchor"/>.</description></item>
    /// <item><description><see cref="BuildModeState.Welding"/>: the guidance line says to hold the key, with
    /// <see cref="WeldProgress01"/> for its bar.</description></item>
    /// </list>
    /// Zero per-frame GC allocations: words are written only when they change.
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

        private const float ReasonAnchorHeightOffset = 1.15f;
        private const int StepCount = 3;

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly IntText _numbers;
        private readonly IMaterialStock _stock;
        private readonly BuildField _buildField;
        private readonly GlyphLabels _glyphs;
        private readonly IWeldEnergy _weldEnergy;
        private readonly EventBus _events;
        private readonly string[] _energyTexts = new string[WeldEnergyShare.MaxPercent + 1];
        private readonly string[] _stepTexts = new string[StepCount];
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
        private InputDeviceKind _shownDevice = (InputDeviceKind)(-1);
        private InputDeviceKind _stepDevice = (InputDeviceKind)(-1);
        private PlacementReason _shownReason = PlacementReason.Valid;
        private bool _stale = true;
        private int _lastPickFrame = -1;

        public BuildStripView(
            UiLayout layout,
            RevealSettings stripReveal,
            PromptSettings promptSettings,
            ILocalization localization,
            IntText numbers,
            IMaterialStock stock,
            IViewCamera view,
            GlyphLabels glyphs,
            EventBus events,
            BuildField buildField = null,
            Texture2D[] structureIcons = null,
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
            _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _buildField = buildField;
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

        /// <summary>True while the card row is visible or easing.</summary>
        public bool IsVisible => !_stripReveal.IsHidden;

        /// <summary>True while the build mode is in Menu or Placing state.</summary>
        public bool IsOpen =>
            _buildField != null &&
            (_buildField.State == BuildModeState.Menu || _buildField.State == BuildModeState.Placing);

        /// <summary>The build mode's state (Closed without a build field).</summary>
        public BuildModeState State => _buildField != null ? _buildField.State : BuildModeState.Closed;

        /// <summary>True while the world-anchored placement reason chip is visible.</summary>
        public bool IsReasonVisible => !_reasonReveal.IsHidden;

        /// <summary>The words on the reason chip (tests and captures).</summary>
        public string ReasonText => _layout.BuildReasonText.text;

        /// <summary>The recipe cost view on the strip (for tests and captures).</summary>
        public RecipeView Recipe => _recipe;

        /// <summary>The step the guidance line explains now (None while the gate is shut).</summary>
        public BuildStep Step { get; private set; }

        /// <summary>The words for <see cref="Step"/> with the active device's key; the same string until it changes.</summary>
        public string StepText => _stepTexts[(int)Step] ?? string.Empty;

        /// <summary>0..1 how far the structure being welded is.</summary>
        public float WeldProgress01 { get; private set; }

        /// <summary>The energy words on card <paramref name="index"/> (tests and captures).</summary>
        public string EnergyText(int index) => _cards[index].Energy.text;

        /// <summary>Card <paramref name="index"/>'s element (tests and captures).</summary>
        public VisualElement Card(int index) => _cards[index].Root;

        /// <summary>Number of structure cards currently displayed in the strip.</summary>
        public int CardCount => _shownAvailableCount > 0 ? _shownAvailableCount : 0;

        public void Tick(float deltaTime, bool gateOpen, Vector2 panelSize, InputDeviceKind device)
        {
            BuildModeController controller = _buildField != null ? _buildField.Controller : null;
            BuildModeState state = controller != null ? controller.State : BuildModeState.Closed;

            bool wantsStrip = gateOpen &&
                              controller != null &&
                              state == BuildModeState.Menu &&
                              controller.AvailableStructures.Count > 0;
            if (wantsStrip)
            {
                RefreshStrip(controller, device);
            }

            _stripReveal.Set(wantsStrip);
            _stripReveal.Tick(deltaTime);

            Step = gateOpen && controller != null
                ? BuildSteps.Of(state, controller.ActiveWeldInstanceId)
                : BuildStep.None;
            WeldProgress01 = Step == BuildStep.Weld ? controller.ActiveWeldProgress01 : 0f;
            if (Step != BuildStep.None)
            {
                WriteStepText(device);
            }

            bool wantsReason = gateOpen &&
                               controller != null &&
                               state == BuildModeState.Placing &&
                               controller.HasCandidatePoint &&
                               controller.CandidateReason != PlacementReason.Valid;
            bool onScreen = false;
            if (wantsReason)
            {
                RefreshReason(controller.CandidateReason);
                Vector3 anchorWorld = controller.CandidatePose.Position + Vector3.up * ReasonAnchorHeightOffset;
                onScreen = _reasonAnchor.Track(anchorWorld, panelSize, deltaTime, _reasonReveal.IsHidden);
            }

            _reasonReveal.Set(wantsReason && onScreen);
            _reasonReveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _recipe.Relocalize();
            Array.Clear(_energyTexts, 0, _energyTexts.Length);
            Array.Clear(_stepTexts, 0, _stepTexts.Length);
            _stale = true;
            _shownReason = PlacementReason.Valid;
        }

        /// <summary>
        /// A click on card <paramref name="index"/> picks it, as the place key does on the selected card: the controller
        /// leaves the menu for placing and the ghost appears.
        /// </summary>
        public bool Pick(int index)
        {
            BuildModeController controller = _buildField != null ? _buildField.Controller : null;
            if (controller == null || controller.State != BuildModeState.Menu || _lastPickFrame == Time.frameCount ||
                !controller.PickStructure(index))
            {
                return false;
            }

            _lastPickFrame = Time.frameCount;
            _events.Publish(new UiCue(UiCueKind.Confirm));
            return true;
        }

        /// <summary>The mouse moved to <paramref name="panelPos"/> (HUD panel space): the card under it is selected.</summary>
        public void PointerMoveAt(Vector2 panelPos)
        {
            int card = CardAt(panelPos);
            if (card >= 0)
            {
                Hover(card);
            }
        }

        /// <summary>The mouse went down at <paramref name="panelPos"/> (HUD panel space): the card under it is picked.</summary>
        public void PointerDownAt(Vector2 panelPos)
        {
            int card = CardAt(panelPos);
            if (card >= 0)
            {
                Pick(card);
            }
        }

        private int CardAt(Vector2 panelPos)
        {
            if (float.IsNaN(panelPos.x) || float.IsNaN(panelPos.y) || _stripReveal.IsHidden)
            {
                return -1;
            }

            for (int i = 0; i < CardCount && i < _cards.Count; i++)
            {
                if (_cards[i].Root.worldBound.Contains(panelPos))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The pointer over card <paramref name="index"/> selects it, the way the arrows do.</summary>
        private void Hover(int index)
        {
            BuildModeController controller = _buildField != null ? _buildField.Controller : null;
            if (controller == null || controller.State != BuildModeState.Menu || controller.SelectedIndex == index ||
                !controller.SelectIndex(index))
            {
                return;
            }

            _events.Publish(new UiCue(UiCueKind.FocusMove));
        }

        private void WriteStepText(InputDeviceKind device)
        {
            if (device != _stepDevice)
            {
                _stepDevice = device;
                Array.Clear(_stepTexts, 0, _stepTexts.Length);
            }

            int index = (int)Step;
            if (_stepTexts[index] == null)
            {
                _stepTexts[index] = string.Format(_localization.Get(BuildSteps.KeyOf(Step)),
                    _glyphs.For(RoverAction.Excavate, device));
            }
        }

        private void RefreshStrip(BuildModeController controller, InputDeviceKind device)
        {
            IReadOnlyList<StructureDefinition> available = controller.AvailableStructures;
            int count = available.Count;
            int selectedIndex = controller.SelectedIndex;
            StructureDefinition selected = controller.SelectedStructure;
            int metal = _stock.Metal;
            int wiring = _stock.Wiring;
            int optics = _stock.Optics;

            bool cardsChanged = _stale || count != _shownAvailableCount;
            bool stockChanged = metal != _shownMetal || wiring != _shownWiring || optics != _shownOptics;
            bool selectionChanged = selectedIndex != _shownSelectedIndex ||
                                    (selected != null && !ReferenceEquals(selected.Id, _shownSelectedId));
            bool deviceChanged = device != _shownDevice;
            bool energyChanged = !cardsChanged && EnergyChanged(available, count);

            if (!cardsChanged && !stockChanged && !selectionChanged && !deviceChanged && !energyChanged)
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

            if (_stale || deviceChanged)
            {
                _primaryGlyph.Set(_localization.Get(BuildKeyHints.CapKey(BuildHint.Pick, device)), device);
                _secondaryGlyph.Set(_localization.Get(BuildKeyHints.CapKey(BuildHint.Leave, device)), device);
                _layout.BuildStripHintPrimary.text = _localization.Get(BuildKeyHints.WordKey(BuildHint.Pick));
                _layout.BuildStripHintSecondary.text = _localization.Get(BuildKeyHints.WordKey(BuildHint.Leave));
            }

            _shownAvailableCount = count;
            _shownSelectedIndex = selectedIndex;
            _shownSelectedId = selected != null ? selected.Id : null;
            _shownMetal = metal;
            _shownWiring = wiring;
            _shownOptics = optics;
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

        /// <summary>
        /// Cards are made once per structure count and reused; each one takes the pointer (hover selects, a press
        /// picks), while everything inside it lets the pointer through to the card.
        /// </summary>
        private void EnsureCardPool(int count)
        {
            while (_cards.Count < count)
            {
                int index = _cards.Count;
                var root = new VisualElement { name = "build-card-" + index, pickingMode = PickingMode.Position };
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
                root.RegisterCallback<PointerEnterEvent>(_ => Hover(index));
                root.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        Pick(index);
                    }
                });
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
